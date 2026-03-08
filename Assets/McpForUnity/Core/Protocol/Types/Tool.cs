using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class Tool
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("inputSchema")]
        public JToken InputSchema { get; set; } = JObject.Parse("{\"type\":\"object\"}");

        [JsonProperty("outputSchema", NullValueHandling = NullValueHandling.Ignore)]
        public JToken OutputSchema { get; set; }

        [JsonProperty("annotations", NullValueHandling = NullValueHandling.Ignore)]
        public ToolAnnotations Annotations { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }

        [JsonIgnore]
        public bool IsDisabled { get; set; }

        [JsonIgnore]
        public bool IsValid { get; set; } = true;

        [JsonIgnore]
        public string ValidationError { get; set; }
    }

    public class ToolAnnotations
    {
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("readOnlyHint")]
        public bool ReadOnlyHint { get; set; }

        [JsonProperty("destructiveHint")]
        public bool DestructiveHint { get; set; } = true;

        [JsonProperty("idempotentHint")]
        public bool IdempotentHint { get; set; }

        [JsonProperty("openWorldHint")]
        public bool OpenWorldHint { get; set; } = true;
    }

    public class ToolExecution
    {
        [JsonProperty("taskId")]
        public string TaskId { get; set; }
    }

    public class ToolTaskSupport
    {
        [JsonProperty("supported")]
        public bool Supported { get; set; }
    }
}
