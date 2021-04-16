using System;
using Adeotek.MicroWebServer.WebSocket;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.WorkerExample
{
    class WebSocketServerWorker : WorkerBase
    {
        private readonly WebSocketServer _wsServer;

        public WebSocketServerWorker(ILogger logger, int workerId, bool autoStart = false)
        {
            _workerId = workerId;
            _logger = logger;
            _wsServer = new WebSocketServer(
                ipAddress: "127.0.0.1",
                port: 8080,
                messageConsumerMethod: ProcessWsMessage,
                logger: _logger
            );
            if (autoStart)
            {
                Start();
            }
        }

        public override bool IsRunning => (_wsServer?.IsRunning ?? false) || _starting;

        protected override void WorkerLoop()
        {
            try
            {
                if (!_wsServer.IsRunning)
                {
                    _wsServer.Start();
                }
                OnStart();
                IsWorking = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught starting worker loop [workerId:{_workerId}]");
                Stop();
            }
        }

        protected override void EndWorkerLoop()
        {
            try
            {
                _wsServer?.Stop();
                IsWorking = false;
                OnStop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught stopping worker loop [workerId:{_workerId}]");
                Stop();
            }

        }

        private void ProcessWsMessage(WsSession session, string message)
        {
            _logger.LogInformation("Message received from [{id}]: {msg}", session.Id, message);
            session.SendAsync(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
