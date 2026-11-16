using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace SmartDSAutoLauncher.Services
{
    public class StartupRegistrationService : IStartupRegistrationService
    {
        private const string RegKeyName = "SmartDSAutoLauncher";
        private const string RegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private readonly ILogger<StartupRegistrationService> _logger;

        public bool IsRegistered { get; private set; }

        public StartupRegistrationService(ILogger<StartupRegistrationService> logger)
        {
            _logger = logger;
            Refresh();
        }

        public void Register(int delaySeconds = 0)
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName
                    ?? Assembly.GetExecutingAssembly().Location;
                var args = "--minimized";
                if (delaySeconds > 0) args += $" --startup-delay {delaySeconds}";
                var value = $"\"{exePath}\" {args}";

                using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
                key?.SetValue(RegKeyName, value);
                IsRegistered = true;
                _logger.LogInformation("Startup registration added: {Value}", value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register startup");
                throw;
            }
        }

        public void Unregister()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
                if (key?.GetValue(RegKeyName) != null)
                {
                    key.DeleteValue(RegKeyName);
                    _logger.LogInformation("Startup registration removed");
                }
                IsRegistered = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister startup");
            }
        }

        public void Refresh()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegPath);
                IsRegistered = key?.GetValue(RegKeyName) != null;
            }
            catch { IsRegistered = false; }
        }
    }
}
