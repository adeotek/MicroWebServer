using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer
{
    public class WebSocketServer
    {
        private readonly ILogger _logger;
        //private BasicWsServer _server;

        //public ICollection<string> CrossDomains { get; set; }
        //public bool IsRunning { get; private set; }

        //public delegate void ServerErrorDelegate(object sender, WsErrorEventArgs e);
        //public delegate void SessionErrorDelegate(object sender, WsErrorEventArgs e);
        //public delegate void SessionConnectedDelegate(object sender, WsConnectionEventArgs e);
        //public delegate void SessionDisconnectedDelegate(object sender, WsConnectionEventArgs e);
        //public delegate void SessionMessageReceivedDelegate(object sender, WsMessageEventArgs e);
        //public event ServerErrorDelegate OnServerError;
        //public event SessionErrorDelegate OnSessionError;
        //public event SessionConnectedDelegate OnSessionConnected;
        //public event SessionDisconnectedDelegate OnSessionDisconnected;
        //public event SessionMessageReceivedDelegate OnMessageReceived;

        //public WebSocketServer(
        //    string ipAddress = "127.0.0.1",
        //    int port = 8080,
        //    ICollection<string> crossDomains = null,
        //    ILogger logger = null
        //    )
        //{
        //    _logger = logger;
        //    _server = new BasicWsServer(IPAddress.Parse(ipAddress), port, _logger);
        //    _server.OnWsServerError += OnWsServerError;
        //    _server.OnSessionError += OnWsSessionError;
        //    _server.OnSessionConnected += OnWsSessionConnected;
        //    _server.OnSessionDisconnected += OnWsSessionDisconnected;
        //    _server.OnSessionMessageReceived += OnWsMessageReceived;
        //    CrossDomains = crossDomains;
        //}

        //public bool Start()
        //{
        //    return _server?.Start() ?? false;
        //}

        //public bool Stop()
        //{
        //    return _server?.Stop() ?? false;
        //}

        //public bool Restart()
        //{
        //    return _server?.Restart() ?? false;
        //}

        //public bool MulticastText(string text)
        //{
        //    return _server?.Multicast(text) ?? false;
        //}

        //private void OnWsMessageReceived(object sender, WsMessageEventArgs e)
        //{
        //    OnMessageReceived?.Invoke(sender, e);
        //}

        //private void OnWsSessionConnected(object sender, WsConnectionEventArgs e)
        //{
        //    OnSessionConnected?.Invoke(sender, e);
        //}

        //private void OnWsSessionDisconnected(object sender, WsConnectionEventArgs e)
        //{
        //    OnSessionDisconnected?.Invoke(sender, e);
        //}

        //private void OnWsSessionError(object sender, WsErrorEventArgs e)
        //{
        //    _logger.LogError("WebSocket [{id}] session error: {e}", e.Id, e.Error);
        //    OnSessionError?.Invoke(sender, e);
        //}

        //private void OnWsServerError(object sender, WsErrorEventArgs e)
        //{
        //    _logger.LogError("WebSocket [{id}] server error: {e}", e.Id, e.Error);
        //    OnServerError?.Invoke(sender, e);
        //}
    }
}
