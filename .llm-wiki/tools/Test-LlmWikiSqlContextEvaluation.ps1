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
$imageWikiRegressionCorpus = Join-Path $PSScriptRoot '../evals/context-search-image-wiki-regression.json'
$imageWikiRegressionEvaluation = & $measure -CorpusPath $imageWikiRegressionCorpus -SkipBuild -Format Json | ConvertFrom-Json
$businessWikiRegressionCorpus = Join-Path $PSScriptRoot '../evals/context-search-business-wiki-regression.json'
$businessWikiRegressionEvaluation = & $measure -CorpusPath $businessWikiRegressionCorpus -SkipBuild -Format Json | ConvertFrom-Json
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
$probe7Corpus = Join-Path $PSScriptRoot '../evals/context-search-probe-7.json'
$probe7Evaluation = & $measure -CorpusPath $probe7Corpus -SkipBuild -Format Json | ConvertFrom-Json
$retirementHoldoutCorpusPath = Join-Path $PSScriptRoot '../evals/context-search-holdout-100.json'
$retirementHoldoutCorpus = [IO.File]::ReadAllText(
    (Resolve-Path -LiteralPath $retirementHoldoutCorpusPath).Path,
    [Text.Encoding]::UTF8) | ConvertFrom-Json
$retirementHoldoutEvaluation = & $measure `
    -CorpusPath $retirementHoldoutCorpusPath `
    -SkipBuild `
    -FailOnRegression `
    -Format Json | ConvertFrom-Json
if (-not [bool]$retirementHoldoutEvaluation.liveRegressionPassed) {
    throw "Independent 100-case holdout live regression gate failed: $($retirementHoldoutEvaluation.liveRegressionGaps -join '; ')."
}
$postFixControlCorpusPath = Join-Path $PSScriptRoot '../evals/context-search-postfix-control-30.json'
$postFixControlCorpus = [IO.File]::ReadAllText(
    (Resolve-Path -LiteralPath $postFixControlCorpusPath).Path,
    [Text.Encoding]::UTF8) | ConvertFrom-Json
$postFixControlEvaluation = & $measure `
    -CorpusPath $postFixControlCorpusPath `
    -SkipBuild `
    -Format Json | ConvertFrom-Json
$postTuneControlCorpusPath = Join-Path $PSScriptRoot '../evals/context-search-posttune-control-30.json'
$postTuneControlCorpus = [IO.File]::ReadAllText(
    (Resolve-Path -LiteralPath $postTuneControlCorpusPath).Path,
    [Text.Encoding]::UTF8) | ConvertFrom-Json
$postTuneControlEvaluation = & $measure `
    -CorpusPath $postTuneControlCorpusPath `
    -SkipBuild `
    -Format Json | ConvertFrom-Json
$controlTop1Rate = ([double]$postFixControlEvaluation.metrics.top1Rate + [double]$postTuneControlEvaluation.metrics.top1Rate) / 2
$blindTop1Rate = [double]$retirementHoldoutEvaluation.metrics.top1Rate
if (($controlTop1Rate - $blindTop1Rate) -gt 0.25) {
    throw "Context ranking shows likely control-corpus overfitting: controls=$([Math]::Round($controlTop1Rate, 4)), blind=$blindTop1Rate."
}
$rankingPolicy = Get-Content (Join-Path $PSScriptRoot '../policies/context-search-ranking.json') -Raw | ConvertFrom-Json
$rankingRuleCount = @($rankingPolicy.queryTermExpansions.PSObject.Properties).Count +
    @($rankingPolicy.queryPrefixExpansions.PSObject.Properties).Count + @($rankingPolicy.pathBoosts).Count +
    @($rankingPolicy.identityBoosts).Count + @($rankingPolicy.structuralRoleBoosts).Count
