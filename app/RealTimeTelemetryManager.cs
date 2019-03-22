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

namespace TelemetrySigner
{
    public class RealTimeTelemetryManager
    {
        private WebClient _webClient;
        private string _jsonRpcURL;
        private string _webSocketURI;
        private TalkToIngress _tti;
        private PayloadSigner _signer;
        private string _nodeId;

        private FTPManager _ftpMgr;

        UTF8Encoding encoder = new UTF8Encoding();

        public RealTimeTelemetryManager(string nodeId, string jsonRpcURL, string webSocketURL, string ingressEndPoint, string ingressFingerPrint, PayloadSigner signer, FTPManager ftpMgr, bool verbose)
        {

            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException("Node ID is empty", nameof(nodeId));
            }

            if (string.IsNullOrWhiteSpace(jsonRpcURL))
            {
                throw new ArgumentException("RPC URL is empty", nameof(jsonRpcURL));
            }

            if (string.IsNullOrWhiteSpace(webSocketURL))
            {
                throw new ArgumentException("Web Socket URL is empty", nameof(webSocketURL));
            }

            if (string.IsNullOrWhiteSpace(ingressEndPoint))
            {
                throw new ArgumentException("Ingress END Point is empty", nameof(ingressEndPoint));
            }

            if (string.IsNullOrWhiteSpace(ingressFingerPrint))
            {
                throw new ArgumentException("Ingress Finger Point is empty", nameof(ingressFingerPrint));
            }

            if (signer == null)
            {
                throw new ArgumentException("Signer is null", nameof(signer));
            }
            if (ftpMgr == null)
            {
                throw new ArgumentException("FTP Manager is null", nameof(signer));
            }

            _webSocketURI = webSocketURL;
            _jsonRpcURL = jsonRpcURL;
            _signer = signer;
            _nodeId = nodeId;
            _ftpMgr = ftpMgr;

            _webClient = new WebClient();

            _tti = new TalkToIngress(ingressEndPoint, ingressFingerPrint);
        }
        private const bool verbose = true;

        public void subscribeAndPost(bool reAttemptConnection)
        {
            ClientWebSocket webSocket = null;

            webSocket = new ClientWebSocket();
            //do
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
                    Console.WriteLine("Exception occurred in WebSocket Connection: {0}", ex);
                    //wait 20 second and re attempt operations based on flag(websocket connection to parity, data pushing to ingress)
                    if (reAttemptConnection) { Thread.Sleep(20000); }
                }
                //reconnect the websocket right away if connection drops
            } while (reAttemptConnection) ;
        }




        private async Task Connect(ClientWebSocket webSocket)
        {

            await webSocket.ConnectAsync(new Uri(_webSocketURI), CancellationToken.None);

            await Task.WhenAll(Receive(webSocket), Send(webSocket));
        }


        private async Task Send(ClientWebSocket webSocket)
        {
            byte[] buffer = encoder.GetBytes("{\"method\":\"parity_subscribe\",\"params\":[\"eth_getBlockByNumber\",[\"latest\",true]],\"id\":1,\"jsonrpc\":\"2.0\"}");
            await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);

            // re subscription so server knows that client is not timing out etc
            while (webSocket.State == WebSocketState.Open)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(30000));
            }
        }

        private async Task Receive(ClientWebSocket webSocket)
        {
            int BufferSize = 4096;
            var temporaryBuffer = new byte[BufferSize];
            var buffer = new byte[BufferSize * 20];
            int offset = 0;
            WebSocketReceiveResult response;

            while (true)
            {
                response = await webSocket.ReceiveAsync(
                                     new ArraySegment<byte>(temporaryBuffer),
                                     CancellationToken.None);
                temporaryBuffer.CopyTo(buffer, offset);
                offset += response.Count;
                temporaryBuffer = new byte[BufferSize];

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

        private RealTimeTelemetry ParseAndSignData(byte[] buffer)
        {
            try
            {
                Console.WriteLine(encoder.GetString(buffer));

                var json = "{\"method\":\"net_peerCount\",\"params\":[],\"id\":1,\"jsonrpc\":\"2.0\"}";
                _webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                var numOfPeersResponse = _webClient.UploadString(_jsonRpcURL, "POST", json);

                dynamic jsonObjPeers = JsonConvert.DeserializeObject(numOfPeersResponse.Trim());
                string numPeers = jsonObjPeers["result"];

                dynamic jsonObj = JsonConvert.DeserializeObject(encoder.GetString(buffer).Trim());
                dynamic paramsObj = jsonObj["params"];
                if (paramsObj != null && paramsObj["result"] != null)
                {
                    dynamic resultObj = paramsObj["result"];

                    string gasLimit = resultObj["gasLimit"];
                    string gasUsed = resultObj["gasUsed"];

                    long CurrentEPoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

                    RealTimeTelemetryPayload rttp = new RealTimeTelemetryPayload
                    {
                        Client = "Parity",
                        BlockNum = Convert.ToUInt64(resultObj["number"].ToString(), 16),
                        BlockHash = resultObj["hash"].ToString(),
                        BlockTS = Convert.ToInt64(resultObj["timestamp"].ToString(), 16),
                        BlockReceived = CurrentEPoch,
                        NumPeers = Convert.ToUInt16(numPeers, 16),
                        NumTxInBlock = (ushort)resultObj["transactions"].Count
                    };

                    RealTimeTelemetry rtt = new RealTimeTelemetry
                    {
                        NodeId = _nodeId,
                        Payload = rttp,
                        Signature = _signer.SignPayload(JsonConvert.SerializeObject(rttp))
                    };

                    if (verbose)
                    {
                        Console.WriteLine(encoder.GetString(buffer));
                        Console.WriteLine(numOfPeersResponse);

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
                else
                {
                    Console.WriteLine("Unable to Parse\\Sign real Time data. Invalid Data");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to Parse\\Sign real Time data. Error Occurred {0}", ex.ToString());
            }
            return null;

        }
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

                    string fileName = string.Format("{0}-{1}.json", _nodeId, DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss"));
                    try
                    {
                        if (!_ftpMgr.transferData(jsonPayload, fileName))
                        {
                            Console.WriteLine("ERROR: Unable to send real time telemetry on second channel. Data File {0}", fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: Unable to send real time telemetry on second channel. Error Details {0}", ex.ToString());
                    }

                }
                else
                {
                    Console.WriteLine("Real Time Telemetry Block data sent to Ingress Block # {0}", rtt.Payload.BlockNum);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR Occurred While sending data to Ingress.");
            }

        }

    }

}
