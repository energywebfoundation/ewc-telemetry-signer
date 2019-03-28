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
        public string ParityEndpoint { get; set; }

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
        public string FtpHost { get; set; }

        /// <summary>
        /// The property for FTP Port
        /// </summary>
        public int FtpPort { get; set; }

        /// <summary>
        /// The property for  FTP User
        /// </summary>
        public string FtpUser { get; set; }

        /// <summary>
        /// The property for FTP Pass
        /// </summary>
        public string FtpPass { get; set; }

        /// <summary>
        /// The property for FTP Finger Print
        /// </summary>
        public string FtpFingerPrint { get; set; }

        /// <summary>
        /// The property for FTP Dir
        /// </summary>
        public string FtpDir { get; set; }




    }
}