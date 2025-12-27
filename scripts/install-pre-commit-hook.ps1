#!/usr/bin/env pwsh
# Cross-platform pre-commit hook installer (PowerShell Core)
# Works on Windows, macOS, and Linux

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Check if PowerShell Core is available
$PwshCommand = $null
if (Get-Command pwsh -ErrorAction SilentlyContinue) {
    $PwshCommand = "pwsh"
} elseif (Get-Command powershell -ErrorAction SilentlyContinue) {
    # Fallback to Windows PowerShell if pwsh not available
    $PwshCommand = "powershell"
    Write-Warning "PowerShell Core (pwsh) not found. Using Windows PowerShell (powershell)."
    Write-Warning "For cross-platform compatibility, install PowerShell Core: https://aka.ms/powershell"
} else {
    Write-Error "PowerShell not found. Please install PowerShell Core (pwsh) from https://aka.ms/powershell"
    exit 1
}

# Get the repository root directory
try {
    $RepoRoot = (git rev-parse --show-toplevel)
} catch {
    Write-Error "Error: Not a git repository or git not found"
    exit 1
}

Set-Location $RepoRoot

# Use forward slashes for cross-platform compatibility
$HookSource = Join-Path $RepoRoot "scripts" "pre-commit-hook.ps1"

# Check if hook source exists
if (-not (Test-Path $HookSource)) {
    Write-Error "Error: Pre-commit hook script not found at $HookSource"
    exit 1
}

# Create .git/hooks directory if it doesn't exist (use forward slashes)
$HooksDir = Join-Path $RepoRoot ".git" "hooks"
New-Item -ItemType Directory -Path $HooksDir -Force | Out-Null

$HookTarget = Join-Path $HooksDir "pre-commit"

# Check if hook already exists
if ((Test-Path $HookTarget) -and -not $Force) {
    Write-Warning "Warning: Pre-commit hook already exists at $HookTarget"
    $response = Read-Host "Do you want to overwrite it? (y/N)"
    if ($response -ne "y" -and $response -ne "Y") {
        Write-Host "Installation cancelled."
        exit 0
    }
}

# Normalize path separators for cross-platform compatibility
# Use forward slashes which work on all platforms
$NormalizedHookSource = $HookSource -replace '\\', '/'

# Create the hook that calls our script
# Use the appropriate PowerShell command based on what's available
$HookContent = @"
#!/bin/sh
# Pre-commit hook - calls the PowerShell script (cross-platform)
exec $PwshCommand -File "$NormalizedHookSource"
"@

# Write hook file
[System.IO.File]::WriteAllText($HookTarget, $HookContent)

# Make the hook executable (Unix-like systems)
# Check if we're on a Unix-like system (macOS, Linux)
# $IsWindows is available in PowerShell Core 6+, and is $false on Unix systems
$isUnixSystem = $false
if (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue) {
    $isUnixSystem = -not $IsWindows
} else {
    # Fallback for Windows PowerShell 5.1: check if chmod command exists
    $isUnixSystem = (Get-Command chmod -ErrorAction SilentlyContinue) -ne $null
}

if ($isUnixSystem) {
    try {
        # Use native chmod command if available
        $chmodResult = & chmod +x $HookTarget 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not set executable permissions on hook file. You may need to run: chmod +x $HookTarget"
        }
    } catch {
        # Ignore if chmod fails - hook might still work
        Write-Warning "Could not set executable permissions on hook file: $_"
    }
}

Write-Host "✓ Pre-commit hook installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "The hook will now run on every commit and will:"
Write-Host "  1. Auto-format your code"
Write-Host "  2. Stage any auto-formatted files"
Write-Host "  3. Check for vulnerable packages"
Write-Host "  4. Block commits if vulnerabilities are found"
Write-Host ""
Write-Host "PowerShell command used: $PwshCommand"
Write-Host ""
Write-Host "To bypass the hook (not recommended), use:"
Write-Host "  git commit --no-verify"
