#!/usr/bin/env pwsh
<#
.SYNOPSIS
Run unit or integration tests, optionally with code coverage.

.DESCRIPTION
Single script to run either unit tests or integration tests.
Integration tests run sequentially (shared webshop_test DB).

.PARAMETER TestType
Unit or Integration.

.PARAMETER CollectCoverage
If set, collects XPlat code coverage (writes coverage.cobertura.xml under tests/.../TestResults).

.EXAMPLE
pwsh scripts/run-tests.ps1 -TestType Unit
pwsh scripts/run-tests.ps1 -TestType Integration
pwsh scripts/run-tests.ps1 -TestType Unit -CollectCoverage
pwsh scripts/run-tests.ps1 -TestType Integration -CollectCoverage
#>

param(
    [ValidateSet('Unit', 'Integration')]
    [string]$TestType = 'Unit',

    [switch]$CollectCoverage
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot/..

if ($TestType -eq 'Unit') {
    Write-Host "Running unit tests..."
    if ($CollectCoverage) {
        Write-Host "Coverage: tests/CodeCoverage.runsettings"
        dotnet test tests/WebShop.UnitTests/WebShop.UnitTests.csproj `
            --filter "Category=Unit" `
            --settings tests/CodeCoverage.runsettings `
            --collect:"XPlat Code Coverage" `
            --verbosity normal
    } else {
        dotnet test tests/WebShop.UnitTests/WebShop.UnitTests.csproj `
            --filter "Category=Unit" `
            --verbosity normal
    }
} else {
    Write-Host "Running integration tests (sequential)..."
    if ($CollectCoverage) {
        dotnet test tests/WebShop.IntegrationTests/WebShop.IntegrationTests.csproj `
            --filter "Category=Integration" `
            --settings tests/runsettings.integration.xml `
            --collect:"XPlat Code Coverage" `
            --verbosity normal
    } else {
        dotnet test tests/WebShop.IntegrationTests/WebShop.IntegrationTests.csproj `
            --filter "Category=Integration" `
            --settings tests/runsettings.integration.xml `
            --verbosity normal
    }
}

if ($LASTEXITCODE -ne 0) {
    exit 1
}

if ($CollectCoverage) {
    $dir = if ($TestType -eq 'Unit') { 'WebShop.UnitTests' } else { 'WebShop.IntegrationTests' }
    $files = @(Get-ChildItem -Path "tests" -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match $dir })
    Write-Host ""
    Write-Host "Coverage file(s):"
    $files | Select-Object -First 3 | ForEach-Object { Write-Host "  $($_.FullName)" }
    Write-Host ""
    Write-Host "Generate HTML report: pwsh scripts/run-coverage.ps1 -TestType $TestType -ReportType Html"
}
