[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$build = & $manager build -Format Json | ConvertFrom-Json
if ([int]$build.files -lt 100 -or [int]$build.symbols -lt 100) { throw 'Code graph build produced an implausibly small repository graph.' }
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
Write-Host "LLM Wiki code graph regression passed: $($warm.files) files, $($warm.symbols) symbols, incremental no-op and Recipes queries are valid."
