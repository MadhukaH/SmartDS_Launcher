using System.Threading.Tasks;
using SmartDSAutoLauncher.Models;

namespace SmartDSAutoLauncher.Services
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        Task LoadAsync();
        Task SaveAsync();
        AppSettings GetDefaultSettings();
        void ResetToDefaults();
        bool ValidateSettings(AppSettings settings, out string errorMessage);
    }
}
