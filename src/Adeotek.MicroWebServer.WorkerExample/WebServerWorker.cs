using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer.WorkerExample
{
    class WebServerWorker : WorkerBase
    {
        private readonly ILogger _logger;
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
                crossDomains: new List<string>() { "*" },
                logger: _logger
            );
            if (autoStart)
            {
                Start();
            }
        }

        protected override void WorkerLoop()
        {
            try
            {
                if (!_webServer.IsRunning)
                {
                    _webServer.Start();
                }
                Working = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught starting worker loop [workerId:{_workerId}]");
                Stop(true);
            }
        }

        protected override void EndWorkerLoop()
        {
            try
            {
                _webServer?.Stop();
                Working = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught stopping worker loop [workerId:{_workerId}]");
                Stop(true);
            }
            
        }

        private string ProcessWebRequest(HttpListenerRequest request)
        {
            _logger.LogInformation("Request received: {url}", request.RawUrl);
            return "Hello world!";
        }
    }
}
