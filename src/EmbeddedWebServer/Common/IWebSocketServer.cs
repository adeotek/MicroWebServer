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
        event IWebSocketSession.SessionConnectedDelegate OnSessionConnected;
        event IWebSocketSession.SessionDisconnectedDelegate OnSessionDisconnected;
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
        bool Broadcast(string text);
        bool Broadcast(byte[] buffer, long offset, long size);
        bool BroadcastBinary(string text);
        bool BroadcastBinary(byte[] buffer, long offset, long size);

        // Implement IDisposable.
        void Dispose();
    }
}