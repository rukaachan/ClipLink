from cliplink.clipboard_io import ClipboardSnapshot
from cliplink.config import AppConfig
from cliplink.paste import paste_text


class FakeClipboard:
    def __init__(self):
        self.snapshot_value = ClipboardSnapshot(((13, "original"), (1, b"bytes")))
        self.restored = None
        self.text_values = []

    def snapshot(self):
        return self.snapshot_value

    def restore(self, snapshot):
        self.restored = snapshot

    def set_text(self, value):
        self.text_values.append(value)


def test_paste_text_restores_full_snapshot(monkeypatch):
    clipboard = FakeClipboard()
    monkeypatch.setattr("cliplink.paste.send_paste", lambda sequence: None)
    monkeypatch.setattr("cliplink.paste.time.sleep", lambda seconds: None)

    paste_text(clipboard, "prompt", AppConfig(), None)

    assert clipboard.text_values == ["prompt"]
    assert clipboard.restored == clipboard.snapshot_value
