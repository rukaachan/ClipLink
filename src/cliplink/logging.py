from __future__ import annotations

import logging
from pathlib import Path

from .paths import app_data_root


def configure_logger(root: Path | None = None) -> logging.Logger:
    log_root = (root or app_data_root()) / "logs"
    log_root.mkdir(parents=True, exist_ok=True)

    logger = logging.getLogger("cliplink")
    logger.setLevel(logging.INFO)
    logger.propagate = False
    if not logger.handlers:
        handler = logging.FileHandler(log_root / "bridge.log", encoding="utf-8")
        handler.setFormatter(logging.Formatter("%(asctime)s [%(levelname)s] %(message)s"))
        logger.addHandler(handler)
    return logger
