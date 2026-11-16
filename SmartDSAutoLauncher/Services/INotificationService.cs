namespace SmartDSAutoLauncher.Services
{
    public interface INotificationService
    {
        void NotifySuccess(string title, string message);
        void NotifyError(string title, string message);
        void NotifyInfo(string title, string message);
    }
}
