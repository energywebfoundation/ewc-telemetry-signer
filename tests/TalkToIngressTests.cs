using System;
using TelemetrySigner;
using Xunit;

namespace tests
{
    public class TalkToIngressTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("foobar")]
        [InlineData("http://foo.bar")]
        public void ShouldNotAcceptInvalidUrl(string url)
        {
            const string fingerPrint = "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E";
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new TalkToIngress(url,fingerPrint);
            });
        }
        
        [Theory]
        [InlineData("https://foo.bar")]
        [InlineData("https://slock.it")]
        [InlineData("https://192.168.1.1")]
        [InlineData("https://1.1.1.1")]
        [InlineData("https://1.1.1.1:8080")]
        public void ShouldAcceptValidUrl(string url)
        {
            const string fingerPrint = "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E";
            
            var ex = Record.Exception(() =>
            {
                _ = new TalkToIngress(url,fingerPrint);
            });

            // check that no exception was thrown
            Assert.Null(ex);
            
        }
        
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void ShouldNotAcceptEmptyFingerprint(string fingerPrint)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _ = new TalkToIngress("https://foo.bar",fingerPrint);
            });
        }
    }
}