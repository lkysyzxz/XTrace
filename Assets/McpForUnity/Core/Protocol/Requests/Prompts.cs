using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class ListPromptsRequestParams
    {
        [JsonProperty("cursor", NullValueHandling = NullValueHandling.Ignore)]
        public string Cursor { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ListPromptsResult
    {
        [JsonProperty("prompts")]
        public List<Prompt> Prompts { get; set; } = new List<Prompt>();

        [JsonProperty("nextCursor", NullValueHandling = NullValueHandling.Ignore)]
        public string NextCursor { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class GetPromptRequestParams
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Arguments { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class GetPromptResult
    {
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("messages")]
        public List<PromptMessage> Messages { get; set; } = new List<PromptMessage>();

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }
}
