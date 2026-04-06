# SnapDroid

## Overview

SnapDroid is a small Windows CLI for Factory Droid.

It saves clipboard images and copies a prompt like:

```text
analyze this image: C:\Users\<user>\Pictures\SnapDroid\img-xxxx.png
```

## Quick use

1. Start SnapDroid with `snapdroid start`
2. Take a screenshot or copy an image
3. Focus Droid in your terminal
4. Press `Alt+V`
5. Paste manually in your terminal with your usual paste shortcut

`Alt+V` and `Ctrl+Alt+V` both use the reliable copy-to-clipboard flow.

## CLI commands

```powershell
snapdroid start
snapdroid stop
snapdroid status
snapdroid restart
```

The CLI runs the background worker that watches hotkeys and prepares clipboard content.

## Install

Install globally:

```powershell
.\scripts\Install-SnapDroid.bat
```

Uninstall:

```powershell
.\scripts\Uninstall-SnapDroid.bat
```

Remove config and logs too:

```powershell
powershell -File .\scripts\Uninstall-SnapDroid.ps1 -RemoveData
```

## Default paths

- Images: `%USERPROFILE%\Pictures\SnapDroid`
- Config: `%LOCALAPPDATA%\SnapDroid\config.json`
- Logs: `%LOCALAPPDATA%\SnapDroid\logs\bridge.log`

## Config

Example:

```json
{
  "ImageRootDirectory": "%USERPROFILE%\\Pictures\\SnapDroid",
  "PromptTemplate": "analyze this image: {path}",
  "RetentionHours": 1,
  "PasteHotkey": "Alt+V",
  "CopyHotkey": "Ctrl+Alt+V",
  "AutoStartOnLogin": true
}
```

Sample file: `config/SnapDroid.config.example.json`

## Commands

```powershell
dotnet build .\SnapDroid.slnx
dotnet test .\tests\SnapDroid.Tests\SnapDroid.Tests.csproj
dotnet run --project .\src\SnapDroid.Cli\SnapDroid.Cli.csproj -- status
dotnet run --project .\src\SnapDroid.Cli\SnapDroid.Cli.csproj -- start
dotnet run --project .\src\SnapDroid.Cli\SnapDroid.Cli.csproj -- stop
```

Publish:

```powershell
dotnet publish .\src\SnapDroid.Worker\SnapDroid.Worker.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\dist\SnapDroid
dotnet publish .\src\SnapDroid.Cli\SnapDroid.Cli.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o .\dist\SnapDroid
```

## Repository layout

- `src/SnapDroid.Cli` - CLI entry point
- `src/SnapDroid.Worker` - background worker used by the CLI for image capture and clipboard copy
- `src/SnapDroid.Core` - shared logic
- `scripts/` - install and uninstall helpers
- `config/` - sample config
- `tests/` - xUnit tests
