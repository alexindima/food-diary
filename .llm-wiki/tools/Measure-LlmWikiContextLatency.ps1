[CmdletBinding()]
param(
    [string]$CorpusPath = '.llm-wiki/evals/context-search-holdout-100.json',
    [ValidateRange(1, 20)]
    [int]$Iterations = 3,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$measure = Join-Path $PSScriptRoot 'Measure-LlmWikiSqlContextEvaluation.ps1'
function Get-WorkspaceFingerprint {
    $status = (& git -C $repositoryRoot status --porcelain=v1 -z) -join ''
    $bytes = [Text.Encoding]::UTF8.GetBytes($status)
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}
function Get-Percentile([double[]]$Values, [double]$Percentile) {
    if ($Values.Count -eq 0) { return 0.0 }
    $ordered = @($Values | Sort-Object)
    $index = [Math]::Max(0, [Math]::Ceiling($ordered.Count * $Percentile) - 1)
    [Math]::Round([double]$ordered[$index], 2)
}
$before = Get-WorkspaceFingerprint
$runs = [Collections.Generic.List[object]]::new()
for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $evaluation = & $measure -CorpusPath $CorpusPath -SkipBuild -Format Json | ConvertFrom-Json
    $stopwatch.Stop()
    $durations = [double[]]@($evaluation.results.sqlDurationMs)
    $runs.Add([pscustomobject][ordered]@{
        iteration = $iteration
        wallClockMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
        queryP50Ms = Get-Percentile $durations 0.5
        queryP95Ms = Get-Percentile $durations 0.95
        top1Rate = [double]$evaluation.metrics.top1Rate
        top10Rate = [double]$evaluation.metrics.top10Rate
        meanReciprocalRank = [double]$evaluation.metrics.meanReciprocalRank
    })
}
$after = Get-WorkspaceFingerprint
$warmRuns = @($(if ($runs.Count -gt 1) { @($runs | Select-Object -Skip 1) } else { @($runs) }))
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    corpusPath = $CorpusPath.Replace('\\', '/')
    iterations = $Iterations
    workspaceStable = $before -eq $after
    workspaceFingerprintBefore = $before
    workspaceFingerprintAfter = $after
    warmQueryP50Ms = Get-Percentile ([double[]]@($warmRuns.queryP50Ms)) 0.5
    warmQueryP95Ms = Get-Percentile ([double[]]@($warmRuns.queryP95Ms)) 0.95
    wallClockP50Ms = Get-Percentile ([double[]]@($runs.wallClockMs)) 0.5
    runs = @($runs)
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6; exit 0 }
Write-Host "Context latency: iterations=$Iterations, stable=$($result.workspaceStable), warm-p50=$($result.warmQueryP50Ms)ms, warm-p95=$($result.warmQueryP95Ms)ms, wall-p50=$($result.wallClockP50Ms)ms."
$runs | Format-Table -AutoSize
