using System.Collections.Generic;

namespace SmartDSAutoLauncher.Models
{
    public class AppSettings
    {
        public string Theme { get; set; } = "Dark";
        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
        public int StartupDelaySeconds { get; set; } = 20;
        public bool MinimizeToTray { get; set; } = true;
        public bool ShowNotifications { get; set; } = true;
        public bool ConfirmOnShutdown { get; set; } = true;
        public bool ConfirmOnStartup { get; set; } = false;
        public bool AutoStartOnLaunch { get; set; } = true;
        public List<ServiceConfig> Services { get; set; } = new();
        public string LogDirectory { get; set; } = @"C:\SmartDS\logs";
        public bool EnableLogRotation { get; set; } = false;
        public int MaxLogSizeMB { get; set; } = 50;
        public string FrontendHost { get; set; } = "0.0.0.0";
        public int FrontendPort { get; set; } = 3001;
        public string BackendHost { get; set; } = "localhost";
        public int BackendPort { get; set; } = 3000;
        public int MaxLogLines { get; set; } = 2000;
        public string AppVersion { get; set; } = "1.0.0";
    }
}
