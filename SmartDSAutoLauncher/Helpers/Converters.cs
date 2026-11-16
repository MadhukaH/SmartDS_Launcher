using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.Helpers
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var invert = parameter?.ToString()?.ToLower() == "invert";
            var boolVal = value is bool b && b;
            if (invert) boolVal = !boolVal;
            return boolVal ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Visibility v && v == Visibility.Visible;
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : false;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : false;
    }

    public class ServiceStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ServiceStatus status)
            {
                return status switch
                {
                    ServiceStatus.Running => new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71)),
                    ServiceStatus.Starting => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)),
                    ServiceStatus.Stopping => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)),
                    ServiceStatus.Failed => new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)),
                    ServiceStatus.Disabled => new SolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0x8D)),
                    _ => new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6))
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class ServiceStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ServiceStatus status)
            {
                return status switch
                {
                    ServiceStatus.Running => "Running",
                    ServiceStatus.Starting => "Starting...",
                    ServiceStatus.Stopping => "Stopping...",
                    ServiceStatus.Failed => "Failed",
                    ServiceStatus.Disabled => "Disabled",
                    _ => "Stopped"
                };
            }
            return "Unknown";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var invert = parameter?.ToString()?.ToLower() == "invert";
            var isNull = value == null || (value is string s && string.IsNullOrEmpty(s));
            if (invert) isNull = !isNull;
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value?.Equals(parameter) ?? false;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            (value is bool b && b) ? parameter : Binding.DoNothing;
    }

    public class PageToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null && parameter != null)
            {
                return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    public class LogLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Error => new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                    LogLevel.Warn => new SolidColorBrush(Color.FromRgb(0xFF, 0xD9, 0x3D)),
                    LogLevel.Success => new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71)),
                    LogLevel.Debug => new SolidColorBrush(Color.FromRgb(0x74, 0xB9, 0xFF)),
                    _ => new SolidColorBrush(Color.FromRgb(0xEC, 0xF0, 0xF1))
                };
            }
            return new SolidColorBrush(Colors.White);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
