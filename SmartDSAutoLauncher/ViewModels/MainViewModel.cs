using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SmartDSAutoLauncher.Helpers;
using SmartDSAutoLauncher.Models;
using SmartDSAutoLauncher.Services;

namespace SmartDSAutoLauncher.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly ILaunchOrchestrator _orchestrator;
        private readonly ISettingsService _settingsService;
        private readonly LogService _logService;
        private readonly INotificationService _notificationService;
        private CancellationTokenSource? _cts;

        private string _currentPage = "Dashboard";
        private bool _isStartupRunning;
        private string _statusMessage = "Ready";
        private int _totalServices;
        private int _runningServices;
        private int _stoppedServices;
        private int _failedServices;
        private LaunchStep? _currentStep;

        public string CurrentPage { get => _currentPage; set => SetProperty(ref _currentPage, value); }

        public bool IsStartupRunning
        {
            get => _isStartupRunning;
            set { SetProperty(ref _isStartupRunning, value); OnPropertyChanged(nameof(IsNotRunning)); }
        }

        public bool IsNotRunning => !_isStartupRunning;
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public int TotalServices { get => _totalServices; set => SetProperty(ref _totalServices, value); }
        public int RunningServices { get => _runningServices; set => SetProperty(ref _runningServices, value); }
        public int StoppedServices { get => _stoppedServices; set => SetProperty(ref _stoppedServices, value); }
        public int FailedServices { get => _failedServices; set => SetProperty(ref _failedServices, value); }
        public LaunchStep? CurrentStep { get => _currentStep; set => SetProperty(ref _currentStep, value); }

        public ObservableCollection<ServiceInstanceViewModel> ServiceViewModels { get; } = new();
        public ObservableCollection<LogEntryViewModel> LogEntries { get; } = new();

        public AsyncRelayCommand StartAllCommand { get; }
        public AsyncRelayCommand StopAllCommand { get; }
        public AsyncRelayCommand RestartAllCommand { get; }
        public RelayCommand NavigateToDashboardCommand { get; }
        public RelayCommand NavigateToSettingsCommand { get; }
        public RelayCommand NavigateToSequenceCommand { get; }
        public RelayCommand NavigateToLogsCommand { get; }
        public RelayCommand NavigateToAboutCommand { get; }
        public RelayCommand ClearLogsCommand { get; }
        public RelayCommand CopyLastErrorCommand { get; }
        public RelayCommand OpenLogFolderCommand { get; }

        public DashboardViewModel DashboardVM { get; }
        public SettingsViewModel SettingsVM { get; }
        public SequenceViewModel SequenceVM { get; }
        public LogsViewModel LogsVM { get; }

        public MainViewModel(
            ILaunchOrchestrator orchestrator,
            ISettingsService settingsService,
            LogService logService,
            INotificationService notificationService,
            DashboardViewModel dashboardVM,
            SettingsViewModel settingsVM,
            SequenceViewModel sequenceVM,
            LogsViewModel logsVM)
        {
            _orchestrator = orchestrator;
            _settingsService = settingsService;
            _logService = logService;
            _notificationService = notificationService;

            DashboardVM = dashboardVM;
            SettingsVM = settingsVM;
            SequenceVM = sequenceVM;
            LogsVM = logsVM;

            _orchestrator.Initialize(_settingsService.Settings.Services);
            InitializeServiceViewModels();

            _orchestrator.ServiceStatusChanged += OnServiceStatusChanged;
            _orchestrator.StepChanged += OnStepChanged;
            _orchestrator.LogMessage += OnLogMessage;
            _logService.NewLogEntry += OnNewLogEntry;

            StartAllCommand = new AsyncRelayCommand(StartAllAsync, () => !IsStartupRunning);
            StopAllCommand = new AsyncRelayCommand(StopAllAsync);
            RestartAllCommand = new AsyncRelayCommand(RestartAllAsync, () => !IsStartupRunning);
            NavigateToDashboardCommand = new RelayCommand(() => CurrentPage = "Dashboard");
            NavigateToSettingsCommand = new RelayCommand(() => CurrentPage = "Settings");
            NavigateToSequenceCommand = new RelayCommand(() => CurrentPage = "Sequence");
            NavigateToLogsCommand = new RelayCommand(() => CurrentPage = "Logs");
            NavigateToAboutCommand = new RelayCommand(() => CurrentPage = "About");
            ClearLogsCommand = new RelayCommand(ClearLogs);
            CopyLastErrorCommand = new RelayCommand(CopyLastError);
            OpenLogFolderCommand = new RelayCommand(OpenLogFolder);

            UpdateCounts();
        }

        private void InitializeServiceViewModels()
        {
            ServiceViewModels.Clear();
            foreach (var instance in _orchestrator.ServiceInstances)
                ServiceViewModels.Add(new ServiceInstanceViewModel(instance, _orchestrator));
            UpdateCounts();
        }

        private async Task StartAllAsync()
        {
            _cts = new CancellationTokenSource();
            IsStartupRunning = true;
            StatusMessage = "Starting all services...";
            try
            {
                await _orchestrator.StartAllAsync(_cts.Token);
                StatusMessage = "All services started";
                _notificationService.NotifySuccess("SmartDS Launcher", "All services started successfully");
            }
            catch (OperationCanceledException) { StatusMessage = "Startup cancelled"; }
            catch (Exception ex)
            {
                StatusMessage = $"Startup failed: {ex.Message}";
                _notificationService.NotifyError("SmartDS Launcher", $"Startup failed: {ex.Message}");
            }
            finally { IsStartupRunning = false; _cts = null; }
        }

        private async Task StopAllAsync()
        {
            _cts?.Cancel();
            StatusMessage = "Stopping all services...";
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _orchestrator.StopAllAsync(cts.Token);
                StatusMessage = "All services stopped";
            }
            catch (Exception ex) { StatusMessage = $"Stop failed: {ex.Message}"; }
        }

        private async Task RestartAllAsync()
        {
            StatusMessage = "Restarting all services...";
            IsStartupRunning = true;
            _cts = new CancellationTokenSource();
            try
            {
                await _orchestrator.RestartAllAsync(_cts.Token);
                StatusMessage = "All services restarted";
            }
            catch (Exception ex) { StatusMessage = $"Restart failed: {ex.Message}"; }
            finally { IsStartupRunning = false; _cts = null; }
        }

        private void OnServiceStatusChanged(object? sender, ServiceInstance instance)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = ServiceViewModels.FirstOrDefault(s => s.Instance.Config.Id == instance.Config.Id);
                if (vm != null) vm.Refresh();
                UpdateCounts();
            });
        }

        private void OnStepChanged(object? sender, LaunchStep step)
        {
            Application.Current?.Dispatcher.Invoke(() => CurrentStep = step);
        }

        private void OnLogMessage(object? sender, LogEntry entry)
        {
            Application.Current?.Dispatcher.Invoke(() => AddLogEntry(entry));
        }

        private void OnNewLogEntry(object? sender, LogEntry entry)
        {
            Application.Current?.Dispatcher.Invoke(() => AddLogEntry(entry));
        }

        private void AddLogEntry(LogEntry entry)
        {
            LogEntries.Add(new LogEntryViewModel(entry));
            while (LogEntries.Count > _settingsService.Settings.MaxLogLines)
                LogEntries.RemoveAt(0);
        }

        private void UpdateCounts()
        {
            TotalServices = ServiceViewModels.Count;
            RunningServices = ServiceViewModels.Count(s => s.Status == ServiceStatus.Running);
            StoppedServices = ServiceViewModels.Count(s => s.Status == ServiceStatus.Stopped);
            FailedServices = ServiceViewModels.Count(s => s.Status == ServiceStatus.Failed);
        }

        private void ClearLogs()
        {
            var r = MessageBox.Show("Clear all log entries?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) { LogEntries.Clear(); _logService.Clear(); }
        }

        private void CopyLastError()
        {
            var err = ServiceViewModels.Where(s => !string.IsNullOrEmpty(s.LastError)).Select(s => s.LastError).LastOrDefault();
            if (!string.IsNullOrEmpty(err)) Clipboard.SetText(err);
        }

        private void OpenLogFolder()
        {
            var dir = _settingsService.Settings.LogDirectory;
            if (Directory.Exists(dir)) System.Diagnostics.Process.Start("explorer.exe", dir);
            else MessageBox.Show($"Log folder not found: {dir}", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void OnWindowClosing()
        {
            if (_settingsService.Settings.MinimizeToTray) return;
            Application.Current?.Shutdown();
        }

        public void RefreshServiceViewModels()
        {
            InitializeServiceViewModels();
        }
    }
}
