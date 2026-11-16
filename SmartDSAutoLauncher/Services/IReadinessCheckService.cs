using System.Threading;
using System.Threading.Tasks;
using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.Services
{
    public interface IReadinessCheckService
    {
        Task<bool> CheckTcpAsync(string host, int port, int timeoutSeconds, CancellationToken ct = default);
        Task<bool> CheckHttpAsync(string url, int timeoutSeconds, CancellationToken ct = default);
        Task<bool> WaitForReadinessAsync(ReadinessCheckConfig config, CancellationToken ct = default);
    }
}
