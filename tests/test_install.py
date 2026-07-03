import importlib.util
from pathlib import Path

def load_install_module():
    path = Path(__file__).resolve().parents[1] / "scripts" / "install_cliplink.py"
    spec = importlib.util.spec_from_file_location("install_cliplink", path)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module

def test_pyinstaller_build_hides_console_window():
    install_cliplink = load_install_module()
    command = install_cliplink.build_pyinstaller_command(Path("repo"), Path("dist"))

    assert "--noconsole" in command
    assert "--icon" in command
    assert str(Path("repo") / "assets" / "cliplink.ico") in command
