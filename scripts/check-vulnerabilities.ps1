#!/usr/bin/env pwsh
# Standalone vulnerability check for all .csproj files (for CI or manual use)
# Use in pipelines: pwsh scripts/check-vulnerabilities.ps1

$ErrorActionPreference = "Stop"

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

$ProjectFiles = Get-ChildItem -Path $RepoRoot -Filter "*.csproj" -Recurse -File | Where-Object {
    $fullPath = $_.FullName -replace '\\', '/'
    $fullPath -notmatch "/bin/|/obj/|/TestResults/|/\.vs/" -and
    $fullPath -notmatch "/node_modules/|/packages/"
} | Sort-Object FullName

$VulnerabilitiesFound = $false
foreach ($ProjectFile in $ProjectFiles) {
    $VulnOutput = dotnet list $ProjectFile.FullName package --vulnerable --include-transitive 2>&1 | Out-String
    if ($VulnOutput -notmatch "No vulnerable packages" -and $VulnOutput -notmatch "No vulnerabilities found") {
        if ($VulnOutput -match "^\s+\S+\s+\d+\.\d+") {
            Write-Host "Vulnerabilities in $($ProjectFile.BaseName):"
            Write-Host $VulnOutput
            $VulnerabilitiesFound = $true
        }
    }
}

if ($VulnerabilitiesFound) {
    Write-Host "One or more projects have vulnerable packages. Fix before merging." -ForegroundColor Red
    exit 1
}
Write-Host "No vulnerable packages found." -ForegroundColor Green
exit 0
