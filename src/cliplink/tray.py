from __future__ import annotations

import ctypes
import logging
from ctypes import wintypes

NIM_ADD = 0x00000000
NIM_DELETE = 0x00000002
NIF_ICON = 0x00000002
NIF_TIP = 0x00000004
IDI_APPLICATION = 32512

class NOTIFYICONDATA(ctypes.Structure):
    _fields_ = [
        ("cbSize", wintypes.DWORD),
        ("hWnd", wintypes.HWND),
        ("uID", wintypes.UINT),
        ("uFlags", wintypes.UINT),
        ("uCallbackMessage", wintypes.UINT),
        ("hIcon", wintypes.HANDLE),
        ("szTip", wintypes.WCHAR * 128),
        ("dwState", wintypes.DWORD),
        ("dwStateMask", wintypes.DWORD),
        ("szInfo", wintypes.WCHAR * 256),
        ("uTimeoutOrVersion", wintypes.UINT),
        ("szInfoTitle", wintypes.WCHAR * 64),
        ("dwInfoFlags", wintypes.DWORD),
        ("guidItem", ctypes.c_byte * 16),
        ("hBalloonIcon", wintypes.HANDLE),
    ]

class TrayIcon:
    def __init__(self, logger: logging.Logger, tooltip: str = "ClipLink") -> None:
        self.logger = logger
        self.tooltip = tooltip
        self._data: NOTIFYICONDATA | None = None
        self._shell32 = None

    def add(self, hwnd: int) -> None:
        try:
            user32, shell32, hinstance = _load_libraries()
            data = NOTIFYICONDATA()
            data.cbSize = ctypes.sizeof(NOTIFYICONDATA)
            data.hWnd = hwnd
            data.uID = 1
            data.uFlags = NIF_ICON | NIF_TIP
            data.hIcon = load_app_icon(user32, hinstance)
            data.szTip = self.tooltip[:127]
            if not shell32.Shell_NotifyIconW(NIM_ADD, ctypes.cast(ctypes.pointer(data), ctypes.c_void_p)):
                raise ctypes.WinError(ctypes.get_last_error())
            self._data = data
            self._shell32 = shell32
        except Exception:
            self.logger.warning("Tray icon unavailable.", exc_info=True)

    def remove(self) -> None:
        if self._data is None or self._shell32 is None:
            return
        self._shell32.Shell_NotifyIconW(NIM_DELETE, ctypes.cast(ctypes.pointer(self._data), ctypes.c_void_p))
        self._data = None
        self._shell32 = None

def _load_libraries():
    user32 = ctypes.WinDLL("user32", use_last_error=True)
    shell32 = ctypes.WinDLL("shell32", use_last_error=True)
    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel32.GetModuleHandleW.argtypes = [wintypes.LPCWSTR]
    kernel32.GetModuleHandleW.restype = wintypes.HMODULE
    user32.LoadIconW.argtypes = [wintypes.HINSTANCE, ctypes.c_void_p]
    user32.LoadIconW.restype = wintypes.HANDLE
    shell32.Shell_NotifyIconW.argtypes = [wintypes.DWORD, ctypes.c_void_p]
    shell32.Shell_NotifyIconW.restype = wintypes.BOOL
    return user32, shell32, kernel32.GetModuleHandleW(None)

def load_app_icon(user32, hinstance):
    return user32.LoadIconW(hinstance, ctypes.c_void_p(1)) or user32.LoadIconW(None, ctypes.c_void_p(IDI_APPLICATION))
