using System;
using UnityEngine;

namespace ModelContextProtocol
{
    public enum LogLevel
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }

    public static class UnityLogger
    {
        public static LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        public static void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level < MinimumLevel) return;

            string formattedMessage = $"[MCP] {message}";
            
            switch (level)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Information:
                    if (exception != null)
                        Debug.Log($"{formattedMessage}\n{exception}");
                    else
                        Debug.Log(formattedMessage);
                    break;
                    
                case LogLevel.Warning:
                    if (exception != null)
                        Debug.LogWarning($"{formattedMessage}\n{exception}");
                    else
                        Debug.LogWarning(formattedMessage);
                    break;
                    
                case LogLevel.Error:
                case LogLevel.Critical:
                    if (exception != null)
                        Debug.LogError($"{formattedMessage}\n{exception}");
                    else
                        Debug.LogError(formattedMessage);
                    break;
            }
        }

        public static void LogTrace(string message) => Log(LogLevel.Trace, message);
        public static void LogDebug(string message) => Log(LogLevel.Debug, message);
        public static void LogInformation(string message) => Log(LogLevel.Information, message);
        public static void LogWarning(string message) => Log(LogLevel.Warning, message);
        public static void LogError(string message, Exception exception = null) => Log(LogLevel.Error, message, exception);
        public static void LogCritical(string message, Exception exception = null) => Log(LogLevel.Critical, message, exception);

        public static bool IsEnabled(LogLevel level) => level >= MinimumLevel;
    }

    public interface ILogger
    {
        void Log(LogLevel level, string message, Exception exception = null);
        bool IsEnabled(LogLevel level);
    }

    public class UnityLoggerImpl : ILogger
    {
        public void Log(LogLevel level, string message, Exception exception = null)
        {
            UnityLogger.Log(level, message, exception);
        }

        public bool IsEnabled(LogLevel level)
        {
            return UnityLogger.IsEnabled(level);
        }
    }
}
