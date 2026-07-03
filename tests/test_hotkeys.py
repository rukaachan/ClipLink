from cliplink.hotkeys import COPY_HOTKEY_ID, MOD_ALT, MOD_CONTROL, MOD_SHIFT, PASTE_HOTKEY_ID, hotkey_candidates, parse_hotkey


def test_parse_hotkey_modifiers_and_key():
    binding = parse_hotkey("Ctrl+Alt+V")

    assert binding.modifiers == MOD_CONTROL | MOD_ALT
    assert binding.key == ord("V")


def test_parse_ctrl_alias():
    binding = parse_hotkey("Control+Shift+F10")

    assert binding.modifiers == MOD_CONTROL | MOD_SHIFT
    assert binding.key == 0x79


def test_paste_hotkey_fallbacks_skip_duplicates():
    assert hotkey_candidates(PASTE_HOTKEY_ID, "Alt+V") == ["Alt+V", "Alt+Shift+V", "Alt+F10"]


def test_copy_hotkey_fallbacks():
    assert hotkey_candidates(COPY_HOTKEY_ID, "Ctrl+Alt+V") == ["Ctrl+Alt+V", "Ctrl+Shift+F10", "Ctrl+Alt+F10"]
