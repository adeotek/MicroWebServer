using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Adeotek.MicroWebServer.WebSocket
{
    public class WsSession : IWebSocketEvents
    {
        private Buffer _receiveBuffer;
        private SocketAsyncEventArgs _receiveEventArg;

        // Receive buffer
        private bool _receiving;
        private Buffer _sendBufferFlush;
        private long _sendBufferFlushOffset;
        private Buffer _sendBufferMain;
        private SocketAsyncEventArgs _sendEventArg;
        private bool _sending;
        // Send buffer
        private readonly object _sendLock = new object();

        internal readonly WebSocket WebSocket;

        // events
        public delegate void EmptyMessageDelegate(object sender, RawMessageEventArgs e);
        public delegate void MessageSentDelegate(object sender, MessageSentEventArgs e);
        public delegate void RawMessageReceivedDelegate(object sender, RawMessageEventArgs e);
        public delegate void SessionErrorDelegate(object sender, SocketErrorEventArgs e);

        public event SessionErrorDelegate OnSessionError;
        public event RawMessageReceivedDelegate OnMessageReceived;
        public event MessageSentDelegate OnMessageSent;
        public event EmptyMessageDelegate OnEmptyMessage;

        /// <summary>
        ///     Initialize the session with a given server
        /// </summary>
        /// <param name="server">TCP server</param>
        public WsSession(WsServer server)
        {
            Id = Guid.NewGuid();
            Server = server;
            OptionReceiveBufferSize = server.OptionReceiveBufferSize;
            OptionSendBufferSize = server.OptionSendBufferSize;
            Request = new Request();
            Response = new Response();
            WebSocket = new WebSocket(this);
        }

        /// <summary>
        ///     Session Id
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        ///     Server
        /// </summary>
        public WsServer Server { get; }

        /// <summary>
        ///     Socket
        /// </summary>
        public Socket Socket { get; protected set; }

        /// <summary>
        ///     Get the HTTP request
        /// </summary>
        protected Request Request { get; }

        /// <summary>
        ///     Get the HTTP response
        /// </summary>
        public Response Response { get; }

        /// <summary>
        ///     Number of bytes pending sent by the session
        /// </summary>
        public long BytesPending { get; protected set; }

        /// <summary>
        ///     Number of bytes sending by the session
        /// </summary>
        public long BytesSending { get; protected set; }

        /// <summary>
        ///     Number of bytes sent by the session
        /// </summary>
        public long BytesSent { get; protected set; }

        /// <summary>
        ///     Number of bytes received by the session
        /// </summary>
        public long BytesReceived { get; protected set; }

        /// <summary>
        ///     Option: receive buffer size
        /// </summary>
        public int OptionReceiveBufferSize { get; set; } = 8192;

        /// <summary>
        ///     Option: send buffer size
        /// </summary>
        public int OptionSendBufferSize { get; set; } = 8192;

        #region Error handling

        /// <summary>
        ///     Send error notification
        /// </summary>
        /// <param name="error">Socket error code</param>
        private void SendError(SocketError error)
        {
            // Skip disconnect errors
            if (error == SocketError.ConnectionAborted ||
                error == SocketError.ConnectionRefused ||
                error == SocketError.ConnectionReset ||
                error == SocketError.OperationAborted ||
                error == SocketError.Shutdown)
            {
                return;
            }

            OnWsError(error);
        }

        #endregion

        #region Connect/Disconnect session

        /// <summary>
        ///     Is the session connected?
        /// </summary>
        public bool IsConnected { get; protected set; }

        /// <summary>
        ///     Connect the session
        /// </summary>
        /// <param name="socket">Session socket</param>
        internal void Connect(Socket socket)
        {
            Socket = socket;

            // Update the session socket disposed flag
            IsSocketDisposed = false;

            // Setup buffers
            _receiveBuffer = new Buffer();
            _sendBufferMain = new Buffer();
            _sendBufferFlush = new Buffer();

            // Setup event args
            _receiveEventArg = new SocketAsyncEventArgs();
            _receiveEventArg.Completed += OnAsyncCompleted;
            _sendEventArg = new SocketAsyncEventArgs();
            _sendEventArg.Completed += OnAsyncCompleted;

            // Apply the option: keep alive
            if (Server.OptionKeepAlive)
            {
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
            // Apply the option: no delay
            if (Server.OptionNoDelay)
            {
                Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            }

            // Prepare receive & send buffers
            _receiveBuffer.Reserve(OptionReceiveBufferSize);
            _sendBufferMain.Reserve(OptionSendBufferSize);
            _sendBufferFlush.Reserve(OptionSendBufferSize);

            // Reset statistic
            BytesPending = 0;
            BytesSending = 0;
            BytesSent = 0;
            BytesReceived = 0;

            // Update the connected flag
            IsConnected = true;

            // Call the session connected handler
            OnConnected();

            // Call the session connected handler in the server
            Server.OnConnectedInternal(this);

            // Call the empty send buffer handler
            if (_sendBufferMain.IsEmpty)
            {
                OnWsEmpty();
            }

            // Try to receive something from the client
            TryReceive();
        }

        /// <summary>
        ///     Disconnect the session
        /// </summary>
        /// <returns>'true' if the section was successfully disconnected, 'false' if the section is already disconnected</returns>
        public virtual bool Disconnect()
        {
            if (!IsConnected)
            {
                return false;
            }

            // Reset event args
            _receiveEventArg.Completed -= OnAsyncCompleted;
            _sendEventArg.Completed -= OnAsyncCompleted;

            try
            {
                try
                {
                    // Shutdown the socket associated with the client
                    Socket.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                }

                // Close the session socket
                Socket.Close();

                // Dispose the session socket
                Socket.Dispose();

                // Update the session socket disposed flag
                IsSocketDisposed = true;
            }
            catch (ObjectDisposedException)
            {
            }

            // Update the connected flag
            IsConnected = false;

            // Update sending/receiving flags
            _receiving = false;
            _sending = false;

            // Clear send/receive buffers
            ClearBuffers();

            // Call the session disconnected handler
            OnDisconnected();

            // Call the session disconnected handler in the server
            Server.OnDisconnectedInternal(this);

            // Unregister session
            Server.UnregisterSession(Id);

            return true;
        }

        public virtual bool Close(int status)
        {
            SendCloseAsync(status, null, 0, 0);
            Disconnect();
            return true;
        }

        #endregion

        #region Send/Recieve data

        /// <summary>
        ///     Send data to the client (synchronous)
        /// </summary>
        /// <param name="buffer">Buffer to send</param>
        /// <returns>Size of sent data</returns>
        public virtual long Send(byte[] buffer)
        {
            return Send(buffer, 0, buffer.Length);
        }

        /// <summary>
        ///     Send data to the client (synchronous)
        /// </summary>
        /// <param name="buffer">Buffer to send</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="size">Buffer size</param>
        /// <returns>Size of sent data</returns>
        public virtual long Send(byte[] buffer, long offset, long size)
        {
            if (!IsConnected)
            {
                return 0;
            }

            if (size == 0)
            {
                return 0;
            }

            // Sent data to the client
            long sent = Socket.Send(buffer, (int)offset, (int)size, SocketFlags.None, out var ec);
            if (sent > 0)
            {
                // Update statistic
                BytesSent += sent;
                Interlocked.Add(ref Server._bytesSent, size);

                // Call the buffer sent handler
                OnWsSent(sent, BytesPending + BytesSending);
            }

            // Check for socket error
            if (ec != SocketError.Success)
            {
                SendError(ec);
                Disconnect();
            }

            return sent;
        }

        ///// <summary>
        /////     Send text to the client (synchronous)
        ///// </summary>
        ///// <param name="text">Text string to send</param>
        ///// <returns>Size of sent data</returns>
        //public virtual long Send(string text)
        //{
        //    return Send(Encoding.UTF8.GetBytes(text));
        //}

        /// <summary>
        ///     Send data to the client (asynchronous)
        /// </summary>
        /// <param name="buffer">Buffer to send</param>
        /// <returns>'true' if the data was successfully sent, 'false' if the session is not connected</returns>
        public virtual bool SendAsync(byte[] buffer)
        {
            return SendAsync(buffer, 0, buffer.Length);
        }

        /// <summary>
        ///     Send data to the client (asynchronous)
        /// </summary>
        /// <param name="buffer">Buffer to send</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="size">Buffer size</param>
        /// <returns>'true' if the data was successfully sent, 'false' if the session is not connected</returns>
        public virtual bool SendAsync(byte[] buffer, long offset, long size)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (size == 0)
            {
                return true;
            }

            lock (_sendLock)
            {
                // Detect multiple send handlers
                var sendRequired = _sendBufferMain.IsEmpty || _sendBufferFlush.IsEmpty;

                // Fill the main send buffer
                _sendBufferMain.Append(buffer, offset, size);

                // Update statistic
                BytesPending = _sendBufferMain.Size;

                // Avoid multiple send handlers
                if (!sendRequired)
                {
                    return true;
                }
            }

            // Try to send the main buffer
            Task.Factory.StartNew(TrySend);

            return true;
        }

        ///// <summary>
        /////     Send text to the client (asynchronous)
        ///// </summary>
        ///// <param name="text">Text string to send</param>
        ///// <returns>'true' if the text was successfully sent, 'false' if the session is not connected</returns>
        //public virtual bool SendAsync(string text)
        //{
        //    return SendAsync(Encoding.UTF8.GetBytes(text));
        //}

        /// <summary>
        ///     Receive data from the client (synchronous)
        /// </summary>
        /// <param name="buffer">Buffer to receive</param>
        /// <returns>Size of received data</returns>
        public virtual long Receive(byte[] buffer)
        {
            return Receive(buffer, 0, buffer.Length);
        }

        /// <summary>
        ///     Receive data from the client (synchronous)
        /// </summary>
        /// <param name="buffer">Buffer to receive</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="size">Buffer size</param>
        /// <returns>Size of received data</returns>
        public virtual long Receive(byte[] buffer, long offset, long size)
        {
            if (!IsConnected)
            {
                return 0;
            }

            if (size == 0)
            {
                return 0;
            }

            // Receive data from the client
            long received = Socket.Receive(buffer, (int)offset, (int)size, SocketFlags.None, out var ec);
            if (received > 0)
            {
                // Update statistic
                BytesReceived += received;
                Interlocked.Add(ref Server._bytesReceived, received);

                // Call the buffer received handler
                OnReceived(buffer, 0, received);
            }

            // Check for socket error
            if (ec != SocketError.Success)
            {
                SendError(ec);
                Disconnect();
            }

            return received;
        }

        ///// <summary>
        /////     Receive text from the client (synchronous)
        ///// </summary>
        ///// <param name="size">Text size to receive</param>
        ///// <returns>Received text</returns>
        //public virtual string Receive(long size)
        //{
        //    var buffer = new byte[size];
        //    var length = Receive(buffer);
        //    return Encoding.UTF8.GetString(buffer, 0, (int)length);
        //}

        ///// <summary>
        /////     Receive data from the client (asynchronous)
        ///// </summary>
        //public virtual void ReceiveAsync()
        //{
        //    // Try to receive data from the client
        //    TryReceive();
        //}

        /// <summary>
        ///     Try to receive new data
        /// </summary>
        private void TryReceive()
        {
            if (_receiving)
            {
                return;
            }

            if (!IsConnected)
            {
                return;
            }

            var process = true;

            while (process)
            {
                process = false;

                try
                {
                    // Async receive with the receive handler
                    _receiving = true;
                    _receiveEventArg.SetBuffer(_receiveBuffer.Data, 0, (int)_receiveBuffer.Capacity);
                    if (!Socket.ReceiveAsync(_receiveEventArg))
                    {
                        process = ProcessReceive(_receiveEventArg);
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        /// <summary>
        ///     Try to send pending data
        /// </summary>
        private void TrySend()
        {
            if (_sending)
            {
                return;
            }

            if (!IsConnected)
            {
                return;
            }

            var process = true;

            while (process)
            {
                process = false;

                lock (_sendLock)
                {
                    if (_sending)
                    {
                        return;
                    }

                    // Swap send buffers
                    if (_sendBufferFlush.IsEmpty)
                    {
                        // Swap flush and main buffers
                        _sendBufferFlush = Interlocked.Exchange(ref _sendBufferMain, _sendBufferFlush);
                        _sendBufferFlushOffset = 0;

                        // Update statistic
                        BytesPending = 0;
                        BytesSending += _sendBufferFlush.Size;

                        _sending = !_sendBufferFlush.IsEmpty;
                    }
                    else
                    {
                        return;
                    }
                }

                // Check if the flush buffer is empty
                if (_sendBufferFlush.IsEmpty)
                {
                    // Call the empty send buffer handler
                    OnWsEmpty();
                    return;
                }

                try
                {
                    // Async write with the write handler
                    _sendEventArg.SetBuffer(_sendBufferFlush.Data, (int)_sendBufferFlushOffset,
                        (int)(_sendBufferFlush.Size - _sendBufferFlushOffset));
                    if (!Socket.SendAsync(_sendEventArg))
                    {
                        process = ProcessSend(_sendEventArg);
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        /// <summary>
        ///     Clear send/receive buffers
        /// </summary>
        private void ClearBuffers()
        {
            lock (_sendLock)
            {
                // Clear send buffers
                _sendBufferMain.Clear();
                _sendBufferFlush.Clear();
                _sendBufferFlushOffset = 0;

                // Update statistic
                BytesPending = 0;
                BytesSending = 0;
            }
        }

        #endregion

        #region IO processing

        /// <summary>
        ///     This method is called whenever a receive or send operation is completed on a socket
        /// </summary>
        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            // Determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    if (ProcessReceive(e))
                    {
                        TryReceive();
                    }

                    break;
                case SocketAsyncOperation.Send:
                    if (ProcessSend(e))
                    {
                        TrySend();
                    }

                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        /// <summary>
        ///     This method is invoked when an asynchronous receive operation completes
        /// </summary>
        private bool ProcessReceive(SocketAsyncEventArgs e)
        {
            if (!IsConnected)
            {
                return false;
            }

            long size = e.BytesTransferred;

            // Received some data from the client
            if (size > 0)
            {
                // Update statistic
                BytesReceived += size;
                Interlocked.Add(ref Server._bytesReceived, size);

                // Call the buffer received handler
                OnReceived(_receiveBuffer.Data, 0, size);

                // If the receive buffer is full increase its size
                if (_receiveBuffer.Capacity == size)
                {
                    _receiveBuffer.Reserve(2 * size);
                }
            }

            _receiving = false;

            // Try to receive again if the session is valid
            if (e.SocketError == SocketError.Success)
            {
                // If zero is returned from a read operation, the remote end has closed the connection
                if (size > 0)
                {
                    return true;
                }

                Disconnect();
            }
            else
            {
                SendError(e.SocketError);
                Disconnect();
            }

            return false;
        }

        /// <summary>
        ///     This method is invoked when an asynchronous send operation completes
        /// </summary>
        private bool ProcessSend(SocketAsyncEventArgs e)
        {
            if (!IsConnected)
            {
                return false;
            }

            long size = e.BytesTransferred;

            // Send some data to the client
            if (size > 0)
            {
                // Update statistic
                BytesSending -= size;
                BytesSent += size;
                Interlocked.Add(ref Server._bytesSent, size);

                // Increase the flush buffer offset
                _sendBufferFlushOffset += size;

                // Successfully send the whole flush buffer
                if (_sendBufferFlushOffset == _sendBufferFlush.Size)
                {
                    // Clear the flush buffer
                    _sendBufferFlush.Clear();
                    _sendBufferFlushOffset = 0;
                }

                // Call the buffer sent handler
                OnWsSent(size, BytesPending + BytesSending);
            }

            _sending = false;

            // Try to send again if the session is valid
            if (e.SocketError == SocketError.Success)
            {
                return true;
            }

            SendError(e.SocketError);
            Disconnect();
            return false;
        }

        #endregion

        #region Send response / Send response body

        /// <summary>
        ///     Send WebSocket server upgrade response
        /// </summary>
        /// <param name="response">WebSocket upgrade HTTP response</param>
        public void SendResponse(Response response)
        {
            SendResponseAsync(response);
        }

        /// <summary>
        ///     Send the HTTP response (asynchronous)
        /// </summary>
        /// <param name="response">HTTP response</param>
        /// <returns>'true' if the current HTTP response was successfully sent, 'false' if the session is not connected</returns>
        public bool SendResponseAsync(Response response)
        {
            return SendAsync(response.Cache.Data, response.Cache.Offset, response.Cache.Size);
        }

        #endregion

        #region WebSocket send text methods

        public long SendText(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_TEXT, false, buffer, offset, size);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public long SendText(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_TEXT, false, data, 0, data.Length);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendTextAsync(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_TEXT, false, buffer, offset, size);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendTextAsync(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_TEXT, false, data, 0, data.Length);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        #region WebSocket send binary methods

        public long SendBinary(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_BINARY, false, buffer, offset, size);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public long SendBinary(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_BINARY, false, data, 0, data.Length);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendBinaryAsync(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_BINARY, false, buffer, offset, size);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendBinaryAsync(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_BINARY, false, data, 0, data.Length);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        #region WebSocket send close methods

        public long SendClose(int status, byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_CLOSE, false, buffer, offset, size, status);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public long SendClose(int status, string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_CLOSE, false, data, 0, data.Length, status);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendCloseAsync(int status, byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_CLOSE, false, buffer, offset, size, status);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendCloseAsync(int status, string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_CLOSE, false, data, 0, data.Length, status);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        #region WebSocket send ping methods

        public long SendPing(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PING, false, buffer, offset, size);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public long SendPing(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PING, false, data, 0, data.Length);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendPingAsync(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PING, false, buffer, offset, size);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendPingAsync(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PING, true, data, 0, data.Length);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        #region WebSocket send pong methods

        public long SendPong(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PONG, false, buffer, offset, size);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public long SendPong(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PONG, false, data, 0, data.Length);
                return Send(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendPongAsync(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PONG, false, buffer, offset, size);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendPongAsync(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PONG, false, data, 0, data.Length);
                return SendAsync(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        #region WebSocket receive methods

        public string ReceiveText()
        {
            var result = new Buffer();

            if (!WebSocket.WsHandshaked)
            {
                return result.ExtractString(0, result.Data.Length);
            }

            var cache = new Buffer();

            // Receive WebSocket frame data
            while (!WebSocket.WsReceived)
            {
                var required = WebSocket.RequiredReceiveFrameSize();
                cache.Resize(required);
                var received = (int)Receive(cache.Data, 0, required);
                if (received != required)
                {
                    return result.ExtractString(0, result.Data.Length);
                }

                WebSocket.PrepareReceiveFrame(cache.Data, 0, received);
            }

            // Copy WebSocket frame data
            result.Append(WebSocket.WsReceiveBuffer.ToArray(), WebSocket.WsHeaderSize,
                WebSocket.WsHeaderSize + WebSocket.WsPayloadSize);
            WebSocket.PrepareReceiveFrame(null, 0, 0);
            return result.ExtractString(0, result.Data.Length);
        }

        public Buffer ReceiveBinary()
        {
            var result = new Buffer();

            if (!WebSocket.WsHandshaked)
            {
                return result;
            }

            var cache = new Buffer();

            // Receive WebSocket frame data
            while (!WebSocket.WsReceived)
            {
                var required = WebSocket.RequiredReceiveFrameSize();
                cache.Resize(required);
                var received = (int)Receive(cache.Data, 0, required);
                if (received != required)
                {
                    return result;
                }

                WebSocket.PrepareReceiveFrame(cache.Data, 0, received);
            }

            // Copy WebSocket frame data
            result.Append(WebSocket.WsReceiveBuffer.ToArray(), WebSocket.WsHeaderSize,
                WebSocket.WsHeaderSize + WebSocket.WsPayloadSize);
            WebSocket.PrepareReceiveFrame(null, 0, 0);
            return result;
        }

        #endregion

        #region Session handlers/events

        private void OnReceivedRequestInternal(Request request)
        {
            // Process the request
            OnReceivedRequest(request);
        }

        protected virtual void OnReceivedRequest(Request request)
        {
            // Check for WebSocket handshaked status
            if (WebSocket.WsHandshaked)
            {
                // Prepare receive frame from the remaining request body
                var body = Request.Body;
                var data = Encoding.UTF8.GetBytes(body);
                WebSocket.PrepareReceiveFrame(data, 0, data.Length);
            }
        }

        /// <summary>
        ///     Handle client connected notification
        /// </summary>
        protected virtual void OnConnected()
        {
        }

        /// <summary>
        ///     Handle client disconnected notification
        /// </summary>
        protected virtual void OnDisconnected()
        {
            // Disconnect WebSocket
            if (WebSocket.WsHandshaked)
            {
                WebSocket.WsHandshaked = false;
                OnWsDisconnected();
            }

            // Reset WebSocket upgrade HTTP request and response
            Request.Clear();
            Response.Clear();

            // Clear WebSocket send/receive buffers
            WebSocket.ClearWsBuffers();
        }

        /// <summary>
        ///     Handle HTTP request header received notification
        /// </summary>
        /// <remarks>Notification is called when HTTP request header was received from the client.</remarks>
        /// <param name="request">HTTP request</param>
        protected virtual void OnReceivedRequestHeader(Request request)
        {
            // Check for WebSocket handshaked status
            if (WebSocket.WsHandshaked)
            {
                return;
            }

            // Try to perform WebSocket upgrade
            WebSocket.PerformServerUpgrade(request, Response);
        }

        /// <summary>
        ///     Handle HTTP request error notification
        /// </summary>
        /// <remarks>Notification is called when HTTP request error was received from the client.</remarks>
        /// <param name="request">HTTP request</param>
        /// <param name="error">HTTP request error</param>
        protected virtual void OnReceivedRequestError(Request request, string error)
        {
            // Check for WebSocket handshaked status
            if (WebSocket.WsHandshaked)
            {
                OnWsError(new SocketError());
            }
        }

        /// <summary>
        ///     Handle buffer received notification
        /// </summary>
        /// <param name="buffer">Received buffer</param>
        /// <param name="offset">Received buffer offset</param>
        /// <param name="size">Received buffer size</param>
        /// <remarks>
        ///     Notification is called when another chunk of buffer was received from the client
        /// </remarks>
        protected virtual void OnReceived(byte[] buffer, long offset, long size)
        {
            // Check for WebSocket handshaked status
            if (WebSocket.WsHandshaked)
            {
                // Prepare receive frame
                WebSocket.PrepareReceiveFrame(buffer, offset, size);
                return;
            }

            // Receive HTTP request header
            if (Request.IsPendingHeader())
            {
                if (Request.ReceiveHeader(buffer, (int)offset, (int)size))
                {
                    OnReceivedRequestHeader(Request);
                }

                size = 0;
            }

            // Check for HTTP request error
            if (Request.IsErrorSet)
            {
                OnReceivedRequestError(Request, "Invalid HTTP request!");
                Request.Clear();
                Disconnect();
                return;
            }

            // Receive HTTP request body
            if (Request.ReceiveBody(buffer, (int)offset, (int)size))
            {
                OnReceivedRequestInternal(Request);
                Request.Clear();
                return;
            }

            // Check for HTTP request error
            if (Request.IsErrorSet)
            {
                OnReceivedRequestError(Request, "Invalid HTTP request!");
                Request.Clear();
                Disconnect();
            }
        }

        #endregion

        #region Web socket handlers

        /// <summary>
        ///     Handle WebSocket client connecting notification
        /// </summary>
        /// <remarks>
        ///     Notification is called when WebSocket client is connecting to the server.You can handle the connection and
        ///     change WebSocket upgrade HTTP request by providing your own headers.
        /// </remarks>
        /// <param name="request">WebSocket upgrade HTTP request</param>
        public virtual void OnWsConnecting(Request request)
        {
        }

        /// <summary>
        ///     Handle WebSocket client connected notification
        /// </summary>
        /// <param name="response">WebSocket upgrade HTTP response</param>
        public virtual void OnWsConnected(Response response)
        {
        }

        /// <summary>
        ///     Handle WebSocket server session validating notification
        /// </summary>
        /// <remarks>
        ///     Notification is called when WebSocket client is connecting to the server.You can handle the connection and
        ///     validate WebSocket upgrade HTTP request.
        /// </remarks>
        /// <param name="request">WebSocket upgrade HTTP request</param>
        /// <param name="response">WebSocket upgrade HTTP response</param>
        /// <returns>return 'true' if the WebSocket update request is valid, 'false' if the WebSocket update request is not valid</returns>
        public virtual bool OnWsConnecting(Request request, Response response)
        {
            return true;
        }

        /// <summary>
        ///     Handle WebSocket server session connected notification
        /// </summary>
        /// <param name="request">WebSocket upgrade HTTP request</param>
        public virtual void OnWsConnected(Request request)
        {
        }

        /// <summary>
        ///     Handle WebSocket client disconnected notification
        /// </summary>
        public virtual void OnWsDisconnected()
        {
        }

        /// <summary>
        ///     Handle buffer sent notification
        /// </summary>
        /// <param name="sent">Size of sent buffer</param>
        /// <param name="pending">Size of pending buffer</param>
        /// <remarks>
        ///     Notification is called when another chunk of buffer was sent to the client.
        ///     This handler could be used to send another buffer to the client for instance when the pending size is zero.
        /// </remarks>
        public virtual void OnWsSent(long sent, long pending)
        {
            OnMessageSent?.Invoke(this, new MessageSentEventArgs(Id, sent, pending));
        }

        /// <summary>
        ///     Handle empty send buffer notification
        /// </summary>
        /// <remarks>
        ///     Notification is called when the send buffer is empty and ready for a new data to send.
        ///     This handler could be used to send another buffer to the client.
        /// </remarks>
        public virtual void OnWsEmpty()
        {
            OnEmptyMessage?.Invoke(this, new RawMessageEventArgs(Id, null, 0, 0));
        }

        /// <summary>
        ///     Handle WebSocket received notification
        /// </summary>
        /// <param name="buffer">Received buffer</param>
        /// <param name="offset">Received buffer offset</param>
        /// <param name="size">Received buffer size</param>
        public virtual void OnWsReceived(byte[] buffer, long offset, long size)
        {
            OnMessageReceived?.Invoke(this, new RawMessageEventArgs(Id, buffer, offset, size));
        }

        /// <summary>
        ///     Handle WebSocket client close notification
        /// </summary>
        /// <param name="buffer">Received buffer</param>
        /// <param name="offset">Received buffer offset</param>
        /// <param name="size">Received buffer size</param>
        public virtual void OnWsClose(byte[] buffer, long offset, long size)
        {
            Close(1000);
        }

        /// <summary>
        ///     Handle WebSocket ping notification
        /// </summary>
        /// <param name="buffer">Received buffer</param>
        /// <param name="offset">Received buffer offset</param>
        /// <param name="size">Received buffer size</param>
        public virtual void OnWsPing(byte[] buffer, long offset, long size)
        {
            SendPongAsync(buffer, offset, size);
        }

        /// <summary>
        ///     Handle WebSocket pong notification
        /// </summary>
        /// <param name="buffer">Received buffer</param>
        /// <param name="offset">Received buffer offset</param>
        /// <param name="size">Received buffer size</param>
        public virtual void OnWsPong(byte[] buffer, long offset, long size)
        {
        }

        /// <summary>
        ///     Handle WebSocket error notification
        /// </summary>
        /// <param name="error">Error message</param>
        public virtual void OnWsError(string error)
        {
            OnSessionError?.Invoke(this, new SocketErrorEventArgs(Id, SocketError.SocketError, error));
        }

        /// <summary>
        ///     Handle socket error notification
        /// </summary>
        /// <param name="error">Socket error</param>
        public virtual void OnWsError(SocketError error)
        {
            OnSessionError?.Invoke(this, new SocketErrorEventArgs(Id, SocketError.SocketError));
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        ///     Disposed flag
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        ///     Session socket disposed flag
        /// </summary>
        public bool IsSocketDisposed { get; private set; } = true;

        // Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposingManagedResources)
        {
            // The idea here is that Dispose(Boolean) knows whether it is
            // being called to do explicit cleanup (the Boolean is true)
            // versus being called due to a garbage collection (the Boolean
            // is false). This distinction is useful because, when being
            // disposed explicitly, the Dispose(Boolean) method can safely
            // execute code using reference type fields that refer to other
            // objects knowing for sure that these other objects have not been
            // finalized or disposed of yet. When the Boolean is false,
            // the Dispose(Boolean) method should not execute code that
            // refer to reference type fields because those objects may
            // have already been finalized."

            if (!IsDisposed)
            {
                if (disposingManagedResources)
                {
                    // Dispose managed resources here...
                    Disconnect();
                }

                // Dispose unmanaged resources here...

                // Set large fields to null here...

                // Mark as disposed.
                IsDisposed = true;
            }
        }

        // Use C# destructor syntax for finalization code.
        ~WsSession()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }

        #endregion
    }
}