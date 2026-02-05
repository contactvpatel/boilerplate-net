#!/usr/bin/env pwsh
<#
.SYNOPSIS
Security Audit Script for Local Development

.DESCRIPTION
This script provides optional local security scanning before pushing to Git.
Complete security scanning (SAST, code analysis) runs in Jenkins CI/CD.

.PARAMETER Fast
Quick scan only (dependency check, no code analysis)

.PARAMETER Help
Show help message

.EXAMPLE
./scripts/security-audit.ps1              # Full audit
./scripts/security-audit.ps1 -Fast        # Quick scan
./scripts/security-audit.ps1 -Help        # Show help

.NOTES
Full security scanning runs in Jenkins CI/CD pipeline
This script is for catching common issues locally
#>

param(
    [switch]$Fast,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

# Colors for output (ANSI codes work in PowerShell Core on all platforms)
$RED = "`e[0;31m"
$GREEN = "`e[0;32m"
$YELLOW = "`e[1;33m"
$BLUE = "`e[0;34m"
$NC = "`e[0m"  # No Color

# Get script and project root directories
$ScriptDir = Split-Path -Parent $PSCommandPath
$ProjectRoot = Split-Path -Parent $ScriptDir

# Show help
if ($Help) {
    @"
Security Audit Script - WebShop.Api

USAGE:
  ./scripts/security-audit.ps1 [OPTIONS]
  pwsh scripts/security-audit.ps1 [OPTIONS]

OPTIONS:
  -Fast              Quick scan only (dependency check, no code analysis)
  -Help              Show this help message

DESCRIPTION:
  This script provides local security scanning before pushing to Git.
  Complete security scanning (SAST, code analysis) runs in Jenkins CI/CD.

CHECKS PERFORMED:
  Default:
    ✓ Dependency vulnerability scanning
    ✓ Hardcoded credentials check
    ✓ Git secrets scan

  Fast Mode (-Fast):
    ✓ Dependency vulnerability scanning only

JENKINS CI/CD PIPELINE CHECKS:
  These checks run automatically in Jenkins (not locally):
    ✓ SonarQube SAST analysis
    ✓ Full code quality scanning
    ✓ DAST (Dynamic security testing)
    ✓ Docker image vulnerability scan
    ✓ Dependency graph analysis (Supply Chain Security)

SECURITY SCANNING DOCUMENTATION:
  See /docs/standards/security-scanning.md for full details

"@
    exit 0
}

Write-Host ""
Write-Host "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
Write-Host "${BLUE}Security Audit - WebShop.Api${NC}"
Write-Host "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

# Change to project root
Push-Location $ProjectRoot

$Failed = $false

# ============================================================================
# 1. Dependency Vulnerability Scanning (All Projects)
# ============================================================================
Write-Host ""
Write-Host "${YELLOW}[1/3] Checking NuGet package vulnerabilities in all projects...${NC}"

# Check if dotnet CLI is available
try {
    $dotnetVersion = dotnet --version 2>$null
} catch {
    Write-Host "${RED}✗ dotnet CLI not found${NC}"
    $Failed = $true
}

if (-not $Failed) {
    # Find all .csproj files (excluding build artifacts)
    $projectFiles = @(Get-ChildItem -Path $ProjectRoot -Filter "*.csproj" -Recurse -File | Where-Object {
        $fullPath = $_.FullName -replace '\\', '/'
        $fullPath -notmatch "/bin/|/obj/|/TestResults/|/\.vs/" -and
        $fullPath -notmatch "/node_modules/|/packages/"
    } | Sort-Object FullName)

    if ($projectFiles.Count -eq 0) {
        Write-Host "${RED}✗ No .csproj files found${NC}"
        $Failed = $true
    } else {
        Write-Host "Found $($projectFiles.Count) project(s) to scan"
        Write-Host ""

        $vulnFound = $false

        foreach ($projectFile in $projectFiles) {
            $projectName = $projectFile.BaseName
            Write-Host "${BLUE}Scanning: $projectName${NC}"

            try {
                $vulnOutput = dotnet list $projectFile.FullName package --vulnerable --include-transitive 2>$null | Out-String
            } catch {
                $vulnOutput = ""
            }

            # Check if vulnerabilities exist - look for actual package vulnerability lines
            # Pattern: [spaces]PackageName [spaces] Version [spaces] ...
            $vulnLines = @($vulnOutput -split "`n" | Where-Object {
                $_ -match "^\s+[a-zA-Z0-9][a-zA-Z0-9.\-]*\s+[0-9]+\.[0-9]+" -and
                $_ -notmatch "Determining|All projects|nuget.org"
            })

            if ($vulnLines.Count -gt 0) {
                Write-Host "${RED}  ✗ Vulnerabilities found:${NC}"
                $vulnLines | ForEach-Object { Write-Host "    $_" }
                $vulnFound = $true
                $Failed = $true
            } else {
                Write-Host "${GREEN}  ✓ No vulnerable packages${NC}"
            }
        }

        Write-Host ""
        if ($vulnFound) {
            Write-Host "${RED}✗ Vulnerable packages detected in one or more projects!${NC}"
            Write-Host "${YELLOW}Action: Update packages using: dotnet add package [name] --version [safe-version]${NC}"
        } else {
            Write-Host "${GREEN}✓ All projects: No vulnerable packages found${NC}"
        }
    }
}

# If fast mode, skip remaining checks
if ($Fast) {
    Write-Host ""
    Write-Host "${YELLOW}Fast mode enabled - skipping additional checks${NC}"
    Write-Host "${BLUE}(Full security scanning runs in Jenkins CI/CD pipeline)${NC}"
    Write-Host ""

    Pop-Location

    if ($Failed) {
        Write-Host "${RED}❌ Security audit FAILED${NC}"
        exit 1
    } else {
        Write-Host "${GREEN}✓ Fast security scan PASSED${NC}"
        exit 0
    }
}

# ============================================================================
# 2. Hardcoded Credentials Check (Warning - not blocking)
# ============================================================================
Write-Host ""
Write-Host "${YELLOW}[2/3] Scanning for hardcoded credentials...${NC}"

$credPatterns = @(
    'password.*=.*[' + "'" + '"].+[' + "'" + '"]',
    'apikey.*=.*[' + "'" + '"].+[' + "'" + '"]',
    'secret.*=.*[' + "'" + '"].+[' + "'" + '"]',
    'token.*=.*[' + "'" + '"].+[' + "'" + '"]',
    'aws_access_key',
    'aws_secret_key'
)

$foundCreds = $false

foreach ($pattern in $credPatterns) {
    try {
        $matches = @(Get-ChildItem -Path "$ProjectRoot/src" -Recurse -Include "*.json", "*.config", "*.cs" -File |
            Select-String -Pattern $pattern |
            Where-Object {
                $_.Path -notmatch "appsettings\.Development\.json" -and
                $_.Path -notmatch "\.example" -and
                $_.Path -notmatch "\.template"
            })

        if ($matches.Count -gt 0) {
            Write-Host "${RED}⚠ Possible hardcoded credential found:${NC}"
            $matches | ForEach-Object { Write-Host "  $_" }
            $foundCreds = $true
        }
    } catch {
        # Continue if grep fails
    }
}

if ($foundCreds) {
    Write-Host "${YELLOW}⚠ Review: Use Vault/Key Vault for production credentials${NC}"
} else {
    Write-Host "${GREEN}✓ No obvious hardcoded credentials detected${NC}"
}

# ============================================================================
# 3. Git Secrets Check (if tool installed)
# ============================================================================
Write-Host ""
Write-Host "${YELLOW}[3/3] Checking Git history for secrets...${NC}"

try {
    $gitSecretsPath = Get-Command git-secrets -ErrorAction SilentlyContinue
    if ($gitSecretsPath) {
        Write-Host "Running: git-secrets --scan"
        $gitSecretsOutput = git-secrets --scan 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "${GREEN}✓ No secrets found in git${NC}"
        } else {
            Write-Host "${RED}✗ Secrets detected in git history${NC}"
            Write-Host $gitSecretsOutput
            $Failed = $true
        }
    } else {
        Write-Host "${YELLOW}ℹ git-secrets tool not installed (optional)${NC}"
        Write-Host "  Install: brew install git-secrets (macOS) or apt-get install git-secrets (Linux)"
    }
} catch {
    Write-Host "${YELLOW}ℹ git-secrets tool not installed (optional)${NC}"
    Write-Host "  Install: brew install git-secrets (macOS) or apt-get install git-secrets (Linux)"
}


# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"

Pop-Location

if ($Failed) {
    Write-Host "${RED}❌ Security audit FAILED${NC}"
    Write-Host ""
    Write-Host "Fix issues before committing to Git."
    Write-Host "Full security scanning will run in Jenkins CI/CD pipeline."
    exit 1
} else {
    Write-Host "${GREEN}✓ Local security checks PASSED${NC}"
    Write-Host ""
    exit 0
}
