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
        private readonly string _fingerprint;
        //private readonly string _url;
        private readonly HttpClient _client;

        private string _endPoint;

        public TalkToIngress(string endPoint, string ingressFingerPrint, HttpMessageHandler testHandler = null)
        {
            if (string.IsNullOrWhiteSpace(endPoint))
            {
                throw new ArgumentException("URL is empty",nameof(endPoint));
            }
            
            if (string.IsNullOrWhiteSpace(ingressFingerPrint))
            {
                throw new ArgumentException("Fingerprint is empty",nameof(ingressFingerPrint));
            }
    
            if (!endPoint.StartsWith("https://"))
            {
                throw new ArgumentException("URL is not https",nameof(endPoint));
            }

            _fingerprint = ingressFingerPrint.Replace(":",string.Empty).ToUpperInvariant();
            //_url = ingressUrl;
            _endPoint = endPoint;
            

            // Use the default handler when no specific handler is passed in.
            var handler = new HttpClientHandler {ServerCertificateCustomValidationCallback = PinPublicKey};
            _client = new HttpClient(testHandler ?? handler);
            
        }
        
        public async Task<bool> SendRequest(string jsonPayload)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                throw new ArgumentException("Payload is empty",nameof(jsonPayload));
            }

            try
            {
                HttpResponseMessage response = await _client.PostAsync(_endPoint , //$"{_url}/api/ingress/influx", 
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
            
            
                return response.StatusCode == HttpStatusCode.Accepted;    
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: unable to send: " + ex.Message + " ==> " + ex.InnerException?.Message);
                return false;
            }
        }

        public bool PinPublicKey(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (certificate == null)
            {
                return false;
            }

            string certFingerprintFromIngress = certificate.GetCertHashString(HashAlgorithmName.SHA256);
            // Check that fingerprint matches expected

            bool fingerPrintMatch = certFingerprintFromIngress == _fingerprint;
            if (!fingerPrintMatch)
            {
                Console.WriteLine($"WARN: Fingerprints don't match: {certFingerprintFromIngress} - expected: {_fingerprint}");
            }
            return fingerPrintMatch;
        }
    }
}