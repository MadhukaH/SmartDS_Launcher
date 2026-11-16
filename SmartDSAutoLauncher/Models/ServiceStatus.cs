using System;
using System.Text.Json.Serialization;

namespace SmartDSAutoLauncher.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ServiceStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Failed,
        Disabled
    }

    public class ServiceInstance
    {
        public ServiceConfig Config { get; set; } = new();
        public ServiceStatus Status { get; set; } = ServiceStatus.Stopped;
        public int? ProcessId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? StopTime { get; set; }
        public string? LastError { get; set; }
        public int RetryCount { get; set; } = 0;
        public string OutputLog { get; set; } = string.Empty;
        public bool IsOwned { get; set; } = false;
        public int ExitCode { get; set; } = 0;
        public TimeSpan Uptime => StartTime.HasValue && Status == ServiceStatus.Running
            ? DateTime.Now - StartTime.Value
            : TimeSpan.Zero;
    }
}
