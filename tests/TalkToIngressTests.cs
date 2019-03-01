using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
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
        
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ShouldNotAcceptEmptyPayload(string payload)
        {
            const string fingerPrint = "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E";

            var tti = new TalkToIngress("https://foo.bar",fingerPrint);
            await Assert.ThrowsAsync<ArgumentException>(async () => { await tti.SendRequest(payload); });
        }

        [Fact]
        public void ShouldSendTelemetryAsJson()
        {
            // Test config
            string expectedPayload = JsonConvert.SerializeObject(new
            {
                Test = true,
                AString = "hello world"
            });
            const string url = "https://foo.bar";
            const string expectedUrl = "https://foo.bar/api/ingress/influx";
            
            // Setup
            const string fingerPrint = "ED:40:5C:C9:E2:71:44:11:78:47:1C:09:6F:28:2E:B5:F9:4D:6E:CE:90:BC:64:5B:ED:9A:46:1F:20:E2:EE:4E";
            bool payloadCorrect = false;
            bool urlCorrect = false;
            
            // Prepare mock
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                // Setup the PROTECTED method to mock
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // prepare the expected response of the mocked http call
                .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
                {
                    // make sure payload is correct
                    payloadCorrect = request.Content.ReadAsStringAsync().Result == expectedPayload;
                    urlCorrect = request.RequestUri.ToString() == expectedUrl;
                    
                    return new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.Accepted,
                        Content = new StringContent(""),
                    };
                })
                .Verifiable();
            
            
            // run test code
            var tti = new TalkToIngress(url,fingerPrint,handlerMock.Object);
            bool sendResult = tti.SendRequest(expectedPayload).Result;
            
            Assert.True(payloadCorrect);
            Assert.True(urlCorrect);
            Assert.True(sendResult);

        }
    }
}