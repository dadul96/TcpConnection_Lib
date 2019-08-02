using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace TcpConnection_Class
{
    public class TcpConnection : IDisposable
    {
        //properties:
        private TcpClient client { get; set; }
        private TcpListener listener { get; set; }

        private Thread ListenThread { get; set; }
        private Thread TcpReaderThread;

        public string remoteEndpointAddress { get; set; }
        private string receivedString { get; set; }

        public bool TcpIsConnected
        {
            get
            {
                return (client != null && client.Connected);
            }
        }

        private readonly byte[] receiveBuffer = new byte[8192];

        private readonly object synchLock = new object();

        public object receiveStringLock { get; set; }


        //methods:
        public bool Connect(string IP, int port)
        {
            try
            {
                lock (synchLock)
                {
                    client.Connect(IP, port);
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    TcpReaderThread = new Thread(ReadData);
                    TcpReaderThread.IsBackground = true;
                    TcpReaderThread.Start();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Disconnect()
        {
            try
            {
                lock (synchLock)
                {
                    if (TcpReaderThread != null)
                    {
                        TcpReaderThread.Abort();
                        TcpReaderThread = null;
                    }
                    if (client.Connected == true && client != null)
                    {
                        client.Client.Close();
                        client.Close();
                        client = null;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Send(string sendString)
        {
            try
            {
                lock (synchLock)
                {
                    if (client != null)
                    {
                        client.Client.Send(ASCIIEncoding.ASCII.GetBytes(sendString));
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Listen(int port)
        {
            try
            {
                if (ListenThread != null)
                {
                    ListenThread.Abort();
                    ListenThread = null;
                }
                try
                {
                    listener.Server.SendTimeout = 1000;
                    listener.Server.ReceiveTimeout = 1000;
                    listener.Start();

                    ListenThread = new Thread(ListeningMethod);
                    ListenThread.IsBackground = true;
                    ListenThread.Start();
                }
                catch
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                lock (synchLock)
                {
                    Disconnect();
                    if (listener != null)
                    {
                        listener.Stop();
                        listener = null;
                    }
                    if (client != null)
                    {
                        client.Close();
                        client = null;
                    }
                    if (ListenThread != null)
                    {
                        ListenThread.Abort();
                        ListenThread = null;
                    }
                    if (TcpReaderThread != null)
                    {
                        TcpReaderThread.Abort();
                        TcpReaderThread = null;
                    }
                    GC.SuppressFinalize(this);
                }
            }
            catch { }
        }

        private void ListeningMethod()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        client = listener.AcceptTcpClient();
                        remoteEndpointAddress = client.Client.RemoteEndPoint.ToString();
                        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                        TcpReaderThread = new Thread(ReadData);
                        TcpReaderThread.IsBackground = true;
                        TcpReaderThread.Start();
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void ReadData()
        {
            try
            {
                int bytesRead = 0;

                while (true)
                {
                    try
                    {
                        if (!client.Connected)
                        {
                            break;
                        }

                        bytesRead = client.GetStream().Read(receiveBuffer, 0, receiveBuffer.Length);

                        if (bytesRead == 0)
                        {
                            break;
                        }

                        string dataReceived = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                        CopyReceived(dataReceived);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void CopyReceived(string receivedData)
        {
            lock (receiveStringLock)
            {
                receivedString = receivedData;
            }
        }
    }
}