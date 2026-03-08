using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    public class ElicitRequestParams
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("requestedSchema")]
        public JObject RequestedSchema { get; set; }

        [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
        public string Uri { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class ElicitResult
    {
        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Content { get; set; }

        [JsonProperty("_meta", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Meta { get; set; }
    }

    public class UrlElicitationRequiredErrorData
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    public class UrlElicitationRequiredException : McpProtocolException
    {
        public string Uri { get; }

        public UrlElicitationRequiredException(string uri) 
            : base(McpErrorCode.UrlElicitationRequired, "URL elicitation required")
        {
            Uri = uri;
        }

        public UrlElicitationRequiredErrorData CreateErrorData()
        {
            return new UrlElicitationRequiredErrorData { Uri = Uri };
        }
    }
}
