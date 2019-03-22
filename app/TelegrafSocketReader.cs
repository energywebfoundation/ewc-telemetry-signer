using System;
using System.Collections.Concurrent;
using System.IO;

namespace TelemetrySigner
{
    /// <summary>
    /// Reads telemetry from a named-pipe
    /// </summary>
    public class TelegrafSocketReader
    {
        private readonly string _namedPipe;

        /// <summary>
        /// Get a new pipe reader
        /// </summary>
        /// <param name="socketPath">Path to the named pipe</param>
        /// <exception cref="ArgumentException">Path does not exist</exception>
        public TelegrafSocketReader(string socketPath)
        {
            if (!File.Exists(socketPath))
            {
                throw new ArgumentException("Socket does not exist",nameof(socketPath));
            }
            
            _namedPipe = socketPath;
        }

        /// <summary>
        /// Read from the pipe. Blocking.
        /// </summary>
        /// <param name="telemetryQueue">Queue to append read telemetry to</param>
        /// <exception cref="ArgumentNullException">Queue is not initialized</exception>
        public void Read(ConcurrentQueue<string> telemetryQueue)
        {
            if (telemetryQueue == null)
            {
                throw new ArgumentNullException(nameof(telemetryQueue),"Queue can't be null");
            }

            using (StreamReader socketStream = File.OpenText(_namedPipe))
            {
                // Read and block
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