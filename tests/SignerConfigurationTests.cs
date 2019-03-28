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
                FtpHost = "127.0.0.1",
                FtpPort = 22,
                FtpUser = "uname",
                FtpPass = "pass",
                FtpFingerPrint = "A:B:C:D:E:Z",
                FtpDir = "/",
            };

            Assert.NotNull(configuration);
        }
    }
}