using System;
using System.Threading;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using TelemetrySigner.Models;

namespace TelemetrySigner
{
    /// <summary>
    /// RealTimeTelemetryManager class contains functionality for subscription of real time blocks data from a parity using web sockets, signing and sending that data to Influx and incase of failure sending data to provided SFTP 
    /// </summary>
    public class RealTimeTelemetryManager
    {
        private readonly string _jsonRpcUrl;
        private readonly string _webSocketUri;
        private readonly TalkToIngress _tti;
        private readonly PayloadSigner _signer;
        private readonly string _nodeId;
        private readonly bool _verbose;
        private readonly UTF8Encoding _encoder = new UTF8Encoding();
        private readonly FtpManager _ftpMgr;

        /// <summary>
        /// RealTimeTelemetryManager constructor for RealTimeTelemetryManager instance creation
        /// </summary>
        /// <param name="nodeId">Node Id </param>
        /// <param name="jsonRpcUrl">JSON Rpc of Parity </param>
        /// <param name="webSocketUrl">Web Socket URL of Parity </param>
        /// <param name="ingressEndPoint">Ingress REal time restful End Point </param>
        /// <param name="ingressFingerPrint">Ingress Finger Print </param>
        /// <param name="signer">Payload Signer instance reference </param>
        /// <param name="ftpMgr">FTPManager instance reference </param>
        /// <param name="verbose">if detailed logs are required set verbose to true </param>
        /// <returns>returns instance of RealTimeTelemetryManager</returns>
        /// <exception cref="System.ArgumentException">Thrown when any of provided argument is null or empty.</exception>
        public RealTimeTelemetryManager(string nodeId, string jsonRpcUrl,string webSocketUrl,  string ingressEndPoint, string ingressFingerPrint, PayloadSigner signer, FtpManager ftpMgr, bool verbose = true)
        {
            

            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException("Node ID is empty", nameof(nodeId));
            }

            if (string.IsNullOrWhiteSpace(jsonRpcUrl))
            {
                throw new ArgumentException("RPC URL is empty", nameof(jsonRpcUrl));
            }

            if (string.IsNullOrWhiteSpace(webSocketUrl))
            {
                throw new ArgumentException("Web Socket URL is empty", nameof(webSocketUrl));
            }

            if (string.IsNullOrWhiteSpace(ingressEndPoint))
            {
                throw new ArgumentException("Ingress END Point is empty", nameof(ingressEndPoint));
            }

            if (string.IsNullOrWhiteSpace(ingressFingerPrint))
            {
                throw new ArgumentException("Ingress Finger Point is empty", nameof(ingressFingerPrint));
            }

            _ftpMgr = ftpMgr ?? throw new ArgumentException("FTP Manager is null", nameof(ftpMgr));
            _signer = signer ?? throw new ArgumentException("Signer is null", nameof(signer));
            
            _webSocketUri = webSocketUrl;
            _jsonRpcUrl = jsonRpcUrl;
            _nodeId = nodeId;
            _verbose = verbose;


            _tti = new TalkToIngress(ingressEndPoint, ingressFingerPrint);
        }

        /// <summary>
        /// Subscribes to Parity for real time blocks using websocket and posts that to Influx restful end point
        /// </summary>
        /// <param name="reAttemptConnection">Flag for re attempting connection control</param>
        public void SubscribeAndPost(bool reAttemptConnection)
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            do
            {
                try
                {
                    if (webSocket.State != WebSocketState.Connecting
                    || webSocket.State != WebSocketState.Open
                    )
                    {
                        Console.WriteLine("Connecting to websocket for Realtime Telemetry");
                        Connect(webSocket).Wait();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception occurred in WebSocket Connection: {0} => {1}", ex.Message, ex.InnerException?.Message ?? "");
                    Console.WriteLine("Reconnecting in 20 seconds");
                    //wait 20 second and re attempt operations based on flag(websocket connection to parity, data pushing to ingress)
                    if (reAttemptConnection) { Thread.Sleep(20000); }
                }
                //reconnect the websocket right away if connection drops
            } while (reAttemptConnection) ;
        }

