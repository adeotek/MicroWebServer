using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer
{
    public class WebSocketServer1 : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _ipAddress;
        private readonly int _port;
        //private readonly ConnectionsManager _connectionsManager;
        private Socket _listener;
        public static ManualResetEvent ListenerState = new ManualResetEvent(false);

        //public delegate void NewMessage(object sender, WebSocketMessageEventArgs e);
        //public event NewMessage OnNewMessage;
        public bool IsRunning { get; private set; }

        public WebSocketServer1(
            //Func<HttpListenerRequest, string> requestResponderMethod,
            string ipAddress = "127.0.0.1",
            int port = 8080,
            ILogger logger = null)
        {
            _logger = logger;
            _ipAddress = ipAddress;
            _port = port;
            //_connectionsManager = new ConnectionsManager();
        }

        //public void Start()
        //{
        //    _logger?.LogDebug("WebSocket server is starting...");
        //    try
        //    {
        //        var ipAddress = IPAddress.Parse(_ipAddress);
        //        _listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        //        _listener.Bind(new IPEndPoint(ipAddress, _port));
        //        _listener.Listen(100);
        //        IsRunning = true;
        //        _logger?.LogInformation("WebSocket server is listening on: {ip}:{port}", _ipAddress, _port);
        //        ThreadPool.QueueUserWorkItem((o) =>
        //        {
        //            while (IsRunning)
        //            {
        //                // Set the event to nonsignaled state.  
        //                ListenerState.Reset();

        //                // Start an asynchronous socket to listen for connections.  
        //                Console.WriteLine("Waiting for a connection...");
        //                _logger?.LogDebug("Waiting for a connection...");
        //                _listener.BeginAccept(new AsyncCallback(AcceptCallback), _listener);

        //                // Wait until a connection is made before continuing.  
        //                ListenerState.WaitOne();
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        IsRunning = false;
        //        _listener?.Dispose();
        //        _listener = null;
        //        throw ex;
        //    }
        //}

        //public void Stop()
        //{
        //    _logger?.LogDebug("WebSocket server is stopping...");
        //    IsRunning = false;
        //    _listener?.Dispose();
        //    _listener = null;
        //    _logger?.LogInformation("WebSocket server not listening anymore!");
        //}

        public void Dispose()
        {
            _listener?.Dispose();
            _listener = null;
        }

        //private void AcceptCallback(IAsyncResult ar)
        //{
        //    // Signal the main thread to continue.  
        //    allDone.Set();

        //    // Get the socket that handles the client request.  
        //    Socket listener = (Socket)ar.AsyncState;
        //    Socket handler = listener.EndAccept(ar);

        //    // Create the state object.  
        //    StateObject state = new StateObject();
        //    state.workSocket = handler;
        //    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
        //        new AsyncCallback(ReadCallback), state);
        //}

        //private void ReadCallback(IAsyncResult ar)
        //{
        //    String content = String.Empty;

        //    // Retrieve the state object and the handler socket  
        //    // from the asynchronous state object.  
        //    StateObject state = (StateObject)ar.AsyncState;
        //    Socket handler = state.workSocket;

        //    // Read data from the client socket.
        //    int bytesRead = handler.EndReceive(ar);

        //    if (bytesRead > 0)
        //    {
        //        // There  might be more data, so store the data received so far.  
        //        state.sb.Append(Encoding.ASCII.GetString(
        //            state.buffer, 0, bytesRead));

        //        // Check for end-of-file tag. If it is not there, read
        //        // more data.  
        //        content = state.sb.ToString();
        //        if (content.IndexOf("<EOF>") > -1)
        //        {
        //            // All the data has been read from the
        //            // client. Display it on the console.  
        //            Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
        //                content.Length, content);
        //            // Echo the data back to the client.  
        //            Send(handler, content);
        //        }
        //        else
        //        {
        //            // Not all data received. Get more.  
        //            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
        //            new AsyncCallback(ReadCallback), state);
        //        }
        //    }
        //}

        //private void Send(Socket handler, string data)
        //{
        //    // Convert the string data to byte data using ASCII encoding.  
        //    var byteData = Encoding.ASCII.GetBytes(data);
        //    // Begin sending the data to the remote device.  
        //    handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        //}

        //private void SendCallback(IAsyncResult ar)
        //{
        //    try
        //    {
        //        // Retrieve the socket from the state object.  
        //        var handler = (Socket)ar.AsyncState;
        //        // Complete sending the data to the remote device.  
        //        var bytesSent = handler.EndSend(ar);
        //        _logger?.LogDebug("{b} bytes sent to client.", bytesSent);
        //        handler.Shutdown(SocketShutdown.Both);
        //        handler.Close();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }
        //}
    }
}
