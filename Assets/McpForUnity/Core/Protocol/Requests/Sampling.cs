using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class CreateMessageRequestParams
    {
        [JsonProperty("messages")]
        public List<SamplingMessage> Messages { get; set; } = new List<SamplingMessage>();

        [JsonProperty("modelPreferences", NullValueHandling = NullValueHandling.Ignore)]
        public ModelPreferences ModelPreferences { get; set; }

        [JsonProperty("systemPrompt", NullValueHandling = NullValueHandling.Ignore)]
        public string SystemPrompt { get; set; }

        [JsonProperty("includeContext")]
        public string IncludeContext { get; set; } = "none";

        [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
        public double? Temperature { get; set; }

        [JsonProperty("maxTokens")]
        public int MaxTokens { get; set; }

        [JsonProperty("stopSequences", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> StopSequences { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Metadata { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class SamplingMessage
    {
        [JsonProperty("role")]
        public Role Role { get; set; }

        [JsonProperty("content")]
        public ContentBlock Content { get; set; }
    }

    public class ModelPreferences
    {
        [JsonProperty("hints", NullValueHandling = NullValueHandling.Ignore)]
        public List<ModelHint> Hints { get; set; }

        [JsonProperty("costPriority", NullValueHandling = NullValueHandling.Ignore)]
        public double? CostPriority { get; set; }

        [JsonProperty("speedPriority", NullValueHandling = NullValueHandling.Ignore)]
        public double? SpeedPriority { get; set; }

        [JsonProperty("intelligencePriority", NullValueHandling = NullValueHandling.Ignore)]
        public double? IntelligencePriority { get; set; }
    }

    public class ModelHint
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }

    public class CreateMessageResult
    {
        [JsonProperty("role")]
        public Role Role { get; set; } = Role.Assistant;

        [JsonProperty("content")]
        public ContentBlock Content { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("stopReason", NullValueHandling = NullValueHandling.Ignore)]
        public string StopReason { get; set; }

        [JsonProperty("usage", NullValueHandling = NullValueHandling.Ignore)]
        public UsageInfo Usage { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class UsageInfo
    {
        [JsonProperty("inputTokens")]
        public int InputTokens { get; set; }

        [JsonProperty("outputTokens")]
        public int OutputTokens { get; set; }

        [JsonProperty("totalTokens", NullValueHandling = NullValueHandling.Ignore)]
        public int? TotalTokens { get; set; }
    }
}
