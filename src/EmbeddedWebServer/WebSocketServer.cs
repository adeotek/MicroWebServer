using System;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Adeotek.EmbeddedWebServer.Common;
using Microsoft.Extensions.Logging;
using NetCoreServer;
using WsServer = Adeotek.EmbeddedWebServer.Common.WsServer;
using WssServer = Adeotek.EmbeddedWebServer.Common.WssServer;

namespace Adeotek.EmbeddedWebServer
{
    public class WebSocketServer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private readonly X509Certificate2 _certificate;
        private readonly Action<IWebSocketSession, string> _messageConsumer;
        private IWebSocketServer _server;

        public event IWebSocketServer.ServerStartedDelegate OnServerStarted;
        public event IWebSocketServer.ServerStoppedDelegate OnServerStopped;
        public event IWebSocketServer.ServerSocketErrorDelegate OnServerError;
        public event IWebSocketSession.SessionConnectedDelegate OnServerSessionConnected;
        public event IWebSocketSession.SessionDisconnectedDelegate OnServerSessionDisconnected;

        public event IWebSocketSession.SessionConnectedDelegate OnSessionConnected;
        public event IWebSocketSession.SessionDisconnectedDelegate OnSessionDisconnected;
        public event IWebSocketSession.RawMessageReceivedDelegate OnMessageReceived;
        public event IWebSocketSession.SessionErrorDelegate OnSessionError;

        public WebSocketServer(
            IPAddress ipAddress = null,
            int port = 8080,
            X509Certificate2 certificate = null,
            Action<IWebSocketSession, string> messageConsumerMethod = null,
            ILogger logger = null
            )
        {
            _ipAddress = ipAddress ?? IPAddress.Any;
            _port = port;
            _certificate = certificate;
            _messageConsumer = messageConsumerMethod;
            _logger = logger;
            InitializeServer();
        }

        public WebSocketServer(
            string ipAddress = null,
            int port = 8080,
            X509Certificate2 certificate = null,
            Action<IWebSocketSession, string> messageConsumerMethod = null,
            ILogger logger = null
        )
        {
            _ipAddress = string.IsNullOrEmpty(ipAddress) ? IPAddress.Any : IPAddress.Parse(ipAddress);
            _port = port;
            _certificate = certificate;
            _messageConsumer = messageConsumerMethod;
            _logger = logger;
            InitializeServer();
        }

        private void InitializeServer()
        {
            if (_certificate == null)
            {
                _server = new WsServer(_ipAddress, _port);
            }
            else
            {
                var context = new SslContext(SslProtocols.Tls12, _certificate)
                {
                    ClientCertificateRequired = false,
                    CertificateValidationCallback = (sender, certificate, chain, errors) => true
                };
                _server = new WssServer(context, _ipAddress, _port);
                _server.AddStaticContent("www", "/chat");
            }
            _server.OnServerStarted += OnWsServerStarted;
            _server.OnServerStopped += OnWsServerStopped;
            _server.OnServerError += OnWsServerError;
            _server.OnServerSessionConnected += OnWsServerSessionConnected;
            _server.OnServerSessionDisconnected += OnWsServerSessionDisconnected;
            _server.OnSessionConnected += OnWsSessionConnected;
            _server.OnSessionDisconnected += OnWsSessionDisconnected;
            _server.OnMessageReceived += OnWsMessageReceived;
            _server.OnSessionError += OnWsSessionError;
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
            return _server?.Multicast(text) ?? false;
        }

        private void OnWsServerStarted(object sender, ServerStateEventArgs e)
        {
            if (OnServerStarted == null)
            {
                if (_certificate != null)
                {
                    _logger?.LogDebug("WebSocket server [{Id}] started and listening on wss://{Ip}:{Port}/ using certificate [{Certificate}]", e.Id.ToString(), _ipAddress.ToString(), _port.ToString(), _certificate.SerialNumber);
                }
                else
                {
                    _logger?.LogDebug("WebSocket server [{Id}] started and listening on ws://{Ip}:{Port}/", e.Id.ToString(), _ipAddress.ToString(), _port.ToString());
                }
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
                _logger?.LogDebug("WebSocket session [{Id}] has stop!", e.Id.ToString());
            }
            else
            {
                OnServerStopped?.Invoke(sender, e);
            }
        }

        private void OnWsServerError(object sender, SocketErrorEventArgs e)
        {
            _logger.LogError("WebSocket [{Id}] server error ({Message}): {Error}", e.SessionId.ToString(), e.Message, e.Error);
            OnServerError?.Invoke(sender, e);
        }

        private void OnWsMessageReceived(object sender, RawMessageEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Buffer, (int)e.Offset, (int)e.Size);
            if (OnMessageReceived == null && _messageConsumer == null)
            {
                _logger?.LogDebug("WebSocket session [{Id}] message received: {Message}", e.SessionId.ToString(), message);
                return;
            }
            OnMessageReceived?.Invoke(sender, e);
            _messageConsumer?.Invoke((IWebSocketSession) sender, message);
        }

        private void OnWsServerSessionConnected(object sender, ConnectionStateEventArgs e)
        {
            if (OnServerSessionConnected == null)
            {
                _logger?.LogDebug("WebSocket server session [{Id}] connected!", e.SessionId.ToString());
            }
            else
            {
                OnServerSessionConnected?.Invoke(sender, e);
            }
        }

        private void OnWsServerSessionDisconnected(object sender, ConnectionStateEventArgs e)
        {
            if (OnServerSessionDisconnected == null)
            {
                _logger?.LogDebug("WebSocket server session [{Id}] disconnected!", e.SessionId.ToString());
            }
            else
            {
                OnServerSessionDisconnected?.Invoke(sender, e);
            }
        }

        private void OnWsSessionConnected(object sender, ConnectionStateEventArgs e)
        {
            if (OnSessionConnected == null)
            {
                _logger?.LogDebug("WebSocket session [{Id}] connected!", e.SessionId.ToString());
            }
            else
            {
                OnSessionConnected?.Invoke(sender, e);
            }
        }

        private void OnWsSessionDisconnected(object sender, ConnectionStateEventArgs e)
        {
            if (OnSessionDisconnected == null)
            {
                _logger?.LogDebug("WebSocket session [{Id}] disconnected!", e.SessionId.ToString());
            }
            else
            {
                OnSessionDisconnected?.Invoke(sender, e);
            }
        }

        private void OnWsSessionError(object sender, SocketErrorEventArgs e)
        {
            _logger.LogError("WebSocket [{Id}] session error ({Message}): {Error}", e.SessionId.ToString(), e.Message, e.Error);
            OnSessionError?.Invoke(sender, e);
        }

        public void Dispose()
        {
            _server?.Stop();
            _server?.Dispose();
        }
    }
}
