from __future__ import annotations

import os
import subprocess
import sys
import time
from pathlib import Path

from .process_control import is_worker_running, stop_worker
from .worker import run_worker

USAGE = "Usage: cliplink [start|stop|status|restart]"


def main(argv: list[str] | None = None) -> int:
    args = list(sys.argv[1:] if argv is None else argv)
    if args == ["--worker"]:
        return run_worker()
    if len(args) != 1 or args[0] not in {"start", "stop", "status", "restart"}:
        print(USAGE, file=sys.stderr)
        return 1
    command = args[0]
    if command == "start":
        return start_worker()
    if command == "stop":
        return stop_worker_command()
    if command == "status":
        return status_command()
    if command == "restart":
        stop_worker_command()
        return start_worker()
    return 1


def start_worker() -> int:
    if is_worker_running():
        print("ClipLink worker is already running.")
        return 0
    executable = Path(sys.executable)
    if executable.name.lower().startswith("python"):
        args = [str(executable), "-m", "cliplink.cli", "--worker"]
    else:
        args = [str(executable), "--worker"]
    env = os.environ.copy()
    env["PYINSTALLER_RESET_ENVIRONMENT"] = "1"
    subprocess.Popen(args, cwd=str(executable.parent), creationflags=subprocess.CREATE_NO_WINDOW, env=env)
    deadline = time.monotonic() + 5.0
    while time.monotonic() < deadline:
        if is_worker_running():
            break
        time.sleep(0.05)
    if not is_worker_running():
        print("ClipLink worker failed to stay running. Check %LOCALAPPDATA%\\ClipLink\\logs\\bridge.log.", file=sys.stderr)
        return 1
    print("ClipLink worker started.")
    return 0


def stop_worker_command() -> int:
    if stop_worker():
        print("ClipLink worker stopped.")
    else:
        print("ClipLink worker is not running.")
    return 0


def status_command() -> int:
    print("ClipLink worker is running." if is_worker_running() else "ClipLink worker is stopped.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
