#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace XTrace
{
    public static class XTraceEditorMenu
    {
        private const string ConfigFileName = "XTraceSessionConfig.json";
        private const string ConfigFolder = "Resources";
        
        private static string ConfigPath
        {
            get
            {
                return Path.Combine(Application.dataPath, ConfigFolder, ConfigFileName);
            }
        }

        [MenuItem("Tools/XTrace/Enable Session", priority = 10)]
        public static void EnableSession()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    EditorUtility.DisplayDialog(
                        "Config File Not Found",
                        $"XTrace session config not found at:\n{ConfigPath}\n\nPlease create a config file first.",
                        "OK"
                    );
                    return;
                }

                string json = File.ReadAllText(ConfigPath);
                var config = XTraceSessionConfig.FromJson(json);
                
                XTraceSampler.Initialize(config);
                
                XTraceSampler.EnableSession();
                
                Debug.Log($"[XTrace] Session enabled with {config.Samplers.Count} samplers");
                EditorUtility.DisplayDialog(
                    "XTrace Session Enabled",
                    $"Session is now active with {config.Samplers.Count} configured samplers.\n\nEnabled samplers: {GetEnabledSamplerCount(config)}",
                    "OK"
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[XTrace] Failed to enable session: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to enable XTrace session:\n{e.Message}",
                    "OK"
                );
            }
        }

        [MenuItem("Tools/XTrace/Disable Session", priority = 11)]
        public static void DisableSession()
        {
            try
            {
                int traceCount = XTraceSampler.TraceCount;
                
                XTraceSampler.DisableSession();
                Debug.Log("[XTrace] Session disabled");
                
                if (traceCount > 0)
                {
                    bool shouldSave = EditorUtility.DisplayDialog(
                        "Save Trace Data",
                        $"Session is now paused.\n\nTrace points collected: {traceCount}\n\nDo you want to save the trace data to a file?",
                        "Yes, Save Data",
                        "No, Don't Save"
                    );
                    
                    if (shouldSave)
                    {
                        string logsFolder = Path.Combine(Application.dataPath, "..", "Logs");
                        if (!Directory.Exists(logsFolder))
                        {
                            Directory.CreateDirectory(logsFolder);
                        }
                        
                        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string fileName = $"xtrace_{timestamp}.xtrace";
                        string filePath = Path.Combine(logsFolder, fileName);
                        
                        XTraceSampler.Export(filePath, "Session disabled export");
                        
                        Debug.Log($"[XTrace] Trace data exported to: {filePath}");
                        
                        EditorUtility.DisplayDialog(
                            "Export Successful",
                            $"Trace data saved to:\nLogs/{fileName}\n\nTrace points: {traceCount}",
                            "OK"
                        );
                    }
                    
                    bool clearData = EditorUtility.DisplayDialog(
                        "Clear Trace Data",
                        "Do you want to clear current trace data from memory?",
                        "Yes, Clear Data",
                        "No, Keep Data"
                    );
                    
                    if (clearData)
                    {
                        XTraceSampler.Clear();
                        Debug.Log("[XTrace] Trace data cleared");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "XTrace Session Disabled",
                        "Session is now paused.\n\nNo trace data was collected during this session.",
                        "OK"
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[XTrace] Failed to disable session: {e.Message}");
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to disable XTrace session:\n{e.Message}",
                    "OK"
                );
            }
        }

        private static int GetEnabledSamplerCount(XTraceSessionConfig config)
        {
            int count = 0;
            foreach (var sampler in config.Samplers)
            {
                if (sampler.EnabledByDefault)
                    count++;
            }
            return count;
        }
    }
}
#endif
