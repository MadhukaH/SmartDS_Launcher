using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.Services
{
    public interface ILaunchOrchestrator
    {
        IReadOnlyList<ServiceInstance> ServiceInstances { get; }
        bool IsRunning { get; }
        event EventHandler<ServiceInstance>? ServiceStatusChanged;
        event EventHandler<LaunchStep>? StepChanged;
        event EventHandler<LogEntry>? LogMessage;
        /// <summary>Raised after Initialize() rebuilds the service list (e.g. after saving the sequence).</summary>
        event EventHandler? InstancesReloaded;
        Task StartAllAsync(CancellationToken ct = default);
        Task StopAllAsync(CancellationToken ct = default);
        Task RestartAllAsync(CancellationToken ct = default);
        Task StartServiceAsync(Guid serviceId, CancellationToken ct = default);
        Task StopServiceAsync(Guid serviceId, CancellationToken ct = default);
        Task RestartServiceAsync(Guid serviceId, CancellationToken ct = default);
        void Initialize(IEnumerable<ServiceConfig> configs);
    }
}
