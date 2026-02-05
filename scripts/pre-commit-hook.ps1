#!/usr/bin/env pwsh
# Pre-commit hook for .NET projects (Cross-platform PowerShell Core)
# This hook runs code formatting before allowing commits.
# Vulnerability checks are not run here; use: pwsh scripts/check-vulnerabilities.ps1
# Works on Windows, macOS, and Linux with PowerShell Core (pwsh)

$ErrorActionPreference = "Stop"

# Colors for output (cross-platform compatible)
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    
    # PowerShell Core supports colors on all platforms
    # Windows PowerShell also supports colors
    # Only fallback to plain text if explicitly needed
    $supportedColors = @("Black", "DarkBlue", "DarkGreen", "DarkCyan", "DarkRed", "DarkMagenta", 
                          "DarkYellow", "Gray", "DarkGray", "Blue", "Green", "Cyan", "Red", 
                          "Magenta", "Yellow", "White")
    
    if ($supportedColors -contains $Color) {
        try {
            Write-Host $Message -ForegroundColor $Color
        } catch {
            # Fallback if color output fails
            Write-Host $Message
        }
    } else {
        Write-Host $Message
    }
}

# Get the repository root directory
try {
    $RepoRoot = (git rev-parse --show-toplevel)
    if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        Write-Error "Error: Could not determine repository root directory"
        exit 1
    }
    Set-Location $RepoRoot
} catch {
    Write-Error "Error: Not a git repository or git not found: $_"
    exit 1
}

Write-ColorOutput "Running pre-commit checks..." "Green"

# Step 1: Auto-format code
Write-ColorOutput "Step 1: Auto-formatting code..." "Yellow"

# Find all solution files in the repository root (.sln and .slnx)
$SolutionFiles = Get-ChildItem -Path $RepoRoot -Filter "*.sln*" -File | Where-Object { 
    $_.Extension -match "\.(sln|slnx)$" 
} | Sort-Object Name

