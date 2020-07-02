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

        private bool PreparePackage(MutexActions action, string key, string sourceAddress, out byte[] package)
        {
            if (string.IsNullOrEmpty(sourceAddress))
                sourceAddress = string.Empty;
            
            package = null;

            switch (action)
            {
                case MutexActions.Lock:
                case MutexActions.Unlock:
                case MutexActions.ResetByKey:
                    if (string.IsNullOrEmpty(key) || key.Length > 128)
                        return false;
                    break;
            }

            MemoryStream contentStream = null;
            BinaryWriter binaryWriter = null;

            try
            {
                contentStream = 
                    new MemoryStream();
                binaryWriter = 
                    new BinaryWriter(contentStream);

                binaryWriter.Write((byte) action);
                
                switch (action)
                {
                    case MutexActions.Lock:
                    case MutexActions.Unlock:
                    case MutexActions.ResetByKey:
                        byte keySize =
                            (byte) key.Length;
                        binaryWriter.Write(keySize);

                        byte[] keyBytes =
                            System.Text.Encoding.UTF8.GetBytes(key);
                        binaryWriter.Write(keyBytes, 0, keyBytes.Length);

                        if (action == MutexActions.Lock)
                        {
                            byte sourceAddressSizeL =
                                (byte) sourceAddress.Length;
                            binaryWriter.Write(sourceAddressSizeL);

                            byte[] sourceAddressBytesL =
                                System.Text.Encoding.UTF8.GetBytes(sourceAddress);
                            binaryWriter.Write(sourceAddressBytesL, 0, sourceAddressBytesL.Length);
                        }

                        break;
                    case MutexActions.ResetBySource:
                        byte sourceAddressSizeR =
                            (byte) sourceAddress.Length;
                        binaryWriter.Write(sourceAddressSizeR);

                        byte[] sourceAddressBytesR =
                            System.Text.Encoding.UTF8.GetBytes(sourceAddress);
                        binaryWriter.Write(sourceAddressBytesR, 0, sourceAddressBytesR.Length);

                        break;
                }
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

        private bool Query(MutexActions action, string key, string sourceAddress = null) =>
            this.Connect(client =>
            {
                if (!this.PreparePackage(action, key, sourceAddress, out byte[] package))
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
        
        public void Lock(string key, string sourceAddress)
        {
            while (!this.Query(MutexActions.Lock, key, sourceAddress)) {}
        }

        public void Unlock(string key)
        {
            while (!this.Query(MutexActions.Unlock, key)) {}
        }

        public void Wait(string key)
        {
            while (!this.Query(MutexActions.Lock, key)) {}
            while (!this.Query(MutexActions.Unlock, key)) {}
        }
        
        public void ResetByKey(string key)
        {
            while (!this.Query(MutexActions.ResetByKey, key)) {}
        }
        
        public void ResetBySource(string sourceAddr = null)
        {
            if (string.IsNullOrEmpty(sourceAddr)) sourceAddr = string.Empty;
            
            while (!this.Query(MutexActions.ResetBySource, string.Empty, sourceAddr)) {}
        }
    }
}