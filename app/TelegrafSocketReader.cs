using System;
using System.Collections.Concurrent;
using System.IO;

namespace TelemetrySigner
{
    public class TelegrafSocketReader
    {
        private readonly string _namedPipe;

        public TelegrafSocketReader(string socketPath)
        {
            if (!File.Exists(socketPath))
            {
                throw new ArgumentException("Socket does not exist",nameof(socketPath));
            }
            
            _namedPipe = socketPath;
        }

        public void Read(ConcurrentQueue<string> telemetryQueue)
        {
            if (telemetryQueue == null)
            {
                throw new ArgumentNullException(nameof(telemetryQueue),"Queue can't be null");
            }

            using (StreamReader socketStream = File.OpenText(_namedPipe))
            {
                while (!socketStream.EndOfStream)
                {
                    // read forever from pipe
                    string line = socketStream.ReadLine();
                    telemetryQueue.Enqueue(line);
                }
            }
        }
    }
}