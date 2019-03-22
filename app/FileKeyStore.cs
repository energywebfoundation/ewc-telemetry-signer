using System;
using System.IO;

namespace TelemetrySigner
{
    /// <summary>
    /// Implements a file based keystore to store the encrypted signing key and salt
    /// </summary>
    public class FileKeyStore : IKeyStore
    {
        private readonly string _pkFile;
        private readonly string _saltFilePath;

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="basePath">Path to store/read the files in/from</param>
        /// <exception cref="ArgumentException">Path is empty or null</exception>
        /// <exception cref="DirectoryNotFoundException">The given path is not an existing directory</exception>
        public FileKeyStore(string basePath)
        {

            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException("Base path given can't be empty or null",nameof(basePath));
            }
            
            if (!Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException("Base path does not exist or isn't a directory");
            }
            
            _pkFile = Path.Combine(basePath, "signing.key");
            _saltFilePath = Path.Combine(basePath, "signing.salt");
        }
        
        /// <summary>
        /// Save the given bytes to the signing.salt file
        /// </summary>
        /// <param name="salt"></param>
        public void SaveSalt(byte[] salt)
        {
            File.WriteAllBytes(_saltFilePath,salt);
        }

        /// <summary>
        /// Load the salt as bytes from the signing.salt file
        /// </summary>
        /// <returns></returns>
        public byte[] LoadSalt()
        {
            return File.ReadAllBytes(_saltFilePath);
        }

        /// <summary>
        /// Load the encrypted key from the signing.key file
        /// </summary>
        /// <returns></returns>
        public byte[] LoadEncryptedKey()
        {
            return File.ReadAllBytes(_pkFile);
        }

        /// <summary>
        /// Save the given bytes to the signing.key file
        /// </summary>
        /// <param name="key"></param>
        public void SaveEncryptedKey(byte[] key)
        {
            File.WriteAllBytes(_pkFile,key);
        }
    }
}