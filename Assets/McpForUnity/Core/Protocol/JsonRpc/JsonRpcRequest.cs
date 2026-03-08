using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class JsonRpcRequest : JsonRpcMessageWithId
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Params { get; set; }
    }
}
