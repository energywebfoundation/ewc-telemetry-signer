namespace TelemetrySigner
{
    public class SignerConfiguration
    {
        public string TelegrafSocket { get; set; }
        public string IngressHost { get; set; }
        public string NodeId { get; set; }
        public string PersistanceDirectory { get; set; }
        public string ParityEndpoiunt { get; set; }
        public string IngressFingerprint { get; set; }
        public string ParityWebSocketAddress { get; set; }
        public string FTPHost {get; set;}
        public int FTPPort {get; set;}
        public string FTPUser { get; set;}
        public string FTPPass {get; set;}
        public string FTPFingerPrint {get; set;}
        public string FTPDir {get; set;}




    }
}