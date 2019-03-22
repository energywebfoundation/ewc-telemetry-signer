namespace TelemetrySigner
{
    /// <summary>
    /// Configuration for the signer 
    /// </summary>
    public class SignerConfiguration
    {
        /// <summary>
        /// Named pipe we read the telegraf data from
        /// </summary>
        public string TelegrafSocket { get; set; }
        
        /// <summary>
        /// Base HTTPS URL of the ingress host 
        /// </summary>
        public string IngressHost { get; set; }
        
        /// <summary>
        /// Validator address or other unique node id that identifies us against the ingress 
        /// </summary>
        public string NodeId { get; set; }
        
        /// <summary>
        /// Directory used to persist necessary information 
        /// </summary>
        public string PersistanceDirectory { get; set; }
        
        /// <summary>
        /// HTTP RPC URL to the local parity client 
        /// </summary>
        public string ParityEndpoint { get; set; }
        
        /// <summary>
        /// X.509 certificate SHA256 fingerprint of the expected certificate from ingress 
        /// </summary>
        public string IngressFingerprint { get; set; }

        /// <summary>
        /// WebSocket RPC URL to the local parity client
        /// </summary>
        public string ParityWebSocketAddress { get; set; }
    }
}