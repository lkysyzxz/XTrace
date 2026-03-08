using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class PromptMessage
    {
        [JsonProperty("role")]
        public Role Role { get; set; }

        [JsonProperty("content")]
        public ContentBlock Content { get; set; }
    }

    public class Prompt
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
        public List<PromptArgument> Arguments { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class PromptArgument
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }
    }

    public class PromptReference
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "ref/prompt";

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class ResourceTemplateReference
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "ref/resource";

        [JsonProperty("uri")]
        public string Uri { get; set; }
    }
}
