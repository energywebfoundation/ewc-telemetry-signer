using FluentAssertions;
using Newtonsoft.Json;
using TelemetrySigner;
using TelemetrySigner.Models;
using Xunit;

namespace tests
{
    public class TelemetryPacketTests
    {
        [Fact]
        public void ShouldDeseializeFromJson()
        {
            string json = "{ \"nodeid\":\"validator-1\",\"payload\": [ \"payload-line-1\",\"payload-line-2\" ],\"signature\":\"a-signature\"}";

            TelemetryPacket tp = JsonConvert.DeserializeObject<TelemetryPacket>(json);
            tp.NodeId.Should().Be("validator-1");
            tp.Signature.Should().Be("a-signature");
            tp.Payload.Should()
                .HaveCount(2)
                .And
                .BeEquivalentTo(new[]
                {
                    "payload-line-1",
                    "payload-line-2"
                });
            
        } 
    }
}