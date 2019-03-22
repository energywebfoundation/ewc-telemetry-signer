

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Renci.SshNet;

namespace TelemetrySigner
{
    public class FTPManager
    {
        private string _userName;
        private string _password;
        private string _sftpHost;
        private int _port;
        private string _fingerPrint;
        private string _workingDir;

        public FTPManager(string userName, string password, string sftpHost, int port, string fingerPrint, string workingDir)
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
            _fingerPrint = fingerPrint.Replace(":", string.Empty).ToUpperInvariant(); ;
            _workingDir = workingDir;
        }

        public bool transferData(string data, string fileName)
        {
            Console.WriteLine("Creating client and connecting");
            try
            {
                using (var client = new SftpClient(_sftpHost, _port, _userName, _password))
                {
                    client.HostKeyReceived += (sender, e) =>
                    {
                        StringBuilder stringBuilder = new StringBuilder();

                        foreach (byte b in e.FingerPrint)
                            stringBuilder.AppendFormat("{0:X2}", b);

                        string hashString = stringBuilder.ToString();
                        e.CanTrust = (hashString == _fingerPrint);
                    };

                    client.Connect();
                    Console.WriteLine("Connected to {0}", _sftpHost);

                    client.ChangeDirectory(_workingDir);

                    byte[] byteData = Encoding.ASCII.GetBytes(data);
                    var stream = new MemoryStream();
                    stream.Write(byteData, 0, byteData.Length);
                    stream.Position = 0;

                    client.UploadFile(stream, fileName, null);

                    client.Disconnect();
                }
            }
            catch (Renci.SshNet.Common.SshConnectionException ex)
            {
                Console.WriteLine("Cannot connect to the server. {0}", ex.ToString());
                return false;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                Console.WriteLine("Unable to establish the socket. {0}", ex.ToString());
                return false;
            }
            catch (Renci.SshNet.Common.SshAuthenticationException ex)
            {
                Console.WriteLine("Authentication of SSH session failed. {0}", ex.ToString());
                return false;
            }
            return true;
        }

    }
}