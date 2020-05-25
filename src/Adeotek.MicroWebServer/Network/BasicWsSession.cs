using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.Network
{
    public class BasicWsSession : WsSession
    {
        private readonly ILogger _logger;

        public delegate void SessionConnectedDelegate(object sender, WsConnectionEventArgs e);
        public delegate void SessionDisconnectedDelegate(object sender, WsConnectionEventArgs e);
        public delegate void SessionErrorDelegate(object sender, WsErrorEventArgs e);
        public delegate void MessageReceivedDelegate(object sender, WsMessageEventArgs e);
        public event SessionConnectedDelegate OnSessionConnected;
        public event SessionDisconnectedDelegate OnSessionDisconnected;
        public event SessionErrorDelegate OnSessionError;
        public event MessageReceivedDelegate OnMessageReceived;

        public BasicWsSession(WsServer server, ILogger logger) : base(server)
        {
            _logger = logger;
        }

        public override void OnWsConnected(HttpRequest request)
        {
            _logger?.LogDebug("WebSocket session [{id}] connected!", Id);
            OnSessionConnected?.Invoke(this, new WsConnectionEventArgs(Id));
        }

        public override void OnWsDisconnected()
        {
            _logger?.LogDebug("WebSocket session [{id}] disconnected!", Id);
            OnSessionDisconnected?.Invoke(this, new WsConnectionEventArgs(Id));
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            var message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            _logger?.LogDebug("WebSocket session [{id}] message received: {msg}", Id, message);
            OnMessageReceived?.Invoke(this, new WsMessageEventArgs(Id, message));
            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
            {
                Close(1000);
            }
        }

        protected override void OnError(SocketError error)
        {
            _logger?.LogDebug("WebSocket session [{id}] error: ", Id, error);
            OnSessionError?.Invoke(this, new WsErrorEventArgs(Id, error));
        }
    }
}
