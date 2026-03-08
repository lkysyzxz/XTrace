using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class JsonRpcResponse : JsonRpcMessageWithId
    {
        [JsonProperty("result")]
        public JToken Result { get; set; }
    }
}
