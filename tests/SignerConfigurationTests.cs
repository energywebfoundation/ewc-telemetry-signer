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
                ParityWebSocketAddress = "ws:\\127.0.0.1"
            };

            Assert.NotNull(configuration);
            Assert.Equal("4816d758dd37833a3a5551001dac8a5fa737a342",configuration.NodeId);
            Assert.Equal("https://localhost:5010",configuration.IngressHost);
            Assert.Equal("/var/run/influxdb.sock",configuration.TelegrafSocket);
            Assert.Equal("http://localhost:8545",configuration.ParityEndpoint);
            Assert.Equal("./",configuration.PersistanceDirectory);
            Assert.Equal("A:B:C:D:E:F:Z",configuration.IngressFingerprint);
            Assert.Equal("ws:\\127.0.0.1",configuration.ParityWebSocketAddress);
        }
    }
}