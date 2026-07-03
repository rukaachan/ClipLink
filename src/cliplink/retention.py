from __future__ import annotations

from datetime import datetime, timedelta, timezone
from pathlib import Path


def expired_files(root: Path, now: datetime, retention: timedelta) -> list[Path]:
    if retention.total_seconds() <= 0:
        raise ValueError("retention must be greater than zero")
    if not root.exists() or not root.is_dir():
        return []

    cutoff = now.astimezone(timezone.utc) - retention
    result: list[Path] = []
    for path in root.iterdir():
        if not path.is_file():
            continue
        modified = datetime.fromtimestamp(path.stat().st_mtime, tz=timezone.utc)
        if modified < cutoff:
            result.append(path)
    return sorted(result, key=lambda p: str(p).lower())


def delete_expired_files(root: Path, now: datetime, retention: timedelta) -> list[Path]:
    deleted: list[Path] = []
    for path in expired_files(root, now, retention):
        path.unlink()
        deleted.append(path)
    return deleted
