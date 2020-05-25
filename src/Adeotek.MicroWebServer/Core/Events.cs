using System;
using System.Drawing;
using System.Net.Sockets;
using System.Text;

namespace Adeotek.MicroWebServer.Core
{
    public class ServerStateEventArgs : EventArgs
    {
        public ServerStateEventArgs(Guid id)
        {
            Id = id;
        }

        public readonly Guid Id;
    }

    public class ConnectionEventArgs : EventArgs
    {
        public ConnectionEventArgs(Guid sessionId)
        {
            SessionId = sessionId;
        }

        public readonly Guid SessionId;
    }

    public class SocketErrorEventArgs : EventArgs
    {
        public SocketErrorEventArgs(Guid sessionId, SocketError error)
        {
            SessionId = sessionId;
            Error = error;
        }

        public readonly Guid SessionId;
        public readonly SocketError Error;
    }

    public class RawMessageEventArgs : EventArgs
    {
        public RawMessageEventArgs(Guid sessionId, byte[] buffer, long offset, long size)
        {
            SessionId = sessionId;
            Buffer = buffer;
            Offset = offset;
            Size = size;
        }

        public readonly Guid SessionId;
        public readonly byte[] Buffer;
        public readonly long Offset;
        public readonly long Size;
        public string Message => Encoding.UTF8.GetString(Buffer, (int) Offset, (int) Size);
    }

    public class MessageSentEventArgs : EventArgs
    {
        public MessageSentEventArgs(Guid sessionId, long sent, long pending)
        {
            SessionId = sessionId;
            Sent = sent;
            Pending = pending;
        }

        public readonly Guid SessionId;
        public readonly long Sent;
        public readonly long Pending;
    }

    public class TextMessageEventArgs : EventArgs
    {
        public TextMessageEventArgs(Guid sessionId, string message)
        {
            SessionId = sessionId;
            Message = message;
        }

        public readonly Guid SessionId;
        public readonly string Message;
    }
}
