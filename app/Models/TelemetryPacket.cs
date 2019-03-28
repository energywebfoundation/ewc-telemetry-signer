using System.Collections.Generic;
using Newtonsoft.Json;

namespace TelemetrySigner.Models
{
    /// <summary>
    /// The model class for Telemetry Packet
    /// </summary>
    public class TelemetryPacket
    {
        /// <summary>
        /// Node Id
        /// </summary>
        [JsonProperty("nodeid")]
        public string NodeId { get; set; }

        /// <summary>
        /// Payload
        /// </summary>
        [JsonProperty("payload")]
        public IList<string> Payload { get; set; }

        /// <summary>
        /// Signature
        /// </summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
}