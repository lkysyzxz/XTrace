#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace XTrace
{
    public static class XTraceMenuIntegrationTest
    {
        [MenuItem("Tools/XTrace/Test Integration")]
        public static void RunIntegrationTest()
        {
            Debug.Log("=== XTrace Menu Integration Test ===");
            
            string configPath = Path.Combine(Application.dataPath, "XTrace/Resources/XTraceSessionConfig.json");
            
            if (!File.Exists(configPath))
            {
                Debug.LogError($"[FAIL] Config file not found at: {configPath}");
                return;
            }
            Debug.Log($"[PASS] Config file exists at: {configPath}");
            
            try
            {
                string json = File.ReadAllText(configPath);
                var config = XTraceSessionConfig.FromJson(json);
                Debug.Log($"[PASS] Config loaded: {config.Samplers.Count} samplers, EnabledByDefault={config.EnabledByDefault}");
                
                foreach (var sampler in config.Samplers)
                {
                    Debug.Log($"  - {sampler.UniqueName}: {sampler.Description} (enabled={sampler.EnabledByDefault})");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FAIL] Failed to load config: {e.Message}");
                return;
            }
            
            try
            {
                var config = XTraceSessionConfig.LoadFromFile(configPath);
                XTraceSession.Create(config);
                
                if (XTraceSession.Current == null)
                {
                    Debug.LogError("[FAIL] XTraceSession.Instance is null after Create()");
                    return;
                }
                Debug.Log("[PASS] XTraceSession.Instance is not null");
                
                if (XTraceSession.Current == null)
                {
                    Debug.LogError("[FAIL] XTraceSession.Current is null");
                    return;
                }
                Debug.Log("[PASS] XTraceSession.Current returns Instance");
                
                if (XTraceSampler.Session == null)
                {
                    Debug.LogError("[FAIL] XTraceSampler.Instance is null");
                    return;
                }
                Debug.Log("[PASS] XTraceSampler.Instance works");
                
                XTraceSampler.EnableSession();
                if (!XTraceSampler.IsSessionEnabled)
                {
                    Debug.LogError("[FAIL] Session not enabled after EnableSession()");
                    return;
                }
                Debug.Log("[PASS] Session enabled successfully");
                
                XTraceSampler.DisableSession();
                if (XTraceSampler.IsSessionEnabled)
                {
                    Debug.LogError("[FAIL] Session still enabled after DisableSession()");
                    return;
                }
                Debug.Log("[PASS] Session disabled successfully");
                
                Debug.Log("=== ALL INTEGRATION TESTS PASSED ===");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FAIL] Integration test error: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
#endif