if ($rankingRuleCount -gt 600 -or $null -eq $rankingPolicy.genericAffinities) {
    throw "Context ranking policy exceeded its 600-rule complexity budget or lost generic affinities: rules=$rankingRuleCount."
}
$allEvaluations = @($primaryEvaluation, $challengeEvaluation, $generalizationEvaluation, $validationEvaluation, $imageWikiRegressionEvaluation, $probeEvaluation, $probe2Evaluation, $probe3Evaluation, $probe4Evaluation, $probe5Evaluation, $probe6Evaluation, $probe7Evaluation)
foreach ($evaluation in $allEvaluations) {
    if (-not $evaluation.passed) {
        $missIds = @($evaluation.misses | ForEach-Object {
                if ($null -ne $_.PSObject.Properties['id']) { $_.id } else { '<unknown>' }
            })
        throw "SQL context evaluation missed its thresholds for '$($evaluation.corpusPath)': top1=$($evaluation.metrics.top1Rate), top10=$($evaluation.metrics.top10Rate), MRR=$($evaluation.metrics.meanReciprocalRank); misses=$($missIds -join ', ')."
    }
}
if (-not $businessWikiRegressionEvaluation.passed) {
    throw "Business and Wiki regression corpus missed its thresholds: top1=$($businessWikiRegressionEvaluation.metrics.top1Rate), top10=$($businessWikiRegressionEvaluation.metrics.top10Rate), MRR=$($businessWikiRegressionEvaluation.metrics.meanReciprocalRank)."
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
if ([int]$imageWikiRegressionEvaluation.caseCount -lt 30) { throw 'Image and Wiki regression evaluation must contain at least 30 frozen cases.' }
if ([int]$businessWikiRegressionEvaluation.caseCount -lt 20) { throw 'Business and Wiki regression evaluation must contain at least 20 frozen cases.' }
if ([int]$probeEvaluation.caseCount -lt 30) { throw 'Promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
if ([int]$probe2Evaluation.caseCount -lt 30) { throw 'Second promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
if ([int]$probe3Evaluation.caseCount -lt 30) { throw 'Third promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
if ([int]$probe4Evaluation.caseCount -lt 30) { throw 'Fourth promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
if ([int]$probe5Evaluation.caseCount -lt 40) { throw 'Fifth promoted probe SQL context evaluation must contain at least 40 frozen cases.' }
if ([int]$probe6Evaluation.caseCount -lt 30) { throw 'Sixth promoted probe SQL context evaluation must contain at least 30 frozen cases.' }
if ([int]$probe7Evaluation.caseCount -lt 40) { throw 'Seventh promoted probe SQL context evaluation must contain at least 40 frozen cases.' }
$expectedProbe7Cohorts = @('adjacent-role-disambiguation', 'behavior-to-test', 'conversational-ru', 'mixed-ru-en', 'wiki-intent')
if (@($probe7Evaluation.cohortMetrics).Count -ne $expectedProbe7Cohorts.Count -or
    @(Compare-Object $expectedProbe7Cohorts @($probe7Evaluation.cohortMetrics.cohort)).Count -ne 0) {
    throw 'Seventh promoted probe SQL context evaluation did not preserve its five methodology cohorts.'
}
foreach ($cohortMetric in @($probe7Evaluation.cohortMetrics)) {
    if ([int]$cohortMetric.caseCount -ne 8 -or [int]$cohortMetric.top1Count -ne 8) {
        throw "Seventh promoted probe cohort '$($cohortMetric.cohort)' must retain 8/8 top-1 cases."
    }
}
$retirementHoldoutCases = @($retirementHoldoutCorpus.cases)
if ($retirementHoldoutCases.Count -ne 100) {
    throw 'Independent JSON fallback retirement holdout must preserve exactly 100 frozen cases.'
}
$expectedRetirementCohorts = @(
    'adjacent-role-disambiguation',
    'api-transport',
    'behavior-to-test',
    'conversational-ru',
    'domain-invariants',
    'frontend',
    'integrations-jobs',
    'mixed-ru-en',
    'persistence',
    'wiki-tooling')
$retirementCohorts = @($retirementHoldoutCases | Group-Object cohort | Sort-Object Name)
if ($retirementCohorts.Count -ne $expectedRetirementCohorts.Count -or
    @(Compare-Object $expectedRetirementCohorts @($retirementCohorts.Name)).Count -ne 0 -or
    @($retirementCohorts | Where-Object Count -ne 10).Count -ne 0) {
    throw 'Independent JSON fallback retirement holdout must preserve ten methodology cohorts with ten cases each.'
}
$duplicateRetirementIds = @($retirementHoldoutCases | Group-Object id | Where-Object Count -gt 1)
$retirementPrimaryTargets = @($retirementHoldoutCases | ForEach-Object { [string]@($_.expectedPaths)[0] })
$duplicateRetirementTargets = @($retirementPrimaryTargets | Group-Object | Where-Object Count -gt 1)
if ($duplicateRetirementIds.Count -gt 0 -or $duplicateRetirementTargets.Count -gt 0) {
    throw 'Independent JSON fallback retirement holdout must preserve unique case IDs and primary targets.'
}
$promotedExpectedPaths = @($allEvaluations.results.expectedPaths | ForEach-Object { [string]$_ } | Sort-Object -Unique)
$reusedRetirementTargets = @($retirementPrimaryTargets | Where-Object { $_ -in $promotedExpectedPaths })
if ($reusedRetirementTargets.Count -gt 0) {
    throw "Independent JSON fallback retirement holdout reused promoted targets: $($reusedRetirementTargets -join ', ')."
}
$repositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$missingRetirementTargets = @($retirementPrimaryTargets | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_))
    })
if ($missingRetirementTargets.Count -gt 0) {
    throw "Independent JSON fallback retirement holdout targets are missing: $($missingRetirementTargets -join ', ')."
}
if ([int]$retirementHoldoutCorpus.blindBaseline.node.top1Count -ne 21 -or
    [int]$retirementHoldoutCorpus.blindBaseline.node.top10Count -ne 61 -or
    [double]$retirementHoldoutCorpus.blindBaseline.node.meanReciprocalRank -ne 0.3404 -or
    [int]$retirementHoldoutCorpus.blindBaseline.parity.exactRankAndTop5Differences -ne 0) {
    throw 'Independent JSON fallback retirement holdout must retain its frozen blind baseline and parity evidence.'
}
if ([int]$retirementHoldoutCorpus.postFixEvaluation.node.top1Count -ne 57 -or
    [int]$retirementHoldoutCorpus.postFixEvaluation.node.top10Count -ne 100 -or
    [double]$retirementHoldoutCorpus.postFixEvaluation.node.meanReciprocalRank -ne 0.719 -or
    [int]$retirementHoldoutCorpus.postFixEvaluation.parity.exactRankDifferences -ne 0 -or
    [int]$retirementHoldoutCorpus.postFixEvaluation.parity.top5Differences -ne 0) {
    throw 'Independent JSON fallback retirement holdout must retain its post-fix quality and parity evidence.'
}
$runtimeRetirementEvidence = $retirementHoldoutCorpus.runtimeRetirementEvidence
$runtimeSampleCount = [int]$runtimeRetirementEvidence.sampleCount
$runtimeSqlitePrimaryCount = [int]$runtimeRetirementEvidence.sqlitePrimaryCount
$runtimeSqliteUnavailableCount = [int]$runtimeRetirementEvidence.sqliteUnavailableCount
$runtimeJsonFallbackCount = [int]$runtimeRetirementEvidence.jsonFallbackCount
$runtimeMaximumFallbackRate = [double]$runtimeRetirementEvidence.maximumJsonFallbackRate
$runtimeMinimumSampleCount = [int]$runtimeRetirementEvidence.minimumSampleCount
$expectedRuntimeFallbackRate = if ($runtimeSampleCount -eq 0) {
    0.0
} else {
    [Math]::Round(
        $runtimeJsonFallbackCount / $runtimeSampleCount,
        4,
        [MidpointRounding]::AwayFromZero)
}
$runtimeRequiredSampleCount = [Math]::Max(
    $runtimeMinimumSampleCount,
    $(if ($runtimeJsonFallbackCount -eq 0) {
            $runtimeMinimumSampleCount
        } else {
            [Math]::Ceiling($runtimeJsonFallbackCount / $runtimeMaximumFallbackRate)
        }))
$expectedAdditionalRuntimeSamples = [Math]::Max(0, $runtimeRequiredSampleCount - $runtimeSampleCount)
if ($runtimeSampleCount -ne
        $runtimeSqlitePrimaryCount + $runtimeSqliteUnavailableCount + $runtimeJsonFallbackCount -or
    [double]$runtimeRetirementEvidence.jsonFallbackRate -ne $expectedRuntimeFallbackRate -or
    [int]$runtimeRetirementEvidence.refreshAttemptCount -ne
        [int]$runtimeRetirementEvidence.refreshSuccessCount + [int]$runtimeRetirementEvidence.refreshFailureCount -or
    [int]$runtimeRetirementEvidence.consecutiveSqlitePrimaryCount -gt $runtimeSqlitePrimaryCount -or
    [int]$runtimeRetirementEvidence.minimumAdditionalSqlitePrimarySamplesRequired -ne
        $expectedAdditionalRuntimeSamples) {
    throw 'Committed runtime fallback-retirement evidence is internally inconsistent.'
}
$postFixControlCases = @($postFixControlCorpus.cases)
$postFixControlTargets = @($postFixControlCases | ForEach-Object { [string]@($_.expectedPaths)[0] })
if ($postFixControlCases.Count -ne 30 -or
    @($postFixControlCases.id | Sort-Object -Unique).Count -ne 30 -or
    @($postFixControlTargets | Sort-Object -Unique).Count -ne 30) {
    throw 'Post-fix control corpus must preserve 30 unique cases and targets.'
}
$reusedPostFixControlTargets = @($postFixControlTargets | Where-Object {
        $_ -in $promotedExpectedPaths -or $_ -in $retirementPrimaryTargets
    })
if ($reusedPostFixControlTargets.Count -gt 0) {
    throw "Post-fix control corpus reused earlier targets: $($reusedPostFixControlTargets -join ', ')."
}
$missingPostFixControlTargets = @($postFixControlTargets | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_))
    })
