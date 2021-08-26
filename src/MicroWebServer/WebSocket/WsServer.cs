﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Adeotek.MicroWebServer.WebSocket
{
    public class WsServer : IWebSocketEvents
    {
        // Server sessions
        protected readonly ConcurrentDictionary<Guid, WsSession> Sessions = new ConcurrentDictionary<Guid, WsSession>();
        protected SocketAsyncEventArgs _acceptorEventArg;
        // Server acceptor
        protected Socket _acceptorSocket;

        internal readonly WebSocket WebSocket;
        // Server statistic
        internal long _bytesPending;
        internal long _bytesReceived;
        internal long _bytesSent;

        // events
        public delegate void SessionConnectedDelegate(object sender, ConnectionEventArgs e);
        public delegate void SessionDisconnectedDelegate(object sender, ConnectionEventArgs e);
        public delegate void ServerSocketErrorDelegate(object sender, SocketErrorEventArgs e);
        public delegate void ServerStartedDelegate(object sender, ServerStateEventArgs e);
        public delegate void ServerStoppedDelegate(object sender, ServerStateEventArgs e);

        public event SessionConnectedDelegate OnSessionConnected;
        public event SessionDisconnectedDelegate OnSessionDisconnected;
        public event ServerStartedDelegate OnServerStarted;
        public event ServerStoppedDelegate OnServerStopped;
        public event ServerSocketErrorDelegate OnServerError;

        public event WsSession.SessionErrorDelegate OnSessionError;
        public event WsSession.RawMessageReceivedDelegate OnMessageReceived;
        public event WsSession.MessageSentDelegate OnMessageSent;
        public event WsSession.EmptyMessageDelegate OnEmptyMessage;

        /// <summary>
        ///     Initialize WebSocket server with a given IP address and port number
        /// </summary>
        /// <param name="address">IP address</param>
        /// <param name="port">Port number</param>
        public WsServer(IPAddress address, int port) : this(new IPEndPoint(address, port))
        {
        }

        /// <summary>
        ///     Initialize WebSocket server with a given IP address and port number
        /// </summary>
        /// <param name="address">IP address</param>
        /// <param name="port">Port number</param>
        public WsServer(string address, int port) : this(new IPEndPoint(IPAddress.Parse(address), port))
        {
        }

        /// <summary>
        ///     Initialize WebSocket server with a given IP endpoint
        /// </summary>
        /// <param name="endpoint">IP endpoint</param>
        public WsServer(IPEndPoint endpoint)
        {
            Id = Guid.NewGuid();
            Endpoint = endpoint;
            WebSocket = new WebSocket(this);
        }

        /// <summary>
        ///     Server Id
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        ///     IP endpoint
        /// </summary>
        public IPEndPoint Endpoint { get; protected set; }

        /// <summary>
        ///     Number of sessions connected to the server
        /// </summary>
        public long ConnectedSessions => Sessions.Count;

        /// <summary>
        ///     Number of bytes pending sent by the server
        /// </summary>
        public long BytesPending => _bytesPending;

        /// <summary>
        ///     Number of bytes sent by the server
        /// </summary>
        public long BytesSent => _bytesSent;

        /// <summary>
        ///     Number of bytes received by the server
        /// </summary>
        public long BytesReceived => _bytesReceived;

        /// <summary>
        ///     Option: acceptor backlog size
        /// </summary>
        /// <remarks>
        ///     This option will set the listening socket's backlog size
        /// </remarks>
        public int OptionAcceptorBacklog { get; set; } = 1024;

        /// <summary>
        ///     Option: dual mode socket
        /// </summary>
        /// <remarks>
        ///     Specifies whether the Socket is a dual-mode socket used for both IPv4 and IPv6.
        ///     Will work only if socket is bound on IPv6 address.
        /// </remarks>
        public bool OptionDualMode { get; set; }

        /// <summary>
        ///     Option: keep alive
        /// </summary>
        /// <remarks>
        ///     This option will setup SO_KEEPALIVE if the OS support this feature
        /// </remarks>
        public bool OptionKeepAlive { get; set; }

        /// <summary>
        ///     Option: no delay
        /// </summary>
        /// <remarks>
        ///     This option will enable/disable Nagle's algorithm for TCP protocol
        /// </remarks>
        public bool OptionNoDelay { get; set; }

        /// <summary>
        ///     Option: reuse address
        /// </summary>
        /// <remarks>
        ///     This option will enable/disable SO_REUSEADDR if the OS support this feature
        /// </remarks>
        public bool OptionReuseAddress { get; set; }

        /// <summary>
        ///     Option: enables a socket to be bound for exclusive access
        /// </summary>
        /// <remarks>
        ///     This option will enable/disable SO_EXCLUSIVEADDRUSE if the OS support this feature
        /// </remarks>
        public bool OptionExclusiveAddressUse { get; set; }

        /// <summary>
        ///     Option: receive buffer size
        /// </summary>
        public int OptionReceiveBufferSize { get; set; } = 8192;

        /// <summary>
        ///     Option: send buffer size
        /// </summary>
        public int OptionSendBufferSize { get; set; } = 8192;

        #region Session factory

        /// <summary>
        ///     Create TCP session factory method
        /// </summary>
        /// <returns>TCP session</returns>
        protected virtual WsSession CreateSession()
        {
            var newSession = new WsSession(this);

            if (OnSessionError != null)
            {
                newSession.OnSessionError += OnSessionError;
            }

            if (OnMessageSent != null)
            {
                newSession.OnMessageSent += OnMessageSent;
            }

            if (OnMessageReceived != null)
            {
                newSession.OnMessageReceived += OnMessageReceived;
            }

            if (OnEmptyMessage != null)
            {
                newSession.OnEmptyMessage += OnEmptyMessage;
            }

            return newSession;
        }

        #endregion

        #region Error handling

        /// <summary>
        ///     Send error notification
        /// </summary>
        /// <param name="error">Socket error code</param>
        protected void SendError(SocketError error)
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

            OnError(error);
        }

        #endregion

        #region Start/Stop server

        /// <summary>
        ///     Is the server started?
        /// </summary>
        public bool IsStarted { get; protected set; }

        /// <summary>
        ///     Is the server accepting new clients?
        /// </summary>
        public bool IsAccepting { get; protected set; }

        /// <summary>
        ///     Start the server
        /// </summary>
        /// <returns>'true' if the server was successfully started, 'false' if the server failed to start</returns>
        public virtual bool Start()
        {
            Debug.Assert(!IsStarted, "TCP server is already started!");
            if (IsStarted)
            {
                return false;
            }

            // Setup acceptor event arg
            _acceptorEventArg = new SocketAsyncEventArgs();
            _acceptorEventArg.Completed += OnAsyncCompleted;

            // Create a new acceptor socket
            _acceptorSocket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Update the acceptor socket disposed flag
            IsSocketDisposed = false;

            // Apply the option: reuse address
            _acceptorSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress,
                OptionReuseAddress);
            // Apply the option: exclusive address use
            _acceptorSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse,
                OptionExclusiveAddressUse);
            // Apply the option: dual mode (this option must be applied before listening)
            if (_acceptorSocket.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _acceptorSocket.DualMode = OptionDualMode;
            }

            // Bind the acceptor socket to the IP endpoint
            _acceptorSocket.Bind(Endpoint);
            // Refresh the endpoint property based on the actual endpoint created
            Endpoint = (IPEndPoint)_acceptorSocket.LocalEndPoint;
            // Start listen to the acceptor socket with the given accepting backlog size
            _acceptorSocket.Listen(OptionAcceptorBacklog);

            // Reset statistic
            _bytesPending = 0;
            _bytesSent = 0;
            _bytesReceived = 0;

            // Update the started flag
            IsStarted = true;

            // Call the server started handler
            OnStarted();

            // Perform the first server accept
            IsAccepting = true;
            StartAccept(_acceptorEventArg);

            return true;
        }

        /// <summary>
        ///     Stop the server
        /// </summary>
        /// <returns>'true' if the server was successfully stopped, 'false' if the server is already stopped</returns>
        public virtual bool Stop()
        {
            Debug.Assert(IsStarted, "TCP server is not started!");
            if (!IsStarted)
            {
                return false;
            }

            // Stop accepting new clients
            IsAccepting = false;

            // Reset acceptor event arg
            _acceptorEventArg.Completed -= OnAsyncCompleted;

            // Close the acceptor socket
            _acceptorSocket.Close();

            // Dispose the acceptor socket
            _acceptorSocket.Dispose();

            // Update the acceptor socket disposed flag
            IsSocketDisposed = true;

            // Disconnect all sessions
            DisconnectAll();

            // Update the started flag
            IsStarted = false;

            // Call the server stopped handler
            OnStopped();

            return true;
        }

        /// <summary>
        ///     Restart the server
        /// </summary>
        /// <returns>'true' if the server was successfully restarted, 'false' if the server failed to restart</returns>
        public virtual bool Restart()
        {
            if (!Stop())
            {
                return false;
            }

            while (IsStarted)
            {
                Thread.Yield();
            }

            return Start();
        }

        public virtual bool CloseAll(int status)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_CLOSE, false, null, 0, 0, status);
                return BroadcastData(WebSocket.WsSendBuffer.ToArray()) && DisconnectAll();
            }
        }

        #endregion

        #region Accepting clients

        /// <summary>
        ///     Start accept a new client connection
        /// </summary>
        protected void StartAccept(SocketAsyncEventArgs e)
        {
            // Socket must be cleared since the context object is being reused
            e.AcceptSocket = null;

            // Async accept a new client connection
            if (!_acceptorSocket.AcceptAsync(e))
            {
                ProcessAccept(e);
            }
        }

        /// <summary>
        ///     Process accepted client connection
        /// </summary>
        protected void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // Create a new session to register
                var session = CreateSession();

                // Register the session
                RegisterSession(session);

                // Connect new session
                session.Connect(e.AcceptSocket);
            }
            else
            {
                SendError(e.SocketError);
            }

            // Accept the next client connection
            if (IsAccepting)
            {
                StartAccept(e);
            }
        }

        /// <summary>
        ///     This method is the callback method associated with Socket.AcceptAsync()
        ///     operations and is invoked when an accept operation is complete
        /// </summary>
        protected void OnAsyncCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        #endregion

        #region Session management

        /// <summary>
        ///     Disconnect all connected sessions
        /// </summary>
        /// <returns>'true' if all sessions were successfully disconnected, 'false' if the server is not started</returns>
        public virtual bool DisconnectAll()
        {
            if (!IsStarted)
            {
                return false;
            }

            // Disconnect all sessions
            foreach (var session in Sessions.Values)
            {
                session.Disconnect();
            }

            return true;
        }

        /// <summary>
        ///     Find a session with a given Id
        /// </summary>
        /// <param name="id">Session Id</param>
        /// <returns>Session with a given Id or null if the session it not connected</returns>
        public WsSession FindSession(Guid id)
        {
            // Try to find the required session
            return Sessions.TryGetValue(id, out var result) ? result : null;
        }

        /// <summary>
        ///     Register a new session
        /// </summary>
        /// <param name="session">Session to register</param>
        internal void RegisterSession(WsSession session)
        {
            // Register a new session
            Sessions.TryAdd(session.Id, session);
        }

        /// <summary>
        ///     Unregister session by Id
        /// </summary>
        /// <param name="id">Session Id</param>
        internal void UnregisterSession(Guid id)
        {
            // Unregister session by Id
            Sessions.TryRemove(id, out var temp);
        }

        #endregion

        #region Broadcasting

        /// <summary>
        ///     Broadcast data to all connected sessions
        /// </summary>
        /// <param name="buffer">Buffer to broadcast</param>
        /// <returns>'true' if the data was successfully broadcasted, 'false' if the data was not broadcasted</returns>
        internal virtual bool BroadcastData(byte[] buffer)
        {
            return BroadcastData(buffer, 0, buffer.Length);
        }

        /// <summary>
        ///     Broadcast data to all connected clients
        /// </summary>
        /// <param name="buffer">Buffer to broadcast</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="size">Buffer size</param>
        /// <returns>'true' if the data was successfully broadcasted, 'false' if the data was not broadcasted</returns>
        internal virtual bool BroadcastData(byte[] buffer, long offset, long size)
        {
            if (!IsStarted)
            {
                return false;
            }

            if (size == 0)
            {
                return true;
            }

            // Broadcast data to all WebSocket sessions
            foreach (var session in Sessions.Values)
            {
                if (!(session is WsSession wsSession))
                {
                    continue;
                }

                if (wsSession.WebSocket.WsHandshaked)
                {
                    wsSession.SendDataAsync(buffer, offset, size);
                }
            }

            return true;
        }

        /// <summary>
        ///     Broadcast to all connected clients
        /// </summary>
        /// <param name="text">Text string to broadcast</param>
        /// <returns>'true' if the text was successfully broadcasted, 'false' if the text was not broadcasted</returns>
        public bool Broadcast(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_TEXT, false, buffer, offset, size);
                return BroadcastData(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool Broadcast(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_TEXT, false, data, 0, data.Length);
                return BroadcastData(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool BroadcastBinary(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_BINARY, false, buffer, offset, size);
                return BroadcastData(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool BroadcastBinary(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_BINARY, false, data, 0, data.Length);
                return BroadcastData(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        #region WebSocket Broadcast ping/pong methods

        public bool SendPing(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PING, false, buffer, offset, size);
                return BroadcastData(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendPing(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PING, false, data, 0, data.Length);
                return BroadcastData(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendPong(byte[] buffer, long offset, long size)
        {
            lock (WebSocket.WsSendLock)
            {
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PONG, false, buffer, offset, size);
                return BroadcastData(WebSocket.WsSendBuffer.ToArray());
            }
        }

        public bool SendPong(string text)
        {
            lock (WebSocket.WsSendLock)
            {
                var data = Encoding.UTF8.GetBytes(text);
                WebSocket.PrepareSendFrame(WebSocket.WS_FIN | WebSocket.WS_PONG, false, data, 0, data.Length);
                return BroadcastData(WebSocket.WsSendBuffer.ToArray());
            }
        }

        #endregion

        #region Server handlers/events

        /// <summary>
        ///     Handle server started notification
        /// </summary>
        protected virtual void OnStarted()
        {
            OnServerStarted?.Invoke(this, new ServerStateEventArgs(Id));
        }

        /// <summary>
        ///     Handle server stopped notification
        /// </summary>
        protected virtual void OnStopped()
        {
            OnServerStopped?.Invoke(this, new ServerStateEventArgs(Id));
        }

        /// <summary>
        ///     Handle session connected notification
        /// </summary>
        /// <param name="session">Connected session</param>
        protected virtual void OnConnected(WsSession session)
        {
            OnSessionConnected?.Invoke(session, new ConnectionEventArgs(session.Id));
        }

        /// <summary>
        ///     Handle session disconnected notification
        /// </summary>
        /// <param name="session">Disconnected session</param>
        protected virtual void OnDisconnected(WsSession session)
        {
            OnSessionDisconnected?.Invoke(session, new ConnectionEventArgs(session.Id));
        }

        /// <summary>
        ///     Handle error notification
        /// </summary>
        /// <param name="error">Socket error code</param>
        protected virtual void OnError(SocketError error)
        {
            OnServerError?.Invoke(this, new SocketErrorEventArgs(Guid.Empty, error));
        }

        internal void OnConnectedInternal(WsSession session)
        {
            OnConnected(session);
        }

        internal void OnDisconnectedInternal(WsSession session)
        {
            OnDisconnected(session);
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        ///     Disposed flag
        /// </summary>
        public bool IsDisposed { get; protected set; }

        /// <summary>
        ///     Acceptor socket disposed flag
        /// </summary>
        public bool IsSocketDisposed { get; protected set; } = true;

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

            if (IsDisposed)
            {
                return;
            }

            if (disposingManagedResources)
            {
                // Dispose managed resources here...
                Stop();
            }

            // Dispose unmanaged resources here...

            // Set large fields to null here...

            // Mark as disposed.
            IsDisposed = true;
        }

        // Use C# destructor syntax for finalization code.
        ~WsServer()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }

        #endregion
    }
}