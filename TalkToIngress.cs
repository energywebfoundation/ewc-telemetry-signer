using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace TelemetrySigner
{
    internal class TalkToIngress
    {
        // Encoded RSAPublicKey
        private const string PUB_KEY = "30818902818100C4A06B7B52F8D17DC1CCB47362" + 
                                       "C64AB799AAE19E245A7559E9CEEC7D8AA4DF07CB0B21FDFD763C63A313A668FE9D764E" + 
                                       "D913C51A676788DB62AF624F422C2F112C1316922AA5D37823CD9F43D1FC54513D14B2" + 
                                       "9E36991F08A042C42EAAEEE5FE8E2CB10167174A359CEBF6FACC2C9CA933AD403137EE" + 
                                       "2C3F4CBED9460129C72B0203010001";
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
    
            string pk = certificate.GetPublicKeyString();
            return pk.Equals(PUB_KEY);
        }
    }
}