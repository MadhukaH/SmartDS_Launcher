using System;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace SmartDSAutoLauncher.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public void NotifySuccess(string title, string message)
        {
            ShowBalloon(title, message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }

        public void NotifyError(string title, string message)
        {
            ShowBalloon(title, message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
        }

        public void NotifyInfo(string title, string message)
        {
            ShowBalloon(title, message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }

        private void ShowBalloon(string title, string message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon icon)
        {
            try
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (Application.Current is App app && app.TrayIcon != null)
                        app.TrayIcon.ShowBalloonTip(title, message, icon);
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to show notification");
            }
        }
    }
}
