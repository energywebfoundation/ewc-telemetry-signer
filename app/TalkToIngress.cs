using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TelemetrySigner
{
    public class TalkToIngress
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

            _fingerprint = ingressFingerPrint.Replace(":",string.Empty).ToUpperInvariant();
            _url = ingressUrl;
        }
        
        internal bool SendRequest(string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                throw new ArgumentException("Payload is empty",nameof(jsonPayload));
            }
                
            ServicePointManager.ServerCertificateValidationCallback = PinPublicKey;
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create($"{_url}/api/ingress/influx");
            wr.ContentType = "application/json";
            wr.Method = "POST";
                
            using (StreamWriter streamWriter = new StreamWriter(wr.GetRequestStream()))
            {
                streamWriter.Write(jsonPayload);
                streamWriter.Flush();
                streamWriter.Close();
            }

            try
            {
                HttpWebResponse httpResponse = (HttpWebResponse) wr.GetResponse();
                return httpResponse.StatusCode == HttpStatusCode.Accepted;
            }
            catch (WebException ex)
            {
                Console.WriteLine("ERROR: unable to send: " + ex.Message);
                return false;
            }

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