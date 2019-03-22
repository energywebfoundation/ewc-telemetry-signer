using System;
using System.IO;
using System.Reflection;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using TelemetrySigner;
using TelemetrySigner.Models;
using Xunit;

namespace tests
{
    public class RealTimeTelemetryManagerTests
    {

        private static IKeyStore GetMockKeyStore()
        {
            const string saltB64 = "NZ4KOCA1dtE=";
            const string keyB64 = "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";
            return new MockKeyStore
            {
                Salt = Convert.FromBase64String(saltB64), Key = Convert.FromBase64String(keyB64)
            };
        }

        [Fact]
        void InvalidParityConnectionShouldNotPass()
        {
            string nodeId = "node-1";

            PayloadSigner signer = new PayloadSigner(nodeId, GetMockKeyStore());
            signer.Init();

            var cfg = new SignerConfiguration
            {
                NodeId = nodeId,
                ParityEndpoint = "http://127.0.0.1",
                IngressHost = "https://localhost:5010/api/ingress/realtime",
                IngressFingerprint ="ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E",
                ParityWebSocketAddress = "ws://127.0.0.1",
                TelegrafSocket = "",
                PersistanceDirectory = ""
            };

            var logger = new MockLogger();
            RealTimeTelemetryManager mgr = new RealTimeTelemetryManager(cfg,signer,logger ,true);

            mgr.SubscribeAndPost(false);
            logger.LoggedMessages.Should().Contain("Exception occurred in WebSocket Connection: One or more errors occurred. (Unable to connect to the remote server) => Unable to connect to the remote server");
        }

        [Fact]
        void InvalidSendToInfluxShouldNotPass()
        {
            string nodeId = "node-1";

            PayloadSigner signer = new PayloadSigner(nodeId, GetMockKeyStore());
            signer.Init();

            var cfg = new SignerConfiguration
            {
                NodeId = nodeId,
                ParityEndpoint = "http://127.0.0.1",
                IngressHost = "https://localhost:5010/api/ingress/realtime",
                IngressFingerprint ="ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E",
                ParityWebSocketAddress = "ws://127.0.0.1",
                TelegrafSocket = "",
                PersistanceDirectory = ""
            };
            
            var logger = new MockLogger();
            RealTimeTelemetryManager mgr = new RealTimeTelemetryManager(cfg,signer,logger,true);

            RealTimeTelemetryPayload rttp = new RealTimeTelemetryPayload
            {
                Client = "Parity",
                BlockNum = 0,
                BlockHash = "",
                BlockTS = 0,
                BlockReceived = 0,
                NumPeers = 0,
                NumTxInBlock = 0,
                GasUsed = 0,
                GasLimit = 80000000
            };

            RealTimeTelemetry rtt = new RealTimeTelemetry
            {
                NodeId = "",
                Payload = rttp,
                Signature = signer.SignPayload(JsonConvert.SerializeObject(rttp))
            };
            
            // invoke private method
            MethodInfo methodInfo = typeof(RealTimeTelemetryManager).GetMethod("SendDataToIngress", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { rtt };
            methodInfo.Invoke(mgr, parameters);

            logger.LoggedMessages.Should().Contain("ERROR: Unable to send to ingress.");
        }

        [Theory]
        [InlineData("")]
        [InlineData("{result:null}")]
        void InvalidParseAndSignShouldNotPass(string data)
        {
            string nodeId = "node-1";

            PayloadSigner signer = new PayloadSigner(nodeId, GetMockKeyStore());
            signer.Init();

            var cfg = new SignerConfiguration
            {
                NodeId = nodeId,
                ParityEndpoint = "http://127.0.0.1",
                IngressHost = "https://localhost:5010/api/ingress/realtime",
                IngressFingerprint ="ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E",
                ParityWebSocketAddress = "ws://127.0.0.1",
                TelegrafSocket = "",
                PersistanceDirectory = ""
            };
            
            RealTimeTelemetryManager mgr = new RealTimeTelemetryManager(cfg,signer,new MockLogger(),true);

            MethodInfo methodInfo = typeof(RealTimeTelemetryManager).GetMethod("ParseAndSignData", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { Encoding.ASCII.GetBytes(data), "2","parity // Ethereum" };
            object ret = methodInfo.Invoke(mgr, parameters);
            Assert.Null(ret);
        }

    }
}