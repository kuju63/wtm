# install.ps1 - wtm installer for Windows (PowerShell 5.1+)
# Usage: irm https://kuju63.github.io/wt/install.ps1 | iex
#        & ([scriptblock]::Create((irm https://kuju63.github.io/wt/install.ps1))) -Prefix "$env:ProgramFiles\wtm"
#        & ([scriptblock]::Create((irm https://kuju63.github.io/wt/install.ps1))) -Force
[CmdletBinding()]
param(
    [string]$Prefix = "$env:LOCALAPPDATA\Programs\wtm",
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# --- Variables ---
$InstallDir = $Prefix
$LatestVersion = ""
$BinaryName = ""
$DownloadUrl = ""
$HashUrl = ""
$InstallPath = ""

# --- Functions ---

function Exit-WithError {
    param([string[]]$Messages)
    $Messages | ForEach-Object { Write-Error "Error: $_" }
    exit 1
}

function Invoke-Download {
    param(
        [string]$Url,
        [string]$Destination
    )
    try {
        Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing
    }
    catch {
        Exit-WithError @(
            "Failed to download from $Url",
            "Please check your network connection.",
            "Error: $_"
        )
    }
}

function Get-LatestVersion {
    Write-Output "Fetching latest version..."
    try {
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/kuju63/wt/releases/latest" -UseBasicParsing
        $script:LatestVersion = $response.tag_name
    }
    catch {
        Exit-WithError @(
            "Failed to fetch latest version.",
            "This may be due to GitHub API rate limiting. Please wait and retry.",
            "Manual install: https://github.com/kuju63/wt/releases",
            "Error: $_"
        )
    }

    if ([string]::IsNullOrEmpty($script:LatestVersion)) {
        Exit-WithError @(
            "Failed to fetch latest version from GitHub API.",
            "This may be due to rate limiting. Please wait a moment and retry.",
            "Manual install: https://github.com/kuju63/wt/releases"
        )
    }

    Write-Output "Fetching latest version... $($script:LatestVersion)"
}

function Test-ExistingInstall {
    $script:InstallPath = Join-Path $script:InstallDir "wtm.exe"

    if (-not (Test-Path $script:InstallPath)) {
        return
    }

    $currentVersion = ""
    try {
        $currentVersion = & $script:InstallPath --version 2>$null | Select-Object -First 1
    }
    catch {
        $currentVersion = "unknown"
    }

    if ($currentVersion -eq $script:LatestVersion) {
        Write-Output "wtm $($script:LatestVersion) is already the latest version. Skipping."
        exit 0
    }

    Write-Output "wtm $currentVersion is already installed. Updating to $($script:LatestVersion)..."
    Invoke-HandleExistingInstall -InstallPath $script:InstallPath -CurrentVersion $currentVersion
}

function Invoke-HandleExistingInstall {
    param(
        [string]$InstallPath,
        [string]$CurrentVersion
    )

    if (-not (Test-Path $InstallPath)) {
        return
    }

    if ($Force) {
        return
    }

    if (-not [Environment]::UserInteractive) {
        Write-Output "Note: $InstallPath already exists. Skipping overwrite in non-interactive mode."
        Write-Output "To force overwrite, re-run with -Force:"
        Write-Output "  & ([scriptblock]::Create((irm https://kuju63.github.io/wt/install.ps1))) -Force"
        exit 0
    }

    $answer = Read-Host "Overwrite existing $InstallPath? [y/N]"
    if ($answer -ne 'y' -and $answer -ne 'Y') {
        Write-Output "Installation cancelled."
        exit 0
    }
}

function Build-Urls {
    $script:BinaryName = "wtm-$($script:LatestVersion)-windows-x64.exe"
    $script:DownloadUrl = "https://github.com/kuju63/wt/releases/download/$($script:LatestVersion)/$($script:BinaryName)"
    $script:HashUrl = "$($script:DownloadUrl).sha256"
}

function Install-Binary {
    $tmpDir = [System.IO.Path]::GetTempPath()
    $binPath = Join-Path $tmpDir $script:BinaryName
    $hashPath = Join-Path $tmpDir "$($script:BinaryName).sha256"

    Write-Output "Downloading wtm $($script:LatestVersion) for windows-x64..."
    Invoke-Download -Url $script:DownloadUrl -Destination $binPath

    Write-Output "Verifying SHA256 checksum..."
    Invoke-Download -Url $script:HashUrl -Destination $hashPath

    $expectedHash = (Get-Content $hashPath -Raw).Split(' ')[0].Trim()
    $actualHash = (Get-FileHash -Path $binPath -Algorithm SHA256).Hash

    if ($expectedHash.ToUpper() -ne $actualHash.ToUpper()) {
        Remove-Item -Path $binPath -Force -ErrorAction SilentlyContinue
        Exit-WithError @(
            "SHA256 verification failed for $($script:BinaryName).",
            "Downloaded file has been removed. Please retry.",
            "Manual install: https://github.com/kuju63/wt/releases"
        )
    }

    Write-Output "Installing to $($script:InstallPath)..."
    New-Item -ItemType Directory -Path $script:InstallDir -Force | Out-Null
    Copy-Item -Path $binPath -Destination $script:InstallPath -Force

    Remove-Item -Path $binPath -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $hashPath -Force -ErrorAction SilentlyContinue
}

function Show-PathInstruction {
    $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    $systemPath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    $allPaths = ($userPath + ";" + $systemPath).Split(';') | Where-Object { $_ -ne "" }

    if ($allPaths -notcontains $script:InstallDir) {
        Write-Output ""
        Write-Output "Note: $($script:InstallDir) is not in your PATH."
        Write-Output "To add it permanently, run in PowerShell:"
        Write-Output "  [Environment]::SetEnvironmentVariable(`"PATH`", `$env:PATH + `";$($script:InstallDir)`", `"User`")"
        Write-Output "Then restart your terminal."
    }
}

function Write-Success {
    Write-Output ""
    Write-Output "✓ wtm $($script:LatestVersion) installed successfully!"
    Write-Output ""
    Write-Output "Next steps:"
    Write-Output "  wtm --version"
    Write-Output "  wtm --help"
}

# --- Main ---
Get-LatestVersion
Test-ExistingInstall
Build-Urls
Install-Binary
Show-PathInstruction
Write-Success
