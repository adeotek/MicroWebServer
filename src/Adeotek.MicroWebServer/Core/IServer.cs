using System;
using System.Net;

namespace Adeotek.MicroWebServer.Core
{
    public interface IServer
    {
        delegate void ServerStartedDelegate(object sender, ServerStateEventArgs e);
        delegate void ServerStoppedDelegate(object sender, ServerStateEventArgs e);
        //delegate void SessionConnectedDelegate(object sender, ConnectionEventArgs e);
        //delegate void SessionDisconnectedDelegate(object sender, ConnectionEventArgs e);
        //delegate void SessionErrorDelegate(object sender, SocketErrorEventArgs e);
        //delegate void RawMessageReceivedDelegate(object sender, RawMessageEventArgs e);
        event ServerStartedDelegate OnServerStarted;
        event ServerStoppedDelegate OnServerStopped;
        event ISession.SessionConnectedDelegate OnSessionConnected;
        event ISession.SessionDisconnectedDelegate OnSessionDisconnected;
        event ISession.SessionErrorDelegate OnSocketError;
        event ISession.RawMessageReceivedDelegate OnMessageReceived;

        /// <summary>
        /// Server Id
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// IP endpoint
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// Number of sessions connected to the server
        /// </summary>
        long ConnectedSessions { get; }

        /// <summary>
        /// Number of bytes pending sent by the server
        /// </summary>
        long BytesPending { get; }

        /// <summary>
        /// Number of bytes sent by the server
        /// </summary>
        long BytesSent { get; }

        /// <summary>
        /// Number of bytes received by the server
        /// </summary>
        long BytesReceived { get; }

        /// <summary>
        /// Option: acceptor backlog size
        /// </summary>
        /// <remarks>
        /// This option will set the listening socket's backlog size
        /// </remarks>
        int OptionAcceptorBacklog { get; set; }
        /// <summary>
        /// Option: dual mode socket
        /// </summary>
        /// <remarks>
        /// Specifies whether the Socket is a dual-mode socket used for both IPv4 and IPv6.
        /// Will work only if socket is bound on IPv6 address.
        /// </remarks>
        bool OptionDualMode { get; set; }
        /// <summary>
        /// Option: keep alive
        /// </summary>
        /// <remarks>
        /// This option will setup SO_KEEPALIVE if the OS support this feature
        /// </remarks>
        bool OptionKeepAlive { get; set; }
        /// <summary>
        /// Option: no delay
        /// </summary>
        /// <remarks>
        /// This option will enable/disable Nagle's algorithm for TCP protocol
        /// </remarks>
        bool OptionNoDelay { get; set; }
        /// <summary>
        /// Option: reuse address
        /// </summary>
        /// <remarks>
        /// This option will enable/disable SO_REUSEADDR if the OS support this feature
        /// </remarks>
        bool OptionReuseAddress { get; set; }
        /// <summary>
        /// Option: enables a socket to be bound for exclusive access
        /// </summary>
        /// <remarks>
        /// This option will enable/disable SO_EXCLUSIVEADDRUSE if the OS support this feature
        /// </remarks>
        bool OptionExclusiveAddressUse { get; set; }
        /// <summary>
        /// Option: receive buffer size
        /// </summary>
        int OptionReceiveBufferSize { get; set; }
        /// <summary>
        /// Option: send buffer size
        /// </summary>
        int OptionSendBufferSize { get; set; }

        /// <summary>
        /// Is the server started?
        /// </summary>
        bool IsStarted { get; }
        /// <summary>
        /// Is the server accepting new clients?
        /// </summary>
        bool IsAccepting { get; }

        /// <summary>
        /// Start the server
        /// </summary>
        /// <returns>'true' if the server was successfully started, 'false' if the server failed to start</returns>
        bool Start();

        /// <summary>
        /// Stop the server
        /// </summary>
        /// <returns>'true' if the server was successfully stopped, 'false' if the server is already stopped</returns>
        bool Stop();

        /// <summary>
        /// Restart the server
        /// </summary>
        /// <returns>'true' if the server was successfully restarted, 'false' if the server failed to restart</returns>
        bool Restart();

        /// <summary>
        /// Disconnect all connected sessions
        /// </summary>
        /// <returns>'true' if all sessions were successfully disconnected, 'false' if the server is not started</returns>
        bool DisconnectAll();

        /// <summary>
        /// Find a session with a given Id
        /// </summary>
        /// <param name="sessionId">Session Id</param>
        /// <returns>Session with a given Id or null if the session it not connected</returns>
        ISession FindSession(Guid sessionId);

        /// <summary>
        /// Broadcast text to all connected clients
        /// </summary>
        /// <param name="text">Text string to broadcast</param>
        /// <returns>'true' if the text was successfully broadcast, 'false' if the text was not broadcast</returns>
        bool Broadcast(string text);

        /// <summary>
        /// Broadcast data to all connected sessions
        /// </summary>
        /// <param name="buffer">Buffer to broadcast</param>
        /// <returns>'true' if the data was successfully broadcast, 'false' if the data was not broadcast</returns>
        bool Broadcast(byte[] buffer);

        /// <summary>
        /// Broadcast data to all connected clients
        /// </summary>
        /// <param name="buffer">Buffer to broadcast</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="size">Buffer size</param>
        /// <returns>'true' if the data was successfully broadcast, 'false' if the data was not broadcast</returns>
        bool Broadcast(byte[] buffer, long offset, long size);

        /// <summary>
        /// Disposed flag
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Acceptor socket disposed flag
        /// </summary>
        bool IsSocketDisposed { get; }

        // Implement IDisposable.
        void Dispose();
    }

    internal static class IServerExtentions
    {
        /// <summary>
        /// Register a new session
        /// </summary>
        /// <param name="session">Session to register</param>
        public static void RegisterSession(this IServer instance, ISession session)
        {
            instance.RegisterSession(session);
        }

        /// <summary>
        /// Unregister session by Id
        /// </summary>
        /// <param name="sessionId">Session Id</param>
        public static void UnregisterSession(this IServer instance, Guid sessionId)
        {
            instance.UnregisterSession(sessionId);
        }


        public static void OnConnectedInternal(this IServer instance, ISession session)
        {
            instance.OnConnectedInternal(session);
        }

        public static void OnDisconnectedInternal(this IServer instance, ISession session)
        {
            instance.OnDisconnectedInternal(session);
        }
    }
}
