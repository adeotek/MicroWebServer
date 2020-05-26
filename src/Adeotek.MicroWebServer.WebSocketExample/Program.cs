using System;
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

            // Create a new WebSocket server
            var server = new WebSocketServer(
                ipAddress: "127.0.0.1",
                port: 8080,
                messageConsumerMethod: ProcessWebRequest,
                logger: _logger
            );
            server.Start();

            _logger.LogInformation("Press Enter to stop the server or '!' to restart the server...");
            // Perform text input
            while (true)
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    break;
                }
                if (line == "!")
                {
                    server.Restart();
                }

                // Broadcast server message to all sessions
                line = "(server) " + line;
                server.Broadcast(line);
            }
            server.Dispose();
        }

        static void ProcessWebRequest(WsSession session, string message)
        {
            _logger.LogInformation("Message received from [{id}]: {msg}", session.Id, message);
            session.SendAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
