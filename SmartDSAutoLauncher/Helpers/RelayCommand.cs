using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace SmartDSAutoLauncher.Helpers
{
    /// <summary>
    /// Command that exposes <see cref="IsExecuting"/> so buttons can show a loading spinner.
    /// The action runs after the UI has had a chance to render the loading state,
    /// and the state stays visible for a short minimum time so the feedback is noticeable.
    /// </summary>
    public class RelayCommand : ICommand, INotifyPropertyChanged
    {
        private const int MinVisibleMs = 300;

        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private bool _isExecuting;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute())
        {
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

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            IsExecuting = true;
            var started = DateTime.UtcNow;
            try
            {
                // Let WPF render the loading state before running (possibly modal) work
                await Dispatcher.Yield(DispatcherPriority.Background);
                _execute(parameter);

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
