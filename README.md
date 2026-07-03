# ClipLink

Windows clipboard bridge for image-to-prompt workflows.

Copy/screenshot an image → press hotkey → ClipLink saves image → pastes prompt with saved path:

```text
 : C:/Users/<user>\Pictures\ClipLink\img-xxxx.png
```

## Requirements

- Windows
- Python 3.11+
- `uv` for dev/build install script

## Install

```powershell
python scripts\install_cliplink.py
```

Installs `cliplink.exe` to:

```text
%LOCALAPPDATA%\Programs\ClipLink
```

Adds install dir to user `PATH`, then restarts worker.

## CLI

```powershell
cliplink start
cliplink stop
cliplink status
cliplink restart
```

## Use

1. Start worker: `cliplink start`
2. Copy image / take screenshot
3. Press ClipLink hotkey
4. Prompt is pasted into active app

## Hotkeys

Defaults:

| Action | Hotkey |
|---|---|
| Paste/action | `Alt+V` |
| Copy/action | `Ctrl+Alt+V` |

Both actions currently save clipboard image, generate prompt, paste prompt into active window, then restore clipboard if enabled.

Fallbacks if configured/default hotkey is unavailable:

| Action | Fallback order |
|---|---|
| Paste/action | `Alt+V` → `Alt+Shift+V` → `Alt+F10` |
| Copy/action | `Ctrl+Alt+V` → `Ctrl+Shift+F10` → `Ctrl+Alt+F10` |

Supported hotkey modifiers: `Ctrl`/`Control`, `Alt`, `Shift`, `Win`.

Supported keys: `A-Z`, `0-9`, `F1-F12`, `Insert`, `Delete`, `Home`, `End`, `PageUp`, `PageDown`, `Space`, `Esc`/`Escape`.

## Config

Path:

```text
%LOCALAPPDATA%\ClipLink\config.json
```

Created on first run.

Example:

```json
{
  "image_root_directory": "%USERPROFILE%\Pictures\ClipLink",
  "prompt_template": " : {path}",
  "retention_hours": 1,
  "paste_hotkey": "Alt+V",
  "copy_hotkey": "Ctrl+Alt+V",
  "auto_start_on_login": true,
  "paste_sequence": "Ctrl+Shift+V",
  "restore_clipboard_after_paste": true,
  "restore_delay_ms": 250,
  "process_overrides": {
    "notepad": {
      "paste_sequence": "Ctrl+V",
      "restore_delay_ms": 500
    }
  }
}
```

Notes:

- `prompt_template` must contain `{path}`.
- `retention_hours` must be greater than `0`.
- Supported `paste_sequence`: `Ctrl+Shift+V`, `Ctrl+V`, `Shift+Insert`.
- `process_overrides` keys are process names without `.exe`, lowercased internally.

## Paths

| Item | Path |
|---|---|
| Images | `%USERPROFILE%\Pictures\ClipLink` |
| Config | `%LOCALAPPDATA%\ClipLink\config.json` |
| Logs | `%LOCALAPPDATA%\ClipLink\logs\bridge.log` |
| Worker PID | `%LOCALAPPDATA%\ClipLink\worker.pid` |
| Install | `%LOCALAPPDATA%\Programs\ClipLink\cliplink.exe` |

## Uninstall

```powershell
python scripts\uninstall_cliplink.py
```

Remove app data too:

```powershell
python scripts\uninstall_cliplink.py --remove-data
```

Uninstall stops worker, removes startup entry, removes install dir from user `PATH`, deletes install dir.

## Development

```powershell
uv sync
pytest tests -q
python -m cliplink.cli status
python -m cliplink.cli start
python -m cliplink.cli stop
```

Build executable:

```powershell
uv run pyinstaller --onefile --name cliplink --distpath dist\ClipLink --workpath build --specpath build cliplink_launcher.py
```

## Project layout

```text
src/cliplink/          package
scripts/               install/uninstall helpers
tests/                 pytest suite
cliplink_launcher.py   PyInstaller launcher
```

## Troubleshooting

Worker failed to start → check:

```text
%LOCALAPPDATA%\ClipLink\logs\bridge.log
```

Hotkey not working → likely registered by another app. ClipLink tries fallback hotkeys automatically.

Prompt not pasted correctly → set per-process `paste_sequence` override, commonly `Ctrl+V` for simple editors.
