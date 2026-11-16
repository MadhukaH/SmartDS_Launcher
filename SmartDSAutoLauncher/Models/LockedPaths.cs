using System;

namespace SmartDSAutoLauncher.Models
{
    /// <summary>
    /// Fixed (non-editable) application folders for the SmartDS backend and frontend.
    /// Enforced in the UI, when settings are loaded/saved, and again right before launching,
    /// so editing settings.json by hand cannot change them either.
    /// </summary>
    public static class LockedPaths
    {
        public static readonly Guid BackendId = new("22222222-2222-2222-2222-222222222222");
        public static readonly Guid FrontendId = new("33333333-3333-3333-3333-333333333333");

        public const string BackendDirectory = @"C:\SmartDS\backend";
        public const string FrontendDirectory = @"C:\SmartDS\frontend";

        /// <summary>Returns the locked working directory for a service id, if it has one.</summary>
        public static bool TryGetDirectory(Guid serviceId, out string directory)
        {
            if (serviceId == BackendId) { directory = BackendDirectory; return true; }
            if (serviceId == FrontendId) { directory = FrontendDirectory; return true; }
            directory = string.Empty;
            return false;
        }

        public static bool IsLocked(Guid serviceId) => TryGetDirectory(serviceId, out _);

        /// <summary>Forces the locked working directory onto the config. Returns true if it changed.</summary>
        public static bool Enforce(ServiceConfig config)
        {
            if (!TryGetDirectory(config.Id, out var dir)) return false;
            if (string.Equals(config.WorkingDirectory, dir, StringComparison.Ordinal)) return false;
            config.WorkingDirectory = dir;
            return true;
        }
    }
}
