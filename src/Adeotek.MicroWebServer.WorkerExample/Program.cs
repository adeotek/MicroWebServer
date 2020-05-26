using System;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.WorkerExample
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
            _logger.LogInformation("Preparing the Worker...");


            //// Generic Worker example
            var worker = new GenericWorker(_logger, 1) { RunInterval = 5.5 };
            worker.OnWorkerJobExecuted += (sender, e) =>
            {
                _logger.LogInformation("Job done with result: ", e.Result.ToString());
            };
            worker.Start();


            //// MicroHttpServer Worker example
            //var worker = new WebServerWorker(_logger, 2);
            //worker.Start();


            //// WebSocketServer Worker example
            //var worker = new WebSocketServerWorker(_logger, 3);
            //worker.Start();


            _logger.LogInformation($"Worker [{worker.GetType().Name}] started. \nCTRL+S to stop the Worker and exit.");
            while (worker.IsRunning)
            {
                cki = Console.ReadKey(true);
                if (cki.Key != ConsoleKey.S)
                {
                    continue;
                }
                _logger.LogInformation("Initiating Worker.Stop()...");
                worker.Stop(false,true);
                break;
            }
        }
    }
}
