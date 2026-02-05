#!/usr/bin/env pwsh
<#
.SYNOPSIS
Install optional security tools for enhanced vulnerability scanning

.DESCRIPTION
Installs git-secrets and dotnet-nuget-audit to enable more comprehensive
security scanning in the security-audit.ps1 script.

Tools are optional - the main security audit works without them.

.EXAMPLE
./scripts/install-security-tools.ps1               # Interactive (prompts for each tool)
pwsh scripts/install-security-tools.ps1            # Cross-platform execution
pwsh scripts/install-security-tools.ps1 -All       # Install all tools without prompting
pwsh scripts/install-security-tools.ps1 -Help      # Show help

.NOTES
Requires:
- Administrator/sudo privileges (for system package managers)
- .NET 6 SDK or higher (for dotnet-nuget-audit)
#>

param(
    [switch]$All,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

# Colors
$BLUE = "`e[0;34m"
$GREEN = "`e[0;32m"
$YELLOW = "`e[1;33m"
$RED = "`e[0;31m"
$NC = "`e[0m"

function Write-Title {
    param([string]$Message)
    Write-Host ""
    Write-Host "$BLUE━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━$NC"
    Write-Host "$BLUE$Message$NC"
    Write-Host "$BLUE━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━$NC"
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "$GREEN✓ $Message$NC"
}

function Write-Info {
    param([string]$Message)
    Write-Host "$YELLOW$Message$NC"
}

function Write-Error {
    param([string]$Message)
    Write-Host "$RED✗ $Message$NC"
}

function Test-CommandExists {
    param([string]$Command)

    try {
        $null = Get-Command $Command -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

function Test-AdminPrivileges {
    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    return $isAdmin
}

# Show help
if ($Help) {
    @"
Install Optional Security Tools

USAGE:
  pwsh scripts/install-security-tools.ps1 [OPTIONS]

OPTIONS:
  -All                Install all optional tools without prompting
  -Help               Show this help message

WHAT'S INCLUDED:
  Security audit script already has:
    ✓ NuGet vulnerability scanning (built-in dotnet list package --vulnerable)
    ✓ Hardcoded credentials detection
    ✓ Git secrets scanning (if tool installed)

OPTIONAL TOOLS:
  This script installs:
    • git-secrets - Scan git history for leaked secrets (API keys, passwords, tokens)

REQUIREMENTS:
  - Administrator/sudo privileges (to install system packages)
  - macOS: brew command
  - Linux: apt-get, dnf, or yum
  - Windows: Chocolatey (or manual installation)

INSTALLATION TIME:
  2-5 minutes depending on your internet connection

WHAT'S NEXT:
  1. Run: pwsh scripts/security-audit.ps1 -Fast
  2. Optional tools will be automatically used if installed
  3. View results: See colored output and exit code (0 = pass, 1 = fail)

DOCUMENTATION:
  See /docs/standards/security-scanning.md for complete details

"@
    exit 0
}

Write-Title "Optional Security Tools Installer"

Write-Host "Security audit already includes:"
Write-Host "  ✓ NuGet vulnerability scanning (built-in dotnet list package --vulnerable)"
Write-Host "  ✓ Hardcoded credentials detection"
Write-Host ""
Write-Host "This script installs additional optional tools:"
Write-Host "  • git-secrets - Scan git history for leaked secrets"
Write-Host ""

# Detect OS (using custom variable names to avoid built-in read-only variables)
$DetectedWindows = $PSVersionTable.Platform -eq 'Win32NT' -or $PSVersionTable.OS -like '*Windows*'
$DetectedMacOS = $PSVersionTable.OS -like '*Darwin*'
$DetectedLinux = $PSVersionTable.OS -like '*Linux*'

Write-Host "Detected OS: " -NoNewline
if ($DetectedWindows) { Write-Host "Windows" }
elseif ($DetectedMacOS) { Write-Host "macOS" }
elseif ($DetectedLinux) { Write-Host "Linux" }
Write-Host ""

# ============================================================================
# 1. Install git-secrets
# ============================================================================
Write-Title "1. Git Secrets - Scan Git History for Secrets"

Write-Host "What it does:"
Write-Host "  Scans git commit history to catch secrets (API keys, passwords, tokens)"
Write-Host "  before they are pushed to the repository"
Write-Host ""

if (-not $All) {
    $installGitSecrets = Read-Host "Install git-secrets? (y/n)"
} else {
    $installGitSecrets = "y"
}

if ($installGitSecrets -eq "y" -or $installGitSecrets -eq "yes") {
    Write-Host "Installing git-secrets..."
    Write-Host ""

    try {
        if ($DetectedWindows) {
            # Windows via Chocolatey
            if (Test-CommandExists "choco") {
                Write-Info "Installing via Chocolatey..."
                choco install git-secrets -y
                Write-Success "git-secrets installed via Chocolatey"
            } else {
                Write-Info "Chocolatey not found. Install git-secrets manually:"
                Write-Host "  1. Download from: https://github.com/awslabs/git-secrets"
                Write-Host "  2. Extract and add to PATH"
                Write-Host "  OR install Chocolatey first: https://chocolatey.org/install"
            }
        } elseif ($DetectedMacOS) {
            # macOS via Homebrew
            Write-Info "Installing via Homebrew..."
            brew install git-secrets
            Write-Success "git-secrets installed via Homebrew"
        } elseif ($DetectedLinux) {
            # Linux - check for apt or dnf
            if (Test-CommandExists "apt-get") {
                Write-Info "Installing via apt-get..."
                sudo apt-get update
                sudo apt-get install git-secrets -y
                Write-Success "git-secrets installed via apt-get"
            } elseif (Test-CommandExists "dnf") {
                Write-Info "Installing via dnf..."
                sudo dnf install git-secrets -y
                Write-Success "git-secrets installed via dnf"
            } elseif (Test-CommandExists "yum") {
                Write-Info "Installing via yum..."
                sudo yum install git-secrets -y
                Write-Success "git-secrets installed via yum"
            } else {
                Write-Info "Package manager not detected. Install manually:"
                Write-Host "  Ubuntu/Debian: sudo apt-get install git-secrets"
                Write-Host "  Fedora/RHEL: sudo dnf install git-secrets"
                Write-Host "  Or build from source: https://github.com/awslabs/git-secrets"
            }
        }

        # Verify installation
        if (Test-CommandExists "git-secrets") {
            Write-Success "git-secrets installed successfully"
            git-secrets --version
        } else {
            Write-Error "git-secrets installation may have failed. Install manually from: https://github.com/awslabs/git-secrets"
        }
    } catch {
        Write-Error "Failed to install git-secrets: $_"
        Write-Host "Install manually from: https://github.com/awslabs/git-secrets"
    }
} else {
    Write-Info "Skipped git-secrets installation"
}

Write-Host ""

# ============================================================================
# 2. NuGet Vulnerability Scanning (Built-in)
# ============================================================================
Write-Title "2. NuGet Vulnerability Scanning - Already Included!"

Write-Host "Good news! The security audit script already includes:"
Write-Host ""
Write-Host "  ✓ Built-in vulnerability scanning"
Write-Host "    Command: dotnet list package --vulnerable"
Write-Host "    Scans all projects for known CVEs"
Write-Host "    Runs on every audit (no installation needed)"
Write-Host ""
Write-Host "This is the PRIMARY method for NuGet package scanning."
Write-Host "It's more reliable and always available."
Write-Host ""
Write-Host "Jenkins CI/CD performs additional checks:"
Write-Host "  • SonarQube SAST code analysis"
Write-Host "  • Docker image scanning"
Write-Host "  • Supply chain security"
Write-Host ""

Write-Host ""

# ============================================================================
# Summary
# ============================================================================
Write-Title "Installation Complete"

Write-Host "Security scanning status:"
Write-Host ""
Write-Host "✓ Primary auditing (always available):"
Write-Host "  • NuGet vulnerability scanning (built-in)"
Write-Host "  • Hardcoded credentials detection"
Write-Host ""

Write-Host "Optional enhancements:"
Write-Host ""

if (Test-CommandExists "git-secrets") {
    Write-Success "git-secrets: Ready to use"
} else {
    Write-Info "git-secrets: Not installed (optional)"
}

Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Run the security audit: pwsh scripts/security-audit.ps1"
Write-Host "  2. All checks will run automatically"
Write-Host "  3. Optional tools will be used if installed"
Write-Host ""
Write-Host "Documentation:"
Write-Host "  See /docs/standards/security-scanning.md for complete details"
Write-Host ""

Write-Success "Security scanning is ready to use!"
