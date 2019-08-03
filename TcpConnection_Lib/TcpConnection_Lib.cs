using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;
using System.Collections;

namespace TcpConnection_Lib
{
    public class TcpConnection : IDisposable
    {
        //fields and properties:
        private TcpClient client;
        private TcpListener listener;

        private Thread ListenThread;
        private Thread TcpReaderThread;

        public string RemoteEndpointAddress { get; private set; }

        private readonly Queue ReceivedStringQueue = new Queue();

        public bool TcpIsConnected
        {
            get
            {
                if (client != null)
                {
                    return client.Connected;
                }
                else
                {
                    return false;
                }
            }
        }

        private readonly byte[] receiveBuffer = new byte[4096];

        private readonly object syncLock = new object();


        //methods:
        public bool Connect(string IP, int port)
        {
            try
            {
                bool successFlag = false;

                lock (syncLock)
                {
                    try
                    {
                        client = new TcpClient();
                        client.Connect(IP, port);
                        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                        if (TcpReaderThread != null)
                        {
                            TcpReaderThread.Abort();
                            TcpReaderThread = null;
                        }
                        TcpReaderThread = new Thread(ReadData)
                        {
                            IsBackground = true
                        };
                        TcpReaderThread.Start();
                        successFlag = true;
                    }
                    catch { }
                }
                return successFlag;
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
                lock (syncLock)
                {
                    try
                    {
                        if (TcpReaderThread != null)
                        {
                            TcpReaderThread.Abort();
                            TcpReaderThread = null;
                        }
                        if (client != null)
                        {
                            client.Client.Close();
                            client.Close();
                            client = null;
                        }
                        if (ReceivedStringQueue.Count > 0)
                        {
                            ReceivedStringQueue.Clear();
                        }
                    }
                    catch { }
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
                bool successFlag = false;

                lock (syncLock)
                {
                    try
                    {
                        client.Client.Send(ASCIIEncoding.ASCII.GetBytes(sendString));
                        successFlag = true;
                    }
                    catch { }
                }
                return successFlag;
            }
            catch
            {
                return false;
            }
        }

        public string GetReceivedString()
        {
            try
            {
                string returnString = "";

                lock (ReceivedStringQueue.SyncRoot)
                {
                    try
                    {
                        if (ReceivedStringQueue.Count > 0)
                        {
                            returnString = ReceivedStringQueue.Dequeue().ToString();
                        }
                    }
                    catch { }
                }
                return returnString;
            }
            catch
            {
                return "";
            }
        }

        public bool Listen(int port)
        {
            try
            {
                IPEndPoint ipLocalEndPoint = new IPEndPoint(IPAddress.Any, port);
                listener = new TcpListener(ipLocalEndPoint);
                listener.Start(port);

                if (ListenThread != null)
                {
                    ListenThread.Abort();
                    ListenThread = null;
                }
                ListenThread = new Thread(ListeningMethod)
                {
                    IsBackground = true
                };
                ListenThread.Start();
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
                lock (syncLock)
                {
                    try
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
                        if (ReceivedStringQueue.Count > 0)
                        {
                            ReceivedStringQueue.Clear();
                        }
                    }
                    catch { }
                }
                GC.SuppressFinalize(this);
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
                        RemoteEndpointAddress = client.Client.RemoteEndPoint.ToString();
                        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                        if (TcpReaderThread != null)
                        {
                            TcpReaderThread.Abort();
                            TcpReaderThread = null;
                        }
                        TcpReaderThread = new Thread(ReadData)
                        {
                            IsBackground = true
                        };
                        TcpReaderThread.Start();
                    }
                    catch
                    {
                        if (listener != null)
                        {
                            listener.Stop();
                            listener = null;
                        }
                        break;
                    }
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
                    if (!client.Connected)
                    {
                        break;
                    }

                    bytesRead = client.GetStream().Read(receiveBuffer, 0, receiveBuffer.Length);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    CopyReceived(Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead));
                }
            }
            catch { }
        }

        private void CopyReceived(string receivedData)
        {
            try
            {
                lock (ReceivedStringQueue.SyncRoot)
                {
                    try
                    {
                        ReceivedStringQueue.Enqueue(receivedData);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}