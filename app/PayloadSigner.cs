using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TelemetrySigner
{
    public class PayloadSigner
    {
        private readonly SignerConfiguration _configuration;
        private RSACryptoServiceProvider _rsa;
        const int keySize = 4096;

        public PayloadSigner(SignerConfiguration config)
        {
            _configuration = config;
        }

        public void Init()
        {
            LoadKey();
        }

        public string SignPayload(string payload)
        {
            // Convert payload to bytes
            ASCIIEncoding byteConverter = new ASCIIEncoding();
            byte[] payloadBytes = byteConverter.GetBytes(payload);
            
            // Sign payload bytes
            byte[] signatureBytes = _rsa.SignData(payloadBytes, new SHA256CryptoServiceProvider());
            
            // Convert to bas64 and return
            string base64Signatrue = Convert.ToBase64String(signatureBytes);
            return base64Signatrue;
        }
        
        private void LoadKey()
        {
            
            string pkFile = Path.Combine(_configuration.PersistanceDirectory, "signing.key");
            string saltFilePath = Path.Combine(_configuration.PersistanceDirectory, "signing.salt");
            
            // Load private key
            _rsa = new RSACryptoServiceProvider(keySize);
                
            // Only if both files are there wqe can load the key
            if (File.Exists(pkFile) && File.Exists(saltFilePath))
            {
                // Load and decrypt the key from disk
                byte[] decryptedPrivateKey = LoadKeyFromDisk(pkFile, saltFilePath);

                // Load decrypted CSP blob into RSA
                _rsa.ImportCspBlob(decryptedPrivateKey);
            }
            else
            {
                throw new Exception("Key files not present. Generate first using --genkey");
            }
        }

        public string GenerateKeys()
        {
            string pkFile = Path.Combine(_configuration.PersistanceDirectory, "signing.key");
            string saltFilePath = Path.Combine(_configuration.PersistanceDirectory, "signing.salt");

            
            using (var rsa = new RSACryptoServiceProvider(keySize))
            {
                // Generate new keypair when not all key files found on disk
                byte[] publicKey = rsa.ExportCspBlob(false);
                string publicKeyBase64 = Convert.ToBase64String(publicKey);
        
                // Save new private key to disk so it can be reloaded after service restart
                byte[] privateKey = rsa.ExportCspBlob(true);
                StoreKeyOnDisk(privateKey, saltFilePath, pkFile);
                return publicKeyBase64;    
            }
            
            
        }

        private void StoreKeyOnDisk(byte[] privateKey, string saltFilePath, string pkFile)
        {
            // encrypt private key to be stored on disk using RFC2898 derived keys and AES256 encryption
            byte[] salt = new byte[8];

            // generate new salt
            var rnd = new RNGCryptoServiceProvider();
            rnd.GetBytes(salt);

            // store salt
            File.WriteAllBytes(saltFilePath, salt);

            // derive key and IV from nodeid and salt
            Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(_configuration.NodeId, salt, 1024);
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
            File.WriteAllBytes(pkFile, encryptedPrivateKey);
        }

        private byte[] LoadKeyFromDisk(string pkFile, string saltFilePath)
        {
            // Read encrypted private key from disk
            byte[] encryptedPk = File.ReadAllBytes(pkFile);

            // Read Salt from disk
            byte[] salt = File.ReadAllBytes(saltFilePath);

            // verify salt length
            if (salt.Length != 8)
            {
                throw new Exception("Salt from disk is not 64bits");
            }

            // derive keys from nodeid and salt
            Rfc2898DeriveBytes rfc2898 = new Rfc2898DeriveBytes(_configuration.NodeId, salt, 1024);
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