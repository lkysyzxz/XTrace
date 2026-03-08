using System;

namespace XTrace
{
    public class Sampler
    {
        public string UniqueName { get; }
        public string Description { get; }

        private readonly XTraceSession _session;

        protected internal Sampler(string uniqueName, string description, XTraceSession session)
        {
            if (string.IsNullOrEmpty(uniqueName))
                throw new ArgumentNullException(nameof(uniqueName));

            UniqueName = uniqueName;
            Description = description ?? "";
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        #region Sample Overloads

        public TracePoint Sample(int value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(bool value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(float value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(double value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(string value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(long value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(byte value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(short value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(decimal value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(char value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(DateTime value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(TimeSpan value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

#if UNITY_5_3_OR_NEWER
        public TracePoint Sample(UnityEngine.Vector2 value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(UnityEngine.Vector3 value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(UnityEngine.Vector4 value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(UnityEngine.Quaternion value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        public TracePoint Sample(UnityEngine.Color value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }
#endif

        public TracePoint Sample(object value, string prompt)
        {
            return _session.TryCapture(UniqueName, value, prompt);
        }

        #endregion
    }
}
