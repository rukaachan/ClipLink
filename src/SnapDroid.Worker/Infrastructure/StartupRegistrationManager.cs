using SnapDroid.Core;
using Microsoft.Win32;

namespace SnapDroid.Worker
{
    internal static class StartupRegistrationManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SnapDroid";

        public static void EnsureConfigured(bool enable, string executablePath)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? throw new InvalidOperationException("Unable to open the Windows Run registry key.");

            if (enable)
            {
                key.SetValue(AppName, StartupRegistrationFormatter.BuildRunCommand(executablePath), RegistryValueKind.String);
                return;
            }

            if (key.GetValue(AppName) is not null)
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
    }
}

