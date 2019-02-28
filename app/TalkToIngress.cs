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
        private static string _fingerprint;
        private readonly string _url;

        public TalkToIngress(string ingressUrl, string ingressFingerPrint)
        {
            if (string.IsNullOrWhiteSpace(ingressUrl))
            {
                throw new ArgumentException("URL is empty",nameof(ingressUrl));
            }
            
            if (string.IsNullOrWhiteSpace(ingressFingerPrint))
            {
                throw new ArgumentException("Fingerprint is empty",nameof(ingressFingerPrint));
            }
    
            if (!ingressUrl.StartsWith("https://"))
            {
                throw new ArgumentException("URL is not https",nameof(ingressUrl));
            }

            _fingerprint = ingressFingerPrint;
            _url = ingressUrl;
        }
        
        internal bool SendRequest(string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                throw new ArgumentException("Payload is empty",nameof(jsonPayload));
            }
                
            ServicePointManager.ServerCertificateValidationCallback = PinPublicKey;
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(_url);
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

            return certFingerprintFromIngress == _fingerprint;
        }
    }
}