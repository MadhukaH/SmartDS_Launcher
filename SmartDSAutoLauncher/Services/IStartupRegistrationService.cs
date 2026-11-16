namespace SmartDSAutoLauncher.Services
{
    public interface IStartupRegistrationService
    {
        bool IsRegistered { get; }
        void Register(int delaySeconds = 0);
        void Unregister();
        void Refresh();
    }
}
