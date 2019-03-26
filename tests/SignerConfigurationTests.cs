using TelemetrySigner;
using Xunit;

namespace tests
{
    public class SignerConfigurationTests
    {
        [Fact]
        void SignerConfigurationObjectCreationShouldPass()
        {

            SignerConfiguration configuration = new SignerConfiguration
            {
                NodeId = "4816d758dd37833a3a5551001dac8a5fa737a342",
                IngressHost = "https://localhost:5010",
                TelegrafSocket = "/var/run/influxdb.sock",
                ParityEndpoint = "http://localhost:8545",
                PersistanceDirectory = "./",
                IngressFingerprint = "A:B:C:D:E:F:Z",
                ParityWebSocketAddress = "ws:\\127.0.0.1",
                FTPHost = "127.0.0.1",
                FTPPort = 22,
                FTPUser = "uname",
                FTPPass = "pass",
                FTPFingerPrint = "A:B:C:D:E:Z",
                FTPDir = "/",
            };

            Assert.NotNull(configuration);
        }
    }
}