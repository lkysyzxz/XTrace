using System.Collections.Generic;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ModelContextProtocol.Samples.InstanceTools
{
    /// <summary>
    /// 敌人实例工具示例
    /// 演示同一类型的多实例注册
    /// </summary>
    [McpInstanceTool(Name = "Enemy", Description = "敌人实例工具")]
    public class EnemyInstance
    {
        public string EnemyType { get; set; }
        public int Health { get; set; }
        public int AttackPower { get; set; }
        public bool IsAlive { get; set; } = true;

        public EnemyInstance(string enemyType, int health, int attackPower)
        {
            EnemyType = enemyType;
            Health = health;
            AttackPower = attackPower;
        }

        [McpServerTool(Description = "获取敌人信息")]
        public CallToolResult GetInfo()
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock 
                    { 
                        Text = $"Type: {EnemyType}\nHealth: {Health}\nAttack: {AttackPower}\nAlive: {IsAlive}" 
                    }
                }
            };
        }

        [McpServerTool(Description = "攻击敌人")]
        public CallToolResult TakeDamage(
            [McpArgument(Description = "伤害量", Required = true)] int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;
                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"Enemy defeated! Dealt {damage} damage." }
                    }
                };
            }
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = $"Dealt {damage} damage. Remaining health: {Health}" }
                }
            };
        }
    }
}
