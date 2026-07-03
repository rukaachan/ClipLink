import logging

from cliplink import tray

class FakeUser32:
    def LoadIconW(self, instance, icon):
        return 123

class FakeShell32:
    def __init__(self):
        self.calls = []

    def Shell_NotifyIconW(self, operation, data):
        self.calls.append(operation)
        return 1

def test_tray_icon_adds_and_removes_notification_icon(monkeypatch):
    shell32 = FakeShell32()
    monkeypatch.setattr(tray, "_load_libraries", lambda: (FakeUser32(), shell32, 77))
    icon = tray.TrayIcon(logging.getLogger("test"))

    icon.add(42)
    icon.remove()

    assert shell32.calls == [tray.NIM_ADD, tray.NIM_DELETE]

def test_load_app_icon_falls_back_to_default():
    calls = []

    class User32:
        def LoadIconW(self, instance, icon):
            calls.append((instance, icon.value))
            return 456 if instance is None else 0

    assert tray.load_app_icon(User32(), 77) == 456
    assert calls == [(77, 1), (None, tray.IDI_APPLICATION)]
