using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.Services
{
    public class ReadinessCheckService : IReadinessCheckService
    {
        private readonly ILogger<ReadinessCheckService> _logger;
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

        public ReadinessCheckService(ILogger<ReadinessCheckService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> CheckTcpAsync(string host, int port, int timeoutSeconds, CancellationToken ct = default)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                try
                {
                    using var client = new TcpClient();
                    var connectTask = client.ConnectAsync(host, port, ct).AsTask();
                    if (await Task.WhenAny(connectTask, Task.Delay(2000, ct)) == connectTask && client.Connected)
                    {
                        _logger.LogInformation("TCP readiness check passed: {Host}:{Port}", host, port);
                        return true;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch { }
                await Task.Delay(1000, ct);
            }
            _logger.LogWarning("TCP readiness check timed out: {Host}:{Port}", host, port);
            return false;
        }

        public async Task<bool> CheckHttpAsync(string url, int timeoutSeconds, CancellationToken ct = default)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url, ct);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("HTTP readiness check passed: {Url}", url);
                        return true;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch { }
                await Task.Delay(1000, ct);
            }
            _logger.LogWarning("HTTP readiness check timed out: {Url}", url);
            return false;
        }

        public async Task<bool> WaitForReadinessAsync(ReadinessCheckConfig config, CancellationToken ct = default)
        {
            if (config.Type == ReadinessCheckType.None) return true;
            if (config.Type == ReadinessCheckType.TcpPort)
                return await CheckTcpAsync(config.Host, config.Port, config.TimeoutSeconds, ct);
            if (config.Type == ReadinessCheckType.HttpEndpoint)
                return await CheckHttpAsync(config.Url, config.TimeoutSeconds, ct);
            return true;
        }
    }
}
