using Newtonsoft.Json;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Samples.CustomTypes
{
    public class AddressInfo
    {
        [JsonProperty("street")]
        [McpArgument(Description = "街道地址", Required = true)]
        public string Street;

        [JsonProperty("city")]
        [McpArgument(Description = "城市名称", Required = true)]
        public string City;

        [JsonProperty("zipCode")]
        [McpArgument(Description = "邮政编码")]
        public string ZipCode;

        [JsonProperty("country")]
        [McpArgument(Description = "国家")]
        public string Country = "China";
    }
}
