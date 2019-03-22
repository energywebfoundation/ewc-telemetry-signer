using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TelemetrySigner;
using Xunit;

namespace tests
{
    public class FTPManagerTests
    {

        public FTPManagerTests()
        {
            PayloadSigner sig = new PayloadSigner(
                "4816d758dd37833a3a5551001dac8a5fa737a342",
                new FileKeyStore("./"));
            string pubkey = sig.GenerateKeys();

        }

        [Fact]
        void validFingerPrintShouldUploadFile()
        {

            string nodeId = "4816d758dd37833a3a5551001dac8a5fa737a342";
            PayloadSigner _signer = new PayloadSigner(nodeId, new FileKeyStore("./"));
            _signer.Init();

            List<string> telemetryToSend = new List<string>();
            telemetryToSend.Add("abc 1");
            telemetryToSend.Add("abc 2");
            telemetryToSend.Add("abc 3");

            TelemetryPacket pkt = new TelemetryPacket
            {
                NodeId = nodeId,
                Payload = telemetryToSend,
                Signature = _signer.SignPayload(string.Join(string.Empty, telemetryToSend))
            };

            string jsonPayload = JsonConvert.SerializeObject(pkt);

            FTPManager fm = new FTPManager("foo", "pass", "127.0.0.1", 2222, "78:72:96:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "/upload/dropzone/");
            string fileName = string.Format("{0}-{1}.json", nodeId, DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss"));
           // Assert.True(fm.transferData(jsonPayload, fileName));
        }

        [Fact]
        void inValidFingerPrintShouldFail()
        {

            string nodeId = "4816d758dd37833a3a5551001dac8a5fa737a342";
            PayloadSigner _signer = new PayloadSigner(nodeId, new FileKeyStore("./"));
            _signer.Init();

            List<string> telemetryToSend = new List<string>();
            telemetryToSend.Add("abc 1");
            telemetryToSend.Add("abc 2");
            telemetryToSend.Add("abc 3");

            TelemetryPacket pkt = new TelemetryPacket
            {
                NodeId = nodeId,
                Payload = telemetryToSend,
                Signature = _signer.SignPayload(string.Join(string.Empty, telemetryToSend))
            };

            string jsonPayload = JsonConvert.SerializeObject(pkt);

            FTPManager fm = new FTPManager("foo", "pass", "127.0.0.1", 2222, "38:32:36:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "/upload/dropzone/");
            string fileName = string.Format("{0}-{1}.json", nodeId, DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss"));
            Assert.True(!fm.transferData(jsonPayload, fileName));
        }
    }
}