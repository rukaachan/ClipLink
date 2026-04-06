namespace SnapDroid.Core
{
    public sealed class PasteInjectionSettings
    {
        public string SendKeysSequence { get; init; } = "+{INSERT}";
        public string TerminalSendKeysSequence { get; init; } = "^+v";
        public int RestoreDelayMilliseconds { get; init; } = 750;
        public int TerminalRestoreDelayMilliseconds { get; init; } = 1500;
        public bool RestoreClipboardAfterPaste { get; init; }

        public static PasteInjectionSettings CreateDefault()
        {
            return new PasteInjectionSettings();
        }

        public string ResolveSequence(string? processName)
        {
            return processName?.ToLowerInvariant() switch
            {
                "alacritty" => TerminalSendKeysSequence,
                "windowsterminal" => TerminalSendKeysSequence,
                _ => SendKeysSequence
            };
        }

        public int ResolveRestoreDelayMilliseconds(string? processName)
        {
            return processName?.ToLowerInvariant() switch
            {
                "alacritty" => TerminalRestoreDelayMilliseconds,
                "windowsterminal" => TerminalRestoreDelayMilliseconds,
                _ => RestoreDelayMilliseconds
            };
        }
    }
}
