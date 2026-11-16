using System;
using System.Threading.Tasks;
using System.Windows;
using SmartDSAutoLauncher.Helpers;
using SmartDSAutoLauncher.Models;
using SmartDSAutoLauncher.Services;

namespace SmartDSAutoLauncher.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IStartupRegistrationService _startupService;

        private string _theme = "Dark";
        private bool _startWithWindows;
        private bool _startMinimized;
        private int _startupDelay = 20;
        private bool _minimizeToTray = true;
        private bool _showNotifications = true;
        private bool _confirmOnShutdown = true;
        private bool _confirmOnStartup;
        private bool _autoStartOnLaunch = true;
        private string _logDirectory = @"C:\SmartDS\logs";
        private bool _enableLogRotation;
        private int _maxLogSizeMB = 50;
        private int _frontendPort = 3001;
        private int _backendPort = 3000;
        private string _backendHost = "localhost";
        private string _frontendHost = "0.0.0.0";
        private bool _isDirty;
        private string _statusMessage = string.Empty;

        public string Theme { get => _theme; set { SetProperty(ref _theme, value); MarkDirty(); } }
        public bool StartWithWindows { get => _startWithWindows; set { SetProperty(ref _startWithWindows, value); MarkDirty(); } }
        public bool StartMinimized { get => _startMinimized; set { SetProperty(ref _startMinimized, value); MarkDirty(); } }
        public int StartupDelay { get => _startupDelay; set { SetProperty(ref _startupDelay, value); MarkDirty(); } }
        public bool MinimizeToTray { get => _minimizeToTray; set { SetProperty(ref _minimizeToTray, value); MarkDirty(); } }
        public bool ShowNotifications { get => _showNotifications; set { SetProperty(ref _showNotifications, value); MarkDirty(); } }
        public bool ConfirmOnShutdown { get => _confirmOnShutdown; set { SetProperty(ref _confirmOnShutdown, value); MarkDirty(); } }
        public bool ConfirmOnStartup { get => _confirmOnStartup; set { SetProperty(ref _confirmOnStartup, value); MarkDirty(); } }
        public bool AutoStartOnLaunch { get => _autoStartOnLaunch; set { SetProperty(ref _autoStartOnLaunch, value); MarkDirty(); } }
        public string LogDirectory { get => _logDirectory; set { SetProperty(ref _logDirectory, value); MarkDirty(); } }
        public bool EnableLogRotation { get => _enableLogRotation; set { SetProperty(ref _enableLogRotation, value); MarkDirty(); } }
        public int MaxLogSizeMB { get => _maxLogSizeMB; set { SetProperty(ref _maxLogSizeMB, value); MarkDirty(); } }
        public int FrontendPort { get => _frontendPort; set { SetProperty(ref _frontendPort, value); MarkDirty(); } }
        public int BackendPort { get => _backendPort; set { SetProperty(ref _backendPort, value); MarkDirty(); } }
        public string BackendHost { get => _backendHost; set { SetProperty(ref _backendHost, value); MarkDirty(); } }
        public string FrontendHost { get => _frontendHost; set { SetProperty(ref _frontendHost, value); MarkDirty(); } }
        public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public bool IsStartupRegistered => _startupService.IsRegistered;

        public AsyncRelayCommand SaveCommand { get; }
        public RelayCommand ResetCommand { get; }
        public RelayCommand DiscardCommand { get; }
        public RelayCommand BrowseLogDirCommand { get; }

        public SettingsViewModel(ISettingsService settingsService, IStartupRegistrationService startupService)
        {
            _settingsService = settingsService;
            _startupService = startupService;
            SaveCommand = new AsyncRelayCommand(SaveAsync, () => IsDirty);
            ResetCommand = new RelayCommand(Reset);
            DiscardCommand = new RelayCommand(LoadFromSettings);
            BrowseLogDirCommand = new RelayCommand(BrowseLogDir);
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            var s = _settingsService.Settings;
            _theme = s.Theme; _startWithWindows = s.StartWithWindows; _startMinimized = s.StartMinimized;
            _startupDelay = s.StartupDelaySeconds; _minimizeToTray = s.MinimizeToTray;
            _showNotifications = s.ShowNotifications; _confirmOnShutdown = s.ConfirmOnShutdown;
            _confirmOnStartup = s.ConfirmOnStartup; _autoStartOnLaunch = s.AutoStartOnLaunch;
            _logDirectory = s.LogDirectory; _enableLogRotation = s.EnableLogRotation;
            _maxLogSizeMB = s.MaxLogSizeMB; _frontendPort = s.FrontendPort; _backendPort = s.BackendPort;
            _backendHost = s.BackendHost; _frontendHost = s.FrontendHost;
            OnPropertyChanged(string.Empty);
            IsDirty = false;
            StatusMessage = string.Empty;
        }

        private async Task SaveAsync()
        {
            try
            {
                var s = _settingsService.Settings;
                s.Theme = _theme; s.StartWithWindows = _startWithWindows; s.StartMinimized = _startMinimized;
                s.StartupDelaySeconds = _startupDelay; s.MinimizeToTray = _minimizeToTray;
                s.ShowNotifications = _showNotifications; s.ConfirmOnShutdown = _confirmOnShutdown;
                s.ConfirmOnStartup = _confirmOnStartup; s.AutoStartOnLaunch = _autoStartOnLaunch;
                s.LogDirectory = _logDirectory; s.EnableLogRotation = _enableLogRotation;
                s.MaxLogSizeMB = _maxLogSizeMB; s.FrontendPort = _frontendPort; s.BackendPort = _backendPort;
                s.BackendHost = _backendHost; s.FrontendHost = _frontendHost;

                if (_startWithWindows && !_startupService.IsRegistered) _startupService.Register(_startupDelay);
                else if (!_startWithWindows && _startupService.IsRegistered) _startupService.Unregister();

                await _settingsService.SaveAsync();
                IsDirty = false; StatusMessage = "Settings saved successfully";
                OnPropertyChanged(nameof(IsStartupRegistered));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
                MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Reset()
        {
            var r = MessageBox.Show("Reset all settings to defaults?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) { _settingsService.ResetToDefaults(); LoadFromSettings(); StatusMessage = "Reset to defaults"; }
        }

        private void BrowseLogDir()
        {
            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select log directory",
                InitialDirectory = System.IO.Directory.Exists(LogDirectory) ? LogDirectory : string.Empty
            };
            if (dlg.ShowDialog() == true)
            {
                LogDirectory = dlg.FolderName;
            }
        }

        private void MarkDirty() => IsDirty = true;
    }
}
