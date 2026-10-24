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
. (Join-Path $PSScriptRoot 'LlmWikiEnvironmentDiagnostics.ps1')
$healthTimer = [Diagnostics.Stopwatch]::StartNew()
$environment = Get-LlmWikiEnvironmentDiagnostics -RepositoryRoot ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')))
if (-not $environment.valid) {
    $blocked = [ordered]@{ schemaVersion = 1; valid = $false; repaired = $false; environment = $environment; projectionFresh = $false; projectionStatus = @{ reason = 'environment-blocked' }; durationMs = $healthTimer.ElapsedMilliseconds; nextAction = @($environment.issues.action | Select-Object -Unique) -join ' ' }
    if ($Format -eq 'Json') { $blocked | ConvertTo-Json -Depth 12 } else { Write-Host "Wiki health: environment-blocked. $($blocked.nextAction)" }
    if ($FailOnInvalid) { throw 'Wiki health is blocked by environment prerequisites; inspect environment.issues.' }
    return
}
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
$graphStatus = $null
if (Test-Path -LiteralPath $databasePath -PathType Leaf) {
    $graphStatus = & $manager -Action status -SkipRefresh -Format Json | ConvertFrom-Json
    $projectionFresh = [bool]$graphStatus.changeSetFresh
}
$freshnessReason = if ($projectionFresh) { 'current' }
elseif ($null -eq $graphStatus) { 'projection-unavailable' }
elseif ($graphStatus.changeSetGitHead -cne $graphStatus.currentChangeSetGitHead) { 'head-changed' }
else { 'working-tree-changed' }
$nextActions = @()
foreach ($issue in $environment.issues) { $nextActions += [string]$issue.action }
if (-not $projectionFresh) { $nextActions += 'Run wiki.ps1 graph-build to refresh the SQLite projection, then rerun health -QualityArea Wiki.' }
if ($ownership.unresolvedCount -gt 0) { $nextActions += 'Run repair-verify for repairable metadata; unknown project roles require source-backed ownership rules.' }
if (@($remainingSources.findings).Count -gt 0) { $nextActions += 'Inspect sourceFindings; regenerate generated pages with update and resolve unconfirmed source moves from repository evidence.' }
$result = [ordered]@{
    schemaVersion = 1; repaired = [bool]$Repair; changedPages = @($sources.changedPages)
    ownership = $ownership; sourceFindings = @($remainingSources.findings)
    projectionFresh = $projectionFresh
    projectionStatus = [ordered]@{
        reason = $freshnessReason
        indexedHead = $(if ($null -ne $graphStatus) { $graphStatus.changeSetGitHead } else { $null })
        currentHead = $(if ($null -ne $graphStatus) { $graphStatus.currentChangeSetGitHead } else { $null })
        indexedFingerprint = $(if ($null -ne $graphStatus) { $graphStatus.changeSetFingerprint } else { $null })
        currentFingerprint = $(if ($null -ne $graphStatus) { $graphStatus.currentChangeSetFingerprint } else { $null })
    }
    environment = $environment
    durationMs = $healthTimer.ElapsedMilliseconds
    valid = $environment.valid -and $projectionFresh -and $ownership.unresolvedCount -eq 0 -and @($remainingSources.findings).Count -eq 0
    nextAction = $(if ($nextActions.Count) { $nextActions -join ' ' } else { 'No action required.' })
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12 }
else {
    Write-Host "Environment: valid=$($environment.valid), PowerShell=$($environment.powershellVersion), diagnostics=$($environment.durationMs)ms, total=$($result.durationMs)ms. Baseline: $($environment.performanceBaselineCommand)"
    foreach ($issue in $environment.issues) { Write-Host " - $($issue.kind): $($issue.path). $($issue.action)" }
    Write-Host "Wiki self-maintenance: valid=$($result.valid), projection fresh=$projectionFresh, changed pages=$($result.changedPages.Count), ownership gaps=$($ownership.unresolvedCount), source gaps=$($result.sourceFindings.Count)."
    Write-Host "Projection: $freshnessReason; indexed HEAD=$($result.projectionStatus.indexedHead); current HEAD=$($result.projectionStatus.currentHead). $($result.nextAction)"
    foreach ($finding in @($ownership.findings) + @($result.sourceFindings)) { Write-Host " - $($finding.kind): $($finding.path)" }
}
if ($FailOnInvalid -and -not $result.valid) { throw 'Wiki self-maintenance still has unresolved findings; inspect health -QualityArea Wiki -Format Json.' }
