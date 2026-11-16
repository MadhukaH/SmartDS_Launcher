using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.Services
{
    public interface IProcessManager
    {
        Task<Process?> StartServiceAsync(ServiceConfig config, CancellationToken ct = default);
        Task StopServiceAsync(ServiceInstance instance, CancellationToken ct = default);
        bool IsProcessRunning(int processId);
        bool IsOwnedProcess(int processId);
        void RegisterOwnedProcess(int processId);
        void UnregisterOwnedProcess(int processId);
    }
}
