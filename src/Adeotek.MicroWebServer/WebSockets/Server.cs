using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.WebSockets
{
    public class WebSocketMessageEventArgs : EventArgs
    {
        public WebSocketMessageEventArgs(string clientId, string message)
        {
            ClientId = clientId;
            Message = message;
        }

        public readonly string ClientId;
        public readonly string Message;
    }

    public class Server : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _ipAddress;
        private readonly ConnectionsManager _connectionsManager;
        private TcpListener _listener;

        public delegate void NewMessage(object sender, WebSocketMessageEventArgs e);
        public event NewMessage OnNewMessage;
        public bool IsRunning { get; private set; }

        public Server(
            //Func<HttpListenerRequest, string> requestResponderMethod,
            string ipAddress = "127.0.0.1",
            int port = 8080,
            ILogger logger = null)
        {
            _logger = logger;
            _ipAddress = ipAddress;
            _connectionsManager = new ConnectionsManager();
            _listener = new TcpListener(IPAddress.Parse(_ipAddress), port);

            //_responderMethod = requestResponderMethod ?? throw new ArgumentException("Invalid request responder method.");
        }

        public void Start()
        {
            _logger?.LogDebug("WebSocket server is starting...");
            try
            {
                _listener.Start();
                IsRunning = true;
                if (_logger != null)
                {
                    try
                    {
                        _logger.LogInformation("WebSocket server is listening on: {ip}", _ipAddress);
                    }
                    catch
                    {
                        // suppress exceptions
                    }
                }
            }
            catch (Exception ex)
            {
                IsRunning = false;
                _listener?.Stop();
                throw ex;
            }

            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    while (IsRunning)
                    {
                        _listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClientCallback), _listener);
                    }
                }
                catch (Exception ex)
                {
                    // suppress any exceptions and send it to logger object
                    _logger?.LogWarning(ex, "Exception caught and suppressed.");
                }
            });
        }

        public void Stop()
        {
            _logger?.LogDebug("WebSocket server is stopping...");
            IsRunning = false;
            _listener?.Stop();
            _logger?.LogInformation("WebSocket server not listening anymore!");
        }

        public void Dispose()
        {
            _listener?.Stop();
            _listener = null;
        }

        public async Task SendMessageAsync(string message, string clientId)
        {
            var client = _connectionsManager.GetClientById(clientId);
            if (client.Connected)
            {
                var byteData = Encoding.ASCII.GetBytes(message);
                await Task.Run(() => client.Client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(EndMessageSend), client.Client));
            }
        }

        private void EndMessageSend(IAsyncResult arg)
        {
            try
            {
                // Retrieve the socket from the state object.  
                var handler = (Socket)arg.AsyncState;

                // Complete sending the data to the remote device.  
                var bytesSent = handler.EndSend(arg);
                _logger?.LogDebug("{size} bytes sent to client...", bytesSent);

            }
            catch (Exception e)
            {
                _logger?.LogError(e, "EndMessageSend exception");
            }
        }

        private void AcceptClientCallback(IAsyncResult arg)
        {
            var clientId = _connectionsManager.AddClient(((TcpListener) arg.AsyncState)?.EndAcceptTcpClient(arg));
            _logger?.LogDebug("A client connected [{id}]...", clientId);
            ThreadPool.QueueUserWorkItem((o) => { ClientConnectionLoop(clientId); });
        }

        private void ClientConnectionLoop(string clientId)
        {
            var client = _connectionsManager.GetClientById(clientId);
            var stream = client.GetStream();

            // enter to an infinite cycle to be able to handle every change in stream
            while (client.Connected)
            {
                if (stream.DataAvailable && client.Available >= 3)
                {
                    Process(stream, client.Available, clientId);
                }
            }
        }

        private void Process(NetworkStream stream, int length, string clientId)
        {
            var bytes = new byte[length];
            stream.Read(bytes, 0, length);
            var s = Encoding.UTF8.GetString(bytes);

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                _logger?.LogDebug("=====Handshaking from client=====\n{0}", s);

                // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                // 3. Compute SHA-1 and Base64 hash of the new value
                // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                var swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                var swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                var swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                var swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                var response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);
            }
            else
            {
                bool fin = (bytes[0] & 0b10000000) != 0,
                    mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                    msglen = bytes[1] - 128, // & 0111 1111
                    offset = 2;

                if (msglen == 126)
                {
                    // was ToUInt16(bytes, offset) but the result is incorrect
                    msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                    offset = 4;
                }
                else if (msglen == 127)
                {
                    _logger?.LogDebug("TODO: msglen == 127, needs qword to store msglen");
                    // i don't really know the byte order, please edit this
                    // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                    // offset = 10;
                }

                if (msglen == 0)
                {
                    _logger?.LogDebug("msglen == 0");
                }
                else if (mask)
                {
                    var decoded = new byte[msglen];
                    var masks = new [] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                    offset += 4;

                    for (var i = 0; i < msglen; ++i)
                    {
                        decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);
                    }

                    var text = Encoding.UTF8.GetString(decoded);
                    _logger?.LogDebug("New message from [{id}]: {text}", clientId, text);
                    OnNewMessage?.Invoke(this, new WebSocketMessageEventArgs(clientId, text));
                }
                else
                {
                    _logger?.LogDebug("mask bit not set");
                }

                _logger?.LogDebug("...");
            }
        }
    }
}
