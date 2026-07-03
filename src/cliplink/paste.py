from __future__ import annotations

import ctypes
import time
from dataclasses import dataclass

from .config import AppConfig

VK_CONTROL = 0x11
VK_SHIFT = 0x10
VK_V = 0x56
VK_INSERT = 0x2D
KEYEVENTF_KEYUP = 0x0002

SEQUENCES = {
    "Ctrl+Shift+V": [VK_CONTROL, VK_SHIFT, VK_V],
    "Ctrl+V": [VK_CONTROL, VK_V],
    "Shift+Insert": [VK_SHIFT, VK_INSERT],
}

user32 = ctypes.WinDLL("user32", use_last_error=True)


@dataclass(frozen=True)
class PasteSettings:
    sequence: str
    restore_delay_ms: int


def validate_paste_sequence(sequence: str) -> None:
    if sequence not in SEQUENCES:
        supported = ", ".join(sorted(SEQUENCES))
        raise ValueError(f"Unsupported paste_sequence '{sequence}'. Supported values: {supported}")


def resolve_paste_settings(config: AppConfig, process_name: str | None) -> PasteSettings:
    sequence = config.paste_sequence
    restore_delay_ms = config.restore_delay_ms
    if process_name:
        override = config.process_overrides.get(process_name.lower())
        if override:
            sequence = override.paste_sequence or sequence
            restore_delay_ms = override.restore_delay_ms or restore_delay_ms
    validate_paste_sequence(sequence)
    if restore_delay_ms < 0:
        raise ValueError("restore_delay_ms must be greater than or equal to zero")
    return PasteSettings(sequence=sequence, restore_delay_ms=restore_delay_ms)


def send_paste(sequence: str) -> None:
    validate_paste_sequence(sequence)
    keys = SEQUENCES[sequence]
    for key in keys:
        user32.keybd_event(key, 0, 0, 0)
    for key in reversed(keys):
        user32.keybd_event(key, 0, KEYEVENTF_KEYUP, 0)


def paste_text(clipboard, text: str, config: AppConfig, process_name: str | None) -> None:
    snapshot = clipboard.snapshot() if config.restore_clipboard_after_paste else None
    clipboard.set_text(text)
    settings = resolve_paste_settings(config, process_name)
    time.sleep(0.075)
    send_paste(settings.sequence)
    time.sleep(settings.restore_delay_ms / 1000)
    if snapshot is not None:
        clipboard.restore(snapshot)
