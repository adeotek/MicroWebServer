using System;
using Adeotek.Worker;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.WorkerExample
{
    class GenericWorker : WorkerBase
    {
        public GenericWorker(ILogger logger, int workerId, bool autoStart = false)
        {
            _workerId = workerId;
            _logger = logger;
            if (autoStart)
            {
                Start();
            }
        }

        protected override bool ExecuteJob()
        {
            _logger.LogDebug("{type} [workerId:{id}] - ExecuteJob triggered...", GetType().Name, _workerId);

            var maxI = 10;
            for (var i = 0; i < maxI; i++)
            {
                //Thread.Sleep(800);

                var r = (new Random()).Next(1, 4);
                var m = $"Test text {i} of {maxI}...";
                if (r == 3)
                    _logger.LogWarning(m);
                else if (r == 4)
                    _logger.LogError(m);
                else
                    _logger.LogDebug(m);
            }

            _logger.LogDebug("{type} [workerId:{id}] - ExecuteJob finished...", GetType().Name, _workerId);

            // _logger.LogInformation($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Working and finalizing the job!");
            return true;
        }
    }
}
