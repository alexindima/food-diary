[CmdletBinding()]
param(
    [string]$CorpusPath = '.llm-wiki/evals/context-search-holdout-100.json',
    [ValidateRange(1, 16)]
    [int]$Workers = 4,
    [ValidateRange(1, 100)]
    [int]$QueriesPerWorker = 10,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$resolvedCorpus = if ([IO.Path]::IsPathRooted($CorpusPath)) { $CorpusPath } else { Join-Path $repositoryRoot $CorpusPath }
$corpus = [IO.File]::ReadAllText((Resolve-Path $resolvedCorpus), [Text.Encoding]::UTF8) | ConvertFrom-Json
$cases = @($corpus.cases)
if ($cases.Count -eq 0) { throw 'Concurrency corpus is empty.' }
$manager = (Resolve-Path (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1')).Path
$gitBefore = @(git -C $repositoryRoot status --porcelain=v1 --untracked-files=all)
$wall = [Diagnostics.Stopwatch]::StartNew()
$jobs = for ($worker = 0; $worker -lt $Workers; $worker++) {
    $workerCases = for ($index = 0; $index -lt $QueriesPerWorker; $index++) {
        $cases[($worker * $QueriesPerWorker + $index) % $cases.Count]
    }
    Start-Job -ScriptBlock {
        param($Manager, $Items)
        $durations = foreach ($item in $Items) {
            $started = [Diagnostics.Stopwatch]::StartNew()
            $null = & $Manager search -Query ([string]$item.query) -ChangeType ([string]$item.changeType) -Limit 10 -SkipRefresh -Format Json
            $started.Stop()
            $started.Elapsed.TotalMilliseconds
        }
        [pscustomobject]@{ durations = @($durations) }
    } -ArgumentList $manager, $workerCases
}
$null = $jobs | Wait-Job
$failed = @($jobs | Where-Object State -ne 'Completed')
$results = @($jobs | Receive-Job)
$jobs | Remove-Job
$wall.Stop()
if ($failed.Count -gt 0) { throw "$($failed.Count) context concurrency worker(s) failed." }
$durations = @($results | ForEach-Object durations | Sort-Object)
function Get-Percentile([double[]]$Values, [double]$Percentile) {
    if ($Values.Count -eq 0) { return 0 }
    return $Values[[Math]::Min($Values.Count - 1, [Math]::Ceiling($Values.Count * $Percentile) - 1)]
}
$gitAfter = @(git -C $repositoryRoot status --porcelain=v1 --untracked-files=all)
$summary = [pscustomobject][ordered]@{
    schemaVersion = 1
    workers = $Workers
    queryCount = $durations.Count
    wallDurationMs = [Math]::Round($wall.Elapsed.TotalMilliseconds, 2)
    throughputPerSecond = [Math]::Round($durations.Count / [Math]::Max($wall.Elapsed.TotalSeconds, 0.001), 2)
    queryP50Ms = [Math]::Round((Get-Percentile $durations 0.50), 2)
    queryP95Ms = [Math]::Round((Get-Percentile $durations 0.95), 2)
    workspaceStable = (($gitBefore -join "`n") -ceq ($gitAfter -join "`n"))
}
if ($Format -eq 'Json') { $summary | ConvertTo-Json; return }
Write-Host "Context concurrency: workers=$Workers, queries=$($summary.queryCount), throughput=$($summary.throughputPerSecond)/s, p50=$($summary.queryP50Ms)ms, p95=$($summary.queryP95Ms)ms, workspace-stable=$($summary.workspaceStable)."
