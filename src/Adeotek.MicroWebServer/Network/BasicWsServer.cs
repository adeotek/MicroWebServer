using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.Network
{
    public class BasicWsServer : WsServer
    {
        private readonly ILogger _logger;

        public delegate void WsServerErrorDelegate(object sender, WsErrorEventArgs e);
        public event WsServerErrorDelegate OnWsServerError;

        public event BasicWsSession.SessionConnectedDelegate OnSessionConnected;
        public event BasicWsSession.SessionDisconnectedDelegate OnSessionDisconnected;
        public event BasicWsSession.SessionErrorDelegate OnSessionError;
        public event BasicWsSession.MessageReceivedDelegate OnSessionMessageReceived;

        public BasicWsServer(IPAddress address, int port, ILogger logger) : base(address, port)
        {
            _logger = logger;
        }

        protected override TcpSession CreateSession()
        {
            var session = new BasicWsSession(this, _logger);
            if (OnSessionConnected != null)
            {
                session.OnSessionConnected += OnSessionConnected;
            }
            if (OnSessionDisconnected != null)
            {
                session.OnSessionDisconnected += OnSessionDisconnected;
            }
            if (OnSessionError != null)
            {
                session.OnSessionError += OnSessionError;
            }
            if (OnSessionMessageReceived != null)
            {
                session.OnMessageReceived += OnSessionMessageReceived;
            }
            return session;
        }

        protected override void OnError(SocketError error)
        {
            _logger?.LogDebug("WebSocket server [{id}] error: ", Id, error);
            OnWsServerError?.Invoke(this, new WsErrorEventArgs(Id, error));
        }
    }
}
