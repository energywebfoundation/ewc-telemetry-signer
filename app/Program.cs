using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TelemetrySigner.Models;

namespace TelemetrySigner
{
    /// <summary>
    /// Class with entry point of project
    /// </summary>
    internal static class Program
    {
        private static ConcurrentQueue<string> _globalQueue;
        private static DateTime _lastFlush;
        private static SignerConfiguration _configuration;
        private static PayloadSigner _signer;
        private static Timer _flushTimer;
        private static FtpManager _ftpMgr;

        /// <summary>
        /// Function for getting Environment Variables
        /// </summary>
        /// <param name="name">Name of Environment Variable</param>
        /// <param name="defaultValue">Default value of Environment Variable</param>
        /// <returns>returns Environment Variable value</returns>
        private static string GetConfig(string name, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args">Command Line arguments</param>
        private static void Main(string[] args)
        {

            Console.WriteLine("Telemetry signer starting...");
            _lastFlush = DateTime.UtcNow;

            _configuration = new SignerConfiguration
            {
                NodeId = GetConfig("TELEMETRY_NODE_ID", "4816d758dd37833a3a5551001dac8a5fa737a342"),
                IngressHost = GetConfig("TELEMETRY_INGRESS_HOST", "https://localhost:5010"),
                TelegrafSocket = GetConfig("INFLUX_SOCKET", "/var/run/influxdb.sock"),
                ParityEndpoint = GetConfig("RPC_ENDPOINT", "http://localhost:8545"),
                PersistanceDirectory = GetConfig("TELEMETRY_INTERNAL_DIR", "./"),
                IngressFingerprint = GetConfig("TELEMETRY_INGRESS_FINGERPRINT", string.Empty),
                ParityWebSocketAddress = GetConfig("PARITY_WEB_SOCKET", string.Empty),

                FtpHost = GetConfig("SFTP_HOST", string.Empty),
                FtpPort = int.Parse(GetConfig("SFTP_PORT", "22")),
                FtpUser = GetConfig("SFTP_USER", string.Empty),
                FtpPass = GetConfig("SFTP_PASS", string.Empty),
                FtpFingerPrint = GetConfig("SFTP_FINGER_PRINT", string.Empty),
                FtpDir = GetConfig("FTP_DIR", "/")
            };

            Console.WriteLine("Configuration:");
            Console.WriteLine($"\tReading from: {_configuration.TelegrafSocket}");
            Console.WriteLine($"\tSending telemetry to: {_configuration.IngressHost}");
            Console.WriteLine($"\tIngress fingerprint: {_configuration.IngressFingerprint}");
            Console.WriteLine($"\tUsing NodeId: {_configuration.NodeId}");
            
            if (args.Length > 0 &&  args[0] == "--genkeys")
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
            _signer = new PayloadSigner(_configuration.NodeId,new FileKeyStore(_configuration.PersistanceDirectory));
            _signer.Init();

            //init FTP Manager
            _ftpMgr = new FtpManager(_configuration.FtpUser, _configuration.FtpPass, _configuration.FtpHost, _configuration.FtpPort,
            _configuration.FtpFingerPrint, _configuration.FtpDir);

            Task.Run(() =>
            {
                // Prepare flush timer
                _flushTimer = new Timer(FlushToIngress);
                _flushTimer.Change(5000, 10000);

                TelegrafSocketReader reader = new TelegrafSocketReader(_configuration.TelegrafSocket);
                reader.Read(_globalQueue);
            });
            
            //Real time telemetry subscription and sending to ingress
            RealTimeTelemetryManager ps = new RealTimeTelemetryManager(
                _configuration.NodeId,
                _configuration.ParityEndpoint,
                _configuration.ParityWebSocketAddress,
                (_configuration.IngressHost + "/api/ingress/realtime"),
                _configuration.IngressFingerprint, _signer, _ftpMgr);

            ps.SubscribeAndPost(true);

        }


        /// <summary>
        /// Sends data to Ingress restful end point
        /// </summary>
        /// <param name="state">Object state</param>
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
                    string fileName = $"{_configuration.NodeId}-{DateTime.UtcNow:yyyy-MM-dd_HH:mm:ss}.json";
                    try
                    {
                        if (!_ftpMgr.TransferData(jsonPayload, fileName))
                        {
                            Console.WriteLine("ERROR: Unable to send data on second channel. Data File {0}", fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: Unable to send data on second channel. Error Details {0}", ex);
                    }

                }
                _lastFlush = DateTime.UtcNow;

            }
            else
            {
                if (_globalQueue.Count > 250)
                {
                    // increase processing speed to 2 seconds
                    _flushTimer.Change(2000, 2000);
                }
                else // queue is small enough to get processed. back to normal speed
                {
                    _flushTimer.Change(10000, 10000);
                }
                _lastFlush = DateTime.UtcNow;
            }
            
        }
    }
}