if ($SolutionFiles.Count -eq 0) {
    Write-ColorOutput "⚠ No solution files found in repository root" "Yellow"
    Write-ColorOutput "  Skipping code formatting check" "Yellow"
} else {
    Write-Host "  Found $($SolutionFiles.Count) solution file(s) to format"
    
    $formattingFailed = $false
    $allFormatResults = @()
    
    # Normal messages to exclude from warning detection
    $normalMessages = @(
        "No files were formatted",
        "Formatting code files",
        "Formatting complete",
        "Running code formatters",
        "Determining projects to format"
    )
    
    foreach ($SolutionFile in $SolutionFiles) {
        $SolutionName = $SolutionFile.Name
        Write-Host "  Formatting: $SolutionName"
        
        try {
            # Restore packages first (required for dotnet format to work)
            Write-Host "    Restoring packages..."
            $restoreResult = dotnet restore $SolutionFile.FullName --verbosity quiet 2>&1 | Out-String
            $restoreExitCode = $LASTEXITCODE
            
            if ($restoreExitCode -ne 0) {
                Write-ColorOutput "    ✗ Package restore failed with exit code: $restoreExitCode" "Red"
                Write-Host "    $restoreResult"
                $formattingFailed = $true
                $allFormatResults += "Solution: $SolutionName`nRestore failed:`n$restoreResult"
                continue
            }
            
            # Use minimal verbosity to capture warnings and errors
            # Use --no-restore since we already restored packages above
            $formatResult = dotnet format $SolutionFile.FullName --verbosity minimal --no-restore 2>&1 | Out-String
            $formatExitCode = $LASTEXITCODE
            
            # Check for errors (non-zero exit code)
            if ($formatExitCode -ne 0) {
                Write-ColorOutput "    ✗ Failed with exit code: $formatExitCode" "Red"
                Write-Host "    $formatResult"
                $formattingFailed = $true
                $allFormatResults += "Solution: $SolutionName`n$formatResult"
                continue
            }
            
            # Check output for warnings or errors (even if exit code is 0)
            $hasWarnings = $false
            $outputLines = $formatResult -split "`n" | Where-Object { $_.Trim() -ne "" }
            
            foreach ($line in $outputLines) {
                $isNormalMessage = $false
                foreach ($normalMsg in $normalMessages) {
                    if ($line -match [regex]::Escape($normalMsg)) {
                        $isNormalMessage = $true
                        break
                    }
                }
                
                if (-not $isNormalMessage) {
                    # Check for warning/error patterns
                    if ($line -match "(warning\s+[A-Z]+\d+|error\s+[A-Z]+\d+|Failed to|Exception|Unable to|Formatting failed|Build failed)") {
                        $hasWarnings = $true
                        break
                    }
                }
            }
            
            if ($hasWarnings) {
                Write-ColorOutput "    ✗ Warnings or errors detected" "Red"
                Write-Host "    $formatResult"
                $formattingFailed = $true
                $allFormatResults += "Solution: $SolutionName`n$formatResult"
            } else {
                Write-ColorOutput "    ✓ Formatting completed" "Green"
            }
        } catch {
            Write-ColorOutput "    ✗ Formatting failed: $_" "Red"
            $formattingFailed = $true
            $allFormatResults += "Solution: $SolutionName`nError: $_"
        }
    }
    
    if ($formattingFailed) {
        Write-ColorOutput "" "Red"
        Write-ColorOutput "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" "Red"
        Write-ColorOutput "✗ COMMIT BLOCKED: Code formatting issues detected" "Red"
        Write-ColorOutput "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" "Red"
        Write-Host ""
        Write-Host "Formatting results:"
        Write-Host ($allFormatResults -join "`n`n")
        Write-Host ""
        Write-Host "Please fix the formatting issues before committing."
        Write-Host "Run 'dotnet format <solution-file>' to see detailed information."
        exit 1
    }
    
    Write-ColorOutput "✓ Code formatting completed for all solutions" "Green"
    
    # Stage any files that were auto-formatted
    try {
        $stagedFiles = git diff --cached --name-only 2>&1
        $modifiedFiles = git diff --name-only 2>&1
        
        # Check if git commands succeeded (output might be empty but that's OK)
        if ($LASTEXITCODE -eq 0) {
            # Get all modified C# files
            $allFiles = @()
            if ($stagedFiles) { $allFiles += $stagedFiles }
            if ($modifiedFiles) { $allFiles += $modifiedFiles }
            
            $formattedFiles = $allFiles | Where-Object { $_ -match '\.(cs|csproj|slnx?)$' } | Select-Object -Unique
            
            if ($formattedFiles) {
                Write-ColorOutput "Staging auto-formatted files..." "Yellow"
                $stagedCount = 0
                $formattedFiles | ForEach-Object { 
                    try {
                        # Use absolute path or relative path from repo root for cross-platform compatibility
                        $fileToStage = $_
                        if (-not [System.IO.Path]::IsPathRooted($fileToStage)) {
                            $fileToStage = Join-Path $RepoRoot $fileToStage
                        }
                        git add $fileToStage 2>&1 | Out-Null
                        if ($LASTEXITCODE -eq 0) {
                            $stagedCount++
                        }
                    } catch {
                        # Ignore individual file staging errors
                    }
                }
                if ($stagedCount -gt 0) {
                    Write-ColorOutput "✓ Auto-formatted files staged ($stagedCount file(s))" "Green"
                }
            }
        }
    } catch {
        # Ignore git staging errors - not critical
        Write-Host "  Note: Could not stage auto-formatted files: $_" -ForegroundColor Yellow
    }
}

# Vulnerability checks are not run in the pre-commit hook.
# To check for vulnerable packages run: pwsh scripts/check-vulnerabilities.ps1

Write-ColorOutput "" "Green"
Write-ColorOutput "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" "Green"
Write-ColorOutput "✓ All pre-commit checks passed" "Green"
Write-ColorOutput "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" "Green"
Write-Host ""

exit 0
