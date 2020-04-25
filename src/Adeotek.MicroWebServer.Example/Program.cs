using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.Example
{
    class Program
    {
        static ILogger logger;
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
            logger = loggerFactory.CreateLogger<Program>();

            logger.LogInformation("Preparing the MicroWebServer...");

            var microWebServer = new MicroHttpServer(
                requestResponderMethod: ProcessWebRequest,
                routes: new List<string>() { "hello/" },
                host: "localhost",
                port: 8080,
                responseType: ResponseTypes.Text,
                utf8: true,
                crossDomains: new List<string>() { "*" },
                logger: logger
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

        public static string ProcessWebRequest(HttpListenerRequest request)
        {
            logger.LogInformation("Request received: {url}", request.RawUrl);
            return "Hello world!";
        }
    }
}
