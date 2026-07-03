from __future__ import annotations

import ctypes
import os
from pathlib import Path

from .paths import app_data_root

MUTEX_NAME = "Global\\ClipLink"
PID_FILE = "worker.pid"
ERROR_ALREADY_EXISTS = 183


def pid_path(root: Path | None = None) -> Path:
    return (root or app_data_root()) / PID_FILE


class WorkerMutex:
    def __init__(self, name: str = MUTEX_NAME) -> None:
        self._kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
        self.handle = self._kernel32.CreateMutexW(None, True, name)
        if not self.handle:
            raise ctypes.WinError(ctypes.get_last_error())
        self.already_exists = ctypes.get_last_error() == ERROR_ALREADY_EXISTS

    def close(self) -> None:
        if self.handle:
            self._kernel32.CloseHandle(self.handle)
            self.handle = 0

    def __enter__(self) -> "WorkerMutex":
        return self

    def __exit__(self, exc_type, exc, tb) -> None:
        self.close()


def write_pid(root: Path | None = None, pid: int | None = None) -> None:
    path = pid_path(root)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(str(pid or os.getpid()), encoding="utf-8")


def read_pid(root: Path | None = None) -> int | None:
    path = pid_path(root)
    if not path.exists():
        return None
    try:
        return int(path.read_text(encoding="utf-8").strip())
    except ValueError:
        return None


def clear_pid(root: Path | None = None) -> None:
    try:
        pid_path(root).unlink()
    except FileNotFoundError:
        pass


def is_pid_running(pid: int | None) -> bool:
    if not pid or pid <= 0:
        return False
    try:
        import psutil

        return psutil.pid_exists(pid) and psutil.Process(pid).is_running()
    except Exception:
        try:
            os.kill(pid, 0)
            return True
        except OSError:
            return False


def is_worker_running(root: Path | None = None) -> bool:
    return is_pid_running(read_pid(root))


def stop_worker(root: Path | None = None, timeout: float = 5.0) -> bool:
    pid = read_pid(root)
    if not is_pid_running(pid):
        clear_pid(root)
        return False
    import psutil

    process = psutil.Process(pid)
    process.terminate()
    try:
        process.wait(timeout=timeout)
    except psutil.TimeoutExpired:
        process.kill()
        process.wait(timeout=timeout)
    clear_pid(root)
    return True
