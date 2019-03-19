namespace TelemetrySigner
{
    public class SignerConfiguration
    {
        public string TelegrafSocket { get; set; }
        public string IngressHost { get; set; }
        public string NodeId { get; set; }
        public string PersistanceDirectory { get; set; }
        public string ParityEndpoint { get; set; }
        public string IngressFingerprint { get; set; }
        public string ParityWebSocketAddress { get; set; }
    }
}