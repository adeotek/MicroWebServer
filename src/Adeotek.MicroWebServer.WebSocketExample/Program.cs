using System;
using System.Text;
using Adeotek.MicroWebServer.WebSocket;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.WebSocketExample
{
    class Program
    {
        static ILogger _logger;

        static void Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddConsole();
            });
            _logger = loggerFactory.CreateLogger<Program>();

            _logger.LogInformation("Logger started...");

            // WebSocket server port
            var ip = "127.0.0.1";
            var port = 8080;

            // Create a new WebSocket server
            //var server = new WebSocketServer("127.0.0.1", port, logger: logger);
            //server.OnMessageReceived += OnMessageReceived;
            var server = new Server(ip, port);
            server.OnMessageReceived += OnWsReceived;
            server.OnServerError += OnError;
            server.OnSessionError += OnError;
            server.OnSessionConnected += OnWsConnected;
            server.OnSessionDisconnected += OnWsDisconnected;

            Console.WriteLine($"WebSocket server website: http://{ip}:{port}/");
            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Server starting done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");
            Console.WriteLine();

            // Perform text input
            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                }

                // Multicast admin message to all sessions
                line = "(server) " + line;
                server.BroadcastText(line);
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");
        }

        private static void OnWsConnected(object sender, ConnectionEventArgs e)
        {
            _logger?.LogDebug("WebSocket session [{id}] connected!", e.SessionId);
        }

        private static void OnWsDisconnected(object sender, ConnectionEventArgs e)
        {
            _logger?.LogDebug("WebSocket session [{id}] disconnected!", e.SessionId);
        }

        private static void OnWsReceived(object sender, RawMessageEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Buffer, (int)e.Offset, (int)e.Size);
            _logger?.LogDebug("WebSocket session [{id}] message received: {msg}", e.SessionId, message);
            //((BasicWsSession) sender).SendAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private static void OnError(object sender, SocketErrorEventArgs e)
        {
            _logger?.LogDebug("WebSocket session [{id}] error ({msg}): {err}", e.SessionId, e.Message ?? string.Empty, e.Error);
        }
    }
}
