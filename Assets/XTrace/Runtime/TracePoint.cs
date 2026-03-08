using System;
using System.Collections.Generic;

namespace XTrace
{
    /// <summary>
    /// Represents a single trace point captured by the sampling system.
    /// </summary>
    [Serializable]
    public class TracePoint
    {
    public int Id { get; set; }

    public long Timestamp { get; set; }

    public string Value { get; set; }

    public string ValueType { get; set; }

    public string Prompt { get; set; }

    public string SamplerUniqueName { get; set; }

    public List<StackFrame> CallStack { get; set; } = new List<StackFrame>();

        public TracePoint() { }

        public TracePoint(int id, long timestamp, string value, string valueType, string prompt, string samplerUniqueName = null)
        {
            Id = id;
            Timestamp = timestamp;
            Value = value;
            ValueType = valueType;
            Prompt = prompt;
            SamplerUniqueName = samplerUniqueName ?? "";
        }

        public override string ToString()
        {
            return $"[#{Id}] {ValueType}: {Value} | {Prompt}";
        }
    }

    /// <summary>
    /// Represents a single frame in the call stack.
    /// </summary>
    [Serializable]
    public class StackFrame
    {
    public string MethodName { get; set; }

    public string DeclaringType { get; set; }

    public string FileName { get; set; }

    public int LineNumber { get; set; }

        public StackFrame() { }

        public StackFrame(string methodName, string declaringType, string fileName, int lineNumber)
        {
            MethodName = methodName;
            DeclaringType = declaringType;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        public override string ToString()
        {
            var location = string.IsNullOrEmpty(FileName) ? "" : $" at {FileName}:{LineNumber}";
            return $"{DeclaringType}.{MethodName}(){location}";
        }
    }
}
