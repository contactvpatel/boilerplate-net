# SonarQube SAST Setup Guide

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Architecture: SonarScanner + SonarCloud](#architecture-sonarscanner--sonarcloud)
- [SonarCloud Setup](#sonarcloud-setup)
- [Understanding Results](#understanding-results)
- [Integration with CI/CD](#integration-with-cicd)
- [Troubleshooting](#troubleshooting)
- [Related Documentation](#related-documentation)

---

## Overview

This guide covers setting up **SonarQube SAST (Static Application Security Testing)** for local developer analysis using **SonarScanner CLI with SonarCloud**. Local SAST scanning identifies code quality issues, security vulnerabilities, and technical debt before pushing code to Git.

SonarCloud is a cloud-hosted solution (free tier available) that is recommended for local development—no server infrastructure to manage, quick setup, and works from anywhere.

Both the CLI scan and dashboard use the same simple `sonarqube-scan.ps1` PowerShell script.

---

## Architecture: SonarScanner + SonarCloud

The workflow is simple:

```
Developer Machine
├── Run: pwsh scripts/sonarqube-scan.ps1
├── Executes: SonarScanner CLI
├── Scans: src/ directory
└── Reports: Uploads to ↓

SonarCloud (Cloud-Hosted)
├── Stores: Project metadata
├── Stores: Analysis results
├── Provides: Web UI dashboard
└── URL: sonarcloud.io
```

**Key Points:**

- **SonarScanner** runs locally (lightweight CLI tool)
- **Analysis** happens on your machine (uses your CPU)
- **Results** are uploaded to SonarCloud
- **Dashboard** available at sonarcloud.io

---

## SonarCloud Setup

**For local development:** SonarCloud provides a cloud-hosted solution with no server infrastructure needed. Free tier is available for open-source and public projects.

**Advantages:**

- ✅ No server to manage or maintain
- ✅ Free tier for open source and public projects
- ✅ Works from anywhere (cloud-hosted)
- ✅ Automatic updated rules and patterns
- ✅ 5-minute setup

### Step 1: Create SonarCloud Account

1. Visit <https://sonarcloud.io>
2. Click "Sign up" or "Login with GitHub/GitLab/Bitbucket"
3. Authorize SonarCloud to access your account
4. Create or use existing organization

**First time?** Create a new organization:

- Organization key: `my-organization` (lowercase, no spaces)
- Display name: `My Organization`

### Step 2: Generate Authentication Token

Authentication tokens allow SonarScanner to upload results securely.

1. Visit: <https://sonarcloud.io/account/security>
   - Or: Profile (top-right) → Security
2. Click "Generate token"
3. Give it a name: `Local Development`
4. Set expiration: Optional (90 days recommended)
5. Click "Generate"
6. **Copy the token** (displayed once, can't be seen again)

**Example token:** `sqc_1234567890abcdef1234567890abcdef12345678`

### Step 3: Create Project on SonarCloud

1. Log in to <https://sonarcloud.io>
2. Click "+" → "Create project"
3. Fill in details:
   - **Organization:** Select your organization
   - **Project key:** `com.webshop:api` (matches sonar-project.properties)
   - **Project name:** `WebShop API`
   - **Visibility:** Public (free tier) or Private (paid/qualifying projects)
4. Click "Create"

### Step 4: Update sonar-project.properties

Edit the file in project root:

```properties
# Project Identity
sonar.projectKey=com.webshop:api
sonar.projectName=WebShop API
sonar.projectVersion=1.0.0

# Source code
sonar.sources=src
sonar.tests=tests
sonar.language=cs
sonar.sourceEncoding=UTF-8

# SonarCloud Organization (REQUIRED)
sonar.organization=your-organization-key
```

**Replace `your-organization-key`** with your actual organization key from SonarCloud.

### Step 5: Set Environment Variable

**Windows (PowerShell):**

```powershell
# Set for current session
$env:SONAR_TOKEN = "sqc_1234567890abcdef1234567890abcdef12345678"

# Verify it's set
echo $env:SONAR_TOKEN
```

**macOS/Linux:**

```bash
# Set for current session
export SONAR_TOKEN="sqc_1234567890abcdef1234567890abcdef12345678"

# Verify it's set
echo $SONAR_TOKEN

# Make permanent (add to ~/.bash_profile or ~/.zshrc)
echo 'export SONAR_TOKEN="sqc_1234567890abcdef1234567890abcdef12345678"' >> ~/.bash_profile
source ~/.bash_profile
```

**Important:** Store token securely. Treat it like a password - don't commit it to Git!

### Step 6: Run First Analysis

```powershell
pwsh scripts/sonarqube-scan.ps1
```

**Output will show:**

```
[1/4] Checking SonarScanner installation...
✓ SonarScanner found
[2/4] Checking SonarQube configuration...
✓ sonar-project.properties found
✓ Configuration valid
[3/4] Checking authentication...
✓ SONAR_TOKEN is set
[4/4] Running SonarQube analysis...
Analyzing against SonarCloud...
Building project...
✓ Build successful
Uploading analysis results...
✓ Analysis complete
```

### Step 7: View Results

1. Log in to <https://sonarcloud.io>
2. Open your organization
3. Click your project
4. Review:
   - **Issues:** Bugs, vulnerabilities, code smells
   - **Security Hotspots:** Code that needs security review
   - **Coverage:** Test coverage percentage
   - **Duplicates:** Repeated code blocks
   - **Metrics:** Quality metrics and trends

---

## WebShop Project Configuration

This project uses a `sonar-config.json` file for configuration management. The script reads this configuration automatically:

**Configuration File:** `sonar-config.json` (project root)

**Current WebShop Configuration:**

```json
{
  "projectKey": "86f3bb56-974d-4a8f-becd-6e4a030e5d4a_webshop",
  "projectName": "WebShop API",
  "projectVersion": "1.0.0",
  "sonarCloud": {
    "enabled": true,
    "organizationKey": "86f3bb56-974d-4a8f-becd-6e4a030e5d4a",
    "hostUrl": "https://sonarcloud.io"
  },
  "source": {
    "encoding": "utf-8",
    "language": "cs",
    "inclusions": ["src/**"],
    "exclusions": [
      "**/bin/**",
      "**/obj/**",
      "**/test/**",
      "**/*.Test.cs",
      "**/*Tests.cs"
    ]
  }
}
```

**Environment Variable Override Priority:**

The script uses this priority (highest to lowest):

1. **Environment Variables:** `SONAR_PROJECT_KEY`, `SONAR_ORG_KEY`, `SONAR_TOKEN`
2. **Configuration File:** `sonar-config.json` (if values not set via env vars)
3. **Interactive Prompts:** User input (if both above unavailable)

**For WebShop development, you only need to set:**

```powershell
$env:SONAR_TOKEN = "your-sonarcloud-token-here"
```

The script will automatically use the project key and organization from `sonar-config.json`.

## Understanding Results

After analysis completes, SonarQube displays:

### Issue Types

| Type | Severity | Example | Action |
|------|----------|---------|--------|
| **Bug** | Critical | Null pointer dereference | Fix immediately |
| **Vulnerability** | Critical | SQL injection risk | Fix immediately |
| **Code Smell** | Low→Medium | Long method, duplicate code | Fix gradually |
| **Security Hotspot** | High | Cryptography, authentication | Manual review |

### Quality Metrics

| Metric | What It Means | Target |
|--------|---------------|--------|
| **Reliability** | Bug density | A grade |
| **Security** | Vulnerability density | A grade |
| **Maintainability** | Technical debt / code smells | A grade |
| **Coverage** | Test coverage % | 80%+ |
| **Duplications** | Duplicate lines % | <3% |

### Quality Gates

Automatic pass/fail rules for code quality (configurable):

- No critical bugs
- No critical vulnerabilities
- Coverage >80%
- Technical debt <5%

---

## Integration with CI/CD

### Jenkins Pipeline Example

```groovy
pipeline {
    agent any

    stages {
        stage('SonarQube SAST') {
            steps {
                withCredentials([string(credentialsId: 'sonar-token', variable: 'SONAR_TOKEN')]) {
                    sh '''
                        dotnet-sonarscanner begin \
                            /k:"com.webshop:api" \
                            /o:"your-organization" \
                            /d:sonar.token="${SONAR_TOKEN}" \
                            /d:sonar.host.url="https://sonarcloud.io"

                        dotnet build --configuration Release

                        dotnet-sonarscanner end \
                            /d:sonar.token="${SONAR_TOKEN}"
                    '''
                }
            }
        }
    }
}
```

---

## Troubleshooting

### Issue: "SONAR_TOKEN environment variable not set"

**Cause:** Token not provided to SonarScanner

**Fix:**

```powershell
# Set token
$env:SONAR_TOKEN = "your-token"

# Verify it's set
echo $env:SONAR_TOKEN

# Run again
pwsh scripts/sonarqube-scan.ps1
```

### Issue: "Analyzing … sonar.organization not found"

**Cause:** Wrong organization key in sonar-project.properties

**Fix:**

```properties
# Check your SonarCloud organization key:
# https://sonarcloud.io/organizations
sonar.organization=your-actual-org-key
```

### Issue: "Build failed during analysis"

**Cause:** Project doesn't compile

**Fix:**

```bash
# Test build locally first
dotnet build --configuration Release

# If build fails, fix it before running analysis
```

### Issue: "Analysis takes very long (>10 minutes)"

**Cause:** Large project, slow network, or server overload

**Solutions:**

- Run on faster machine
- Check network connectivity (for SonarCloud)
- Reduce scope temporarily
- Wait for server to finish other builds

### Issue: "Token expired or invalid"

**Cause:** SonarCloud token has expired

**Fix:**

```
1. Visit https://sonarcloud.io/account/security
2. Generate new token
3. Update $env:SONAR_TOKEN
4. Run analysis again
```

---

## Related Documentation

### Related Docs in This Repository

- **[Security Scanning Guide](../standards/security-scanning.md)** - Local vulnerability scanning (dependency checks, credentials)
- **[Security Hardening](../standards/security-hardening.md)** - Security standards and best practices
- **[Code Quality Standards](../standards/code-quality.md)** - Code quality requirements

### Microsoft Learn References

- **[SonarScanner for .NET](https://docs.sonarqube.org/latest/analysis/scan/sonarscanner-for-msbuild/)** - Official SonarScanner documentation for .NET
- **[Code Analysis in .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)** - Microsoft's built-in .NET code analysis
- **[.NET Security Best Practices](https://learn.microsoft.com/en-us/dotnet/fundamentals/security/)** - Microsoft security guidelines

### Industry & Standards References

- **[SonarCloud](https://sonarcloud.io)** - Cloud-hosted SonarQube for open source and private projects
- **[CWE Top 25](https://cwe.mitre.org/top25/)** - Top security weaknesses SonarQube detects
- **[OWASP Top 10](https://owasp.org/www-project-top-ten/)** - Web application security risks

---
