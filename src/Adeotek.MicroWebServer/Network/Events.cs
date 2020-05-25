using System;
using System.Net.Sockets;

namespace Adeotek.MicroWebServer.Network
{
    public class WsConnectionEventArgs : EventArgs
    {
        public WsConnectionEventArgs(Guid id)
        {
            Id = id;
        }

        public readonly Guid Id;
    }

    public class WsErrorEventArgs : EventArgs
    {
        public WsErrorEventArgs(Guid id, SocketError error)
        {
            Id = id;
            Error = error;
        }

        public readonly Guid Id;
        public readonly SocketError Error;
    }

    public class WsMessageEventArgs : EventArgs
    {
        public WsMessageEventArgs(Guid id, string message)
        {
            Id = id;
            Message = message;
        }

        public readonly Guid Id;
        public readonly string Message;
    }
}
