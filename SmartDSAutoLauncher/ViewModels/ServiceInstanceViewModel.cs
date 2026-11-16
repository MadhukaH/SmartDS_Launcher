using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SmartDSAutoLauncher.Helpers;
using SmartDSAutoLauncher.Models;
using SmartDSAutoLauncher.Services;

namespace SmartDSAutoLauncher.ViewModels
{
    public class ServiceInstanceViewModel : ObservableObject
    {
        private readonly ServiceInstance _instance;
        private readonly ILaunchOrchestrator _orchestrator;

        public ServiceInstance Instance => _instance;
        public string Name => _instance.Config.Name;
        public string Description => _instance.Config.Description;
        public ServiceStatus Status => _instance.Status;
        public int? ProcessId => _instance.ProcessId;
        public DateTime? StartTime => _instance.StartTime;
        public string? LastError => _instance.LastError;
        public bool IsEnabled => _instance.Config.IsEnabled;
        public bool HasError => !string.IsNullOrEmpty(_instance.LastError);

        public string UptimeText
        {
            get
            {
                if (_instance.Status != ServiceStatus.Running || !_instance.StartTime.HasValue) return string.Empty;
                var u = DateTime.Now - _instance.StartTime.Value;
                if (u.TotalHours >= 1) return $"{(int)u.TotalHours}h {u.Minutes}m";
                if (u.TotalMinutes >= 1) return $"{u.Minutes}m {u.Seconds}s";
                return $"{u.Seconds}s";
            }
        }

        public bool CanStart => _instance.Status is ServiceStatus.Stopped or ServiceStatus.Failed;
        public bool CanStop => _instance.Status is ServiceStatus.Running or ServiceStatus.Starting;
        public bool CanRestart => _instance.Status == ServiceStatus.Running;

        public AsyncRelayCommand StartCommand { get; }
        public AsyncRelayCommand StopCommand { get; }
        public AsyncRelayCommand RestartCommand { get; }
        public RelayCommand CopyErrorCommand { get; }

        public ServiceInstanceViewModel(ServiceInstance instance, ILaunchOrchestrator orchestrator)
        {
            _instance = instance;
            _orchestrator = orchestrator;
            StartCommand = new AsyncRelayCommand(StartAsync, () => CanStart);
            StopCommand = new AsyncRelayCommand(StopAsync, () => CanStop);
            RestartCommand = new AsyncRelayCommand(RestartAsync, () => CanRestart);
            CopyErrorCommand = new RelayCommand(CopyError, () => HasError);
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(ProcessId));
            OnPropertyChanged(nameof(StartTime));
            OnPropertyChanged(nameof(LastError));
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanStop));
            OnPropertyChanged(nameof(CanRestart));
            OnPropertyChanged(nameof(UptimeText));
        }

        private async Task StartAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await _orchestrator.StartServiceAsync(_instance.Config.Id, cts.Token);
            Refresh();
        }

        private async Task StopAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await _orchestrator.StopServiceAsync(_instance.Config.Id, cts.Token);
            Refresh();
        }

        private async Task RestartAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await _orchestrator.RestartServiceAsync(_instance.Config.Id, cts.Token);
            Refresh();
        }

        private void CopyError()
        {
            if (!string.IsNullOrEmpty(_instance.LastError))
                Clipboard.SetText(_instance.LastError);
        }
    }
}
