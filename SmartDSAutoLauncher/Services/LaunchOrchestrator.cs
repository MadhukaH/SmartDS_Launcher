using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartDSAutoLauncher.Models;
using LogLevel = SmartDSAutoLauncher.Models.LogLevel;

namespace SmartDSAutoLauncher.Services
{
    public class LaunchOrchestrator : ILaunchOrchestrator
    {
        private readonly IProcessManager _processManager;
        private readonly IReadinessCheckService _readinessCheck;
        private readonly ILogger<LaunchOrchestrator> _logger;
        private readonly LogService _logService;
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private readonly Dictionary<Guid, Process?> _ownedProcesses = new();
        private readonly List<ServiceInstance> _instances = new();

        public IReadOnlyList<ServiceInstance> ServiceInstances => _instances.AsReadOnly();
        public bool IsRunning => _instances.Any(i => i.Status == ServiceStatus.Running || i.Status == ServiceStatus.Starting);

        public event EventHandler<ServiceInstance>? ServiceStatusChanged;
        public event EventHandler<LaunchStep>? StepChanged;
        public event EventHandler<LogEntry>? LogMessage;

        public LaunchOrchestrator(
            IProcessManager processManager,
            IReadinessCheckService readinessCheck,
            ILogger<LaunchOrchestrator> logger,
            LogService logService)
        {
            _processManager = processManager;
            _readinessCheck = readinessCheck;
            _logger = logger;
            _logService = logService;
        }

        public event EventHandler? InstancesReloaded;

        public void Initialize(IEnumerable<ServiceConfig> configs)
        {
            // Keep live instances (matched by Id) so running processes are never orphaned
            var previous = _instances.ToList();
            _instances.Clear();
            foreach (var config in configs)
            {
                var existing = previous.FirstOrDefault(i => i.Config.Id == config.Id);
                if (existing != null &&
                    (existing.Status == ServiceStatus.Running || existing.Status == ServiceStatus.Starting))
                {
                    // Running: keep process info, but remember the new config for the next start
                    existing.Config = config;
                    _instances.Add(existing);
                    continue;
                }

                _instances.Add(new ServiceInstance
                {
                    Config = config,
                    Status = config.IsEnabled ? ServiceStatus.Stopped : ServiceStatus.Disabled
                });
            }
            InstancesReloaded?.Invoke(this, EventArgs.Empty);
        }

