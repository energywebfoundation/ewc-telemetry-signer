using System;
using System.IO;

namespace TelemetrySigner
{
    /// <inheritdoc />
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
        
        /// <inheritdoc />
        /// <summary>
        /// Save the given bytes to the signing.salt file
        /// </summary>
        /// <param name="salt"></param>
        public void SaveSalt(byte[] salt)
        {
            File.WriteAllBytes(_saltFilePath,salt);
        }

        /// <inheritdoc />
        /// <summary>
        /// Load the salt as bytes from the signing.salt file
        /// </summary>
        /// <returns>The salt as bytes</returns>
        /// <exception cref="FileNotFoundException">Thrown when the salt file is not found</exception>
        public byte[] LoadSalt()
        {
            if (!File.Exists(_saltFilePath))
            {
                throw new FileNotFoundException($"Salt file not found at {_saltFilePath}");
            }
            return File.ReadAllBytes(_saltFilePath);
        }

        /// <inheritdoc />
        /// <summary>
        /// Load the encrypted key from the signing.key file
        /// </summary>
        /// <returns>The encrypted key as bytes</returns>
        /// <exception cref="FileNotFoundException">Thrown when the key file is not found</exception>
        public byte[] LoadEncryptedKey()
        {
            if (!File.Exists(_pkFile))
            {
                throw new FileNotFoundException($"Key file not found at {_saltFilePath}");
            }
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