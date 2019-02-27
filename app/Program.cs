using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TelemetrySigner
{
    class Program
    {
        private static ConcurrentQueue<string> _globalQueue;
        private static DateTime _lastFlush;

        public static SignerConfiguration _configuration;
        
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
                IngressHost = GetConfig("TELEMETRY_INGRESS_HOST","localhost:5010"),
                TelegrafSocket = GetConfig("INFLUX_SOCKET","/var/run/influxdb.sock"),
                ParityEndpoiunt = GetConfig("RPC_ENDPOINT","localhost"),
                PersistanceDirectory = GetConfig("TELEMETRY_INTERNAL_DIR","./")
            };

            Console.WriteLine("Configuration:");
            Console.WriteLine($"\tReading from: {_configuration.TelegrafSocket}");
            Console.WriteLine($"\tSending telemetry to: {_configuration.IngressHost}");
            Console.WriteLine($"\tUsing NodeId: {_configuration.NodeId}");
            
            // Prepare thread-safe queue
            _globalQueue = new ConcurrentQueue<string>();
            
            
            // Load private key
            
            
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
            
            // TODO: Do the actual flush to ingress
            
            Console.WriteLine($"Flushing {telemetryToSend.Count} to ingress...");
            Console.WriteLine($"{_globalQueue.Count} still in queue...");
                
            _lastFlush = DateTime.UtcNow;
        }
    }
}