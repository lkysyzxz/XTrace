using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ModelContextProtocol.Unity
{
    public class McpServerHost : IMcpServerHost
    {
        private readonly McpServerHostOptions _options;
        private readonly ILogger _logger;
        private McpServer _server;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private bool _disposed;

        public McpServer Server => _server;
        public bool IsRunning => _isRunning;
        public int Port => _options.Port;
        public int ConnectedClients => _server?.ConnectedClients ?? 0;

        public event Action OnServerStarted;
        public event Action OnServerStopped;
        public event Action<string> OnServerError;

        public McpServerHost(McpServerHostOptions options = null, ILogger logger = null)
        {
            _options = options ?? new McpServerHostOptions();
            _logger = logger ?? new UnityLoggerImpl();

            UnityLogger.MinimumLevel = _options.LogLevel;

            Application.quitting += OnApplicationQuitting;
        }

        private void OnApplicationQuitting()
        {
            if (_isRunning)
            {
                _cts?.Cancel();
            }
        }

        public async Task StartAsync()
        {
            if (_isRunning)
            {
                Debug.LogWarning("[MCP] Server is already running");
                return;
            }

            try
            {
                _cts = new CancellationTokenSource();

                var mcpOptions = new McpServerOptions
                {
                    Port = _options.Port,
                    ServerInfo = new Implementation
                    {
                        Name = _options.ServerName,
                        Version = _options.ServerVersion
                    },
                    Instructions = _options.Instructions
                };

                _server = new McpServer(mcpOptions, _logger);

                await _server.StartAsync(_cts.Token);
                _isRunning = true;

                Debug.Log($"[MCP] Server started at http://localhost:{_options.Port}/mcp");
                OnServerStarted?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Failed to start server: {ex.Message}");
                OnServerError?.Invoke(ex.Message);
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning || _server == null) return;

            try
            {
                _cts?.Cancel();
                await _server.DisposeAsync();
                _server = null;
                _isRunning = false;

                Debug.Log("[MCP] Server stopped");
                OnServerStopped?.Invoke();
            }
            catch (Exception)
            {
                Debug.Log("[MCP] Server stopped");
                OnServerStopped?.Invoke();
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        public void AddCustomTool(string name, string description, Func<JObject, CancellationToken, Task<CallToolResult>> handler, JObject inputSchema = null)
        {
            if (_server == null)
            {
                Debug.LogError("[MCP] Server not initialized");
                return;
            }

            _server.AddTool(name, description, handler, inputSchema);
        }

        public void AddCustomTool<T>(string name, string description, Func<T, CancellationToken, Task<CallToolResult>> handler)
        {
            if (_server == null)
            {
                Debug.LogError("[MCP] Server not initialized");
                return;
            }

            _server.AddTool(name, description, async (args, ct) =>
            {
                T typedArgs = args != null ? args.ToObject<T>() : default;
                return await handler(typedArgs, ct);
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            Application.quitting -= OnApplicationQuitting;

            if (_isRunning)
            {
                await StopAsync();
            }
        }
    }
}
