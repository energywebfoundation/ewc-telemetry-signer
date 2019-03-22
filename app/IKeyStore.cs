namespace TelemetrySigner
{
    /// <summary>
    /// Interface that describes a store to persist a key and a salt
    /// </summary>
    public interface IKeyStore
    {
        /// <summary>
        /// Method to save the salt to persistence
        /// </summary>
        /// <param name="salt">salt as bytes</param>
        void SaveSalt(byte[] salt);
        
        /// <summary>
        /// Load the salt from persistence
        /// </summary>
        /// <returns></returns>
        byte[] LoadSalt();

        /// <summary>
        /// Load the key from persistence
        /// </summary>
        /// <returns></returns>
        byte[] LoadEncryptedKey();
        
        /// <summary>
        /// Method to save the key to persistence
        /// </summary>
        /// <param name="key">key as bytes</param>
        void SaveEncryptedKey(byte[] key);
    }
}