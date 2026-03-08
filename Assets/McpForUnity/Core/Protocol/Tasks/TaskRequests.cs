using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class GetTaskRequestParams
    {
        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class GetTaskResult
    {
        [JsonProperty("task")]
        public McpTask Task { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class GetTaskPayloadRequestParams
    {
        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ListTasksRequestParams
    {
        [JsonProperty("cursor", NullValueHandling = NullValueHandling.Ignore)]
        public string Cursor { get; set; }

        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ListTasksResult
    {
        [JsonProperty("tasks")]
        public List<McpTask> Tasks { get; set; } = new List<McpTask>();

        [JsonProperty("nextCursor", NullValueHandling = NullValueHandling.Ignore)]
        public string NextCursor { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class CancelMcpTaskRequestParams
    {
        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
        public string Reason { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class CancelMcpTaskResult
    {
        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class CreateTaskResult
    {
        [JsonProperty("task")]
        public McpTask Task { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }
}
