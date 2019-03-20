using System;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using TelemetrySigner;
using TelemetrySigner.Models;
using Xunit;

namespace tests
{
    public class RealTimeTelemetryManagerTests
    {

        public RealTimeTelemetryManagerTests()
        {
            PayloadSigner sig = new PayloadSigner(
                "4816d758dd37833a3a5551001dac8a5fa737a342", 
                new FileKeyStore("./"));
            string pubkey = sig.GenerateKeys();
        }

        [Fact]
        void InvalidParityConnectionShouldNotPass()
        {
            string nodeId = "4816d758dd37833a3a5551001dac8a5fa737a342";

            PayloadSigner signer = new PayloadSigner(nodeId, new FileKeyStore("./"));
            signer.Init();

            RealTimeTelemetryManager mgr = new RealTimeTelemetryManager(
                nodeId,
                "http://127.0.0.1",
                "ws://127.0.0.1",
                "https://localhost:5010/api/ingress/realtime",
                "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E",
                signer,
                true);

            var currentConsoleOut = Console.Out;
            using (var cop = new ConsoleOutputCapturer())
            {
                mgr.SubscribeAndPost(false);
                string ret = cop.GetOuput();
                Assert.Contains("Unable to connect to the remote server", ret);
            }
        }

        [Fact]
        void InvalidSendToInfluxShouldNotPass()
        {
            string nodeId = "4816d758dd37833a3a5551001dac8a5fa737a342";

            PayloadSigner signer = new PayloadSigner(nodeId, new FileKeyStore("./"));
            signer.Init();

            RealTimeTelemetryManager mgr = new RealTimeTelemetryManager(
                nodeId,
                "http://127.0.0.1",
                "ws://127.0.0.1",
                "https://localhost:5010/api/ingress/realtime",
                "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E",
                signer,
                true);

            RealTimeTelemetryPayload rttp = new RealTimeTelemetryPayload
            {
                Client = "Parity",
                BlockNum = 0,
                BlockHash = "",
                BlockTS = 0,
                BlockReceived = 0,
                NumPeers = 0,
                NumTxInBlock = 0
            };

            RealTimeTelemetry rtt = new RealTimeTelemetry
            {
                NodeId = "",
                Payload = rttp,
                Signature = signer.SignPayload(JsonConvert.SerializeObject(rttp))
            };

            var currentConsoleOut = Console.Out;
            using (var cop = new ConsoleOutputCapturer())
            {
                MethodInfo methodInfo = typeof(RealTimeTelemetryManager).GetMethod("SendDataToIngress", BindingFlags.NonPublic | BindingFlags.Instance);
                object[] parameters = { rtt };
                methodInfo.Invoke(mgr, parameters);

                string ret = cop.GetOuput();
                Assert.Contains("Connection refused", ret);
                //Assert.True(ret.Contains("ERROR Occurred While sending data to Ingress"));
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("{result:null}")]
        void InvalidParseAndSignShouldNotPass(string data)
        {
            string nodeId = "4816d758dd37833a3a5551001dac8a5fa737a342";

            PayloadSigner signer = new PayloadSigner(nodeId, new FileKeyStore("./"));
            signer.Init();

            RealTimeTelemetryManager mgr = new RealTimeTelemetryManager(
                nodeId,
                "http://127.0.0.1",
                "ws://127.0.0.1",
                "https://localhost:5010/api/ingress/realtime",
                "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E",
                signer,
                true);

            MethodInfo methodInfo = typeof(RealTimeTelemetryManager).GetMethod("ParseAndSignData", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { Encoding.ASCII.GetBytes(data) };
            object ret = methodInfo.Invoke(mgr, parameters);
            Assert.Null(ret);
        }

    }

    public class ConsoleOutputCapturer : IDisposable
    {
        private StringWriter stringWriter;
        private TextWriter originalOutput;

        public ConsoleOutputCapturer()
        {
            stringWriter = new StringWriter();
            originalOutput = Console.Out;
            Console.SetOut(stringWriter);
        }

        public string GetOuput()
        {
            return stringWriter.ToString();
        }

        public void Dispose()
        {
            Console.SetOut(originalOutput);
            stringWriter.Dispose();
        }
    }
}