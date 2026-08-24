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
$probe3Corpus = Join-Path $PSScriptRoot '../evals/context-search-probe-3.json'
$probe3Evaluation = & $measure -CorpusPath $probe3Corpus -SkipBuild -Format Json | ConvertFrom-Json
$probe4Corpus = Join-Path $PSScriptRoot '../evals/context-search-probe-4.json'
$probe4Evaluation = & $measure -CorpusPath $probe4Corpus -SkipBuild -Format Json | ConvertFrom-Json
$probe5Corpus = Join-Path $PSScriptRoot '../evals/context-search-probe-5.json'
$probe5Evaluation = & $measure -CorpusPath $probe5Corpus -SkipBuild -Format Json | ConvertFrom-Json
$probe6Corpus = Join-Path $PSScriptRoot '../evals/context-search-probe-6.json'
$probe6Evaluation = & $measure -CorpusPath $probe6Corpus -SkipBuild -Format Json | ConvertFrom-Json
$allEvaluations = @($primaryEvaluation, $challengeEvaluation, $generalizationEvaluation, $validationEvaluation, $probeEvaluation, $probe2Evaluation, $probe3Evaluation, $probe4Evaluation, $probe5Evaluation, $probe6Evaluation)
foreach ($evaluation in $allEvaluations) {
    if (-not $evaluation.passed) {
        $missIds = @($evaluation.misses | ForEach-Object {
                if ($null -ne $_.PSObject.Properties['id']) { $_.id } else { '<unknown>' }
            })
        throw "SQL context evaluation missed its thresholds for '$($evaluation.corpusPath)': top1=$($evaluation.metrics.top1Rate), top10=$($evaluation.metrics.top10Rate), MRR=$($evaluation.metrics.meanReciprocalRank); misses=$($missIds -join ', ')."
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
if ([int]$probe3Evaluation.caseCount -lt 30) { throw 'Third promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
if ([int]$probe4Evaluation.caseCount -lt 30) { throw 'Fourth promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
if ([int]$probe5Evaluation.caseCount -lt 40) { throw 'Fifth promoted probe SQL context evaluation must contain at least 40 frozen cases.' }
if ([int]$probe6Evaluation.caseCount -lt 30) { throw 'Sixth promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
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
    [double]$probe2Evaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$probe3Evaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$probe4Evaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$probe5Evaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$probe6Evaluation.metrics.p95SqlDurationMs -lt 0) {
    throw 'SQL context evaluation reported an invalid p95 duration.'
}

$strictCaseCount = [int](($allEvaluations.caseCount | Measure-Object -Sum).Sum)
$strictTop1Count = [int](($allEvaluations.metrics.top1Count | Measure-Object -Sum).Sum)
if ($strictTop1Count -ne $strictCaseCount) {
    throw "Strict SQL context evaluation requires every case at top-1: top1=$strictTop1Count/$strictCaseCount."
}
$probeCaseCount = [int](($probeEvaluation.caseCount, $probe2Evaluation.caseCount, $probe3Evaluation.caseCount, $probe4Evaluation.caseCount, $probe5Evaluation.caseCount, $probe6Evaluation.caseCount | Measure-Object -Sum).Sum)
$probeTop1Count = [int](($probeEvaluation.metrics.top1Count, $probe2Evaluation.metrics.top1Count, $probe3Evaluation.metrics.top1Count, $probe4Evaluation.metrics.top1Count, $probe5Evaluation.metrics.top1Count, $probe6Evaluation.metrics.top1Count | Measure-Object -Sum).Sum)
Write-Host "LLM Wiki SQL context evaluation passed: strict top1=$strictTop1Count/$strictCaseCount; promotion top10=$combinedTop10Count/$combinedCaseCount; primary MRR=$($primaryEvaluation.metrics.meanReciprocalRank), p95=$($primaryEvaluation.metrics.p95SqlDurationMs)ms; challenge MRR=$($challengeEvaluation.metrics.meanReciprocalRank), p95=$($challengeEvaluation.metrics.p95SqlDurationMs)ms; generalization MRR=$($generalizationEvaluation.metrics.meanReciprocalRank), p95=$($generalizationEvaluation.metrics.p95SqlDurationMs)ms; validation MRR=$($validationEvaluation.metrics.meanReciprocalRank), p95=$($validationEvaluation.metrics.p95SqlDurationMs)ms; probes top1=$probeTop1Count/$probeCaseCount; probe5 baseline=22/40 and promoted=$($probe5Evaluation.metrics.top1Count)/$($probe5Evaluation.caseCount); probe6 baseline=9/30 and promoted=$($probe6Evaluation.metrics.top1Count)/$($probe6Evaluation.caseCount)."
