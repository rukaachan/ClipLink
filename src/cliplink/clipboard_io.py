from __future__ import annotations

import time
import uuid
from contextlib import contextmanager
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Protocol

from PIL import Image, ImageGrab

CLIPBOARD_RETRIES = 8
CLIPBOARD_RETRY_DELAY = 0.025


@dataclass(frozen=True)
class ClipboardSnapshot:
    formats: tuple[tuple[int, object], ...]

    @property
    def is_empty(self) -> bool:
        return len(self.formats) == 0


class ClipboardBackend(Protocol):
    def get_image(self) -> Image.Image | None: ...
    def snapshot(self) -> ClipboardSnapshot: ...
    def restore(self, snapshot: ClipboardSnapshot) -> None: ...
    def set_text(self, value: str) -> None: ...


class PillowClipboardBackend:
    def get_image(self) -> Image.Image | None:
        value = ImageGrab.grabclipboard()
        if isinstance(value, Image.Image):
            return value
        return None

    def snapshot(self) -> ClipboardSnapshot:
        import win32clipboard  # ty: ignore[unresolved-import]

        with open_clipboard(win32clipboard):
            formats: list[tuple[int, object]] = []
            fmt = 0
            while True:
                fmt = win32clipboard.EnumClipboardFormats(fmt)
                if fmt == 0:
                    break
                try:
                    formats.append((fmt, win32clipboard.GetClipboardData(fmt)))
                except (TypeError, win32clipboard.error):
                    continue
            return ClipboardSnapshot(tuple(formats))

    def restore(self, snapshot: ClipboardSnapshot) -> None:
        import win32clipboard  # ty: ignore[unresolved-import]

        with open_clipboard(win32clipboard):
            win32clipboard.EmptyClipboard()
            for fmt, data in snapshot.formats:
                try:
                    win32clipboard.SetClipboardData(fmt, data)
                except (TypeError, win32clipboard.error):
                    continue

    def set_text(self, value: str) -> None:
        import win32clipboard  # ty: ignore[unresolved-import]

        with open_clipboard(win32clipboard):
            win32clipboard.EmptyClipboard()
            win32clipboard.SetClipboardData(win32clipboard.CF_UNICODETEXT, value)


@contextmanager
def open_clipboard(win32clipboard):
    last_error: Exception | None = None
    for _ in range(CLIPBOARD_RETRIES):
        try:
            win32clipboard.OpenClipboard()
            break
        except win32clipboard.error as exc:
            last_error = exc
            time.sleep(CLIPBOARD_RETRY_DELAY)
    else:
        assert last_error is not None
        raise last_error
    try:
        yield
    finally:
        win32clipboard.CloseClipboard()


def save_clipboard_image(backend: ClipboardBackend, directory: Path, now: datetime | None = None) -> Path | None:
    image = backend.get_image()
    if image is None:
        return None

    timestamp = (now or datetime.now()).strftime("%Y%m%d-%H%M%S")
    suffix = uuid.uuid4().hex[:6]
    directory.mkdir(parents=True, exist_ok=True)
    path = directory / f"img-{timestamp}-{suffix}.png"
    image.save(path, format="PNG")
    return path
