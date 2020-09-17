using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.WorkerExample
{
    class WebServerWorker : WorkerBase
    {
        private readonly MicroHttpServer _webServer;

        public WebServerWorker(ILogger logger, int workerId, bool autoStart = false)
        {
            _workerId = workerId;
            _logger = logger;
            _webServer = new MicroHttpServer(
                requestResponderMethod: ProcessWebRequest,
                routes: new List<string>() { "hello/" },
                host: "localhost",
                port: 8080,
                responseType: ResponseTypes.Text,
                utf8: true,
                allowedOrigin: "*",
                logger: _logger
            );
            if (autoStart)
            {
                Start();
            }
        }

        public override bool IsRunning => (_webServer?.IsRunning ?? false) || _starting;

        protected override void WorkerLoop()
        {
            try
            {
                if (!_webServer.IsRunning)
                {
                    _webServer.Start();
                }
                OnStart();
                IsWorking = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught starting worker loop [workerId:{_workerId}]");
                Stop(false, true);
            }
        }

        protected override void EndWorkerLoop()
        {
            try
            {
                _webServer?.Stop();
                IsWorking = false;
                OnStop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught stopping worker loop [workerId:{_workerId}]");
                Stop(false, true);
            }
            
        }

        private string ProcessWebRequest(HttpListenerRequest request)
        {
            _logger.LogInformation("Request received: {url}", request.RawUrl);
            return "Hello world!";
        }
    }
}
