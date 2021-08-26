using System;
using System.Net.Sockets;
using System.Text;

namespace Adeotek.MicroWebServer.WebSocket
{
    public class ServerStateEventArgs : EventArgs
    {
        public readonly Guid Id;

        public ServerStateEventArgs(Guid id)
        {
            Id = id;
        }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public readonly Guid SessionId;

        public ConnectionEventArgs(Guid sessionId)
        {
            SessionId = sessionId;
        }
    }

    public class SocketErrorEventArgs : EventArgs
    {
        public readonly SocketError Error;

        public readonly Guid SessionId;

        public readonly string Message;

        public SocketErrorEventArgs(Guid sessionId, SocketError error, string message = null)
        {
            SessionId = sessionId;
            Error = error;
            Message = message;
        }
    }

    public class RawMessageEventArgs : EventArgs
    {
        public readonly byte[] Buffer;
        public readonly long Offset;

        public readonly Guid SessionId;
        public readonly long Size;

        public RawMessageEventArgs(Guid sessionId, byte[] buffer, long offset, long size)
        {
            SessionId = sessionId;
            Buffer = buffer;
            Offset = offset;
            Size = size;
        }

        public string Message => Encoding.UTF8.GetString(Buffer, (int) Offset, (int) Size);
    }

    public class MessageSentEventArgs : EventArgs
    {
        public readonly long Pending;
        public readonly long Sent;

        public readonly Guid SessionId;

        public MessageSentEventArgs(Guid sessionId, long sent, long pending)
        {
            SessionId = sessionId;
            Sent = sent;
            Pending = pending;
        }
    }
}