using System;
using System.Net.Sockets;

namespace Adeotek.MicroWebServer.Core
{
    public interface ISession
    {
        delegate void SessionConnectedDelegate(object sender, ConnectionEventArgs e);
        delegate void SessionDisconnectedDelegate(object sender, ConnectionEventArgs e);
        delegate void SessionErrorDelegate(object sender, SocketErrorEventArgs e);
        delegate void RawMessageReceivedDelegate(object sender, RawMessageEventArgs e);
        delegate void MessageSentDelegate(object sender, MessageSentEventArgs e);
        delegate void EmptyMessageDelegate(object sender, RawMessageEventArgs e);
        event SessionConnectedDelegate OnSessionConnected;
        event SessionDisconnectedDelegate OnSessionDisconnected;
        event SessionErrorDelegate OnSocketError;
        event RawMessageReceivedDelegate OnMessageReceived;
        event MessageSentDelegate OnMessageSent;
        event EmptyMessageDelegate OnEmptyMessage;

        /// <summary>
        /// Session Id
        /// </summary>
        Guid Id { get; }
        /// <summary>
        /// Server
        /// </summary>
        IServer Server { get; }
        /// <summary>
        /// Socket
        /// </summary>
        Socket Socket { get; }
        /// <summary>
        /// Number of bytes pending sent by the session
        /// </summary>
        long BytesPending { get; }
        /// <summary>
        /// Number of bytes sending by the session
        /// </summary>
        long BytesSending { get; }
        /// <summary>
        /// Number of bytes sent by the session
        /// </summary>
        long BytesSent { get; }
        /// <summary>
        /// Number of bytes received by the session
        /// </summary>
        long BytesReceived { get; }
        /// <summary>
        /// Option: receive buffer size
        /// </summary>
        int OptionReceiveBufferSize { get; set; }
        /// <summary>
        /// Option: send buffer size
        /// </summary>
        int OptionSendBufferSize { get; set; }

        /// <summary>
        /// Is the session connected?
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Disconnect the session
        /// </summary>
        /// <returns>'true' if the section was successfully disconnected, 'false' if the section is already disconnected</returns>
        bool Disconnect();

        /// <summary>
        /// Send data to the client (synchronous)
        /// </summary>
        /// <param name="buffer">Buffer to send</param>
        /// <returns>Size of sent data</returns>
        long Send(byte[] buffer);

        /// <summary>
        /// Send data to the client (synchronous)
        /// </summary>
        /// <param name="buffer">Buffer to send</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="size">Buffer size</param>
        /// <returns>Size of sent data</returns>
        long Send(byte[] buffer, long offset, long size);

        /// <summary>
        /// Send text to the client (synchronous)
        /// </summary>
        /// <param name="text">Text string to send</param>
        /// <returns>Size of sent data</returns>
        long Send(string text);

        /// <summary>
        /// Send data to the client (asynchronous)
        /// </summary>
        /// <param name="buffer">Buffer to send</param>
        /// <returns>'true' if the data was successfully sent, 'false' if the session is not connected</returns>
        bool SendAsync(byte[] buffer);

        /// <summary>
        /// Send data to the client (asynchronous)
        /// </summary>
        /// <param name="buffer">Buffer to send</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="size">Buffer size</param>
        /// <returns>'true' if the data was successfully sent, 'false' if the session is not connected</returns>
        bool SendAsync(byte[] buffer, long offset, long size);

        /// <summary>
        /// Send text to the client (asynchronous)
        /// </summary>
        /// <param name="text">Text string to send</param>
        /// <returns>'true' if the text was successfully sent, 'false' if the session is not connected</returns>
        bool SendAsync(string text);

        /// <summary>
        /// Receive data from the client (synchronous)
        /// </summary>
        /// <param name="buffer">Buffer to receive</param>
        /// <returns>Size of received data</returns>
        long Receive(byte[] buffer);

        /// <summary>
        /// Receive data from the client (synchronous)
        /// </summary>
        /// <param name="buffer">Buffer to receive</param>
        /// <param name="offset">Buffer offset</param>
        /// <param name="size">Buffer size</param>
        /// <returns>Size of received data</returns>
        long Receive(byte[] buffer, long offset, long size);

        /// <summary>
        /// Receive text from the client (synchronous)
        /// </summary>
        /// <param name="size">Text size to receive</param>
        /// <returns>Received text</returns>
        string Receive(long size);

        /// <summary>
        /// Receive data from the client (asynchronous)
        /// </summary>
        void ReceiveAsync();

        /// <summary>
        /// Disposed flag
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Session socket disposed flag
        /// </summary>
        bool IsSocketDisposed { get; }

        // Implement IDisposable.
        void Dispose();
    }

    internal static class ISessionExtensions
    {
        /// <summary>
        /// Connect the session
        /// </summary>
        /// <param name="socket">Session socket</param>
        public static void Connect(this ISession instance, Socket socket)
        {
            instance.Connect(socket);
        }
    }
}
