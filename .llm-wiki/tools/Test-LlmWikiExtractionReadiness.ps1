[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$result = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Users -Format Json | ConvertFrom-Json
if ($result.contractReadiness.aggregateBlockers -ne 0) { throw 'Current IUserContextService should have no aggregate blockers.' }
if (-not $result.moduleReadiness.ready) { throw "Users module should be extraction-ready after external aggregate and mutation consumers are removed: $($result.moduleReadiness.blockers -join '; ')" }
if (@($result.moduleReadiness.leakingContracts) -contains 'IUserDirectoryService') { throw 'Removed IUserDirectoryService must not remain in extraction readiness.' }
if (@($result.moduleReadiness.leakingContracts) -notcontains 'IUserLookupRepository') { throw 'Boundary scan missed an inherited transitive wrapper.' }
if ($result.categories.transitiveWrapper -lt 1) { throw 'Inherited aggregate wrappers must be categorized separately.' }
if (@($result.leaks | Where-Object contract -eq 'IUserDirectoryService').Count -ne 0) { throw 'Removed IUserDirectoryService must have no consumers.' }
if (@($result.moduleReadiness.blockers).Count -ne 0) { throw 'Extraction-ready module must have no blockers.' }
if ($result.contractReadiness.mutationBlockers -ne 0) { throw 'Owner-internal IUserContextService mutations must not block extraction.' }
if (-not $result.contractReadiness.aggregateReady) { throw 'Contract and module readiness were not separated.' }
$dietologist = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Dietologist -Format Json | ConvertFrom-Json
if ($dietologist.contractReadiness.mutationBlockers -ne 0) { throw 'Users-owned mutation consumers must not block unrelated module extraction.' }
if (-not $dietologist.moduleReadiness.ready) { throw "Dietologist should be extraction-ready after cross-feature dependencies are removed: $($dietologist.moduleReadiness.blockers -join '; ')" }
$probe = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Dietologist -CompileProbe -Format Json | ConvertFrom-Json
if (-not $probe.compileProbe.passed) { throw "Dietologist extraction compile probe failed: $($probe.compileProbe.diagnostics -join '; ')" }
if (-not $probe.dependencyReadiness.ready -or @($probe.dependencyReadiness.actualModules).Count -ne 0) { throw 'Dietologist dependency scan must be clean before extraction.' }

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixtureRoot = Join-Path $repositoryRoot ".artifacts/llm-wiki/extraction-readiness-$PID"
$fixturePath = Join-Path $fixtureRoot 'DashboardLeak.cs'
try {
    $null = New-Item -ItemType Directory -Path $fixtureRoot -Force
    [IO.File]::WriteAllText($fixturePath, "using FoodDiary.Application.Dashboard.Models;`nnamespace FoodDiary.Application.Dietologist.Tests;`ninternal sealed class DashboardLeak;`n", [Text.UTF8Encoding]::new($false))
    $relativeFixture = $fixturePath.Substring($repositoryRoot.Length + 1).Replace('\', '/')
    $withLeak = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Dietologist -DependencyFixturePath $relativeFixture -Format Json | ConvertFrom-Json
    if (@($withLeak.dependencyReadiness.actualModules) -notcontains 'Dashboard') { throw 'Universal dependency scan missed a Dashboard namespace/type reference.' }
    if ($withLeak.moduleReadiness.ready) { throw 'A cross-feature source dependency must block physical extraction readiness.' }
} finally {
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "LLM Wiki extraction readiness regression passed: $($result.moduleReadiness.aggregateLeakPaths) production leak path(s)."
