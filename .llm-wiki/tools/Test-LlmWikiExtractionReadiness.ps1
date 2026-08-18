[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$readinessToolText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Raw
if (-not $readinessToolText.Contains('$reusable = -not $CompileProbe -or [bool]$cached.compileProbe.passed') -or
    -not $readinessToolText.Contains('$cachePath -and (-not $CompileProbe -or [bool]$compileProbeResult.passed)')) {
    throw 'Extraction readiness must not reuse or persist interrupted/failed compile probes.'
}
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

$bodyMetrics = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module BodyMetrics -Format Json | ConvertFrom-Json
if (-not $bodyMetrics.moduleReadiness.ready -or -not $bodyMetrics.dependencyReadiness.ready) {
    throw "BodyMetrics internal feature namespaces must not be treated as external module dependencies: $($bodyMetrics.moduleReadiness.blockers -join '; ')"
}
if (@($bodyMetrics.dependencyReadiness.internalFeatureNamespaces) -notcontains 'WeightEntries' -or
    @($bodyMetrics.dependencyReadiness.internalFeatureNamespaces) -notcontains 'WaistEntries') {
    throw 'BodyMetrics readiness did not discover both logical features owned by its physical source set.'
}
if (@($bodyMetrics.dependencyReadiness.actualModules) -contains 'WeightEntries' -or
    @($bodyMetrics.dependencyReadiness.actualModules) -contains 'WaistEntries') {
    throw 'BodyMetrics readiness still reports assembly-internal logical features as external dependencies.'
}

$recipesProject = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'FoodDiary.Application.Recipes/FoodDiary.Application.Recipes.csproj'
if (Test-Path -LiteralPath $recipesProject -PathType Leaf) {
    $recipes = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Recipes -Format Json | ConvertFrom-Json
    $recipeRegistrations = @($recipes.dependencyReadiness.diRegistrations)
    foreach ($compositionRoot in @(
        'FoodDiary.Initializer/Program.cs'
        'FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs'
    )) {
        if ($compositionRoot -notin @($recipeRegistrations.path)) { throw "Extracted Recipes DI registration was not found in $compositionRoot." }
    }
    if (@($recipeRegistrations | Where-Object kind -eq 'module-extension-call').Count -ne 2) {
        throw "Expected exactly two extracted Recipes composition registrations, found $(@($recipeRegistrations).Count)."
    }
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'extraction-readiness'
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
