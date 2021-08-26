using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Adeotek.MicroWebServer.WebSocket
{
    /// <summary>
    ///     HTTP response is used to create or process parameters of HTTP protocol response(status, headers, etc).
    /// </summary>
    /// <remarks>Not thread-safe.</remarks>
    public class Response
    {
        // HTTP response body
        private int _bodyIndex;
        private int _bodyLength;
        private int _bodySize;

        // HTTP response headers
        private readonly List<Tuple<string, string>> _headers = new List<Tuple<string, string>>();

        /// <summary>
        ///     Initialize an empty HTTP response
        /// </summary>
        public Response()
        {
            Clear();
        }

        /// <summary>
        ///     Is the HTTP response error flag set?
        /// </summary>
        public bool IsErrorSet { get; private set; }

        /// <summary>
        ///     Get the HTTP response status
        /// </summary>
        public int Status { get; private set; }

        /// <summary>
        ///     Get the HTTP response status phrase
        /// </summary>
        public string StatusPhrase { get; private set; }

        /// <summary>
        ///     Get the HTTP response protocol version
        /// </summary>
        public string Protocol { get; private set; }

        /// <summary>
        ///     Get the HTTP response headers count
        /// </summary>
        public long Headers => _headers.Count;

        /// <summary>
        ///     Get the HTTP response body as string
        /// </summary>
        public string Body => Cache.ExtractString(_bodyIndex, _bodySize);

        /// <summary>
        ///     Get the HTTP response body length
        /// </summary>
        public long BodyLength => _bodyLength;

        /// <summary>
        ///     Get the HTTP response cache content
        /// </summary>
        public Buffer Cache { get; } = new Buffer();

        /// <summary>
        ///     Get the HTTP response header by index
        /// </summary>
        public Tuple<string, string> Header(int i)
        {
            Debug.Assert(i < _headers.Count, "Index out of bounds!");
            if (i >= _headers.Count)
            {
                return new Tuple<string, string>("", "");
            }

            return _headers[i];
        }

        /// <summary>
        ///     Get string from the current HTTP response
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Status: {Status}");
            sb.AppendLine($"Status phrase: {StatusPhrase}");
            sb.AppendLine($"Protocol: {Protocol}");
            sb.AppendLine($"Headers: {Headers}");
            for (var i = 0; i < Headers; ++i)
            {
                var header = Header(i);
                sb.AppendLine($"{header.Item1} : {header.Item2}");
            }

            sb.AppendLine($"Body: {BodyLength}");
            sb.AppendLine(Body);
            return sb.ToString();
        }

        /// <summary>
        ///     Clear the HTTP response cache
        /// </summary>
        public Response Clear()
        {
            IsErrorSet = false;
            Status = 0;
            StatusPhrase = "";
            Protocol = "";
            _headers.Clear();
            _bodyIndex = 0;
            _bodySize = 0;
            _bodyLength = 0;

            return this;
        }

        /// <summary>
        ///     Set the HTTP response begin with a given status and protocol
        /// </summary>
        /// <param name="status">HTTP status</param>
        /// <param name="protocol">Protocol version (default is "HTTP/1.1")</param>
        public Response SetBegin(int status, string protocol = "HTTP/1.1")
        {
            string statusPhrase;

            switch (status)
            {
                case 100:
                    statusPhrase = "Continue";
                    break;
                case 101:
                    statusPhrase = "Switching Protocols";
                    break;
                case 102:
                    statusPhrase = "Processing";
                    break;
                case 103:
                    statusPhrase = "Early Hints";
                    break;

                case 200:
                    statusPhrase = "OK";
                    break;
                case 201:
                    statusPhrase = "Created";
                    break;
                case 202:
                    statusPhrase = "Accepted";
                    break;
                case 203:
                    statusPhrase = "Non-Authoritative Information";
                    break;
                case 204:
                    statusPhrase = "No Content";
                    break;
                case 205:
                    statusPhrase = "Reset Content";
                    break;
                case 206:
                    statusPhrase = "Partial Content";
                    break;
                case 207:
                    statusPhrase = "Multi-Status";
                    break;
                case 208:
                    statusPhrase = "Already Reported";
                    break;

                case 226:
                    statusPhrase = "IM Used";
                    break;

                case 300:
                    statusPhrase = "Multiple Choices";
                    break;
                case 301:
                    statusPhrase = "Moved Permanently";
                    break;
                case 302:
                    statusPhrase = "Found";
                    break;
                case 303:
                    statusPhrase = "See Other";
                    break;
                case 304:
                    statusPhrase = "Not Modified";
                    break;
                case 305:
                    statusPhrase = "Use Proxy";
                    break;
                case 306:
                    statusPhrase = "Switch Proxy";
                    break;
                case 307:
                    statusPhrase = "Temporary Redirect";
                    break;
                case 308:
                    statusPhrase = "Permanent Redirect";
                    break;

                case 400:
                    statusPhrase = "Bad Request";
                    break;
                case 401:
                    statusPhrase = "Unauthorized";
                    break;
                case 402:
                    statusPhrase = "Payment Required";
                    break;
                case 403:
                    statusPhrase = "Forbidden";
                    break;
                case 404:
                    statusPhrase = "Not Found";
                    break;
                case 405:
                    statusPhrase = "Method Not Allowed";
                    break;
                case 406:
                    statusPhrase = "Not Acceptable";
                    break;
                case 407:
                    statusPhrase = "Proxy Authentication Required";
                    break;
                case 408:
                    statusPhrase = "Request Timeout";
                    break;
                case 409:
                    statusPhrase = "Conflict";
                    break;
                case 410:
                    statusPhrase = "Gone";
                    break;
                case 411:
                    statusPhrase = "Length Required";
                    break;
                case 412:
                    statusPhrase = "Precondition Failed";
                    break;
                case 413:
                    statusPhrase = "Payload Too Large";
                    break;
                case 414:
                    statusPhrase = "URI Too Long";
                    break;
                case 415:
                    statusPhrase = "Unsupported Media Type";
                    break;
                case 416:
                    statusPhrase = "Range Not Satisfiable";
                    break;
                case 417:
                    statusPhrase = "Expectation Failed";
                    break;

                case 421:
                    statusPhrase = "Misdirected Request";
                    break;
                case 422:
                    statusPhrase = "Unprocessable Entity";
                    break;
                case 423:
                    statusPhrase = "Locked";
                    break;
                case 424:
                    statusPhrase = "Failed Dependency";
                    break;
                case 425:
                    statusPhrase = "Too Early";
                    break;
                case 426:
                    statusPhrase = "Upgrade Required";
                    break;
                case 427:
                    statusPhrase = "Unassigned";
                    break;
                case 428:
                    statusPhrase = "Precondition Required";
                    break;
                case 429:
                    statusPhrase = "Too Many Requests";
                    break;
                case 431:
                    statusPhrase = "Request Header Fields Too Large";
                    break;

                case 451:
                    statusPhrase = "Unavailable For Legal Reasons";
                    break;

                case 500:
                    statusPhrase = "Internal Server Error";
                    break;
                case 501:
                    statusPhrase = "Not Implemented";
                    break;
                case 502:
                    statusPhrase = "Bad Gateway";
                    break;
                case 503:
                    statusPhrase = "Service Unavailable";
                    break;
                case 504:
                    statusPhrase = "Gateway Timeout";
                    break;
                case 505:
                    statusPhrase = "HTTP Version Not Supported";
                    break;
                case 506:
                    statusPhrase = "Variant Also Negotiates";
                    break;
                case 507:
                    statusPhrase = "Insufficient Storage";
                    break;
                case 508:
                    statusPhrase = "Loop Detected";
                    break;

                case 510:
                    statusPhrase = "Not Extended";
                    break;
                case 511:
                    statusPhrase = "Network Authentication Required";
                    break;

                default:
                    statusPhrase = "Unknown";
                    break;
            }

            SetBegin(status, statusPhrase, protocol);
            return this;
        }

        /// <summary>
        ///     Set the HTTP response begin with a given status, status phrase and protocol
        /// </summary>
        /// <param name="status">HTTP status</param>
        /// <param name="statusPhrase"> HTTP status phrase</param>
        /// <param name="protocol">Protocol version</param>
        public Response SetBegin(int status, string statusPhrase, string protocol)
        {
            // Clear the HTTP response cache
            Clear();

            // Append the HTTP response protocol version
            Cache.Append(protocol);
            Protocol = protocol;

            Cache.Append(" ");

            // Append the HTTP response status
            Cache.Append(status.ToString());
            Status = status;

            Cache.Append(" ");

            // Append the HTTP response status phrase
            Cache.Append(statusPhrase);
            StatusPhrase = statusPhrase;

            Cache.Append("\r\n");
            return this;
        }

        /// <summary>
        ///     Set the HTTP response header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="value">Header value</param>
        public Response SetHeader(string key, string value)
        {
            // Append the HTTP response header's key
            Cache.Append(key);

            Cache.Append(": ");

            // Append the HTTP response header's value
            Cache.Append(value);

            Cache.Append("\r\n");

            // Add the header to the corresponding collection
            _headers.Add(new Tuple<string, string>(key, value));
            return this;
        }

        /// <summary>
        ///     Set the HTTP response body
        /// </summary>
        /// <param name="body">Body string content (default is "")</param>
        public Response SetBody(string body = "")
        {
            var length = string.IsNullOrEmpty(body) ? 0 : Encoding.UTF8.GetByteCount(body);

            // Append content length header
            SetHeader("Content-Length", length.ToString());

            Cache.Append("\r\n");

            var index = (int)Cache.Size;

            // Append the HTTP response body
            Cache.Append(body);
            _bodyIndex = index;
            _bodySize = length;
            _bodyLength = length;
            return this;
        }

        /// <summary>
        ///     Make ERROR response
        /// </summary>
        /// <param name="error">Error content (default is "")</param>
        /// <param name="status">OK status (default is 200 (OK))</param>
        public Response MakeErrorResponse(string error = "", int status = 500)
        {
            Clear();
            SetBegin(status);
            SetHeader("Content-Type", "text/html; charset=UTF-8");
            SetBody(error);
            return this;
        }
    }
}