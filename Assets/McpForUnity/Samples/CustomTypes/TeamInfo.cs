using System.Collections.Generic;
using Newtonsoft.Json;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Samples.CustomTypes
{
    public class TeamInfo
    {
        [JsonProperty("teamName")]
        [McpArgument(Description = "团队名称", Required = true)]
        public string TeamName;

        [JsonProperty("members")]
        [McpArgument(Description = "团队成员列表（PersonInfo数组）")]
        public PersonInfo[] Members;

        [JsonProperty("tags")]
        [McpArgument(Description = "团队标签")]
        public List<string> Tags;
    }
}
