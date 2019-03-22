using System;
using System.IO;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using TelemetrySigner;
using Xunit;

namespace tests
{
    public class RealTimeTelemetryManagerTests
    {
        FTPManager ftpMgr;

        public RealTimeTelemetryManagerTests()
        {
            PayloadSigner sig = new PayloadSigner(
                "4816d758dd37833a3a5551001dac8a5fa737a342",
                new FileKeyStore("./"));
            string pubkey = sig.GenerateKeys();

            ftpMgr = new FTPManager("foo", "pass", "127.0.0.1", 2222, "78:72:96:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "/upload/dropzone/");
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
                ftpMgr,
                true);

            var currentConsoleOut = Console.Out;
            using (var cop = new ConsoleOutputCapturer())
            {
                mgr.subscribeAndPost(false);
                string ret = cop.GetOuput();
                Assert.True(ret.Contains("Connection refused"));
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
                ftpMgr,
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
                Assert.True(ret.Contains("Connection refused"));
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
                ftpMgr,
                true);

            MethodInfo methodInfo = typeof(RealTimeTelemetryManager).GetMethod("ParseAndSignData", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { Encoding.ASCII.GetBytes(data) };
            object ret = methodInfo.Invoke(mgr, parameters);
            Assert.Null(ret);
        }

        [Theory]
        [InlineData("",
                    "http://127.0.0.1",
                    "ws://127.0.0.1",
                    "https://localhost:5010/api/ingress/realtime",
                    "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E")]
        [InlineData("4816d758dd37833a3a5551001dac8a5fa737a342",
                    "",
                    "ws://127.0.0.1",
                    "https://localhost:5010/api/ingress/realtime",
                    "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E")]
        [InlineData("4816d758dd37833a3a5551001dac8a5fa737a342",
                    "http://127.0.0.1",
                    "",
                    "https://localhost:5010/api/ingress/realtime",
                    "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E")]
        [InlineData("4816d758dd37833a3a5551001dac8a5fa737a342",
                    "http://127.0.0.1",
                    "ws://127.0.0.1",
                    "",
                    "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E")]
        [InlineData("4816d758dd37833a3a5551001dac8a5fa737a342",
                    "http://127.0.0.1",
                    "ws://127.0.0.1",
                    "https://localhost:5010/api/ingress/realtime",
                    "")]
       
        void InvalidArgumentShouldFail(string nodeId, string jsonRpcURL, string webSocketURL, string ingressEndPoint,
            string ingressFingerPrint)
        {
            RealTimeTelemetryManager mgr = null;

            PayloadSigner signer = new PayloadSigner("4816d758dd37833a3a5551001dac8a5fa737a342", new FileKeyStore("./"));
            signer.Init();

            Assert.Throws<ArgumentException>(() =>
            {
                mgr = new RealTimeTelemetryManager(
                               nodeId, jsonRpcURL, webSocketURL, ingressEndPoint, ingressFingerPrint,
                               signer,
                               ftpMgr,
                               true);
            });
            Assert.Null(mgr);

        }

        [Fact]
        void InvalidSignerShouldFailInstanceCreation()
        {
            string nodeId = "4816d758dd37833a3a5551001dac8a5fa737a342";
            RealTimeTelemetryManager mgr = null;

            Assert.Throws<ArgumentException>(() =>
            {
                mgr = new RealTimeTelemetryManager(
                nodeId,
                "http://127.0.0.1",
                "ws://127.0.0.1",
                "https://localhost:5010/api/ingress/realtime",
                "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E",
                null,
                ftpMgr,
                true);
            });
            Assert.Null(mgr);

        }

        [Fact]
        void InvalidFTPManagerShouldFailInstanceCreation()
        {
            string nodeId = "4816d758dd37833a3a5551001dac8a5fa737a342";
            RealTimeTelemetryManager mgr = null;

            PayloadSigner signer = new PayloadSigner(nodeId, new FileKeyStore("./"));
            signer.Init();

            Assert.Throws<ArgumentException>(() =>
            {
                mgr = new RealTimeTelemetryManager(
                nodeId,
                "http://127.0.0.1",
                "ws://127.0.0.1",
                "https://localhost:5010/api/ingress/realtime",
                "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E",
                signer,
                null,
                true);
            });
            Assert.Null(mgr);

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