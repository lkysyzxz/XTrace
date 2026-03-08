using Newtonsoft.Json;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Samples.CustomTypes
{
    /// <summary>
    /// 演示 JsonRequired 特性使用的示例类型
    /// 
    /// Required 字段判定规则：
    /// 1. [JsonRequired] 存在 → Required = true（最高优先级）
    /// 2. [McpArgument(Required = true)] → Required = true（回退选项）
    /// 3. 都没有 → Required = false
    /// </summary>
    public class JsonRequiredExample
    {
        [JsonProperty("name")]
        [JsonRequired]
        [McpArgument(Description = "名称（使用 JsonRequired 标记为必需）")]
        public string Name;

        [JsonProperty("email")]
        [McpArgument(Description = "邮箱（使用 McpArgument.Required 标记为必需）", Required = true)]
        public string Email;

        [JsonProperty("phone")]
        [McpArgument(Description = "电话（可选字段）")]
        public string Phone;

        [JsonProperty("age")]
        [JsonRequired]
        [McpArgument(Description = "年龄（必需）")]
        public int Age;
    }
}
