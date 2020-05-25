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
        private bool _bodyLengthProvided;
        private int _bodySize;

        // HTTP response cache

        private int _cacheSize;

        // HTTP response headers
        private readonly List<Tuple<string, string>> _headers = new List<Tuple<string, string>>();

        // HTTP response protocol

        // HTTP response status phrase

        /// <summary>
        ///     Initialize an empty HTTP response
        /// </summary>
        public Response()
        {
            Clear();
        }

        /// <summary>
        ///     Initialize a new HTTP response with a given status and protocol
        /// </summary>
        /// <param name="status">HTTP status</param>
        /// <param name="protocol">Protocol version (default is "HTTP/1.1")</param>
        public Response(int status, string protocol = "HTTP/1.1")
        {
            SetBegin(status, protocol);
        }

        /// <summary>
        ///     Initialize a new HTTP response with a given status, status phrase and protocol
        /// </summary>
        /// <param name="status">HTTP status</param>
        /// <param name="statusPhrase">HTTP status phrase</param>
        /// <param name="protocol">Protocol version</param>
        public Response(int status, string statusPhrase, string protocol)
        {
            SetBegin(status, statusPhrase, protocol);
        }

        /// <summary>
        ///     Is the HTTP response empty?
        /// </summary>
        public bool IsEmpty => Cache.Size > 0;

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
        ///     Get the HTTP request body as byte array
        /// </summary>
        public byte[] BodyBytes => Cache.Data[_bodyIndex..(_bodyIndex + _bodySize)];

        /// <summary>
        ///     Get the HTTP request body as read-only byte span
        /// </summary>
        public ReadOnlySpan<byte> BodySpan => new ReadOnlySpan<byte>(Cache.Data, _bodyIndex, _bodySize);

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
                return new Tuple<string, string>("", "");

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
            _bodyLengthProvided = false;

            Cache.Clear();
            _cacheSize = 0;
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
        ///     Set the HTTP response content type
        /// </summary>
        /// <param name="extension">Content extension</param>
        public Response SetContentType(string extension)
        {
            // Base content types
            if (extension == ".html")
                return SetHeader("Content-Type", "text/html");
            if (extension == ".css")
                return SetHeader("Content-Type", "text/css");
            if (extension == ".js")
                return SetHeader("Content-Type", "text/javascript");
            if (extension == ".xml")
                return SetHeader("Content-Type", "text/xml");

            // Common content types
            if (extension == ".gzip")
                return SetHeader("Content-Type", "application/gzip");
            if (extension == ".json")
                return SetHeader("Content-Type", "application/json");
            if (extension == ".map")
                return SetHeader("Content-Type", "application/json");
            if (extension == ".pdf")
                return SetHeader("Content-Type", "application/pdf");
            if (extension == ".zip")
                return SetHeader("Content-Type", "application/zip");
            if (extension == ".mp3")
                return SetHeader("Content-Type", "audio/mpeg");
            if (extension == ".jpg")
                return SetHeader("Content-Type", "image/jpeg");
            if (extension == ".gif")
                return SetHeader("Content-Type", "image/gif");
            if (extension == ".png")
                return SetHeader("Content-Type", "image/png");
            if (extension == ".svg")
                return SetHeader("Content-Type", "image/svg+xml");
            if (extension == ".mp4")
                return SetHeader("Content-Type", "video/mp4");

            // Application content types
            if (extension == ".atom")
                return SetHeader("Content-Type", "application/atom+xml");
            if (extension == ".fastsoap")
                return SetHeader("Content-Type", "application/fastsoap");
            if (extension == ".ps")
                return SetHeader("Content-Type", "application/postscript");
            if (extension == ".soap")
                return SetHeader("Content-Type", "application/soap+xml");
            if (extension == ".sql")
                return SetHeader("Content-Type", "application/sql");
            if (extension == ".xslt")
                return SetHeader("Content-Type", "application/xslt+xml");
            if (extension == ".zlib")
                return SetHeader("Content-Type", "application/zlib");

            // Audio content types
            if (extension == ".aac")
                return SetHeader("Content-Type", "audio/aac");
            if (extension == ".ac3")
                return SetHeader("Content-Type", "audio/ac3");
            if (extension == ".ogg")
                return SetHeader("Content-Type", "audio/ogg");

            // Font content types
            if (extension == ".ttf")
                return SetHeader("Content-Type", "font/ttf");

            // Image content types
            if (extension == ".bmp")
                return SetHeader("Content-Type", "image/bmp");
            if (extension == ".jpm")
                return SetHeader("Content-Type", "image/jpm");
            if (extension == ".jpx")
                return SetHeader("Content-Type", "image/jpx");
            if (extension == ".jrx")
                return SetHeader("Content-Type", "image/jrx");
            if (extension == ".tiff")
                return SetHeader("Content-Type", "image/tiff");
            if (extension == ".emf")
                return SetHeader("Content-Type", "image/emf");
            if (extension == ".wmf")
                return SetHeader("Content-Type", "image/wmf");

            // Message content types
            if (extension == ".http")
                return SetHeader("Content-Type", "message/http");
            if (extension == ".s-http")
                return SetHeader("Content-Type", "message/s-http");

            // Model content types
            if (extension == ".mesh")
                return SetHeader("Content-Type", "model/mesh");
            if (extension == ".vrml")
                return SetHeader("Content-Type", "model/vrml");

            // Text content types
            if (extension == ".csv")
                return SetHeader("Content-Type", "text/csv");
            if (extension == ".plain")
                return SetHeader("Content-Type", "text/plain");
            if (extension == ".richtext")
                return SetHeader("Content-Type", "text/richtext");
            if (extension == ".rtf")
                return SetHeader("Content-Type", "text/rtf");
            if (extension == ".rtx")
                return SetHeader("Content-Type", "text/rtx");
            if (extension == ".sgml")
                return SetHeader("Content-Type", "text/sgml");
            if (extension == ".strings")
                return SetHeader("Content-Type", "text/strings");
            if (extension == ".url")
                return SetHeader("Content-Type", "text/uri-list");

            // Video content types
            if (extension == ".H264")
                return SetHeader("Content-Type", "video/H264");
            if (extension == ".H265")
                return SetHeader("Content-Type", "video/H265");
            if (extension == ".mpeg")
                return SetHeader("Content-Type", "video/mpeg");
            if (extension == ".raw")
                return SetHeader("Content-Type", "video/raw");

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
        ///     Set the HTTP response cookie
        /// </summary>
        /// <param name="name">Cookie name</param>
        /// <param name="value">Cookie value</param>
        /// <param name="maxAge">Cookie age in seconds until it expires (default is 86400)</param>
        /// <param name="path">Cookie path (default is "")</param>
        /// <param name="domain">Cookie domain (default is "")</param>
        /// <param name="secure">Cookie secure flag (default is true)</param>
        /// <param name="httpOnly">Cookie HTTP-only flag (default is false)</param>
        public Response SetCookie(string name, string value, int maxAge = 86400, string path = "", string domain = "",
            bool secure = true, bool httpOnly = false)
        {
            var key = "Set-Cookie";

            // Append the HTTP response header's key
            Cache.Append(key);

            Cache.Append(": ");

            // Append the HTTP response header's value
            var valueIndex = (int) Cache.Size;

            // Append cookie
            Cache.Append(name);
            Cache.Append("=");
            Cache.Append(value);
            Cache.Append("; Max-Age=");
            Cache.Append(maxAge.ToString());
            if (!string.IsNullOrEmpty(domain))
            {
                Cache.Append("; Domain=");
                Cache.Append(domain);
            }

            if (!string.IsNullOrEmpty(path))
            {
                Cache.Append("; Path=");
                Cache.Append(path);
            }

            if (secure)
                Cache.Append("; Secure");
            if (httpOnly)
                Cache.Append("; HttpOnly");

            var valueSize = (int) Cache.Size - valueIndex;

            var cookie = Cache.ExtractString(valueIndex, valueSize);

            Cache.Append("\r\n");

            // Add the header to the corresponding collection
            _headers.Add(new Tuple<string, string>(key, cookie));
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

            var index = (int) Cache.Size;

            // Append the HTTP response body
            Cache.Append(body);
            _bodyIndex = index;
            _bodySize = length;
            _bodyLength = length;
            _bodyLengthProvided = true;
            return this;
        }

        /// <summary>
        ///     Set the HTTP response body
        /// </summary>
        /// <param name="body">Body binary content</param>
        public Response SetBody(byte[] body)
        {
            // Append content length header
            SetHeader("Content-Length", body.Length.ToString());

            Cache.Append("\r\n");

            var index = (int) Cache.Size;

            // Append the HTTP response body
            Cache.Append(body);
            _bodyIndex = index;
            _bodySize = body.Length;
            _bodyLength = body.Length;
            _bodyLengthProvided = true;
            return this;
        }

        /// <summary>
        ///     Set the HTTP response body
        /// </summary>
        /// <param name="body">Body buffer content</param>
        public Response SetBody(Buffer body)
        {
            // Append content length header
            SetHeader("Content-Length", body.Size.ToString());

            Cache.Append("\r\n");

            var index = (int) Cache.Size;

            // Append the HTTP response body
            Cache.Append(body.Data, body.Offset, body.Size);
            _bodyIndex = index;
            _bodySize = (int) body.Size;
            _bodyLength = (int) body.Size;
            _bodyLengthProvided = true;
            return this;
        }

        /// <summary>
        ///     Set the HTTP response body length
        /// </summary>
        /// <param name="length">Body length</param>
        public Response SetBodyLength(int length)
        {
            // Append content length header
            SetHeader("Content-Length", length.ToString());

            Cache.Append("\r\n");

            var index = (int) Cache.Size;

            // Clear the HTTP response body
            _bodyIndex = index;
            _bodySize = 0;
            _bodyLength = length;
            _bodyLengthProvided = true;
            return this;
        }

        /// <summary>
        ///     Make OK response
        /// </summary>
        /// <param name="status">OK status (default is 200 (OK))</param>
        public Response MakeOkResponse(int status = 200)
        {
            Clear();
            SetBegin(status);
            SetHeader("Content-Type", "text/html; charset=UTF-8");
            SetBody();
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

        /// <summary>
        ///     Make HEAD response
        /// </summary>
        public Response MakeHeadResponse()
        {
            Clear();
            SetBegin(200);
            SetHeader("Content-Type", "text/html; charset=UTF-8");
            SetBody();
            return this;
        }

        /// <summary>
        ///     Make GET response
        /// </summary>
        /// <param name="body">Body string content (default is "")</param>
        public Response MakeGetResponse(string body = "")
        {
            Clear();
            SetBegin(200);
            SetHeader("Content-Type", "text/html; charset=UTF-8");
            SetBody(body);
            return this;
        }

        /// <summary>
        ///     Make GET response
        /// </summary>
        /// <param name="body">Body binary content</param>
        public Response MakeGetResponse(byte[] body)
        {
            Clear();
            SetBegin(200);
            SetHeader("Content-Type", "text/html; charset=UTF-8");
            SetBody(body);
            return this;
        }

        /// <summary>
        ///     Make GET response
        /// </summary>
        /// <param name="body">Body buffer content</param>
        public Response MakeGetResponse(Buffer body)
        {
            Clear();
            SetBegin(200);
            SetHeader("Content-Type", "text/html; charset=UTF-8");
            SetBody(body);
            return this;
        }

        /// <summary>
        ///     Make OPTIONS response
        /// </summary>
        /// <param name="allow">Allow methods (default is "HEAD,GET,POST,PUT,DELETE,OPTIONS,TRACE")</param>
        public Response MakeOptionsResponse(string allow = "HEAD,GET,POST,PUT,DELETE,OPTIONS,TRACE")
        {
            Clear();
            SetBegin(200);
            SetHeader("Allow", allow);
            SetBody();
            return this;
        }

        /// <summary>
        ///     Make TRACE response
        /// </summary>
        /// <param name="request">Request string content</param>
        public Response MakeTraceResponse(string request)
        {
            Clear();
            SetBegin(200);
            SetHeader("Content-Type", "message/http");
            SetBody(request);
            return this;
        }

        /// <summary>
        ///     Make TRACE response
        /// </summary>
        /// <param name="request">Request binary content</param>
        public Response MakeTraceResponse(byte[] request)
        {
            Clear();
            SetBegin(200);
            SetHeader("Content-Type", "message/http");
            SetBody(request);
            return this;
        }

        /// <summary>
        ///     Make TRACE response
        /// </summary>
        /// <param name="request">Request buffer content</param>
        public Response MakeTraceResponse(Buffer request)
        {
            Clear();
            SetBegin(200);
            SetHeader("Content-Type", "message/http");
            SetBody(request);
            return this;
        }

        // Is pending parts of HTTP response
        internal bool IsPendingHeader()
        {
            return !IsErrorSet && _bodyIndex == 0;
        }

        internal bool IsPendingBody()
        {
            return !IsErrorSet && _bodyIndex > 0 && _bodySize > 0;
        }

        // Receive parts of HTTP response
        internal bool ReceiveHeader(byte[] buffer, int offset, int size)
        {
            // Update the request cache
            Cache.Append(buffer, offset, size);

            // Try to seek for HTTP header separator
            for (var i = _cacheSize; i < (int) Cache.Size; ++i)
            {
                // Check for the request cache out of bounds
                if (i + 3 >= (int) Cache.Size)
                    break;

                // Check for the header separator
                if (Cache[i + 0] == '\r' && Cache[i + 1] == '\n' && Cache[i + 2] == '\r' && Cache[i + 3] == '\n')
                {
                    var index = 0;

                    // Set the error flag for a while...
                    IsErrorSet = true;

                    // Parse protocol version
                    var protocolIndex = index;
                    var protocolSize = 0;
                    while (Cache[index] != ' ')
                    {
                        ++protocolSize;
                        ++index;
                        if (index >= (int) Cache.Size)
                            return false;
                    }

                    ++index;
                    if (index >= (int) Cache.Size)
                        return false;
                    Protocol = Cache.ExtractString(protocolIndex, protocolSize);

                    // Parse status code
                    var statusIndex = index;
                    var statusSize = 0;
                    while (Cache[index] != ' ')
                    {
                        if (Cache[index] < '0' || Cache[index] > '9')
                            return false;
                        ++statusSize;
                        ++index;
                        if (index >= (int) Cache.Size)
                            return false;
                    }

                    Status = 0;
                    for (var j = statusIndex; j < statusIndex + statusSize; ++j)
                    {
                        Status *= 10;
                        Status += Cache[j] - '0';
                    }

                    ++index;
                    if (index >= (int) Cache.Size)
                        return false;

                    // Parse status phrase
                    var statusPhraseIndex = index;
                    var statusPhraseSize = 0;
                    while (Cache[index] != '\r')
                    {
                        ++statusPhraseSize;
                        ++index;
                        if (index >= (int) Cache.Size)
                            return false;
                    }

                    ++index;
                    if (index >= (int) Cache.Size || Cache[index] != '\n')
                        return false;
                    ++index;
                    if (index >= (int) Cache.Size)
                        return false;
                    StatusPhrase = Cache.ExtractString(statusPhraseIndex, statusPhraseSize);

                    // Parse headers
                    while (index < (int) Cache.Size && index < i)
                    {
                        // Parse header name
                        var headerNameIndex = index;
                        var headerNameSize = 0;
                        while (Cache[index] != ':')
                        {
                            ++headerNameSize;
                            ++index;
                            if (index >= i)
                                break;
                            if (index >= (int) Cache.Size)
                                return false;
                        }

                        ++index;
                        if (index >= i)
                            break;
                        if (index >= (int) Cache.Size)
                            return false;

                        // Skip all prefix space characters
                        while (char.IsWhiteSpace((char) Cache[index]))
                        {
                            ++index;
                            if (index >= i)
                                break;
                            if (index >= (int) Cache.Size)
                                return false;
                        }

                        // Parse header value
                        var headerValueIndex = index;
                        var headerValueSize = 0;
                        while (Cache[index] != '\r')
                        {
                            ++headerValueSize;
                            ++index;
                            if (index >= i)
                                break;
                            if (index >= (int) Cache.Size)
                                return false;
                        }

                        ++index;
                        if (index >= (int) Cache.Size || Cache[index] != '\n')
                            return false;
                        ++index;
                        if (index >= (int) Cache.Size)
                            return false;

                        // Validate header name and value
                        if (headerNameSize == 0 || headerValueSize == 0)
                            return false;

                        // Add a new header
                        var headerName = Cache.ExtractString(headerNameIndex, headerNameSize);
                        var headerValue = Cache.ExtractString(headerValueIndex, headerValueSize);
                        _headers.Add(new Tuple<string, string>(headerName, headerValue));

                        // Try to find the body content length
                        if (headerName == "Content-Length")
                        {
                            _bodyLength = 0;
                            for (var j = headerValueIndex; j < headerValueIndex + headerValueSize; ++j)
                            {
                                if (Cache[j] < '0' || Cache[j] > '9')
                                    return false;
                                _bodyLength *= 10;
                                _bodyLength += Cache[j] - '0';
                                _bodyLengthProvided = true;
                            }
                        }
                    }

                    // Reset the error flag
                    IsErrorSet = false;

                    // Update the body index and size
                    _bodyIndex = i + 4;
                    _bodySize = (int) Cache.Size - i - 4;

                    // Update the parsed cache size
                    _cacheSize = (int) Cache.Size;

                    return true;
                }
            }

            // Update the parsed cache size
            _cacheSize = (int) Cache.Size >= 3 ? (int) Cache.Size - 3 : 0;

            return false;
        }

        internal bool ReceiveBody(byte[] buffer, int offset, int size)
        {
            // Update the request cache
            Cache.Append(buffer, offset, size);

            // Update the parsed cache size
            _cacheSize = (int) Cache.Size;

            // Update body size
            _bodySize += size;

            // Check if the body was fully parsed
            if (_bodyLengthProvided && _bodySize >= _bodyLength)
            {
                _bodySize = _bodyLength;
                return true;
            }

            return false;
        }
    }
}