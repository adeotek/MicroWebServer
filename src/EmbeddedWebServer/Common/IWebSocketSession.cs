using System;
using System.Net.Sockets;

namespace Adeotek.EmbeddedWebServer.Common
{
    public interface IWebSocketSession
    {
        /// <summary>
        ///     Session Id
        /// </summary>
        Guid Id { get; }

        /// <summary>
        ///     Socket
        /// </summary>
        Socket Socket { get; }

        /// <summary>
        ///     Number of bytes pending sent by the session
        /// </summary>
        long BytesPending { get; }

        /// <summary>
        ///     Number of bytes sending by the session
        /// </summary>
        long BytesSending { get; }

        /// <summary>
        ///     Number of bytes sent by the session
        /// </summary>
        long BytesSent { get; }

        /// <summary>
        ///     Number of bytes received by the session
        /// </summary>
        long BytesReceived { get; }

        /// <summary>
        ///     Option: receive buffer size
        /// </summary>
        int OptionReceiveBufferSize { get; set; }

        /// <summary>
        ///     Option: send buffer size
        /// </summary>
        int OptionSendBufferSize { get; set; }

        /// <summary>
        ///     Is the session connected?
        /// </summary>
        bool IsConnected { get; }

        // events
        delegate void SessionConnectedDelegate(object sender, ConnectionStateEventArgs e);
        delegate void SessionDisconnectedDelegate(object sender, ConnectionStateEventArgs e);
        delegate void RawMessageReceivedDelegate(object sender, RawMessageEventArgs e);
        delegate void SessionErrorDelegate(object sender, SocketErrorEventArgs e);

        event SessionConnectedDelegate OnSessionConnected;
        event SessionDisconnectedDelegate OnSessionDisconnected;
        event RawMessageReceivedDelegate OnMessageReceived;
        event SessionErrorDelegate OnSessionError;

        long SendText(byte[] buffer, long offset, long size);
        long SendText(string text);
        bool SendTextAsync(byte[] buffer, long offset, long size);
        bool SendTextAsync(string text);
        long SendBinary(byte[] buffer, long offset, long size);
        long SendBinary(string text);
        bool SendBinaryAsync(byte[] buffer, long offset, long size);
        bool SendBinaryAsync(string text);
        long SendClose(int status, byte[] buffer, long offset, long size);
        long SendClose(int status, string text);
        bool SendCloseAsync(int status, byte[] buffer, long offset, long size);
        bool SendCloseAsync(int status, string text);
    }
}