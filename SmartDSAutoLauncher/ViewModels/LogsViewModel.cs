using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using SmartDSAutoLauncher.Helpers;
using SmartDSAutoLauncher.Models;
using SmartDSAutoLauncher.Services;

namespace SmartDSAutoLauncher.ViewModels
{
    public class LogsViewModel : ObservableObject
    {
        private readonly LogService _logService;
        private readonly ISettingsService _settingsService;
        private string _filterText = string.Empty;
        private LogLevel _minLevel = LogLevel.Debug;

        public string FilterText { get => _filterText; set { SetProperty(ref _filterText, value); ApplyFilter(); } }
        public LogLevel MinLevel { get => _minLevel; set { SetProperty(ref _minLevel, value); ApplyFilter(); } }

        public ObservableCollection<LogEntryViewModel> AllEntries { get; } = new();
        public ObservableCollection<LogEntryViewModel> FilteredEntries { get; } = new();

        public RelayCommand ClearCommand { get; }
        public RelayCommand CopyAllCommand { get; }
        public RelayCommand OpenFolderCommand { get; }

        public LogsViewModel(LogService logService, ISettingsService settingsService)
        {
            _logService = logService; _settingsService = settingsService;
            _logService.NewLogEntry += (s, e) => Application.Current?.Dispatcher.Invoke(() =>
            {
                var vm = new LogEntryViewModel(e);
                AllEntries.Add(vm);
                while (AllEntries.Count > _settingsService.Settings.MaxLogLines) AllEntries.RemoveAt(0);
                ApplyFilter();
            });
            ClearCommand = new RelayCommand(Clear);
            CopyAllCommand = new RelayCommand(CopyAll);
            OpenFolderCommand = new RelayCommand(OpenFolder);
        }

        private void ApplyFilter()
        {
            FilteredEntries.Clear();
            foreach (var e in AllEntries)
            {
                if (e.LogLevel < _minLevel) continue;
                if (!string.IsNullOrEmpty(_filterText) && !e.Message.Contains(_filterText, StringComparison.OrdinalIgnoreCase) && !e.Source.Contains(_filterText, StringComparison.OrdinalIgnoreCase)) continue;
                FilteredEntries.Add(e);
            }
        }

        private void Clear()
        {
            var r = MessageBox.Show("Clear all logs?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes) { AllEntries.Clear(); FilteredEntries.Clear(); _logService.Clear(); }
        }

        private void CopyAll()
        {
            var sb = new StringBuilder();
            foreach (var e in AllEntries) sb.AppendLine(e.Formatted);
            if (sb.Length > 0) Clipboard.SetText(sb.ToString());
        }

        private void OpenFolder()
        {
            var dir = _settingsService.Settings.LogDirectory;
            if (Directory.Exists(dir)) System.Diagnostics.Process.Start("explorer.exe", dir);
            else MessageBox.Show($"Log folder not found:\n{dir}", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
