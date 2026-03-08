using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    public class ClientCapabilities
    {
        [JsonProperty("experimental", NullValueHandling = NullValueHandling.Ignore)]
        public ExperimentalCapabilities Experimental { get; set; }

        [JsonProperty("roots", NullValueHandling = NullValueHandling.Ignore)]
        public RootsCapability Roots { get; set; }

        [JsonProperty("sampling", NullValueHandling = NullValueHandling.Ignore)]
        public SamplingCapability Sampling { get; set; }

        [JsonProperty("elicitation", NullValueHandling = NullValueHandling.Ignore)]
        public ElicitationCapability Elicitation { get; set; }

        [JsonProperty("tasks", NullValueHandling = NullValueHandling.Ignore)]
        public RequestMcpTasksCapability Tasks { get; set; }
    }

    public class RootsCapability
    {
        [JsonProperty("listChanged")]
        public bool ListChanged { get; set; }
    }

    public class SamplingCapability { }

    public class ElicitationCapability { }
}
