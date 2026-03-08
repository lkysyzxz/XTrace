using System;
using System.Collections.Generic;

namespace XTrace
{
    /// <summary>
    /// Container for all trace points collected during a session.
    /// </summary>
    [Serializable]
    public class XTraceData
    {
    public const int FormatVersion = 1;

    public string SessionId { get; set; }

    public string StartTime { get; set; }

    public string EndTime { get; set; }

    public string ApplicationName { get; set; }

    public string Description { get; set; }

    public int TotalPoints { get; set; }

    public List<TracePoint> TracePoints { get; set; } = new List<TracePoint>();

    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        public XTraceData() { }

        public XTraceData(string sessionId, string applicationName = null)
        {
            SessionId = sessionId;
            StartTime = DateTime.UtcNow.ToString("o");
            ApplicationName = applicationName ?? "Unknown";
        }

    public void AddTracePoint(TracePoint point)
        {
            TracePoints.Add(point);
            TotalPoints = TracePoints.Count;
        }

    public void Complete()
        {
            EndTime = DateTime.UtcNow.ToString("o");
        }
    }
}
