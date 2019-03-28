using System;
using System.IO;
using FluentAssertions;
using TelemetrySigner;
using Xunit;

namespace tests
{
    public class FileKeyStoreTests
    {
        [Fact]
        public void ShouldThrowOnNonExistingBasePath()
        {
            Assert.Throws<DirectoryNotFoundException>(() => { _ = new FileKeyStore("does-not-exist"); });
        } 
        
        [Theory]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData(null)]
        public void ShouldThrowOnEmptyBasePath(string path)
        {
            Assert.Throws<ArgumentException>(() => { _ = new FileKeyStore(path); });
        }
        
        [Fact]
        public void ShouldSaveAndLoadSaltToFile()
        {
            // create a temp path
            string tmpPath = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tmpPath);
            
            var fksForWrite = new FileKeyStore(tmpPath);

            byte[] bytesToWrite = {0x0, 0x1, 0x2, 0x3, 0x4, 0x5};
            
            // write some bytes to the salt file
            fksForWrite.SaveSalt(bytesToWrite);
            
            // check file if bytes where written
            byte[] directFromFile = File.ReadAllBytes(Path.Join(tmpPath, "signing.salt"));

            directFromFile.Should().ContainInOrder(bytesToWrite);


            // see if we can load them again
            var fksForLoad = new FileKeyStore(tmpPath);
            byte[] loadedByFks = fksForLoad.LoadSalt();
            loadedByFks.Should().ContainInOrder(bytesToWrite);
        } 
        
        [Fact]
        public void ShouldSaveAndLoadKeyToFile()
        {
            // create a temp path
            string tmpPath = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tmpPath);
            
            var fksForWrite = new FileKeyStore(tmpPath);

            byte[] bytesToWrite = {0x0, 0x1, 0x2, 0x3, 0x4, 0x5,0x0, 0x1, 0x2, 0x3, 0x4, 0x5,0x0, 0x1, 0x2, 0x3, 0x4, 0x5,0x0, 0x1, 0x2, 0x3, 0x4, 0x5};
            
            // write some bytes to the salt file
            fksForWrite.SaveEncryptedKey(bytesToWrite);
            
            // check file if bytes where written
            byte[] directFromFile = File.ReadAllBytes(Path.Join(tmpPath, "signing.key"));

            directFromFile.Should().ContainInOrder(bytesToWrite);


            // see if we can load them again
            var fksForLoad = new FileKeyStore(tmpPath);
            byte[] loadedByFks = fksForLoad.LoadEncryptedKey();
            loadedByFks.Should().ContainInOrder(bytesToWrite);
        }
    }
}