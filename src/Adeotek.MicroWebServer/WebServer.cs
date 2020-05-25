using System;
using System.Collections.Generic;
using System.Linq;
using Adeotek.MicroWebServer.Network;
using Microsoft.Extensions.Logging;

namespace Adeotek.MicroWebServer
{
    public class WebServer : TcpServer
    {
        private readonly ILogger _logger;
        private readonly List<string> _routes;

        public ResponseTypes ResponseType { get; set; }
        public bool Utf8 { get; set; }
        public ICollection<string> CrossDomains { get; set; }

        public WebServer(
            IReadOnlyCollection<string> routes = null,
            string host = "localhost",
            string ipAddress = "127.0.0.1",
            int port = 80,
            ResponseTypes responseType = ResponseTypes.Text,
            bool utf8 = true,
            ICollection<string> crossDomains = null,
            IReadOnlyCollection<string> fullRoutes = null,
            ILogger logger = null
            ) : base(ipAddress, port)
        {
            _logger = logger;
            ResponseType = responseType;
            Utf8 = utf8;
            CrossDomains = crossDomains;
            Initialize(host, port, routes, fullRoutes);
        }

        private void Initialize(string host, int port, IReadOnlyCollection<string> routes, IReadOnlyCollection<string> fullRoutes)
        {
            var prefixes = new List<string>();
            if (routes != null && routes.Count > 0)
            {
                foreach (var r in routes)
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
            
            foreach (var s in prefixes.Where(s => !string.IsNullOrEmpty(s)))
            {
                _routes.Add(s);
            }
            
        }
    }
}
