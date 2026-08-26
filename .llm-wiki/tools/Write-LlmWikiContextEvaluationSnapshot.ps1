[CmdletBinding()]
param(
    [string]$CorpusPath = '.llm-wiki/evals/context-search-holdout-100.json',
    [string]$OutputPath,
    [string]$SummaryOutputPath,
    [switch]$SkipBuild,
    [switch]$FailOnRegression,
    [ValidateRange(1, 20)]
    [int]$Iterations = 3,
    [ValidateRange(0, 10)]
    [int]$WarmupIterations = 1,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$corpusInputPath = if ([IO.Path]::IsPathRooted($CorpusPath)) { $CorpusPath } else { Join-Path $repositoryRoot $CorpusPath }
$resolvedCorpusPath = (Resolve-Path -LiteralPath $corpusInputPath).Path
$graphManager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$graphStatus = & $graphManager status -Format Json | ConvertFrom-Json
if (-not [bool]$graphStatus.changeSetFresh) {
    if ($SkipBuild) {
        throw "Context evaluation snapshot refused a stale code graph: graph=$($graphStatus.changeSetFingerprint), worktree=$($graphStatus.currentChangeSetFingerprint). Omit -SkipBuild or refresh the graph first."
    }
    & $graphManager build -Format Json | Out-Null
    $graphStatus = & $graphManager status -Format Json | ConvertFrom-Json
    if (-not [bool]$graphStatus.changeSetFresh) {
        throw 'Context evaluation snapshot could not refresh the code graph to the current worktree.'
    }
}
$changeSetFingerprintBefore = [string]$graphStatus.currentChangeSetFingerprint
$measure = Join-Path $PSScriptRoot 'Measure-LlmWikiSqlContextEvaluation.ps1'
for ($warmup = 0; $warmup -lt $WarmupIterations; $warmup++) {
    & $measure -CorpusPath $resolvedCorpusPath -SkipBuild -Format Json | Out-Null
}
$evaluations = @(
    for ($iteration = 0; $iteration -lt $Iterations; $iteration++) {
        & $measure -CorpusPath $resolvedCorpusPath -SkipBuild `
            -FailOnRegression:$FailOnRegression -Format Json | ConvertFrom-Json
    }
)
$graphStatusAfter = & $graphManager status -Format Json | ConvertFrom-Json
if ([string]$graphStatusAfter.currentChangeSetFingerprint -ne $changeSetFingerprintBefore) {
    throw 'The worktree changed while the context evaluation snapshot was being measured.'
}
$evaluation = $evaluations[-1]
$policyPath = Join-Path $repositoryRoot '.llm-wiki/policies/context-search-ranking.json'
$head = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve Git HEAD for context evaluation snapshot.' }
$workingTreeStatus = @(& git -C $repositoryRoot status --porcelain=v1 -uall)
if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect the working tree for context evaluation snapshot.' }
$statusText = $workingTreeStatus -join "`n"
$diffMaterial = @(& git -C $repositoryRoot diff --binary HEAD)
$untrackedPaths = @(& git -C $repositoryRoot ls-files --others --exclude-standard)
foreach ($untrackedPath in $untrackedPaths) {
    $absoluteUntrackedPath = Join-Path $repositoryRoot $untrackedPath
    if (Test-Path -LiteralPath $absoluteUntrackedPath -PathType Leaf) {
        $diffMaterial += "$untrackedPath=$((Get-FileHash -LiteralPath $absoluteUntrackedPath -Algorithm SHA256).Hash.ToLowerInvariant())"
    }
}
$sha = [Security.Cryptography.SHA256]::Create()
try { $workingTreeHash = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($statusText))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
$sha = [Security.Cryptography.SHA256]::Create()
try { $diffHash = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($diffMaterial -join "`n")))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
function Get-Percentile([double[]]$Values, [double]$Percentile) {
    $sorted = @($Values | Sort-Object)
    if ($sorted.Count -eq 0) { return $null }
    $index = [Math]::Ceiling($Percentile * $sorted.Count) - 1
    [Math]::Round([double]$sorted[[Math]::Max(0, $index)], 2)
}
$averageSamples = [double[]]@($evaluations | ForEach-Object { $_.metrics.averageSqlDurationMs })
$p95Samples = [double[]]@($evaluations | ForEach-Object { $_.metrics.p95SqlDurationMs })
$snapshot = [pscustomobject][ordered]@{
    schemaVersion = 3
    recordedAtUtc = [DateTime]::UtcNow.ToString('o')
    gitHead = $head
    workingTree = [pscustomobject][ordered]@{ clean = $workingTreeStatus.Count -eq 0; statusSha256 = $workingTreeHash; diffSha256 = $diffHash; changedPathCount = $workingTreeStatus.Count }
    runtime = [pscustomobject][ordered]@{
        reader = 'node-sqlite'
        powershell = $PSVersionTable.PSVersion.ToString()
        node = (& node --version).Trim()
        codeGraphParserVersion = [string]$graphStatus.parserVersion
        contextSearchSchemaVersion = 4
        codeGraphSourceSha256 = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'code-graph.mjs') -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    changeSetFingerprint = $changeSetFingerprintBefore
    graphChangeSetFingerprint = [string]$graphStatus.changeSetFingerprint
    corpusPath = $(
        $relativeCorpusPath = [IO.Path]::GetRelativePath($repositoryRoot, $resolvedCorpusPath)
        if ($relativeCorpusPath.StartsWith('..', [StringComparison]::Ordinal)) {
            $resolvedCorpusPath.Replace('\', '/')
        } else {
            $relativeCorpusPath.Replace('\', '/')
        }
    )
    corpusSha256 = (Get-FileHash -LiteralPath $resolvedCorpusPath -Algorithm SHA256).Hash.ToLowerInvariant()
    rankingPolicySha256 = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
    caseCount = [int]$evaluation.caseCount
    metrics = $evaluation.metrics
    performance = [pscustomobject][ordered]@{
        warmupIterations = $WarmupIterations; measuredIterations = $Iterations
        averageSqlDurationMs = [pscustomobject][ordered]@{ samples = $averageSamples; median = Get-Percentile $averageSamples 0.5; p90 = Get-Percentile $averageSamples 0.9; p95 = Get-Percentile $averageSamples 0.95 }
        queryP95SqlDurationMs = [pscustomobject][ordered]@{ samples = $p95Samples; median = Get-Percentile $p95Samples 0.5; p90 = Get-Percentile $p95Samples 0.9; p95 = Get-Percentile $p95Samples 0.95 }
    }
    cohortMetrics = @($evaluation.cohortMetrics)
    failureCategoryMetrics = @($evaluation.failureCategoryMetrics)
    liveRegressionPassed = [bool]$evaluation.liveRegressionPassed
    liveRegressionGaps = @($evaluation.liveRegressionGaps)
    misses = @($evaluation.misses | Select-Object id, cohort, rank, query, expectedPaths, topCandidates)
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = ".artifacts/llm-wiki/context-evaluations/$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')).json"
}
$absoluteOutputPath = if ([IO.Path]::IsPathRooted($OutputPath)) { [IO.Path]::GetFullPath($OutputPath) } else { [IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath)) }
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteOutputPath) -Force
$previousSnapshotPath = @(Get-ChildItem -LiteralPath (Split-Path -Parent $absoluteOutputPath) -Filter '*.json' -File -ErrorAction SilentlyContinue |
    Where-Object FullName -ne $absoluteOutputPath | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1)
if ($previousSnapshotPath.Count -eq 1) {
    try {
        $previous = Get-Content -LiteralPath $previousSnapshotPath[0].FullName -Raw | ConvertFrom-Json
        $snapshot | Add-Member -NotePropertyName deltaFromPrevious -NotePropertyValue ([pscustomobject][ordered]@{
            snapshot = $previousSnapshotPath[0].Name
            top1Count = [int]$snapshot.metrics.top1Count - [int]$previous.metrics.top1Count
            top10Count = [int]$snapshot.metrics.top10Count - [int]$previous.metrics.top10Count
            meanReciprocalRank = [Math]::Round([double]$snapshot.metrics.meanReciprocalRank - [double]$previous.metrics.meanReciprocalRank, 4)
            p95SqlDurationMs = [Math]::Round([double]$snapshot.metrics.p95SqlDurationMs - [double]$previous.metrics.p95SqlDurationMs, 2)
        })
    } catch {
        Write-Warning "Previous context evaluation snapshot could not be compared: $($_.Exception.Message)"
    }
}
[IO.File]::WriteAllText($absoluteOutputPath, (($snapshot | ConvertTo-Json -Depth 10) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
if (-not [string]::IsNullOrWhiteSpace($SummaryOutputPath)) {
    $absoluteSummaryPath = if ([IO.Path]::IsPathRooted($SummaryOutputPath)) {
        [IO.Path]::GetFullPath($SummaryOutputPath)
    } else {
        [IO.Path]::GetFullPath((Join-Path $repositoryRoot $SummaryOutputPath))
    }
    $relativeSnapshotPath = [IO.Path]::GetRelativePath($repositoryRoot, $absoluteOutputPath).Replace('\', '/')
    if ($relativeSnapshotPath.StartsWith('..', [StringComparison]::Ordinal)) {
        $relativeSnapshotPath = $absoluteOutputPath.Replace('\', '/')
    }
    $summaryLines = @(
        '---'
        'id: generated-context-evaluation-summary'
        'kind: system'
        'status: current'
        'generated_by: .llm-wiki/tools/Write-LlmWikiContextEvaluationSnapshot.ps1'
        'sources:'
        '  - .llm-wiki/tools/Write-LlmWikiContextEvaluationSnapshot.ps1'
        "  - $($snapshot.corpusPath)"
        '  - .llm-wiki/policies/context-search-ranking.json'
        '---'
        ''
        '# Context retrieval evaluation'
        ''
        '> Generated by `Write-LlmWikiContextEvaluationSnapshot.ps1`; do not edit measured values manually.'
        ''
        "- Recorded: $($snapshot.recordedAtUtc)"
        "- Corpus: ``$($snapshot.corpusPath)`` ($($snapshot.caseCount) cases)"
        "- Result: Top-1 $($snapshot.metrics.top1Count)/$($snapshot.caseCount), Top-10 $($snapshot.metrics.top10Count)/$($snapshot.caseCount), MRR $($snapshot.metrics.meanReciprocalRank)"
        "- Performance: average $($snapshot.metrics.averageSqlDurationMs) ms, query p95 $($snapshot.metrics.p95SqlDurationMs) ms"
        "- Engine: ``$($snapshot.runtime.reader)``, context-search schema $($snapshot.runtime.contextSearchSchemaVersion), source ``$($snapshot.runtime.codeGraphSourceSha256)``"
        "- Provenance: Git ``$($snapshot.gitHead)``, corpus ``$($snapshot.corpusSha256)``, policy ``$($snapshot.rankingPolicySha256)``"
        "- Working tree: clean=$($snapshot.workingTree.clean), changed paths=$($snapshot.workingTree.changedPathCount)"
        "- Snapshot: ``$relativeSnapshotPath``"
        ''
        '## Cohorts'
        ''
        '| Cohort | Top-1 | Top-10 | MRR |'
        '| --- | ---: | ---: | ---: |'
    )
    foreach ($cohort in @($snapshot.cohortMetrics)) {
        $summaryLines += "| $($cohort.cohort) | $($cohort.top1Count)/$($cohort.caseCount) | $($cohort.top10Count)/$($cohort.caseCount) | $($cohort.meanReciprocalRank) |"
    }
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteSummaryPath) -Force
    [IO.File]::WriteAllText($absoluteSummaryPath, (($summaryLines -join [Environment]::NewLine) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}
if ($Format -eq 'Json') { $snapshot | ConvertTo-Json -Depth 10; exit 0 }
Write-Host "Context evaluation snapshot: $($snapshot.caseCount) cases, top1=$($snapshot.metrics.top1Count), top10=$($snapshot.metrics.top10Count), MRR=$($snapshot.metrics.meanReciprocalRank), p95=$($snapshot.metrics.p95SqlDurationMs)ms."
Write-Host "Snapshot: $OutputPath"
if (-not [string]::IsNullOrWhiteSpace($SummaryOutputPath)) { Write-Host "Summary: $SummaryOutputPath" }
