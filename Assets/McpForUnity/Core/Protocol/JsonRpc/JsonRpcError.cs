using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    public class JsonRpcError : JsonRpcMessageWithId
    {
        [JsonProperty("error")]
        public JsonRpcErrorDetail Error { get; set; }
    }

    public class JsonRpcErrorDetail
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }
    }
}
