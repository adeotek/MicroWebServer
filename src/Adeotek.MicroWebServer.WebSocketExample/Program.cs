using System;
using Adeotek.MicroWebServer.Network;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.WebSocketExample
{
    class Program
    {
        static ILogger logger;

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
            logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation("Logger started...");

            // WebSocket server port
            int port = 8080;
            if (args.Length > 0)
                port = int.Parse(args[0]);
            // WebSocket server content path
            string www = "../../../../../www/ws";
            if (args.Length > 1)
                www = args[1];

            Console.WriteLine($"WebSocket server port: {port}");
            Console.WriteLine($"WebSocket server static content path: {www}");
            Console.WriteLine($"WebSocket server website: http://localhost:{port}/chat/index.html");

            Console.WriteLine();

            // Create a new WebSocket server
            var server = new WebSocketServer("127.0.0.1", port, logger: logger);
            server.OnMessageReceived += OnMessageReceived;
            //server.AddStaticContent(www, "/chat");

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

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
                server.MulticastText(line);
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");
        }

        private static void OnMessageReceived(object sender, WsMessageEventArgs e)
        {
            //((BasicWsSession) sender).SendAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
