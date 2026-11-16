using System;
using System.Text.Json.Serialization;

namespace SmartDSAutoLauncher.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Success
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public LogLevel Level { get; set; } = LogLevel.Info;
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public string Formatted => $"[{Timestamp:HH:mm:ss}] [{Level}] [{Source}] {Message}";
    }
}
