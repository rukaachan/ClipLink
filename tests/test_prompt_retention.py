from datetime import datetime, timedelta, timezone
import os

from cliplink.prompt import build_prompt
from cliplink.retention import expired_files


def test_build_prompt_replaces_path():
    assert build_prompt(" : {path}", "C:/img.png") == " : C:/img.png"


def test_build_prompt_requires_placeholder():
    try:
        build_prompt("no path", "C:/img.png")
    except ValueError as exc:
        assert "{path}" in str(exc)
    else:
        raise AssertionError("expected ValueError")


def test_expired_files_sorted(tmp_path):
    old_b = tmp_path / "b.png"
    old_a = tmp_path / "a.png"
    fresh = tmp_path / "fresh.png"
    for path in [old_b, old_a, fresh]:
        path.write_text("x", encoding="utf-8")

    now = datetime(2026, 5, 17, tzinfo=timezone.utc)
    old_ts = (now - timedelta(hours=2)).timestamp()
    fresh_ts = (now - timedelta(minutes=10)).timestamp()
    os.utime(old_b, (old_ts, old_ts))
    os.utime(old_a, (old_ts, old_ts))
    os.utime(fresh, (fresh_ts, fresh_ts))

    assert expired_files(tmp_path, now, timedelta(hours=1)) == [old_a, old_b]
