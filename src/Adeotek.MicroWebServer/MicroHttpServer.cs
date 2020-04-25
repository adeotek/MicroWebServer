using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer
{
    public enum ResponseTypes
    {
        Text = 0,
        Html = 1,
        Json = 2,
        Jsonp = 3
    }

    public class MicroHttpServer : IDisposable
    {
        private ILogger _logger;
        private HttpListener _listener;
        private Func<HttpListenerRequest, string> _responderMethod;

        public ResponseTypes ResponseType;
        public bool UTF8;
        public List<string> CrossDomains;
        public bool SendChunked;
        public bool IsRunning { get; private set; } = false;


        public MicroHttpServer(
            Func<HttpListenerRequest, string> requestResponderMethod,
            List<string> routes = null,
            string host = "localhost",
            int port = 80,
            ResponseTypes responseType = ResponseTypes.Text,
            bool utf8 = true,
            List<string> crossDomains = null,
            bool sendChunked = false,
            List<string> fullRoutes = null,
            ILogger logger = null
            )
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("HttpListner is not supported.");
            }
            _logger = logger;
            var prefixes = new List<string>();
            if (routes != null && routes.Count > 0)
            {
                foreach(var r in routes)
                {
                    if (string.IsNullOrEmpty(r))
                    {
                        continue;
                    }
                    var route = string.Format("http://{0}:{1}/{2}", host, port, r.TrimStart('/'));
                    if (prefixes.Contains(route))
                    {
                        continue;
                    }
                    prefixes.Add(route);
                }
            }
            else
            {
                prefixes.Add(string.Format("http://{0}:{1}/", host, port));
            }
            if (fullRoutes != null && fullRoutes.Count > 0)
            {
                foreach (var r in fullRoutes)
                {
                    if (string.IsNullOrEmpty(r) || prefixes.Contains(r))
                    {
                        continue;
                    }
                    prefixes.Add(r);
                }
            }

            // At least one URI prefix is required (e.g. "http://localhost:8080/index/").
            if (prefixes == null || prefixes.Count == 0)
            {
                throw new ArgumentException("No URI prefixes provided.");
            }
            _listener = new HttpListener();
            foreach (var s in prefixes)
            {
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }
                _listener.Prefixes.Add(s);
            }
            _responderMethod = requestResponderMethod ?? throw new ArgumentException("Invalid request responder method.");
            ResponseType = responseType;
            UTF8 = utf8;
            CrossDomains = crossDomains;
            SendChunked = sendChunked;
        }

        public void Start()
        {
            if (_logger != null)
            {
                _logger.LogDebug("Webserver is starting...");
            }
            try
            {
                _listener.Start();
                IsRunning = true;
                if (_logger != null)
                {
                    try
                    {
                        _logger.LogInformation("Webserver is listening on: {routes}", string.Join("\n\t", _listener.Prefixes.ToArray<string>()));
                    }
                    catch
                    {
                        // supress exceptions
                    }
                }
            }
            catch (Exception ex)
            {
                IsRunning = false;
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
                throw ex;
            }
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                // Set response custom headers
                                ProcessResponseHeaders(ref ctx);
                                string rstr = _responderMethod(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch (Exception exx)
                            {
                                // suppress any exceptions and send it to logger object
                                if (_logger != null)
                                {
                                    _logger.LogWarning(exx, "Exception cauth and supressed while listening.");
                                }
                            }
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch (Exception ex)
                {
                    // suppress any exceptions and send it to logger object
                    if (_logger != null)
                    {
                        _logger.LogWarning(ex, "Exception cauth and supressed.");
                    }
                }
            });
        }

        public void Stop()
        {
            if (_logger != null)
            {
                _logger.LogDebug("Webserver is stopping...");
            }
            IsRunning = false;
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
            _listener.Close();
            if (_logger != null)
            {
                _logger.LogInformation("Webserver not listening anymore!");
            }
        }

        public virtual void Dispose()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
            _listener.Close();
            _listener = null;
            _responderMethod = null;
        }

        protected virtual void ProcessResponseHeaders(ref HttpListenerContext context)
        {
            if (context == null)
            {
                return;
            }
            string charset = string.Empty;
            if (UTF8)
            {
                charset = "; charset=utf-8";
                context.Response.ContentEncoding = Encoding.UTF8;
            }
            else if (CrossDomains != null && CrossDomains.Count > 0)
            {
                foreach (var d in CrossDomains)
                {
                    if (string.IsNullOrEmpty(d))
                    {
                        continue;
                    }
                    context.Response.Headers.Add("Access-Control-Allow-Origin: " + d + ";");
                }
            }
            switch (ResponseType)
            {
                case ResponseTypes.Text:
                    context.Response.ContentType = "text/plain" + charset;
                    break;
                case ResponseTypes.Html:
                    context.Response.ContentType = "text/html" + charset;
                    break;
                case ResponseTypes.Json:
                    context.Response.ContentType = "application/json" + charset;
                    break;
                case ResponseTypes.Jsonp:
                    context.Response.ContentType = "application/json" + charset;
                    break;
            }
            context.Response.SendChunked = SendChunked;
        }
    }
}
