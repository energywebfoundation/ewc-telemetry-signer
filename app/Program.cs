using System;

namespace TelemetrySigner
{
    class Program
    {
        private static string GetConfig(string name, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(name);
            return String.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }
        
        static void Main(string[] args)
        {
            Console.WriteLine("Telegraf signer starting...");

            string path = GetConfig("INFLUX_SOCKET","/var/run/influxdb.sock");
            string ingressHost = GetConfig("TELEMETRY_INGRESS_HOST","localhost:5010");


            Console.WriteLine("Configuration:");
            Console.WriteLine($"\tReading from: {path}");
            Console.WriteLine($"\tSending telemetry to: {ingressHost}");
            
            var reader = new TelegrafSocketReader(path);
            reader.Read();

        }
    }
}