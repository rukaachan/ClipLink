# ClipLink

## Overview

ClipLink is a Windows clipboard bridge for cross-app image prompt workflows.

When you copy an image to clipboard, ClipLink saves it and copies a text prompt in this format:

```text
 : C:\Users\<user>\Pictures\ClipLink\img-xxxx.png
```

## Quick use

1. Start ClipLink with `cliplink start`
2. Take a screenshot or copy an image
3. Trigger the ClipLink hotkey
4. Paste in your target app with your normal paste shortcut

ClipLink is currently configured for copy-only flow (no auto-paste injection).

## Hotkeys

Configured defaults:
- Paste/action hotkey: `Alt+V`
- Copy hotkey: `Ctrl+Alt+V`

If a configured hotkey is already taken by another app, ClipLink automatically falls back at runtime.

Fallback order:
- Paste/action: `Alt+V` -> `Alt+Shift+V` -> `Alt+F10`
- Copy: `Ctrl+Alt+V` -> `Ctrl+Shift+F10` -> `Ctrl+Alt+F10`

You can check active hotkeys in `%LOCALAPPDATA%\ClipLink\logs\bridge.log`.

## CLI commands

```powershell
cliplink start
cliplink stop
cliplink status
cliplink restart
```

## Install

Install globally:

```powershell
.\scripts\Install-ClipLink.bat
```

Uninstall:

```powershell
.\scripts\Uninstall-ClipLink.bat
```

Remove config and logs too:

```powershell
powershell -File .\scripts\Uninstall-ClipLink.ps1 -RemoveData
```

## Default paths

- Images: `%USERPROFILE%\Pictures\ClipLink`
- Config: `%LOCALAPPDATA%\ClipLink\config.json`
- Logs: `%LOCALAPPDATA%\ClipLink\logs\bridge.log`

## Config

Example:

```json
{
  "ImageRootDirectory": "%USERPROFILE%\\Pictures\\ClipLink",
  "PromptTemplate": " : {path}",
  "RetentionHours": 1,
  "PasteHotkey": "Alt+V",
  "CopyHotkey": "Ctrl+Alt+V",
  "AutoStartOnLogin": true
}
```

Sample file: `config/ClipLink.config.example.json`

## Commands

```powershell
dotnet build .\ClipLink.slnx
dotnet test .\tests\ClipLink.Tests\ClipLink.Tests.csproj
dotnet run --project .\src\ClipLink.Cli\ClipLink.Cli.csproj -- status
dotnet run --project .\src\ClipLink.Cli\ClipLink.Cli.csproj -- start
dotnet run --project .\src\ClipLink.Cli\ClipLink.Cli.csproj -- stop
```

Publish:

```powershell
dotnet publish .\src\ClipLink.Worker\ClipLink.Worker.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\dist\ClipLink
dotnet publish .\src\ClipLink.Cli\ClipLink.Cli.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\dist\ClipLink
```

## Repository layout

- `src/ClipLink.Cli` - CLI entry point
- `src/ClipLink.Worker` - background worker for clipboard image capture and prompt copy
- `src/ClipLink.Core` - shared logic
- `scripts/` - install/uninstall helpers
- `config/` - sample config
- `tests/` - xUnit tests