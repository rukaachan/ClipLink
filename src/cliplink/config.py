from __future__ import annotations

import json
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Any

from .paths import app_data_root, default_image_root, expand_path


@dataclass(frozen=True)
class ProcessPasteOverride:
    paste_sequence: str | None = None
    restore_delay_ms: int | None = None


@dataclass(frozen=True)
class AppConfig:
    image_root_directory: str = str(default_image_root())
    prompt_template: str = " : {path}"
    retention_hours: int = 1
    paste_hotkey: str = "Alt+V"
    copy_hotkey: str = "Ctrl+Alt+V"
    auto_start_on_login: bool = True
    paste_sequence: str = "Ctrl+Shift+V"
    restore_clipboard_after_paste: bool = True
    restore_delay_ms: int = 250
    process_overrides: dict[str, ProcessPasteOverride] = field(default_factory=dict)

    @property
    def image_root_path(self) -> Path:
        return expand_path(self.image_root_directory)


def config_path(root: Path | None = None) -> Path:
    return (root or app_data_root()) / "config.json"


def default_config() -> AppConfig:
    return AppConfig(image_root_directory=str(default_image_root()))


def load_or_create(root: Path | None = None) -> AppConfig:
    path = config_path(root)
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists():
        config = default_config()
        save_config(config, path)
        return config

    with path.open("r", encoding="utf-8") as handle:
        raw = json.load(handle)
    if not isinstance(raw, dict):
        raise ValueError("config.json must contain a JSON object")
    return parse_config(raw)


def save_config(config: AppConfig, path: Path) -> None:
    def encode(value: Any) -> Any:
        if isinstance(value, ProcessPasteOverride):
            return {k: v for k, v in asdict(value).items() if v is not None}
        if isinstance(value, dict):
            return {k: encode(v) for k, v in value.items()}
        return value

    data = {k: encode(v) for k, v in asdict(config).items()}
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def parse_config(raw: dict[str, Any]) -> AppConfig:
    allowed = set(AppConfig.__dataclass_fields__.keys())
    unknown = sorted(set(raw) - allowed)
    if unknown:
        raise ValueError(f"Unsupported config keys: {', '.join(unknown)}")

    overrides_raw = raw.get("process_overrides", {}) or {}
    if not isinstance(overrides_raw, dict):
        raise ValueError("process_overrides must be an object")

    supported_sequences = {"Ctrl+Shift+V", "Ctrl+V", "Shift+Insert"}
    overrides: dict[str, ProcessPasteOverride] = {}
    for process, value in overrides_raw.items():
        if not isinstance(value, dict):
            raise ValueError(f"process_overrides.{process} must be an object")
        paste_sequence = value.get("paste_sequence")
        if paste_sequence is not None and paste_sequence not in supported_sequences:
            raise ValueError(f"process_overrides.{process}.paste_sequence is unsupported")
        restore_delay_ms = value.get("restore_delay_ms")
        if restore_delay_ms is not None and int(restore_delay_ms) < 0:
            raise ValueError(f"process_overrides.{process}.restore_delay_ms must be greater than or equal to zero")
        overrides[str(process).lower()] = ProcessPasteOverride(
            paste_sequence=paste_sequence,
            restore_delay_ms=None if restore_delay_ms is None else int(restore_delay_ms),
        )

    defaults = default_config()
    config = AppConfig(
        image_root_directory=str(raw.get("image_root_directory") or defaults.image_root_directory),
        prompt_template=str(raw.get("prompt_template") or defaults.prompt_template),
        retention_hours=int(raw.get("retention_hours") or defaults.retention_hours),
        paste_hotkey=str(raw.get("paste_hotkey") or defaults.paste_hotkey),
        copy_hotkey=str(raw.get("copy_hotkey") or defaults.copy_hotkey),
        auto_start_on_login=bool(raw.get("auto_start_on_login", defaults.auto_start_on_login)),
        paste_sequence=str(raw.get("paste_sequence") or defaults.paste_sequence),
        restore_clipboard_after_paste=bool(raw.get("restore_clipboard_after_paste", defaults.restore_clipboard_after_paste)),
        restore_delay_ms=int(raw.get("restore_delay_ms") or defaults.restore_delay_ms),
        process_overrides=overrides,
    )
    if config.retention_hours <= 0:
        raise ValueError("retention_hours must be greater than zero")
    if config.restore_delay_ms < 0:
        raise ValueError("restore_delay_ms must be greater than or equal to zero")
    if config.paste_sequence not in supported_sequences:
        supported = ", ".join(sorted(supported_sequences))
        raise ValueError(f"Unsupported paste_sequence '{config.paste_sequence}'. Supported values: {supported}")
    if "{path}" not in config.prompt_template:
        raise ValueError("prompt_template must contain {path}")
    return config
