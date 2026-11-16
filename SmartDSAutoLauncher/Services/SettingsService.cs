using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartDSAutoLauncher");
        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AppSettings Settings { get; private set; } = new();

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger;
        }

        public async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    _logger.LogInformation("Settings file not found, creating defaults at {Path}", SettingsFilePath);
                    Settings = GetDefaultSettings();
                    await SaveAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(SettingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded == null)
                {
                    _logger.LogWarning("Failed to deserialize settings, using defaults");
                    Settings = GetDefaultSettings();
                }
                else
                {
                    Settings = loaded;
                    // Merge defaults if services list is empty
                    if (Settings.Services == null || Settings.Services.Count == 0)
                        Settings.Services = GetDefaultSettings().Services;
                }
                _logger.LogInformation("Settings loaded from {Path}", SettingsFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings, using defaults");
                Settings = GetDefaultSettings();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                var json = JsonSerializer.Serialize(Settings, JsonOptions);
                await File.WriteAllTextAsync(SettingsFilePath, json);
                _logger.LogInformation("Settings saved to {Path}", SettingsFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                throw;
            }
        }

        public AppSettings GetDefaultSettings()
        {
            return new AppSettings
            {
                Theme = "Dark",
                StartWithWindows = false,
                StartMinimized = false,
                StartupDelaySeconds = 20,
                MinimizeToTray = true,
                ShowNotifications = true,
                ConfirmOnShutdown = true,
                ConfirmOnStartup = false,
                AutoStartOnLaunch = true,
                LogDirectory = @"C:\SmartDS\logs",
                EnableLogRotation = false,
                MaxLogSizeMB = 50,
                FrontendHost = "0.0.0.0",
                FrontendPort = 3001,
                BackendHost = "localhost",
                BackendPort = 3000,
                MaxLogLines = 2000,
                Services = new List<ServiceConfig>
                {
                    new ServiceConfig
                    {
                        Id = new Guid("11111111-1111-1111-1111-111111111111"),
                        Name = "pgAdmin 4",
                        Description = "PostgreSQL administration tool",
                        IsEnabled = true,
                        ExecutablePath = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\PostgreSQL 18\pgAdmin 4.lnk",
                        Arguments = string.Empty,
                        WorkingDirectory = string.Empty,
                        UseShellExecute = true,
                        DelayAfterStartSeconds = 10,
                        WaitForPrevious = false,
                        FailurePolicy = FailurePolicy.Continue,
                        MaxRetries = 1,
                        LogFilePath = string.Empty,
                        EnvironmentVariables = new Dictionary<string, string>()
                    },
                    new ServiceConfig
                    {
                        Id = new Guid("22222222-2222-2222-2222-222222222222"),
                        Name = "Node.js Backend",
                        Description = "SmartDS backend API server",
                        IsEnabled = true,
                        ExecutablePath = "node.exe",
                        Arguments = @"dist\server.js",
                        WorkingDirectory = @"C:\SmartDS\backend",
                        UseShellExecute = false,
                        DelayAfterStartSeconds = 15,
                        WaitForPrevious = true,
                        FailurePolicy = FailurePolicy.Stop,
                        MaxRetries = 3,
                        LogFilePath = @"C:\SmartDS\logs\backend.log",
                        EnvironmentVariables = new Dictionary<string, string>
                        {
                            { "NODE_ENV", "production" }
                        },
                        ReadinessCheck = new ReadinessCheckConfig
                        {
                            Type = ReadinessCheckType.TcpPort,
                            Host = "localhost",
                            Port = 3000,
                            TimeoutSeconds = 30,
                            RetryIntervalMs = 1000
                        }
                    },
                    new ServiceConfig
                    {
                        Id = new Guid("33333333-3333-3333-3333-333333333333"),
                        Name = "Next.js Frontend",
                        Description = "SmartDS frontend web application",
                        IsEnabled = true,
                        ExecutablePath = "node.exe",
                        Arguments = @".next\standalone\server.js",
                        WorkingDirectory = @"C:\SmartDS\frontend",
                        UseShellExecute = false,
                        DelayAfterStartSeconds = 0,
                        WaitForPrevious = true,
                        FailurePolicy = FailurePolicy.Continue,
                        MaxRetries = 3,
                        LogFilePath = @"C:\SmartDS\logs\frontend.log",
                        EnvironmentVariables = new Dictionary<string, string>
                        {
                            { "NODE_ENV", "production" },
                            { "PORT", "3001" },
                            { "HOSTNAME", "0.0.0.0" }
                        },
                        ReadinessCheck = new ReadinessCheckConfig
                        {
                            Type = ReadinessCheckType.TcpPort,
                            Host = "localhost",
                            Port = 3001,
                            TimeoutSeconds = 30,
                            RetryIntervalMs = 1000
                        }
                    }
                }
            };
        }

        public void ResetToDefaults()
        {
            Settings = GetDefaultSettings();
        }

        public bool ValidateSettings(AppSettings settings, out string errorMessage)
        {
            if (settings == null)
            {
                errorMessage = "Settings cannot be null";
                return false;
            }
            if (settings.Services == null)
            {
                errorMessage = "Services list cannot be null";
                return false;
            }
            foreach (var svc in settings.Services)
            {
                if (string.IsNullOrWhiteSpace(svc.Name))
                {
                    errorMessage = $"Service has empty name";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(svc.ExecutablePath))
                {
                    errorMessage = $"Service '{svc.Name}' has no executable path";
                    return false;
                }
            }
            errorMessage = string.Empty;
            return true;
        }
    }
}
