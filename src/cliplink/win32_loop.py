from __future__ import annotations

import ctypes
import logging
from ctypes import wintypes
from typing import Callable

from .hotkeys import HotkeyBinding, parse_hotkey

WM_HOTKEY = 0x0312
WM_DESTROY = 0x0002

LRESULT = ctypes.c_ssize_t
WPARAM = wintypes.WPARAM
LPARAM = wintypes.LPARAM

WNDPROC = ctypes.WINFUNCTYPE(LRESULT, wintypes.HWND, wintypes.UINT, WPARAM, LPARAM)
user32 = ctypes.WinDLL("user32", use_last_error=True)
kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)

class WNDCLASS(ctypes.Structure):
    _fields_ = [
        ("style", wintypes.UINT),
        ("lpfnWndProc", WNDPROC),
        ("cbClsExtra", ctypes.c_int),
        ("cbWndExtra", ctypes.c_int),
        ("hInstance", wintypes.HINSTANCE),
        ("hIcon", wintypes.HANDLE),
        ("hCursor", wintypes.HANDLE),
        ("hbrBackground", wintypes.HANDLE),
        ("lpszMenuName", wintypes.LPCWSTR),
        ("lpszClassName", wintypes.LPCWSTR),
    ]


class MSG(ctypes.Structure):
    _fields_ = [
        ("hwnd", wintypes.HWND),
        ("message", wintypes.UINT),
        ("wParam", WPARAM),
        ("lParam", LPARAM),
        ("time", wintypes.DWORD),
        ("pt", wintypes.POINT),
    ]


kernel32.GetModuleHandleW.argtypes = [wintypes.LPCWSTR]
kernel32.GetModuleHandleW.restype = wintypes.HMODULE

user32.RegisterClassW.argtypes = [ctypes.POINTER(WNDCLASS)]
user32.RegisterClassW.restype = wintypes.ATOM
user32.CreateWindowExW.argtypes = [
    wintypes.DWORD,
    wintypes.LPCWSTR,
    wintypes.LPCWSTR,
    wintypes.DWORD,
    ctypes.c_int,
    ctypes.c_int,
    ctypes.c_int,
    ctypes.c_int,
    wintypes.HWND,
    wintypes.HANDLE,
    wintypes.HINSTANCE,
    wintypes.LPVOID,
]
user32.CreateWindowExW.restype = wintypes.HWND
user32.RegisterHotKey.argtypes = [wintypes.HWND, ctypes.c_int, wintypes.UINT, wintypes.UINT]
user32.RegisterHotKey.restype = wintypes.BOOL
user32.UnregisterHotKey.argtypes = [wintypes.HWND, ctypes.c_int]
user32.UnregisterHotKey.restype = wintypes.BOOL
user32.GetMessageW.argtypes = [ctypes.POINTER(MSG), wintypes.HWND, wintypes.UINT, wintypes.UINT]
user32.GetMessageW.restype = wintypes.BOOL
user32.TranslateMessage.argtypes = [ctypes.POINTER(MSG)]
user32.TranslateMessage.restype = wintypes.BOOL
user32.DispatchMessageW.argtypes = [ctypes.POINTER(MSG)]
user32.DispatchMessageW.restype = LRESULT
user32.DestroyWindow.argtypes = [wintypes.HWND]
user32.DestroyWindow.restype = wintypes.BOOL
user32.PostQuitMessage.argtypes = [ctypes.c_int]
user32.PostQuitMessage.restype = None
user32.DefWindowProcW.argtypes = [wintypes.HWND, wintypes.UINT, WPARAM, LPARAM]
user32.DefWindowProcW.restype = LRESULT

class HotkeyMessageLoop:
    def __init__(self, on_hotkey: Callable[[int], None], logger: logging.Logger) -> None:
        self.on_hotkey = on_hotkey
        self.logger = logger
        self.class_name = "ClipLinkHotkeyWindow"
        self.hwnd = None
        self.registered_hotkeys: set[int] = set()
        self._wnd_proc = WNDPROC(self._handle_message)

    def create(self) -> None:
        hinstance = kernel32.GetModuleHandleW(None)
        wndclass = WNDCLASS(
            0,
            self._wnd_proc,
            0,
            0,
            hinstance,
            None,
            None,
            None,
            None,
            self.class_name,
        )
        atom = user32.RegisterClassW(ctypes.byref(wndclass))
        if not atom and ctypes.get_last_error() != 1410:
            raise ctypes.WinError(ctypes.get_last_error())
        self.hwnd = user32.CreateWindowExW(0, self.class_name, "ClipLink", 0, 0, 0, 0, 0, None, None, hinstance, None)
        if not self.hwnd:
            raise ctypes.WinError(ctypes.get_last_error())

    def register_hotkey(self, hotkey_id: int, binding: HotkeyBinding) -> bool:
        if not self.hwnd:
            raise RuntimeError("message loop window not created")
        registered = bool(user32.RegisterHotKey(self.hwnd, hotkey_id, binding.modifiers, binding.key))
        if registered:
            self.registered_hotkeys.add(hotkey_id)
        return registered

    def register_with_fallback(self, hotkey_id: int, candidates: list[str], purpose: str) -> str:
        last_error = None
        for candidate in candidates:
            try:
                binding = parse_hotkey(candidate)
            except ValueError as exc:
                last_error = exc
                continue
            if self.register_hotkey(hotkey_id, binding):
                return candidate
            last_error = ctypes.WinError(ctypes.get_last_error())
        raise RuntimeError(f"Unable to register {purpose} hotkey") from last_error

    def run(self) -> None:
        msg = MSG()
        while user32.GetMessageW(ctypes.byref(msg), None, 0, 0) > 0:
            user32.TranslateMessage(ctypes.byref(msg))
            user32.DispatchMessageW(ctypes.byref(msg))

    def close(self) -> None:
        if self.hwnd:
            for hotkey_id in tuple(self.registered_hotkeys):
                user32.UnregisterHotKey(self.hwnd, hotkey_id)
                self.registered_hotkeys.discard(hotkey_id)
            user32.DestroyWindow(self.hwnd)
            self.hwnd = None

    def _handle_message(self, hwnd, message, wparam, lparam):
        if message == WM_HOTKEY:
            self.on_hotkey(int(wparam))
            return 0
        if message == WM_DESTROY:
            user32.PostQuitMessage(0)
            return 0
        return user32.DefWindowProcW(hwnd, message, wparam, lparam)
