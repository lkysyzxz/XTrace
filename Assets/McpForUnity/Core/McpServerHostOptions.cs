namespace ModelContextProtocol.Unity
{
    public class McpServerHostOptions
    {
        public int Port { get; set; } = 3000;
        public string ServerName { get; set; } = "UnityMCP";
        public string ServerVersion { get; set; } = "1.0.0";
        public string Instructions { get; set; } = "Unity MCP Server - Control Unity from AI assistants";
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
    }
}
