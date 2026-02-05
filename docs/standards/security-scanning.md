# Security Scanning - Local Development Guide

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start-5-minutes)
- [What Is This](#what-is-this)
- [What the Local Audit Detects](#what-the-local-audit-detects)
- [Using the Script](#using-the-script)
- [Daily Developer Workflow](#daily-developer-workflow)
- [Optional: Auto-Run Before Pushing](#optional-auto-run-before-pushing)
- [When Local Audit Finds Issues](#when-local-audit-finds-issues)
- [When Jenkins Blocks Your Build](#when-jenkins-blocks-your-build)
- [Troubleshooting](#troubleshooting)
- [Optional: Enhanced Security Tools](#optional-enhanced-security-tools)
- [Built-In Security Scanning](#built-in-security-scanning-always-available)
- [Helper Script: Install Optional Tools](#helper-script-install-optional-tools)
- [Common Questions](#common-questions)
- [Implementation Files](#implementation-files)
- [Key Takeaways](#key-takeaways)
- [Next Steps](#next-steps)
- [Related Documentation](#related-documentation)

---

## Overview

This guide covers optional local security scanning for developers to catch common vulnerabilities before pushing code to Git. The audit is quick (30 seconds), optional, and complements Jenkins CI/CD pipeline scanning. Developers can run it locally for early feedback on NuGet vulnerabilities, hardcoded credentials, and Git secrets without blocking their workflow.

## Quick Start (5 Minutes)

### Step 1: Ensure PowerShell Core is installed

**Windows:** Usually pre-installed, check with `pwsh --version`

**macOS:** `brew install powershell`

**Linux:** See [installation guide](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-linux)

### Step 2: Run quick audit

**Windows:**
```powershell
pwsh scripts/security-audit.ps1 -Fast
```

**macOS/Linux:**
```bash
pwsh scripts/security-audit.ps1 -Fast
```

### Step 3: Result

```
✓ Local security checks PASSED → Safe to push
✗ Issues found → Fix and try again
```

---

## What Is This?

A simple **optional** local security audit tool for developers to catch common vulnerabilities **before** pushing code to Git.

**Your workflow:**

1. Write code
2. (Optional) Run: `pwsh scripts/security-audit.ps1 -Fast`
3. Commit & push
4. Your Jenkins team handles comprehensive security scanning
5. Fix any issues Jenkins finds

This gives you early feedback locally without slowing down your workflow.

---

## What the Local Audit Detects

### ✓ Detects Locally (30 seconds)

- **NuGet package vulnerabilities** - CVEs in your dependencies
- **Hardcoded credentials** - Credit cards, API keys, passwords
- **Git secrets** - AWS keys, private tokens (if git-secrets installed)
- **Assembly version check** - Version consistency

### ⏭ Jenkins CI/CD Handles (Infrastructure)

- SonarQube SAST source code analysis
- Full code quality scanning
- Docker image vulnerability scanning
- DAST dynamic security testing
- Supply chain security analysis

---

## Using the Script

### See All Options

**Windows:**
```powershell
pwsh scripts/security-audit.ps1 -Help
```

**macOS/Linux:**
```bash
pwsh scripts/security-audit.ps1 -Help
```

### Quick Check (30 seconds)

**Windows:**
```powershell
pwsh scripts/security-audit.ps1 -Fast

Output:
✓ Local security checks PASSED
(or shows vulnerabilities found)
```

**macOS/Linux:**
```bash
pwsh scripts/security-audit.ps1 -Fast

Output:
✓ Local security checks PASSED
(or shows vulnerabilities found)
```

### Full Check (1-2 minutes)

**Windows:**
```powershell
pwsh scripts/security-audit.ps1

Checks:
  ✓ NuGet package vulnerabilities
  ✓ Hardcoded credentials scan
  ✓ Git secrets (optional)
  ✓ Assembly version check
```

**macOS/Linux:**
```bash
pwsh scripts/security-audit.ps1

Checks:
  ✓ NuGet package vulnerabilities
  ✓ Hardcoded credentials scan
  ✓ Git secrets (optional)
  ✓ Assembly version check
```

### Exit Codes

**Windows:**
```powershell
pwsh scripts/security-audit.ps1 -Fast
$LASTEXITCODE  # 0 = pass, 1 = fail
# 0 = All pass, safe to push
# 1 = Issues found, review and fix
```

**macOS/Linux:**
```bash
pwsh scripts/security-audit.ps1 -Fast
echo $?  # 0 = pass, 1 = fail
# 0 = All pass, safe to push
# 1 = Issues found, review and fix
```

---

## Daily Developer Workflow

**Windows:**
```powershell
# 1. Write code
# Edit src/WebShop.Api/Services/MyService.cs

# 2. Test locally
dotnet build
dotnet test

# 3. (Optional) Run local audit before pushing
pwsh scripts/security-audit.ps1 -Fast
# ✓ Local security checks PASSED

# 4. Commit and push
git add .
git commit -m "feat: Add new feature"
git push origin feature-branch

# 5. Jenkins automatically runs comprehensive scanning
#    (SonarQube, dependencies, Docker, DAST, etc.)
#
#    If critical issues found:
#      → Jenkins blocks merge
#      → Shows clear error message
#      → See /docs/standards/security-scanning.md for fix
#
# 6. Fix locally based on Jenkins guidance
#    Example: Update vulnerable package
dotnet add package SomePackage --version newversion
dotnet build
dotnet test

# 7. Commit and push fix
git commit -m "fix: security issue"
git push origin feature-branch

# 8. Jenkins re-runs automatically
#    Once all pass → Ready to merge!
```

**macOS/Linux:**
```bash
# 1. Write code
# Edit src/WebShop.Api/Services/MyService.cs

# 2. Test locally
dotnet build
dotnet test

# 3. (Optional) Run local audit before pushing
pwsh scripts/security-audit.ps1 -Fast
# ✓ Local security checks PASSED

# 4. Commit and push
git add .
git commit -m "feat: Add new feature"
git push origin feature-branch

# 5. Jenkins automatically runs comprehensive scanning
#    (SonarQube, dependencies, Docker, DAST, etc.)
#
#    If critical issues found:
#      → Jenkins blocks merge
#      → Shows clear error message
#      → See /docs/standards/security-scanning.md for fix
#
# 6. Fix locally based on Jenkins guidance
#    Example: Update vulnerable package
dotnet add package SomePackage --version newversion
dotnet build
dotnet test

# 7. Commit and push fix
git commit -m "fix: security issue"
git push origin feature-branch

# 8. Jenkins re-runs automatically
#    Once all pass → Ready to merge!
```

---

## Optional: Auto-Run Before Pushing

Set up git pre-push hook to automatically run audit before each push:

**Windows:**
```powershell
# Create hook file
@"
#!/usr/bin/env pwsh
pwsh scripts/security-audit.ps1 -Fast
exit `$LASTEXITCODE
"@ | Set-Content -Path .git/hooks/pre-push -Encoding UTF8

# Make executable (PowerShell doesn't need this on Windows)
```

**macOS/Linux:**
```bash
# Create hook file
cat > .git/hooks/pre-push << 'EOF'
#!/usr/bin/env pwsh
pwsh scripts/security-audit.ps1 -Fast
exit $LASTEXITCODE
EOF

# Make executable
chmod +x .git/hooks/pre-push
```

Now audit runs automatically on `git push` (~30 seconds).

**Skip if needed:**

```bash
git push --no-verify
```

---

## When Local Audit Finds Issues

### Example: Vulnerable Package

**Windows:**
```powershell
pwsh scripts/security-audit.ps1 -Fast

Critical: System.Net.Http 4.3.0 has CVE-2018-0765
Action: Update packages in the CI/CD pipeline
```

**macOS/Linux:**
```bash
pwsh scripts/security-audit.ps1 -Fast

Critical: System.Net.Http 4.3.0 has CVE-2018-0765
Action: Update packages in the CI/CD pipeline
```

**Fix (both Windows & Linux):**

```bash
# Update package
dotnet add package System.Net.Http --version 4.3.1

# Test locally
dotnet build
dotnet test

# Commit and push
git commit -m "fix: Update System.Net.Http for CVE-2018-0765"
git push
```

### Example: Hardcoded Credentials Pattern

**Windows:**
```powershell
pwsh scripts/security-audit.ps1

⚠ Possible hardcoded credential found:
  appsettings.json line 42: password="hardcoded-value"

Review: Use Vault/Key Vault for production credentials
```

**macOS/Linux:**
```bash
pwsh scripts/security-audit.ps1

⚠ Possible hardcoded credential found:
  appsettings.json line 42: password="hardcoded-value"

Review: Use Vault/Key Vault for production credentials
```

**Fix (both Windows & Linux):**

- Move to environment variables or secrets management
- Never commit secrets to Git
- Use: `dotnet user-secrets` for local development

---

## When Jenkins Blocks Your Build

Jenkins performs deeper scanning than local audit. If Jenkins finds issues:

1. **Read the error** in Jenkins console output
2. **Understand the issue** (see table below)
3. **Fix locally** (example commands below)
4. **Push fix** (Jenkins re-runs automatically)
5. **Merge when passing**

### Common Jenkins Issues & Fixes

| Issue | Example | Fix |
|-------|---------|-----|
| **Vulnerable package** | CVE-2021-XXXXX in Newtonsoft.Json | `dotnet add package Newtonsoft.Json --version X.X.X` |
| **Code coverage too low** | Coverage 45% < 80% target | Write more unit tests for new code |
| **Security hotspot unreviewed** | SonarQube hotspot P1 | Review code in SonarQube dashboard, mark reviewed |
| **Docker vulnerability** | Alpine base image has CVE | Update base image in Dockerfile |
| **Missing security header** | No HSTS header | Verify SecurityHeaders middleware enabled |

---

## Troubleshooting

### Issue: "dotnet CLI not found"

**Fix:**

```bash
# Install from https://dotnet.microsoft.com/download
dotnet --version

# Reinstall if needed
```

### Issue: "PowerShell Core not found"

**Fix:**

- **Windows:** Usually pre-installed, or download from [PowerShell GitHub](https://github.com/PowerShell/PowerShell)
- **macOS:** `brew install powershell`
- **Linux:** See [installation guide](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-linux)

Verify installation:
```bash
pwsh --version
```

### Issue: Pre-push hook not running

**Fix:**

**Windows:**
```powershell
# Test hook manually
.git/hooks/pre-push

# Check exit code
$LASTEXITCODE
```

**macOS/Linux:**
```bash
# Verify hook is executable
chmod +x .git/hooks/pre-push

# Test manually
.git/hooks/pre-push

# Check exit code
echo $?
```

### Issue: "Local audit passed but Jenkins failed"

**Reason:**
Jenkins performs much deeper scanning (SonarQube SAST, code quality, Docker, etc.). Local audit is just quick early feedback.

**Solution:**
See Jenkins console output for details. Check `/docs/standards/security-scanning.md` for how to fix common issues.

---

## Optional: Enhanced Security Tools

The local audit script includes **built-in** vulnerability scanning. Additional optional tools can enhance it:

### 1. Git Secrets - Detect Secrets in Git History

**What it does:** Prevents secrets (API keys, passwords, tokens) from being committed to Git

**When to install:** If your team uses Git and wants to catch secret leaks before they happen

**Installation:**

**Windows:**
```powershell
# Using Chocolatey
choco install git-secrets

# OR manually from GitHub
# https://github.com/awslabs/git-secrets
```

**macOS:**
```bash
brew install git-secrets
```

**Linux (Ubuntu/Debian):**
```bash
apt-get install git-secrets
```

**Linux (Fedora/RHEL):**
```bash
dnf install git-secrets
```

**Verify installation:**
```bash
git-secrets --version
```

**Usage:**

After installing, the security audit script will automatically:
```bash
pwsh scripts/security-audit.ps1
# [3/4] Checking Git history for secrets...
# ✓ No secrets found in git
```

---

## Built-In Security Scanning (Always Available)

These checks run in the security audit automatically, no installation needed:

### NuGet Vulnerability Scanning

**What it does:** Scans all NuGet packages for known CVEs using the built-in dotnet CLI

**Command:** `dotnet list package --vulnerable --include-transitive`

**Features:**
- Checks all .csproj projects in the repository
- Includes transitive dependencies
- Output: `[1/4] Checking NuGet package vulnerabilities`

**To run manually:**
```bash
dotnet list package --vulnerable --include-transitive
```

---

### Hardcoded Credentials Detection

**What it does:** Scans source code for patterns that look like secrets (passwords, API keys, tokens)

**Files scanned:**
- `*.json` - Configuration files
- `*.config` - Application config files
- `*.cs` - C# source files

**Excluded files:**
- `appsettings.Development.json`
- Files with `.example` or `.template` suffix

**Output:** `[2/4] Scanning for hardcoded credentials...`

---

## Helper Script: Install Optional Tools

**Optional:** Use this script to install optional security tools:

**Windows:**
```powershell
pwsh scripts/install-security-tools.ps1
```

**macOS/Linux:**
```bash
pwsh scripts/install-security-tools.ps1
```

This script will:
- ✓ Check your platform (Windows/macOS/Linux)
- ✓ Install git-secrets using your system package manager
- ✓ Verify installation
- ✓ Show usage instructions

You can also install tools individually using the instructions above.

---

## Common Questions

### Q: Do I have to run the local audit?

**A:** No, it's optional. Jenkins handles comprehensive scanning. Use local audit for early feedback if helpful.

### Q: Will the pre-push hook slow down my pushes?

**A:** ~30 seconds added. You can skip with `git push --no-verify` if in a hurry.

### Q: Can I bypass Jenkins security checks?

**A:** No. Jenkins blocks critical issues on main branch merge (cannot be bypassed).

### Q: Why did my local audit pass but Jenkins blocked?

**A:** Jenkins is more comprehensive:

- Local: Quick CVE check only
- Jenkins: SAST code analysis, quality gates, Docker, DAST

### Q: Does this work on Windows/Mac/Linux?

**A:** Yes, PowerShell Core (pwsh) runs on all platforms with .NET SDK pre-installed.

### Q: What if I don't have .NET SDK?

**A:** Install from <https://dotnet.microsoft.com/download>

### Q: Can I see what the script does?

**A:** Yes, view it: `cat scripts/security-audit.ps1`

---

## Implementation Files

```
/scripts/security-audit.ps1
  └─ Executable script for developers (PowerShell Core, cross-platform)
     Run before pushing code to catch vulnerabilities early

/docs/standards/security-scanning.md
  └─ This file - complete developer guide (Windows, macOS, Linux)
```

---

## Key Takeaways

✓ **Optional:** Local audit is not required, just helpful
✓ **Fast:** ~30 seconds for quick check
✓ **Jenkins Blocks:** Critical issues cannot bypass Jenkins on main branch
✓ **Early Feedback:** Catch issues before Jenkins
✓ **Clear Guidance:** Jenkins shows what to fix and how
✓ **Simple:** One command to run

---

## Next Steps

1. **Install PowerShell Core** (if not already installed):
   - Windows: `pwsh --version` or download from [PowerShell GitHub](https://github.com/PowerShell/PowerShell)
   - macOS: `brew install powershell`
   - Linux: See [installation guide](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-linux)

2. **Try it:** `pwsh scripts/security-audit.ps1 -Fast`

3. **(Optional) Set up pre-push hook** for automatic checks before pushing

4. **Use regularly** before pushing code to catch issues early

---

## Related Documentation

### Related Docs in This Repository

- **[Security Hardening Standards](security-hardening.md)** - Mandatory encryption, authentication, and data protection rules
- **[Exception Handling Guide](../architecture/exception-handling.md)** - How security errors are logged and handled
- **[Rate Limiting Guide](../architecture/rate-limiting.md)** - Preventing abuse and DoS attacks

### Microsoft Learn References

- **[PowerShell on Different Platforms](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell)** - Installation across Windows, macOS, and Linux
- **[.NET Security Best Practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/security/)** - Microsoft's security guidelines for .NET applications
- **[NuGet Package Security](https://learn.microsoft.com/en-us/nuget/concepts/security-best-practices)** - Securing dependency management in .NET

### Industry & Standards References

- **[OWASP Dependency Check](https://owasp.org/www-project-dependency-check/)** - Understanding vulnerability scanning for open source components
- **[OWASP Top 10](https://owasp.org/www-project-top-ten/)** - Common web application security risks the local audit helps catch
- **[CWE-798: Use of Hardcoded Credentials](https://cwe.mitre.org/data/definitions/798.html)** - Why hardcoded credential detection matters
- **[Git Secrets GitHub Repository](https://github.com/awslabs/git-secrets)** - Documentation for the optional git-secrets tool

---

**Status:** ✓ Ready to use (PowerShell Core, Windows/macOS/Linux)
**Audience:** Developers
**Support:** Run `pwsh scripts/security-audit.ps1 -Help` for script options
