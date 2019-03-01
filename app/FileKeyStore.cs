using System.IO;

namespace TelemetrySigner
{
    public class FileKeyStore : IKeyStore
    {
        private readonly string _pkFile;
        private readonly string _saltFilePath;

        public FileKeyStore(string basePath)
        {
            _pkFile = Path.Combine(basePath, "signing.key");
            _saltFilePath = Path.Combine(basePath, "signing.salt");
        }
        public void SaveSalt(byte[] salt)
        {
            File.WriteAllBytes(_saltFilePath,salt);
        }

        public byte[] LoadSalt()
        {
            return File.ReadAllBytes(_saltFilePath);
        }

        public byte[] LoadEncryptedKey()
        {
            return File.ReadAllBytes(_pkFile);
        }

        public void SaveEncryptedKey(byte[] key)
        {
            File.WriteAllBytes(_pkFile,key);
        }
    }
}