if ($missingPostFixControlTargets.Count -gt 0) {
    throw "Post-fix control corpus targets are missing: $($missingPostFixControlTargets -join ', ')."
}
if ([int]$postFixControlCorpus.blindBaseline.node.top1Count -ne 17 -or
    [int]$postFixControlCorpus.blindBaseline.node.top10Count -ne 29 -or
    [double]$postFixControlCorpus.blindBaseline.node.meanReciprocalRank -ne 0.7079) {
    throw 'Post-fix control corpus must retain its frozen first-run baseline.'
}
if ([int]$postFixControlCorpus.postFixEvaluation.node.top1Count -ne 27 -or
    [int]$postFixControlCorpus.postFixEvaluation.node.top10Count -ne 30 -or
    [double]$postFixControlCorpus.postFixEvaluation.node.meanReciprocalRank -ne 0.95 -or
    [int]$postFixControlCorpus.postFixEvaluation.parity.exactRankDifferences -ne 0 -or
    [int]$postFixControlCorpus.postFixEvaluation.parity.top5Differences -ne 0) {
    throw 'Post-fix control corpus must retain its tuned quality and parity evidence.'
}
if ([int]$postFixControlEvaluation.metrics.top1Count -lt 27 -or
    [int]$postFixControlEvaluation.metrics.top10Count -ne 30 -or
    [double]$postFixControlEvaluation.metrics.meanReciprocalRank -lt 0.95) {
    throw "Post-fix control regressed: top1=$($postFixControlEvaluation.metrics.top1Count)/30, top10=$($postFixControlEvaluation.metrics.top10Count)/30, MRR=$($postFixControlEvaluation.metrics.meanReciprocalRank)."
}
$postTuneControlCases = @($postTuneControlCorpus.cases)
$postTuneControlTargets = @($postTuneControlCases | ForEach-Object { [string]@($_.expectedPaths)[0] })
$postTuneAcceptedTargets = @($postTuneControlCases | ForEach-Object {
        $acceptedPathsProperty = $_.PSObject.Properties['acceptedPaths']
        if ($null -ne $acceptedPathsProperty) { @($acceptedPathsProperty.Value) }
    } | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
if ($postTuneControlCases.Count -ne 30 -or
    @($postTuneControlCases.id | Sort-Object -Unique).Count -ne 30 -or
    @($postTuneControlTargets | Sort-Object -Unique).Count -ne 30) {
    throw 'Second post-tuning control corpus must preserve 30 unique cases and targets.'
}
$reusedPostTuneControlTargets = @($postTuneControlTargets | Where-Object {
        $_ -in $promotedExpectedPaths -or
        $_ -in $retirementPrimaryTargets -or
        $_ -in $postFixControlTargets
    })
if ($reusedPostTuneControlTargets.Count -gt 0) {
    throw "Second post-tuning control corpus reused earlier targets: $($reusedPostTuneControlTargets -join ', ')."
}
$missingPostTuneControlTargets = @($postTuneControlTargets | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_))
    })
