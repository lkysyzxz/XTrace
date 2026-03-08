using System;

namespace XTrace
{
    public static class XTraceSampler
    {
        #region Session Management

        public static void Initialize(XTraceSessionConfig config)
        {
            XTraceSession.Create(config);
        }

        public static int TraceCount => XTraceSession.Current.Data.TotalPoints;

        public static XTraceSession Session => XTraceSession.Current;

        public static void Clear()
        {
            XTraceSession.Current.Clear();
        }

        public static void ResetSession()
        {
            XTraceSession.Reset();
        }

        public static XTraceData GetCurrentData()
        {
            return XTraceSession.Current.Data;
        }

        #endregion

        #region Session Control

        public static bool IsSessionEnabled => XTraceSession.Current.IsEnabled;

        public static void EnableSession()
        {
            XTraceSession.Current.Enable();
        }

        public static void DisableSession()
        {
            XTraceSession.Current.Disable();
        }

        #endregion

        #region Sampler Management

        public static Sampler CreateSampler(string uniqueName, string description = null, bool enabled = true)
        {
            return XTraceSession.Current.CreateSampler(uniqueName, description, enabled);
        }

        public static Sampler GetOrCreateSampler(string uniqueName, string description = null, bool enabled = true)
        {
            var session = XTraceSession.Current;
            if (session == null || !session.IsEnabled)
                return null;
            
            return session.GetOrCreateSampler(uniqueName, description, enabled);
        }

        public static T GetOrCreateSampler<T>(string uniqueName = null, string description = null, bool enabled = true) where T : Sampler
        {
            var session = XTraceSession.Current;
            if (session == null || !session.IsEnabled)
                return null;
            
            return session.GetOrCreateSampler<T>(uniqueName, description, enabled);
        }

        public static Sampler GetSampler(string uniqueName)
        {
            return XTraceSession.Current.GetSampler(uniqueName);
        }

        public static bool IsSamplerEnabled(string uniqueName)
        {
            return XTraceSession.Current.IsSamplerEnabled(uniqueName);
        }

        public static void EnableSampler(string uniqueName)
        {
            XTraceSession.Current.EnableSampler(uniqueName);
        }

        public static void DisableSampler(string uniqueName)
        {
            XTraceSession.Current.DisableSampler(uniqueName);
        }

        public static bool UnregisterSampler(string uniqueName)
        {
            return XTraceSession.Current.UnregisterSampler(uniqueName);
        }

        public static string GetSamplersInfoJson()
        {
            return XTraceSession.Current.GetSamplersInfoJson();
        }

        #endregion

        #region Call Stack Control

        public static void SetCaptureCallStack(string uniqueName, bool enabled)
        {
            XTraceSession.Current.SetCaptureCallStack(uniqueName, enabled);
        }

        public static bool IsCaptureCallStackEnabled(string uniqueName)
        {
            return XTraceSession.Current.IsCaptureCallStackEnabled(uniqueName);
        }

        #endregion

        #region Export/Import

        public static XTraceData Export(string filePath, string description = null)
        {
            return XTraceSession.Current.Export(filePath, description);
        }

        public static void ExportSnapshot(string filePath, string description = null)
        {
            XTraceSession.Current.ExportSnapshot(filePath, description);
        }

        public static XTraceData Import(string filePath)
        {
            return XTraceImporter.Import(filePath);
        }

        #endregion

        #region JSON Export

        public static XTraceData ExportJson(string filePath, string description = null)
        {
            return XTraceSession.Current.ExportJson(filePath, description);
        }

        public static string ToJsonString()
        {
            return XTraceSession.Current.ToJsonString();
        }

        public static void ExportJsonSnapshot(string filePath, string description = null)
        {
            XTraceSession.Current.ExportJsonSnapshot(filePath, description);
        }

        public static void ConvertToJson(string xtraceFilePath, string jsonFilePath)
        {
            XTraceSession.ConvertToJson(xtraceFilePath, jsonFilePath);
        }

        public static string ConvertToJsonString(string xtraceFilePath)
        {
            return XTraceSession.ConvertToJsonString(xtraceFilePath);
        }

        #endregion
    }
}
