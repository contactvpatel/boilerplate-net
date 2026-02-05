#!/usr/bin/env pwsh
<#
.SYNOPSIS
Run tests with code coverage analysis

.DESCRIPTION
Runs all tests with cross-platform code coverage collection using configured exclusions.
Generates coverage reports in XPlat format.

.PARAMETER ReportType
The type of report to generate. Options: Summary, Detailed, Html
Default: Summary

.EXAMPLE
./scripts/run-coverage.ps1              # Run with default settings
pwsh scripts/run-coverage.ps1           # Cross-platform execution
./scripts/run-coverage.ps1 -ReportType Detailed   # Detailed report
./scripts/run-coverage.ps1 -ReportType Html       # HTML coverage report

.NOTES
Requires:
- .NET SDK installed
- CodeCoverage.runsettings configured in tests directory
#>

param(
    [ValidateSet('Summary', 'Detailed', 'Html')]
    [string]$ReportType = 'Summary'
)

$ErrorActionPreference = "Stop"

Write-Host "Running tests with code coverage..."
Write-Host "Report type: $ReportType"
Write-Host "Using configuration: tests/CodeCoverage.runsettings"
Write-Host ""

# Run tests with code coverage
try {
    dotnet test `
        --settings tests/CodeCoverage.runsettings `
        --collect:"XPlat Code Coverage" `
        --verbosity normal

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Test execution failed with exit code $LASTEXITCODE"
        exit 1
    }
} catch {
    Write-Error "Failed to run tests: $_"
    exit 1
}

Write-Host ""
Write-Host "Coverage reports generated in:"

# Find and display coverage files
$coverageFiles = @(Get-ChildItem -Path "tests" -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 3)

if ($coverageFiles) {
    $coverageFiles | ForEach-Object { Write-Host "  $($_.FullName)" }
} else {
    Write-Host "  (No coverage files found)"
}

Write-Host ""

# Show next steps based on report type
switch ($ReportType) {
    'Summary' {
        Write-Host "Summary report displayed above."
        Write-Host ""
        Write-Host "To view detailed coverage, use:"
        Write-Host "  dotnet reportgenerator -reports:'tests/**/coverage.cobertura.xml' -targetdir:coverage-report -reporttypes:Html"
        Write-Host ""
        Write-Host "Then open coverage-report/index.html in your browser."
    }
    'Detailed' {
        Write-Host "To generate detailed HTML report, install reportgenerator:"
        Write-Host "  dotnet tool install --global dotnet-reportgenerator-globaltool"
        Write-Host ""
        Write-Host "Then generate the report:"
        Write-Host "  dotnet reportgenerator -reports:'tests/**/coverage.cobertura.xml' -targetdir:coverage-report -reporttypes:Html"
    }
    'Html' {
        Write-Host "Generating HTML coverage report..."

        # Check if reportgenerator is installed
        try {
            $reportGeneratorPath = Get-Command reportgenerator -ErrorAction SilentlyContinue
            if ($reportGeneratorPath) {
                Write-Host "Using reportgenerator: $($reportGeneratorPath.Source)"
                reportgenerator -reports:"tests/**/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:Html
                Write-Host ""
                Write-Host "✓ HTML report generated at coverage-report/index.html"
                Write-Host ""

                # Try to open the report in default browser
                $reportPath = Join-Path (Get-Location) "coverage-report" "index.html"
                if (Test-Path $reportPath) {
                    Write-Host "Opening report in browser..."
                    if ($IsWindows -or -not (Get-Variable -Name IsWindows -ErrorAction SilentlyContinue)) {
                        # Windows
                        Start-Process $reportPath
                    } elseif ($IsMacOS) {
                        # macOS
                        & open $reportPath
                    } elseif ($IsLinux) {
                        # Linux
                        & xdg-open $reportPath
                    }
                }
            } else {
                Write-Warning "reportgenerator not found. Install with:"
                Write-Host "  dotnet tool install --global dotnet-reportgenerator-globaltool"
                Write-Host ""
                Write-Host "Then run again:"
                Write-Host "  pwsh scripts/run-coverage.ps1 -ReportType Html"
            }
        } catch {
            Write-Warning "Could not generate HTML report: $_"
            Write-Host "Install reportgenerator:"
            Write-Host "  dotnet tool install --global dotnet-reportgenerator-globaltool"
        }
    }
}
