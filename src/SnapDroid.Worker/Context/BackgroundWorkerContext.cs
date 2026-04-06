using SnapDroid.Core;

namespace SnapDroid.Worker
{
    internal sealed class BackgroundWorkerContext : ApplicationContext
    {
        private readonly FileLogger logger;
        private readonly BridgeConfiguration configuration;
        private readonly AppRuntimeOptions options;
        private readonly HotkeyWindow hotkeyWindow;
        private readonly PasteInjectionSettings pasteInjectionSettings;

        public BackgroundWorkerContext()
        {
            var appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SnapDroid");

            logger = new FileLogger(Path.Combine(appDataRoot, "logs"));
            options = ConfigurationStore.LoadOrCreate(appDataRoot, logger);
            configuration = new BridgeConfiguration
            {
                ImageRootDirectory = options.ImageRootDirectory,
                PromptTemplate = options.PromptTemplate
            };
            pasteInjectionSettings = PasteInjectionSettings.CreateDefault();

            hotkeyWindow = new HotkeyWindow();
            hotkeyWindow.HotkeyPressed += HandleHotkey;

            CleanupExpiredFiles();
            ConfigureStartupOnLogin();
            RegisterConfiguredHotkeys();
            logger.Info($"Worker ready. Paste: {options.PasteHotkey}, Copy: {options.CopyHotkey}");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hotkeyWindow.Dispose();
            }

            base.Dispose(disposing);
        }

        private void RegisterConfiguredHotkeys()
        {
            RegisterHotkey(HotkeyWindow.PasteHotkeyId, options.PasteHotkey);
            RegisterHotkey(HotkeyWindow.CopyHotkeyId, options.CopyHotkey);
        }

        private void ConfigureStartupOnLogin()
        {
            var executablePath = Application.ExecutablePath;
            StartupRegistrationManager.EnsureConfigured(options.AutoStartOnLogin, executablePath);
            logger.Info($"Startup on login {(options.AutoStartOnLogin ? "enabled" : "disabled")}: {executablePath}");
        }

        private void RegisterHotkey(int id, string hotkey)
        {
            var binding = HotkeyBinding.Parse(hotkey);
            if (!hotkeyWindow.RegisterHotkey(id, binding))
            {
                throw new InvalidOperationException($"Unable to register hotkey '{hotkey}'.");
            }
        }

        private void HandleHotkey(int hotkeyId)
        {
            ProcessClipboard(ResolveHotkeyAction(hotkeyId));
        }

        internal static HotkeyAction ResolveHotkeyAction(int hotkeyId)
        {
            return hotkeyId switch
            {
                HotkeyWindow.PasteHotkeyId => HotkeyAction.CopyPromptToClipboard,
                HotkeyWindow.CopyHotkeyId => HotkeyAction.CopyPromptToClipboard,
                _ => HotkeyAction.CopyPromptToClipboard
            };
        }

        private void ProcessClipboard(HotkeyAction action)
        {
            try
            {
                CleanupExpiredFiles();

                if (!Clipboard.ContainsImage())
                {
                    logger.Info("Clipboard does not contain an image.");
                    return;
                }

                var targetWindow = NativeMethods.GetForegroundWindow();
                var targetProcessName = GetProcessName(targetWindow);
                var imagePath = SaveClipboardImage();
                var prompt = configuration.BuildPrompt(imagePath);

                switch (action)
                {
                    case HotkeyAction.PasteIntoActiveWindow:
                        ClipboardInjector.PastePrompt(prompt, targetWindow, targetProcessName, pasteInjectionSettings, logger);
                        break;
                    case HotkeyAction.CopyPromptToClipboard:
                        Clipboard.SetText(prompt);
                        break;
                }

                logger.Info($"{action}: {imagePath}");
            }
            catch (Exception ex)
            {
                logger.Error("Clipboard processing failed.", ex);
            }
        }

        private string SaveClipboardImage()
        {
            var directory = configuration.ResolveStorageDirectory(DateTime.Now);
            Directory.CreateDirectory(directory);

            using var image = Clipboard.GetImage() ?? throw new InvalidOperationException("Clipboard image could not be read.");
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var fileName = $"img-{DateTime.Now:yyyyMMdd-HHmmss}-{suffix}.png";
            var fullPath = Path.Combine(directory, fileName);
            image.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
            return fullPath;
        }

        private void CleanupExpiredFiles()
        {
            foreach (var path in FileRetentionService.GetExpiredFiles(
                         configuration.ImageRootDirectory,
                         DateTime.UtcNow,
                         TimeSpan.FromHours(options.RetentionHours)))
            {
                try
                {
                    File.Delete(path);
                    logger.Info($"Deleted expired image file: {path}");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to delete expired image file '{path}'.", ex);
                }
            }
        }

        private static string? GetProcessName(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return null;
            }

            var result = NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);
            if (result == 0 || processId == 0)
            {
                return null;
            }

            try
            {
                return System.Diagnostics.Process.GetProcessById((int)processId).ProcessName;
            }
            catch
            {
                return null;
            }
        }
    }
}

