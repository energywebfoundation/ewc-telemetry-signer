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
    /// <summary>
    /// Sends telemetry packages to the ingress host
    /// </summary>
    public class TalkToIngress
    {
        private readonly string _fingerprint;
        private readonly HttpClient _client;
        private readonly string _endPoint;

        /// <summary>
        /// Instantiate a new talker
        /// </summary>
        /// <param name="endPoint">https base url to the ingress host</param>
        /// <param name="ingressFingerPrint">SHA256 fingerprint of the ingress TLS certificate</param>
        /// <param name="testHandler">message handler for the HttpClient. When null the default is used. [used for testing]</param>
        /// <exception cref="ArgumentException">Any of the arguments given is not correct. see exception message.</exception>
        public TalkToIngress(string endPoint, string ingressFingerPrint, HttpMessageHandler testHandler = null)
        {
            // Verify endpoint
            if (string.IsNullOrWhiteSpace(endPoint))
            {
                throw new ArgumentException("URL is empty",nameof(endPoint));
            }
            
            if (!endPoint.StartsWith("https://"))
            {
                throw new ArgumentException("URL is not https",nameof(endPoint));
            }
            
            // Verify fingerprint
            if (string.IsNullOrWhiteSpace(ingressFingerPrint))
            {
                throw new ArgumentException("Fingerprint is empty",nameof(ingressFingerPrint));
            }

            _fingerprint = ingressFingerPrint.Replace(":",string.Empty).ToUpperInvariant();
            _endPoint = endPoint;

            // Use the default handler when no specific handler is passed in.
            var handler = new HttpClientHandler {ServerCertificateCustomValidationCallback = PinPublicKey};
            _client = new HttpClient(testHandler ?? handler);
            
        }
        
        /// <summary>
        /// Send the payload to the ingress
        /// </summary>
        /// <param name="jsonPayload">JSON string containing the payload</param>
        /// <returns>true if send was successful</returns>
        /// <exception cref="ArgumentException">Payload is empty</exception>
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

        /// <summary>
        /// Verify the certificate fingerprint during TLS handshake with the Ingress. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate">The certificate send by ingress</param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns>true if the fingerprint matches</returns>
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