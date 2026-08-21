[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$graphToolText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'code-graph.mjs') -Raw
foreach ($lockSafetyFragment in @('isProcessAlive(owner.pid)', 'process.kill(pid, 0)', 'owner.token === ownerToken')) {
    if (-not $graphToolText.Contains($lockSafetyFragment)) { throw "Code graph live-owner lock safety is missing: $lockSafetyFragment" }
}
foreach ($corruptionSafetyFragment in @('isDatabaseCorruption(error)', 'quarantineCorruptDatabase(databasePath)', 'recoveredFromCorruption: true')) {
    if (-not $graphToolText.Contains($corruptionSafetyFragment)) { throw "Code graph corruption recovery is missing: $corruptionSafetyFragment" }
}
$recipesBoundary = if (Test-Path -LiteralPath (Join-Path $repositoryRoot 'FoodDiary.Application.Recipes') -PathType Container) { 'FoodDiary.Application.Recipes' } else { 'FoodDiary.Application/Recipes' }
$recipesSourcePrefix = if ($recipesBoundary -eq 'FoodDiary.Application.Recipes') { 'FoodDiary.Application.Recipes/Recipes' } else { $recipesBoundary }
$russianServerQuery = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('0L/QvtC00LrQu9GO0YfQuNGB0Ywg0YHQtdGA0LLQtdGA0YM='))
$build = & $manager build -Format Json | ConvertFrom-Json
if ([int]$build.files -lt 100 -or [int]$build.symbols -lt 100) { throw 'Code graph build produced an implausibly small repository graph.' }
if ([int]$build.typedEdges -lt 1000) { throw 'Code graph build produced an implausibly small typed relationship graph.' }
if ([string]::IsNullOrWhiteSpace([string]$build.graphDependencyFingerprint) -or
    -not (Test-Path -LiteralPath (Join-Path $repositoryRoot '.artifacts/llm-wiki/code-graph/code-graph.fingerprint') -PathType Leaf)) {
    throw 'Code graph build did not publish its cache dependency fingerprint sidecar.'
}
$warm = & $manager build -Format Json | ConvertFrom-Json
if ([int]$warm.updated -ne 0 -or [int]$warm.scanned -ne 0) { throw 'Unchanged code graph build was not incremental.' }
if ([int]$warm.contextSearch.documents -lt 1000) { throw 'Code graph FTS projection contains too few repository documents.' }
$fts = & $manager search -Query 'Recipe nutrition updater' -Limit 20 -SkipRefresh -Format Json | ConvertFrom-Json
if (-not $fts.ready -or
    @($fts.records | Where-Object path -eq "$recipesSourcePrefix/Services/RecipeNutritionUpdater.cs").Count -ne 1) {
    throw 'SQLite FTS context search did not locate RecipeNutritionUpdater.'
}
foreach ($searchCase in @(
    @{ Query = 'MCP PowerShell command stage telemetry'; ExpectedPath = 'FoodDiary.Development.Mcp/Wiki/PowerShellWikiCommandExecutor.cs' }
    @{ Query = 'Mail inbox SMTP rate limiter'; ExpectedPath = 'MailInbox/FoodDiary.MailInbox.Infrastructure/Services/MailInboxMailboxFilter.cs' }
    @{ Query = 'weight history measurements'; ExpectedPath = 'FoodDiary.Web.Client/src/app/features/weight-history/components/weight-history-chart-card/weight-history-chart-card.ts' }
    @{ Query = 'periodic cleanup fasting telemetry registration'; ExpectedPath = 'FoodDiary.JobManager/Services/FastingTelemetryCleanupJob.cs' }
    @{ Query = $russianServerQuery; ExpectedPath = 'AGENTS.md' }
)) {
    $searchResult = & $manager search -Query $searchCase.Query -Limit 10 -SkipRefresh -Format Json | ConvertFrom-Json
    if ($searchCase.ExpectedPath -notin @($searchResult.records.path)) {
        throw "SQLite FTS context search did not locate '$($searchCase.ExpectedPath)' for '$($searchCase.Query)'."
    }
}
$symbol = & $manager symbol -Query RecipeNutritionUpdater -Format Json | ConvertFrom-Json
if (@($symbol.symbols | Where-Object path -eq "$recipesSourcePrefix/Services/RecipeNutritionUpdater.cs").Count -ne 1) {
    throw 'Code graph symbol query did not locate RecipeNutritionUpdater.'
}
$consumers = & $manager consumers -Query IRecipeOverviewReadService -Limit 100 -Format Json | ConvertFrom-Json
foreach ($requiredConsumer in @(
    "$recipesSourcePrefix/Queries/GetRecipeById/GetRecipeByIdQueryHandler.cs"
    'FoodDiary.Infrastructure/Persistence/Recipes/RecipeOverviewReadService.cs'
)) {
    if ($requiredConsumer -notin @($consumers.consumers.path)) { throw "Code graph omitted expected consumer: $requiredConsumer" }
}
$impact = & $manager impact -ChangedPath $recipesBoundary -Limit 500 -Format Json | ConvertFrom-Json
if (@($impact.paths).Count -lt 20 -or @($impact.references).Count -eq 0 -or @($impact.consumers).Count -eq 0) {
    throw 'Code graph module impact did not expose the expected Recipes boundary.'
}
$consumerTool = Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1'
$scanConsumers = & $consumerTool -Contract IRecipeOverviewReadService -Format Json | ConvertFrom-Json
$graphConsumers = & $consumerTool -Contract IRecipeOverviewReadService -Fast -Format Json | ConvertFrom-Json
if (@(Compare-Object @($scanConsumers.consumers.path) @($graphConsumers.consumers.path)).Count -ne 0 -or
    $graphConsumers.declarationPath -ne $scanConsumers.declarationPath) {
    throw 'Graph-prefiltered contract consumers differ from the authoritative repository scan.'
}
$coverage = & $manager coverage -Format Json | ConvertFrom-Json
$powerShellCoverage = @($coverage.languages | Where-Object language -eq 'powershell')
if ($powerShellCoverage.Count -ne 1 -or [int]$powerShellCoverage[0].files -lt 100) {
    throw 'Code graph coverage omitted the repository PowerShell tool surface.'
}
foreach ($requiredKind in @('di-service','mediator-handler','method-call','type-construction','type-inheritance','project-reference','http-client','template-component','test-ownership','configuration-key','migration-table')) {
    if ($requiredKind -notin @($coverage.relationKinds.kind)) { throw "Code graph coverage omitted typed relationship kind '$requiredKind'." }
}
foreach ($shadow in @($coverage.legacySymbolCoverage)) {
    if ([int]$shadow.missing -ne 0) { throw "Graph shadow coverage is incomplete for $($shadow.index): $($shadow.missing) symbol(s) missing." }
}
$recipeRelations = & $manager relations -ChangedPath $recipesBoundary -RelationKind mediator-handler -Limit 100 -Format Json | ConvertFrom-Json
if (@($recipeRelations.relations | Where-Object { $_.target -eq 'CreateRecipeCommand' -and $_.path -match 'CreateRecipeCommandHandler.cs$' }).Count -ne 1) {
    throw 'Typed graph did not preserve mediator handler provenance for CreateRecipeCommand.'
}
$migrationRelations = & $manager relations -ChangedPath 'FoodDiary.Infrastructure/Migrations/20251108210736_InitialCreate.cs' -RelationKind migration-table -Limit 100 -Format Json | ConvertFrom-Json
if (@($migrationRelations.relations).Count -eq 0) { throw 'Typed graph did not preserve migration table provenance.' }
$namespaceTrace = & $manager trace -Query 'FoodDiary.Presentation.Api.Features.Auth' -Limit 100 -Format Json | ConvertFrom-Json
if (@($namespaceTrace.consumers | Where-Object { $_.relationKind -eq 'namespace-filter' -and $_.path -match 'ControllerConventionsTests.cs$' }).Count -eq 0 -or
    @($namespaceTrace.namespaceFilters | Where-Object { [int]$_.matchedDeclarations -gt 0 }).Count -eq 0) {
    throw 'Code graph did not connect a namespace convention literal to matching production declarations.'
}
$graphTestPlan = & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphTestPlan.ps1') -ProposedPath $recipesBoundary -Limit 100 -Format Json | ConvertFrom-Json
if (@($graphTestPlan.recommended | Where-Object { $_ -match 'RecipesFeatureTests\.cs$' }).Count -ne 1 -or
    @($graphTestPlan.required | Where-Object { $_ -match 'RecipesFeatureTests\.cs$' }).Count -ne 0) {
    throw 'Graph-only test plan did not classify a transitive Recipes test consumer as recommended.'
}
$cyclePredictionImpact = & $manager impact -ChangedPath 'FoodDiary.Application.Cycles/Services/CyclePredictionService.cs' -Limit 100 -Format Json | ConvertFrom-Json
if (@($cyclePredictionImpact.consumers | Where-Object { $_.language -ne 'csharp' }).Count -gt 0 -or
    @($cyclePredictionImpact.references | Where-Object { $_.declarationPath -match '^FoodDiary\.Web\.Client/' }).Count -gt 0) {
    throw 'C# cycle prediction impact retained an unexplained cross-language token link.'
}
$measurementPaths = @(
    'FoodDiary.Web.Client/src/app/features/weight-history'
    'FoodDiary.Web.Client/src/app/features/waist-history'
    'FoodDiary.Web.Client/src/app/shared/measurements'
)
$measurementTestPlan = & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphTestPlan.ps1') -ProposedPath $measurementPaths -Limit 100 -Format Json | ConvertFrom-Json
$measurementSpecs = @(Get-ChildItem -LiteralPath @($measurementPaths | ForEach-Object { Join-Path $repositoryRoot $_ }) -Recurse -File | Where-Object Name -match '\.spec\.ts$')
if ($measurementSpecs.Count -lt 20 -or @($measurementTestPlan.required | Where-Object { $_ -match '/(?:weight-history|waist-history|shared/measurements)/.+\.spec\.ts$' }).Count -ne $measurementSpecs.Count) {
    throw 'Graph-only test plan did not prioritize every spec under the planned frontend directories.'
}
$broadFrontendPlan = & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphTestPlan.ps1') -ProposedPath 'FoodDiary.Web.Client/src/app/features' -Limit 20 -Format Json | ConvertFrom-Json
if (@($broadFrontendPlan.scopeTooBroad).Count -ne 1 -or $broadFrontendPlan.confidence -ne 'low') {
    throw 'Graph-only test plan did not diagnose an overly broad frontend scope.'
}
$graphArtifactDirectory = Join-Path $repositoryRoot '.artifacts/llm-wiki/code-graph'
$corruptDatabaseName = "corruption-recovery-$([Guid]::NewGuid().ToString('N')).sqlite"
$corruptDatabasePath = Join-Path $graphArtifactDirectory $corruptDatabaseName
$corruptFingerprintPath = "$corruptDatabasePath.fingerprint"
try {
    [IO.File]::WriteAllText($corruptDatabasePath, 'this is not a SQLite database', [Text.Encoding]::UTF8)
    $recoveredBuild = & node (Join-Path $PSScriptRoot 'code-graph.mjs') build "--database=$corruptDatabasePath" | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Code graph build did not recover a corrupt derived database.' }
    if (-not $recoveredBuild.recoveredFromCorruption -or @($recoveredBuild.quarantinedPaths).Count -ne 1) {
        throw 'Code graph build did not report the quarantined corrupt database.'
    }
    if (-not (Test-Path -LiteralPath $corruptDatabasePath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $corruptFingerprintPath -PathType Leaf) -or
        [int]$recoveredBuild.files -lt 100) {
        throw 'Code graph corruption recovery did not publish a rebuilt graph and isolated dependency fingerprint.'
    }
} finally {
    $cleanupPaths = @(
        $corruptDatabasePath
        "$corruptDatabasePath-wal"
        "$corruptDatabasePath-shm"
        $corruptFingerprintPath
    ) + @(Get-ChildItem -LiteralPath $graphArtifactDirectory -Filter "$corruptDatabaseName.corrupt-*" -File -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName)
    foreach ($cleanupPath in @($cleanupPaths | Sort-Object -Unique)) {
        $resolvedCleanupPath = [IO.Path]::GetFullPath($cleanupPath)
        if (-not [string]::Equals([IO.Path]::GetDirectoryName($resolvedCleanupPath), $graphArtifactDirectory, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clean a corruption-test artifact outside the graph directory: $resolvedCleanupPath"
        }
        Remove-Item -LiteralPath $resolvedCleanupPath -Force -ErrorAction SilentlyContinue
    }
}
Write-Host "LLM Wiki code graph regression passed: $($warm.files) files, $($warm.symbols) symbols, incremental no-op and Recipes queries are valid."
