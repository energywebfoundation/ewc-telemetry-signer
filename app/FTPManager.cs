using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace TelemetrySigner
{
    /// <summary>
    /// FTPManager class contains functionality for uploading files to SFTP Host
    /// </summary>
    public class FtpManager
    {
        private readonly string _userName;
        private readonly string _password;
        private readonly string _sftpHost;
        private readonly int _port;
        private readonly string _fingerPrint;
        private readonly string _workingDir;

        /// <summary>
        /// FTPManager constructor for FTPManager instance creation
        /// </summary>
        /// <param name="userName">SFTP User name</param>
        /// <param name="password">SFTP password</param>
        /// <param name="sftpHost">SFTP Host </param>
        /// <param name="port">SFTP port</param>
        /// <param name="fingerPrint">SFTP fingerPrint</param>
        /// <param name="workingDir">SFTP workingDir where file will be uploaded</param>
        /// <returns>returns instance of FTPManager</returns>
        /// <exception cref="System.ArgumentException">Thrown when any of provided argument is null or empty.</exception>
        public FtpManager(string userName, string password, string sftpHost, int port, string fingerPrint, string workingDir)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("SFTP user name is empty", nameof(userName));
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("SFTP password is empty", nameof(password));
            }
            if (string.IsNullOrWhiteSpace(sftpHost))
            {
                throw new ArgumentException("SFTP host is empty", nameof(sftpHost));
            }
            if (port < 0 || port > 65535)
            {
                throw new ArgumentException("SFTP  port is invalid", nameof(port));
            }
            if (string.IsNullOrWhiteSpace(fingerPrint))
            {
                throw new ArgumentException("SFTP  fingerPrint is empty", nameof(fingerPrint));
            }
            if (string.IsNullOrWhiteSpace(workingDir))
            {
                throw new ArgumentException("SFTP workingDir is empty", nameof(workingDir));
            }

            _userName = userName;
            _password = password;
            _sftpHost = sftpHost;
            _port = port;
            _fingerPrint = fingerPrint.Replace(":", string.Empty).ToUpperInvariant();
            _workingDir = workingDir;
        }

        /// <summary>
        /// Uploads the file to SFTP Server
        /// </summary>
        /// <param name="data">File data to be uploaded</param>
        /// <param name="fileName">File name to be used</param>
        /// <returns>returns true if operation is successful else false</returns>
        public bool TransferData(string data, string fileName)
        {
            Console.WriteLine("Creating client and connecting");
            try
            {
                using (var client = new SftpClient(_sftpHost, _port, _userName, _password))
                {
                    //remote host finger print validation
                    client.HostKeyReceived += (sender, e) =>
                    {
                        StringBuilder stringBuilder = new StringBuilder();

                        foreach (byte b in e.FingerPrint)
                            stringBuilder.AppendFormat("{0:X2}", b);

                        string hashString = stringBuilder.ToString();
                        bool fingerprintMatch = hashString == _fingerPrint;
                        if (!fingerprintMatch)
                        {
                            Console.WriteLine($"Second Channel fingerprint don't match!\n\tExp: {_fingerPrint}\n\tGot: {hashString}");
                        }
                        e.CanTrust = fingerprintMatch;
                    };

                    client.Connect();
                    Console.WriteLine("Connected to {0}", _sftpHost);

                    client.ChangeDirectory(_workingDir);

                    //data conversion to memory stream for writing to sftp
                    byte[] byteData = Encoding.ASCII.GetBytes(data);
                    var stream = new MemoryStream();
                    stream.Write(byteData, 0, byteData.Length);
                    stream.Position = 0;

                    //uploading file
                    client.UploadFile(stream, fileName);

                    client.Disconnect();
                }
            }
            catch (SshConnectionException ex)
            {
                Console.WriteLine("Cannot connect to the server. {0}", ex);
                return false;
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Unable to establish the socket. {0}", ex);
                return false;
            }
            catch (SshAuthenticationException ex)
            {
                Console.WriteLine("Authentication of SSH session failed. {0}", ex);
                return false;
            }
            return true;
        }

    }
}