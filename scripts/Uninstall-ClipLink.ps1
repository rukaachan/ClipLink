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

function Remove-StartupRunEntry([string]$AppName) {
    $runKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $existingValue = Get-ItemPropertyValue -Path $runKeyPath -Name $AppName -ErrorAction SilentlyContinue
    if ($null -eq $existingValue) {
        return $false
    }

    Remove-ItemProperty -Path $runKeyPath -Name $AppName -ErrorAction SilentlyContinue
    return $true
}

$cliPath = Join-Path $InstallDir "cliplink.exe"

if (Test-Path $cliPath) {
    & $cliPath stop | Out-Host
}
else {
    Get-Process -Name "ClipLink.Worker" -ErrorAction SilentlyContinue | Stop-Process -Force
}

$startupRemoved = Remove-StartupRunEntry "ClipLink"
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
if ($startupRemoved) {
    Write-Host "Removed ClipLink startup entry from HKCU Run."
}
else {
    Write-Host "No ClipLink startup entry found in HKCU Run."
}

$stillOnPath = (Get-Command cliplink -ErrorAction SilentlyContinue) -ne $null
if ($stillOnPath) {
    Write-Host "Warning: 'cliplink' is still resolvable in this shell/session. Open a new terminal to refresh PATH."
}
else {
    Write-Host "Verified: 'cliplink' is not resolvable from PATH in this shell/session."
}