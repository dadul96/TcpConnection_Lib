using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace TcpConnection_Lib
{
    public class TcpConnection : IDisposable
    {
        //##########################
        //fields and properties:
        //##########################
        private TcpClient _client;
        private TcpListener _listener;

        private Thread readingThread;

        private volatile bool _threadRunningFlag;

        private readonly ConcurrentQueue<string> _receivedDataQueue = new ConcurrentQueue<string>();

        private readonly object _syncRoot = new object();

        /// <summary>
        /// <c>TcpIsConnected</c> returns a boolean value that signals the TCP connection state.
        /// </summary>
        public bool TcpIsConnected { get; private set; }


        //##########################
        //methods:
        //##########################

        /// <summary>
        /// Try connecting a client to a specific endpoint.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <returns>True, if a connection could be accomplished.</returns>
        public bool TryConnect(string ipAddress, int port)
        {
            lock (_syncRoot)
            {
                try
                {
                    if (TcpIsConnected || _client != null)
                    {
                        return false;
                    }

                    _client = new TcpClient();
                    _client.Connect(ipAddress, port);
                    _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    return TcpIsConnected = _client.Connected;
                }
                catch (SocketException)
                {
                    return false;
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
            }
        }

        /// <summary>
        /// Try to listen for clients on a specific port and any available IP address.
        /// </summary>
        /// <param name="port"></param>
        /// <returns>True, if a connection could be accomplished.</returns>
        public bool TryListen(int port)
        {
            lock (_syncRoot)
            {
                try
                {
                    if (TcpIsConnected || _listener != null)
                    {
                        return false;
                    }

                    IPEndPoint ipLocalEndPoint = new IPEndPoint(IPAddress.Any, port);
                    _listener = new TcpListener(ipLocalEndPoint);
                    _listener.Start(port);

                    _client = _listener.AcceptTcpClient();
                    _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    _listener?.Stop();

                    return TcpIsConnected = _client.Connected;
                }
                catch (SocketException)
                {
                    return false;
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
            }
        }

        /// <summary>
        /// Try to listen for clients on a specific port and any available IP address and pass the string-argument "RemoteEndpointAddress" by reference.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="RemoteEndpointAddress"></param>
        /// <returns>True, if a connection could be accomplished.</returns>
        public bool TryListen(int port, out string RemoteEndpointAddress)
        {
            lock (_syncRoot)
            {
                try
                {
                    if (TcpIsConnected || _listener != null)
                    {
                        RemoteEndpointAddress = null;
                        return false;
                    }

                    IPEndPoint ipLocalEndPoint = new IPEndPoint(IPAddress.Any, port);
                    _listener = new TcpListener(ipLocalEndPoint);
                    _listener.Start(port);

                    _client = _listener.AcceptTcpClient();
                    _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                    RemoteEndpointAddress = _client.Client.RemoteEndPoint.ToString();

                    _listener?.Stop();

                    return TcpIsConnected = _client.Connected;
                }
                catch (SocketException)
                {
                    RemoteEndpointAddress = null;
                    return false;
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
            }
        }

        /// <summary>
        /// Stop the data reading, close the client/listener, clear the _receivedDataQueue and set the TcpIsConnected-flag to false.
        /// </summary>
        public void Disconnect()
        {
            lock (_syncRoot)
            {
                try
                {
                    StopReadingData();

                    _client?.Client?.Close();
                    _client?.Close();
                    _client = null;

                    _listener?.Stop();
                    _listener = null;

                    while (_receivedDataQueue.TryDequeue(out string tempString)) { }    //workaround, because .Clear() is not available in .NET Standard 2.0

                    TcpIsConnected = false;
                }
                catch (Exception Ex)
                {
                    throw Ex;
                }
            }
        }

        /// <summary>
        /// Run the Disconnect()-method.
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// Try sending the sendString.
        /// </summary>
        /// <param name="sendString"></param>
        /// <returns>True, if the sending was successful.</returns>
        public bool TrySend(string sendString)
        {
            lock (_syncRoot)
            {
                try
                {
                    if (!(TcpIsConnected = _client.Connected))
                    {
                        return false;
                    }

                    NetworkStream stream = _client?.GetStream();

                    byte[] sendMessageBuffer = UTF8Encoding.UTF8.GetBytes(sendString);

                    int length = sendMessageBuffer.Length;

                    byte[] lengthBuffer = System.BitConverter.GetBytes(length);

                    if (System.BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(lengthBuffer);
                    }

                    stream.Write(lengthBuffer, 0, lengthBuffer.Length);
                    stream.Write(sendMessageBuffer, 0, sendMessageBuffer.Length);

                    return true;
                }
                catch (ArgumentNullException ArgNulEx)
                {
                    throw ArgNulEx;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Polling method for checking, if a new string was added to the _receivedDataQueue.
        /// </summary>
        /// <returns>NULL, if the queue is empty. Otherwise the string will be returned.</returns>
        public string GetReceivedString()
        {
            try
            {
                if (_receivedDataQueue.IsEmpty)
                {
                    return null;
                }

                _receivedDataQueue.TryDequeue(out string tempString);
                return tempString;
            }
            catch (Exception Ex)
            {
                throw Ex;
            }
        }

        /// <summary>
        /// Try to start the reading thread.
        /// </summary>
        /// <returns>True, if the thread could be successfully started.</returns>
        public bool TryReadingData()
        {
            try
            {
                if (!(TcpIsConnected = _client.Connected) || _threadRunningFlag)
                {
                    return false;
                }

                readingThread = new Thread(Reading)
                {
                    IsBackground = true
                };

                readingThread.Start();

                return true;
            }
            catch (Exception Ex)
            {
                throw Ex;
            }
        }

        /// <summary>
        /// Stops the reading thread.
        /// </summary>
        public void StopReadingData()
        {
            try
            {
                _threadRunningFlag = false;

                if (readingThread != null && readingThread.IsAlive)
                {
                    readingThread.Join();
                }
            }
            catch (Exception Ex)
            {
                throw Ex;
            }
        }

        private void Reading()
        {
            try
            {
                _threadRunningFlag = true;

                while (_threadRunningFlag)
                {
                    if ((TcpIsConnected = _client.Connected))
                    {
                        byte[] lengthBuffer = ReadBytes(sizeof(int));

                        if (lengthBuffer != null)
                        {
                            if (System.BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(lengthBuffer);
                            }

                            int length = System.BitConverter.ToInt32(lengthBuffer, 0);

                            byte[] receiveBuffer;

                            while ((receiveBuffer = ReadBytes(length)) == null && (TcpIsConnected = _client.Connected))
                            {
                            }

                            if (receiveBuffer != null)
                            {
                                _receivedDataQueue.Enqueue(Encoding.UTF8.GetString(receiveBuffer, 0, length));
                            }
                        }

                        Thread.Sleep(1); //for decreasing the CPU usage
                    }
                    else
                    {
                        _threadRunningFlag = false;
                    }
                }
            }
            catch (Exception Ex)
            {
                throw Ex;
            }
        }

        private byte[] ReadBytes(int count)
        {
            try
            {
                NetworkStream stream = _client?.GetStream();

                byte[] bytes = new byte[count];
                int readCount = 0;

                while (readCount < count)
                {
                    if (stream.DataAvailable)
                    {
                        int leftBytes = count - readCount;
                        int readBytes = stream.Read(bytes, readCount, leftBytes);

                        if (readBytes == 0)
                        {
                            return bytes = null;
                        }

                        readCount += readBytes;
                    }
                    else
                    {
                        return bytes = null;
                    }
                }
                return bytes;
            }
            catch (Exception Ex)
            {
                throw Ex;
            }
        }
    }
}