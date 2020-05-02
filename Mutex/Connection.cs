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

        private bool Ping()
        {
            try
            {
                TcpClient client = 
                    this.Connect();
                client.Close();

                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private TcpClient Connect() =>
            new TcpClient(this._remoteEndPoint.Item1, this._remoteEndPoint.Item2);

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

                package = contentStream.GetBuffer();
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

        private bool Result(TcpClient client)
        {
            try
            {
                byte r =
                    (byte) client.GetStream().ReadByte();

                return (char) r == '+';
            }
            catch
            {
                return false;
            }
        }

        private bool Query(TcpClient client, string key, byte action)
        {
            if (!this.PreparePackage(key, action, out byte[] package))
                return false;

            try
            {
                client.GetStream().Write(package, 0, package.Length);
            }
            catch
            {
                return false;
            }

            return this.Result(client);
        }
        
        public void Lock(string key)
        {
            TcpClient client =
                this.Connect();
            
            while (!this.Query(client, key, 1)) {}
            
            client.Close();
        }

        public void Unlock(string key)
        {
            TcpClient client =
                this.Connect();
            
            while (!this.Query(client, key, 2)) {}
            
            client.Close();
        }

        public void Wait(string key)
        {
            TcpClient client =
                this.Connect();
            
            while (!this.Query(client, key, 1)) {}
            
            while (!this.Query(client, key, 2)) {}
            
            client.Close();
        }
        
        public void Reset(string key)
        {
            TcpClient client =
                this.Connect();
            
            while (!this.Query(client, key, 3)) {}
            
            client.Close();
        }
    }
}