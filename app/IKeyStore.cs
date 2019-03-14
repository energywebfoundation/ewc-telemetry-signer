namespace TelemetrySigner
{
    public interface IKeyStore
    {
        void SaveSalt(byte[] salt);
        byte[] LoadSalt();

        byte[] LoadEncryptedKey();
        void SaveEncryptedKey(byte[] key);
    }
}