using System;
using TelemetrySigner;

namespace tests
{
    public class MockKeyStore : IKeyStore
    {
        public byte[] Salt { get; set; }
        public byte[] Key { get; set; }

        public void SaveSalt(byte[] salt)
        {
            Salt = salt;
        }

        public byte[] LoadSalt()
        {
            if (Salt != null && Salt.Length > 0)
            {
                return Salt;
            }
            throw new Exception("No Salt stored");
        }

        public byte[] LoadEncryptedKey()
        {
            if (Key != null && Key.Length > 0)
            {
                return Key;
            }
            throw new Exception("No Key stored");
        }

        public void SaveEncryptedKey(byte[] key)
        {
            Key = key;
        }
    }
}