[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$plan = & (Join-Path $PSScriptRoot 'Get-LlmWikiCoveragePlan.ps1') `
    -ProposedPath 'Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/Admin/UserAdministrationMutationServiceTests.cs' `
    -Query 'cover the currently uncovered branches' `
    -Format Json | ConvertFrom-Json

if ($plan.scope.testProject -ne 'Modules/Admin/tests/FoodDiary.Modules.Admin.Application.Tests/FoodDiary.Modules.Admin.Application.Tests.csproj') { throw 'Coverage plan inferred the wrong test project.' }
if ($plan.commands.dotCover -notmatch 'target-working-directory' -or $plan.commands.dotCover -notmatch 'FullyQualifiedName~') { throw 'dotCover plan is missing its reproducible working directory or focused test filter.' }
if ($plan.commands.xplatCoverage -notmatch 'coverage\.runsettings') { throw 'XPlat coverage fallback is missing repository settings.' }
if ($plan.scope.integration) { throw 'Unit-test coverage plan was incorrectly classified as integration coverage.' }

Write-Host 'LLM Wiki coverage plan regression passed.'

$clientRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../FoodDiary.Web.Client')).Path
foreach ($entry in @(@('src/app', 'app'), @('projects/fooddiary-admin', 'admin'), @('projects/fd-ui-kit', 'ui-kit'), @('projects/fd-tour', 'tour'))) {
    $file = @(Get-ChildItem -LiteralPath (Join-Path $clientRoot $entry[0]) -Recurse -File -Filter '*.spec.ts' | Select-Object -First 1)[0]
    $relative = 'FoodDiary.Web.Client/' + $file.FullName.Substring($clientRoot.Length + 1).Replace('\', '/')
    $frontend = & (Join-Path $PSScriptRoot 'Get-LlmWikiCoveragePlan.ps1') -ProposedPath $relative -Format Json | ConvertFrom-Json
    if ($frontend.commands.coverage -notmatch ('npm run test:coverage:' + [regex]::Escape($entry[1]) + ' -- --include=')) { throw "Frontend coverage selected the wrong script for '$relative'." }
    if ($frontend.commands.focusedTest -notmatch ('npm run test:ci:' + [regex]::Escape($entry[1]) + ' -- --include=')) { throw 'Frontend coverage invented a focused script.' }
    if ($frontend.commands.PSObject.Properties['dotCover']) { throw 'Frontend coverage incorrectly requested a .NET coverage runner.' }
}
Write-Host 'Frontend coverage plans passed for all four Angular projects.'
