#!/usr/bin/env pwsh
<#
.SYNOPSIS
Local SonarQube SAST (Static Application Security Testing) scan using SonarCloud

.DESCRIPTION
Runs SonarScanner to perform static code analysis locally before pushing to Git.
Uses SonarCloud (cloud-hosted) for results analysis and display.

.PARAMETER Help
Show help message

.EXAMPLE
./scripts/sonarqube-scan.ps1                 # Run analysis
pwsh scripts/sonarqube-scan.ps1 -Help        # Show help

.NOTES
Requires:
- SonarScanner CLI installed (dotnet-sonarscanner)
- sonar-project.properties configured in project root
- SONAR_TOKEN environment variable set
- Network access to SonarCloud
#>

param(
    [switch]$Help
)

$ErrorActionPreference = "Stop"

# Colors for output
$RED = "`e[0;31m"
$GREEN = "`e[0;32m"
$YELLOW = "`e[1;33m"
$BLUE = "`e[0;34m"
$NC = "`e[0m"  # No Color

# Get script and project root directories
$ScriptDir = Split-Path -Parent $PSCommandPath
$ProjectRoot = Split-Path -Parent $ScriptDir
$SonarConfigPath = Join-Path $ProjectRoot "sonar-config.json"

# Show help
if ($Help) {
    @"
SonarQube SAST Analysis - Local Development with SonarCloud

USAGE:
  ./scripts/sonarqube-scan.ps1
  pwsh scripts/sonarqube-scan.ps1

OPTIONS:
  -Help              Show this help message

DESCRIPTION:
  Performs static code analysis using SonarScanner with SonarCloud to identify:
    ✓ Code smells and technical debt
    ✓ Security vulnerabilities
    ✓ Code quality issues
    ✓ Coverage metrics
    ✓ Duplicate code

REQUIREMENTS:
  1. SonarScanner CLI: dotnet-sonarscanner (auto-installed via script)
  2. Configuration file: sonar-config.json (optional, auto-creates if missing)
  3. Environment variables OR user prompts (if config file not available)

CONFIGURATION PRIORITY (highest to lowest):
  1. Environment Variables: SONAR_PROJECT_KEY, SONAR_ORG_KEY, SONAR_TOKEN
  2. Configuration File: sonar-config.json in project root
  3. Interactive Prompts: User input if env vars and config file not available

SETUP (SonarCloud):
  1. Create free account: https://sonarcloud.io
  2. Create organization and project
  3. Generate token at: https://sonarcloud.io/account/security
  4. Edit sonar-config.json with your project key and organization key
  5. (Optional) Set environment: SONAR_TOKEN=<your-token> (or script will prompt)
  6. Run: pwsh scripts/sonarqube-scan.ps1

NEXT STEPS:
  1. Edit sonar-config.json with your project details
  2. Set SONAR_TOKEN environment variable
  3. Run analysis: pwsh scripts/sonarqube-scan.ps1

"@
    exit 0
}

Write-Host "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
Write-Host "${BLUE}SonarQube SAST Analysis - SonarCloud${NC}"
Write-Host "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
Write-Host ""

# ============================================================================
# 1. Check if SonarScanner is installed
# ============================================================================
Write-Host "${YELLOW}[1/4] Checking SonarScanner installation...${NC}"
Write-Host ""

try {
    $sonarScannerVersion = dotnet-sonarscanner --version 2>$null
    Write-Host "${GREEN}✓ SonarScanner found: $sonarScannerVersion${NC}"
} catch {
    Write-Host "${YELLOW}ℹ SonarScanner not found. Installing...${NC}"
    Write-Host ""

    try {
        Write-Host "Running: dotnet tool install --global dotnet-sonarscanner"
        dotnet tool install --global dotnet-sonarscanner
        Write-Host "${GREEN}✓ SonarScanner installed successfully${NC}"
        Write-Host ""
    } catch {
        Write-Host "${RED}✗ Failed to install SonarScanner${NC}"
        Write-Host ""
        Write-Host "Manual installation:"
        Write-Host "  dotnet tool install --global dotnet-sonarscanner"
        Write-Host ""
        exit 1
    }
}

# ============================================================================
# 2. Collect Configuration from sonar-config.json, Environment, or User Input
# ============================================================================
Write-Host ""
Write-Host "${YELLOW}[2/4] Collecting SonarQube configuration...${NC}"
Write-Host ""

# Load configuration from sonar-config.json if it exists
$configData = $null
if (Test-Path $SonarConfigPath) {
    try {
        $configData = Get-Content $SonarConfigPath -Raw | ConvertFrom-Json
        Write-Host "${GREEN}✓ Configuration file loaded: sonar-config.json${NC}"
    } catch {
        Write-Host "${YELLOW}⚠ Warning: Could not parse sonar-config.json, using environment variables${NC}"
    }
} else {
    Write-Host "${YELLOW}ℹ No sonar-config.json found, using environment variables${NC}"
}

# For SonarScanner for .NET, we pass config via command-line parameters
# Configuration priority: Environment Variables → Config File → User Input

# Get configuration from environment, config file, or user input
$projectKey = $env:SONAR_PROJECT_KEY
if (-not $projectKey -and $configData) {
    $projectKey = $configData.projectKey
}
if (-not $projectKey) {
    $projectKey = Read-Host "Enter SonarCloud Project Key (e.g., com.webshop:api)"
}