$missingPostTuneAcceptedTargets = @($postTuneAcceptedTargets | Where-Object {
        -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_))
    })
if ($missingPostTuneControlTargets.Count -gt 0 -or $missingPostTuneAcceptedTargets.Count -gt 0) {
    throw "Second post-tuning control corpus targets are missing: $(@($missingPostTuneControlTargets + $missingPostTuneAcceptedTargets) -join ', ')."
}
if ([int]$postTuneControlCorpus.blindBaseline.node.top1Count -ne 18 -or
    [int]$postTuneControlCorpus.blindBaseline.node.top10Count -ne 28 -or
    [double]$postTuneControlCorpus.blindBaseline.node.meanReciprocalRank -ne 0.7467 -or
    [int]$postTuneControlCorpus.blindBaseline.parity.exactRankDifferences -ne 0 -or
    [int]$postTuneControlCorpus.blindBaseline.parity.top5Differences -ne 0) {
    throw 'Second post-tuning control corpus must retain its frozen blind baseline and parity evidence.'
}
if ([int]$postTuneControlCorpus.postFixEvaluation.node.top1Count -ne 26 -or
    [int]$postTuneControlCorpus.postFixEvaluation.node.top10Count -ne 30 -or
    [double]$postTuneControlCorpus.postFixEvaluation.node.meanReciprocalRank -ne 0.925 -or
    [int]$postTuneControlCorpus.postFixEvaluation.parity.exactRankDifferences -ne 0 -or
    [int]$postTuneControlCorpus.postFixEvaluation.parity.top5Differences -ne 0) {
    throw 'Second post-tuning control corpus must retain its post-fix quality and parity evidence.'
}
if ([int]$postTuneControlEvaluation.metrics.top1Count -lt 26 -or
    [int]$postTuneControlEvaluation.metrics.top10Count -ne 30 -or
    [double]$postTuneControlEvaluation.metrics.meanReciprocalRank -lt 0.925) {
    throw "Second post-tuning control regressed: top1=$($postTuneControlEvaluation.metrics.top1Count)/30, top10=$($postTuneControlEvaluation.metrics.top10Count)/30, MRR=$($postTuneControlEvaluation.metrics.meanReciprocalRank)."
}
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
    [double]$probe6Evaluation.metrics.p95SqlDurationMs -lt 0 -or
    [double]$probe7Evaluation.metrics.p95SqlDurationMs -lt 0) {
    throw 'SQL context evaluation reported an invalid p95 duration.'
}

