# Scripts Directory

This directory contains utility scripts for the project, including Git hooks for code quality and security.

**All primary scripts are PowerShell Core** (`.ps1`) for cross-platform compatibility on Windows, macOS, and Linux.

---

## Available Scripts

### 🔧 Development Scripts

| Script | Purpose | Runs On | Usage |
|--------|---------|---------|-------|
| **pre-commit-hook.ps1** | Auto-format code before commits | Windows, macOS, Linux | Installed via `install-pre-commit-hook.ps1` |
| **security-audit.ps1** | Scan for vulnerabilities locally | Windows, macOS, Linux | `pwsh scripts/security-audit.ps1 -Fast` |
| **sonarqube-scan.ps1** | SAST analysis with SonarQube | Windows, macOS, Linux | `pwsh scripts/sonarqube-scan.ps1` |
| **run-coverage.ps1** | Run tests with code coverage | Windows, macOS, Linux | `pwsh scripts/run-coverage.ps1` |
| **install-pre-commit-hook.ps1** | Install pre-commit hook | Windows, macOS, Linux | `pwsh scripts/install-pre-commit-hook.ps1` |
| **install-security-tools.ps1** | Install optional security tools | Windows, macOS, Linux | `pwsh scripts/install-security-tools.ps1 -All` |

---

## Pre-commit Hook

The pre-commit hook automatically runs code formatting before each commit. Vulnerability checks are not run in the hook; run `pwsh scripts/security-audit.ps1` when you want to check for vulnerable packages locally.

**Cross-platform:** Works on Windows, macOS, and Linux using PowerShell Core (pwsh).

### Prerequisites

**PowerShell Core (pwsh)** must be installed:

