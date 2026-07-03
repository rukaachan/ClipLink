from __future__ import annotations

from datetime import datetime, timedelta, timezone
import logging

from .clipboard_io import PillowClipboardBackend, save_clipboard_image
from .config import AppConfig, load_or_create
from .hotkeys import COPY_HOTKEY_ID, PASTE_HOTKEY_ID, hotkey_candidates
from .logging import configure_logger
from .paste import paste_text
from .process_control import WorkerMutex, clear_pid, write_pid
from .prompt import build_prompt
from .retention import delete_expired_files
from .startup import ensure_startup_configured
from .tray import TrayIcon
from .win32_loop import HotkeyMessageLoop


def run_worker() -> int:
    logger = configure_logger()
    with WorkerMutex() as mutex:
        if mutex.already_exists:
            logger.info("Worker launch skipped because another instance is already running.")
            return 0
        write_pid()
        try:
            config = load_or_create()
            ensure_startup_configured(config.auto_start_on_login)
            Worker(config, logger).run()
            return 0
        except Exception:
            logger.exception("Worker startup failed.")
            raise
        finally:
            clear_pid()


class Worker:
    def __init__(self, config: AppConfig, logger: logging.Logger) -> None:
        self.config = config
        self.logger = logger
        self.clipboard = PillowClipboardBackend()
        self.loop = HotkeyMessageLoop(self.handle_hotkey, logger)
        self.tray = TrayIcon(logger)

    def run(self) -> None:
        self.cleanup_expired_files()
        self.loop.create()
        paste = self.loop.register_with_fallback(
            PASTE_HOTKEY_ID,
            hotkey_candidates(PASTE_HOTKEY_ID, self.config.paste_hotkey),
            "paste",
        )
        copy = self.loop.register_with_fallback(
            COPY_HOTKEY_ID,
            hotkey_candidates(COPY_HOTKEY_ID, self.config.copy_hotkey),
            "copy",
        )
        self.logger.info("Worker ready. Paste: %s, Copy: %s", paste, copy)
        self.tray.add(self.loop.hwnd)
        try:
            self.loop.run()
        finally:
            self.tray.remove()
            self.loop.close()

    def handle_hotkey(self, hotkey_id: int) -> None:
        try:
            self.cleanup_expired_files()
            image_path = save_clipboard_image(self.clipboard, self.config.image_root_path)
            if image_path is None:
                self.logger.info("Clipboard does not contain an image.")
                return
            prompt = build_prompt(self.config.prompt_template, str(image_path))
            paste_text(self.clipboard, prompt, self.config, self.get_foreground_process_name())
            self.logger.info("PasteIntoActiveWindow: %s", image_path)
        except Exception:
            self.logger.exception("Clipboard processing failed.")

    def cleanup_expired_files(self) -> None:
        deleted = delete_expired_files(
            self.config.image_root_path,
            datetime.now(timezone.utc),
            timedelta(hours=self.config.retention_hours),
        )
        for path in deleted:
            self.logger.info("Deleted expired image file: %s", path)

    def get_foreground_process_name(self) -> str | None:
        try:
            import ctypes
            import psutil

            user32 = ctypes.WinDLL("user32", use_last_error=True)
            hwnd = user32.GetForegroundWindow()
            pid = ctypes.c_ulong()
            if not hwnd or user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid)) == 0:
                return None
            return psutil.Process(pid.value).name().removesuffix(".exe")
        except Exception:
            return None
