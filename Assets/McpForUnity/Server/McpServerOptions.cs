using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server
{
    public class McpServerOptions
    {
        public Implementation ServerInfo { get; set; } = new Implementation
        {
            Name = "UnityMCP",
            Version = "1.0.0"
        };

        public string Instructions { get; set; }
        public ServerCapabilities Capabilities { get; set; } = new ServerCapabilities();
        public int Port { get; set; } = 3000;
        public string Path { get; set; } = "/mcp";
        public string ProtocolVersion { get; set; } = "2025-11-25";
    }
}
