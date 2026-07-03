from __future__ import annotations

from dataclasses import dataclass

MOD_ALT = 0x0001
MOD_CONTROL = 0x0002
MOD_SHIFT = 0x0004
MOD_WIN = 0x0008

KEY_ALIASES = {
    "CTRL": "CONTROL",
    "WINDOWS": "WIN",
    "ESC": "ESCAPE",
}

VK_CODES = {
    **{chr(code): code for code in range(ord("A"), ord("Z") + 1)},
    **{str(i): ord(str(i)) for i in range(10)},
    "F1": 0x70,
    "F2": 0x71,
    "F3": 0x72,
    "F4": 0x73,
    "F5": 0x74,
    "F6": 0x75,
    "F7": 0x76,
    "F8": 0x77,
    "F9": 0x78,
    "F10": 0x79,
    "F11": 0x7A,
    "F12": 0x7B,
    "INSERT": 0x2D,
    "DELETE": 0x2E,
    "HOME": 0x24,
    "END": 0x23,
    "PAGEUP": 0x21,
    "PAGEDOWN": 0x22,
    "SPACE": 0x20,
    "ESCAPE": 0x1B,
}

MODIFIERS = {
    "ALT": MOD_ALT,
    "CONTROL": MOD_CONTROL,
    "SHIFT": MOD_SHIFT,
    "WIN": MOD_WIN,
}

PASTE_HOTKEY_ID = 1001
COPY_HOTKEY_ID = 1002


@dataclass(frozen=True)
class HotkeyBinding:
    key: int
    modifiers: int


def normalize_token(token: str) -> str:
    value = token.strip().upper()
    return KEY_ALIASES.get(value, value)


def parse_hotkey(value: str) -> HotkeyBinding:
    if not value or not value.strip():
        raise ValueError("Hotkey value is empty")
    parts = [normalize_token(part) for part in value.split("+") if part.strip()]
    if len(parts) < 2:
        raise ValueError(f"Hotkey '{value}' must include modifiers and a key")

    modifiers = 0
    for part in parts[:-1]:
        if part not in MODIFIERS:
            raise ValueError(f"Unsupported hotkey modifier '{part}'")
        modifiers |= MODIFIERS[part]

    key_name = parts[-1]
    if key_name not in VK_CODES:
        raise ValueError(f"Unsupported hotkey key '{key_name}'")
    return HotkeyBinding(key=VK_CODES[key_name], modifiers=modifiers)


def hotkey_candidates(hotkey_id: int, configured: str) -> list[str]:
    candidates: list[str] = []

    def add(value: str) -> None:
        if value and not any(value.lower() == existing.lower() for existing in candidates):
            candidates.append(value)

    add(configured)
    if hotkey_id == PASTE_HOTKEY_ID:
        add("Alt+V")
        add("Alt+Shift+V")
        add("Alt+F10")
    elif hotkey_id == COPY_HOTKEY_ID:
        add("Ctrl+Alt+V")
        add("Ctrl+Shift+F10")
        add("Ctrl+Alt+F10")
    return candidates
