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
        private TcpClient _client;
        private TcpListener _listener;

        private Thread _listenThread;
        private Thread _tcpReaderThread;

        private readonly Queue _receivedDataQueue = new Queue();

        private readonly byte[] _receiveBuffer = new byte[4096];

        private readonly object _syncLock = new object();

        public string RemoteEndpointAddress { get; private set; }

        public bool TcpIsConnected
        {
            get
            {
                if (_client != null)
                {
                    return _client.Connected;
                }
                else
                {
                    return false;
                }
            }
        }


        //methods:
        public bool TryConnect(string IP, int port)
        {
            try
            {
                bool successFlag = false;

                lock (_syncLock)
                {
                    try
                    {
                        _client = new TcpClient();
                        _client.Connect(IP, port);
                        _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                        if (_tcpReaderThread != null)
                        {
                            _tcpReaderThread.Abort();
                            _tcpReaderThread = null;
                        }
                        _tcpReaderThread = new Thread(ReadData)
                        {
                            IsBackground = true
                        };
                        _tcpReaderThread.Start();
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

        public bool TryDisconnect()
        {
            try
            {
                lock (_syncLock)
                {
                    try
                    {
                        if (_tcpReaderThread != null)
                        {
                            _tcpReaderThread.Abort();
                            _tcpReaderThread = null;
                        }
                        if (_client != null)
                        {
                            _client.Client.Close();
                            _client.Close();
                            _client = null;
                        }
                        if (_receivedDataQueue.Count > 0)
                        {
                            _receivedDataQueue.Clear();
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

        public bool TrySend(string sendString)
        {
            try
            {
                bool successFlag = false;

                lock (_syncLock)
                {
                    try
                    {
                        _client.Client.Send(ASCIIEncoding.ASCII.GetBytes(sendString));
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

                lock (_receivedDataQueue.SyncRoot)
                {
                    try
                    {
                        if (_receivedDataQueue.Count > 0)
                        {
                            returnString = _receivedDataQueue.Dequeue().ToString();
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

        public bool TryListen(int port)
        {
            try
            {
                IPEndPoint ipLocalEndPoint = new IPEndPoint(IPAddress.Any, port);
                _listener = new TcpListener(ipLocalEndPoint);
                _listener.Start(port);

                if (_listenThread != null)
                {
                    _listenThread.Abort();
                    _listenThread = null;
                }
                _listenThread = new Thread(ListeningMethod)
                {
                    IsBackground = true
                };
                _listenThread.Start();
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
                lock (_syncLock)
                {
                    try
                    {
                        TryDisconnect();
                        if (_listener != null)
                        {
                            _listener.Stop();
                            _listener = null;
                        }
                        if (_client != null)
                        {
                            _client.Close();
                            _client = null;
                        }
                        if (_listenThread != null)
                        {
                            _listenThread.Abort();
                            _listenThread = null;
                        }
                        if (_tcpReaderThread != null)
                        {
                            _tcpReaderThread.Abort();
                            _tcpReaderThread = null;
                        }
                        _receivedDataQueue.Clear();
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
                        _client = _listener.AcceptTcpClient();
                        RemoteEndpointAddress = _client.Client.RemoteEndPoint.ToString();
                        _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                        if (_tcpReaderThread != null)
                        {
                            _tcpReaderThread.Abort();
                            _tcpReaderThread = null;
                        }
                        _tcpReaderThread = new Thread(ReadData)
                        {
                            IsBackground = true
                        };
                        _tcpReaderThread.Start();
                    }
                    catch
                    {
                        if (_listener != null)
                        {
                            _listener.Stop();
                            _listener = null;
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
                    if (!_client.Connected)
                    {
                        break;
                    }

                    bytesRead = _client.GetStream().Read(_receiveBuffer, 0, _receiveBuffer.Length);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    CopyReceived(Encoding.ASCII.GetString(_receiveBuffer, 0, bytesRead));
                }
            }
            catch { }
        }

        private void CopyReceived(string receivedData)
        {
            try
            {
                lock (_receivedDataQueue.SyncRoot)
                {
                    try
                    {
                        _receivedDataQueue.Enqueue(receivedData);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}