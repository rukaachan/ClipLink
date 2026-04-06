param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "Programs\SnapDroid"),
    [string]$Runtime = "win-x64",
    [switch]$NoStart
)

$ErrorActionPreference = "Stop"

function Resolve-DotnetPath {
    $candidates = @(
        "C:\Program Files\dotnet\dotnet.exe",
        (Join-Path $env:DOTNET_ROOT "dotnet.exe"),
        (Join-Path $env:USERPROFILE "scoop\apps\dotnet-sdk-lts\current\dotnet.exe"),
        (Join-Path $env:USERPROFILE "scoop\apps\dotnet-sdk\current\dotnet.exe"),
        (Join-Path $env:ProgramFiles "dotnet\dotnet.exe")
    ) | Where-Object { $_ }

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    throw "dotnet.exe was not found. Install the .NET SDK or add it to PATH."
}

function Set-DotnetSdkEnvironment([string]$DotnetPath) {
    $dotnetRoot = Split-Path -Parent $DotnetPath
    $sdkVersion = (& $DotnetPath --list-sdks | Select-Object -Last 1).Split(' ')[0]
    if ([string]::IsNullOrWhiteSpace($sdkVersion)) {
        throw "Unable to determine installed .NET SDK version."
    }

    $env:DOTNET_ROOT = $dotnetRoot
    $env:MSBuildSDKsPath = Join-Path $dotnetRoot "sdk\$sdkVersion\Sdks"
}

function Get-PathEntries([string]$Value) {
    return @($Value -split ";" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Add-UserPathEntry([string]$PathEntry) {
    $current = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($null -eq $current) {
        $current = ""
    }
    $entries = Get-PathEntries $current

    if ($entries | Where-Object { $_.TrimEnd('\') -ieq $PathEntry.TrimEnd('\') }) {
        return $false
    }

    $updated = @($entries + $PathEntry) -join ";"
    [Environment]::SetEnvironmentVariable("Path", $updated, "User")
    $env:Path = $updated + ";" + [Environment]::GetEnvironmentVariable("Path", "Machine")
    return $true
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
$dotnet = Resolve-DotnetPath
Set-DotnetSdkEnvironment $dotnet
$workerProject = Join-Path $repoRoot "src\SnapDroid.Worker\SnapDroid.Worker.csproj"
$cliProject = Join-Path $repoRoot "src\SnapDroid.Cli\SnapDroid.Cli.csproj"
$cliPath = Join-Path $InstallDir "snapdroid.exe"

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

if (Test-Path $cliPath) {
    & $cliPath stop | Out-Host
}
else {
    Get-Process -Name "SnapDroid.Worker" -ErrorAction SilentlyContinue | Stop-Process -Force
}

& $dotnet publish $workerProject -c Release -r $Runtime --self-contained true /p:PublishSingleFile=true -o $InstallDir
if ($LASTEXITCODE -ne 0) {
    throw "Failed to publish SnapDroid worker."
}

& $dotnet publish $cliProject -c Release -r $Runtime --self-contained true /p:PublishSingleFile=true -o $InstallDir
if ($LASTEXITCODE -ne 0) {
    throw "Failed to publish SnapDroid CLI."
}

$pathAdded = Add-UserPathEntry $InstallDir

if (-not $NoStart) {
    & $cliPath start | Out-Host
}

Write-Host ""
Write-Host "SnapDroid installed to: $InstallDir"
if ($pathAdded) {
    Write-Host "Added to your user PATH. Open a new terminal to use 'snapdroid' everywhere."
}
else {
    Write-Host "Install directory is already on your user PATH."
}
