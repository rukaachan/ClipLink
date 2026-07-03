from __future__ import annotations

import argparse
import os
import shutil
import subprocess
from pathlib import Path

APP_NAME = "ClipLink"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--remove-data", action="store_true")
    args = parser.parse_args()

    target_dir = Path.home() / "AppData" / "Local" / "Programs" / "ClipLink"
    exe = target_dir / "cliplink.exe"
    if exe.exists():
        subprocess.call([str(exe), "stop"])
    remove_startup_value()
    remove_from_user_path(target_dir)
    shutil.rmtree(target_dir, ignore_errors=True)
    if args.remove_data:
        shutil.rmtree(Path.home() / "AppData" / "Local" / APP_NAME, ignore_errors=True)
    print("ClipLink uninstalled.")
    return 0


def remove_startup_value() -> None:
    import winreg

    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Microsoft\Windows\CurrentVersion\Run", 0, winreg.KEY_SET_VALUE) as key:
        try:
            winreg.DeleteValue(key, APP_NAME)
        except FileNotFoundError:
            pass


def remove_from_user_path(path: Path) -> None:
    import winreg

    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment", 0, winreg.KEY_READ | winreg.KEY_WRITE) as key:
        try:
            current, kind = winreg.QueryValueEx(key, "Path")
        except FileNotFoundError:
            return
        parts = [p for p in current.split(os.pathsep) if p]
        kept = [p for p in parts if not same_path(p, path)]
        winreg.SetValueEx(key, "Path", 0, kind, os.pathsep.join(kept))


def same_path(left: str, right: Path) -> bool:
    try:
        return Path(left).resolve() == right.resolve()
    except OSError:
        return False


if __name__ == "__main__":
    raise SystemExit(main())
