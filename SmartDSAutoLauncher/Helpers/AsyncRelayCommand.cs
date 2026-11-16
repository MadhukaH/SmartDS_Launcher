using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartDSAutoLauncher.Helpers
{
    /// <summary>
    /// Async command that exposes <see cref="IsExecuting"/> so buttons can show a loading spinner
    /// for as long as the operation runs.
    /// </summary>
    public class AsyncRelayCommand : ICommand, INotifyPropertyChanged
    {
        private const int MinVisibleMs = 300;

        private readonly Func<object?, Task> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute())
        {
        }

        public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                if (_isExecuting == value) return;
                _isExecuting = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExecuting)));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) =>
            !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

        public async void Execute(object? parameter) => await ExecuteAsync(parameter);

        public async Task ExecuteAsync(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            IsExecuting = true;
            var started = DateTime.UtcNow;
            try
            {
                await _execute(parameter);
                var remaining = MinVisibleMs - (int)(DateTime.UtcNow - started).TotalMilliseconds;
                if (remaining > 0) await Task.Delay(remaining);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}
