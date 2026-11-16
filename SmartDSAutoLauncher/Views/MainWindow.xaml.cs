using System.ComponentModel;
using System.Windows;
using SmartDSAutoLauncher.Services;
using SmartDSAutoLauncher.ViewModels;

namespace SmartDSAutoLauncher.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly ISettingsService _settingsService;
        private readonly DashboardView _dashboardView;
        private readonly SequenceView _sequenceView;
        private readonly LogsView _logsView;
        private readonly SettingsView _settingsView;
        private readonly AboutView _aboutView;
        private bool _isExplicitExit;

        public MainWindow(MainViewModel viewModel, ISettingsService settingsService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _settingsService = settingsService;
            DataContext = _viewModel;

            _dashboardView = new DashboardView { DataContext = _viewModel };
            _sequenceView = new SequenceView { DataContext = _viewModel.SequenceVM };
            _logsView = new LogsView { DataContext = _viewModel.LogsVM };
            _settingsView = new SettingsView { DataContext = _viewModel.SettingsVM };
            _aboutView = new AboutView { DataContext = _viewModel };

            UpdateCurrentView();
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentPage))
                {
                    Dispatcher.Invoke(UpdateCurrentView);
                }
            };
        }

        private void UpdateCurrentView()
        {
            MainContentHost.Content = _viewModel.CurrentPage switch
            {
                "Dashboard" => _dashboardView,
                "Sequence" => _sequenceView,
                "Logs" => _logsView,
                "Settings" => _settingsView,
                "About" => _aboutView,
                _ => _dashboardView
            };
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isExplicitExit)
            {
                base.OnClosing(e);
                return;
            }

            if (_settingsService.Settings.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            if (_settingsService.Settings.ConfirmOnShutdown)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to exit SmartDS Auto Launcher?\nAny background services owned by this launcher may be stopped.",
                    "Confirm Exit",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnClosing(e);
        }

        public void ForceClose()
        {
            _isExplicitExit = true;
            Close();
        }
    }
}
