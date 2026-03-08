using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class HttpListenerServerTransport : ITransport
    {
        private static readonly JsonSerializerSettings CachedJsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new RequestIdConverter() }
        };

        private readonly HttpListener _listener;
        private readonly int _port;
        private readonly string _path;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ConcurrentQueue<JsonRpcMessage> _incomingQueue;
        private readonly SemaphoreSlim _incomingSignal;
        private readonly ConcurrentDictionary<string, SseSession> _sseSessions = new ConcurrentDictionary<string, SseSession>();
        private Task _listenerTask;
        private volatile int _state = StateInitial;
        private Exception _completeError;

        private const int StateInitial = 0;
        private const int StateConnected = 1;
        private const int StateDisconnected = 2;

        public string SessionId { get; private set; }
        public bool IsConnected => _state == StateConnected;
        public int ConnectedClients => _sseSessions.Count;
        public IMessageReader<JsonRpcMessage> MessageReader { get; }

        public HttpListenerServerTransport(int port = 3000, string path = "/mcp", ILogger logger = null)
        {
            _port = port;
            _path = path.StartsWith("/") ? path : "/" + path;
            if (!_path.EndsWith("/"))
            {
                _path += "/";
            }
            _logger = logger ?? new UnityLoggerImpl();
            _listener = new HttpListener();
            _incomingQueue = new ConcurrentQueue<JsonRpcMessage>();
            _incomingSignal = new SemaphoreSlim(0);
            MessageReader = new MessageQueueReader<JsonRpcMessage>(_incomingQueue, _incomingSignal, () => _state == StateDisconnected, () => _completeError);
            SessionId = Guid.NewGuid().ToString("N");
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_state != StateInitial)
            {
                throw new InvalidOperationException("Transport already started");
            }

            string prefix = $"http://localhost:{_port}{_path}";
            _listener.Prefixes.Add(prefix);

            try
            {
                _listener.Start();
                _state = StateConnected;
                _logger.Log(LogLevel.Information, $"MCP Server started at {prefix}");

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);
                _listenerTask = ListenAsync(linkedCts.Token);
            }
            catch (HttpListenerException ex)
            {
                _logger.Log(LogLevel.Error, $"Failed to start HTTP listener: {ex.Message}", ex);
                throw;
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = HandleRequestAsync(context, cancellationToken);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "Error accepting request", ex);
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, Mcp-Session-Id");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    return;
                }

                string localPath = request.Url.AbsolutePath;
                if (localPath.StartsWith(_path))
                {
                    localPath = localPath.Substring(_path.Length);
                }

                if (request.HttpMethod == "GET")
                {
                    await HandleGetRequestAsync(context, cancellationToken);
                }
                else if (request.HttpMethod == "POST")
                {
                    await HandlePostRequestAsync(context, cancellationToken);
                }
                else
                {
                    response.StatusCode = 405;
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Error handling request: {ex.Message}", ex);
                response.StatusCode = 500;
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch { }
            }
        }

        private async Task HandleGetRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var response = context.Response;
            response.ContentType = "text/event-stream; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            var sessionId = SessionId;
            var sseSession = new SseSession(sessionId, response.OutputStream, _logger);
            _sseSessions[sessionId] = sseSession;

            await sseSession.SendEndpointEventAsync(_path + "message", cancellationToken);

            _logger.Log(LogLevel.Information, $"SSE session started: {sessionId}");

            try
            {
                await sseSession.WaitForCompletionAsync(cancellationToken);
            }
            finally
            {
                _sseSessions.TryRemove(sessionId, out _);
                _logger.Log(LogLevel.Information, $"SSE session ended: {sessionId}");
            }
        }

        private async Task HandlePostRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            var response = context.Response;

            if (request.ContentType?.Contains("application/json") != true)
            {
                response.StatusCode = 415;
                return;
            }

            string body;
            
            try
            {
                using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync();
                }
                
                _logger.Log(LogLevel.Debug, $"Received POST: {body}");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Failed to read request body: {ex.Message}", ex);
                response.StatusCode = 400;
                return;
            }

            try
            {
                var messages = ParseMessages(body);

                var responseMessages = new JArray();
                bool hasResponse = false;

                foreach (var message in messages)
                {
                    message.Context = JsonRpcMessageContext.Create(this);

                    if (message is JsonRpcRequest req)
                    {
                        hasResponse = true;
                        responseMessages.Add(CreateAckResponse(req.Id));
                    }

                    _incomingQueue.Enqueue(message);
                    _incomingSignal.Release();
                }

                if (hasResponse)
                {
                    response.ContentType = "application/json; charset=utf-8";
                    response.ContentEncoding = Encoding.UTF8;
                    response.StatusCode = 202;

                    string responseBody;
                    if (responseMessages.Count == 1)
                    {
                        responseBody = responseMessages[0].ToString(Formatting.None);
                    }
                    else
                    {
                        responseBody = responseMessages.ToString(Formatting.None);
                    }

                    var buffer = Encoding.UTF8.GetBytes(responseBody);
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                }
                else
                {
                    response.StatusCode = 202;
                }
            }
            catch (JsonException ex)
            {
                _logger.Log(LogLevel.Error, $"JSON parse error: {ex.Message}", ex);
                response.StatusCode = 400;
            }
        }

        private List<JsonRpcMessage> ParseMessages(string body)
        {
            var messages = new List<JsonRpcMessage>();
            var token = JToken.Parse(body);
            var serializer = JsonSerializer.Create(CachedJsonSettings);

            if (token is JArray array)
            {
                foreach (var item in array)
                {
                    var msg = item.ToObject<JsonRpcMessage>(serializer);
                    if (msg != null)
                    {
                        messages.Add(msg);
                    }
                }
            }
            else
            {
                var msg = token.ToObject<JsonRpcMessage>(serializer);
                if (msg != null)
                {
                    messages.Add(msg);
                }
            }

            return messages;
        }

        private JObject CreateAckResponse(RequestId id)
        {
            return new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = JToken.FromObject(id, JsonSerializer.Create(CachedJsonSettings)),
                ["result"] = new JObject { ["status"] = "accepted" }
            };
        }

        public async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
        {
            if (_state != StateConnected)
            {
                throw new InvalidOperationException("Transport is not connected");
            }

            string json = JsonConvert.SerializeObject(message, CachedJsonSettings);
            _logger.Log(LogLevel.Debug, $"Sending message: {json}");

            foreach (var session in _sseSessions.Values)
            {
                try
                {
                    await session.SendMessageAsync(json, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Warning, $"Failed to send to SSE session: {ex.Message}");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _state = StateDisconnected;
            _completeError = null;
            _incomingSignal.Release();
            _cts.Cancel();

            foreach (var session in _sseSessions.Values)
            {
                session.Dispose();
            }
            _sseSessions.Clear();

            try
            {
                _listener.Stop();
            }
            catch { }

            try
            {
                _listener.Close();
            }
            catch { }

            if (_listenerTask != null)
            {
                try
                {
                    await _listenerTask;
                }
                catch { }
            }

            _cts.Dispose();
            _logger.Log(LogLevel.Information, "MCP Server stopped");
        }

        private class SseSession : IDisposable
        {
            private readonly string _sessionId;
            private readonly Stream _stream;
            private readonly SseEventWriter _writer;
            private readonly ILogger _logger;
            private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

            public SseSession(string sessionId, Stream stream, ILogger logger)
            {
                _sessionId = sessionId;
                _stream = stream;
                _logger = logger;
                _writer = new SseEventWriter(stream);
            }

            public async Task SendEndpointEventAsync(string endpoint, CancellationToken cancellationToken)
            {
                await _writer.WriteAsync("endpoint", endpoint, cancellationToken: cancellationToken);
            }

            public async Task SendMessageAsync(string json, CancellationToken cancellationToken)
            {
                await _writer.WriteAsync("message", json, cancellationToken: cancellationToken);
            }

            public Task WaitForCompletionAsync(CancellationToken cancellationToken)
            {
                using var registration = cancellationToken.Register(() => _tcs.TrySetCanceled());
                return _tcs.Task;
            }

            public void Dispose()
            {
                try
                {
                    _tcs.TrySetResult(true);
                    _writer?.Dispose();
                }
                catch (Exception e)
                {
                    _logger.Log(LogLevel.Error, $"Dispose Session Exception: {e.Message}");
                }
            }
        }
    }
}
