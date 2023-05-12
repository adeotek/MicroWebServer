using System;

namespace Adeotek.EmbeddedWebServer.Common
{
    public interface IWebSocketServer
    {
        /// <summary>
        ///     Is the server started?
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        ///     Is the server accepting new clients?
        /// </summary>
        bool IsAccepting { get; }

        /// <summary>
        ///     Disposed flag
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        ///     Acceptor socket disposed flag
        /// </summary>
        bool IsSocketDisposed { get; }

        // events
        delegate void ServerStartedDelegate(object sender, ServerStateEventArgs e);
        delegate void ServerStoppedDelegate(object sender, ServerStateEventArgs e);
        delegate void ServerSocketErrorDelegate(object sender, SocketErrorEventArgs e);

        event ServerStartedDelegate OnServerStarted;
        event ServerStoppedDelegate OnServerStopped;
        event ServerSocketErrorDelegate OnServerError;
        event IWebSocketSession.SessionConnectedDelegate OnServerSessionConnected;
        event IWebSocketSession.SessionDisconnectedDelegate OnServerSessionDisconnected;
        public event IWebSocketSession.SessionConnectedDelegate OnSessionConnected;
        public event IWebSocketSession.SessionDisconnectedDelegate OnSessionDisconnected;
        event IWebSocketSession.RawMessageReceivedDelegate OnMessageReceived;
        event IWebSocketSession.SessionErrorDelegate OnSessionError;

        /// <summary>
        ///     Start the server
        /// </summary>
        /// <returns>'true' if the server was successfully started, 'false' if the server failed to start</returns>
        bool Start();

        /// <summary>
        ///     Stop the server
        /// </summary>
        /// <returns>'true' if the server was successfully stopped, 'false' if the server is already stopped</returns>
        bool Stop();

        /// <summary>
        ///     Restart the server
        /// </summary>
        /// <returns>'true' if the server was successfully restarted, 'false' if the server failed to restart</returns>
        bool Restart();

        /// <summary>
        ///     Broadcast to all connected clients
        /// </summary>
        /// <param name="text">Text string to broadcast</param>
        /// <returns>'true' if the text was successfully broadcasted, 'false' if the text was not broadcasted</returns>
        bool Multicast(string text);
        bool Multicast(byte[] buffer, long offset, long size);
        bool MulticastText(string text);
        bool MulticastText(byte[] buffer, long offset, long size);
        bool MulticastBinary(string text);
        bool MulticastBinary(byte[] buffer, long offset, long size);

        /// <summary>
        /// Add static content cache
        /// </summary>
        /// <param name="path">Static content path</param>
        /// <param name="prefix">Cache prefix (default is "/")</param>
        /// <param name="filter">Cache filter (default is "*.*")</param>
        /// <param name="timeout">Refresh cache timeout (default is 1 hour)</param>
        void AddStaticContent(string path, string prefix = "/", string filter = "*.*", TimeSpan? timeout = null);

        /// <summary>
        /// Remove static content cache
        /// </summary>
        /// <param name="path">Static content path</param>
        void RemoveStaticContent(string path);

        /// <summary>
        /// Clear static content cache
        /// </summary>
        void ClearStaticContent();

        // Implement IDisposable.
        void Dispose();
    }
}