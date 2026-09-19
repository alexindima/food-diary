<#
.SYNOPSIS
Wiki health self-maintenance: stale paths, source links, missing index metadata, project discovery and ownership repair; FailOnInvalid enforces validation.
Самопроверка вики: устаревшие пути, ссылки на исходники, полнота индекса, новые модули и папки; поиск и исправление проблем.
#>
[CmdletBinding()]
param(
    [switch]$Repair,
    [switch]$FailOnInvalid,
    [string]$BaseRef = 'HEAD',
    [ValidateSet('Text', 'Json')][string]$Format = 'Text'
)
$ErrorActionPreference = 'Stop'
$sourceTool = Join-Path $PSScriptRoot 'wiki-source-maintenance.mjs'
$sourceJson = & node $sourceTool "--repair=$($Repair.IsPresent.ToString().ToLowerInvariant())" "--base=$BaseRef"
if ($LASTEXITCODE -ne 0) { throw 'Wiki source maintenance failed.' }
$sources = $sourceJson | ConvertFrom-Json
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
if ($Repair) {
    $build = & $manager -Action build -Format Json | ConvertFrom-Json
    $sourcesAfterJson = & node $sourceTool '--repair=false' "--base=$BaseRef"
    if ($LASTEXITCODE -ne 0) { throw 'Wiki source maintenance recheck failed.' }
    $remainingSources = $sourcesAfterJson | ConvertFrom-Json
} else { $remainingSources = $sources }
$databasePath = Join-Path $PSScriptRoot '../../.artifacts/llm-wiki/code-graph/code-graph.sqlite'
$ownership = if (Test-Path -LiteralPath $databasePath -PathType Leaf) {
    & $manager -Action ownership-audit -SkipRefresh -Format Json | ConvertFrom-Json
} else {
    [pscustomobject]@{ unresolvedCount = 1; findings = @([pscustomobject]@{
        kind = 'projection-unavailable'; path = '.artifacts/llm-wiki/code-graph/code-graph.sqlite'
        repairable = $true; action = 'Run graph-build or repair-verify.'
    }) }
}
$projectionFresh = $false
if (Test-Path -LiteralPath $databasePath -PathType Leaf) {
    $graphStatus = & $manager -Action status -SkipRefresh -Format Json | ConvertFrom-Json
    $projectionFresh = [bool]$graphStatus.changeSetFresh
}
$result = [ordered]@{
    schemaVersion = 1; repaired = [bool]$Repair; changedPages = @($sources.changedPages)
    ownership = $ownership; sourceFindings = @($remainingSources.findings)
    projectionFresh = $projectionFresh
    valid = $projectionFresh -and $ownership.unresolvedCount -eq 0 -and @($remainingSources.findings).Count -eq 0
    nextAction = 'Rebuild generated pages with update. Unconfirmed moves and unknown project roles require source-backed rule changes; rerun health -QualityArea Wiki afterward.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12 }
else {
    Write-Host "Wiki self-maintenance: valid=$($result.valid), projection fresh=$projectionFresh, changed pages=$($result.changedPages.Count), ownership gaps=$($ownership.unresolvedCount), source gaps=$($result.sourceFindings.Count)."
    foreach ($finding in @($ownership.findings) + @($result.sourceFindings)) { Write-Host " - $($finding.kind): $($finding.path)" }
}
if ($FailOnInvalid -and -not $result.valid) { throw 'Wiki self-maintenance still has unresolved findings; inspect health -QualityArea Wiki -Format Json.' }
