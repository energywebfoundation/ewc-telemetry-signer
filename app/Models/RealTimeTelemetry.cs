using Newtonsoft.Json;

namespace TelemetrySigner.Models
{
    /// <summary>
    /// The model class for Real Time Telemetry
    /// </summary>
    public class RealTimeTelemetry
    {
        /// <summary>
        /// The property for nodeid
        /// </summary>
        [JsonProperty("nodeid")]
        public string NodeId { get; set; }

        /// <summary>
        /// The property for payload
        /// </summary>
        [JsonProperty("payload")]
        public RealTimeTelemetryPayload Payload { get; set; }

        /// <summary>
        /// The property for signature
        /// </summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
}