using System;
using System.Text.Json.Serialization;

namespace SmartDSAutoLauncher.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LaunchStepStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Skipped
    }

    public class LaunchStep
    {
        public int StepNumber { get; set; }
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public LaunchStepStatus Status { get; set; } = LaunchStepStatus.Pending;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int CountdownSecondsRemaining { get; set; } = 0;
        public string Message { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
