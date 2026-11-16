using System;
using System.Threading.Tasks;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartDSAutoLauncher.Services;
using SmartDSAutoLauncher.ViewModels;
using SmartDSAutoLauncher.Views;
using SmartDSAutoLauncher.Helpers;

namespace SmartDSAutoLauncher
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        public TaskbarIcon? TrayIcon { get; private set; }
        private bool _startMinimized;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _startMinimized = WindowsStartupHelper.IsCommandLineArgPresent(e.Args, "--minimized");
            var startupDelay = WindowsStartupHelper.GetStartupDelay(e.Args);

            // Build DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            // Load settings
            var settingsService = Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadAsync();

            // Apply startup delay
            if (startupDelay > 0)
                await Task.Delay(TimeSpan.FromSeconds(startupDelay));

            // Setup tray icon
            SetupTrayIcon();

            // Show main window
            var mainWindow = Services.GetRequiredService<MainWindow>();
            if (_startMinimized || settingsService.Settings.StartMinimized)
            {
                mainWindow.WindowState = WindowState.Minimized;
                if (settingsService.Settings.MinimizeToTray)
                    mainWindow.Hide();
                else
                    mainWindow.Show();
            }
            else
            {
                mainWindow.Show();
            }

            // Auto-start services if configured
            if (settingsService.Settings.AutoStartOnLaunch)
            {
                var vm = Services.GetRequiredService<MainViewModel>();
                _ = Task.Run(async () => await vm.StartAllCommand.ExecuteAsync(null));
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(b => b.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug));

            // Services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IProcessManager, ProcessManager>();
            services.AddSingleton<IReadinessCheckService, ReadinessCheckService>();
            services.AddSingleton<IStartupRegistrationService, StartupRegistrationService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<ILaunchOrchestrator, LaunchOrchestrator>();
            services.AddSingleton<LogService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<SequenceViewModel>();
            services.AddSingleton<LogsViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
        }

        private void SetupTrayIcon()
        {
            TrayIcon = new TaskbarIcon
            {
                ToolTipText = "SmartDS Auto Launcher",
                Visibility = Visibility.Visible
            };

            var menu = new System.Windows.Controls.ContextMenu();
            var openItem = new System.Windows.Controls.MenuItem { Header = "Open Dashboard" };
            openItem.Click += (s, e) => ShowMainWindow();
            var startItem = new System.Windows.Controls.MenuItem { Header = "Start All" };
            startItem.Click += async (s, e) =>
            {
                var vm = Services.GetRequiredService<MainViewModel>();
                await vm.StartAllCommand.ExecuteAsync(null);
            };
            var stopItem = new System.Windows.Controls.MenuItem { Header = "Stop All" };
            stopItem.Click += async (s, e) =>
            {
                var vm = Services.GetRequiredService<MainViewModel>();
                await vm.StopAllCommand.ExecuteAsync(null);
            };
            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => Shutdown();

            menu.Items.Add(openItem);
            menu.Items.Add(new System.Windows.Controls.Separator());
            menu.Items.Add(startItem);
            menu.Items.Add(stopItem);
            menu.Items.Add(new System.Windows.Controls.Separator());
            menu.Items.Add(exitItem);

            TrayIcon.ContextMenu = menu;
            TrayIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
