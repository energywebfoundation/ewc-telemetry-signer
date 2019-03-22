using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace TelemetrySigner
{
    internal static class Program
    {
        private static ConcurrentQueue<string> _globalQueue;
        private static DateTime _lastFlush;
        private static SignerConfiguration _configuration;
        private static PayloadSigner _signer;

        private static FTPManager _ftpMgr;

        private static string GetConfig(string name, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private static void Main(string[] args)
        {

            Console.WriteLine("Telemetry signer starting...");
            _lastFlush = DateTime.UtcNow;

            _configuration = new SignerConfiguration
            {
                NodeId = GetConfig("TELEMETRY_NODE_ID", "4816d758dd37833a3a5551001dac8a5fa737a342"),
                IngressHost = GetConfig("TELEMETRY_INGRESS_HOST", "https://localhost:5010"),
                TelegrafSocket = GetConfig("INFLUX_SOCKET", "/var/run/influxdb.sock"),
                ParityEndpoiunt = GetConfig("RPC_ENDPOINT", "localhost"),
                PersistanceDirectory = GetConfig("TELEMETRY_INTERNAL_DIR", "./"),
                IngressFingerprint = GetConfig("TELEMETRY_INGRESS_FINGERPRINT", string.Empty),
                ParityWebSocketAddress = GetConfig("PARITY_WEB_SOCKET", string.Empty),

                FTPHost = GetConfig("SFTP_HOST", string.Empty),
                FTPPort = System.Int32.Parse(GetConfig("SFTP_PORT", "22")),
                FTPUser = GetConfig("SFTP_USER", string.Empty),
                FTPPass = GetConfig("SFTP_PASS", string.Empty),
                FTPFingerPrint = GetConfig("SFTP_FINGER_PRINT", string.Empty),
                FTPDir = GetConfig("FTP_DIR", "/")
            };

            Console.WriteLine("Configuration:");
            Console.WriteLine($"\tReading from: {_configuration.TelegrafSocket}");
            Console.WriteLine($"\tSending telemetry to: {_configuration.IngressHost}");
            Console.WriteLine($"\tIngress fingerprint: {_configuration.IngressFingerprint}");
            Console.WriteLine($"\tUsing NodeId: {_configuration.NodeId}");

            if (args.Length > 0 && args[0] == "--genkeys")
            {
                Console.WriteLine("Telemetry signer generating keys...");
                PayloadSigner sig = new PayloadSigner(_configuration.NodeId, new FileKeyStore(_configuration.PersistanceDirectory));
                string pubkey = sig.GenerateKeys();
                Console.WriteLine("This nodes Public Key:");
                Console.WriteLine(pubkey);
                return;
            }

            // Prepare thread-safe queue
            _globalQueue = new ConcurrentQueue<string>();

            // load keys
            _signer = new PayloadSigner(_configuration.NodeId, new FileKeyStore(_configuration.PersistanceDirectory));
            _signer.Init();

            //init FTP Manager
            _ftpMgr = new FTPManager(_configuration.FTPUser, _configuration.FTPPass, _configuration.FTPHost, _configuration.FTPPort,
            _configuration.FTPFingerPrint, _configuration.FTPDir);


            // Prepare flush timer
            Timer flushTimer = new Timer(FlushToIngress);
            flushTimer.Change(5000, 10000);

            TelegrafSocketReader reader = new TelegrafSocketReader(_configuration.TelegrafSocket);
            reader.Read(_globalQueue);

            //Real time telemetry subscription and sending to ingress
            RealTimeTelemetryManager ps = new RealTimeTelemetryManager(
                _configuration.NodeId,
                _configuration.ParityEndpoiunt,
                _configuration.ParityWebSocketAddress,
                (_configuration.IngressHost + "/api/ingress/realtime"),
                _configuration.IngressFingerprint, _signer, _ftpMgr, true);

            ps.subscribeAndPost(true);

        }

        private static void FlushToIngress(object state)
        {
            // Flush to ingress if more than 10 telemetry recordings -or- last flush older that 1 minute
            if (_globalQueue.Count <= 10 && DateTime.UtcNow - _lastFlush <= new TimeSpan(0, 1, 0))
            {
                Console.WriteLine($"Not flushing: {_globalQueue.Count} Queued - {(DateTime.UtcNow - _lastFlush).TotalSeconds} seconds since flush");
                return;
            }

            List<string> telemetryToSend = new List<string>();
            while (telemetryToSend.Count < 50 && _globalQueue.TryDequeue(out string lineFromQueue))
            {
                telemetryToSend.Add(lineFromQueue);
            }

            Console.WriteLine($"Flushing {telemetryToSend.Count} to ingress. {_globalQueue.Count} still in queue.");

            TelemetryPacket pkt = new TelemetryPacket
            {
                NodeId = _configuration.NodeId,
                Payload = telemetryToSend,
                Signature = _signer.SignPayload(string.Join(string.Empty, telemetryToSend))
            };

            string jsonPayload = JsonConvert.SerializeObject(pkt);

            // Send data
            TalkToIngress tti = new TalkToIngress(_configuration.IngressHost + "/api/ingress/influx", _configuration.IngressFingerprint);
            bool sendSuccess = tti.SendRequest(jsonPayload).Result;
            if (!sendSuccess)
            {
                telemetryToSend.ForEach(_globalQueue.Enqueue);

                if (DateTime.UtcNow - _lastFlush > TimeSpan.FromMinutes(5))
                {
                    // unable to send to ingress for 5 minutes - send by second channel
                    Console.WriteLine("ERROR: Unable to send to ingress for more then 5 minutes. Sending queue on second channel.");
                    string fileName = string.Format("{0}-{1}.json", _configuration.NodeId, DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss"));
                    try
                    {
                        if (!_ftpMgr.transferData(jsonPayload, fileName))
                        {
                            Console.WriteLine("ERROR: Unable to send data on second channel. Data File {0}", fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: Unable to send data on second channel. Error Details {0}", ex.ToString());
                    }

                }
            }
            else
            {
                _lastFlush = DateTime.UtcNow;
            }

        }
    }
}
