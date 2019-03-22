using System;
using System.Collections.Concurrent;
using System.IO;

namespace TelemetrySigner
{
    /// <summary>
    /// FTPManager class contains functionality for Telegraph socket reading
    /// </summary>
    public class TelegrafSocketReader
    {
        private readonly string _namedPipe;

        /// <summary>
        /// TelegrafSocketReader constructor for TelegrafSocketReader instance creation
        /// </summary>
        /// <param name="socketPath">Path to Telegraphs socket</param>
        /// <returns>returns instance of TelegrafSocketReader</returns>
        /// <exception cref="System.ArgumentException">Thrown when any of provided argument is null or empty.</exception>
        public TelegrafSocketReader(string socketPath)
        {
            if (!File.Exists(socketPath))
            {
                throw new ArgumentException("Socket does not exist", nameof(socketPath));
            }

            _namedPipe = socketPath;
        }

        /// <summary>
        /// Function for reading data from Telegraph named pipe
        /// </summary>
        /// <param name="telemetryQueue">Telegraph data will be pushed into provided reference of ConcurrentQueue</param>
        public void Read(ConcurrentQueue<string> telemetryQueue)
        {
            if (telemetryQueue == null)
            {
                throw new ArgumentNullException(nameof(telemetryQueue), "Queue can't be null");
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