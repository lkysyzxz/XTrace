using System;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Server
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class McpServerToolAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public JObject InputSchema { get; set; }
        public bool ReadOnly { get; set; }
        public bool Destructive { get; set; } = true;
        public bool Idempotent { get; set; }
        public bool OpenWorld { get; set; } = true;
        public bool Disable { get; set; }

        public McpServerToolAttribute() { }

        public McpServerToolAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class McpServerPromptAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public McpServerPromptAttribute() { }

        public McpServerPromptAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class McpServerResourceAttribute : Attribute
    {
        public string Uri { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string MimeType { get; set; }

        public McpServerResourceAttribute(string uri)
        {
            Uri = uri;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field)]
    public class McpArgumentAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }

        public McpArgumentAttribute() { }

        public McpArgumentAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class McpInstanceToolAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public McpInstanceToolAttribute() { }

        public McpInstanceToolAttribute(string name)
        {
            Name = name;
        }
    }
}
