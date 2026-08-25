[CmdletBinding()]
param(
    [string]$CorpusPath = '.llm-wiki/evals/context-search-holdout-100.json',
    [string]$OutputPath,
    [switch]$SkipBuild,
    [switch]$FailOnRegression,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$resolvedCorpusPath = (Resolve-Path (Join-Path $repositoryRoot $CorpusPath)).Path
$evaluation = & (Join-Path $PSScriptRoot 'Measure-LlmWikiSqlContextEvaluation.ps1') `
    -CorpusPath $resolvedCorpusPath -SkipBuild:$SkipBuild -FailOnRegression:$FailOnRegression -Format Json | ConvertFrom-Json
$policyPath = Join-Path $repositoryRoot '.llm-wiki/policies/context-search-ranking.json'
$head = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve Git HEAD for context evaluation snapshot.' }
$snapshot = [pscustomobject][ordered]@{
    schemaVersion = 1
    recordedAtUtc = [DateTime]::UtcNow.ToString('o')
    gitHead = $head
    corpusPath = $resolvedCorpusPath.Substring($repositoryRoot.Length + 1).Replace('\', '/')
    corpusSha256 = (Get-FileHash -LiteralPath $resolvedCorpusPath -Algorithm SHA256).Hash.ToLowerInvariant()
    rankingPolicySha256 = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
    caseCount = [int]$evaluation.caseCount
    metrics = $evaluation.metrics
    cohortMetrics = @($evaluation.cohortMetrics)
    liveRegressionPassed = [bool]$evaluation.liveRegressionPassed
    liveRegressionGaps = @($evaluation.liveRegressionGaps)
    misses = @($evaluation.misses | Select-Object id, cohort, rank, query, expectedPaths, topCandidates)
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = ".artifacts/llm-wiki/context-evaluations/$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')).json"
}
$absoluteOutputPath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputPath))
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
if ($Format -eq 'Json') { $snapshot | ConvertTo-Json -Depth 10; exit 0 }
Write-Host "Context evaluation snapshot: $($snapshot.caseCount) cases, top1=$($snapshot.metrics.top1Count), top10=$($snapshot.metrics.top10Count), MRR=$($snapshot.metrics.meanReciprocalRank), p95=$($snapshot.metrics.p95SqlDurationMs)ms."
Write-Host "Snapshot: $OutputPath"
