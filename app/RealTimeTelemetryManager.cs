using System;
using System.Threading;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Net;
using TelemetrySigner.Models;

namespace TelemetrySigner
{

    /// <summary>
    /// Simple logger implementation that just sends the output to the console
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
    
    /// <summary>
    /// Interface that describes a simple logger
    /// </summary>
    public interface ILogger
    {
        void Log(string msg);
    }
    
    /// <summary>
    /// Used to collect realtime block information from parity
    /// </summary>
    public class RealTimeTelemetryManager
    {
        private readonly WebClient _webClient;
        private readonly string _jsonRpcUrl;
        private readonly string _webSocketUri;
        private readonly TalkToIngress _tti;
        private readonly PayloadSigner _signer;
        private readonly string _nodeId;
        private readonly bool _verbose;
        private readonly UTF8Encoding _encoder = new UTF8Encoding();
        private readonly ILogger _logger;

        public RealTimeTelemetryManager(SignerConfiguration config, PayloadSigner signer, ILogger logger, bool verbose  = false)
        {
            _webSocketUri = config.ParityWebSocketAddress;
            _jsonRpcUrl = config.ParityEndpoint;
            _signer = signer;
            _nodeId = config.NodeId;
            _verbose = verbose;
            _logger = logger;

            _webClient = new WebClient();

            _tti = new TalkToIngress($"{config.IngressHost}/api/ingress/realtime", config.IngressFingerprint);
        }

        /// <summary>
        /// Connects to parity via websocket and listen for new blocks. 
        /// </summary>
        /// <param name="reAttemptConnection"></param>
        public void SubscribeAndPost(bool reAttemptConnection)
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            do
            {
                try
                {
                    if (webSocket.State == WebSocketState.Connecting && webSocket.State == WebSocketState.Open)
                    {
                        continue;
                    }
                    _logger.Log("Connecting to websocket for Realtime Telemetry");
                    Connect(webSocket).Wait();
                }
                catch (Exception ex)
                {
                    _logger.Log($"Exception occurred in WebSocket Connection: {ex.Message} => {ex.InnerException?.Message ?? ""}");

                    //wait 20 second and re attempt operations based on flag(websocket connection to parity, data pushing to ingress)
                    if (reAttemptConnection)
                    {
                        _logger.Log("Reconnecting in 20 seconds");
                        Thread.Sleep(20000);
                    }
                }
                //reconnect the websocket right away if connection drops
            } while (reAttemptConnection) ;
        }

        /// <summary>
        /// Connect to websocket and start listening
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task Connect(ClientWebSocket webSocket)
        {

            // make sure to clean up the old one
            webSocket.Abort();
            webSocket.Dispose();
            webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(new Uri(_webSocketUri), CancellationToken.None);

            await Task.WhenAll(Receive(webSocket), Send(webSocket));
        }


        /// <summary>
        /// Subscribe to new blocks and keep pinging the websocket to avoid timeout 
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task Send(ClientWebSocket webSocket)
        {
            byte[] buffer = _encoder.GetBytes("{\"method\":\"parity_subscribe\",\"params\":[\"eth_getBlockByNumber\",[\"latest\",true]],\"id\":1,\"jsonrpc\":\"2.0\"}");
            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);

