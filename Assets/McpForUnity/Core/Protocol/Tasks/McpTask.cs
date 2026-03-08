using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public enum McpTaskStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public class McpTask
    {
        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("status")]
        public McpTaskStatus Status { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("modified", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Modified { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public McpTaskMetadata Metadata { get; set; }

        [JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Context { get; set; }

        [JsonProperty("pollInterval", NullValueHandling = NullValueHandling.Ignore)]
        public int? PollInterval { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class McpTaskMetadata
    {
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("progressHint", NullValueHandling = NullValueHandling.Ignore)]
        public ProgressHint ProgressHint { get; set; }
    }

    public class ProgressHint
    {
        [JsonProperty("total", NullValueHandling = NullValueHandling.Ignore)]
        public int? Total { get; set; }

        [JsonProperty("current", NullValueHandling = NullValueHandling.Ignore)]
        public int? Current { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }
    }
}
