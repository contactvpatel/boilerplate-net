# Scripts Directory

This directory contains utility scripts for the project, including Git hooks for code quality and security.

## Pre-commit Hook

The pre-commit hook automatically runs code formatting before each commit. Vulnerability checks are not run in the hook; run `pwsh scripts/check-vulnerabilities.ps1` when you want to check for vulnerable packages.

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

Vulnerability checks are **not** run in the hook. To check for vulnerable packages (e.g. before a release or in CI), run:

```bash
pwsh scripts/check-vulnerabilities.ps1
```

**Note:** The hook automatically discovers all solution files in the repository, making it generic and reusable across different .NET projects.

### Features

- **Cross-platform compatibility**: Works seamlessly on Windows, macOS, and Linux
- **Shebang support**: Scripts can be executed directly on Unix-like systems (`./scripts/pre-commit-hook.ps1`)
- **Robust error handling**: Improved git repository detection and file staging with graceful error handling
- **Path normalization**: Automatic path separator normalization for maximum cross-platform compatibility
- **Automatic discovery**: Finds all solution and project files automatically - no configuration needed
- **Multi-solution support**: Handles repositories with multiple solution files
- **Smart filtering**: Excludes build artifacts, test directories, and common temporary folders

### Vulnerability check (manual or CI)

Vulnerability checks are not part of the pre-commit hook. Run them when you want (e.g. before a release) or in CI:

```bash
pwsh scripts/check-vulnerabilities.ps1
```

Exits with 0 if no vulnerable packages, 1 otherwise. Use in your pipeline to block merges when vulnerabilities are present.

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

## Database Migrations

Database schema and migrations are managed through **DbUp**, which automatically runs migrations on application startup. See the [DbUp Migrations Guide](docs/dbup-migrations.md) for details.

**Note:** All database schema changes should be made through DbUp migration scripts located in `src/WebShop.Api/DbUpMigration/Migrations/`.
