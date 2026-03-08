using Newtonsoft.Json;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Samples.CustomTypes
{
    /// <summary>
    /// 非法自定义类型示例 - 用于测试严格验证模式
    /// 
    /// 此类型应该被验证拒绝，原因：
    /// - Name 字段只有 [JsonProperty]，缺少 [McpArgument]
    /// - Age 字段只有 [McpArgument]，缺少 [JsonProperty]
    /// 
    /// 严格验证模式规则：
    /// 如果字段使用了 [JsonProperty] 或 [McpArgument] 中的任何一个属性，
    /// 则必须同时使用两个属性，否则类型被判定为非法。
    /// </summary>
    public class InvalidCustomType
    {
        [JsonProperty("name")]
        public string Name;

        [McpArgument(Description = "年龄")]
        public int Age;

        [JsonProperty("email")]
        [McpArgument(Description = "邮箱")]
        public string Email;
    }
}
