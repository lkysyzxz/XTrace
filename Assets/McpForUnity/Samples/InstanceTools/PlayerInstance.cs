using System.Collections.Generic;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Samples.InstanceTools
{
    /// <summary>
    /// 玩家实例工具示例
    /// 
    /// 使用 McpInstanceTool 特性标记类，表示该类的实例可以作为工具注册。
    /// 每个玩家实例可以独立注册，工具名称格式为: {instanceId}.{methodName}
    /// </summary>
    [McpInstanceTool(Name = "Player", Description = "玩家实例工具")]
    public class PlayerInstance
    {
        public int Health { get; set; }
        public int MaxHealth { get; set; } = 100;
        public string Name { get; set; }
        public int Level { get; set; } = 1;
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }

        public PlayerInstance(string name, int maxHealth = 100)
        {
            Name = name;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }

        [McpServerTool(Description = "获取玩家当前生命值")]
        public int GetHealth()
        {
            return Health;
        }

        [McpServerTool(Description = "设置玩家生命值")]
        public CallToolResult SetHealth(
            [McpArgument(Description = "生命值", Required = true)] int value)
        {
            Health = UnityEngine.Mathf.Clamp(value, 0, MaxHealth);
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Health set to {Health}/{MaxHealth}" }
                }
            };
        }

        [McpServerTool(Description = "获取玩家名称")]
        public string GetName()
        {
            return Name;
        }

        [McpServerTool(Description = "获取玩家等级")]
        public int GetLevel()
        {
            return Level;
        }

        [McpServerTool(Description = "设置玩家等级")]
        public CallToolResult SetLevel(
            [McpArgument(Description = "等级", Required = true)] int level)
        {
            Level = level;
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Level set to {Level}" }
                }
            };
        }

        [McpServerTool(Description = "获取玩家状态信息")]
        public CallToolResult GetStatus()
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock 
                    { 
                        Text = $"Player: {Name}\nLevel: {Level}\nHealth: {Health}/{MaxHealth}\nPosition: ({PositionX:F1}, {PositionY:F1}, {PositionZ:F1})" 
                    }
                }
            };
        }

        [McpServerTool(Description = "设置玩家位置")]
        public CallToolResult SetPosition(
            [McpArgument(Description = "X坐标", Required = true)] float x,
            [McpArgument(Description = "Y坐标", Required = true)] float y,
            [McpArgument(Description = "Z坐标", Required = true)] float z)
        {
            PositionX = x;
            PositionY = y;
            PositionZ = z;
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Position set to ({x}, {y}, {z})" }
                }
            };
        }

        [McpServerTool(Description = "治疗玩家")]
        public CallToolResult Heal(
            [McpArgument(Description = "治疗量", Required = true)] int amount)
        {
            int oldHealth = Health;
            Health = UnityEngine.Mathf.Min(Health + amount, MaxHealth);
            int healed = Health - oldHealth;
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Healed {healed} HP. Current health: {Health}/{MaxHealth}" }
                }
            };
        }

        [McpServerTool(Description = "对玩家造成伤害")]
        public CallToolResult Damage(
            [McpArgument(Description = "伤害量", Required = true)] int amount)
        {
            int oldHealth = Health;
            Health = UnityEngine.Mathf.Max(Health - amount, 0);
            int damaged = oldHealth - Health;
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Dealt {damaged} damage. Current health: {Health}/{MaxHealth}" }
                }
            };
        }
    }
}
