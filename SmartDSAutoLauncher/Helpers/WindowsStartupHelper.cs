using System;
using System.IO;

namespace SmartDSAutoLauncher.Helpers
{
    public static class WindowsStartupHelper
    {
        public static bool IsCommandLineArgPresent(string[] args, string arg) =>
            Array.Exists(args, a => a.Equals(arg, StringComparison.OrdinalIgnoreCase));

        public static int GetStartupDelay(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals("--startup-delay", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(args[i + 1], out var delay))
                    return delay;
            }
            return 0;
        }

        public static bool IsNodeExeAvailable()
        {
            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
            foreach (var path in paths)
            {
                if (File.Exists(Path.Combine(path, "node.exe")))
                    return true;
            }
            return false;
        }

        public static string GetLocalAppDataPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SmartDSAutoLauncher");
        }
    }
}
