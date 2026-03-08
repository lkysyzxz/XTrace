using System;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Unity
{
    public interface IMcpServerHost : IAsyncDisposable
    {
        McpServer Server { get; }
        bool IsRunning { get; }
        int Port { get; }
        int ConnectedClients { get; }

        event Action OnServerStarted;
        event Action OnServerStopped;
        event Action<string> OnServerError;

        Task StartAsync();
        Task StopAsync();

        void AddCustomTool(string name, string description,
            Func<JObject, CancellationToken, Task<CallToolResult>> handler,
            JObject inputSchema = null);

        void AddCustomTool<T>(string name, string description,
            Func<T, CancellationToken, Task<CallToolResult>> handler);
    }
}
