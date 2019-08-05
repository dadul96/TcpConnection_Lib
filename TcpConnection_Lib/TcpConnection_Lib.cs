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
            lock (_syncLock)
            {
                try
                {
                    _client = new TcpClient();
                    _client.Connect(IP, port);
                    _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    _tcpReaderThread?.Abort();

                    _tcpReaderThread = new Thread(ReadData)
                    {
                        IsBackground = true
                    };
                    _tcpReaderThread.Start();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool TryDisconnect()
        {
            lock (_syncLock)
            {
                try
                {
                    _tcpReaderThread?.Abort();

                    _client?.Client?.Close();
                    _client?.Close();

                    _receivedDataQueue.Clear();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool TrySend(string sendString)
        {
            lock (_syncLock)
            {
                try
                {
                    _client.Client.Send(ASCIIEncoding.ASCII.GetBytes(sendString));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public string GetReceivedString()
        {
            lock (_receivedDataQueue.SyncRoot)
            {
                try
                {
                    if (_receivedDataQueue.Count > 0)
                    {
                        return _receivedDataQueue.Dequeue().ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool TryListen(int port)
        {
            try
            {
                IPEndPoint ipLocalEndPoint = new IPEndPoint(IPAddress.Any, port);
                _listener = new TcpListener(ipLocalEndPoint);
                _listener.Start(port);

                _listenThread?.Abort();

                _listenThread = new Thread(Listening)
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
            lock (_syncLock)
            {
                try
                {
                    TryDisconnect();

                    _listener?.Stop();

                    _listenThread?.Abort();
                }
                catch { }
            }
        }

        private void Listening()
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

                        _tcpReaderThread?.Abort();

                        _tcpReaderThread = new Thread(ReadData)
                        {
                            IsBackground = true
                        };
                        _tcpReaderThread.Start();
                    }
                    catch
                    {
                        _listener?.Stop();
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
            lock (_receivedDataQueue.SyncRoot)
            {
                try
                {
                    _receivedDataQueue.Enqueue(receivedData);
                }
                catch { }
            }
        }
    }
}