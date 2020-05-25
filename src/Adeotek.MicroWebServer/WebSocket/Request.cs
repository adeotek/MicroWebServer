using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Adeotek.MicroWebServer.WebSocket
{
    /// <summary>
    ///     HTTP request is used to create or process parameters of HTTP protocol request(method, URL, headers, etc).
    /// </summary>
    /// <remarks>Not thread-safe.</remarks>
    public class Request
    {
        // HTTP request body
        private int _bodyIndex;
        private int _bodyLength;
        private bool _bodyLengthProvided;
        private int _bodySize;

        // HTTP request cache
        private int _cacheSize;

        // HTTP request headers
        private readonly List<Tuple<string, string>> _headers = new List<Tuple<string, string>>();

        /// <summary>
        ///     Initialize an empty HTTP request
        /// </summary>
        public Request()
        {
            Clear();
        }

        /// <summary>
        ///     Is the HTTP request error flag set?
        /// </summary>
        public bool IsErrorSet { get; private set; }

        /// <summary>
        ///     Get the HTTP request method
        /// </summary>
        public string Method { get; private set; }

        /// <summary>
        ///     Get the HTTP request URL
        /// </summary>
        public string Url { get; private set; }

        /// <summary>
        ///     Get the HTTP request protocol version
        /// </summary>
        public string Protocol { get; private set; }

        /// <summary>
        ///     Get the HTTP request headers count
        /// </summary>
        public long Headers => _headers.Count;

        /// <summary>
        ///     Get the HTTP request body as string
        /// </summary>
        public string Body => Cache.ExtractString(_bodyIndex, _bodySize);

        /// <summary>
        ///     Get the HTTP request body length
        /// </summary>
        public long BodyLength => _bodyLength;

        /// <summary>
        ///     Get the HTTP request cache content
        /// </summary>
        public Buffer Cache { get; } = new Buffer();

        /// <summary>
        ///     Get the HTTP request header by index
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
        ///     Get string from the current HTTP request
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Request method: {Method}");
            sb.AppendLine($"Request URL: {Url}");
            sb.AppendLine($"Request protocol: {Protocol}");
            sb.AppendLine($"Request headers: {Headers}");
            for (var i = 0; i < Headers; ++i)
            {
                var header = Header(i);
                sb.AppendLine($"{header.Item1} : {header.Item2}");
            }

            sb.AppendLine($"Request body: {BodyLength}");
            sb.AppendLine(Body);
            return sb.ToString();
        }

        /// <summary>
        ///     Clear the HTTP request cache
        /// </summary>
        public Request Clear()
        {
            IsErrorSet = false;
            Method = "";
            Url = "";
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
        ///     Set the HTTP request begin with a given method, URL and protocol
        /// </summary>
        /// <param name="method">HTTP method</param>
        /// <param name="url">Requested URL</param>
        /// <param name="protocol">Protocol version (default is "HTTP/1.1")</param>
        public Request SetBegin(string method, string url, string protocol = "HTTP/1.1")
        {
            // Clear the HTTP request cache
            Clear();

            // Append the HTTP request method
            Cache.Append(method);
            Method = method;

            Cache.Append(" ");

            // Append the HTTP request URL
            Cache.Append(url);
            Url = url;

            Cache.Append(" ");

            // Append the HTTP request protocol version
            Cache.Append(protocol);
            Protocol = protocol;

            Cache.Append("\r\n");
            return this;
        }

        /// <summary>
        ///     Set the HTTP request header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="value">Header value</param>
        public Request SetHeader(string key, string value)
        {
            // Append the HTTP request header's key
            Cache.Append(key);

            Cache.Append(": ");

            // Append the HTTP request header's value
            Cache.Append(value);

            Cache.Append("\r\n");

            // Add the header to the corresponding collection
            _headers.Add(new Tuple<string, string>(key, value));
            return this;
        }

        /// <summary>
        ///     Set the HTTP request body
        /// </summary>
        /// <param name="body">Body binary content</param>
        public Request SetBody(byte[] body)
        {
            // Append content length header
            SetHeader("Content-Length", body.Length.ToString());

            Cache.Append("\r\n");

            var index = (int)Cache.Size;

            // Append the HTTP request body
            Cache.Append(body);
            _bodyIndex = index;
            _bodySize = body.Length;
            _bodyLength = body.Length;
            _bodyLengthProvided = true;
            return this;
        }

        /// <summary>
        ///     Set the HTTP request body
        /// </summary>
        /// <param name="body">Body buffer content</param>
        public Request SetBody(Buffer body)
        {
            // Append content length header
            SetHeader("Content-Length", body.Size.ToString());

            Cache.Append("\r\n");

            var index = (int)Cache.Size;

            // Append the HTTP request body
            Cache.Append(body.Data, body.Offset, body.Size);
            _bodyIndex = index;
            _bodySize = (int)body.Size;
            _bodyLength = (int)body.Size;
            _bodyLengthProvided = true;
            return this;
        }

        // Is pending parts of HTTP request
        internal bool IsPendingHeader()
        {
            return !IsErrorSet && _bodyIndex == 0;
        }

        internal bool ReceiveHeader(byte[] buffer, int offset, int size)
        {
            // Update the request cache
            Cache.Append(buffer, offset, size);

            // Try to seek for HTTP header separator
            for (var i = _cacheSize; i < (int)Cache.Size; ++i)
            {
                // Check for the request cache out of bounds
                if (i + 3 >= (int)Cache.Size)
                {
                    break;
                }

                // Check for the header separator
                if (Cache[i + 0] == '\r' && Cache[i + 1] == '\n' && Cache[i + 2] == '\r' && Cache[i + 3] == '\n')
                {
                    var index = 0;

                    // Set the error flag for a while...
                    IsErrorSet = true;

                    // Parse method
                    var methodIndex = index;
                    var methodSize = 0;
                    while (Cache[index] != ' ')
                    {
                        ++methodSize;
                        ++index;
                        if (index >= (int)Cache.Size)
                        {
                            return false;
                        }
                    }

                    ++index;
                    if (index >= (int)Cache.Size)
                    {
                        return false;
                    }

                    Method = Cache.ExtractString(methodIndex, methodSize);

                    // Parse URL
                    var urlIndex = index;
                    var urlSize = 0;
                    while (Cache[index] != ' ')
                    {
                        ++urlSize;
                        ++index;
                        if (index >= (int)Cache.Size)
                        {
                            return false;
                        }
                    }

                    ++index;
                    if (index >= (int)Cache.Size)
                    {
                        return false;
                    }

                    Url = Cache.ExtractString(urlIndex, urlSize);

                    // Parse protocol version
                    var protocolIndex = index;
                    var protocolSize = 0;
                    while (Cache[index] != '\r')
                    {
                        ++protocolSize;
                        ++index;
                        if (index >= (int)Cache.Size)
                        {
                            return false;
                        }
                    }

                    ++index;
                    if (index >= (int)Cache.Size || Cache[index] != '\n')
                    {
                        return false;
                    }

                    ++index;
                    if (index >= (int)Cache.Size)
                    {
                        return false;
                    }

                    Protocol = Cache.ExtractString(protocolIndex, protocolSize);

                    // Parse headers
                    while (index < (int)Cache.Size && index < i)
                    {
                        // Parse header name
                        var headerNameIndex = index;
                        var headerNameSize = 0;
                        while (Cache[index] != ':')
                        {
                            ++headerNameSize;
                            ++index;
                            if (index >= i)
                            {
                                break;
                            }

                            if (index >= (int)Cache.Size)
                            {
                                return false;
                            }
                        }

                        ++index;
                        if (index >= i)
                        {
                            break;
                        }

                        if (index >= (int)Cache.Size)
                        {
                            return false;
                        }

                        // Skip all prefix space characters
                        while (char.IsWhiteSpace((char)Cache[index]))
                        {
                            ++index;
                            if (index >= i)
                            {
                                break;
                            }

                            if (index >= (int)Cache.Size)
                            {
                                return false;
                            }
                        }

                        // Parse header value
                        var headerValueIndex = index;
                        var headerValueSize = 0;
                        while (Cache[index] != '\r')
                        {
                            ++headerValueSize;
                            ++index;
                            if (index >= i)
                            {
                                break;
                            }

                            if (index >= (int)Cache.Size)
                            {
                                return false;
                            }
                        }

                        ++index;
                        if (index >= (int)Cache.Size || Cache[index] != '\n')
                        {
                            return false;
                        }

                        ++index;
                        if (index >= (int)Cache.Size)
                        {
                            return false;
                        }

                        // Validate header name and value
                        if (headerNameSize == 0 || headerValueSize == 0)
                        {
                            return false;
                        }

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
                                {
                                    return false;
                                }

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
                    _bodySize = (int)Cache.Size - i - 4;

                    // Update the parsed cache size
                    _cacheSize = (int)Cache.Size;

                    return true;
                }
            }

            // Update the parsed cache size
            _cacheSize = (int)Cache.Size >= 3 ? (int)Cache.Size - 3 : 0;

            return false;
        }

        internal bool ReceiveBody(byte[] buffer, int offset, int size)
        {
            // Update the request cache
            Cache.Append(buffer, offset, size);

            // Update the parsed cache size
            _cacheSize = (int)Cache.Size;

            // Update body size
            _bodySize += size;

            // GET request has no body
            if (Method == "HEAD" || Method == "GET" || Method == "OPTIONS" || Method == "TRACE")
            {
                _bodyLength = 0;
                _bodySize = 0;
                return true;
            }

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