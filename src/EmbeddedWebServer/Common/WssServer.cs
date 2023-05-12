using System;
using System.Net;
using System.Net.Sockets;
using NetCoreServer;
using NetCoreWssServer = NetCoreServer.WssServer;

namespace Adeotek.EmbeddedWebServer.Common
{
    public class WssServer : NetCoreWssServer, IWebSocketServer
    {
        public WssServer(SslContext context, IPAddress address, int port) : base(context, address, port)
        {
        }

        public event IWebSocketServer.ServerStartedDelegate OnServerStarted;
        public event IWebSocketServer.ServerStoppedDelegate OnServerStopped;
        public event IWebSocketServer.ServerSocketErrorDelegate OnServerError;
        public event IWebSocketSession.SessionConnectedDelegate OnServerSessionConnected;
        public event IWebSocketSession.SessionDisconnectedDelegate OnServerSessionDisconnected;
        public event IWebSocketSession.SessionConnectedDelegate OnSessionConnected;
        public event IWebSocketSession.SessionDisconnectedDelegate OnSessionDisconnected;
        public event IWebSocketSession.RawMessageReceivedDelegate OnMessageReceived;
        public event IWebSocketSession.SessionErrorDelegate OnSessionError;

        protected override SslSession CreateSession()
        {
            var newSession = new WssSession(this);

            if (OnSessionConnected != null)
            {
                newSession.OnSessionConnected += OnSessionConnected;
            }

            if (OnSessionDisconnected != null)
            {
                newSession.OnSessionDisconnected += OnSessionDisconnected;
            }

            if (OnMessageReceived != null)
            {
                newSession.OnMessageReceived += OnMessageReceived;
            }

            if (OnSessionError != null)
            {
                newSession.OnSessionError += OnSessionError;
            }

            return newSession;
        }

        protected override void OnStarted()
        {
            OnServerStarted?.Invoke(this, new ServerStateEventArgs(Id));
        }

        protected override void OnStopped()
        {
            OnServerStopped?.Invoke(this, new ServerStateEventArgs(Id));
        }

        protected override void OnConnected(SslSession session)
        {
            OnServerSessionConnected?.Invoke(session, new ConnectionStateEventArgs(session.Id, true));
        }

        protected override void OnDisconnected(SslSession session)
        {
            OnServerSessionDisconnected?.Invoke(session, new ConnectionStateEventArgs(session.Id, false));
        }

        protected override void OnError(SocketError error)
        {
            OnServerError?.Invoke(this, new SocketErrorEventArgs(Guid.Empty, error));
        }
    }
}