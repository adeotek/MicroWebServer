using System;
using System.Net;
using System.Text;
using Adeotek.MicroWebServer.WebSocket;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer
{
    public class WebSocketServer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly WsServer _server;
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly Action<WsSession, string> _messageConsumer;

        public event WsServer.SessionConnectedDelegate OnSessionConnected;
        public event WsServer.SessionDisconnectedDelegate OnSessionDisconnected;
        public event WsServer.ServerStartedDelegate OnServerStarted;
        public event WsServer.ServerStoppedDelegate OnServerStopped;
        public event WsServer.ServerSocketErrorDelegate OnServerError;

        public event WsSession.SessionErrorDelegate OnSessionError;
        public event WsSession.RawMessageReceivedDelegate OnMessageReceived;
        public event WsSession.MessageSentDelegate OnMessageSent;
        public event WsSession.EmptyMessageDelegate OnEmptyMessage;

        public WebSocketServer(
            string ipAddress = "127.0.0.1",
            int port = 8080,
            Action<WsSession, string> messageConsumerMethod = null,
            ILogger logger = null
            )
        {
            _ipAddress = ipAddress;
            _port = port;
            _messageConsumer = messageConsumerMethod;
            _logger = logger;
            _server = new WsServer(IPAddress.Parse(ipAddress), port);
            _server.OnServerStarted += OnWsServerStarted;
            _server.OnServerStopped += OnWsServerStopped;
            _server.OnServerError += OnWsServerError;
            _server.OnSessionError += OnWsSessionError;
            _server.OnSessionConnected += OnWsSessionConnected;
            _server.OnSessionDisconnected += OnWsSessionDisconnected;
            _server.OnMessageReceived += OnWsMessageReceived;
            _server.OnEmptyMessage += OnEmptyMessage;
            _server.OnMessageSent += OnMessageSent;
        }

        public bool IsRunning => _server.IsStarted;
        public bool IsAccepting => _server.IsAccepting;

        public bool Start()
        {
            return _server?.Start() ?? false;
        }

        public bool Stop()
        {
            if (!(_server?.IsStarted ?? false))
            {
                return false;
            }

            return _server.Stop();
        }

        public bool Restart()
        {
            return _server?.Restart() ?? false;
        }

        public bool Broadcast(string text)
        {
            return _server?.Broadcast(text) ?? false;
        }

        private void OnWsMessageReceived(object sender, RawMessageEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Buffer, (int)e.Offset, (int)e.Size);
            if (OnMessageReceived == null && _messageConsumer == null)
            {
                _logger?.LogDebug("WebSocket session [{id}] message received: {msg}", e.SessionId, message);
                return;
            }
            OnMessageReceived?.Invoke(sender, e);
            _messageConsumer?.Invoke((WsSession)sender, message);
        }

        private void OnWsServerStarted(object sender, ServerStateEventArgs e)
        {
            if (OnServerStarted == null)
            {
                _logger?.LogDebug("WebSocket server [{id}] started and listening on http://{ip}:{port}/", e.Id, _ipAddress, _port);
            }
            else
            {
                OnServerStarted?.Invoke(sender, e);
            }
        }

        private void OnWsServerStopped(object sender, ServerStateEventArgs e)
        {
            if (OnServerStopped == null)
            {
                _logger?.LogDebug("WebSocket session [{id}] has stop!", e.Id);
            }
            else
            {
                OnServerStopped?.Invoke(sender, e);
            }
        }

        private void OnWsSessionConnected(object sender, ConnectionEventArgs e)
        {
            if (OnSessionConnected == null)
            {
                _logger?.LogDebug("WebSocket session [{id}] connected!", e.SessionId);
            }
            else
            {
                OnSessionConnected?.Invoke(sender, e);
            }
        }

        private void OnWsSessionDisconnected(object sender, ConnectionEventArgs e)
        {
            if (OnSessionDisconnected == null)
            {
                _logger?.LogDebug("WebSocket session [{id}] disconnected!", e.SessionId);
            }
            else
            {
                OnSessionDisconnected?.Invoke(sender, e);
            }
        }

        private void OnWsSessionError(object sender, SocketErrorEventArgs e)
        {
            _logger.LogError("WebSocket [{id}] session error ({msg}): {e}", e.SessionId, e.Message, e.Error);
            OnSessionError?.Invoke(sender, e);
        }

        private void OnWsServerError(object sender, SocketErrorEventArgs e)
        {
            _logger.LogError("WebSocket [{id}] server error ({msg}): {e}", e.SessionId, e.Message, e.Error);
            OnServerError?.Invoke(sender, e);
        }

        public void Dispose()
        {
            _server?.Stop();
            _server?.Dispose();
        }
    }
}
