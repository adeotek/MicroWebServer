using System.Net.Sockets;
using NetCoreServer;
using NetCoreWssSession = NetCoreServer.WssSession;
using NetCoreWssServer = NetCoreServer.WssServer;

namespace Adeotek.EmbeddedWebServer.Common
{
    public class WssSession : NetCoreWssSession, IWebSocketSession
    {
        public WssSession(NetCoreWssServer server) : base(server)
        {
        }

        public event IWebSocketSession.SessionConnectedDelegate OnSessionConnected;
        public event IWebSocketSession.SessionDisconnectedDelegate OnSessionDisconnected;
        public event IWebSocketSession.RawMessageReceivedDelegate OnMessageReceived;
        public event IWebSocketSession.SessionErrorDelegate OnSessionError;

        /// <summary>
        ///     Handle connected event
        /// </summary>
        /// <param name="request">HTTP request object</param>
        /// <remarks>
        ///     Notification is called when connection is established to the client.
        /// </remarks>
        public override void OnWsConnected(HttpRequest request)
        {
            OnSessionConnected?.Invoke(this, new ConnectionStateEventArgs(Id, true));
        }

        /// <summary>
        ///     Handle disconnected event
        /// </summary>
        /// <remarks>
        ///     Notification is called when connection to the client is closed.
        /// </remarks>
        public override void OnWsDisconnected()
        {
            OnSessionDisconnected?.Invoke(this, new ConnectionStateEventArgs(Id, false));
        }

        /// <summary>
        ///     Handle WebSocket received notification
        /// </summary>
        /// <param name="buffer">Received buffer</param>
        /// <param name="offset">Received buffer offset</param>
        /// <param name="size">Received buffer size</param>
        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            OnMessageReceived?.Invoke(this, new RawMessageEventArgs(Id, buffer, offset, size));
        }

        /// <summary>
        ///     Handle WebSocket error notification
        /// </summary>
        /// <param name="error">Error message</param>
        public override void OnWsError(string error)
        {
            OnSessionError?.Invoke(this, new SocketErrorEventArgs(Id, SocketError.SocketError, error));
        }

        /// <summary>
        ///     Handle socket error notification
        /// </summary>
        /// <param name="error">Socket error</param>
        public override void OnWsError(SocketError error)
        {
            OnSessionError?.Invoke(this, new SocketErrorEventArgs(Id, SocketError.SocketError));
        }
    }
}