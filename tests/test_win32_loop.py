import ctypes
import logging

from cliplink import win32_loop


def test_window_proc_lparam_is_signed_pointer_sized():
    pointer_bits = ctypes.sizeof(ctypes.c_void_p) * 8
    max_signed = (1 << (pointer_bits - 1)) - 1
    min_signed = -(1 << (pointer_bits - 1))

    assert win32_loop.LPARAM(max_signed).value == max_signed
    assert win32_loop.LPARAM(min_signed).value == min_signed


def test_window_proc_handles_many_pointer_sized_lparam_values(monkeypatch):
    calls = []

    def fake_def_window_proc(hwnd, message, wparam, lparam):
        calls.append((hwnd, message, wparam, lparam))
        return 0

    monkeypatch.setattr(win32_loop.user32, "DefWindowProcW", fake_def_window_proc)
    loop = win32_loop.HotkeyMessageLoop(lambda _: None, logging.getLogger("test"))
    pointer_bits = ctypes.sizeof(ctypes.c_void_p) * 8
    max_signed = (1 << (pointer_bits - 1)) - 1
    min_signed = -(1 << (pointer_bits - 1))
    values = [*range(0, 4096)]
    values.extend(max_signed - offset for offset in range(0, 4096))
    values.extend(min_signed + offset for offset in range(0, 4096))

    for lparam in values:
        assert loop._handle_message(1, 0x0400, 0, lparam) == 0

    assert len(calls) == 12288
    assert calls[-1] == (1, 0x0400, 0, min_signed + 4095)
