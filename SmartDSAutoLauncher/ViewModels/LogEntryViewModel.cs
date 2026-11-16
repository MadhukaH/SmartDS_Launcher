using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.ViewModels
{
    public class LogEntryViewModel
    {
        private readonly LogEntry _entry;
        public LogEntryViewModel(LogEntry entry) => _entry = entry;
        public string Timestamp => _entry.Timestamp.ToString("HH:mm:ss.fff");
        public string Level => _entry.Level.ToString().ToUpper();
        public LogLevel LogLevel => _entry.Level;
        public string Source => _entry.Source;
        public string Message => _entry.Message;
        public string Formatted => _entry.Formatted;
    }
}
