using Newtonsoft.Json;

namespace TelemetrySigner.Models
{
    /// <summary>
    /// The model class for Real Time Telemetry Payload
    /// </summary>
    public class RealTimeTelemetryPayload
    {
        /// <summary>
        /// The property for client
        /// </summary>
        [JsonProperty("client")]
        public string Client { get; set; }

        /// <summary>
        /// The property for block num
        /// </summary>
        [JsonProperty("blocknum")]
        public ulong BlockNum { get; set; }

        /// <summary>
        /// The property for block hash
        /// </summary>
        [JsonProperty("blockhash")]
        public string BlockHash { get; set; }

        /// <summary>
        /// The property for block Time Stamp
        /// </summary>
        [JsonProperty("blockts")]
        public long BlockTS { get; set; }

        /// <summary>
        /// The property for block received Time Stamp
        /// </summary>
        [JsonProperty("blockreceived")]
        public long BlockReceived { get; set; }

        /// <summary>
        /// The property for Num of Peers
        /// </summary>
        [JsonProperty("numpeers")]
        public ushort NumPeers { get; set; }

        /// <summary>
        /// The property for Num of Transactions in block
        /// </summary>
        [JsonProperty("numtxinblock")]
        public ushort NumTxInBlock { get; set; }

        [JsonProperty("gaslimit")]
        public long GasLimit { get; set; }

        [JsonProperty("gasused")]
        public long GasUsed { get; set; }
    }
}