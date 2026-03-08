using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Server
{
    internal class McpSessionHandler : IAsyncDisposable
    {
        private static readonly JsonSerializerSettings CachedJsonSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new RequestIdConverter() }
        };

        public const string LatestProtocolVersion = "2025-11-25";
        public static readonly string[] SupportedProtocolVersions = new[]
        {
            "2024-11-05",
            "2025-03-26",
            "2025-06-18",
            LatestProtocolVersion
        };

        private readonly ITransport _transport;
        private readonly RequestHandlers _requestHandlers;
        private readonly NotificationHandlers _notificationHandlers;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<RequestId, TaskCompletionSource<JsonRpcMessage>> _pendingRequests = new ConcurrentDictionary<RequestId, TaskCompletionSource<JsonRpcMessage>>();
        private readonly ConcurrentDictionary<RequestId, CancellationTokenSource> _handlingRequests = new ConcurrentDictionary<RequestId, CancellationTokenSource>();
        private readonly ConcurrentDictionary<string, HashSet<string>> _resourceSubscriptions = new ConcurrentDictionary<string, HashSet<string>>();
        private readonly string _sessionId = Guid.NewGuid().ToString("N");

        private CancellationTokenSource _messageProcessingCts;
        private Task _messageProcessingTask;
        private long _lastRequestId;

        public string EndpointName { get; set; }
        public string NegotiatedProtocolVersion { get; set; }
        public McpServerOptions ServerOptions { get; }

        public McpSessionHandler(
            ITransport transport,
            McpServerOptions serverOptions,
            RequestHandlers requestHandlers,
            NotificationHandlers notificationHandlers,
            ILogger logger)
        {
            Throw.IfNull(transport);
            Throw.IfNull(serverOptions);

            _transport = transport;
            ServerOptions = serverOptions;
            EndpointName = serverOptions.ServerInfo?.Name ?? "McpServer";
            _requestHandlers = requestHandlers ?? new RequestHandlers();
            _notificationHandlers = notificationHandlers ?? new NotificationHandlers();
            _logger = logger ?? new UnityLoggerImpl();

            _logger.Log(LogLevel.Information, $"Session created: {_sessionId}");
        }

        public Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            if (_messageProcessingTask != null)
            {
                throw new InvalidOperationException("Message processing has already started.");
            }

            _messageProcessingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _messageProcessingTask = ProcessMessagesCoreAsync(_messageProcessingCts.Token);
            return _messageProcessingTask;
        }

        private async Task ProcessMessagesCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _transport.MessageReader.ProcessMessagesAsync(
                    async message =>
                    {
                        _logger.Log(LogLevel.Debug, $"Received message: {message.GetType().Name}");
                        await ProcessMessageAsync(message, cancellationToken);
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.Log(LogLevel.Information, $"{EndpointName} message processing canceled");
            }
            finally
            {
                foreach (var entry in _pendingRequests)
                {
                    entry.Value.TrySetException(new IOException("Server shut down unexpectedly."));
                }
            }
        }

        private async Task ProcessMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
        {
            var messageWithId = message as JsonRpcMessageWithId;
            CancellationTokenSource combinedCts = null;

            try
            {
                if (messageWithId != null)
                {
                    combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    _handlingRequests[messageWithId.Id] = combinedCts;
                }

                await HandleMessageAsync(message, combinedCts?.Token ?? cancellationToken);
            }
            catch (Exception ex)
            {
                bool isUserCancellation = ex is OperationCanceledException &&
                    !cancellationToken.IsCancellationRequested &&
                    combinedCts?.IsCancellationRequested == true;

                if (!isUserCancellation && message is JsonRpcRequest request)
                {
                    var detail = CreateErrorDetail(ex);
                    var errorMessage = new JsonRpcError
                    {
                        Id = request.Id,
                        JsonRpc = "2.0",
                        Error = detail
                    };

                    await SendMessageAsync(errorMessage, cancellationToken);
                }
                else if (!(ex is OperationCanceledException))
                {
                    _logger.Log(LogLevel.Error, $"Message handler error: {ex.Message}", ex);
                }
            }
            finally
            {
                if (messageWithId != null)
                {
                    _handlingRequests.TryRemove(messageWithId.Id, out _);
                    combinedCts?.Dispose();
                }
            }
        }

        private JsonRpcErrorDetail CreateErrorDetail(Exception ex)
        {
            return ex switch
            {
                UrlElicitationRequiredException urlEx => new JsonRpcErrorDetail
                {
                    Code = (int)urlEx.ErrorCode,
                    Message = urlEx.Message,
                    Data = urlEx.CreateErrorData()
                },
                McpProtocolException mcpEx => new JsonRpcErrorDetail
                {
                    Code = (int)mcpEx.ErrorCode,
                    Message = mcpEx.Message,
                    Data = mcpEx.Data
                },
                McpException mcpEx => new JsonRpcErrorDetail
                {
                    Code = (int)mcpEx.ErrorCode,
                    Message = mcpEx.Message
                },
                _ => new JsonRpcErrorDetail
                {
                    Code = (int)McpErrorCode.InternalError,
                    Message = "An error occurred."
                }
            };
        }

        private async Task HandleMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
        {
            switch (message)
            {
                case JsonRpcRequest request:
                    await HandleRequestAsync(request, cancellationToken);
                    break;
                case JsonRpcNotification notification:
                    await HandleNotificationAsync(notification, cancellationToken);
                    break;
                case JsonRpcResponse response:
                    HandleResponse(response);
                    break;
                case JsonRpcError error:
                    HandleError(error);
                    break;
            }
        }

        private async Task HandleRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, $"Handling request: {request.Method}");

            var response = request.Method switch
            {
                RequestMethods.Initialize => await HandleInitializeAsync(request, cancellationToken),
                RequestMethods.Ping => CreateResponse(request.Id, new PingResult()),
                RequestMethods.ToolsList => await HandleToolsListAsync(request, cancellationToken),
                RequestMethods.ToolsCall => await HandleToolsCallAsync(request, cancellationToken),
                RequestMethods.PromptsList => await HandlePromptsListAsync(request, cancellationToken),
                RequestMethods.PromptsGet => await HandlePromptsGetAsync(request, cancellationToken),
                RequestMethods.ResourcesList => await HandleResourcesListAsync(request, cancellationToken),
                RequestMethods.ResourcesRead => await HandleResourcesReadAsync(request, cancellationToken),
                RequestMethods.ResourcesTemplatesList => await HandleResourceTemplatesListAsync(request, cancellationToken),
                RequestMethods.ResourcesSubscribe => await HandleResourcesSubscribeAsync(request, cancellationToken),
                RequestMethods.ResourcesUnsubscribe => await HandleResourcesUnsubscribeAsync(request, cancellationToken),
                RequestMethods.CompletionComplete => await HandleCompleteAsync(request, cancellationToken),
                RequestMethods.LoggingSetLevel => await HandleSetLevelAsync(request, cancellationToken),
                RequestMethods.TasksGet => await HandleTaskGetAsync(request, cancellationToken),
                RequestMethods.TasksList => await HandleTaskListAsync(request, cancellationToken),
                RequestMethods.TasksCancel => await HandleTaskCancelAsync(request, cancellationToken),
                _ => await HandleUnknownRequestAsync(request, cancellationToken)
            };

            if (response != null)
            {
                await SendMessageAsync(response, cancellationToken);
            }
        }

        private async Task<JsonRpcMessage> HandleInitializeAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var serializer = JsonSerializer.Create(CachedJsonSettings);
            var @params = request.Params?.ToObject<InitializeRequestParams>(serializer);

            if (@params != null)
            {
                NegotiatedProtocolVersion = NegotiateProtocolVersion(@params.ProtocolVersion);
                _logger.Log(LogLevel.Information, $"Initialize from client: {@params.ClientInfo?.Name} v{@params.ClientInfo?.Version}, protocol: {NegotiatedProtocolVersion}");
            }

            var result = new InitializeResult
            {
                ProtocolVersion = NegotiatedProtocolVersion ?? LatestProtocolVersion,
                Capabilities = ServerOptions.Capabilities,
                ServerInfo = ServerOptions.ServerInfo,
                Instructions = ServerOptions.Instructions
            };

            return CreateResponse(request.Id, result);
        }

        private string NegotiateProtocolVersion(string clientVersion)
        {
            foreach (var version in SupportedProtocolVersions)
            {
                if (version == clientVersion)
                {
                    return version;
                }
            }
            return LatestProtocolVersion;
        }

        private async Task<JsonRpcMessage> HandleToolsListAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<ListToolsRequestParams, ListToolsResult>(RequestMethods.ToolsList, out var handler))
            {
                var @params = request.Params?.ToObject<ListToolsRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateResponse(request.Id, new ListToolsResult());
        }

        private async Task<JsonRpcMessage> HandleToolsCallAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<CallToolRequestParams, CallToolResult>(RequestMethods.ToolsCall, out var handler))
            {
                var @params = request.Params?.ToObject<CallToolRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateErrorResponse(request.Id, McpErrorCode.MethodNotFound, "Tool handler not found");
        }

        private async Task<JsonRpcMessage> HandlePromptsListAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<ListPromptsRequestParams, ListPromptsResult>(RequestMethods.PromptsList, out var handler))
            {
                var @params = request.Params?.ToObject<ListPromptsRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateResponse(request.Id, new ListPromptsResult());
        }

        private async Task<JsonRpcMessage> HandlePromptsGetAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<GetPromptRequestParams, GetPromptResult>(RequestMethods.PromptsGet, out var handler))
            {
                var @params = request.Params?.ToObject<GetPromptRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateErrorResponse(request.Id, McpErrorCode.MethodNotFound, "Prompt handler not found");
        }

        private async Task<JsonRpcMessage> HandleResourcesListAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<ListResourcesRequestParams, ListResourcesResult>(RequestMethods.ResourcesList, out var handler))
            {
                var @params = request.Params?.ToObject<ListResourcesRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateResponse(request.Id, new ListResourcesResult());
        }

        private async Task<JsonRpcMessage> HandleResourcesReadAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<ReadResourceRequestParams, ReadResourceResult>(RequestMethods.ResourcesRead, out var handler))
            {
                var @params = request.Params?.ToObject<ReadResourceRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateErrorResponse(request.Id, McpErrorCode.MethodNotFound, "Resource handler not found");
        }

        private Task<JsonRpcMessage> HandleResourceTemplatesListAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult<JsonRpcMessage>(CreateResponse(request.Id, new ListResourceTemplatesResult()));
        }

        private Task<JsonRpcMessage> HandleResourcesSubscribeAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var @params = request.Params?.ToObject<SubscribeRequestParams>();
            if (@params == null || string.IsNullOrEmpty(@params.Uri))
            {
                return Task.FromResult<JsonRpcMessage>(CreateErrorResponse(request.Id, McpErrorCode.InvalidParams, "Resource URI is required"));
            }

            string uri = @params.Uri;
            var subscribers = _resourceSubscriptions.GetOrAdd(uri, _ => new HashSet<string>());
            lock (subscribers)
            {
                subscribers.Add(_sessionId);
            }

            _logger.Log(LogLevel.Debug, $"Resource subscribed: {uri} by session {_sessionId}");
            return Task.FromResult<JsonRpcMessage>(CreateResponse(request.Id, new EmptyResult()));
        }

        private Task<JsonRpcMessage> HandleResourcesUnsubscribeAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var @params = request.Params?.ToObject<UnsubscribeRequestParams>();
            if (@params == null || string.IsNullOrEmpty(@params.Uri))
            {
                return Task.FromResult<JsonRpcMessage>(CreateErrorResponse(request.Id, McpErrorCode.InvalidParams, "Resource URI is required"));
            }

            string uri = @params.Uri;
            if (_resourceSubscriptions.TryGetValue(uri, out var subscribers))
            {
                lock (subscribers)
                {
                    subscribers.Remove(_sessionId);
                }
            }

            _logger.Log(LogLevel.Debug, $"Resource unsubscribed: {uri} by session {_sessionId}");
            return Task.FromResult<JsonRpcMessage>(CreateResponse(request.Id, new EmptyResult()));
        }

        private async Task<JsonRpcMessage> HandleCompleteAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<CompleteRequestParams, CompleteResult>(RequestMethods.CompletionComplete, out var handler))
            {
                var @params = request.Params?.ToObject<CompleteRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateResponse(request.Id, new CompleteResult());
        }

        private Task<JsonRpcMessage> HandleSetLevelAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult<JsonRpcMessage>(CreateResponse(request.Id, new EmptyResult()));
        }

        private async Task<JsonRpcMessage> HandleTaskGetAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<GetTaskRequestParams, GetTaskResult>(RequestMethods.TasksGet, out var handler))
            {
                var @params = request.Params?.ToObject<GetTaskRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateErrorResponse(request.Id, McpErrorCode.MethodNotFound, "Task handler not found");
        }

        private async Task<JsonRpcMessage> HandleTaskListAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<ListTasksRequestParams, ListTasksResult>(RequestMethods.TasksList, out var handler))
            {
                var @params = request.Params?.ToObject<ListTasksRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateResponse(request.Id, new ListTasksResult());
        }

        private async Task<JsonRpcMessage> HandleTaskCancelAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_requestHandlers.TryGet<CancelMcpTaskRequestParams, CancelMcpTaskResult>(RequestMethods.TasksCancel, out var handler))
            {
                var @params = request.Params?.ToObject<CancelMcpTaskRequestParams>();
                var result = await handler(@params, cancellationToken);
                return CreateResponse(request.Id, result);
            }

            return CreateResponse(request.Id, new CancelMcpTaskResult());
        }

        private Task<JsonRpcMessage> HandleUnknownRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Warning, $"Unknown method: {request.Method}");
            return Task.FromResult<JsonRpcMessage>(CreateErrorResponse(request.Id, McpErrorCode.MethodNotFound, $"Method not found: {request.Method}"));
        }

        private async Task HandleNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, $"Handling notification: {notification.Method}");

            switch (notification.Method)
            {
                case NotificationMethods.InitializedNotification:
                    _logger.Log(LogLevel.Information, "Client initialized");
                    break;
                case NotificationMethods.CancelledNotification:
                    await HandleCancelledNotificationAsync(notification, cancellationToken);
                    break;
                case NotificationMethods.ProgressNotification:
                    await HandleProgressNotificationAsync(notification, cancellationToken);
                    break;
            }

            if (_notificationHandlers.TryGet<JsonRpcNotification>(notification.Method, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        await handler(notification, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, $"Notification handler error: {ex.Message}", ex);
                    }
                }
            }
        }

        private Task HandleCancelledNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken)
        {
            var @params = notification.Params?.ToObject<CancelledNotificationParams>();
            if (@params != null && _handlingRequests.TryGetValue(@params.RequestId, out var cts))
            {
                cts.Cancel();
                _logger.Log(LogLevel.Information, $"Request cancelled: {@params.RequestId}, reason: {@params.Reason}");
            }
            return Task.CompletedTask;
        }

        private Task HandleProgressNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken)
        {
            var @params = notification.Params?.ToObject<ProgressNotificationParams>();
            if (@params != null)
            {
                _logger.Log(LogLevel.Debug, $"Progress: {@params.Progress}/{@params.Total} - {@params.Message}");
            }
            return Task.CompletedTask;
        }

        private void HandleResponse(JsonRpcResponse response)
        {
            if (_pendingRequests.TryRemove(response.Id, out var tcs))
            {
                tcs.TrySetResult(response);
            }
        }

        private void HandleError(JsonRpcError error)
        {
            if (_pendingRequests.TryRemove(error.Id, out var tcs))
            {
                var ex = new McpProtocolException((McpErrorCode)error.Error.Code, error.Error.Message, error.Error.Data);
                tcs.TrySetException(ex);
            }
        }

        public async Task<JsonRpcMessage> SendRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Id.Id == null)
            {
                request.Id = new RequestId(Interlocked.Increment(ref _lastRequestId));
            }

            var tcs = new TaskCompletionSource<JsonRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[request.Id] = tcs;

            try
            {
                await SendMessageAsync(request, cancellationToken);

                using var registration = cancellationToken.Register(() =>
                {
                    _pendingRequests.TryRemove(request.Id, out _);
                    tcs.TrySetCanceled(cancellationToken);
                });

                return await tcs.Task;
            }
            finally
            {
                _pendingRequests.TryRemove(request.Id, out _);
            }
        }

        public async Task SendNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken = default)
        {
            await SendMessageAsync(notification, cancellationToken);
        }

        private async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await _transport.SendMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Failed to send message: {ex.Message}", ex);
                throw;
            }
        }

        private JsonRpcResponse CreateResponse(RequestId id, object result)
        {
            return new JsonRpcResponse
            {
                Id = id,
                JsonRpc = "2.0",
                Result = JToken.FromObject(result)
            };
        }

        private JsonRpcError CreateErrorResponse(RequestId id, McpErrorCode code, string message)
        {
            return new JsonRpcError
            {
                Id = id,
                JsonRpc = "2.0",
                Error = new JsonRpcErrorDetail
                {
                    Code = (int)code,
                    Message = message
                }
            };
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var kvp in _resourceSubscriptions.ToArray())
            {
                lock (kvp.Value)
                {
                    kvp.Value.Remove(_sessionId);
                }
            }
            _resourceSubscriptions.Clear();

            _messageProcessingCts?.Cancel();

            if (_messageProcessingTask != null)
            {
                try
                {
                    await _messageProcessingTask;
                }
                catch { }
            }

            _messageProcessingCts?.Dispose();

            foreach (var cts in _handlingRequests.Values)
            {
                cts.Dispose();
            }
            _handlingRequests.Clear();

            _logger.Log(LogLevel.Information, $"Session disposed: {_sessionId}");
        }

        public bool IsResourceSubscribed(string uri)
        {
            if (_resourceSubscriptions.TryGetValue(uri, out var subscribers))
            {
                lock (subscribers)
                {
                    return subscribers.Contains(_sessionId);
                }
            }
            return false;
        }

        public async Task NotifyResourceUpdatedAsync(string uri, CancellationToken cancellationToken = default)
        {
            if (!_resourceSubscriptions.TryGetValue(uri, out var subscribers) || subscribers.Count == 0)
                return;

            var notification = new JsonRpcNotification
            {
                Method = NotificationMethods.ResourceUpdatedNotification,
                Params = JToken.FromObject(new ResourceUpdatedNotificationParams { Uri = uri })
            };

            await SendNotificationAsync(notification, cancellationToken);
            _logger.Log(LogLevel.Debug, $"Resource update notification sent: {uri}");
        }

        public async Task NotifyResourceListChangedAsync(CancellationToken cancellationToken = default)
        {
            var notification = new JsonRpcNotification
            {
                Method = NotificationMethods.ResourceListChangedNotification,
                Params = JToken.FromObject(new ResourceListChangedNotificationParams())
            };

            await SendNotificationAsync(notification, cancellationToken);
            _logger.Log(LogLevel.Debug, "Resource list changed notification sent");
        }
    }
}
