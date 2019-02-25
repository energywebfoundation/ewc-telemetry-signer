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
        
        private static string GetConfig(string name, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return String.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
        
        static void Main(string[] args)
        {
            Console.WriteLine("Telegraf signer starting...");
            _lastFlush = DateTime.UtcNow;

            string path = GetConfig("INFLUX_SOCKET","/var/run/influxdb.sock");
            string ingressHost = GetConfig("TELEMETRY_INGRESS_HOST","localhost:5010");


            Console.WriteLine("Configuration:");
            Console.WriteLine($"\tReading from: {path}");
            Console.WriteLine($"\tSending telemetry to: {ingressHost}");
            
            // Prepare thread-safe queue
            _globalQueue = new ConcurrentQueue<string>();
            
            // Prepare flush timer
            Timer flushTimer = new Timer(FlushToIngress,null,new TimeSpan(0,0,30),new TimeSpan(0,0,10));
            
            var reader = new TelegrafSocketReader(path);
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