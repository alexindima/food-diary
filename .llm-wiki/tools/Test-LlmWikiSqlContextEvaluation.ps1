[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$measure = Join-Path $PSScriptRoot 'Measure-LlmWikiSqlContextEvaluation.ps1'
$primaryEvaluation = & $measure -Format Json | ConvertFrom-Json
$challengeCorpus = Join-Path $PSScriptRoot '../evals/context-search-holdout.json'
$challengeEvaluation = & $measure -CorpusPath $challengeCorpus -SkipBuild -Format Json | ConvertFrom-Json
foreach ($evaluation in @($primaryEvaluation, $challengeEvaluation)) {
    if (-not $evaluation.passed) {
        throw "SQL context evaluation missed its thresholds for '$($evaluation.corpusPath)': top1=$($evaluation.metrics.top1Rate), top10=$($evaluation.metrics.top10Rate), MRR=$($evaluation.metrics.meanReciprocalRank); misses=$(@($evaluation.misses.id) -join ', ')."
    }
    if (-not $evaluation.switchReady) {
        throw "SQL context evaluation did not meet switch criteria for '$($evaluation.corpusPath)': $($evaluation.switchGaps -join '; ')."
    }
    if ($null -eq $evaluation.switchCriteria) { throw "SQL context evaluation did not report switch criteria for '$($evaluation.corpusPath)'." }
}
if ([int]$primaryEvaluation.caseCount -lt 60) { throw 'Primary SQL context evaluation must contain at least 60 representative cases.' }
if ([int]$challengeEvaluation.caseCount -lt 40) { throw 'Challenge SQL context evaluation must contain at least 40 independently authored cases.' }
$combinedCaseCount = [int]$primaryEvaluation.caseCount + [int]$challengeEvaluation.caseCount
$combinedTop10Count = [int]$primaryEvaluation.metrics.top10Count + [int]$challengeEvaluation.metrics.top10Count
if ($combinedCaseCount -lt 100) { throw 'Combined SQL context evaluation must contain at least 100 representative cases.' }
if ($combinedTop10Count -ne $combinedCaseCount) {
    throw "Combined SQL context evaluation must have no top-10 misses: top10=$combinedTop10Count/$combinedCaseCount."
}
$powerShellCase = @($primaryEvaluation.results | Where-Object id -eq 'api-compatibility-powershell')
if ($powerShellCase.Count -ne 1 -or -not $powerShellCase[0].top10) {
    throw 'PowerShell FTS coverage did not retrieve the API compatibility tool in the top 10.'
}
if ([double]$primaryEvaluation.metrics.p95SqlDurationMs -lt 0 -or [double]$challengeEvaluation.metrics.p95SqlDurationMs -lt 0) {
    throw 'SQL context evaluation reported an invalid p95 duration.'
}

Write-Host "LLM Wiki SQL context evaluation passed: combined top10=$combinedTop10Count/$combinedCaseCount; primary top1=$($primaryEvaluation.metrics.top1Count)/$($primaryEvaluation.caseCount), MRR=$($primaryEvaluation.metrics.meanReciprocalRank), p95=$($primaryEvaluation.metrics.p95SqlDurationMs)ms; challenge top1=$($challengeEvaluation.metrics.top1Count)/$($challengeEvaluation.caseCount), MRR=$($challengeEvaluation.metrics.meanReciprocalRank), p95=$($challengeEvaluation.metrics.p95SqlDurationMs)ms."