            // re subscription so server knows that client is not timing out etc
            while (webSocket.State == WebSocketState.Open)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(30000));
            }
        }

        /// <summary>
        /// Listen for data coming in on the websocket
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task Receive(ClientWebSocket webSocket)
        {
            int bufferSize = 4096;
            byte[] temporaryBuffer = new byte[bufferSize];
            byte[] buffer = new byte[bufferSize * 20];
            int offset = 0;

            while (true)
            {
                WebSocketReceiveResult response = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(temporaryBuffer),
                    CancellationToken.None);
                temporaryBuffer.CopyTo(buffer, offset);
                offset += response.Count;
                temporaryBuffer = new byte[bufferSize];

                if (response.EndOfMessage)
                {
                    // fetch more information 
                    // Get extra information from parity
                    string numPeers = GetCurrentNumPeers();
                    string clientVersion = GetCurrentClientVersion();
                    
                    RealTimeTelemetry rtt = ParseAndSignData(buffer,numPeers,clientVersion);
                    if (rtt != null)
                    {
                        SendDataToIngress(rtt);
                    }

                    Array.Clear(temporaryBuffer, 0, temporaryBuffer.Length);
                    Array.Clear(buffer, 0, buffer.Length);
                    offset = 0;
                }
                if (response.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                }
            }
        }

        /// <summary>
        /// Parse websocket message and return real time telemetry payload
        /// </summary>
        /// <param name="buffer">Received buffer from websocket</param>
        /// <param name="numPeers">Number of connected peers</param>
        /// <param name="clientVersion">Parity client version</param>
        /// <returns>telemetry payload</returns>
        private RealTimeTelemetry ParseAndSignData(byte[] buffer, string numPeers, string clientVersion)
        {
            try
            {
                // capture current unix timestamp as block received time
                long blockReceivedTimestamp = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                
                dynamic jsonObj = JsonConvert.DeserializeObject(_encoder.GetString(buffer).Trim());
                dynamic paramsObj = jsonObj["params"];
                if (paramsObj != null && paramsObj["result"] != null)
                {
                    dynamic resultObj = paramsObj["result"];
                    string gasLimit = resultObj["gasLimit"];
                    string gasUsed = resultObj["gasUsed"];

                    // Collect payload
                    RealTimeTelemetryPayload rttp = new RealTimeTelemetryPayload
                    {
                        Client = clientVersion,
                        BlockNum = Convert.ToUInt64(resultObj["number"].ToString(), 16),
                        BlockHash = resultObj["hash"].ToString(),
                        BlockTS = Convert.ToInt64(resultObj["timestamp"].ToString(), 16),
                        BlockReceived = blockReceivedTimestamp,
                        NumPeers = Convert.ToUInt16(numPeers, 16),
                        NumTxInBlock = (ushort)resultObj["transactions"].Count,
                        GasLimit =  Convert.ToInt64(gasLimit,16),
                        GasUsed = Convert.ToInt64(gasUsed,16)
                    };

                    // Prepare package and signature
                    RealTimeTelemetry rtt = new RealTimeTelemetry
                    {
                        NodeId = _nodeId,
                        Payload = rttp,
                        Signature = _signer.SignPayload(JsonConvert.SerializeObject(rttp))
                    };

                    if (_verbose)
                    {
                        _logger.Log("New Block received");
                        _logger.Log($"block num: {rttp.BlockNum}");
                        _logger.Log($"block hash: {rttp.BlockHash}");
                        _logger.Log($"block time stamp: {rttp.BlockTS}");
                        _logger.Log($"Tx Count: {rttp.NumTxInBlock}");
                        _logger.Log($"Gas Limit: {Convert.ToInt64(gasLimit, 16)}");
                        _logger.Log($"Gas Used: {Convert.ToInt64(gasUsed, 16)}");
                        _logger.Log($"Peers: {rttp.NumPeers}");
                    }

                    return rtt;
                }

                _logger.Log("Unable to Parse\\Sign real Time data. Invalid Data");
            }
            catch (Exception ex)
            {
                _logger.Log($"Unable to Parse\\Sign real Time data. Error Occurred {ex.Message}");
            }
            return null;

        }

        /// <summary>
        /// Get current client version over HTTP RPC
        /// </summary>
        /// <returns></returns>
        private string GetCurrentClientVersion()
        {
            const string json = "{\"method\":\"web3_clientVersion\",\"params\":[],\"id\":1,\"jsonrpc\":\"2.0\"}";
            _webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
            string currentVersionResponse = _webClient.UploadString(_jsonRpcUrl, "POST", json);

            dynamic jsonObjPeers = JsonConvert.DeserializeObject(currentVersionResponse.Trim());
            return jsonObjPeers["result"];
        }

        /// <summary>
        /// Get current number of peers via HTTP RPC
        /// </summary>
        /// <returns></returns>
        private string GetCurrentNumPeers()
        {
            const string json = "{\"method\":\"net_peerCount\",\"params\":[],\"id\":1,\"jsonrpc\":\"2.0\"}";
            _webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
            string numOfPeersResponse = _webClient.UploadString(_jsonRpcUrl, "POST", json);

            dynamic jsonObjPeers = JsonConvert.DeserializeObject(numOfPeersResponse.Trim());
            return jsonObjPeers["result"];
        }

        /// <summary>
        /// Send realtime telemetry package to ingress server
        /// </summary>
        /// <param name="rtt"></param>
        private void SendDataToIngress(RealTimeTelemetry rtt)
        {
            try
            {
                //random pause of 55ms so we dnt have spike on ingress
                Thread.Sleep(new Random().Next(1, 500));

                //push data to ingress real time telemetry endpoint
                bool sendSuccess = _tti.SendRequest(JsonConvert.SerializeObject(rtt)).Result;
                _logger.Log(!sendSuccess
                    ? "ERROR: Unable to send to ingress for more then. Use Sending queue on second channel."
                    : $"Real Time Telemetry Block data sent to Ingress Block #{rtt.Payload.BlockNum}");
            }
            catch (Exception ex)
            {
                _logger.Log($"ERROR Occurred While sending data to Ingress => {ex.Message}");
            }
        }
    }
}