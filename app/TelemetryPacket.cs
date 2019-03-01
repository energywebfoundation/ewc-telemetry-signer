using System.Collections.Generic;
using Newtonsoft.Json;

namespace TelemetrySigner
{
    public class TelemetryPacket
    {
        [JsonProperty("nodeid")]
        public string NodeId { get; set; } 
        [JsonProperty("payload")]
        public IList<string> Payload { get; set; }
        [JsonProperty("signature")]
        public string Signature { get; set; }    
    }
}