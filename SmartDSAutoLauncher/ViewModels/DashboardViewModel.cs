using System.Windows;
using SmartDSAutoLauncher.Helpers;
using SmartDSAutoLauncher.Models;
using SmartDSAutoLauncher.Services;

namespace SmartDSAutoLauncher.ViewModels
{
    public class DashboardViewModel : ObservableObject
    {
        private readonly ILaunchOrchestrator _orchestrator;
        private string _currentStepMessage = "Ready";
        private int _countdownSeconds;
        private bool _hasActiveStep;
        private string _currentStepName = string.Empty;
        private int _currentStepNumber;
        private string _currentStepStatus = string.Empty;

        public string CurrentStepMessage { get => _currentStepMessage; set => SetProperty(ref _currentStepMessage, value); }
        public int CountdownSeconds { get => _countdownSeconds; set => SetProperty(ref _countdownSeconds, value); }
        public bool HasActiveStep { get => _hasActiveStep; set => SetProperty(ref _hasActiveStep, value); }
        public string CurrentStepName { get => _currentStepName; set => SetProperty(ref _currentStepName, value); }
        public int CurrentStepNumber { get => _currentStepNumber; set => SetProperty(ref _currentStepNumber, value); }
        public string CurrentStepStatus { get => _currentStepStatus; set => SetProperty(ref _currentStepStatus, value); }

        public DashboardViewModel(ILaunchOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
            _orchestrator.StepChanged += OnStepChanged;
        }

        private void OnStepChanged(object? sender, LaunchStep step)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                CurrentStepMessage = step.Message;
                CountdownSeconds = step.CountdownSecondsRemaining;
                HasActiveStep = step.Status == LaunchStepStatus.Running;
                CurrentStepName = step.ServiceName;
                CurrentStepNumber = step.StepNumber;
                CurrentStepStatus = step.Status.ToString();
            });
        }
    }
}
