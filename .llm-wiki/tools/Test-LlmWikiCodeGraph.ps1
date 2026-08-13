[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$build = & $manager build -Format Json | ConvertFrom-Json
if ([int]$build.files -lt 100 -or [int]$build.symbols -lt 100) { throw 'Code graph build produced an implausibly small repository graph.' }
if ([int]$build.typedEdges -lt 1000) { throw 'Code graph build produced an implausibly small typed relationship graph.' }
$warm = & $manager build -Format Json | ConvertFrom-Json
if ([int]$warm.updated -ne 0 -or [int]$warm.scanned -ne 0) { throw 'Unchanged code graph build was not incremental.' }
$symbol = & $manager symbol -Query RecipeNutritionUpdater -Format Json | ConvertFrom-Json
if (@($symbol.symbols | Where-Object path -eq 'FoodDiary.Application/Recipes/Services/RecipeNutritionUpdater.cs').Count -ne 1) {
    throw 'Code graph symbol query did not locate RecipeNutritionUpdater.'
}
$consumers = & $manager consumers -Query IRecipeOverviewReadService -Limit 100 -Format Json | ConvertFrom-Json
foreach ($requiredConsumer in @(
    'FoodDiary.Application/Recipes/Queries/GetRecipeById/GetRecipeByIdQueryHandler.cs'
    'FoodDiary.Infrastructure/Persistence/Recipes/RecipeOverviewReadService.cs'
)) {
    if ($requiredConsumer -notin @($consumers.consumers.path)) { throw "Code graph omitted expected consumer: $requiredConsumer" }
}
$impact = & $manager impact -ChangedPath 'FoodDiary.Application/Recipes' -Limit 500 -Format Json | ConvertFrom-Json
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
foreach ($requiredKind in @('di-service','mediator-handler','project-reference','http-client','template-component','test-ownership','configuration-key','migration-table')) {
    if ($requiredKind -notin @($coverage.relationKinds.kind)) { throw "Code graph coverage omitted typed relationship kind '$requiredKind'." }
}
foreach ($shadow in @($coverage.legacySymbolCoverage)) {
    if ([int]$shadow.missing -ne 0) { throw "Graph shadow coverage is incomplete for $($shadow.index): $($shadow.missing) symbol(s) missing." }
}
$recipeRelations = & $manager relations -ChangedPath 'FoodDiary.Application/Recipes' -RelationKind mediator-handler -Limit 100 -Format Json | ConvertFrom-Json
if (@($recipeRelations.relations | Where-Object { $_.target -eq 'CreateRecipeCommand' -and $_.path -match 'CreateRecipeCommandHandler.cs$' }).Count -ne 1) {
    throw 'Typed graph did not preserve mediator handler provenance for CreateRecipeCommand.'
}
$migrationRelations = & $manager relations -ChangedPath 'FoodDiary.Infrastructure/Migrations/20251108210736_InitialCreate.cs' -RelationKind migration-table -Limit 100 -Format Json | ConvertFrom-Json
if (@($migrationRelations.relations).Count -eq 0) { throw 'Typed graph did not preserve migration table provenance.' }
$graphTestPlan = & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphTestPlan.ps1') -ProposedPath 'FoodDiary.Application/Recipes' -Limit 100 -Format Json | ConvertFrom-Json
if (@($graphTestPlan.required | Where-Object { $_ -match 'RecipesFeatureTests\.cs$' }).Count -ne 1) {
    throw 'Graph-only test plan did not classify a direct Recipes test consumer as required.'
}
Write-Host "LLM Wiki code graph regression passed: $($warm.files) files, $($warm.symbols) symbols, incremental no-op and Recipes queries are valid."
