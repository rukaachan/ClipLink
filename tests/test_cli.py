from cliplink import cli


def test_start_worker_polls_until_running(monkeypatch):
    popen_kwargs = {}
    states = iter([False, False, True, True])
    monkeypatch.setattr(cli, "is_worker_running", lambda: next(states))
    monkeypatch.setattr(cli.subprocess, "Popen", lambda *args, **kwargs: popen_kwargs.update(kwargs) or object())
    monkeypatch.setattr(cli.time, "sleep", lambda seconds: None)
    now = iter([0.0, 0.1, 0.2])
    monkeypatch.setattr(cli.time, "monotonic", lambda: next(now))
    monkeypatch.setattr(cli.sys, "executable", "python.exe")

    assert cli.start_worker() == 0
    assert popen_kwargs["env"]["PYINSTALLER_RESET_ENVIRONMENT"] == "1"