        public async Task StartAllAsync(CancellationToken ct = default)
        {
            if (!await _operationLock.WaitAsync(100, ct))
            {
                Log("Startup already in progress, skipping", LogLevel.Warn, "Orchestrator");
                return;
            }
            try
            {
                Log("Starting all services...", LogLevel.Info, "Orchestrator");
                var enabledInstances = _instances.Where(i => i.Config.IsEnabled).ToList();

                for (int i = 0; i < enabledInstances.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var instance = enabledInstances[i];
                    var step = new LaunchStep
                    {
                        StepNumber = i + 1,
                        ServiceId = instance.Config.Id,
                        ServiceName = instance.Config.Name,
                        Status = LaunchStepStatus.Running,
                        StartedAt = DateTime.Now,
                        Message = $"Starting {instance.Config.Name}..."
                    };
                    StepChanged?.Invoke(this, step);

                    await StartInstanceAsync(instance, ct);

                    if (instance.Status == ServiceStatus.Failed &&
                        instance.Config.FailurePolicy == FailurePolicy.Stop)
                    {
                        step.Status = LaunchStepStatus.Failed;
                        step.ErrorMessage = instance.LastError;
                        StepChanged?.Invoke(this, step);
                        Log($"Stopping sequence due to failure policy on {instance.Config.Name}", LogLevel.Error, "Orchestrator");
                        break;
                    }

                    step.Status = instance.Status == ServiceStatus.Running
                        ? LaunchStepStatus.Completed : LaunchStepStatus.Failed;
                    step.CompletedAt = DateTime.Now;
                    StepChanged?.Invoke(this, step);

                    if (i < enabledInstances.Count - 1 && instance.Config.DelayAfterStartSeconds > 0)
                    {
                        await RunCountdownAsync(step, instance.Config.DelayAfterStartSeconds, ct);
                    }
                }
                Log("All services startup complete", LogLevel.Success, "Orchestrator");
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task StopAllAsync(CancellationToken ct = default)
        {
            if (!await _operationLock.WaitAsync(100, ct))
            {
                Log("Stop already in progress", LogLevel.Warn, "Orchestrator");
                return;
            }
            try
            {
                Log("Stopping all services...", LogLevel.Info, "Orchestrator");
                var runningInstances = _instances
                    .Where(i => i.Status == ServiceStatus.Running || i.Status == ServiceStatus.Starting)
                    .Reverse()
                    .ToList();

                foreach (var instance in runningInstances)
                {
                    ct.ThrowIfCancellationRequested();
                    await StopInstanceAsync(instance, ct);
                }
                Log("All services stopped", LogLevel.Info, "Orchestrator");
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task RestartAllAsync(CancellationToken ct = default)
        {
            await StopAllAsync(ct);
            await Task.Delay(2000, ct);
            await StartAllAsync(ct);
        }

        public async Task StartServiceAsync(Guid serviceId, CancellationToken ct = default)
        {
            var instance = _instances.FirstOrDefault(i => i.Config.Id == serviceId);
            if (instance == null) return;
            if (!await _operationLock.WaitAsync(100, ct)) return;
            try
            {
                await StartInstanceAsync(instance, ct);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task StopServiceAsync(Guid serviceId, CancellationToken ct = default)
        {
            var instance = _instances.FirstOrDefault(i => i.Config.Id == serviceId);
            if (instance == null) return;
            if (!await _operationLock.WaitAsync(100, ct)) return;
            try
            {
                await StopInstanceAsync(instance, ct);
            }
            finally
            {
                _operationLock.Release();
            }
        }

        public async Task RestartServiceAsync(Guid serviceId, CancellationToken ct = default)
        {
            var instance = _instances.FirstOrDefault(i => i.Config.Id == serviceId);
            if (instance == null) return;
            await StopServiceAsync(serviceId, ct);
            await Task.Delay(1000, ct);
            await StartServiceAsync(serviceId, ct);
        }

        private async Task StartInstanceAsync(ServiceInstance instance, CancellationToken ct)
        {
            if (instance.Status == ServiceStatus.Running)
            {
                Log($"{instance.Config.Name} is already running", LogLevel.Warn, "Orchestrator");
                return;
            }

            int retryCount = 0;
            int maxRetries = instance.Config.FailurePolicy == FailurePolicy.Retry
                ? Math.Max(1, instance.Config.MaxRetries)
                : 1;

            while (retryCount < maxRetries)
            {
                try
                {
                    UpdateStatus(instance, ServiceStatus.Starting);
                    Log($"Starting {instance.Config.Name} (attempt {retryCount + 1}/{maxRetries})...", LogLevel.Info, "Orchestrator");

                    var process = await _processManager.StartServiceAsync(instance.Config, ct);
                    if (process == null)
                    {
                        throw new InvalidOperationException("Process failed to start");
                    }

                    instance.ProcessId = process.Id;
                    instance.StartTime = DateTime.Now;
                    instance.IsOwned = true;
                    instance.RetryCount = retryCount;

                    process.Exited += (s, e) => OnProcessExited(instance, process);

                    var readiness = instance.Config.ReadinessCheck;
                    if (readiness != null && readiness.Type != ReadinessCheckType.None)
                    {
                        Log($"Waiting for {instance.Config.Name} to be ready...", LogLevel.Info, "Orchestrator");
                        bool ready;
                        // Abort the wait immediately if the process dies (e.g. missing script / crash)
                        using var readyCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        process.Exited += (s, e) => { try { readyCts.Cancel(); } catch { } };
                        try
                        {
                            ready = process.HasExited ? false
                                : await _readinessCheck.WaitForReadinessAsync(readiness, readyCts.Token);
                        }
                        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                        {
                            ready = false;
                        }

                        if (process.HasExited)
                        {
                            var tail = ReadLogTail(instance.Config.LogFilePath, 6);
                            throw new InvalidOperationException(
                                $"{instance.Config.Name} exited immediately (exit code {process.ExitCode})." +
                                (string.IsNullOrEmpty(tail) ? string.Empty : $" Last output:{Environment.NewLine}{tail}"));
                        }

                        if (!ready)
                        {
                            var target = readiness.Type == ReadinessCheckType.TcpPort
                                ? $"{readiness.Host}:{readiness.Port}"
                                : readiness.Url;
                            throw new InvalidOperationException(
                                $"{instance.Config.Name} is running but {target} did not respond within " +
                                $"{readiness.TimeoutSeconds}s. Check the readiness port in Startup Sequence matches the port the app listens on.");
                        }

                        Log($"{instance.Config.Name} is ready", LogLevel.Success, "Orchestrator");
                    }
                    else
                    {
                        // No readiness check: still catch an immediate crash
                        await Task.Delay(1500, ct);
                        if (process.HasExited)
                        {
                            var tail = ReadLogTail(instance.Config.LogFilePath, 6);
                            throw new InvalidOperationException(
                                $"{instance.Config.Name} exited immediately (exit code {process.ExitCode})." +
                                (string.IsNullOrEmpty(tail) ? string.Empty : $" Last output:{Environment.NewLine}{tail}"));
                        }
                    }

                    UpdateStatus(instance, ServiceStatus.Running);
                    instance.LastError = null;
                    Log($"{instance.Config.Name} started successfully (PID: {process.Id})", LogLevel.Success, "Orchestrator");
                    return;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    retryCount++;
                    instance.LastError = ex.Message;
                    Log($"Failed to start {instance.Config.Name}: {ex.Message}", LogLevel.Error, "Orchestrator");

                    // Don't leave a half-started owned process behind (only ever our own PID)
                    if (instance.ProcessId.HasValue && _processManager.IsProcessRunning(instance.ProcessId.Value))
                    {
                        try { await _processManager.StopServiceAsync(instance, CancellationToken.None); } catch { }
                    }
                    instance.ProcessId = null;

                    if (retryCount < maxRetries)
                    {
                        Log($"Retrying in 3 seconds...", LogLevel.Warn, "Orchestrator");
                        await Task.Delay(3000, ct);
                    }
                    else
                    {
                        UpdateStatus(instance, ServiceStatus.Failed);
                    }
                }
            }
        }

        private async Task StopInstanceAsync(ServiceInstance instance, CancellationToken ct)
        {
            if (instance.Status == ServiceStatus.Stopped || instance.Status == ServiceStatus.Disabled)
                return;

            try
            {
                UpdateStatus(instance, ServiceStatus.Stopping);
                Log($"Stopping {instance.Config.Name}...", LogLevel.Info, "Orchestrator");
                await _processManager.StopServiceAsync(instance, ct);
                instance.ProcessId = null;
                instance.StopTime = DateTime.Now;
                UpdateStatus(instance, ServiceStatus.Stopped);
                Log($"{instance.Config.Name} stopped", LogLevel.Info, "Orchestrator");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping {Name}", instance.Config.Name);
                UpdateStatus(instance, ServiceStatus.Stopped);
            }
        }

        private static string ReadLogTail(string? path, int lines)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return string.Empty;
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var all = sr.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                return string.Join(Environment.NewLine,
                    all.Skip(Math.Max(0, all.Length - lines)).Select(l => l.TrimEnd('\r')));
            }
            catch { return string.Empty; }
        }

        private void OnProcessExited(ServiceInstance instance, Process process)
        {
            if (!instance.IsOwned) return;
            // Give async stdout/stderr handlers a moment to flush to the log file
            int exitCode;
            try { exitCode = process.ExitCode; } catch { exitCode = -1; }
            instance.ProcessId = null;
            instance.StopTime = DateTime.Now;

            if (instance.Status == ServiceStatus.Failed)
                return; // start failure already reported with a detailed message

            if (instance.Status != ServiceStatus.Stopping && instance.Status != ServiceStatus.Stopped)
            {
                instance.LastError = $"Process exited unexpectedly with code {exitCode}";
                UpdateStatus(instance, ServiceStatus.Failed);
                Log($"{instance.Config.Name} exited unexpectedly (exit code: {exitCode})", LogLevel.Error, "Orchestrator");
            }
            else
            {
                UpdateStatus(instance, ServiceStatus.Stopped);
            }
        }

        private async Task RunCountdownAsync(LaunchStep step, int seconds, CancellationToken ct)
        {
            for (int remaining = seconds; remaining > 0; remaining--)
            {
                ct.ThrowIfCancellationRequested();
                step.CountdownSecondsRemaining = remaining;
                step.Message = $"Waiting {remaining}s before next step...";
                StepChanged?.Invoke(this, step);
                await Task.Delay(1000, ct);
            }
            step.CountdownSecondsRemaining = 0;
            StepChanged?.Invoke(this, step);
        }

        private void UpdateStatus(ServiceInstance instance, ServiceStatus status)
        {
            instance.Status = status;
            ServiceStatusChanged?.Invoke(this, instance);
        }

        private void Log(string message, LogLevel level, string source)
        {
            _logService.Append(message, level, source);
            var entry = new LogEntry { Message = message, Level = level, Source = source };
            LogMessage?.Invoke(this, entry);
        }
    }
}
