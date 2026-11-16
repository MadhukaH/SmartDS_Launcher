using System.Text.Json.Serialization;

namespace SmartDSAutoLauncher.Models
{
    public class ReadinessCheckConfig
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ReadinessCheckType Type { get; set; } = ReadinessCheckType.None;
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 3000;
        public string Url { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryIntervalMs { get; set; } = 1000;

        public ReadinessCheckConfig Clone() => new()
        {
            Type = this.Type,
            Host = this.Host,
            Port = this.Port,
            Url = this.Url,
            TimeoutSeconds = this.TimeoutSeconds,
            RetryIntervalMs = this.RetryIntervalMs
        };
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReadinessCheckType
    {
        None,
        TcpPort,
        HttpEndpoint
    }
}
