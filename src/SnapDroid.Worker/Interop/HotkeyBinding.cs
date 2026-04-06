namespace SnapDroid.Worker
{
    internal readonly record struct HotkeyBinding(Keys Key, KeyModifiers Modifiers)
    {
        public static HotkeyBinding Parse(string hotkey)
        {
            if (string.IsNullOrWhiteSpace(hotkey))
            {
                throw new InvalidOperationException("Hotkey value is empty.");
            }

            var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                throw new InvalidOperationException($"Hotkey '{hotkey}' must include modifiers and a key.");
            }

            KeyModifiers modifiers = 0;
            for (var i = 0; i < parts.Length - 1; i++)
            {
                modifiers |= parts[i].ToUpperInvariant() switch
                {
                    "ALT" => KeyModifiers.Alt,
                    "CTRL" or "CONTROL" => KeyModifiers.Control,
                    "SHIFT" => KeyModifiers.Shift,
                    "WIN" or "WINDOWS" => KeyModifiers.Win,
                    _ => throw new InvalidOperationException($"Unsupported hotkey modifier '{parts[i]}'.")
                };
            }

            if (!Enum.TryParse<Keys>(parts[^1], ignoreCase: true, out var key))
            {
                throw new InvalidOperationException($"Unsupported hotkey key '{parts[^1]}'.");
            }

            return new HotkeyBinding(key, modifiers);
        }
    }

    [Flags]
    internal enum KeyModifiers : uint
    {
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    internal enum HotkeyAction
    {
        PasteIntoActiveWindow,
        CopyPromptToClipboard
    }
}

