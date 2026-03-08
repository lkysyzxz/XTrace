#if UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Samples.InstanceTools;
using ModelContextProtocol.Server;
using ModelContextProtocol.Unity;

namespace ModelContextProtocol.Editor
{
    public static class GlobalEditorMcpServer
    {
        private static McpServer _server;
        private static CancellationTokenSource _cts;
        private static int _port = 8090;
        private static bool _resourcesEnabled = false;
        private static bool _fileWatchingEnabled = false;
        private static EditorResourcesService _resourcesService;

        public static McpServer Server => _server;
        public static bool IsRunning => _server != null;

        public static int Port
        {
            get => _port;
            set => _port = Mathf.Clamp(value, 1, 65535);
        }

        public static bool ResourcesEnabled
        {
            get => _resourcesEnabled;
            set => _resourcesEnabled = value;
        }

        public static bool FileWatchingEnabled
        {
            get => _fileWatchingEnabled;
            set => _fileWatchingEnabled = value;
        }

        public static EditorResourcesService ResourcesService => _resourcesService;

        public static async void StartServer()
        {
            if (_server != null)
            {
                Debug.LogWarning("[MCP Editor] Server is already running");
                return;
            }

            try
            {
                _cts = new CancellationTokenSource();

                var options = new McpServerOptions
                {
                    Port = _port,
                    ServerInfo = new Implementation
                    {
                        Name = "UnityMCPEditor",
                        Version = "1.0.0"
                    },
                    Instructions = "Unity MCP Editor Server - Control Unity Editor from AI assistants"
                };

                _server = new McpServer(options, new UnityLoggerImpl());
                _server.RegisterToolsFromClass(typeof(EditorToolsList));

                if (_resourcesEnabled)
                {
                    _resourcesService = new EditorResourcesService();
                    _resourcesService.RegisterResources(_server, _fileWatchingEnabled);
                    Debug.Log($"[MCP Editor] Resources enabled. Registered {_resourcesService.GetResources().Count} resources.");
                }

                await _server.StartAsync(_cts.Token);

                Debug.Log($"[MCP Editor] Server started at http://localhost:{_port}/mcp");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Editor] Failed to start server: {ex.Message}");
                CleanupServer();
            }
        }

        public static async void StopServer()
        {
            if (_server == null)
            {
                Debug.LogWarning("[MCP Editor] Server is not running");
                return;
            }

            try
            {
                _resourcesService?.StopWatching();
                
                await _server.DisposeAsync();
                Debug.Log("[MCP Editor] Server stopped");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MCP Editor] Error stopping server: {ex.Message}");
            }
            finally
            {
                CleanupServer();
            }
        }

        private static void CleanupServer()
        {
            _cts?.Dispose();
            _cts = null;
            _server = null;
            _resourcesService = null;
        }
    }
}
#endif
