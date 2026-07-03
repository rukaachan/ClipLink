from __future__ import annotations

import os
from pathlib import Path

APP_NAME = "ClipLink"


def local_app_data() -> Path:
    root = os.environ.get("LOCALAPPDATA")
    if root:
        return Path(root)
    return Path.home() / "AppData" / "Local"


def pictures_dir() -> Path:
    return Path(os.environ.get("USERPROFILE", str(Path.home()))) / "Pictures"


def app_data_root() -> Path:
    return local_app_data() / APP_NAME


def default_image_root() -> Path:
    return pictures_dir() / APP_NAME


def install_root() -> Path:
    return local_app_data() / "Programs" / APP_NAME


def expand_path(value: str) -> Path:
    return Path(os.path.expandvars(value)).expanduser()
