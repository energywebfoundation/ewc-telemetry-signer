namespace TelemetrySigner
{
    /// <summary>
    /// The model class for Signer Configuration
    /// </summary>
    public class SignerConfiguration
    {
        /// <summary>
        /// The property for Telegraf Socket
        /// </summary>
        public string TelegrafSocket { get; set; }

        /// <summary>
        /// The property for Ingress Host
        /// </summary>
        public string IngressHost { get; set; }

        /// <summary>
        /// The property for Node Id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// The property for Persistance Directory
        /// </summary>
        public string PersistanceDirectory { get; set; }

        /// <summary>
        /// The property for Parity End poiunt
        /// </summary>
        public string ParityEndpoiunt { get; set; }

        /// <summary>
        /// The property for Ingress Finger print
        /// </summary>
        public string IngressFingerprint { get; set; }

        /// <summary>
        /// The property for Parity Web Socket Address
        /// </summary>
        public string ParityWebSocketAddress { get; set; }

        /// <summary>
        /// The property for FTP Host
        /// </summary>
        public string FTPHost { get; set; }

        /// <summary>
        /// The property for FTP Port
        /// </summary>
        public int FTPPort { get; set; }

        /// <summary>
        /// The property for  FTP User
        /// </summary>
        public string FTPUser { get; set; }

        /// <summary>
        /// The property for FTP Pass
        /// </summary>
        public string FTPPass { get; set; }

        /// <summary>
        /// The property for FTP Finger Print
        /// </summary>
        public string FTPFingerPrint { get; set; }

        /// <summary>
        /// The property for FTP Dir
        /// </summary>
        public string FTPDir { get; set; }




    }
}