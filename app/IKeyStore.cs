namespace TelemetrySigner
{
    /// <summary>
    /// FileKeyStore Interface
    /// </summary>
    public interface IKeyStore
    {
        /// <summary>
        /// Persists a salt
        /// </summary>
        /// <param name="salt"></param>
        void SaveSalt(byte[] salt);
        
        /// <summary>
        /// Load the salt form persistence
        /// </summary>
        /// <returns></returns>
        byte[] LoadSalt();

        /// <summary>
        /// Load the encrypted key from persistence
        /// </summary>
        /// <returns></returns>
        byte[] LoadEncryptedKey();
        
        /// <summary>
        /// Persist the encrypted key
        /// </summary>
        /// <param name="key"></param>
        void SaveEncryptedKey(byte[] key);
    }
}