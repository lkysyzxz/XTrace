using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class InitializedNotificationParams
    {
        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class CancelledNotificationParams
    {
        [JsonProperty("requestId")]
        public RequestId RequestId { get; set; }

        [JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
        public string Reason { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ProgressNotificationParams
    {
        [JsonProperty("progressToken")]
        public ProgressToken ProgressToken { get; set; }

        [JsonProperty("progress")]
        public double Progress { get; set; }

        [JsonProperty("total", NullValueHandling = NullValueHandling.Ignore)]
        public double? Total { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class LoggingMessageNotificationParams
    {
        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("logger", NullValueHandling = NullValueHandling.Ignore)]
        public string Logger { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ToolListChangedNotificationParams
    {
        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class PromptListChangedNotificationParams
    {
        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ResourceListChangedNotificationParams
    {
        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ResourceUpdatedNotificationParams
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class RootsListChangedNotificationParams
    {
        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ElicitationCompleteNotificationParams
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class McpTaskStatusNotificationParams
    {
        [JsonProperty("taskId")]
        public string TaskId { get; set; }

        [JsonProperty("status")]
        public McpTaskStatus Status { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }
}
