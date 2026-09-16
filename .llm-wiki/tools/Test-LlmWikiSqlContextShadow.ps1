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
$shadow = & $contextTool `
    -Module Recipes `
    -Query 'Recipe nutrition updater' `
    -ScopePath 'Modules/Recipes/Application' `
    -Limit 12 `
    -SqlShadow `
    -Format Json | ConvertFrom-Json

if ($shadow.sqlShadow.authoritative -ne 'json-baseline' -or -not $shadow.sqlShadow.ready) {
    throw 'SQL context shadow did not preserve legacy JSON authority or report a ready SQLite search projection.'
}
$topCandidate = @($shadow.sqlShadow.topCandidates | Select-Object -First 1)
if ($topCandidate.Count -ne 1 -or $topCandidate[0].path -notmatch 'RecipeNutritionUpdater\.cs$') {
    throw 'SQL context shadow did not rank RecipeNutritionUpdater first.'
}
if ([int]$shadow.sqlShadow.overlapCount -lt 1) { throw 'SQL context shadow did not overlap the legacy ranked code context.' }
if ([double]$shadow.sqlShadow.sqlQueryDurationMs -lt 0 -or [double]$shadow.sqlShadow.roundTripDurationMs -lt 0) {
    throw 'SQL context shadow did not report non-negative query and transport timings.'
}

Write-Host "LLM Wiki SQL context shadow passed: indexed=$($shadow.sqlShadow.indexedDocuments), overlap=$($shadow.sqlShadow.overlapCount)/$($shadow.sqlShadow.legacyCandidateCount), SQL=$($shadow.sqlShadow.sqlQueryDurationMs)ms, round-trip=$($shadow.sqlShadow.roundTripDurationMs)ms."
