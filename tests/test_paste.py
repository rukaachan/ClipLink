from cliplink.config import AppConfig, ProcessPasteOverride
from cliplink.paste import resolve_paste_settings, validate_paste_sequence


def test_validate_paste_sequence_rejects_unknown():
    try:
        validate_paste_sequence("Alt+V")
    except ValueError as exc:
        assert "Unsupported paste_sequence" in str(exc)
    else:
        raise AssertionError("expected ValueError")


def test_resolve_paste_settings_uses_process_override():
    config = AppConfig(
        paste_sequence="Ctrl+Shift+V",
        restore_delay_ms=250,
        process_overrides={"notepad": ProcessPasteOverride(paste_sequence="Ctrl+V", restore_delay_ms=500)},
    )

    settings = resolve_paste_settings(config, "notepad")

    assert settings.sequence == "Ctrl+V"
    assert settings.restore_delay_ms == 500
