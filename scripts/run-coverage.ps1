#!/usr/bin/env pwsh
<#
.SYNOPSIS
Run tests with code coverage and optionally generate an HTML report.

.DESCRIPTION
Runs unit and/or integration tests with XPlat code coverage, then optionally
generates an HTML report using ReportGenerator.

.PARAMETER TestType
Unit: run unit tests with coverage (tests/CodeCoverage.runsettings).
Integration: run integration tests with coverage (sequential).
All: run Unit then Integration with coverage, then merge into one report.

.PARAMETER ReportType
Summary: run tests with coverage and show where coverage files were written.
Html: run tests with coverage, then generate HTML report (requires dotnet-reportgenerator-globaltool).

.EXAMPLE
pwsh scripts/run-coverage.ps1 -TestType Unit
pwsh scripts/run-coverage.ps1 -TestType Unit -ReportType Html
pwsh scripts/run-coverage.ps1 -TestType Integration -ReportType Html
pwsh scripts/run-coverage.ps1 -TestType All -ReportType Html
#>

param(
    [ValidateSet('Unit', 'Integration', 'All')]
    [string]$TestType = 'Unit',

    [ValidateSet('Summary', 'Html')]
    [string]$ReportType = 'Summary'
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot/..
$scriptDir = $PSScriptRoot

# Run tests with coverage
if ($TestType -eq 'All') {
    & "$scriptDir/run-tests.ps1" -TestType Unit -CollectCoverage
    if ($LASTEXITCODE -ne 0) { exit 1 }
    & "$scriptDir/run-tests.ps1" -TestType Integration -CollectCoverage
    if ($LASTEXITCODE -ne 0) { exit 1 }
} else {
    & "$scriptDir/run-tests.ps1" -TestType $TestType -CollectCoverage
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

# Generate HTML report if requested
if ($ReportType -eq 'Html') {
    $allCoverage = @(Get-ChildItem -Path "tests" -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue)
    $coveragePaths = switch ($TestType) {
        'Unit'        { $allCoverage | Where-Object { $_.FullName -match 'WebShop\.UnitTests' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1 }
        'Integration' { $allCoverage | Where-Object { $_.FullName -match 'WebShop\.IntegrationTests' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1 }
        'All'         {
            $u = $allCoverage | Where-Object { $_.FullName -match 'WebShop\.UnitTests' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            $i = $allCoverage | Where-Object { $_.FullName -match 'WebShop\.IntegrationTests' } | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            @($u, $i) | Where-Object { $_ }
        }
    }
    $coveragePaths = @($coveragePaths)

    if ($coveragePaths.Count -eq 0) {
        Write-Error "No coverage files found for TestType=$TestType."
        exit 1
    }

    $OutputDir = switch ($TestType) {
        'Unit'        { 'coverage-report-unit' }
        'Integration' { 'coverage-report-integration' }
        'All'         { 'coverage-report' }
    }
    $reportsArg = ($coveragePaths | Select-Object -ExpandProperty FullName) -join ';'

    $reportGeneratorPath = Get-Command reportgenerator -ErrorAction SilentlyContinue
    if (-not $reportGeneratorPath) {
        Write-Error "reportgenerator not found. Install with: dotnet tool install --global dotnet-reportgenerator-globaltool"
        exit 1
    }

    Write-Host ""
    Write-Host "Generating HTML report ($($coveragePaths.Count) file(s))..."
    reportgenerator "-reports:$reportsArg" -targetdir:$OutputDir -reporttypes:Html
    Write-Host "Report: $OutputDir/index.html"
}
