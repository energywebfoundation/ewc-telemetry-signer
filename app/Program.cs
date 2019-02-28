using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;

namespace TelemetrySigner
{
    public class TelemetryPacket
    {
        [JsonProperty("nodeid")]
        public string NodeId { get; set; } 
        [JsonProperty("payload")]
        public IList<string> Payload { get; set; }
        [JsonProperty("signature")]
        public string Signature { get; set; }    
    }
    
    class Program
    {
        private static ConcurrentQueue<string> _globalQueue;
        private static DateTime _lastFlush;

        private static SignerConfiguration _configuration;
        private static PayloadSigner _signer;

        private static string GetConfig(string name, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return String.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
        
        
        
        static void Main(string[] args)
        {

            
            
            Console.WriteLine("Telemetry signer starting...");
            _lastFlush = DateTime.UtcNow;

            _configuration = new SignerConfiguration
            {
                NodeId = GetConfig("TELEMETRY_NODE_ID","4816d758dd37833a3a5551001dac8a5fa737a342"),
                IngressHost = GetConfig("TELEMETRY_INGRESS_HOST","https://localhost:5010"),
                TelegrafSocket = GetConfig("INFLUX_SOCKET","/var/run/influxdb.sock"),
                ParityEndpoiunt = GetConfig("RPC_ENDPOINT","localhost"),
                PersistanceDirectory = GetConfig("TELEMETRY_INTERNAL_DIR","./"),
                IngressFingerprint = GetConfig("TELEMETRY_INGRESS_FINGERPRINT",String.Empty),
            };

            Console.WriteLine("Configuration:");
            Console.WriteLine($"\tReading from: {_configuration.TelegrafSocket}");
            Console.WriteLine($"\tSending telemetry to: {_configuration.IngressHost}");
            Console.WriteLine($"\tIngress fingerprint: {_configuration.IngressFingerprint}");
            Console.WriteLine($"\tUsing NodeId: {_configuration.NodeId}");
            
            if (args.Length > 0 &&  args[0] == "--genkeys")
            {
                Console.WriteLine("Telemetry signer generating keys...");
                PayloadSigner sig = new PayloadSigner(_configuration);
                string pubkey = sig.GenerateKeys();
                Console.WriteLine("Public Key: " + pubkey);
                return;
            }
            
            // Prepare thread-safe queue
            _globalQueue = new ConcurrentQueue<string>();
            
            // load keys
            _signer = new PayloadSigner(_configuration);
            _signer.Init();
            

            // Prepare flush timer
            Timer flushTimer = new Timer(FlushToIngress,null,new TimeSpan(0,0,30),new TimeSpan(0,0,10));
            
            var reader = new TelegrafSocketReader(_configuration.TelegrafSocket);
            reader.Read(_globalQueue);

        }

        private static void FlushToIngress(object state)
        {
            // Flush to ingress if more than 10 telemetry recordings -or- last flush older that 1 minute
            if (_globalQueue.Count <= 10 && DateTime.UtcNow - _lastFlush <= new TimeSpan(0, 1, 0)) return;
            
            List<string> telemetryToSend = new List<string>();
            while (telemetryToSend.Count < 50 && _globalQueue.TryDequeue(out string lineFromQueue))
            {
                telemetryToSend.Add(lineFromQueue);
            }
            
            Console.WriteLine($"Flushing {telemetryToSend.Count} to ingress...");
            Console.WriteLine($"{_globalQueue.Count} still in queue...");

            var pkt = new TelemetryPacket
            {
                NodeId = _configuration.NodeId,
                Payload = telemetryToSend,
                Signature = _signer.SignPayload(string.Join(String.Empty,telemetryToSend))
            };
            
            string jsonPayload = JsonConvert.SerializeObject(pkt,Formatting.Indented);

            Console.WriteLine("SENDING:" + jsonPayload);
            
            
            // Send data
            var tti = new TalkToIngress(_configuration.IngressHost,_configuration.IngressFingerprint);
            bool sendSuccess = tti.SendRequest(jsonPayload);
            if (!sendSuccess)
            {
                Console.WriteLine($"ERROR: Unable to send to ingress. Re-queueing data.");
                telemetryToSend.ForEach(_globalQueue.Enqueue);

                if (DateTime.UtcNow - _lastFlush > TimeSpan.FromMinutes(5))
                {
                    // TODO: unable to send to ingress for 5 minutes - send by second channel
                    
                }
            }
            else
            {
                Console.WriteLine("Send success!");
                _lastFlush = DateTime.UtcNow;
            }
            
        }
    }
}