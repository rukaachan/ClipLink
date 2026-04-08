using ClipLink.Core;

namespace ClipLink.Worker
{
    internal sealed class BackgroundWorkerContext : ApplicationContext
    {
        private readonly FileLogger logger;
        private readonly BridgeConfiguration configuration;
        private readonly AppRuntimeOptions options;
        private readonly HotkeyWindow hotkeyWindow;
        private readonly PasteInjectionSettings pasteInjectionSettings;
        private string activePasteHotkey = string.Empty;
        private string activeCopyHotkey = string.Empty;

        public BackgroundWorkerContext()
        {
            var appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClipLink");

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
            logger.Info($"Worker ready. Paste: {activePasteHotkey}, Copy: {activeCopyHotkey}");
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
            activePasteHotkey = RegisterHotkeyWithFallback(
                HotkeyWindow.PasteHotkeyId,
                options.PasteHotkey,
                "paste");
            activeCopyHotkey = RegisterHotkeyWithFallback(
                HotkeyWindow.CopyHotkeyId,
                options.CopyHotkey,
                "copy");
        }

        private void ConfigureStartupOnLogin()
        {
            var executablePath = Application.ExecutablePath;
            StartupRegistrationManager.EnsureConfigured(options.AutoStartOnLogin, executablePath);
            logger.Info($"Startup on login {(options.AutoStartOnLogin ? "enabled" : "disabled")}: {executablePath}");
        }

        private string RegisterHotkeyWithFallback(int id, string configuredHotkey, string purpose)
        {
            Exception? lastFailure = null;

            foreach (var candidate in ResolveHotkeyCandidates(id, configuredHotkey))
            {
                try
                {
                    var binding = HotkeyBinding.Parse(candidate);
                    if (!hotkeyWindow.RegisterHotkey(id, binding))
                    {
                        lastFailure = new InvalidOperationException($"Unable to register hotkey '{candidate}'.");
                        continue;
                    }

                    if (!string.Equals(candidate, configuredHotkey, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Info($"Configured {purpose} hotkey '{configuredHotkey}' unavailable. Using fallback '{candidate}'.");
                    }

                    return candidate;
                }
                catch (InvalidOperationException ex)
                {
                    lastFailure = ex;
                }
            }

            throw new InvalidOperationException(
                $"Unable to register {purpose} hotkey. Configured '{configuredHotkey}' and all fallbacks were unavailable.",
                lastFailure);
        }

        internal static IReadOnlyList<string> ResolveHotkeyCandidates(int id, string configuredHotkey)
        {
            var candidates = new List<string>();
            AddCandidate(configuredHotkey);

            switch (id)
            {
                case HotkeyWindow.PasteHotkeyId:
                    AddCandidate("Alt+V");
                    AddCandidate("Alt+Shift+V");
                    AddCandidate("Alt+F10");
                    break;
                case HotkeyWindow.CopyHotkeyId:
                    AddCandidate("Ctrl+Alt+V");
                    AddCandidate("Ctrl+Shift+F10");
                    AddCandidate("Ctrl+Alt+F10");
                    break;
            }

            return candidates;

            void AddCandidate(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                if (candidates.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                candidates.Add(value);
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

