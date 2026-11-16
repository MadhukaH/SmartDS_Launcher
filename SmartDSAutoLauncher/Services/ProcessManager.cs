using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.Services
{
    public class ProcessManager : IProcessManager
    {
        private readonly ILogger<ProcessManager> _logger;
        private readonly HashSet<int> _ownedPids = new();
        private readonly object _lock = new();

        public ProcessManager(ILogger<ProcessManager> logger)
        {
            _logger = logger;
        }

        public async Task<Process?> StartServiceAsync(ServiceConfig config, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(config.ExecutablePath))
            {
                _logger.LogError("Executable path is empty for service {Name}", config.Name);
                throw new ArgumentException($"Executable path is empty for service '{config.Name}'");
            }

            try
            {
                var psi = new ProcessStartInfo();

                if (config.UseShellExecute)
                {
                    psi.FileName = config.ExecutablePath;
                    psi.UseShellExecute = true;
                    psi.WindowStyle = ProcessWindowStyle.Normal;
                }
                else
                {
                    psi.FileName = config.ExecutablePath;
                    psi.Arguments = config.Arguments;
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    psi.StandardOutputEncoding = Encoding.UTF8;
                    psi.StandardErrorEncoding = Encoding.UTF8;

                    if (!string.IsNullOrWhiteSpace(config.WorkingDirectory))
                        psi.WorkingDirectory = config.WorkingDirectory;

                    foreach (var kvp in config.EnvironmentVariables)
                        psi.EnvironmentVariables[kvp.Key] = kvp.Value;
                }

                _logger.LogInformation("Starting service {Name}: {Exe} {Args}",
                    config.Name, config.ExecutablePath, config.Arguments);

                var process = new Process { StartInfo = psi };

                if (!config.UseShellExecute && !string.IsNullOrWhiteSpace(config.LogFilePath))
                {
                    SetupLogCapture(process, config);
                }

                process.EnableRaisingEvents = true;

                if (!process.Start())
                {
                    _logger.LogError("Failed to start process for {Name}", config.Name);
                    return null;
                }

                if (!config.UseShellExecute)
                {
                    if (psi.RedirectStandardOutput)
                        process.BeginOutputReadLine();
                    if (psi.RedirectStandardError)
                        process.BeginErrorReadLine();
                }

                lock (_lock)
                {
                    _ownedPids.Add(process.Id);
                }

                _logger.LogInformation("Service {Name} started with PID {Pid}", config.Name, process.Id);
                return process;
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogError(ex, "Executable not found for service {Name}: {Path}", config.Name, config.ExecutablePath);
                throw new InvalidOperationException($"Executable not found: {config.ExecutablePath}", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied starting service {Name}", config.Name);
                throw new InvalidOperationException($"Access denied: cannot start {config.ExecutablePath}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting service {Name}", config.Name);
                throw;
            }
        }

        private void SetupLogCapture(Process process, ServiceConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.LogFilePath)) return;

            try
            {
                var logDir = Path.GetDirectoryName(config.LogFilePath);
                if (!string.IsNullOrEmpty(logDir))
                    Directory.CreateDirectory(logDir);

                process.OutputDataReceived += (s, e) => AppendToLog(config.LogFilePath, e.Data, "OUT");
                process.ErrorDataReceived += (s, e) => AppendToLog(config.LogFilePath, e.Data, "ERR");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to setup log capture for {Name}", config.Name);
            }
        }

        private void AppendToLog(string logFilePath, string? data, string stream)
        {
            if (string.IsNullOrEmpty(data)) return;
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{stream}] {data}{Environment.NewLine}";
                File.AppendAllText(logFilePath, line, Encoding.UTF8);
            }
            catch { /* Ignore log write errors */ }
        }

        public async Task StopServiceAsync(ServiceInstance instance, CancellationToken ct = default)
        {
            if (!instance.ProcessId.HasValue)
            {
                _logger.LogWarning("No process ID for service {Name}", instance.Config.Name);
                return;
            }

            var pid = instance.ProcessId.Value;

            if (!IsOwnedProcess(pid))
            {
                _logger.LogWarning("PID {Pid} is not an owned process, skipping stop", pid);
                return;
            }

            try
            {
                _logger.LogInformation("Stopping service {Name} (PID {Pid})", instance.Config.Name, pid);

                var killPsi = new ProcessStartInfo("taskkill", $"/pid {pid} /t /f")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var killProcess = new Process { StartInfo = killPsi };
                killProcess.Start();
                await Task.Run(() => killProcess.WaitForExit(5000), ct);

                lock (_lock)
                {
                    _ownedPids.Remove(pid);
                }

                _logger.LogInformation("Service {Name} stopped", instance.Config.Name);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error stopping service {Name}", instance.Config.Name);
            }
        }

        public bool IsProcessRunning(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        }

        public bool IsOwnedProcess(int processId)
        {
            lock (_lock)
            {
                return _ownedPids.Contains(processId);
            }
        }

        public void RegisterOwnedProcess(int processId)
        {
            lock (_lock)
            {
                _ownedPids.Add(processId);
            }
        }

        public void UnregisterOwnedProcess(int processId)
        {
            lock (_lock)
            {
                _ownedPids.Remove(processId);
            }
        }
    }
}
