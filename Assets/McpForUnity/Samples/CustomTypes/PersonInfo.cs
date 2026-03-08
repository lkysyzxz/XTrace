using Newtonsoft.Json;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Samples.CustomTypes
{
    public class PersonInfo
    {
        [JsonProperty("name")]
        [McpArgument(Description = "姓名", Required = true)]
        public string Name;

        [JsonProperty("age")]
        [McpArgument(Description = "年龄")]
        public int Age;

        [JsonProperty("email")]
        [McpArgument(Description = "电子邮箱")]
        public string Email;

        [JsonProperty("address")]
        [McpArgument(Description = "地址信息（嵌套对象）")]
        public AddressInfo Address;
    }
}
