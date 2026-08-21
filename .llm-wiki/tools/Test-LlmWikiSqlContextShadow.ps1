[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$contextTool = Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1'
$null = & $manager build -Format Json
$shadow = & $contextTool `
    -Module Recipes `
    -Query 'Recipe nutrition updater' `
    -ScopePath 'FoodDiary.Application.Recipes' `
    -Limit 12 `
    -SqlShadow `
    -Format Json | ConvertFrom-Json

if ($shadow.sqlShadow.authoritative -ne 'json' -or -not $shadow.sqlShadow.ready) {
    throw 'SQL context shadow did not preserve JSON authority or report a ready FTS projection.'
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