$orgKey = $env:SONAR_ORG_KEY
if (-not $orgKey -and $configData) {
    $orgKey = $configData.sonarCloud.organizationKey
}
if (-not $orgKey) {
    $orgKey = Read-Host "Enter SonarCloud Organization Key (e.g., my-organization)"
}

if (-not $projectKey -or -not $orgKey) {
    Write-Host "${RED}✗ Project key and organization key are required${NC}"
    exit 1
}

Write-Host "Project Key: $projectKey"
Write-Host "Organization: $orgKey"
Write-Host "Source: $(if ($env:SONAR_PROJECT_KEY -or $env:SONAR_ORG_KEY) { 'Environment Variables' } elseif ($configData) { 'sonar-config.json' } else { 'User Input' })"
Write-Host "${GREEN}✓ Configuration valid${NC}"

# ============================================================================
# 3. Check authentication
# ============================================================================
Write-Host ""
Write-Host "${YELLOW}[3/4] Checking authentication...${NC}"
Write-Host ""

$sonarToken = $env:SONAR_TOKEN

if (-not $sonarToken) {
    Write-Host "${RED}✗ SONAR_TOKEN environment variable not set${NC}"
    Write-Host ""
    Write-Host "To authenticate with SonarCloud:"
    Write-Host "  1. Create free account: https://sonarcloud.io"
    Write-Host "  2. Generate token: https://sonarcloud.io/account/security"
    Write-Host "  3. Set environment variable:"
    Write-Host ""
    Write-Host "     Windows (PowerShell):"
    Write-Host '       $env:SONAR_TOKEN = "your-token-here"'
    Write-Host ""
    Write-Host "     macOS/Linux (bash):"
    Write-Host '       export SONAR_TOKEN="your-token-here"'
    Write-Host ""
    Write-Host "  4. Re-run this script"
    Write-Host ""
    exit 1
}

Write-Host "${GREEN}✓ SONAR_TOKEN is set${NC}"

# ============================================================================
# 4. Run SonarQube analysis
# ============================================================================
Write-Host ""
Write-Host "${YELLOW}[4/4] Running SonarQube analysis...${NC}"
Write-Host ""

# Validate token is set
if (-not $env:SONAR_TOKEN) {
    Write-Host "${RED}✗ SONAR_TOKEN environment variable is not set${NC}"
    Write-Host ""
    Write-Host "Set the token:"
    Write-Host "  Windows: \$env:SONAR_TOKEN = \"your-token\""
    Write-Host "  macOS/Linux: export SONAR_TOKEN=\"your-token\""
    Write-Host ""
    exit 1
}

Push-Location $ProjectRoot

try {
    Write-Host "Analyzing against SonarCloud..."
    Write-Host "Project: $projectKey"
    Write-Host "Organization: $orgKey"
    Write-Host ""

    Write-Host "${YELLOW}Initializing SonarScanner...${NC}"
    Write-Host "Command: dotnet-sonarscanner begin /k:""$projectKey"" /o:""$orgKey"" /d:sonar.token=***REDACTED*** /d:sonar.host.url=https://sonarcloud.io"
    Write-Host ""

    & dotnet-sonarscanner begin `
        /k:"$projectKey" `
        /o:"$orgKey" `
        /d:sonar.token="$env:SONAR_TOKEN" `
        /d:sonar.host.url="https://sonarcloud.io"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "${RED}✗ SonarScanner initialization failed (exit code: $LASTEXITCODE)${NC}"
        Write-Host ""
        Write-Host "Troubleshooting tips:"
        Write-Host "  1. Verify sonar-config.json values are correct"
        Write-Host "  2. Ensure SONAR_TOKEN is set and valid"
        Write-Host "  3. Check that project key and organization exist in SonarCloud"
        Write-Host "  4. Run: pwsh scripts/sonarqube-scan.ps1 -Help for configuration options"
        Write-Host ""
        Pop-Location
        exit 1
    }

    Write-Host ""
    Write-Host "${YELLOW}Building project...${NC}"
    dotnet build --configuration Release

    if ($LASTEXITCODE -ne 0) {
        Write-Host "${RED}✗ Build failed${NC}"
        Pop-Location
        exit 1
    }

    Write-Host ""
    Write-Host "${YELLOW}Uploading analysis results...${NC}"

    dotnet-sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "${RED}✗ Analysis upload failed${NC}"
        Pop-Location
        exit 1
    }

    Write-Host ""
    Write-Host "${GREEN}✓ Analysis complete${NC}"

} catch {
    Write-Host "${RED}✗ Analysis failed: $_${NC}"
    Pop-Location
    exit 1
}

Pop-Location

# ============================================================================
# Summary and Next Steps
# ============================================================================
Write-Host ""
Write-Host "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
Write-Host ""

Write-Host "${GREEN}✓ SonarCloud analysis complete!${NC}"
Write-Host ""
Write-Host "View results:"
Write-Host "  1. Visit: https://sonarcloud.io"
Write-Host "  2. Go to your organization and project"
Write-Host "  3. Review issues and quality metrics"
Write-Host ""
Write-Host "Key metrics:"
Write-Host "  • Bugs & Vulnerabilities - Critical issues"
Write-Host "  • Code Smells - Maintainability issues"
Write-Host "  • Security Hotspots - Security-sensitive code"
Write-Host "  • Coverage - Test coverage percentage"
Write-Host "  • Duplicates - Duplicate code blocks"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  • Review any issues flagged"
Write-Host "  • Fix high-priority security issues"
Write-Host "  • Improve code coverage in critical areas"
Write-Host "  • Run regularly to track improvements"
Write-Host ""

exit 0
