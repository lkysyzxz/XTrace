using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace XTrace
{
    [Serializable]
    public class SamplerDefinition
    {
        public string UniqueName { get; set; }
        public string Description { get; set; }
        public bool EnabledByDefault { get; set; }
        public bool CaptureCallStack { get; set; }

        public SamplerDefinition() { }

        public SamplerDefinition(string uniqueName, string description = null, bool enabledByDefault = false, bool captureCallStack = false)
        {
            if (string.IsNullOrEmpty(uniqueName))
                throw new ArgumentNullException(nameof(uniqueName));

            UniqueName = uniqueName;
            Description = description ?? "";
            EnabledByDefault = enabledByDefault;
            CaptureCallStack = captureCallStack;
        }
    }

    [Serializable]
    public class XTraceSessionConfig
    {
        public List<SamplerDefinition> Samplers { get; set; } = new List<SamplerDefinition>();
        public bool EnabledByDefault { get; set; } = true;

        public XTraceSessionConfig() { }

        public XTraceSessionConfig AddSampler(string uniqueName, string description = null, bool enabledByDefault = true)
        {
            Samplers.Add(new SamplerDefinition(uniqueName, description, enabledByDefault));
            return this;
        }

        #region JSON Serialization

        public static XTraceSessionConfig FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));
            
            return JsonConvert.DeserializeObject<XTraceSessionConfig>(json);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static XTraceSessionConfig LoadFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Config file not found: {filePath}", filePath);
            
            string json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public void SaveToFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            
            string json = ToJson();
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(filePath, json);
        }

        #endregion
    }
}
