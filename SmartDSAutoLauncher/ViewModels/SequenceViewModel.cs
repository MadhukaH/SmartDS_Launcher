using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SmartDSAutoLauncher.Helpers;
using SmartDSAutoLauncher.Models;
using SmartDSAutoLauncher.Services;

namespace SmartDSAutoLauncher.ViewModels
{
    public class ServiceConfigViewModel : ObservableObject
    {
        private readonly ServiceConfig _originalConfig;
        public Guid Id => _originalConfig.Id;

        private string _name = string.Empty;
        private bool _isEnabled = true;
        private string _executablePath = string.Empty;
        private string _arguments = string.Empty;
        private string _workingDirectory = string.Empty;
        private int _delayAfterStartSeconds;
        private bool _useShellExecute;
        private bool _waitForPrevious = true;
        private string _logFilePath = string.Empty;
        private string _description = string.Empty;
        private FailurePolicy _failurePolicy = FailurePolicy.Continue;
        private int _maxRetries = 3;
        private string _envVarsText = string.Empty;
        private bool _hasReadinessCheck;
        private ReadinessCheckType _readinessType = ReadinessCheckType.TcpPort;
        private string _readinessHost = "localhost";
        private int _readinessPort = 3000;
        private string _readinessUrl = string.Empty;
        private int _readinessTimeout = 30;

        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }
        public string ExecutablePath { get => _executablePath; set => SetProperty(ref _executablePath, value); }
        public string Arguments { get => _arguments; set => SetProperty(ref _arguments, value); }
        /// <summary>True for backend/frontend: their working folder is fixed and cannot be edited.</summary>
        public bool IsWorkingDirectoryLocked => LockedPaths.IsLocked(_originalConfig.Id);
        public bool IsWorkingDirectoryEditable => !IsWorkingDirectoryLocked;
        public string WorkingDirectory
        {
            get => _workingDirectory;
            set
            {
                if (IsWorkingDirectoryLocked) return; // fixed path, ignore edits
                SetProperty(ref _workingDirectory, value);
            }
        }
        public int DelayAfterStartSeconds { get => _delayAfterStartSeconds; set => SetProperty(ref _delayAfterStartSeconds, value); }
        public bool UseShellExecute { get => _useShellExecute; set => SetProperty(ref _useShellExecute, value); }
        public bool WaitForPrevious { get => _waitForPrevious; set => SetProperty(ref _waitForPrevious, value); }
        public string LogFilePath { get => _logFilePath; set => SetProperty(ref _logFilePath, value); }
        public string Description { get => _description; set => SetProperty(ref _description, value); }
        public FailurePolicy FailurePolicy { get => _failurePolicy; set => SetProperty(ref _failurePolicy, value); }
        public int MaxRetries { get => _maxRetries; set => SetProperty(ref _maxRetries, value); }
        public string EnvVarsText { get => _envVarsText; set => SetProperty(ref _envVarsText, value); }
        public bool HasReadinessCheck { get => _hasReadinessCheck; set => SetProperty(ref _hasReadinessCheck, value); }
        public ReadinessCheckType ReadinessType { get => _readinessType; set => SetProperty(ref _readinessType, value); }
        public string ReadinessHost { get => _readinessHost; set => SetProperty(ref _readinessHost, value); }
        public int ReadinessPort { get => _readinessPort; set => SetProperty(ref _readinessPort, value); }
        public string ReadinessUrl { get => _readinessUrl; set => SetProperty(ref _readinessUrl, value); }
        public int ReadinessTimeout { get => _readinessTimeout; set => SetProperty(ref _readinessTimeout, value); }

