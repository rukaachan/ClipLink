namespace ClipLink.Worker
{
    public sealed class AppRuntimeOptions
    {
        public string ConfigPath { get; init; } = string.Empty;
        public string ImageRootDirectory { get; init; } = string.Empty;
        public string PromptTemplate { get; init; } = " : {path}";
        public int RetentionHours { get; init; } = 1;
        public string PasteHotkey { get; init; } = "Alt+V";
        public string CopyHotkey { get; init; } = "Ctrl+Alt+V";
        public bool AutoStartOnLogin { get; init; } = true;
    }
}

