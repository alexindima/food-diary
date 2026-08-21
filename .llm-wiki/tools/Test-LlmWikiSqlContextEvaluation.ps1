[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$measure = Join-Path $PSScriptRoot 'Measure-LlmWikiSqlContextEvaluation.ps1'
$primaryEvaluation = & $measure -Format Json | ConvertFrom-Json
$challengeCorpus = Join-Path $PSScriptRoot '../evals/context-search-holdout.json'
$challengeEvaluation = & $measure -CorpusPath $challengeCorpus -SkipBuild -Format Json | ConvertFrom-Json
$generalizationCorpus = Join-Path $PSScriptRoot '../evals/context-search-generalization.json'
$generalizationEvaluation = & $measure -CorpusPath $generalizationCorpus -SkipBuild -Format Json | ConvertFrom-Json
$validationCorpus = Join-Path $PSScriptRoot '../evals/context-search-validation.json'
$validationEvaluation = & $measure -CorpusPath $validationCorpus -SkipBuild -Format Json | ConvertFrom-Json
$probeCorpus = Join-Path $PSScriptRoot '../evals/context-search-probe.json'
$probeEvaluation = & $measure -CorpusPath $probeCorpus -SkipBuild -Format Json | ConvertFrom-Json
$probe2Corpus = Join-Path $PSScriptRoot '../evals/context-search-probe-2.json'
$probe2Evaluation = & $measure -CorpusPath $probe2Corpus -SkipBuild -Format Json | ConvertFrom-Json
foreach ($evaluation in @($primaryEvaluation, $challengeEvaluation, $generalizationEvaluation, $validationEvaluation, $probeEvaluation, $probe2Evaluation)) {
    if (-not $evaluation.passed) {
        throw "SQL context evaluation missed its thresholds for '$($evaluation.corpusPath)': top1=$($evaluation.metrics.top1Rate), top10=$($evaluation.metrics.top10Rate), MRR=$($evaluation.metrics.meanReciprocalRank); misses=$(@($evaluation.misses.id) -join ', ')."
    }
}
foreach ($evaluation in @($primaryEvaluation, $challengeEvaluation)) {
    if (-not $evaluation.switchReady) {
        throw "SQL context evaluation did not meet switch criteria for '$($evaluation.corpusPath)': $($evaluation.switchGaps -join '; ')."
    }
    if ($null -eq $evaluation.switchCriteria) { throw "SQL context evaluation did not report switch criteria for '$($evaluation.corpusPath)'." }
}
if ([int]$primaryEvaluation.caseCount -lt 60) { throw 'Primary SQL context evaluation must contain at least 60 representative cases.' }
if ([int]$challengeEvaluation.caseCount -lt 40) { throw 'Challenge SQL context evaluation must contain at least 40 independently authored cases.' }
if ([int]$generalizationEvaluation.caseCount -lt 70) { throw 'Generalization SQL context evaluation must contain at least 70 frozen cases.' }
if ([int]$validationEvaluation.caseCount -lt 50) { throw 'Validation SQL context evaluation must contain at least 50 frozen cases.' }
if ([int]$probeEvaluation.caseCount -lt 30) { throw 'Promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
if ([int]$probe2Evaluation.caseCount -lt 30) { throw 'Second promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
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
if ([double]$primaryEvaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$challengeEvaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$generalizationEvaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$validationEvaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$probeEvaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$probe2Evaluation.metrics.p95SqlDurationMs -lt 0) {
    throw 'SQL context evaluation reported an invalid p95 duration.'
}

Write-Host "LLM Wiki SQL context evaluation passed: promotion top10=$combinedTop10Count/$combinedCaseCount; primary top1=$($primaryEvaluation.metrics.top1Count)/$($primaryEvaluation.caseCount), MRR=$($primaryEvaluation.metrics.meanReciprocalRank), p95=$($primaryEvaluation.metrics.p95SqlDurationMs)ms; challenge top1=$($challengeEvaluation.metrics.top1Count)/$($challengeEvaluation.caseCount), MRR=$($challengeEvaluation.metrics.meanReciprocalRank), p95=$($challengeEvaluation.metrics.p95SqlDurationMs)ms; generalization top1=$($generalizationEvaluation.metrics.top1Count)/$($generalizationEvaluation.caseCount), top10=$($generalizationEvaluation.metrics.top10Count)/$($generalizationEvaluation.caseCount), MRR=$($generalizationEvaluation.metrics.meanReciprocalRank), p95=$($generalizationEvaluation.metrics.p95SqlDurationMs)ms; validation top1=$($validationEvaluation.metrics.top1Count)/$($validationEvaluation.caseCount), top10=$($validationEvaluation.metrics.top10Count)/$($validationEvaluation.caseCount), MRR=$($validationEvaluation.metrics.meanReciprocalRank), p95=$($validationEvaluation.metrics.p95SqlDurationMs)ms; probe top1=$($probeEvaluation.metrics.top1Count)/$($probeEvaluation.caseCount), top10=$($probeEvaluation.metrics.top10Count)/$($probeEvaluation.caseCount), MRR=$($probeEvaluation.metrics.meanReciprocalRank), p95=$($probeEvaluation.metrics.p95SqlDurationMs)ms; probe2 top1=$($probe2Evaluation.metrics.top1Count)/$($probe2Evaluation.caseCount), top10=$($probe2Evaluation.metrics.top10Count)/$($probe2Evaluation.caseCount), MRR=$($probe2Evaluation.metrics.meanReciprocalRank), p95=$($probe2Evaluation.metrics.p95SqlDurationMs)ms."
