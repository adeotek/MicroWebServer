using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.Example
{
    class Program
    {
        static ILogger _logger;
        static void Main(string[] args)
        {
            ConsoleKeyInfo cki;

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddConsole();
            });
            _logger = loggerFactory.CreateLogger<Program>();
            _logger.LogInformation("Preparing the MicroWebServer...");

            var microWebServer = new MicroHttpServer(
                requestResponderMethod: ProcessWebRequest,
                routes: new List<string>() { "hello/" },
                host: "localhost",
                port: 8080,
                responseType: ResponseTypes.Text,
                utf8: true,
                allowedOrigin: "*",
                logger: _logger
                );
            microWebServer.Start();

            while (true)
            {
                Console.WriteLine("\nCTRL+C to stop the MicroWebServer and exit.");
                cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.C)
                {
                    break;
                }
            }
        }

        static string ProcessWebRequest(HttpListenerRequest request)
        {
            _logger.LogInformation("Request received: {url}", request.RawUrl);
            return "Hello world!";
        }
    }
}
