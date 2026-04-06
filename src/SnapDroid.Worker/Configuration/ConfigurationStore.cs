using System.Text.Json;
using SnapDroid.Core;

namespace SnapDroid.Worker
{
    internal static class ConfigurationStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static AppRuntimeOptions LoadOrCreate(string appDataRoot, FileLogger logger)
        {
            Directory.CreateDirectory(appDataRoot);

            var configPath = Path.Combine(appDataRoot, "config.json");
            if (!File.Exists(configPath))
            {
                var defaults = CreateDefaults(appDataRoot, configPath);
                File.WriteAllText(configPath, JsonSerializer.Serialize(defaults, JsonOptions));
                logger.Info($"Created default config: {configPath}");
                return defaults;
            }

            var json = File.ReadAllText(configPath);
            var shouldRewriteConfig = IsMissingAutoStartOnLogin(json) || IsMissingRetentionHours(json);
            var config = JsonSerializer.Deserialize<ConfigurationFile>(json) ?? new ConfigurationFile();
            var normalized = Normalize(config, configPath);
            var oldDefaultImagePath = Path.Combine(appDataRoot, "images");
            var previousPicturesDefault = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var newDefaultImagePath = GetDefaultImageRootDirectory();

            if (string.Equals(config.PasteHotkey, "Ctrl+Shift+V", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(config.CopyHotkey, "Ctrl+Alt+Shift+V", StringComparison.OrdinalIgnoreCase))
            {
                normalized = new AppRuntimeOptions
                {
                    ConfigPath = normalized.ConfigPath,
                    ImageRootDirectory = normalized.ImageRootDirectory,
                    PromptTemplate = normalized.PromptTemplate,
                    RetentionHours = normalized.RetentionHours,
                    PasteHotkey = "Alt+V",
                    CopyHotkey = "Ctrl+Alt+V",
                    AutoStartOnLogin = normalized.AutoStartOnLogin
                };
                shouldRewriteConfig = true;
            }

            if (string.IsNullOrWhiteSpace(config.ImageRootDirectory) ||
                string.Equals(config.ImageRootDirectory, oldDefaultImagePath, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(config.ImageRootDirectory, previousPicturesDefault, StringComparison.OrdinalIgnoreCase))
            {
                normalized = new AppRuntimeOptions
                {
                    ConfigPath = normalized.ConfigPath,
                    ImageRootDirectory = newDefaultImagePath,
                    PromptTemplate = normalized.PromptTemplate,
                    RetentionHours = normalized.RetentionHours,
                    PasteHotkey = normalized.PasteHotkey,
                    CopyHotkey = normalized.CopyHotkey,
                    AutoStartOnLogin = normalized.AutoStartOnLogin
                };
                shouldRewriteConfig = true;
            }

            if (shouldRewriteConfig)
            {
                File.WriteAllText(configPath, JsonSerializer.Serialize(normalized, JsonOptions));
                logger.Info($"Updated config defaults: {configPath}");
            }

            return normalized;
        }

        private static AppRuntimeOptions CreateDefaults(string appDataRoot, string configPath)
        {
            return Normalize(new ConfigurationFile
            {
                ImageRootDirectory = GetDefaultImageRootDirectory(),
                PromptTemplate = "analyze this image: {path}",
                RetentionHours = 1,
                PasteHotkey = "Alt+V",
                CopyHotkey = "Ctrl+Alt+V",
                AutoStartOnLogin = true
            }, configPath);
        }

        private static AppRuntimeOptions Normalize(ConfigurationFile file, string configPath)
        {
            return new AppRuntimeOptions
            {
                ConfigPath = configPath,
                ImageRootDirectory = string.IsNullOrWhiteSpace(file.ImageRootDirectory)
                    ? GetDefaultImageRootDirectory()
                    : BridgeConfiguration.ExpandPath(file.ImageRootDirectory),
                PromptTemplate = string.IsNullOrWhiteSpace(file.PromptTemplate)
                    ? "analyze this image: {path}"
                    : file.PromptTemplate,
                RetentionHours = file.RetentionHours > 0
                    ? file.RetentionHours
                    : file.RetentionDays > 0 ? file.RetentionDays : 1,
                PasteHotkey = string.IsNullOrWhiteSpace(file.PasteHotkey) ? "Alt+V" : file.PasteHotkey,
                CopyHotkey = string.IsNullOrWhiteSpace(file.CopyHotkey) ? "Ctrl+Alt+V" : file.CopyHotkey,
                AutoStartOnLogin = file.AutoStartOnLogin
            };
        }

        private sealed class ConfigurationFile
        {
            public string? ImageRootDirectory { get; init; }
            public string? PromptTemplate { get; init; }
            public int RetentionDays { get; init; }
            public int RetentionHours { get; init; }
            public string? PasteHotkey { get; init; }
            public string? CopyHotkey { get; init; }
            public bool AutoStartOnLogin { get; init; } = true;
        }

        private static bool IsMissingAutoStartOnLogin(string json)
        {
            using var document = JsonDocument.Parse(json);
            return !document.RootElement.TryGetProperty(nameof(AppRuntimeOptions.AutoStartOnLogin), out _);
        }

        private static bool IsMissingRetentionHours(string json)
        {
            using var document = JsonDocument.Parse(json);
            return !document.RootElement.TryGetProperty(nameof(AppRuntimeOptions.RetentionHours), out _);
        }

        private static string GetDefaultImageRootDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "SnapDroid");
        }
    }
}

