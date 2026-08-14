[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$plan = & (Join-Path $PSScriptRoot 'Get-LlmWikiCoveragePlan.ps1') `
    -ProposedPath 'tests/FoodDiary.Application.Tests/Admin/UserAdministrationMutationServiceTests.cs' `
    -Query 'cover the currently uncovered branches' `
    -Format Json | ConvertFrom-Json

if ($plan.scope.testProject -ne 'tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj') { throw 'Coverage plan inferred the wrong test project.' }
if ($plan.commands.dotCover -notmatch 'target-working-directory' -or $plan.commands.dotCover -notmatch 'FullyQualifiedName~') { throw 'dotCover plan is missing its reproducible working directory or focused test filter.' }
if ($plan.commands.xplatCoverage -notmatch 'coverage\.runsettings') { throw 'XPlat coverage fallback is missing repository settings.' }
if ($plan.scope.integration) { throw 'Unit-test coverage plan was incorrectly classified as integration coverage.' }

Write-Host 'LLM Wiki coverage plan regression passed.'
