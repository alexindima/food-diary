[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$contextTool = Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1'
$null = & $manager build -Format Json
foreach ($moduleName in @('Billing', 'Products', 'Recipes')) {
    $apiContext = & $contextTool -Module $moduleName -ChangeType Api -Limit 8 -Format Json | ConvertFrom-Json
    $moduleTestPrefix = "Modules/$moduleName/tests/"
    if (@($apiContext.tests | Where-Object { $_.path.StartsWith($moduleTestPrefix, [StringComparison]::Ordinal) }).Count -eq 0) {
        throw "$moduleName API context lost focused module tests behind production candidates."
    }
    $productionSearch = & $manager -Action search -Query $moduleName -Module $moduleName -ChangeType Api -Limit 50 -SkipRefresh -Format Json | ConvertFrom-Json
    foreach ($candidate in $apiContext.candidates) {
        $original = @($productionSearch.records | Where-Object path -eq $candidate.path)
        if ($original.Count -ne 1 -or $candidate.score -ne $original[0].score -or
            $candidate.rank -ne $original[0].rank -or $candidate.confidence -cne $original[0].confidence) {
            throw "$moduleName test-context retrieval changed a production candidate's ranking."
        }
    }
}
$context = & $contextTool `
    -Module Recipes `
    -Query 'Recipe nutrition updater' `
    -ScopePath 'Modules/Recipes/Application' `
    -Limit 12 `
    -SkipQueryCache `
    -Format Json | ConvertFrom-Json

$topCandidate = @($context.candidates | Select-Object -First 1)
if ($topCandidate.Count -ne 1 -or $topCandidate[0].path -notmatch 'RecipeNutritionUpdater\.cs$') {
    throw 'SQLite context did not rank RecipeNutritionUpdater first.'
}
if ($context.compiledIndex.source -ne 'sqlite-search' -or -not $context.compiledIndex.fresh -or
    @($context.candidates | Where-Object path -notlike 'Modules/Recipes/Application/*').Count -gt 0 -or
    [double]$context.compiledIndex.sqlDurationMs -lt 0 -or [double]$context.compiledIndex.roundTripDurationMs -lt 0) {
    throw 'SQLite context lost scope, freshness or non-negative query and transport timings.'
}

Write-Host 'LLM Wiki SQLite context ranking passed: exact identity, scope and independently retrieved focused tests.'
