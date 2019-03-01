using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TelemetrySigner
{
    public class TalkToIngress
    {
        private static string _fingerprint;
        private readonly string _url;
        private readonly HttpClient _client;

        public TalkToIngress(string ingressUrl, string ingressFingerPrint, HttpMessageHandler testHandler = null)
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
            
            // Set certificate verifier
            ServicePointManager.ServerCertificateValidationCallback = PinPublicKey;

            // Use the default handler when no specific handler is passed in.
            _client = new HttpClient(testHandler ?? new HttpClientHandler());
        }
        
        public async Task<bool> SendRequest(string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                throw new ArgumentException("Payload is empty",nameof(jsonPayload));
            }

            try
            {
                HttpResponseMessage response = await _client.PostAsync($"{_url}/api/ingress/influx", 
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
            
            
                return response.StatusCode == HttpStatusCode.Accepted;    
            }
            catch (Exception ex)
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