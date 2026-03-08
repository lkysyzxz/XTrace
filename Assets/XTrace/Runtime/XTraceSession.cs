using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace XTrace
{
    public class XTraceSession
    {
        private static XTraceSession _instance;
        private static readonly object _lock = new object();

        private readonly XTraceData _data;
        private readonly XTraceSessionConfig _config;
        private readonly Dictionary<string, Sampler> _samplers;
        private readonly HashSet<string> _enabledSamplers;
        private readonly Dictionary<string, bool> _samplerCallStackEnabled;
        private int _nextId;
        private readonly long _startTicks;
        private bool _isComplete;
        private bool _isEnabled;

        public const string FileExtension = ".xtrace";

        private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes("XTRC");
        

        public static XTraceSession Current
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new XTraceSession(new XTraceSessionConfig());
                    }
                    return _instance;
                }
            }
        }

        public static XTraceSession Create(XTraceSessionConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new XTraceSession(config);
                    }
                }
            }

            return _instance;
        }

        public XTraceData Data => _data;
        public bool IsEnabled => _isEnabled;

        private XTraceSession(XTraceSessionConfig config)
        {
            _config = config;
            _data = new XTraceData(Guid.NewGuid().ToString("N"), "XTrace");
            _samplers = new Dictionary<string, Sampler>();
            _enabledSamplers = new HashSet<string>();
            _samplerCallStackEnabled = new Dictionary<string, bool>();
            _startTicks = Stopwatch.GetTimestamp();
            _nextId = 1;
            _isComplete = false;
            _isEnabled = true;

            InitializeSamplersFromConfig();
        }

        private void InitializeSamplersFromConfig()
        {
            foreach (var definition in _config.Samplers)
            {
                var sampler = new Sampler(definition.UniqueName, definition.Description, this);
                _samplers[definition.UniqueName] = sampler;

                if (definition.EnabledByDefault)
                {
                    _enabledSamplers.Add(definition.UniqueName);
                }

                _samplerCallStackEnabled[definition.UniqueName] = definition.CaptureCallStack;
            }
        }

        #region Sampler Management

        public Sampler CreateSampler(string uniqueName, string description = null, bool enabled = true)
        {
            if (string.IsNullOrEmpty(uniqueName))
                throw new ArgumentNullException(nameof(uniqueName));

            lock (_lock)
            {
                if (_samplers.ContainsKey(uniqueName))
                    throw new InvalidOperationException($"Sampler '{uniqueName}' already exists");

                var sampler = new Sampler(uniqueName, description ?? "", this);
                _samplers[uniqueName] = sampler;

                if (enabled)
                {
                    _enabledSamplers.Add(uniqueName);
                }

                _samplerCallStackEnabled[uniqueName] = false;

                return sampler;
            }
        }

        public Sampler GetOrCreateSampler(string uniqueName, string description = null, bool enabled = true)
        {
            if (!_isEnabled)
                return null;

            if (string.IsNullOrEmpty(uniqueName))
                throw new ArgumentNullException(nameof(uniqueName));

            lock (_lock)
            {
                if (_samplers.TryGetValue(uniqueName, out var existing))
                    return existing;

                return CreateSampler(uniqueName, description, enabled);
            }
        }

        public T GetOrCreateSampler<T>(string uniqueName = null, string description = null, bool enabled = true) where T : Sampler
        {
            if (!_isEnabled)
                return null;

            var key = uniqueName ?? typeof(T).FullName;

            lock (_lock)
            {
                if (_samplers.TryGetValue(key, out var existing))
                    return (T)existing;

                var sampler = (T)Activator.CreateInstance(typeof(T), key, description ?? "", this);
                _samplers[key] = sampler;

                if (enabled)
                {
                    _enabledSamplers.Add(key);
                }

                _samplerCallStackEnabled[key] = false;

                return sampler;
            }
        }

        public Sampler GetSampler(string uniqueName)
        {
            if (string.IsNullOrEmpty(uniqueName))
                return null;

            lock (_lock)
            {
                return _samplers.TryGetValue(uniqueName, out var sampler) ? sampler : null;
            }
        }

        public bool IsSamplerEnabled(string uniqueName)
        {
            if (string.IsNullOrEmpty(uniqueName))
                return false;

            lock (_lock)
            {
                return _enabledSamplers.Contains(uniqueName);
            }
        }

        public void EnableSampler(string uniqueName)
        {
            if (string.IsNullOrEmpty(uniqueName))
                return;

            lock (_lock)
            {
                if (_samplers.ContainsKey(uniqueName))
                {
                    _enabledSamplers.Add(uniqueName);
                }
            }
        }

        public void DisableSampler(string uniqueName)
        {
            if (string.IsNullOrEmpty(uniqueName))
                return;

            lock (_lock)
            {
                _enabledSamplers.Remove(uniqueName);
            }
        }

        public bool UnregisterSampler(string uniqueName)
        {
            if (string.IsNullOrEmpty(uniqueName))
                return false;

            lock (_lock)
            {
                _enabledSamplers.Remove(uniqueName);
                _samplerCallStackEnabled.Remove(uniqueName);
                return _samplers.Remove(uniqueName);
            }
        }

        #region Call Stack Control

        public void SetCaptureCallStack(string uniqueName, bool enabled)
        {
            if (string.IsNullOrEmpty(uniqueName))
                return;

            lock (_lock)
            {
                if (_samplers.ContainsKey(uniqueName))
                {
                    _samplerCallStackEnabled[uniqueName] = enabled;
                }
            }
        }

        public bool IsCaptureCallStackEnabled(string uniqueName)
        {
            if (string.IsNullOrEmpty(uniqueName))
                return false;

            lock (_lock)
            {
                return _samplerCallStackEnabled.TryGetValue(uniqueName, out var enabled) && enabled;
            }
        }

        #endregion

        public IReadOnlyDictionary<string, Sampler> GetAllSamplers()
        {
            lock (_lock)
            {
                return new Dictionary<string, Sampler>(_samplers);
            }
        }

        public string GetSamplersInfoJson()
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                sb.Append('[');

                bool first = true;
                foreach (var kvp in _samplers)
                {
                    if (!first) sb.Append(',');
                    first = false;

                    sb.Append('{');
                    sb.Append("\"uniqueName\":\"").Append(EscapeString(kvp.Key)).Append("\",");
                    sb.Append("\"description\":\"").Append(EscapeString(kvp.Value.Description)).Append("\",");
                    sb.Append("\"enabled\":").Append(_enabledSamplers.Contains(kvp.Key) ? "true" : "false");
                    sb.Append('}');
                }

                sb.Append(']');
                return sb.ToString();
            }
        }

        #endregion

        #region Session Control

        public void Enable()
        {
            lock (_lock)
            {
                _isEnabled = true;
            }
        }

        public void Disable()
        {
            lock (_lock)
            {
                _isEnabled = false;
            }
        }

        #endregion

        #region Capture

        internal TracePoint TryCapture(string samplerUniqueName, object value, string prompt, int skipFrames = 4)
        {
            if (!CanCapture(samplerUniqueName))
                return null;

            bool captureCallStack = ShouldCaptureCallStack(samplerUniqueName);
            return Capture(samplerUniqueName, value, prompt, skipFrames, captureCallStack);
        }

        private bool ShouldCaptureCallStack(string samplerUniqueName)
        {
            lock (_lock)
            {
                return _samplerCallStackEnabled.TryGetValue(samplerUniqueName, out var enabled) && enabled;
            }
        }

        private bool CanCapture(string samplerUniqueName)
        {
            if (!_isEnabled)
                return false;

            lock (_lock)
            {
                return _enabledSamplers.Contains(samplerUniqueName);
            }
        }

        public TracePoint Capture(string samplerUniqueName, object value, string prompt, int skipFrames = 3, bool captureCallStack = false)
        {
            var timestamp = (long)((Stopwatch.GetTimestamp() - _startTicks) * 10000000.0 / Stopwatch.Frequency);
            
            var point = new TracePoint(
                _nextId++,
                timestamp,
                value?.ToString() ?? "null",
                value?.GetType().Name ?? "null",
                prompt ?? "",
                samplerUniqueName ?? ""
            );

            if (captureCallStack)
            {
                CaptureCallStack(point, skipFrames);
            }

            _data.AddTracePoint(point);
            return point;
        }

        #endregion

        private void CaptureCallStack(TracePoint point, int skipFrames)
        {
            try
            {
                var stack = new StackTrace(skipFrames, true);
                var frameCount = Math.Min(stack.FrameCount, 32);

                for (int i = 0; i < frameCount; i++)
                {
                    var frame = stack.GetFrame(i);
                    if (frame == null) continue;

                    var method = frame.GetMethod();
                    if (method == null) continue;

                    if (method.DeclaringType?.Namespace?.StartsWith("XTrace") == true)
                        continue;

                    var stackFrame = new StackFrame(
                        method.Name,
                        method.DeclaringType?.Name ?? "Unknown",
                        frame.GetFileName() ?? "",
                        frame.GetFileLineNumber()
                    );

                    point.CallStack.Add(stackFrame);
                }
            }
            catch
            {
            }
        }

        public XTraceData Complete()
        {
            lock (_lock)
            {
                _isComplete = true;
                _data.Complete();
                return _data;
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _instance = null;
            }
        }

        public void Clear()
        {
            _data.TracePoints.Clear();
            _data.TotalPoints = 0;
            _nextId = 1;
        }

        #region Export

        public XTraceData Export(string filePath, string description = null)
        {
            var data = Complete();
            ApplyDescription(data, description);
            WriteXTraceFile(data, filePath);
            return data;
        }

        public void ExportSnapshot(string filePath, string description = null)
        {
            ApplyDescription(_data, description);
            WriteXTraceFile(_data, filePath);
        }

        public XTraceData ExportJson(string filePath, string description = null)
        {
            var data = Complete();
            ApplyDescription(data, description);
            WriteJsonFile(data, filePath);
            return data;
        }

        public void ExportJsonSnapshot(string filePath, string description = null)
        {
            ApplyDescription(_data, description);
            WriteJsonFile(_data, filePath);
        }

        public string ToJsonString()
        {
            return SerializeToJson(_data);
        }

        public static void ExportData(XTraceData data, string filePath)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            WriteXTraceFile(data, filePath);
        }

        public static void ExportDataJson(XTraceData data, string filePath)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            WriteJsonFile(data, filePath);
        }

        public static string DataToJsonString(XTraceData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return SerializeToJson(data);
        }

        public static void ConvertToJson(string xtraceFilePath, string jsonFilePath)
        {
            var data = XTraceImporter.Import(xtraceFilePath);
            WriteJsonFile(data, jsonFilePath);
        }

        public static string ConvertToJsonString(string xtraceFilePath)
        {
            var data = XTraceImporter.Import(xtraceFilePath);
            return SerializeToJson(data);
        }

        private static void ApplyDescription(XTraceData data, string description)
        {
            if (!string.IsNullOrEmpty(description))
            {
                data.Description = description;
            }
        }

        #endregion

        #region File Writing

        private static void WriteXTraceFile(XTraceData data, string filePath)
        {
            if (!filePath.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                filePath += FileExtension;
            }

            EnsureDirectory(filePath);

            var json = SerializeToJson(data);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Write(MagicBytes, 0, MagicBytes.Length);

                var versionBytes = BitConverter.GetBytes(XTraceData.FormatVersion);
                fileStream.Write(versionBytes, 0, versionBytes.Length);

                var lengthBytes = BitConverter.GetBytes(jsonBytes.Length);
                fileStream.Write(lengthBytes, 0, lengthBytes.Length);

                using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal, leaveOpen: true))
                {
                    gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
                }
            }
        }

        private static void WriteJsonFile(XTraceData data, string filePath)
        {
            if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                filePath += ".json";
            }

            EnsureDirectory(filePath);
            var json = SerializeToJson(data);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        private static void EnsureDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        #endregion

        #region JSON Serialization

        private static string SerializeToJson(XTraceData data)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            
            sb.Append("\"formatVersion\":").Append(XTraceData.FormatVersion).Append(',');
            sb.Append("\"sessionId\":\"").Append(EscapeString(data.SessionId)).Append("\",");
            sb.Append("\"startTime\":\"").Append(EscapeString(data.StartTime)).Append("\",");
            sb.Append("\"endTime\":\"").Append(EscapeString(data.EndTime ?? "")).Append("\",");
            sb.Append("\"applicationName\":\"").Append(EscapeString(data.ApplicationName ?? "")).Append("\",");
            sb.Append("\"description\":\"").Append(EscapeString(data.Description ?? "")).Append("\",");
            sb.Append("\"totalPoints\":").Append(data.TotalPoints).Append(',');

            sb.Append("\"metadata\":{");
            if (data.Metadata != null && data.Metadata.Count > 0)
            {
                bool first = true;
                foreach (var kvp in data.Metadata)
                {
                    if (!first) sb.Append(',');
                    sb.Append("\"").Append(EscapeString(kvp.Key)).Append("\":\"").Append(EscapeString(kvp.Value)).Append("\"");
                    first = false;
                }
            }
            sb.Append("},");

            sb.Append("\"tracePoints\":[");
            if (data.TracePoints != null)
            {
                for (int i = 0; i < data.TracePoints.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    SerializeTracePoint(sb, data.TracePoints[i]);
                }
            }
            sb.Append(']');

            sb.Append('}');
            return sb.ToString();
        }

        private static void SerializeTracePoint(StringBuilder sb, TracePoint point)
        {
            sb.Append('{');
            sb.Append("\"id\":").Append(point.Id).Append(',');
            sb.Append("\"timestamp\":").Append(point.Timestamp).Append(',');
            sb.Append("\"samplerUniqueName\":\"").Append(EscapeString(point.SamplerUniqueName ?? "")).Append("\",");
            sb.Append("\"value\":\"").Append(EscapeString(point.Value ?? "")).Append("\",");
            sb.Append("\"valueType\":\"").Append(EscapeString(point.ValueType ?? "")).Append("\",");
            sb.Append("\"prompt\":\"").Append(EscapeString(point.Prompt ?? "")).Append("\",");

            sb.Append("\"callStack\":[");
            if (point.CallStack != null)
            {
                for (int i = 0; i < point.CallStack.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    SerializeStackFrame(sb, point.CallStack[i]);
                }
            }
            sb.Append(']');

            sb.Append('}');
        }

        private static void SerializeStackFrame(StringBuilder sb, StackFrame frame)
        {
            sb.Append('{');
            sb.Append("\"methodName\":\"").Append(EscapeString(frame.MethodName ?? "")).Append("\",");
            sb.Append("\"declaringType\":\"").Append(EscapeString(frame.DeclaringType ?? "")).Append("\",");
            sb.Append("\"fileName\":\"").Append(EscapeString(frame.FileName ?? "")).Append("\",");
            sb.Append("\"lineNumber\":").Append(frame.LineNumber);
            sb.Append('}');
        }

        private static string EscapeString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        #endregion
    }
}
