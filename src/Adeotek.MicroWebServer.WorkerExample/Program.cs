using System;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.WorkerExample
{
    class Program
    {
        static ILogger logger;
        //private static Server worker;

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

            logger.LogInformation("Preparing the Worker...");

            //var worker = new WebServerWorker(logger, 1);
            //worker.Start();

            //var worker = new GenericWorker(logger, 2) { RunInterval = 5.5 };
            //worker.OnNewMessage += (sender, message) =>
            //{
            //    logger.LogInformation(message);
            //};

            //var worker = new Server("127.0.0.1", 8080, logger);
            //worker.OnNewMessage += OnMessageHandler;

            //var worker = new WebSocketServer("127.0.0.1", 8080, logger);

            //logger.LogInformation($"Starting {worker.GetType().Name}. \nCTRL+S to stop the Worker and exit.");
            //worker.Start();

            //while (worker.IsRunning)
            //{
            //    cki = Console.ReadKey(true);
            //    if (cki.Key != ConsoleKey.S)
            //    {
            //        continue;
            //    }
            //    logger.LogInformation("Initiating Worker.Stop()...");
            //    //worker.Stop(true);
            //    break;
            //}
        }


        //public static void OnMessageHandler(object sender, WebSocketMessageEventArgs e)
        //{
        //    logger.LogInformation("New message received from [{id}]: \n{text}", e.ClientId, e.Message);
        //    worker.SendMessageAsync($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {e.Message}", e.ClientId);
        //}

    }
}
