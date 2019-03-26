using System;
using System.IO;

namespace TelemetrySigner
{
    /// <summary>
    /// FileKeyStore class contains functionality management of salt and key
    /// </summary>
    public class FileKeyStore : IKeyStore
    {
        private readonly string _pkFile;
        private readonly string _saltFilePath;

        /// <summary>
        /// FileKeyStore constructor for FileKeyStore instance creation
        /// </summary>
        /// <param name="basePath">Base Path where signing.key and signing.salt will be located</param>
        /// <returns>returns instance of FileKeyStore</returns>
        /// <exception cref="System.ArgumentException">Thrown when base path is null or empty.</exception>
        /// <exception cref="System.DirectoryNotFoundException">Thrown when base path is invalid.</exception>
        public FileKeyStore(string basePath)
        {

            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException("Base path given can't be empty or null", nameof(basePath));
            }

            if (!Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException("Base path does not exist or isn't a directory");
            }

            _pkFile = Path.Combine(basePath, "signing.key");
            _saltFilePath = Path.Combine(basePath, "signing.salt");
        }

        /// <summary>
        /// Saves Salt byte array into file.
        /// </summary>
        /// <param name="salt">Salt byte array to be saved in file</param>
        public void SaveSalt(byte[] salt)
        {
            File.WriteAllBytes(_saltFilePath, salt);
        }

        /// <summary>
        /// Reads Salt byte array from file and returns that.
        /// </summary>
        /// <returns>returns Salt byte array</returns>
        public byte[] LoadSalt()
        {
            return File.ReadAllBytes(_saltFilePath);
        }

        /// <summary>
        /// Loads Encrypted Key byte array from file and returns that.
        /// </summary>
        /// <returns>returns Encrypted Key byte array</returns>
        public byte[] LoadEncryptedKey()
        {
            return File.ReadAllBytes(_pkFile);
        }

        /// <summary>
        /// Saves Encrypted Key byte array in file.
        /// </summary>
        /// <param name="key">Encrypted Key byte array to be saved in file</param>
        public void SaveEncryptedKey(byte[] key)
        {
            File.WriteAllBytes(_pkFile, key);
        }
    }
}