[CmdletBinding()]
param(
    [string]$CorpusPath = '.llm-wiki/evals/context-search-holdout-100.json',
    [ValidateRange(1, 16)]
    [int]$Workers = 4,
    [ValidateRange(1, 100)]
    [int]$QueriesPerWorker = 10,
    [ValidateRange(1, 7200)]
    [int]$TimeoutSeconds = 300,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiChangeSetSnapshot.ps1')
$resolvedCorpus = if ([IO.Path]::IsPathRooted($CorpusPath)) { $CorpusPath } else { Join-Path $repositoryRoot $CorpusPath }
$corpus = [IO.File]::ReadAllText((Resolve-Path $resolvedCorpus), [Text.Encoding]::UTF8) | ConvertFrom-Json
$cases = @($corpus.cases)
if ($cases.Count -eq 0) { throw 'Concurrency corpus is empty.' }
$manager = (Resolve-Path (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1')).Path
$workspaceBefore = Get-LlmWikiChangeSetSnapshot -RepositoryRoot $repositoryRoot
$wall = [Diagnostics.Stopwatch]::StartNew()
$jobs = @()
try {
for ($worker = 0; $worker -lt $Workers; $worker++) {
    $workerCases = for ($index = 0; $index -lt $QueriesPerWorker; $index++) {
        $cases[($worker * $QueriesPerWorker + $index) % $cases.Count]
    }
    $jobs += Start-Job -ScriptBlock {
        param($Manager, $Items)
        $ErrorActionPreference = 'Stop'
        # Node 22 reports the built-in SQLite module as experimental on stderr.
        # PowerShell remotes native stderr as ErrorRecord output from Start-Job,
        # which can terminate the parent smoke test under ErrorActionPreference=Stop.
        # Suppress only that warning category; non-zero Node exits still propagate
        # through Manage-LlmWikiCodeGraph and fail the worker.
        $env:NODE_OPTIONS = ("$env:NODE_OPTIONS --disable-warning=ExperimentalWarning").Trim()
        $durations = foreach ($item in $Items) {
            $started = [Diagnostics.Stopwatch]::StartNew()
            $response = & $Manager search -Query ([string]$item.query) -ChangeType ([string]$item.changeType) -Limit 10 -SkipRefresh -Format Json | ConvertFrom-Json
            if (-not $response.ready) { throw "Context search is not ready: $($item.id)." }
            $started.Stop()
            $started.Elapsed.TotalMilliseconds
        }
        [pscustomobject]@{ durations = @($durations) }
    } -ArgumentList $manager, $workerCases
}
while (@($jobs | Where-Object State -in @('Running', 'NotStarted', 'Blocked')).Count -gt 0) {
    if ($wall.Elapsed.TotalSeconds -ge $TimeoutSeconds) { throw "Context concurrency workers timed out after ${TimeoutSeconds}s." }
    $null = $jobs | Wait-Job -Timeout 1
}
$failed = @($jobs | Where-Object State -ne 'Completed')
$results = @($jobs | Receive-Job)
} finally {
    if ($jobs.Count) { $jobs | Stop-Job -ErrorAction SilentlyContinue; $jobs | Remove-Job -Force -ErrorAction SilentlyContinue }
}
$wall.Stop()
if ($failed.Count -gt 0) { throw "$($failed.Count) context concurrency worker(s) failed." }
$durations = @($results | ForEach-Object durations | Sort-Object)
if ($durations.Count -ne $Workers * $QueriesPerWorker) { throw 'Context concurrency workers returned incomplete query timings.' }
function Get-Percentile([double[]]$Values, [double]$Percentile) {
    if ($Values.Count -eq 0) { return 0 }
    return $Values[[Math]::Min($Values.Count - 1, [Math]::Ceiling($Values.Count * $Percentile) - 1)]
}
$workspaceAfter = Get-LlmWikiChangeSetSnapshot -RepositoryRoot $repositoryRoot
$summary = [pscustomobject][ordered]@{
    schemaVersion = 1
    workers = $Workers
    queryCount = $durations.Count
    wallDurationMs = [Math]::Round($wall.Elapsed.TotalMilliseconds, 2)
    throughputPerSecond = [Math]::Round($durations.Count / [Math]::Max($wall.Elapsed.TotalSeconds, 0.001), 2)
    queryP50Ms = [Math]::Round((Get-Percentile $durations 0.50), 2)
    queryP95Ms = [Math]::Round((Get-Percentile $durations 0.95), 2)
    workspaceStable = ($workspaceBefore.fingerprint -ceq $workspaceAfter.fingerprint)
}
if ($Format -eq 'Json') { $summary | ConvertTo-Json; return }
Write-Host "Context concurrency: workers=$Workers, queries=$($summary.queryCount), throughput=$($summary.throughputPerSecond)/s, p50=$($summary.queryP50Ms)ms, p95=$($summary.queryP95Ms)ms, workspace-stable=$($summary.workspaceStable)."
