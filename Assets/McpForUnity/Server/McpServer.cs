using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server.TypeHandlers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace ModelContextProtocol.Server
{
    public class McpServer : IAsyncDisposable
    {
        private readonly McpServerOptions _options;
        private readonly HttpListenerServerTransport _transport;
        private readonly McpSessionHandler _sessionHandler;
        private readonly RequestHandlers _requestHandlers = new RequestHandlers();
        private readonly NotificationHandlers _notificationHandlers = new NotificationHandlers();
        private readonly ILogger _logger;

        private readonly McpServerPrimitiveCollection<Tool> _tools = new McpServerPrimitiveCollection<Tool>();
        private readonly List<Tool> _allTools = new List<Tool>();
        private readonly McpServerPrimitiveCollection<Prompt> _prompts = new McpServerPrimitiveCollection<Prompt>();
        private readonly McpServerPrimitiveCollection<Resource> _resources = new McpServerPrimitiveCollection<Resource>();
        private readonly Dictionary<string, Func<CallToolRequestParams, CancellationToken, Task<CallToolResult>>> _toolHandlers = new Dictionary<string, Func<CallToolRequestParams, CancellationToken, Task<CallToolResult>>>();
        private readonly Dictionary<string, Func<GetPromptRequestParams, CancellationToken, Task<GetPromptResult>>> _promptHandlers = new Dictionary<string, Func<GetPromptRequestParams, CancellationToken, Task<GetPromptResult>>>();
        private readonly Dictionary<string, Func<ReadResourceRequestParams, CancellationToken, Task<ReadResourceResult>>> _resourceHandlers = new Dictionary<string, Func<ReadResourceRequestParams, CancellationToken, Task<ReadResourceResult>>>();

        private readonly Dictionary<string, object> _instances = new Dictionary<string, object>();
        private readonly Dictionary<string, List<string>> _instanceToolNames = new Dictionary<string, List<string>>();

        private readonly IMcpTaskStore _taskStore;
        private Task _runTask;

        // 缓存的响应对象
        private ListToolsResult _cachedToolsList;
        private ListPromptsResult _cachedPromptsList;
        private ListResourcesResult _cachedResourcesList;
        private readonly object _cacheLock = new object();

        public McpServerPrimitiveCollection<Tool> Tools => _tools;
        public IReadOnlyList<Tool> AllTools => _allTools;
        public McpServerPrimitiveCollection<Prompt> Prompts => _prompts;
        public McpServerPrimitiveCollection<Resource> Resources => _resources;
        public IMcpTaskStore TaskStore => _taskStore;
        public int ConnectedClients => _transport?.ConnectedClients ?? 0;

        public McpServer(McpServerOptions options = null, ILogger logger = null, IMcpTaskStore taskStore = null)
        {
            _options = options ?? new McpServerOptions();
            _logger = logger ?? new UnityLoggerImpl();
            _taskStore = taskStore ?? new InMemoryMcpTaskStore();

            InitializeCapabilities();
            SetupHandlers();

            _transport = new HttpListenerServerTransport(_options.Port, _options.Path, _logger);
            _sessionHandler = new McpSessionHandler(_transport, _options, _requestHandlers, _notificationHandlers, _logger);
        }

        private void InitializeCapabilities()
        {
            if (_options.Capabilities == null)
            {
                _options.Capabilities = new ServerCapabilities();
            }

            _options.Capabilities.Tools = new ToolsCapability { ListChanged = true };
            _options.Capabilities.Prompts = new PromptsCapability { ListChanged = true };
            _options.Capabilities.Resources = new ResourcesCapability { Subscribe = true, ListChanged = true };
            _options.Capabilities.Tasks = new McpTasksCapability { Supported = true, Storage = "memory" };
        }

        private void SetupHandlers()
        {
            _requestHandlers.Set<ListToolsRequestParams, ListToolsResult>(RequestMethods.ToolsList, HandleToolsListAsync);
            _requestHandlers.Set<CallToolRequestParams, CallToolResult>(RequestMethods.ToolsCall, HandleToolsCallAsync);
            _requestHandlers.Set<ListPromptsRequestParams, ListPromptsResult>(RequestMethods.PromptsList, HandlePromptsListAsync);
            _requestHandlers.Set<GetPromptRequestParams, GetPromptResult>(RequestMethods.PromptsGet, HandlePromptsGetAsync);
            _requestHandlers.Set<ListResourcesRequestParams, ListResourcesResult>(RequestMethods.ResourcesList, HandleResourcesListAsync);
            _requestHandlers.Set<ReadResourceRequestParams, ReadResourceResult>(RequestMethods.ResourcesRead, HandleResourcesReadAsync);
            _requestHandlers.Set<GetTaskRequestParams, GetTaskResult>(RequestMethods.TasksGet, HandleTaskGetAsync);
            _requestHandlers.Set<ListTasksRequestParams, ListTasksResult>(RequestMethods.TasksList, HandleTaskListAsync);
            _requestHandlers.Set<CancelMcpTaskRequestParams, CancelMcpTaskResult>(RequestMethods.TasksCancel, HandleTaskCancelAsync);
        }

        private void InvalidateListCaches()
        {
            lock (_cacheLock)
            {
                _cachedToolsList = null;
                _cachedPromptsList = null;
                _cachedResourcesList = null;
            }
        }

        public void AddTool(Tool tool, Func<CallToolRequestParams, CancellationToken, Task<CallToolResult>> handler)
        {
            Throw.IfNull(tool);
            Throw.IfNullOrWhiteSpace(tool.Name);

            _tools.Add(tool);
            _toolHandlers[tool.Name] = handler;
            _logger.Log(LogLevel.Debug, $"Tool registered: {tool.Name}");
            InvalidateListCaches();
        }

        public void AddTool(string name, string description, Func<JObject, CancellationToken, Task<CallToolResult>> handler, JObject inputSchema = null)
        {
            var tool = new Tool
            {
                Name = name,
                Description = description,
                InputSchema = inputSchema ?? JObject.Parse("{\"type\":\"object\"}")
            };

            AddTool(tool, async (requestParams, ct) =>
            {
                return await handler(requestParams.Arguments, ct);
            });
        }

        public void AddPrompt(Prompt prompt, Func<GetPromptRequestParams, CancellationToken, Task<GetPromptResult>> handler)
        {
            Throw.IfNull(prompt);
            Throw.IfNullOrWhiteSpace(prompt.Name);

            _prompts.Add(prompt);
            _promptHandlers[prompt.Name] = handler;
            _logger.Log(LogLevel.Debug, $"Prompt registered: {prompt.Name}");
            InvalidateListCaches();
        }

        public void AddResource(Resource resource, Func<ReadResourceRequestParams, CancellationToken, Task<ReadResourceResult>> handler)
        {
            Throw.IfNull(resource);
            Throw.IfNullOrWhiteSpace(resource.Uri);

            _resources.Add(resource);
            _resourceHandlers[resource.Uri] = handler;
            _logger.Log(LogLevel.Debug, $"Resource registered: {resource.Uri}");
            InvalidateListCaches();
        }

        private Task<ListToolsResult> HandleToolsListAsync(ListToolsRequestParams requestParams, CancellationToken cancellationToken)
        {
            if (_cachedToolsList == null)
            {
                lock (_cacheLock)
                {
                    if (_cachedToolsList == null)
                    {
                        _cachedToolsList = new ListToolsResult
                        {
                            Tools = new List<Tool>(_tools)
                        };
                    }
                }
            }
            return Task.FromResult(_cachedToolsList);
        }

        private async Task<CallToolResult> HandleToolsCallAsync(CallToolRequestParams requestParams, CancellationToken cancellationToken)
        {
            if (requestParams == null || string.IsNullOrEmpty(requestParams.Name))
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "Tool name is required" }
                    }
                };
            }

            if (!_toolHandlers.TryGetValue(requestParams.Name, out var handler))
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"Tool not found: {requestParams.Name}" }
                    }
                };
            }

            try
            {
                return await handler(requestParams, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"Tool execution error: {ex.Message}", ex);
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"Error: {ex.Message}" }
                    }
                };
            }
        }

        private Task<ListPromptsResult> HandlePromptsListAsync(ListPromptsRequestParams requestParams, CancellationToken cancellationToken)
        {
            if (_cachedPromptsList == null)
            {
                lock (_cacheLock)
                {
                    if (_cachedPromptsList == null)
                    {
                        _cachedPromptsList = new ListPromptsResult
                        {
                            Prompts = new List<Prompt>(_prompts)
                        };
                    }
                }
            }
            return Task.FromResult(_cachedPromptsList);
        }

        private async Task<GetPromptResult> HandlePromptsGetAsync(GetPromptRequestParams requestParams, CancellationToken cancellationToken)
        {
            if (requestParams == null || string.IsNullOrEmpty(requestParams.Name))
            {
                return new GetPromptResult
                {
                    Messages = new List<PromptMessage>
                    {
                        new PromptMessage
                        {
                            Role = Role.Assistant,
                            Content = new TextContentBlock { Text = "Prompt name is required" }
                        }
                    }
                };
            }

            if (!_promptHandlers.TryGetValue(requestParams.Name, out var handler))
            {
                return new GetPromptResult
                {
                    Messages = new List<PromptMessage>
                    {
                        new PromptMessage
                        {
                            Role = Role.Assistant,
                            Content = new TextContentBlock { Text = $"Prompt not found: {requestParams.Name}" }
                        }
                    }
                };
            }

            return await handler(requestParams, cancellationToken);
        }

        private Task<ListResourcesResult> HandleResourcesListAsync(ListResourcesRequestParams requestParams, CancellationToken cancellationToken)
        {
            if (_cachedResourcesList == null)
            {
                lock (_cacheLock)
                {
                    if (_cachedResourcesList == null)
                    {
                        _cachedResourcesList = new ListResourcesResult
                        {
                            Resources = new List<Resource>(_resources)
                        };
                    }
                }
            }
            return Task.FromResult(_cachedResourcesList);
        }

        private async Task<ReadResourceResult> HandleResourcesReadAsync(ReadResourceRequestParams requestParams, CancellationToken cancellationToken)
        {
            if (requestParams == null || string.IsNullOrEmpty(requestParams.Uri))
            {
                return new ReadResourceResult
                {
                    Contents = new List<ResourceContents>
                    {
                        new TextResourceContents { Text = "Resource URI is required" }
                    }
                };
            }

            if (!_resourceHandlers.TryGetValue(requestParams.Uri, out var handler))
            {
                return new ReadResourceResult
                {
                    Contents = new List<ResourceContents>
                    {
                        new TextResourceContents { Text = $"Resource not found: {requestParams.Uri}" }
                    }
                };
            }

            return await handler(requestParams, cancellationToken);
        }

        private async Task<GetTaskResult> HandleTaskGetAsync(GetTaskRequestParams requestParams, CancellationToken cancellationToken)
        {
            if (requestParams == null || string.IsNullOrEmpty(requestParams.TaskId))
            {
                throw new McpException(McpErrorCode.InvalidParams, "Task ID is required");
            }

            var task = await _taskStore.GetTaskAsync(requestParams.TaskId, cancellationToken);
            if (task == null)
            {
                throw new McpException(McpErrorCode.InvalidParams, $"Task not found: {requestParams.TaskId}");
            }

            return new GetTaskResult { Task = task };
        }

        private async Task<ListTasksResult> HandleTaskListAsync(ListTasksRequestParams requestParams, CancellationToken cancellationToken)
        {
            McpTaskStatus? status = null;
            if (!string.IsNullOrEmpty(requestParams?.Status))
            {
                Enum.TryParse(requestParams.Status, true, out McpTaskStatus parsedStatus);
                status = parsedStatus;
            }

            var tasks = await _taskStore.ListTasksAsync(status, cancellationToken);
            return new ListTasksResult { Tasks = tasks };
        }

        private async Task<CancelMcpTaskResult> HandleTaskCancelAsync(CancelMcpTaskRequestParams requestParams, CancellationToken cancellationToken)
        {
            if (requestParams == null || string.IsNullOrEmpty(requestParams.TaskId))
            {
                throw new McpException(McpErrorCode.InvalidParams, "Task ID is required");
            }

            await _taskStore.CancelTaskAsync(requestParams.TaskId, requestParams.Reason, cancellationToken);
            return new CancelMcpTaskResult();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _transport.StartAsync(cancellationToken);
            _runTask = _sessionHandler.ProcessMessagesAsync(cancellationToken);
            _logger.Log(LogLevel.Information, $"MCP Server started on port {_options.Port}");
        }

        public void RegisterToolsFromClass<T>()
        {
            RegisterToolsFromClass(typeof(T));
        }

        public void RegisterToolsFromClass(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<McpServerToolAttribute>();
                if (attr != null)
                {
                    RegisterToolFromMethod(method, attr, type);
                }
            }
        }

        public void RegisterToolsFromInstance(object instance, string instanceId)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            if (string.IsNullOrEmpty(instanceId))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
            
            if (_instances.ContainsKey(instanceId))
            {
                _logger?.Log(LogLevel.Warning, $"Instance with ID '{instanceId}' already exists. Unregister it first.");
                return;
            }
            
            var type = instance.GetType();
            var classAttr = type.GetCustomAttribute<McpInstanceToolAttribute>();
            
            _instances[instanceId] = instance;
            _instanceToolNames[instanceId] = new List<string>();
            
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            foreach (var method in methods)
            {
                var methodAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                if (methodAttr != null && !methodAttr.Disable)
                {
                    RegisterInstanceToolFromMethod(method, methodAttr, instance, instanceId, classAttr);
                }
            }
            
            _logger?.Log(LogLevel.Information, $"Registered instance tools for '{instanceId}' ({methods.Length} methods)");
        }

        public void UnregisterInstanceTools(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;
            
            if (!_instanceToolNames.TryGetValue(instanceId, out var toolNames))
            {
                _logger?.Log(LogLevel.Warning, $"No instance tools found for ID '{instanceId}'");
                return;
            }
            
            foreach (var toolName in toolNames)
            {
                var toolToRemove = _tools.FirstOrDefault(t => t.Name == toolName);
                if (toolToRemove != null)
                {
                    _tools.Remove(toolToRemove);
                }
                _toolHandlers.Remove(toolName);
                _allTools.RemoveAll(t => t.Name == toolName);
            }
            
            _instanceToolNames.Remove(instanceId);
            _instances.Remove(instanceId);
            
            lock (_cacheLock)
            {
                _cachedToolsList = null;
            }
            
            _logger?.Log(LogLevel.Information, $"Unregistered {toolNames.Count} tools for instance '{instanceId}'");
        }

        private void RegisterInstanceToolFromMethod(MethodInfo method, McpServerToolAttribute attr, 
            object instance, string instanceId, McpInstanceToolAttribute classAttr)
        {
            string baseName = !string.IsNullOrEmpty(attr.Name) ? attr.Name : method.Name;
            string toolName = $"{instanceId}.{baseName}";
            
            string baseDescription = attr.Description ?? "";
            string classDescription = classAttr?.Description ?? "";
            string description = $"[Instance: {instanceId}] {classDescription} {baseDescription}".Trim();
            
            Tool tool;
            
            try
            {
                tool = new Tool
                {
                    Name = toolName,
                    Description = description,
                    InputSchema = attr.InputSchema ?? GenerateInputSchema(method),
                    IsDisabled = attr.Disable,
                    IsValid = true
                };
            }
            catch (McpException ex) when (ex.ErrorCode == McpErrorCode.InvalidParams)
            {
                var invalidTool = new Tool
                {
                    Name = toolName,
                    Description = description,
                    InputSchema = JObject.Parse("{\"type\":\"object\"}"),
                    IsDisabled = true,
                    IsValid = false,
                    ValidationError = ex.Message
                };
                
                _allTools.Add(invalidTool);
                _instanceToolNames[instanceId].Add(toolName);
                _logger?.Log(LogLevel.Warning, $"Instance tool '{toolName}' validation failed: {ex.Message}");
                return;
            }

            Func<CallToolRequestParams, CancellationToken, Task<CallToolResult>> handler = async (requestParams, ct) =>
            {
                try
                {
                    var parameters = method.GetParameters();
                    object[] args = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (param.ParameterType == typeof(CancellationToken))
                        {
                            args[i] = ct;
                        }
                        else if (param.ParameterType == typeof(CallToolRequestParams))
                        {
                            args[i] = requestParams;
                        }
                        else if (param.ParameterType == typeof(JObject))
                        {
                            args[i] = requestParams.Arguments;
                        }
                        else if (IsVectorType(param.ParameterType))
                        {
                            args[i] = GeometryTypeHandler.ParseGeometryArgument(requestParams.Arguments, param.Name, param.ParameterType, param.HasDefaultValue ? param.DefaultValue : null);
                        }
                        else if (IsVectorArrayType(param.ParameterType))
                        {
                            args[i] = GeometryTypeHandler.ParseGeometryArrayArgument(requestParams.Arguments, param.Name, param.ParameterType);
                        }
                        else if (IsCustomTypeArray(param.ParameterType))
                        {
                            args[i] = ParseCustomTypeArrayArgument(requestParams.Arguments, param);
                        }
                        else if (IsCustomType(param.ParameterType))
                        {
                            args[i] = ParseCustomTypeArgument(requestParams.Arguments, param);
                        }
                        else if (requestParams.Arguments != null && requestParams.Arguments.TryGetValue(param.Name, out var token))
                        {
                            args[i] = token.ToObject(param.ParameterType);
                        }
                        else if (param.HasDefaultValue)
                        {
                            args[i] = param.DefaultValue;
                        }
                        else
                        {
                            args[i] = param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
                        }
                    }

                    object result = method.Invoke(instance, args);

                    if (result is Task<CallToolResult> taskResult)
                    {
                        return await taskResult;
                    }
                    else if (result is Task<string> taskString)
                    {
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = await taskString }
                            }
                        };
                    }
                    else if (result is Task task)
                    {
                        await task;
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = "OK" }
                            }
                        };
                    }
                    else if (result is CallToolResult directResult)
                    {
                        return directResult;
                    }
                    else if (result is string text)
                    {
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = text }
                            }
                        };
                    }
                    else
                    {
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = result?.ToString() ?? "null" }
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"Error: {ex.InnerException?.Message ?? ex.Message}" }
                        }
                    };
                }
            };

            _allTools.Add(tool);
            _instanceToolNames[instanceId].Add(toolName);
            
            if (!tool.IsDisabled)
            {
                AddTool(tool, handler);
            }
        }

        private void RegisterToolFromMethod(MethodInfo method, McpServerToolAttribute attr, Type declaringType)
        {
            string name = !string.IsNullOrEmpty(attr.Name) ? attr.Name : method.Name;
            string description = attr.Description ?? "";

            Tool tool;
            bool isStatic = method.IsStatic;
            object instance = null;

            try
            {
                tool = new Tool
                {
                    Name = name,
                    Description = description,
                    InputSchema = attr.InputSchema ?? GenerateInputSchema(method),
                    IsDisabled = attr.Disable,
                    IsValid = true
                };

                if (!isStatic)
                {
                    instance = Activator.CreateInstance(declaringType);
                }
            }
            catch (McpException ex) when (ex.ErrorCode == McpErrorCode.InvalidParams)
            {
                var invalidTool = new Tool
                {
                    Name = name,
                    Description = description,
                    InputSchema = JObject.Parse("{\"type\":\"object\"}"),
                    IsDisabled = true,
                    IsValid = false,
                    ValidationError = ex.Message
                };
                
                _allTools.Add(invalidTool);
                _logger?.Log(LogLevel.Warning, $"Tool '{name}' validation failed: {ex.Message}");
                return;
            }

            Func<CallToolRequestParams, CancellationToken, Task<CallToolResult>> handler = async (requestParams, ct) =>
            {
                try
                {
                    var parameters = method.GetParameters();
                    object[] args = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];
                        if (param.ParameterType == typeof(CancellationToken))
                        {
                            args[i] = ct;
                        }
                        else if (param.ParameterType == typeof(CallToolRequestParams))
                        {
                            args[i] = requestParams;
                        }
                        else if (param.ParameterType == typeof(JObject))
                        {
                            args[i] = requestParams.Arguments;
                        }
                        else if (IsVectorType(param.ParameterType))
                        {
                            args[i] = GeometryTypeHandler.ParseGeometryArgument(requestParams.Arguments, param.Name, param.ParameterType, param.HasDefaultValue ? param.DefaultValue : null);
                        }
                        else if (IsVectorArrayType(param.ParameterType))
                        {
                            args[i] = GeometryTypeHandler.ParseGeometryArrayArgument(requestParams.Arguments, param.Name, param.ParameterType);
                        }
                        else if (IsCustomTypeArray(param.ParameterType))
                        {
                            args[i] = ParseCustomTypeArrayArgument(requestParams.Arguments, param);
                        }
                        else if (IsCustomType(param.ParameterType))
                        {
                            args[i] = ParseCustomTypeArgument(requestParams.Arguments, param);
                        }
                        else if (requestParams.Arguments != null && requestParams.Arguments.TryGetValue(param.Name, out var token))
                        {
                            args[i] = token.ToObject(param.ParameterType);
                        }
                        else if (param.HasDefaultValue)
                        {
                            args[i] = param.DefaultValue;
                        }
                        else
                        {
                            args[i] = param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
                        }
                    }

                    object result = method.Invoke(isStatic ? null : instance, args);

                    if (result is Task<CallToolResult> taskResult)
                    {
                        return await taskResult;
                    }
                    else if (result is Task<string> taskString)
                    {
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = await taskString }
                            }
                        };
                    }
                    else if (result is Task task)
                    {
                        await task;
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = "OK" }
                            }
                        };
                    }
                    else if (result is CallToolResult directResult)
                    {
                        return directResult;
                    }
                    else if (result is string text)
                    {
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = text }
                            }
                        };
                    }
                    else
                    {
                        return new CallToolResult
                        {
                            Content = new List<ContentBlock>
                            {
                                new TextContentBlock { Text = result?.ToString() ?? "null" }
                            }
                        };
                    }
                }
                catch (Exception ex)
                {
                    return new CallToolResult
                    {
                        IsError = true,
                        Content = new List<ContentBlock>
                        {
                            new TextContentBlock { Text = $"Error: {ex.InnerException?.Message ?? ex.Message}" }
                        }
                    };
                }
            };

            _allTools.Add(tool);
            if (!tool.IsDisabled)
            {
                AddTool(tool, handler);
            }
        }


        private JObject GenerateInputSchema(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return JObject.Parse("{\"type\":\"object\"}");
            }

            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject(),
                ["required"] = new JArray()
            };

            var properties = (JObject)schema["properties"];
            var required = (JArray)schema["required"];

            foreach (var param in parameters)
            {
                if (param.ParameterType == typeof(CancellationToken) || 
                    param.ParameterType == typeof(CallToolRequestParams) ||
                    param.ParameterType == typeof(JObject))
                {
                    continue;
                }

                var argAttr = param.GetCustomAttribute<McpArgumentAttribute>();
                string paramName = argAttr?.Name ?? param.Name;
                string paramDesc = argAttr?.Description ?? "";
                bool isRequired = argAttr?.Required ?? !param.HasDefaultValue;

                // 处理 Unity 向量类型 - 展开为多个参数
                if (IsVectorType(param.ParameterType))
                {
                    GeometryTypeHandler.AddGeometryProperties(properties, required, paramName, paramDesc, param.ParameterType, isRequired, param.DefaultValue);
                    continue;
                }

                // 处理 Unity 向量数组类型 - 使用扁平化浮点数组
                if (IsVectorArrayType(param.ParameterType))
                {
                    GeometryTypeHandler.AddGeometryArraySchema(properties, required, paramName, paramDesc, param.ParameterType, isRequired);
                    continue;
                }

                if (IsCustomTypeArray(param.ParameterType))
                {
                    var itemType = GetCustomArrayElementType(param.ParameterType);
                    var (isValid, error) = ValidateCustomType(itemType);
                    if (!isValid)
                    {
                        throw new McpException(McpErrorCode.InvalidParams, 
                            $"Tool '{method.Name}' has invalid custom type array parameter '{param.Name}': {error}");
                    }
                    
                    var itemSchema = GenerateCustomTypeSchema(itemType, "");
                    var arraySchema = new JObject
                    {
                        ["type"] = "array",
                        ["items"] = itemSchema,
                        ["description"] = paramDesc
                    };
                    properties[paramName] = arraySchema;
                    if (isRequired) required.Add(paramName);
                    continue;
                }

                if (IsCustomType(param.ParameterType))
                {
                    var (isValid, error) = ValidateCustomType(param.ParameterType);
                    if (!isValid)
                    {
                        throw new McpException(McpErrorCode.InvalidParams, 
                            $"Tool '{method.Name}' has invalid custom type parameter '{param.Name}': {error}");
                    }
                    
                    var customSchema = GenerateCustomTypeSchema(param.ParameterType, paramDesc);
                    properties[paramName] = customSchema;
                    if (isRequired) required.Add(paramName);
                    continue;
                }

                var propSchema = GeneratePropertySchema(param.ParameterType);

                if (!string.IsNullOrEmpty(paramDesc))
                {
                    propSchema["description"] = paramDesc;
                }

                if (param.HasDefaultValue && param.DefaultValue != null)
                {
                    var defaultValue = param.DefaultValue;
                    if (param.ParameterType.IsEnum && defaultValue is Enum enumValue)
                    {
                        propSchema["default"] = enumValue.ToString();
                    }
                    else
                    {
                        propSchema["default"] = JToken.FromObject(defaultValue);
                    }
                }

                properties[paramName] = propSchema;

                if (isRequired)
                {
                    required.Add(paramName);
                }
            }

            if (required.Count == 0)
            {
                schema.Remove("required");
            }

            return schema;
        }

        private bool IsVectorType(Type type)
        {
            return GeometryTypeHandler.IsGeometryType(type);
        }

        private bool IsVectorArrayType(Type type)
        {
            return GeometryTypeHandler.IsGeometryArrayType(type);
        }

        private Type GetVectorArrayElementType(Type type)
        {
            return GeometryTypeHandler.GetArrayElementType(type);
        }

        private bool IsCustomType(Type type)
        {
            if (type == null) return false;
            if (type.IsPrimitive) return false;
            if (type == typeof(string)) return false;
            if (type == typeof(decimal)) return false;
            if (type == typeof(DateTime)) return false;
            if (type == typeof(Guid)) return false;
            if (type.IsArray) return false;
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string)) return false;
            if (type.IsEnum) return false;
            if (type == typeof(object)) return false;
            
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine"))
                return false;
            
            if (type.Namespace != null && (type.Namespace == "System" || type.Namespace.StartsWith("System.")))
                return false;
            
            return type.IsClass || type.IsValueType;
        }

        private bool IsCustomTypeArray(Type type)
        {
            return GetCustomArrayElementType(type) != null;
        }

        private Type GetCustomArrayElementType(Type type)
        {
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (IsCustomType(elementType))
                    return elementType;
            }
            
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || 
                    genericDef == typeof(IList<>) || 
                    genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IReadOnlyList<>) ||
                    genericDef == typeof(IReadOnlyCollection<>))
                {
                    var elementType = type.GetGenericArguments()[0];
                    if (IsCustomType(elementType))
                        return elementType;
                }
            }
            
            return null;
        }

        private (bool isValid, string error) ValidateCustomType(Type type, HashSet<Type> visited = null)
        {
            if (visited == null) visited = new HashSet<Type>();
            
            if (!visited.Add(type))
                return (true, null);
            
            if (!IsCustomType(type))
                return (true, null);
            
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var validFields = new List<FieldInfo>();
            var invalidFields = new List<string>();
            
            foreach (var field in fields)
            {
                var jsonAttr = field.GetCustomAttribute<JsonPropertyAttribute>();
                var mcpAttr = field.GetCustomAttribute<McpArgumentAttribute>();
                
                bool hasJsonAttr = jsonAttr != null;
                bool hasMcpAttr = mcpAttr != null;
                
                if (hasJsonAttr || hasMcpAttr)
                {
                    if (hasJsonAttr && hasMcpAttr)
                    {
                        validFields.Add(field);
                        
                        // 警告：McpArgument.Name 对自定义类型字段无效
                        if (mcpAttr != null && !string.IsNullOrEmpty(mcpAttr.Name))
                        {
                            string jsonName = jsonAttr.PropertyName ?? field.Name;
                            if (mcpAttr.Name != jsonName)
                            {
                                _logger?.Log(LogLevel.Warning, 
                                    $"Field '{field.Name}' in type '{type.Name}': McpArgument.Name '{mcpAttr.Name}' is ignored for custom type fields. " +
                                    $"Using JsonProperty name '{jsonName}' instead.");
                            }
                        }
                        
                        if (IsCustomType(field.FieldType) || IsCustomTypeArray(field.FieldType))
                        {
                            var fieldType = field.FieldType;
                            if (fieldType.IsArray)
                                fieldType = fieldType.GetElementType();
                            else if (fieldType.IsGenericType)
                                fieldType = fieldType.GetGenericArguments()[0];
                            
                            var (nestedValid, nestedError) = ValidateCustomType(fieldType, visited);
                            if (!nestedValid)
                                return (false, $"Field '{field.Name}': {nestedError}");
                        }
                    }
                    else
                    {
                        string missingAttr;
                        if (hasJsonAttr && !hasMcpAttr)
                            missingAttr = "[McpArgument]";
                        else
                            missingAttr = "[JsonProperty]";
                        
                        invalidFields.Add($"Field '{field.Name}' is missing {missingAttr} attribute");
                    }
                }
            }
            
            if (invalidFields.Count > 0)
            {
                return (false, string.Join("; ", invalidFields));
            }
            
            if (validFields.Count == 0)
            {
                return (false, "No valid fields with [JsonProperty] and [McpArgument] attributes found");
            }
            
            return (true, null);
        }


        private JObject GenerateCustomTypeSchema(Type type, string description, HashSet<Type> visited = null)
        {
            if (visited == null) visited = new HashSet<Type>();
            
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject(),
                ["required"] = new JArray()
            };
            
            if (!string.IsNullOrEmpty(description))
                schema["description"] = description;
            
            if (!visited.Add(type))
                return schema;
            
            var properties = (JObject)schema["properties"];
            var required = (JArray)schema["required"];
            
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var jsonAttr = field.GetCustomAttribute<JsonPropertyAttribute>();
                var mcpAttr = field.GetCustomAttribute<McpArgumentAttribute>();
                var jsonRequiredAttr = field.GetCustomAttribute<JsonRequiredAttribute>();
                
                if (jsonAttr == null || mcpAttr == null)
                    continue;
                
                string fieldName = jsonAttr.PropertyName ?? field.Name;
                string fieldDesc = mcpAttr.Description ?? "";
                bool isRequired = jsonRequiredAttr != null || mcpAttr.Required;
                
                JObject fieldSchema;
                
                if (IsCustomType(field.FieldType))
                {
                    fieldSchema = GenerateCustomTypeSchema(field.FieldType, fieldDesc, visited);
                }
                else if (IsCustomTypeArray(field.FieldType))
                {
                    var itemType = GetCustomArrayElementType(field.FieldType);
                    var itemSchema = GenerateCustomTypeSchema(itemType, "", visited);
                    
                    fieldSchema = new JObject
                    {
                        ["type"] = "array",
                        ["items"] = itemSchema,
                        ["description"] = fieldDesc
                    };
                }
                else
                {
                    fieldSchema = GeneratePropertySchema(field.FieldType);
                    if (!string.IsNullOrEmpty(fieldDesc))
                        fieldSchema["description"] = fieldDesc;
                }
                
                properties[fieldName] = fieldSchema;
                
                if (isRequired)
                    required.Add(fieldName);
            }
            
            if (required.Count == 0)
                schema.Remove("required");
            
            return schema;
        }

        private object ParseCustomTypeArgument(JObject arguments, ParameterInfo param)
        {
            if (arguments == null || !arguments.TryGetValue(param.Name, out var token))
            {
                return param.HasDefaultValue 
                    ? param.DefaultValue 
                    : (param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null);
            }
            
            try
            {
                return token.ToObject(param.ParameterType);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Failed to parse custom type argument '{param.Name}': {ex.Message}");
                throw new McpException(McpErrorCode.InvalidParams, 
                    $"Invalid parameter '{param.Name}': {ex.Message}");
            }
        }

        private object ParseCustomTypeArrayArgument(JObject arguments, ParameterInfo param)
        {
            if (arguments == null || !arguments.TryGetValue(param.Name, out var token))
            {
                return param.HasDefaultValue 
                    ? param.DefaultValue 
                    : null;
            }
            
            try
            {
                return token.ToObject(param.ParameterType);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Failed to parse custom type array argument '{param.Name}': {ex.Message}");
                throw new McpException(McpErrorCode.InvalidParams, 
                    $"Invalid parameter '{param.Name}': {ex.Message}");
            }
        }

        private JObject GeneratePropertySchema(Type type)
        {
            var schema = new JObject();

            // 处理枚举类型
            if (type.IsEnum)
            {
                schema["type"] = "string";
                var enumValues = new JArray();
                foreach (var value in System.Enum.GetValues(type))
                {
                    enumValues.Add(value.ToString());
                }
                schema["enum"] = enumValues;
                return schema;
            }

            // 处理可空类型
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                // 可空枚举
                if (underlyingType.IsEnum)
                {
                    schema["type"] = "string";
                    var enumValues = new JArray();
                    foreach (var value in System.Enum.GetValues(underlyingType))
                    {
                        enumValues.Add(value.ToString());
                    }
                    schema["enum"] = enumValues;
                    return schema;
                }

                // 其他可空类型 - 递归处理基础类型
                return GeneratePropertySchema(underlyingType);
            }

            // 处理数组类型
            if (type.IsArray)
            {
                schema["type"] = "array";
                Type elementType = type.GetElementType();
                schema["items"] = GeneratePropertySchema(elementType);
                return schema;
            }

            // 处理泛型集合
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                
                if (genericDef == typeof(List<>) || 
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(HashSet<>) ||
                    genericDef == typeof(IReadOnlyList<>) ||
                    genericDef == typeof(IReadOnlyCollection<>))
                {
                    schema["type"] = "array";
                    Type elementType = type.GetGenericArguments()[0];
                    schema["items"] = GeneratePropertySchema(elementType);
                    return schema;
                }
            }

            // 基本类型
            if (type == typeof(string))
            {
                schema["type"] = "string";
            }
            else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || 
                     type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte))
            {
                schema["type"] = "integer";
            }
            else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                schema["type"] = "number";
            }
            else if (type == typeof(bool))
            {
                schema["type"] = "boolean";
            }
            else if (type == typeof(DateTime))
            {
                schema["type"] = "string";
                schema["format"] = "date-time";
            }
            else if (type == typeof(Guid))
            {
                schema["type"] = "string";
                schema["format"] = "uuid";
            }
            else if (type == typeof(byte[]))
            {
                schema["type"] = "string";
                schema["format"] = "byte";
            }
            else
            {
                schema["type"] = "string";
            }

            return schema;
        }

        public async Task SendNotificationAsync(string method, object parameters = null, CancellationToken cancellationToken = default)
        {
            var notification = new JsonRpcNotification
            {
                Method = method,
                Params = parameters != null ? JToken.FromObject(parameters) : null
            };

            await _sessionHandler.SendNotificationAsync(notification, cancellationToken);
        }

        public async Task NotifyResourceUpdatedAsync(string uri, CancellationToken cancellationToken = default)
        {
            var notification = new JsonRpcNotification
            {
                Method = NotificationMethods.ResourceUpdatedNotification,
                Params = JToken.FromObject(new ResourceUpdatedNotificationParams { Uri = uri })
            };

            await _sessionHandler.SendNotificationAsync(notification, cancellationToken);
            _logger.Log(LogLevel.Debug, $"Resource update notification sent: {uri}");
        }

        public async Task NotifyResourceListChangedAsync(CancellationToken cancellationToken = default)
        {
            var notification = new JsonRpcNotification
            {
                Method = NotificationMethods.ResourceListChangedNotification,
                Params = JToken.FromObject(new ResourceListChangedNotificationParams())
            };

            await _sessionHandler.SendNotificationAsync(notification, cancellationToken);
            _logger.Log(LogLevel.Debug, "Resource list changed notification sent");
        }

        public async ValueTask DisposeAsync()
        {
            await _transport.DisposeAsync();
            await _sessionHandler.DisposeAsync();
            _logger.Log(LogLevel.Information, "MCP Server disposed");
        }
    }
}
