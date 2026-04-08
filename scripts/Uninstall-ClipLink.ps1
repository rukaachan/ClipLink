param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "Programs\ClipLink"),
    [switch]$RemoveData
)

$ErrorActionPreference = "Stop"

function Get-PathEntries([string]$Value) {
    return @($Value -split ";" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Remove-UserPathEntry([string]$PathEntry) {
    $current = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($null -eq $current) {
        $current = ""
    }
    $entries = Get-PathEntries $current
    $filtered = @($entries | Where-Object { $_.TrimEnd('\') -ine $PathEntry.TrimEnd('\') })

    if ($filtered.Count -eq $entries.Count) {
        return $false
    }

    $updated = $filtered -join ";"
    [Environment]::SetEnvironmentVariable("Path", $updated, "User")
    $env:Path = $updated + ";" + [Environment]::GetEnvironmentVariable("Path", "Machine")
    return $true
}

$cliPath = Join-Path $InstallDir "cliplink.exe"

if (Test-Path $cliPath) {
    & $cliPath stop | Out-Host
}
else {
    Get-Process -Name "ClipLink.Worker" -ErrorAction SilentlyContinue | Stop-Process -Force
}

$pathRemoved = Remove-UserPathEntry $InstallDir

if (Test-Path $InstallDir) {
    Remove-Item $InstallDir -Recurse -Force
}

if ($RemoveData) {
    $dataDir = Join-Path $env:LOCALAPPDATA "ClipLink"
    if (Test-Path $dataDir) {
        Remove-Item $dataDir -Recurse -Force
    }
}

Write-Host ""
Write-Host "ClipLink uninstalled from: $InstallDir"
if ($pathRemoved) {
    Write-Host "Removed the install directory from your user PATH."
}
if ($RemoveData) {
    Write-Host "Removed ClipLink app data from %LOCALAPPDATA%\ClipLink."
}
else {
    Write-Host "Kept your config and logs in %LOCALAPPDATA%\ClipLink."
}
