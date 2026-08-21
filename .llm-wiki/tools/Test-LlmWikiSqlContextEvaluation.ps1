[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$measure = Join-Path $PSScriptRoot 'Measure-LlmWikiSqlContextEvaluation.ps1'
$evaluation = & $measure -Format Json | ConvertFrom-Json
if (-not $evaluation.passed) {
    throw "SQL context evaluation missed its thresholds: top1=$($evaluation.metrics.top1Rate), top10=$($evaluation.metrics.top10Rate), MRR=$($evaluation.metrics.meanReciprocalRank); misses=$(@($evaluation.misses.id) -join ', ')."
}
$powerShellCase = @($evaluation.results | Where-Object id -eq 'api-compatibility-powershell')
if ($powerShellCase.Count -ne 1 -or -not $powerShellCase[0].top10) {
    throw 'PowerShell FTS coverage did not retrieve the API compatibility tool in the top 10.'
}
if ([double]$evaluation.metrics.p95SqlDurationMs -lt 0) { throw 'SQL context evaluation reported an invalid p95 duration.' }

Write-Host "LLM Wiki SQL context evaluation passed: top1=$($evaluation.metrics.top1Count)/$($evaluation.caseCount), top10=$($evaluation.metrics.top10Count)/$($evaluation.caseCount), MRR=$($evaluation.metrics.meanReciprocalRank), SQL p95=$($evaluation.metrics.p95SqlDurationMs)ms."