- **Windows**: Usually pre-installed, or install from [Microsoft Store](https://aka.ms/powershell) or [GitHub](https://github.com/PowerShell/PowerShell/releases)
- **macOS**: `brew install --cask powershell` or download from [GitHub](https://github.com/PowerShell/PowerShell/releases)
- **Linux**: Follow [installation guide](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-linux)

Verify installation:

```bash
pwsh --version
```

### Installation

**Any Platform:**

```bash
# Using PowerShell Core (recommended - works on Windows, macOS, Linux)
pwsh scripts/install-pre-commit-hook.ps1

# OR on Unix-like systems (macOS, Linux), the script has a shebang and can be executed directly:
./scripts/install-pre-commit-hook.ps1
```

**Windows (PowerShell):**

```powershell
.\scripts\install-pre-commit-hook.ps1
```

The installer automatically:

- Detects the available PowerShell command (`pwsh` or `powershell`) and configures the hook accordingly
- Sets executable permissions on Unix-like systems (macOS, Linux)
- Normalizes paths for cross-platform compatibility

### What It Does

The pre-commit hook automatically:

1. **Auto-formats code** using `dotnet format` for all solution files (`.sln`, `.slnx`) found in the repository root
2. **Stages auto-formatted files** so formatting changes are included in the commit
3. **Blocks commits** if code formatting produces warnings or errors in any solution

Vulnerability checks are **not** run in the hook. To check for vulnerable packages locally, run:

**Windows:**

```powershell
pwsh scripts/security-audit.ps1 -Fast    # Quick check (~30 seconds)
pwsh scripts/security-audit.ps1          # Full audit with credentials/secrets scan
```

**macOS/Linux:**

```bash
pwsh scripts/security-audit.ps1 -Fast    # Quick check (~30 seconds)
pwsh scripts/security-audit.ps1          # Full audit with credentials/secrets scan
```

**Note:** The hook automatically discovers all solution files in the repository, making it generic and reusable across different .NET projects. For comprehensive security scanning in CI/CD, use the infrastructure team's scanning tools.

### Features

- **Cross-platform compatibility**: Works seamlessly on Windows, macOS, and Linux
- **Shebang support**: Scripts can be executed directly on Unix-like systems (`./scripts/pre-commit-hook.ps1`)
- **Robust error handling**: Improved git repository detection and file staging with graceful error handling
- **Path normalization**: Automatic path separator normalization for maximum cross-platform compatibility
- **Automatic discovery**: Finds all solution and project files automatically - no configuration needed
- **Multi-solution support**: Handles repositories with multiple solution files
- **Smart filtering**: Excludes build artifacts, test directories, and common temporary folders

### Vulnerability check (local development)

Vulnerability checks are not part of the pre-commit hook. Run locally before pushing:

```bash
pwsh scripts/security-audit.ps1 -Fast    # Quick check - NuGet vulnerabilities only (~30 seconds)
pwsh scripts/security-audit.ps1          # Full audit - NuGet, credentials, git secrets (~1-2 minutes)
```

Exits with 0 if all checks pass, 1 otherwise. See `/docs/standards/security-scanning.md` for complete documentation.

### Bypassing the Hook

To bypass the hook (not recommended):

```bash
git commit --no-verify
```

### Manual Execution

You can manually run the pre-commit checks:

```bash
# On Unix-like systems (macOS, Linux), the script has a shebang and can be executed directly:
./scripts/pre-commit-hook.ps1

# OR using PowerShell Core explicitly (cross-platform)
pwsh scripts/pre-commit-hook.ps1

# OR on Windows with PowerShell
powershell scripts/pre-commit-hook.ps1
```

**Note:** The scripts include a `#!/usr/bin/env pwsh` shebang, making them directly executable on Unix-like systems when PowerShell Core is installed.

### Troubleshooting

**PowerShell Core not found:**

- Install PowerShell Core from [https://aka.ms/powershell](https://aka.ms/powershell)
- On macOS: `brew install --cask powershell`
- On Linux: Follow the [installation guide](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-linux)

**Hook not running:**

- Verify the hook is installed: `ls -la .git/hooks/pre-commit`
- Check if the hook is executable: `chmod +x .git/hooks/pre-commit`
- Verify PowerShell Core is available: `pwsh --version`
- Check the hook file contents to ensure it's calling the correct PowerShell script

**Path issues on Windows:**

- The installer automatically normalizes paths for cross-platform compatibility
- Paths are normalized to use forward slashes for maximum compatibility
- The hook script handles both Windows and Unix path separators automatically

**File staging issues:**

- The hook includes improved error handling for git file staging
- If staging fails, the hook will continue but show a warning
- This is non-critical - formatting will still be applied

---

## Security Tools (Optional)

### Install Optional Security Tools

One optional tool can enhance the security scanning:

**git-secrets** - Scans git history for secrets (API keys, passwords, tokens)

**Note:** NuGet vulnerability scanning is **built-in** and doesn't require installation:

- Uses: `dotnet list package --vulnerable`
- Scans all projects for known CVEs
- Always available and runs automatically

**To install optional git-secrets tool:**

**Windows/macOS/Linux:**

```bash
pwsh scripts/install-security-tools.ps1        # Interactive (prompts)
pwsh scripts/install-security-tools.ps1 -All   # Install without prompting
pwsh scripts/install-security-tools.ps1 -Help  # Show options
```

**What git-secrets does:**

| Tool | Purpose | Installation |
|------|---------|---|
| **git-secrets** | Scans git commit history for API keys, passwords, AWS credentials, tokens | Automatic via package manager (brew, apt, yum, or Chocolatey) |

**If you don't install git-secrets:**

- Security audit still works fully (all other checks run)
- Git history scanning is skipped
- NuGet vulnerability scanning always runs (built-in `dotnet list package --vulnerable`)

For detailed information and manual installation instructions, see [Security Scanning Guide](../docs/standards/security-scanning.md#optional-enhanced-security-tools).

---

## Code Coverage

### Run Tests with Code Coverage

Generate test code coverage reports:

**Windows/macOS/Linux:**

```powershell
pwsh scripts/run-coverage.ps1              # Summary report
pwsh scripts/run-coverage.ps1 -ReportType Detailed  # Detailed analysis
pwsh scripts/run-coverage.ps1 -ReportType Html      # HTML coverage report
```

**What it does:**

- Runs all tests with XPlat code coverage collection
- Uses configured exclusions from `tests/CodeCoverage.runsettings`
- Generates coverage reports in `tests/**/` directories
- Optionally generates HTML report (requires `dotnet-reportgenerator-globaltool`)

**Optional: Install Report Generator**

For HTML coverage reports:

```powershell
dotnet tool install --global dotnet-reportgenerator-globaltool
```

Then generate HTML reports:

```powershell
pwsh scripts/run-coverage.ps1 -ReportType Html
```

---

## SonarQube SAST Analysis

### Run Local Code Analysis

Local static application security testing (SAST) using SonarQube to identify code quality issues, security vulnerabilities, and technical debt:

**Windows/macOS/Linux:**

```powershell
pwsh scripts/sonarqube-scan.ps1              # Analyze against SonarCloud
pwsh scripts/sonarqube-scan.ps1 -SelfHosted  # Analyze against self-hosted SonarQube
pwsh scripts/sonarqube-scan.ps1 -Help        # Show options
```

**What it does:**

- ✓ Static code analysis (SAST) for C# and related languages
- ✓ Identifies bugs, vulnerabilities, and code smells
- ✓ Measures code coverage and duplicates
- ✓ Security hotspot detection
- ✓ Technical debt tracking
- ✓ Automatic SonarScanner CLI installation (if needed)

### Configuration

The script uses a **layered configuration approach** for maximum flexibility:

**Configuration Priority (highest to lowest):**
1. **Environment Variables** (for CI/CD pipelines)
   - `SONAR_PROJECT_KEY`: Project identifier
   - `SONAR_ORG_KEY`: SonarCloud organization key
   - `SONAR_HOST_URL`: Self-hosted server URL
   - `SONAR_TOKEN`: Authentication token

2. **sonar-config.json** (for local development)
   - Single source of truth in project root
   - Version-controlled configuration
   - Supports both SonarCloud and self-hosted modes
   - No setup needed - use defaults as-is

3. **Interactive Prompts** (fallback)
   - Script asks for missing values
   - Useful for one-off manual runs

**Example sonar-config.json:**
```json
{
  "projectKey": "com.webshop:api",
  "projectName": "WebShop API",
  "sonarCloud": {
    "enabled": true,
    "organizationKey": "your-organization-key"
  }
}
```

### Setup Options

#### Option A: SonarCloud (Recommended - Cloud-Hosted, Free)

**Best for:** Individual developers, teams wanting quick setup, cloud analysis hosting

**1. Create free SonarCloud account:**
- Visit: https://sonarcloud.io
- Sign up with GitHub/GitLab/Bitbucket
- Create organization (or use personal)

**2. Generate authentication token:**
- Go to: https://sonarcloud.io/account/security
- Click "Generate token"
- Copy the token

**3. Edit `sonar-config.json`:**
```json
{
  "projectKey": "com.webshop:api",
  "projectName": "WebShop API",
  "sonarCloud": {
    "enabled": true,
    "organizationKey": "your-organization-key"
  }
}
```
Replace `your-organization-key` with your actual SonarCloud organization key.

**4. Set SONAR_TOKEN environment variable:**

**Windows (PowerShell):**
```powershell
$env:SONAR_TOKEN = "your-token-from-sonarcloud"
```

**macOS/Linux (bash):**
```bash
export SONAR_TOKEN="your-token-from-sonarcloud"
```

**5. Run analysis:**
```powershell
pwsh scripts/sonarqube-scan.ps1
```

Script will use values from `sonar-config.json` automatically.

**6. View results:**
- Visit https://sonarcloud.io
- Open your organization → project
- Review issues, quality metrics, security hotspots

#### Option B: Self-Hosted SonarQube (Full Control, Docker)

**Best for:** Teams with sensitive code, on-premise deployments, custom configurations

**1. Start SonarQube Server (Docker):**

```bash
docker run -d \
  --name sonarqube \
  -p 9000:9000 \
  sonarqube:latest
```

**2. Access SonarQube:**
- Open: http://localhost:9000
- Default login: admin / admin (change password!)

**3. Create project and configure:**
- Click "Create project"
- Note the project key (e.g., `webshop-api`)

**4. Edit `sonar-config.json`:**
```json
{
  "projectKey": "webshop-api",
  "projectName": "WebShop API",
  "selfHosted": {
    "enabled": true,
    "hostUrl": "http://localhost:9000"
  }
}
```

**5. Run analysis:**
```powershell
pwsh scripts/sonarqube-scan.ps1 -SelfHosted
```

Script will use values from `sonar-config.json` automatically.

**6. View results:**
- Visit http://localhost:9000
- Open your project
- Review issues and metrics

### Understanding Results

| Metric | What It Measures | Action |
|--------|-----------------|--------|
| **Bugs** | Code errors → failures | Fix immediately |
| **Vulnerabilities** | Security-sensitive code | High priority |
| **Code Smells** | Maintainability issues | Improve gradually |
| **Security Hotspots** | Security review required | Manual review |
| **Coverage** | Test coverage % | Increase in critical areas |
| **Duplicates** | Repeated code blocks | Refactor DRY violations |

### Troubleshooting

**Issue: "SONAR_TOKEN not set"**
```powershell
$env:SONAR_TOKEN = "your-token"
```

**Issue: "SonarQube Server not accessible"**
```bash
docker ps | grep sonarqube
docker logs sonarqube
```

**Issue: "Build failed during analysis"**
```bash
dotnet build --configuration Release
```

### Key Takeaways

✓ **SonarScanner auto-installs** on first run
✓ **Choose your platform:** SonarCloud (quick) or self-hosted (control)
✓ **Easy 5-minute setup** to first scan
✓ **Run regularly** before major commits
✓ **Separate workflow** for focused SAST analysis
✓ **Cloud or self-hosted** results dashboard

---
