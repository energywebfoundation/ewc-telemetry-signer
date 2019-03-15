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
    class ParitySubscribe
    {
        private WebClient _webClient;
        private string _jsonRpcURL;
        private string _webSocketURI;
        private TalkToIngress _tti;
        private PayloadSigner _signer;
        private string _nodeId;

        UTF8Encoding encoder = new UTF8Encoding();

        public ParitySubscribe(string nodeId, string jsonRpcURL, string webSocketURL, string ingressEndPoint, string ingressFingerPrint, PayloadSigner signer, bool verbose)
        {
            _webSocketURI = webSocketURL;
            _jsonRpcURL = jsonRpcURL;
            _signer = signer;
            _nodeId = nodeId;

            _webClient = new WebClient();

            _tti = new TalkToIngress(ingressEndPoint, ingressFingerPrint);
        }
        private const bool verbose = true;

        public async Task subscribe()
        {
            ClientWebSocket webSocket = null;
            try
            {
                webSocket = new ClientWebSocket();
                do
                {
                    if (webSocket.State != WebSocketState.Connecting)
                    {
                        await Connect(webSocket);
                    }
                    //reconnect the websocket right away if connection drops
                } while (webSocket.State != WebSocketState.Open);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred in WebSocket Connection: {0}", ex);
            }
            finally
            {
                if (webSocket != null)
                    webSocket.Dispose();
            }
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
                        Console.WriteLine("block time stamp: {0}",rttp.BlockTS);
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
                Thread.Sleep(new Random().Next(1,500));

                //push data to ingress real time telemetry endpoint
                bool sendSuccess = _tti.SendRequest(JsonConvert.SerializeObject(rtt)).Result;
                if (!sendSuccess)
                {
                    // TODO: unable to send real time telemetry to ingress - send by second channel
                    Console.WriteLine("ERROR: Unable to send to ingress for more then. Use Sending queue on second channel.");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR Occurred While sending data to Ingress.");
            }

            Console.WriteLine("Real Time Telemetry Block data sent to Ingress Block # {0}", rtt.Payload.BlockNum);


        }




    }

}