        public ServiceConfigViewModel(ServiceConfig config)
        {
            _originalConfig = config;
            _name = config.Name; _isEnabled = config.IsEnabled;
            _executablePath = config.ExecutablePath; _arguments = config.Arguments;
            _workingDirectory = LockedPaths.TryGetDirectory(config.Id, out var lockedDir)
                ? lockedDir
                : config.WorkingDirectory;
            _delayAfterStartSeconds = config.DelayAfterStartSeconds;
            _useShellExecute = config.UseShellExecute; _waitForPrevious = config.WaitForPrevious;
            _logFilePath = config.LogFilePath; _description = config.Description;
            _failurePolicy = config.FailurePolicy; _maxRetries = config.MaxRetries;
            _envVarsText = string.Join(Environment.NewLine, config.EnvironmentVariables.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            if (config.ReadinessCheck != null && config.ReadinessCheck.Type != ReadinessCheckType.None)
            {
                _hasReadinessCheck = true;
                _readinessType = config.ReadinessCheck.Type;
                _readinessHost = config.ReadinessCheck.Host;
                _readinessPort = config.ReadinessCheck.Port;
                _readinessUrl = config.ReadinessCheck.Url;
                _readinessTimeout = config.ReadinessCheck.TimeoutSeconds;
            }
        }

        public ServiceConfig ToServiceConfig()
        {
            var c = _originalConfig.Clone();
            c.Name = _name; c.IsEnabled = _isEnabled; c.ExecutablePath = _executablePath;
            c.Arguments = _arguments; c.WorkingDirectory = _workingDirectory;
            c.DelayAfterStartSeconds = _delayAfterStartSeconds; c.UseShellExecute = _useShellExecute;
            c.WaitForPrevious = _waitForPrevious; c.LogFilePath = _logFilePath;
            c.Description = _description; c.FailurePolicy = _failurePolicy; c.MaxRetries = _maxRetries;
            c.EnvironmentVariables = ParseEnvVars(_envVarsText);
            c.ReadinessCheck = _hasReadinessCheck ? new ReadinessCheckConfig
            {
                Type = _readinessType, Host = _readinessHost, Port = _readinessPort,
                Url = _readinessUrl, TimeoutSeconds = _readinessTimeout
            } : null;
            LockedPaths.Enforce(c);
            return c;
        }

        private static Dictionary<string, string> ParseEnvVars(string text)
        {
            var d = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(text)) return d;
            foreach (var line in text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var i = line.IndexOf('=');
                if (i > 0) { var k = line[..i].Trim(); if (!string.IsNullOrEmpty(k)) d[k] = line[(i + 1)..].Trim(); }
            }
            return d;
        }
    }

    public class SequenceViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly ILaunchOrchestrator _orchestrator;
        private ServiceConfigViewModel? _selectedStep;
        private bool _isDirty;
        private string _statusMessage = string.Empty;

        public ObservableCollection<ServiceConfigViewModel> Steps { get; } = new();
        public ServiceConfigViewModel? SelectedStep { get => _selectedStep; set => SetProperty(ref _selectedStep, value); }
        public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public AsyncRelayCommand SaveCommand { get; }
        public RelayCommand AddStepCommand { get; }
        public RelayCommand RemoveStepCommand { get; }
        public RelayCommand MoveUpCommand { get; }
        public RelayCommand MoveDownCommand { get; }
        public RelayCommand ResetCommand { get; }
        public RelayCommand BrowseExeCommand { get; }
        public RelayCommand BrowseWorkDirCommand { get; }
        public RelayCommand BrowseLogFileCommand { get; }

        public SequenceViewModel(ISettingsService settingsService, ILaunchOrchestrator orchestrator)
        {
            _settingsService = settingsService; _orchestrator = orchestrator;
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => IsDirty);
            AddStepCommand = new RelayCommand(AddStep);
            RemoveStepCommand = new RelayCommand(RemoveStep, () => SelectedStep != null);
            MoveUpCommand = new RelayCommand(MoveUp, () => SelectedStep != null && Steps.IndexOf(SelectedStep) > 0);
            MoveDownCommand = new RelayCommand(MoveDown, () => SelectedStep != null && Steps.IndexOf(SelectedStep) < Steps.Count - 1);
            ResetCommand = new RelayCommand(Reset);
            BrowseExeCommand = new RelayCommand(BrowseExe, () => SelectedStep != null);
            BrowseWorkDirCommand = new RelayCommand(BrowseWorkDir, () => SelectedStep != null && !SelectedStep.IsWorkingDirectoryLocked);
            BrowseLogFileCommand = new RelayCommand(BrowseLogFile, () => SelectedStep != null);
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            Steps.Clear();
            foreach (var svc in _settingsService.Settings.Services) Steps.Add(new ServiceConfigViewModel(svc));
            SelectedStep = Steps.FirstOrDefault();
            IsDirty = false;
        }

        private async Task SaveAsync()
        {
            try
            {
                _settingsService.Settings.Services.Clear();
                foreach (var step in Steps) _settingsService.Settings.Services.Add(step.ToServiceConfig());
                await _settingsService.SaveAsync();
                _orchestrator.Initialize(_settingsService.Settings.Services);
                IsDirty = false; StatusMessage = "Startup sequence saved successfully";
            }
            catch (Exception ex) { StatusMessage = $"Save failed: {ex.Message}"; }
        }

        private void AddStep()
        {
            var c = new ServiceConfig { Id = Guid.NewGuid(), Name = "New Service", IsEnabled = true, DelayAfterStartSeconds = 5, FailurePolicy = FailurePolicy.Continue, WaitForPrevious = true };
            var vm = new ServiceConfigViewModel(c); Steps.Add(vm); SelectedStep = vm; IsDirty = true;
        }

        private void RemoveStep()
        {
            if (SelectedStep == null) return;
            var r = MessageBox.Show($"Remove '{SelectedStep.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) { Steps.Remove(SelectedStep); SelectedStep = Steps.FirstOrDefault(); IsDirty = true; }
        }

        private void MoveUp() { if (SelectedStep == null) return; var i = Steps.IndexOf(SelectedStep); if (i > 0) { Steps.Move(i, i - 1); IsDirty = true; } }
        private void MoveDown() { if (SelectedStep == null) return; var i = Steps.IndexOf(SelectedStep); if (i < Steps.Count - 1) { Steps.Move(i, i + 1); IsDirty = true; } }

        private void Reset()
        {
            var r = MessageBox.Show("Reset to defaults?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) { _settingsService.ResetToDefaults(); LoadFromSettings(); StatusMessage = "Reset to defaults"; }
        }

        private void BrowseExe()
        {
            if (SelectedStep == null) return;
            var d = new Microsoft.Win32.OpenFileDialog { Filter = "Executables|*.exe;*.lnk;*.bat;*.cmd|All|*.*", Title = "Select executable" };
            if (d.ShowDialog() == true) { SelectedStep.ExecutablePath = d.FileName; IsDirty = true; }
        }

        private void BrowseWorkDir()
        {
            if (SelectedStep == null || SelectedStep.IsWorkingDirectoryLocked) return;
            var d = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select working directory",
                InitialDirectory = System.IO.Directory.Exists(SelectedStep.WorkingDirectory) ? SelectedStep.WorkingDirectory : string.Empty
            };
            if (d.ShowDialog() == true)
            {
                SelectedStep.WorkingDirectory = d.FolderName;
                IsDirty = true;
            }
        }

        private void BrowseLogFile()
        {
            if (SelectedStep == null) return;
            var d = new Microsoft.Win32.SaveFileDialog { Filter = "Log files|*.log|All|*.*", Title = "Select log file", FileName = SelectedStep.LogFilePath };
            if (d.ShowDialog() == true) { SelectedStep.LogFilePath = d.FileName; IsDirty = true; }
        }
    }
}