$strictCaseCount = [int](($allEvaluations.caseCount | Measure-Object -Sum).Sum)
$strictTop1Count = [int](($allEvaluations.metrics.top1Count | Measure-Object -Sum).Sum)
$strictTop10Count = [int](($allEvaluations.metrics.top10Count | Measure-Object -Sum).Sum)
if ($strictTop10Count -ne $strictCaseCount) {
    throw "Promoted SQL context evaluation requires every case in top-10: top10=$strictTop10Count/$strictCaseCount."
}
$probeCaseCount = [int](($probeEvaluation.caseCount, $probe2Evaluation.caseCount, $probe3Evaluation.caseCount, $probe4Evaluation.caseCount, $probe5Evaluation.caseCount, $probe6Evaluation.caseCount, $probe7Evaluation.caseCount | Measure-Object -Sum).Sum)
$probeTop1Count = [int](($probeEvaluation.metrics.top1Count, $probe2Evaluation.metrics.top1Count, $probe3Evaluation.metrics.top1Count, $probe4Evaluation.metrics.top1Count, $probe5Evaluation.metrics.top1Count, $probe6Evaluation.metrics.top1Count, $probe7Evaluation.metrics.top1Count | Measure-Object -Sum).Sum)
Write-Host "LLM Wiki SQL context evaluation passed: promoted top1=$strictTop1Count/$strictCaseCount and top10=$strictTop10Count/$strictCaseCount; promotion top10=$combinedTop10Count/$combinedCaseCount; primary MRR=$($primaryEvaluation.metrics.meanReciprocalRank), p95=$($primaryEvaluation.metrics.p95SqlDurationMs)ms; challenge MRR=$($challengeEvaluation.metrics.meanReciprocalRank), p95=$($challengeEvaluation.metrics.p95SqlDurationMs)ms; generalization MRR=$($generalizationEvaluation.metrics.meanReciprocalRank), p95=$($generalizationEvaluation.metrics.p95SqlDurationMs)ms; validation MRR=$($validationEvaluation.metrics.meanReciprocalRank), p95=$($validationEvaluation.metrics.p95SqlDurationMs)ms; probes top1=$probeTop1Count/$probeCaseCount; probe5 baseline=22/40 and promoted=$($probe5Evaluation.metrics.top1Count)/$($probe5Evaluation.caseCount); probe6 baseline=9/30 and promoted=$($probe6Evaluation.metrics.top1Count)/$($probe6Evaluation.caseCount); probe7 corrected baseline=18/40 and promoted=$($probe7Evaluation.metrics.top1Count)/$($probe7Evaluation.caseCount); controls=$($postFixControlEvaluation.metrics.top1Count)/30 and $($postTuneControlEvaluation.metrics.top1Count)/30 top1, both 30/30 top10."