        /// <summary>
        /// Connects to provided websocker and waits for response
        /// </summary>
        /// <param name="webSocket">ClientWebSocket instance reference</param>
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
        /// Send method for web socket connection
        /// </summary>
        /// <param name="webSocket">ClientWebSocket instance reference</param>
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
        /// Receive method for web socket connection, It receivesreal time block data from parity and sends that to Ingress restful end point
        /// </summary>
        /// <param name="webSocket">ClientWebSocket instance reference</param>
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
                    RealTimeTelemetry rtt = ParseAndSignData(buffer);
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
        /// Function for parsing and signing block data 
        /// </summary>
        /// <param name="buffer">block data in byte array</param>
        /// <returns>returns instance of RealTimeTelemetry if valid data is provided else null</returns>
        private RealTimeTelemetry ParseAndSignData(byte[] buffer)
        {
            try
            {
                string numPeers = GetCurrentNumPeers();
                string clientVersion = GetCurrentClientVersion();

                dynamic jsonObj = JsonConvert.DeserializeObject(_encoder.GetString(buffer).Trim());
                dynamic paramsObj = jsonObj["params"];
                if (paramsObj != null && paramsObj["result"] != null)
                {
                    dynamic resultObj = paramsObj["result"];

                    string gasLimit = resultObj["gasLimit"];
                    string gasUsed = resultObj["gasUsed"];

                    long currentEPoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

                    RealTimeTelemetryPayload rttp = new RealTimeTelemetryPayload
                    {
                        Client = clientVersion,
                        BlockNum = Convert.ToUInt64(resultObj["number"].ToString(), 16),
                        BlockHash = resultObj["hash"].ToString(),
                        BlockTS = Convert.ToInt64(resultObj["timestamp"].ToString(), 16),
                        BlockReceived = currentEPoch,
                        NumPeers = Convert.ToUInt16(numPeers, 16),
                        NumTxInBlock = (ushort)resultObj["transactions"].Count,
                        GasLimit =  Convert.ToInt64(gasLimit,16),
                        GasUsed = Convert.ToInt64(gasUsed,16)
                    };

                    RealTimeTelemetry rtt = new RealTimeTelemetry
                    {
                        NodeId = _nodeId,
                        Payload = rttp,
                        Signature = _signer.SignPayload(JsonConvert.SerializeObject(rttp))
                    };

                    if (_verbose)
                    {
                        Console.WriteLine("New Block received");
                        Console.WriteLine("block num: {0}", rttp.BlockNum);
                        Console.WriteLine("block hash: {0}", rttp.BlockHash);
                        Console.WriteLine("block time stamp: {0}", rttp.BlockTS);
                        Console.WriteLine("Tx Count: {0}", rttp.NumTxInBlock);
                        Console.WriteLine("Gas Limit: {0}", Convert.ToInt64(gasLimit, 16));
                        Console.WriteLine("Gas Used: {0}", Convert.ToInt64(gasUsed, 16));
                        Console.WriteLine("Peers: {0} ", rttp.NumPeers);
                    }

                    return rtt;
                }

                Console.WriteLine("Unable to Parse\\Sign real Time data. Invalid Data");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to Parse\\Sign real Time data. Error Occurred {0}", ex);
            }
            return null;

        }

        

        private string GetCurrentClientVersion()
        {
            using (WebClient webClient = new WebClient())
            {
                const string json = "{\"method\":\"web3_clientVersion\",\"params\":[],\"id\":1,\"jsonrpc\":\"2.0\"}";
                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                string currentVersionResponse = webClient.UploadString(_jsonRpcUrl, "POST", json);

                dynamic jsonObjPeers = JsonConvert.DeserializeObject(currentVersionResponse.Trim());
                return jsonObjPeers["result"];    
            }
            
        }

        private string GetCurrentNumPeers()
        {
            using (WebClient webClient = new WebClient())
            {
                const string json = "{\"method\":\"net_peerCount\",\"params\":[],\"id\":1,\"jsonrpc\":\"2.0\"}";
                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                string numOfPeersResponse = webClient.UploadString(_jsonRpcUrl, "POST", json);

                dynamic jsonObjPeers = JsonConvert.DeserializeObject(numOfPeersResponse.Trim());
                return jsonObjPeers["result"];
            }
        }

        /// <summary>
        /// Function for sending data to ingress restful end point
        /// </summary>
        /// <param name="rtt">Reference of RealTimeTelemetry instance</param>
        private void SendDataToIngress(RealTimeTelemetry rtt)
        {

            try
            {
                //random pause of 55ms so we dnt have spike on ingress
                Thread.Sleep(new Random().Next(1, 500));

                //push data to ingress real time telemetry endpoint
                string jsonPayload = JsonConvert.SerializeObject(rtt);

                bool sendSuccess = _tti.SendRequest(jsonPayload).Result;

                if (!sendSuccess)
                {
                    // unable to send real time telemetry to ingress - send by second channel
                    Console.WriteLine("ERROR: Unable to send to real time telemetry ingress. Sending data on second channel.");

                    string fileName = $"{_nodeId}-{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.json";
                    try
                    {
                        if (!_ftpMgr.TransferData(jsonPayload, fileName))
                        {
                            Console.WriteLine("ERROR: Unable to send real time telemetry on second channel. Data File {0}", fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: Unable to send real time telemetry on second channel. Error Details {0}", ex);
                    }

                }
                else
                {
                    Console.WriteLine("Real Time Telemetry Block data sent to Ingress Block # {0}", rtt.Payload.BlockNum);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR Occurred While sending data to Ingress. {0}",ex);
            }

        }

    }

}
