namespace TelemetrySigner
{
    public class SignerConfiguration
    {
        public string TelegrafSocket { get; set; }
        public string IngressHost { get; set; }
        public string NodeId { get; set; }
        public string PersistanceDirectory { get; set; }
        public string ParityEndpoiunt { get; set; }
    }
}