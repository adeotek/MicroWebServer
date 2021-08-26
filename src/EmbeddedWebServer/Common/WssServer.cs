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
        public event IWebSocketSession.SessionConnectedDelegate OnSessionConnected;
        public event IWebSocketSession.SessionDisconnectedDelegate OnSessionDisconnected;
        public event IWebSocketSession.RawMessageReceivedDelegate OnMessageReceived;
        public event IWebSocketSession.SessionErrorDelegate OnSessionError;

        public bool Broadcast(string text) => Multicast(text);

        public bool Broadcast(byte[] buffer, long offset, long size) => Multicast(buffer, offset, size);

        public bool BroadcastBinary(string text) => MulticastBinary(text);

        public bool BroadcastBinary(byte[] buffer, long offset, long size) => MulticastBinary(buffer, offset, size);

        protected override SslSession CreateSession()
        {
            var newSession = new WssSession(this);

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
            OnSessionConnected?.Invoke(session, new ConnectionStateEventArgs(session.Id, true));
        }

        protected override void OnDisconnected(SslSession session)
        {
            OnSessionDisconnected?.Invoke(session, new ConnectionStateEventArgs(session.Id, false));
        }

        protected override void OnError(SocketError error)
        {
            OnServerError?.Invoke(this, new SocketErrorEventArgs(Guid.Empty, error));
        }
    }
}