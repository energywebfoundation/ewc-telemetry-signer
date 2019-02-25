using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TelemetrySigner
{
    internal class TalkToIngress
    {
        private string _pk;
        private const string ExpectedIngressFingerprint = "BE:B6:42:61:6A:F4:FB:1A:60:59:12:83:2E:42:FB:2A:B7:BF:8C:27:44:7F:1F:EF:A5:B4:BE:F1:71:AC:3B:F0";


        public TalkToIngress(string base64PrivateKey)
        {
            // TODO: find way to store it securely in memory
            _pk = base64PrivateKey;
        }
        
        internal bool SendRequest(string url, string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                throw new ArgumentException("Payload is empty",nameof(jsonPayload));
            }
                
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL is empty",nameof(url));
            }
    
            if (!url.StartsWith("https://"))
            {
                throw new ArgumentException("URL is not https",nameof(url));
            }
                
            ServicePointManager.ServerCertificateValidationCallback = PinPublicKey;
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "application/json";
            wr.Method = "POST";
                
            using (StreamWriter streamWriter = new StreamWriter(wr.GetRequestStream()))
            {
                streamWriter.Write(jsonPayload);
                streamWriter.Flush();
                streamWriter.Close();
            }
                
            HttpWebResponse httpResponse = (HttpWebResponse)wr.GetResponse();
            return httpResponse.StatusCode == HttpStatusCode.Created;
                
        }
        private static bool PinPublicKey(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (certificate == null)
            {
                return false;
            }

            string certFingerprintFromIngress = certificate.GetCertHashString(HashAlgorithmName.SHA256);
            // Check that fingerprint matches expected

            return certFingerprintFromIngress == ExpectedIngressFingerprint;
        }
    }
}