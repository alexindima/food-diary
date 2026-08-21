[CmdletBinding()]
param(
    [string]$CorpusPath,
    [switch]$SkipBuild,
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
foreach ($case in $cases) {
    $expectedPaths = [string[]]@($case.expectedPaths | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($expectedPaths.Count -eq 0) { throw "SQL context evaluation case '$($case.id)' has no expected path." }
    $changeType = if ([string]::IsNullOrWhiteSpace([string]$case.changeType)) { 'Any' } else { [string]$case.changeType }
    $search = & $manager search `
        -Query ([string]$case.query) `
        -ChangeType $changeType `
        -Limit $diagnosticLimit `
        -SkipRefresh `
        -Format Json | ConvertFrom-Json
    $records = @($search.records)
    $relevant = @($records | Where-Object { [string]$_.path -in $expectedPaths } | Sort-Object rank | Select-Object -First 1)
    $rank = if ($relevant.Count -eq 1) { [int]$relevant[0].rank } else { $null }
    $results.Add([pscustomobject][ordered]@{
        id = [string]$case.id
        query = [string]$case.query
        changeType = $changeType
        expectedPaths = $expectedPaths
        rank = $rank
        reciprocalRank = $(if ($null -eq $rank) { 0.0 } else { 1.0 / $rank })
        top1 = $rank -eq 1
        top10 = $null -ne $rank -and $rank -le 10
        sqlDurationMs = [double]$search.durationMs
        topCandidates = @($records | Select-Object -First 5 rank, path, recordType, score)
    })
}

$top1Count = @($results | Where-Object top1).Count
$top10Count = @($results | Where-Object top10).Count
$top1Rate = $top1Count / $results.Count
$top10Rate = $top10Count / $results.Count
$mrr = [double](($results.reciprocalRank | Measure-Object -Average).Average)
$durations = [double[]]@($results.sqlDurationMs | Sort-Object)
$p95Index = [Math]::Max(0, [Math]::Ceiling($durations.Count * 0.95) - 1)
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
    thresholds = $thresholds
    switchCriteria = $switchCriteria
    switchGaps = @($switchGaps)
    misses = @($results | Where-Object { -not $_.top10 })
    results = @($results)
}

if ($Format -eq 'Json') {
    $evaluation | ConvertTo-Json -Depth 10
} else {
    Write-Host "SQL context evaluation: passed=$passed, switchReady=$switchReady, cases=$($results.Count), top1=$top1Count/$($results.Count) ($([Math]::Round($top1Rate * 100, 1))%), top10=$top10Count/$($results.Count) ($([Math]::Round($top10Rate * 100, 1))%), MRR=$([Math]::Round($mrr, 4)), SQL p95=$($evaluation.metrics.p95SqlDurationMs)ms."
    if (-not $switchReady) {
        Write-Host " Switch gaps: $($switchGaps -join '; ')"
    }
    foreach ($miss in @($evaluation.misses)) {
        $observedRank = if ($null -eq $miss.rank) { ">$diagnosticLimit" } else { [string]$miss.rank }
        Write-Host " - miss $($miss.id): rank=$observedRank; expected=$($miss.expectedPaths -join ', '); top=$(@($miss.topCandidates.path) -join ', ')"
    }
}
if ($FailOnRegression -and -not $passed) {
    throw "SQL context evaluation regressed below its committed thresholds: top1=$([Math]::Round($top1Rate, 4)), top10=$([Math]::Round($top10Rate, 4)), MRR=$([Math]::Round($mrr, 4))."
}
