using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Adeotek.EmbeddedWebServer.Common;
using Microsoft.Extensions.Logging;
using NetCoreWssSession = NetCoreServer.WssSession;
using NetCoreWssServer = NetCoreServer.WssServer;

namespace Adeotek.EmbeddedWebServer.WebSocketExample
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

            X509Certificate2 certificate = null;
            // Load certificate from disk
            // var certificateFile = "ecr-cert.pfx";
            var certificatePassword = "";
            var certificateFile = "server.pfx";
            // var certificateFile = "ncs-server.pfx";
            // var certificatePassword = "qwerty";
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, certificateFile)))
            {
                certificate = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, certificateFile), certificatePassword);
            }

            // Create a new WebSocket server
            var server = new WebSocketServer(
                ipAddress: IPAddress.Any.ToString(),
                port: 8080,
                certificate: certificate,
                messageConsumerMethod: ProcessWebRequest,
                logger: _logger
            );
            server.OnSessionConnected += OnWsSessionConnected;
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

            server.Stop();
            server.Dispose();
        }

        static void ProcessWebRequest(IWebSocketSession session, string message)
        {
            _logger.LogInformation("Message received from [{Id}]: {Message}", session.Id.ToString(), message);
            var response = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            session.SendTextAsync(response);
        }

        static void OnWsSessionConnected(object sender, ConnectionStateEventArgs e)
        {
            _logger?.LogDebug("[MAIN] WebSocket session [{Id}] connected!", e.SessionId.ToString());
        }
    }
}