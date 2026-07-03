from __future__ import annotations

import os
import shutil
import subprocess
import sys
from pathlib import Path



def main() -> int:
    repo = Path(__file__).resolve().parents[1]
    dist = repo / "dist" / "ClipLink"
    target_dir = Path.home() / "AppData" / "Local" / "Programs" / "ClipLink"

    subprocess.check_call(build_pyinstaller_command(repo, dist), cwd=repo)

    target_dir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(dist / "cliplink.exe", target_dir / "cliplink.exe")
    add_to_user_path(target_dir)
    subprocess.Popen([str(target_dir / "cliplink.exe"), "restart"], cwd=target_dir, creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0)
    print(f"Installed ClipLink to {target_dir}")
    return 0

def build_pyinstaller_command(repo: Path, dist: Path) -> list[str]:
    return ["uv", "run", "pyinstaller", "--onefile", "--noconsole", "--icon", str(repo / "assets" / "cliplink.ico"), "--name", "cliplink", "--distpath", str(dist), "--workpath", str(repo / "build"), "--specpath", str(repo / "build"), str(repo / "cliplink_launcher.py")]


def add_to_user_path(path: Path) -> None:
    import winreg

    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, "Environment", 0, winreg.KEY_READ | winreg.KEY_WRITE) as key:
        current, _ = winreg.QueryValueEx(key, "Path") if value_exists(key, "Path") else ("", winreg.REG_EXPAND_SZ)
        parts = [p for p in current.split(os.pathsep) if p]
        if not any(Path(p).resolve() == path.resolve() for p in parts if p):
            parts.append(str(path))
            winreg.SetValueEx(key, "Path", 0, winreg.REG_EXPAND_SZ, os.pathsep.join(parts))


def value_exists(key, name: str) -> bool:
    import winreg

    try:
        winreg.QueryValueEx(key, name)
        return True
    except FileNotFoundError:
        return False


if __name__ == "__main__":
    raise SystemExit(main())
