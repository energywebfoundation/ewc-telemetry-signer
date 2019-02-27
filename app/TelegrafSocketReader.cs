using System;
using System.Collections.Concurrent;
using System.IO;

namespace TelemetrySigner
{
    public class TelegrafSocketReader
    {
        private readonly string _namedPipe;
        private readonly bool _shouldExit;

        public TelegrafSocketReader(string socketPath)
        {
            if (!File.Exists(socketPath))
            {
                throw new ArgumentException("Socket does not exist",nameof(socketPath));
            }
            
            _namedPipe = socketPath;
            _shouldExit = false;
        }

        public void Read(ConcurrentQueue<string> telemetryQueue)
        {
            if (telemetryQueue == null)
            {
                throw new ArgumentNullException(nameof(telemetryQueue),"Queue can't be null");
            }
            
            StreamReader socketStream = File.OpenText(_namedPipe);
            
            while (!_shouldExit)
            {
                // read forever from pipe
                string line = socketStream.ReadLine();
                telemetryQueue.Enqueue(line);
            }
        }
    }
}