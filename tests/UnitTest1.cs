using System;
using TelemetrySigner;
using Xunit;

namespace tests
{
    public class PayloadSignerTests
    {
        [Fact]
        public void ShouldGenerateKeys()
        {

            SignerConfiguration sc = new SignerConfiguration
            {
                NodeId = "node1"
            };
            PayloadSigner ps = new PayloadSigner();
        }
    }
}
