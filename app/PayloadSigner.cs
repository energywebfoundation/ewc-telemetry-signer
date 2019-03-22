using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TelemetrySigner
{
    /// <summary>
    /// PayloadSigner class for signing payload and keys creation
    /// </summary>
    public class PayloadSigner
    {
        private RSACryptoServiceProvider _rsa;
        private readonly IKeyStore _keystore;
        private readonly string _nodeId;
        private const int KeySize = 4096;

        /// <summary>
        /// PayloadSigner constructor for PayloadSigner instance creation
        /// </summary>
        /// <param name="nodeId">Id of Node</param>
        /// <param name="keyStore">keyStore reference</param>
        /// <returns>returns instance of PayloadSigner</returns>
        /// <exception cref="System.ArgumentException">Thrown when any of provided argument is null or empty.</exception>
        public PayloadSigner(string nodeId, IKeyStore keyStore)
        {

            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new ArgumentException("Provide nodeId", nameof(nodeId));
            }

            _nodeId = nodeId;
            _keystore = keyStore ?? throw new ArgumentException("Keystore not allowed to be null", nameof(keyStore));
        }

        /// <summary>
        /// Signs provided string payload and returns signature
        /// </summary>
        /// <param name="payload">Payload string to be signed</param>
        /// <returns>returns the signature</returns>
        public string SignPayload(string payload)
        {
            // Convert payload to bytes
            ASCIIEncoding byteConverter = new ASCIIEncoding();
            byte[] payloadBytes = byteConverter.GetBytes(payload);

            // Sign payload bytes
            byte[] signatureBytes = _rsa.SignData(payloadBytes, new SHA256CryptoServiceProvider());

            // Convert to bas64 and return
            string base64Signature = Convert.ToBase64String(signatureBytes);
            return base64Signature;
        }

        /// <summary>
        /// Init RSA Crypto Services Provider
        /// </summary>
        /// <exception cref="TelemetrySigner.KeypairNotFoundException">Thrown when key pair is missing.</exception>
        public void Init()
        {

            // Load private key
            _rsa = new RSACryptoServiceProvider(KeySize);

            try
            {
                // Load and decrypt the key from store
                byte[] decryptedPrivateKey = LoadKeyFromStore();

                // Load decrypted CSP blob into RSA
                _rsa.ImportCspBlob(decryptedPrivateKey);
            }
            catch (Exception e)
            {
                throw new KeypairNotFoundException("Key files not present or invalid. Generate first using --genkey", e);
            }
        }

        /// <summary>
        ///  Generate Keys for first time use
        /// </summary>
        /// <returns>returns base64 encoded public key string</returns>
        public string GenerateKeys()
        {

            using (var rsa = new RSACryptoServiceProvider(KeySize))
            {
                // Generate new keypair when not all key files found on disk
                byte[] publicKey = rsa.ExportCspBlob(false);
                string publicKeyBase64 = Convert.ToBase64String(publicKey);

                // Save new private key to disk so it can be reloaded after service restart
                byte[] privateKey = rsa.ExportCspBlob(true);
                StoreKey(privateKey);
                return publicKeyBase64;
            }


        }

        /// <summary>
        /// Encrypts key using and then stores key into file
        /// </summary>
        /// <param name="privateKey">Private Key byte array to be saved</param>
        private void StoreKey(byte[] privateKey)
        {
            // encrypt private key to be stored on disk using RFC2898 derived keys and AES256 encryption
            byte[] salt = new byte[8];

            // generate new salt
            var rnd = new RNGCryptoServiceProvider();
            rnd.GetBytes(salt);

            // store salt
            _keystore.SaveSalt(salt);

            // derive key and IV from nodeid and salt
            Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(_nodeId, salt, 1024);
            byte[] aesKey = rfc2898.GetBytes(32);
            byte[] aesIv = rfc2898.GetBytes(16);

            byte[] encryptedPrivateKey;
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = aesKey;
                aesAlg.IV = aesIv;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt =
                        new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(Convert.ToBase64String(privateKey));
                        }

                        encryptedPrivateKey = msEncrypt.ToArray();
                    }
                }
            }

            // Write private key to disk
            _keystore.SaveEncryptedKey(encryptedPrivateKey);
        }


        /// <summary>
        /// Loads Key from store and returns its byte array
        /// </summary>
        /// <returns>returns decrypted byte array of private key</returns>
        /// <exception cref="TelemetrySigner.SaltSizeException">Thrown when salt size is invalid.</exception>
        private byte[] LoadKeyFromStore()
        {
            // Read encrypted private key from disk
            byte[] encryptedPk = _keystore.LoadEncryptedKey();

            // Read Salt from disk
            byte[] salt = _keystore.LoadSalt();

            // verify salt length
            if (salt.Length != 8)
            {
                throw new SaltSizeException("Salt from store is not 64bits");
            }

            // derive keys from nodeid and salt
            Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(_nodeId, salt, 1024);
            byte[] aesKey = rfc2898.GetBytes(32);
            byte[] aesIv = rfc2898.GetBytes(16);


            byte[] decryptedPrivateKey;

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = aesKey;
                aesAlg.IV = aesIv;

                // Create an decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(encryptedPk))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            decryptedPrivateKey = Convert.FromBase64String(srDecrypt.ReadToEnd());
                        }
                    }
                }
            }

            return decryptedPrivateKey;
        }
    }
}