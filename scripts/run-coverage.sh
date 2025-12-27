#!/bin/bash
# Script to run tests with code coverage using configured exclusions

set -e

echo "Running tests with code coverage..."
echo "Using configuration: tests/CodeCoverage.runsettings"
echo ""

dotnet test \
  --settings tests/CodeCoverage.runsettings \
  --collect:"XPlat Code Coverage" \
  --verbosity normal

echo ""
echo "Coverage reports generated in:"
find tests -name "coverage.cobertura.xml" -type f | head -3

echo ""
echo "To view detailed coverage, use:"
echo "  dotnet reportgenerator -reports:'tests/**/coverage.cobertura.xml' -targetdir:coverage-report -reporttypes:Html"
