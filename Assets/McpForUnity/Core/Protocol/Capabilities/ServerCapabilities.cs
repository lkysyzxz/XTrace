using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    public class ServerCapabilities
    {
        [JsonProperty("experimental", NullValueHandling = NullValueHandling.Ignore)]
        public ExperimentalCapabilities Experimental { get; set; }

        [JsonProperty("logging", NullValueHandling = NullValueHandling.Ignore)]
        public object Logging { get; set; }

        [JsonProperty("prompts", NullValueHandling = NullValueHandling.Ignore)]
        public PromptsCapability Prompts { get; set; }

        [JsonProperty("resources", NullValueHandling = NullValueHandling.Ignore)]
        public ResourcesCapability Resources { get; set; }

        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public ToolsCapability Tools { get; set; }

        [JsonProperty("completions", NullValueHandling = NullValueHandling.Ignore)]
        public CompletionsCapability Completions { get; set; }

        [JsonProperty("tasks", NullValueHandling = NullValueHandling.Ignore)]
        public McpTasksCapability Tasks { get; set; }
    }

    public class ExperimentalCapabilities { }

    public class PromptsCapability
    {
        [JsonProperty("listChanged")]
        public bool ListChanged { get; set; }
    }

    public class ResourcesCapability
    {
        [JsonProperty("subscribe")]
        public bool Subscribe { get; set; }

        [JsonProperty("listChanged")]
        public bool ListChanged { get; set; }
    }

    public class ToolsCapability
    {
        [JsonProperty("listChanged")]
        public bool ListChanged { get; set; }
    }

    public class CompletionsCapability { }

    public class McpTasksCapability
    {
        [JsonProperty("supported")]
        public bool Supported { get; set; } = true;

        [JsonProperty("storage")]
        public string Storage { get; set; } = "memory";
    }

    public class RequestMcpTasksCapability
    {
        [JsonProperty("supported")]
        public bool Supported { get; set; }
    }
}
