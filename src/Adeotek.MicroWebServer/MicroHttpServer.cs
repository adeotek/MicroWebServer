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

        public ResponseTypes ResponseType { get; set; }
        public bool UTF8 { get; set; }
        public ICollection<string> CrossDomains { get; set; }
        public bool SendChunked { get; set; }
        public bool IsRunning { get; private set; }

        public MicroHttpServer(
            Func<HttpListenerRequest, string> requestResponderMethod,
            IReadOnlyCollection<string> routes = null,
            string host = "localhost",
            int port = 80,
            ResponseTypes responseType = ResponseTypes.Text,
            bool utf8 = true,
            ICollection<string> crossDomains = null,
            bool sendChunked = false,
            IReadOnlyCollection<string> fullRoutes = null,
            ILogger logger = null
            )
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("HttpListener is not supported.");
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
                    var route = $"http://{host}:{port}/{r.TrimStart('/')}";
                    if (prefixes.Contains(route))
                    {
                        continue;
                    }
                    prefixes.Add(route);
                }
            }
            else
            {
                prefixes.Add($"http://{host}:{port}/");
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
            foreach (var s in prefixes.Where(s => !string.IsNullOrEmpty(s)))
            {
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
            _logger?.LogDebug("Web server is starting...");
            try
            {
                _listener.Start();
                IsRunning = true;
                if (_logger != null)
                {
                    try
                    {
                        _logger.LogInformation("Web server is listening on: {routes}", string.Join("\n\t", _listener.Prefixes.ToArray<string>()));
                    }
                    catch
                    {
                        // suppress exceptions
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
                                var responseString = _responderMethod(ctx.Request);
                                var buf = Encoding.UTF8.GetBytes(responseString);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch (Exception exx)
                            {
                                // suppress any exceptions and send it to logger object
                                _logger?.LogWarning(exx, "Exception cauth and supressed while listening.");
                            }
                            finally
                            {
                                // always close the stream
                                ctx?.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch (Exception ex)
                {
                    // suppress any exceptions and send it to logger object
                    _logger?.LogWarning(ex, "Exception caught and suppressed.");
                }
            });
        }

        public void Stop()
        {
            _logger?.LogDebug("Web server is stopping...");
            IsRunning = false;
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
            _listener.Close();
            _logger?.LogInformation("Web server not listening anymore!");
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
            var charset = string.Empty;
            if (UTF8)
            {
                charset = "; charset=utf-8";
                context.Response.ContentEncoding = Encoding.UTF8;
            }
            else if (CrossDomains != null && CrossDomains.Count > 0)
            {
                foreach (var d in CrossDomains.Where(d => !string.IsNullOrEmpty(d)))
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin: " + d + ";");
                }
            }

            context.Response.ContentType = ResponseType switch
            {
                ResponseTypes.Text => "text/plain" + charset,
                ResponseTypes.Html => "text/html" + charset,
                ResponseTypes.Json => "application/json" + charset,
                ResponseTypes.Jsonp => "application/json" + charset,
                _ => context.Response.ContentType
            };
            context.Response.SendChunked = SendChunked;
        }
    }
}
