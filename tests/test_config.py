from cliplink.config import parse_config


def test_parse_snake_case_defaults():
    config = parse_config({})

    assert config.prompt_template == " : {path}"
    assert config.retention_hours == 1
    assert config.paste_hotkey == "Alt+V"
    assert config.copy_hotkey == "Ctrl+Alt+V"
    assert config.paste_sequence == "Ctrl+Shift+V"


def test_parse_rejects_legacy_pascal_case():
    try:
        parse_config({"ImageRootDirectory": "C:/tmp"})
    except ValueError as exc:
        assert "Unsupported config keys" in str(exc)
    else:
        raise AssertionError("expected ValueError")


def test_parse_process_override():
    config = parse_config({"process_overrides": {"notepad": {"paste_sequence": "Ctrl+V", "restore_delay_ms": 500}}})

    assert config.process_overrides["notepad"].paste_sequence == "Ctrl+V"
    assert config.process_overrides["notepad"].restore_delay_ms == 500


def test_parse_rejects_unsupported_paste_sequence():
    try:
        parse_config({"paste_sequence": "Alt+V"})
    except ValueError as exc:
        assert "Unsupported paste_sequence" in str(exc)
    else:
        raise AssertionError("expected ValueError")


def test_parse_rejects_negative_restore_delay():
    try:
        parse_config({"restore_delay_ms": -1})
    except ValueError as exc:
        assert "restore_delay_ms" in str(exc)
    else:
        raise AssertionError("expected ValueError")
