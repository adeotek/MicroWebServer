using System;
using System.Net;
using System.Net.Sockets;
using NetCoreServer;
using NetCoreWsServer = NetCoreServer.WsServer;

namespace Adeotek.EmbeddedWebServer.Common
{
    public class WsServer : NetCoreWsServer, IWebSocketServer
    {
        public WsServer(IPAddress address, int port) : base(address, port)
        {
        }

        public event IWebSocketServer.ServerStartedDelegate OnServerStarted;
        public event IWebSocketServer.ServerStoppedDelegate OnServerStopped;
        public event IWebSocketServer.ServerSocketErrorDelegate OnServerError;
        public event IWebSocketSession.SessionConnectedDelegate OnSessionConnected;
        public event IWebSocketSession.SessionDisconnectedDelegate OnSessionDisconnected;
        public event IWebSocketSession.RawMessageReceivedDelegate OnMessageReceived;
        public event IWebSocketSession.SessionErrorDelegate OnSessionError;

        public bool Broadcast(string text) => Multicast(text);

        public bool Broadcast(byte[] buffer, long offset, long size) => Multicast(buffer, offset, size);

        public bool BroadcastBinary(string text) => MulticastBinary(text);

        public bool BroadcastBinary(byte[] buffer, long offset, long size) => MulticastBinary(buffer, offset, size);

        protected override TcpSession CreateSession()
        {
            var newSession = new WsSession(this);

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

        protected override void OnConnected(TcpSession session)
        {
            OnSessionConnected?.Invoke(session, new ConnectionStateEventArgs(session.Id, true));
        }

        protected override void OnDisconnected(TcpSession session)
        {
            OnSessionDisconnected?.Invoke(session, new ConnectionStateEventArgs(session.Id, false));
        }

        protected override void OnError(SocketError error)
        {
            OnServerError?.Invoke(this, new SocketErrorEventArgs(Guid.Empty, error));
        }
    }
}