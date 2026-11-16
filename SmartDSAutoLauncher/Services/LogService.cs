using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.Services
{
    public class LogService
    {
        private readonly ConcurrentQueue<LogEntry> _buffer = new();
        private int _maxLines = 2000;
        private long _entryCount = 0;

        public event EventHandler<LogEntry>? NewLogEntry;

        public int MaxLines
        {
            get => _maxLines;
            set => _maxLines = Math.Max(100, value);
        }

        public void Append(string message, LogLevel level = LogLevel.Info, string source = "App")
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Source = source,
                Message = message
            };

            _buffer.Enqueue(entry);
            Interlocked.Increment(ref _entryCount);

            // Trim excess entries
            while (_buffer.Count > _maxLines)
                _buffer.TryDequeue(out _);

            NewLogEntry?.Invoke(this, entry);
        }

        public void AppendInfo(string message, string source = "App") =>
            Append(message, LogLevel.Info, source);

        public void AppendWarn(string message, string source = "App") =>
            Append(message, LogLevel.Warn, source);

        public void AppendError(string message, string source = "App") =>
            Append(message, LogLevel.Error, source);

        public void AppendSuccess(string message, string source = "App") =>
            Append(message, LogLevel.Success, source);

        public IEnumerable<LogEntry> GetAll() => _buffer.ToArray();

        public void Clear()
        {
            while (_buffer.TryDequeue(out _)) { }
        }
    }
}
