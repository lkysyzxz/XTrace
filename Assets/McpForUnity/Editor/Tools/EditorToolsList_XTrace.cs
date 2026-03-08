#if UNITY_EDITOR
using System.Collections.Generic;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using XTrace;

namespace ModelContextProtocol.Editor
{
    public static partial class EditorToolsList
    {
        [McpServerTool("EditorGetXTraceSamplers", Description = "Get all XTrace samplers with their info (uniqueName, description, enabled status)")]
        public static CallToolResult EditorGetXTraceSamplers()
        {
            var json = XTraceSampler.GetSamplersInfoJson();
            var samplers = JArray.Parse(json);
            return new CallToolResult
            {
                Content = new List<ContentBlock> { new TextContentBlock { Text = samplers.ToString() } }
            };
        }

        [McpServerTool("EditorEnableXTraceSamplers", Description = "Batch enable XTrace samplers by their unique names")]
        public static CallToolResult EditorEnableXTraceSamplers(
            [McpArgument(Description = "Array of sampler unique names to enable", Required = true)] string[] names)
        {
            if (names == null || names.Length == 0)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "Parameter 'names' is required and cannot be empty" }
                    }
                };
            }

            int enabled = 0;
            int notFound = 0;
            var notFoundNames = new JArray();

            foreach (var name in names)
            {
                if (XTraceSampler.GetSampler(name) != null)
                {
                    XTraceSampler.EnableSampler(name);
                    enabled++;
                }
                else
                {
                    notFound++;
                    notFoundNames.Add(name);
                }
            }

            var result = new JObject
            {
                ["enabled"] = enabled,
                ["notFound"] = notFound,
                ["totalRequested"] = names.Length
            };

            if (notFoundNames.Count > 0)
            {
                result["notFoundNames"] = notFoundNames;
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock> { new TextContentBlock { Text = result.ToString() } }
            };
        }

        [McpServerTool("EditorDisableXTraceSamplers", Description = "Batch disable XTrace samplers by their unique names")]
        public static CallToolResult EditorDisableXTraceSamplers(
            [McpArgument(Description = "Array of sampler unique names to disable", Required = true)] string[] names)
        {
            if (names == null || names.Length == 0)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "Parameter 'names' is required and cannot be empty" }
                    }
                };
            }

            int disabled = 0;
            int notFound = 0;
            var notFoundNames = new JArray();

            foreach (var name in names)
            {
                if (XTraceSampler.GetSampler(name) != null)
                {
                    XTraceSampler.DisableSampler(name);
                    disabled++;
                }
                else
                {
                    notFound++;
                    notFoundNames.Add(name);
                }
            }

            var result = new JObject
            {
                ["disabled"] = disabled,
                ["notFound"] = notFound,
                ["totalRequested"] = names.Length
            };

            if (notFoundNames.Count > 0)
            {
                result["notFoundNames"] = notFoundNames;
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock> { new TextContentBlock { Text = result.ToString() } }
            };
        }
    }
}
#endif
