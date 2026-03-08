using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class ListToolsRequestParams
    {
        [JsonProperty("cursor", NullValueHandling = NullValueHandling.Ignore)]
        public string Cursor { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ListToolsResult
    {
        [JsonProperty("tools")]
        public List<Tool> Tools { get; set; } = new List<Tool>();

        [JsonProperty("nextCursor", NullValueHandling = NullValueHandling.Ignore)]
        public string NextCursor { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class CallToolRequestParams
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Arguments { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class CallToolResult
    {
        [JsonProperty("content")]
        public List<ContentBlock> Content { get; set; } = new List<ContentBlock>();

        [JsonProperty("structuredContent", NullValueHandling = NullValueHandling.Ignore)]
        public JToken StructuredContent { get; set; }

        [JsonProperty("isError")]
        public bool IsError { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }
}
