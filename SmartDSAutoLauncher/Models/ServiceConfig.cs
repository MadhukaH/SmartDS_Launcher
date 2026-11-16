using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartDSAutoLauncher.Models
{
    public class ServiceConfig
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string ExecutablePath { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
        public int DelayAfterStartSeconds { get; set; } = 0;
        public bool UseShellExecute { get; set; } = false;
        public bool WaitForPrevious { get; set; } = true;
        public ReadinessCheckConfig? ReadinessCheck { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FailurePolicy FailurePolicy { get; set; } = FailurePolicy.Continue;
        public int MaxRetries { get; set; } = 3;
        public string LogFilePath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ServiceConfig Clone()
        {
            return new ServiceConfig
            {
                Id = this.Id,
                Name = this.Name,
                IsEnabled = this.IsEnabled,
                ExecutablePath = this.ExecutablePath,
                Arguments = this.Arguments,
                WorkingDirectory = this.WorkingDirectory,
                EnvironmentVariables = new Dictionary<string, string>(this.EnvironmentVariables),
                DelayAfterStartSeconds = this.DelayAfterStartSeconds,
                UseShellExecute = this.UseShellExecute,
                WaitForPrevious = this.WaitForPrevious,
                ReadinessCheck = this.ReadinessCheck?.Clone(),
                FailurePolicy = this.FailurePolicy,
                MaxRetries = this.MaxRetries,
                LogFilePath = this.LogFilePath,
                Description = this.Description
            };
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FailurePolicy
    {
        Continue,
        Stop,
        Retry
    }
}
