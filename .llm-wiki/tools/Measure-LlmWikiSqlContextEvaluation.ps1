[CmdletBinding()]
param(
    [string]$CorpusPath,
    [switch]$SkipBuild,
    [switch]$NoBatch,
    [switch]$FailOnRegression,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
if ([string]::IsNullOrWhiteSpace($CorpusPath)) { $CorpusPath = Join-Path $PSScriptRoot '../evals/context-search.json' }
$resolvedCorpusPath = (Resolve-Path -LiteralPath $CorpusPath).Path
$corpus = [IO.File]::ReadAllText($resolvedCorpusPath, [Text.Encoding]::UTF8) | ConvertFrom-Json
if ([int]$corpus.schemaVersion -ne 1) { throw "Unsupported SQL context evaluation schema: $($corpus.schemaVersion)." }
$cases = @($corpus.cases)
if ($cases.Count -eq 0) { throw 'SQL context evaluation corpus is empty.' }
$duplicateIds = @($cases | Group-Object id | Where-Object Count -gt 1)
if ($duplicateIds.Count -gt 0) { throw "SQL context evaluation contains duplicate ids: $($duplicateIds.Name -join ', ')." }
$diagnosticLimit = [Math]::Max(10, [Math]::Min(500, [int]$corpus.diagnosticLimit))
if (-not $SkipBuild) { & $manager build -Format Json | Out-Null }

$results = [Collections.Generic.List[object]]::new()
function Get-FailureCategory([string]$Query, [string]$ExpectedPath, [string]$TopPath, [string]$ChangeType) {
    if ([string]::IsNullOrWhiteSpace($TopPath)) { return 'candidate-recall' }
    $expectedTest = $ExpectedPath -match '(^|/)(tests?|[^/]+\.tests?)(/|$)|\.(spec|test)\.'
    $topTest = $TopPath -match '(^|/)(tests?|[^/]+\.tests?)(/|$)|\.(spec|test)\.'
    if ($expectedTest -ne $topTest) { return 'test-production-collision' }
    $expectedExtension = [IO.Path]::GetExtension($ExpectedPath).ToLowerInvariant()
    $topExtension = [IO.Path]::GetExtension($TopPath).ToLowerInvariant()
    if ($expectedExtension -ne $topExtension -and @('.ps1', '.mjs', '.js', '.ts') -contains $expectedExtension) { return 'runtime-mismatch' }
    $expectedRoot = ($ExpectedPath -split '/')[0]
    $topRoot = ($TopPath -split '/')[0]
    if ($expectedRoot -cne $topRoot) { return 'layer-or-module-confusion' }
    if ($Query -match '[\p{IsCyrillic}]') { return 'multilingual-disambiguation' }
    if ($ExpectedPath.StartsWith('.llm-wiki/', [StringComparison]::OrdinalIgnoreCase)) { return 'wiki-tool-intent' }
    if ($ChangeType -ne 'Any') { return 'role-disambiguation' }
    'identity-ranking'
}
$batchResults = @()
$batchPath = $null
if (-not $NoBatch) {
    $batchRoot = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path '.artifacts/llm-wiki/eval-batches'
    $null = New-Item -ItemType Directory -Path $batchRoot -Force
    $batchPath = Join-Path $batchRoot "$PID-$([guid]::NewGuid().ToString('N')).json"
    $batchRequests = @($cases | ForEach-Object {
        [pscustomobject][ordered]@{
            query = [string]$_.query
            changeType = $(if ([string]::IsNullOrWhiteSpace([string]$_.changeType)) { 'Any' } else { [string]$_.changeType })
            limit = $diagnosticLimit
        }
    })
    [IO.File]::WriteAllText($batchPath, (($batchRequests | ConvertTo-Json -Depth 4 -AsArray) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    try {
        $batch = & $manager search-batch -InputPath $batchPath -SkipRefresh -Format Json | ConvertFrom-Json
        $batchResults = @($batch.results)
        if ($batchResults.Count -ne $cases.Count) { throw "SQL context batch returned $($batchResults.Count)/$($cases.Count) result(s)." }
    } finally {
        Remove-Item -LiteralPath $batchPath -Force -ErrorAction SilentlyContinue
    }
}
$caseIndex = 0
foreach ($case in $cases) {
    $expectedPaths = [string[]]@($case.expectedPaths | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($expectedPaths.Count -eq 0) { throw "SQL context evaluation case '$($case.id)' has no expected path." }
    $acceptedPathsProperty = $case.PSObject.Properties['acceptedPaths']
    $acceptedPaths = [string[]]@(
        if ($null -ne $acceptedPathsProperty) {
            $acceptedPathsProperty.Value |
                Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) -and [string]$_ -notin $expectedPaths } |
                Sort-Object -Unique
        }
    )
    $relevantPaths = [string[]]@($expectedPaths + $acceptedPaths)
    $changeType = if ([string]::IsNullOrWhiteSpace([string]$case.changeType)) { 'Any' } else { [string]$case.changeType }
    $cohortProperty = $case.PSObject.Properties['cohort']
    $cohort = if ($null -eq $cohortProperty -or [string]::IsNullOrWhiteSpace([string]$cohortProperty.Value)) {
        'unclassified'
    } else {
        [string]$cohortProperty.Value
    }
    $search = if ($NoBatch) {
        & $manager search `
            -Query ([string]$case.query) `
            -ChangeType $changeType `
            -Limit $diagnosticLimit `
            -SkipRefresh `
            -Format Json | ConvertFrom-Json
    } else { $batchResults[$caseIndex] }
    $records = @($search.records)
    $relevant = @($records | Where-Object { [string]$_.path -in $relevantPaths } | Sort-Object rank | Select-Object -First 1)
    $rank = if ($relevant.Count -eq 1) { [int]$relevant[0].rank } else { $null }
    $topPath = if ($records.Count -eq 0) { '' } else { [string]$records[0].path }
    $topCandidate = if ($records.Count -eq 0) { $null } else { $records[0] }
    $results.Add([pscustomobject][ordered]@{
        id = [string]$case.id
        query = [string]$case.query
        changeType = $changeType
        cohort = $cohort
        expectedPaths = $expectedPaths
        acceptedPaths = $acceptedPaths
        rank = $rank
        reciprocalRank = $(if ($null -eq $rank) { 0.0 } else { 1.0 / $rank })
        top1 = $rank -eq 1
        top10 = $null -ne $rank -and $rank -le 10
        topCandidateConfidence = $(if ($null -eq $topCandidate) { 'unavailable' } else { [string]$topCandidate.confidence })
        topCandidateAmbiguous = $null -ne $topCandidate -and [bool]$topCandidate.ambiguous
        failureCategory = $(if ($rank -eq 1) { $null } else { Get-FailureCategory ([string]$case.query) $expectedPaths[0] $topPath $changeType })
        sqlDurationMs = [double]$search.durationMs
        topCandidates = @($records | Select-Object -First 5 rank, path, recordType, score, scoreMargin, confidence, ambiguous, ambiguityReason, sameNameCandidateCount)
    })
    $caseIndex++
}

$top1Count = @($results | Where-Object top1).Count
$top10Count = @($results | Where-Object top10).Count
$top1Rate = $top1Count / $results.Count
$top10Rate = $top10Count / $results.Count
$mrr = [double](($results.reciprocalRank | Measure-Object -Average).Average)
$durations = [double[]]@($results.sqlDurationMs | Sort-Object)
$p95Index = [Math]::Max(0, [Math]::Ceiling($durations.Count * 0.95) - 1)
$cohortMetrics = @($results | Group-Object cohort | Sort-Object Name | ForEach-Object {
        $cohortResults = @($_.Group)
        $cohortTop1Count = @($cohortResults | Where-Object top1).Count
        $cohortTop10Count = @($cohortResults | Where-Object top10).Count
        [pscustomobject][ordered]@{
            cohort = [string]$_.Name
            caseCount = $cohortResults.Count
            top1Count = $cohortTop1Count
            top1Rate = [Math]::Round($cohortTop1Count / $cohortResults.Count, 4)
            top10Count = $cohortTop10Count
            top10Rate = [Math]::Round($cohortTop10Count / $cohortResults.Count, 4)
            meanReciprocalRank = [Math]::Round([double](($cohortResults.reciprocalRank | Measure-Object -Average).Average), 4)
        }
    })
$failureCategoryMetrics = @($results | Where-Object { -not $_.top1 } | Group-Object failureCategory | Sort-Object Count -Descending | ForEach-Object {
        [pscustomobject][ordered]@{ category = $_.Name; count = $_.Count; top10Misses = @($_.Group | Where-Object { -not $_.top10 }).Count }
    })
$confidenceMetrics = @($results | Group-Object topCandidateConfidence | Sort-Object Name | ForEach-Object {
        $confidenceResults = @($_.Group)
        $confidenceTop1Count = @($confidenceResults | Where-Object top1).Count
        [pscustomobject][ordered]@{
            confidence = [string]$_.Name
            caseCount = $confidenceResults.Count
            top1Count = $confidenceTop1Count
            precision = [Math]::Round($confidenceTop1Count / $confidenceResults.Count, 4)
            coverage = [Math]::Round($confidenceResults.Count / $results.Count, 4)
            ambiguousCount = @($confidenceResults | Where-Object topCandidateAmbiguous).Count
        }
    })
$acceptedResults = @($results | Where-Object {
        -not $_.topCandidateAmbiguous -and $_.topCandidateConfidence -in @('high', 'medium')
    })
$abstainedResults = @($results | Where-Object {
        $_.topCandidateAmbiguous -or $_.topCandidateConfidence -notin @('high', 'medium')
    })
$wrongResults = @($results | Where-Object { -not $_.top1 })
$acceptedTop1Count = @($acceptedResults | Where-Object top1).Count
$capturedErrorCount = @($abstainedResults | Where-Object { -not $_.top1 }).Count
$abstentionMetrics = [pscustomobject][ordered]@{
    acceptedCount = $acceptedResults.Count
    acceptedCoverage = [Math]::Round($acceptedResults.Count / $results.Count, 4)
    acceptedPrecision = $(if ($acceptedResults.Count -eq 0) { 0.0 } else { [Math]::Round($acceptedTop1Count / $acceptedResults.Count, 4) })
    abstainedCount = $abstainedResults.Count
    abstentionRate = [Math]::Round($abstainedResults.Count / $results.Count, 4)
    capturedErrorCount = $capturedErrorCount
    errorCaptureRate = $(if ($wrongResults.Count -eq 0) { 1.0 } else { [Math]::Round($capturedErrorCount / $wrongResults.Count, 4) })
}
$liveGateProperty = $corpus.PSObject.Properties['liveRegressionGate']
$liveGate = if ($null -eq $liveGateProperty) { $null } else { $liveGateProperty.Value }
$liveGateGaps = [Collections.Generic.List[string]]::new()
if ($null -ne $liveGate) {
    if ($top1Count -lt [int]$liveGate.minimumTop1Count) { $liveGateGaps.Add("top1=$top1Count<$($liveGate.minimumTop1Count)") }
    if ($top10Count -lt [int]$liveGate.minimumTop10Count) { $liveGateGaps.Add("top10=$top10Count<$($liveGate.minimumTop10Count)") }
    if ($mrr -lt [double]$liveGate.minimumMeanReciprocalRank) { $liveGateGaps.Add("mrr=$([Math]::Round($mrr, 4))<$($liveGate.minimumMeanReciprocalRank)") }
    $maximumP95 = [double]$liveGate.maximumP95SqlDurationMs
    if ($maximumP95 -gt 0 -and $durations[$p95Index] -gt $maximumP95) { $liveGateGaps.Add("p95=$([Math]::Round($durations[$p95Index], 2))>${maximumP95}") }
    foreach ($cohortMinimum in @($liveGate.minimumCohortTop1Counts.PSObject.Properties)) {
        $observed = @($cohortMetrics | Where-Object cohort -eq $cohortMinimum.Name | Select-Object -First 1)
        $observedCount = if ($observed.Count -eq 0) { 0 } else { [int]$observed[0].top1Count }
        if ($observedCount -lt [int]$cohortMinimum.Value) { $liveGateGaps.Add("cohort:$($cohortMinimum.Name)=$observedCount<$($cohortMinimum.Value)") }
    }
    if ($liveGate.PSObject.Properties['minimumAcceptedPrecision'] -and
        $abstentionMetrics.acceptedPrecision -lt [double]$liveGate.minimumAcceptedPrecision) {
        $liveGateGaps.Add("acceptedPrecision=$($abstentionMetrics.acceptedPrecision)<$($liveGate.minimumAcceptedPrecision)")
    }
    if ($liveGate.PSObject.Properties['minimumAcceptedCoverage'] -and
        $abstentionMetrics.acceptedCoverage -lt [double]$liveGate.minimumAcceptedCoverage) {
        $liveGateGaps.Add("acceptedCoverage=$($abstentionMetrics.acceptedCoverage)<$($liveGate.minimumAcceptedCoverage)")
    }
    if ($liveGate.PSObject.Properties['minimumErrorCaptureRate'] -and
        $abstentionMetrics.errorCaptureRate -lt [double]$liveGate.minimumErrorCaptureRate) {
        $liveGateGaps.Add("errorCaptureRate=$($abstentionMetrics.errorCaptureRate)<$($liveGate.minimumErrorCaptureRate)")
    }
}
$liveGatePassed = $null -eq $liveGate -or $liveGateGaps.Count -eq 0
$thresholds = [pscustomobject][ordered]@{
    minimumTop1Rate = [double]$corpus.thresholds.minimumTop1Rate
    minimumTop10Rate = [double]$corpus.thresholds.minimumTop10Rate
    minimumMeanReciprocalRank = [double]$corpus.thresholds.minimumMeanReciprocalRank
}
$passed = $top1Rate -ge $thresholds.minimumTop1Rate -and
    $top10Rate -ge $thresholds.minimumTop10Rate -and
    $mrr -ge $thresholds.minimumMeanReciprocalRank
$switchCriteria = [pscustomobject][ordered]@{
    minimumCaseCount = [int]$corpus.switchCriteria.minimumCaseCount
    minimumTop1Rate = [double]$corpus.switchCriteria.minimumTop1Rate
    minimumTop10Rate = [double]$corpus.switchCriteria.minimumTop10Rate
    minimumMeanReciprocalRank = [double]$corpus.switchCriteria.minimumMeanReciprocalRank
}
$switchReady = $results.Count -ge $switchCriteria.minimumCaseCount -and
    $top1Rate -ge $switchCriteria.minimumTop1Rate -and
    $top10Rate -ge $switchCriteria.minimumTop10Rate -and
    $mrr -ge $switchCriteria.minimumMeanReciprocalRank
$switchGaps = [Collections.Generic.List[string]]::new()
if ($results.Count -lt $switchCriteria.minimumCaseCount) {
    $switchGaps.Add("caseCount=$($results.Count)<$($switchCriteria.minimumCaseCount)")
}
if ($top1Rate -lt $switchCriteria.minimumTop1Rate) {
    $switchGaps.Add("top1=$([Math]::Round($top1Rate, 4))<$($switchCriteria.minimumTop1Rate)")
}
if ($top10Rate -lt $switchCriteria.minimumTop10Rate) {
    $switchGaps.Add("top10=$([Math]::Round($top10Rate, 4))<$($switchCriteria.minimumTop10Rate)")
}
if ($mrr -lt $switchCriteria.minimumMeanReciprocalRank) {
    $switchGaps.Add("mrr=$([Math]::Round($mrr, 4))<$($switchCriteria.minimumMeanReciprocalRank)")
}
$evaluation = [pscustomobject][ordered]@{
    schemaVersion = 1
    corpusPath = $resolvedCorpusPath.Replace('\', '/')
    passed = $passed
    switchReady = $switchReady
    caseCount = $results.Count
    metrics = [pscustomobject][ordered]@{
        top1Count = $top1Count
        top1Rate = [Math]::Round($top1Rate, 4)
        top10Count = $top10Count
        top10Rate = [Math]::Round($top10Rate, 4)
        meanReciprocalRank = [Math]::Round($mrr, 4)
        averageSqlDurationMs = [Math]::Round([double](($durations | Measure-Object -Average).Average), 2)
        p95SqlDurationMs = [Math]::Round($durations[$p95Index], 2)
    }
    cohortMetrics = $cohortMetrics
    failureCategoryMetrics = $failureCategoryMetrics
    confidenceMetrics = $confidenceMetrics
    abstentionMetrics = $abstentionMetrics
    thresholds = $thresholds
    switchCriteria = $switchCriteria
    switchGaps = @($switchGaps)
    liveRegressionGate = $liveGate
    liveRegressionPassed = $liveGatePassed
    liveRegressionGaps = @($liveGateGaps)
    misses = @($results | Where-Object { -not $_.top10 })
    results = @($results)
}

if ($Format -eq 'Json') {
    $evaluation | ConvertTo-Json -Depth 10
} else {
    Write-Host "SQL context evaluation: passed=$passed, switchReady=$switchReady, cases=$($results.Count), top1=$top1Count/$($results.Count) ($([Math]::Round($top1Rate * 100, 1))%), top10=$top10Count/$($results.Count) ($([Math]::Round($top10Rate * 100, 1))%), MRR=$([Math]::Round($mrr, 4)), SQL p95=$($evaluation.metrics.p95SqlDurationMs)ms."
    foreach ($cohortMetric in $cohortMetrics) {
        Write-Host " Cohort $($cohortMetric.cohort): top1=$($cohortMetric.top1Count)/$($cohortMetric.caseCount), top10=$($cohortMetric.top10Count)/$($cohortMetric.caseCount), MRR=$($cohortMetric.meanReciprocalRank)."
    }
    Write-Host " Confidence acceptance: precision=$($abstentionMetrics.acceptedPrecision), coverage=$($abstentionMetrics.acceptedCoverage), abstained=$($abstentionMetrics.abstainedCount), capturedErrors=$($abstentionMetrics.capturedErrorCount)/$($wrongResults.Count)."
    if (-not $switchReady) {
        Write-Host " Switch gaps: $($switchGaps -join '; ')"
    }
    foreach ($miss in @($evaluation.misses)) {
        $observedRank = if ($null -eq $miss.rank) { ">$diagnosticLimit" } else { [string]$miss.rank }
        Write-Host " - miss $($miss.id): rank=$observedRank; expected=$($miss.expectedPaths -join ', '); top=$(@($miss.topCandidates.path) -join ', ')"
    }
}
if ($FailOnRegression -and -not $liveGatePassed) {
    throw "SQL context evaluation regressed below its live gate: $($liveGateGaps -join '; ')."
}
if ($FailOnRegression -and $null -eq $liveGate -and -not $passed) {
    throw "SQL context evaluation regressed below its committed thresholds: top1=$([Math]::Round($top1Rate, 4)), top10=$([Math]::Round($top10Rate, 4)), MRR=$([Math]::Round($mrr, 4))."
}
