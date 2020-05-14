using System;
using System.IO;
using System.Net.Sockets;

namespace LockingCenter.Mutex
{
    public class Connection : IConnection
    {
        private readonly Tuple<string, int> _remoteEndPoint;

        public Connection(string serverPort)
        {
            if (string.IsNullOrEmpty(serverPort))
                throw new ArgumentNullException(nameof(serverPort));

            string[] parts =
                serverPort.Split(':');
            if (parts.Length != 2)
                throw new Exception("not suitable format");
            
            if (!int.TryParse(parts[1], out int port))
                throw new Exception("not suitable format");
            
            this._remoteEndPoint = 
                new Tuple<string, int>(parts[0], port);
            
            if (!this.Ping())
                throw new Exception("unable to make connection!");
        }

        private bool Connect(Func<TcpClient, bool> connectionHandler)
        {
            TcpClient client = null;
            try
            {
                client =
                    new TcpClient(this._remoteEndPoint.Item1, this._remoteEndPoint.Item2);
                return connectionHandler(client);
            }
            catch
            {
                return false;
            }
            finally
            {
                client?.Close();
            }
        }

        private bool Ping() =>
            this.Connect(client => true);

        private bool PreparePackage(string key, byte action, out byte[] package)
        {
            package = null;
            
            if (string.IsNullOrEmpty(key) || key.Length > 128)
                return false;
            
            MemoryStream contentStream = null;
            BinaryWriter binaryWriter = null;

            try
            {
                contentStream = 
                    new MemoryStream();
                binaryWriter = 
                    new BinaryWriter(contentStream);
                
                byte keySize =
                    (byte) key.Length;
                binaryWriter.Write(keySize);

                byte[] keyBytes =
                    System.Text.Encoding.UTF8.GetBytes(key);
                binaryWriter.Write(keyBytes, 0, keyBytes.Length);

                binaryWriter.Write(action);
                binaryWriter.Flush();

                package = new byte[contentStream.Length];
                
                contentStream.Seek(0, SeekOrigin.Begin);
                contentStream.Read(package, 0, package.Length);
                
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                contentStream?.Close();
                binaryWriter?.Close();
            }
        }

        private bool Query(string key, byte action) =>
            this.Connect(client =>
            {
                if (!this.PreparePackage(key, action, out byte[] package))
                    return false;

                try
                {
                    client.GetStream().Write(
                        package, 0, package.Length);
                    byte r =
                        (byte) client.GetStream().ReadByte();
                    return (char) r == '+';
                }
                catch
                {
                    return false;
                }
            });
        
        public void Lock(string key)
        {
            while (!this.Query(key, 1)) {}
        }

        public void Unlock(string key)
        {
            while (!this.Query(key, 2)) {}
        }

        public void Wait(string key)
        {
            while (!this.Query(key, 1)) {}
            while (!this.Query(key, 2)) {}
        }
        
        public void Reset(string key)
        {
            while (!this.Query(key, 3)) {}
        }
    }
}