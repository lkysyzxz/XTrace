using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class ListRootsRequestParams
    {
        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ListRootsResult
    {
        [JsonProperty("roots")]
        public List<Root> Roots { get; set; } = new List<Root>();

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class SetLevelRequestParams
    {
        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class CompleteRequestParams
    {
        [JsonProperty("ref")]
        public JObject Ref { get; set; }

        [JsonProperty("argument")]
        public CompleteArgument Argument { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class CompleteArgument
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class CompleteResult
    {
        [JsonProperty("completion")]
        public CompletionInfo Completion { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class CompletionInfo
    {
        [JsonProperty("values")]
        public List<string> Values { get; set; } = new List<string>();

        [JsonProperty("total", NullValueHandling = NullValueHandling.Ignore)]
        public int? Total { get; set; }

        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }
    }
}
