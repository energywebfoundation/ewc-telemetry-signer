using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TelemetrySigner;
using TelemetrySigner.Models;
using Xunit;

namespace tests
{
    public class FTPManagerTests
    {

        private string _ftpHost;
        private int _ftpPort;
        private string _userName;
        private string _password;
        private string _fingerPrint;
        public FTPManagerTests()
        {
            PayloadSigner sig = new PayloadSigner(
                "4816d758dd37833a3a5551001dac8a5fa737a342",
                new FileKeyStore("./"));
            string pubkey = sig.GenerateKeys();

            _ftpHost = "127.0.0.1";
            _ftpPort = 2222;
            _userName = "foo";
            _password = "pass";
            _fingerPrint = "78:72:96:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d";

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

            FtpManager fm = new FtpManager(_userName, _password, _ftpHost, _ftpPort, _fingerPrint, "/upload/dropzone/");
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

            FtpManager fm = new FtpManager(_userName, _password, _ftpHost, _ftpPort, "38:32:36:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "/upload/dropzone/");
            string fileName = string.Format("{0}-{1}.json", nodeId, DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss"));
            //Assert.True(!fm.transferData(jsonPayload, fileName));
        }

        [Theory]
        [InlineData("", "pass_123", "127.0.0.1", 22, "78:72:96:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "/upload/dropzone/")]
        [InlineData("foo", "", "127.0.0.1", 22, "78:72:96:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "/upload/dropzone/")]
        [InlineData("foo", "pass_123", "", 22, "78:72:96:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "/upload/dropzone/")]
        [InlineData("foo", "pass_123", "127.0.0.1", -22, "78:72:96:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "/upload/dropzone/")]
        [InlineData("foo", "pass_123", "127.0.0.1", 999999, "78:72:96:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "/upload/dropzone/")]
        [InlineData("foo", "pass_123", "127.0.0.1", 22, "", "/upload/dropzone/")]
        [InlineData("foo", "pass_123", "127.0.0.1", 22, "78:72:96:8e:ad:ac:8c:31:57:b4:80:ba:2d:e4:88:9d", "")]
        void invalidArgumentShouldNotCreateInstance(string userName, string password, string sftpHost, int port, string fingerPrint, string workingDir)
        {

            FtpManager mgr = null;

            PayloadSigner signer = new PayloadSigner("4816d758dd37833a3a5551001dac8a5fa737a342", new FileKeyStore("./"));
            signer.Init();

            Assert.Throws<ArgumentException>(() =>
            {
                mgr = new FtpManager(userName, password, sftpHost, port, fingerPrint, workingDir);
            });
            Assert.Null(mgr);
        }


        [Fact]
        void inValidHostAddressShouldFail()
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

            FtpManager fm = new FtpManager(_userName, _password, "127.0.0.1", 8081, _fingerPrint, "/upload/dropzone/");
            string fileName = string.Format("{0}-{1}.json", nodeId, DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss"));
            Assert.True(!fm.TransferData(jsonPayload, fileName));

        }

        [Fact]
        void inValidAuthShouldFail()
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

            FtpManager fm = new FtpManager("foo21", "pass12", _ftpHost, _ftpPort, _fingerPrint, "/upload/dropzone/");
            string fileName = string.Format("{0}-{1}.json", nodeId, DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss"));
            Assert.True(!fm.TransferData(jsonPayload, fileName));

        }
    }
}