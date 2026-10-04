[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiCorpusEvaluation.ps1')
$fixtureParent = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki/corpus-pool-tests'))
$fixtureRoot = Join-Path $fixtureParent ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixtureRoot -Force
$evaluator = Join-Path $fixtureRoot 'evaluate fixture.ps1'
$originalConcurrency = $env:LLM_WIKI_SMOKE_MAX_CONCURRENCY
$originalCancellation = $env:LLM_WIKI_SMOKE_CANCEL_PATH

function New-CorpusFixture([string]$Name, [string]$Mode = 'success', [int]$Cases = 1, [string]$Peer = '') {
    $path = Join-Path $fixtureRoot $Name
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force
    $source = @{ cases = @(1..$Cases | ForEach-Object { @{ id = "$Name-$_" } }); mode = $Mode; peer = $Peer }
    [IO.File]::WriteAllText($path, ($source | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
    $path
}

function Assert-PoolFailure([string[]]$Paths, [string]$Message, [int]$TimeoutSeconds = 10) {
    $caught = $null
    try {
        Invoke-LlmWikiCorpusEvaluation -RepositoryRoot $repositoryRoot -CorpusPath $Paths -EvaluatorPath $evaluator -TimeoutSeconds $TimeoutSeconds | Out-Null
    } catch { $caught = $_ }
    if ($null -eq $caught -or [string]$caught -notlike "*$Message*") { throw "Expected pool failure '$Message', got '$caught'." }
}

function Assert-FixtureProcessesStopped([string[]]$Paths) {
    foreach ($path in $Paths) {
        $statePath = "$path.started.json"
        if (-not (Test-Path -LiteralPath $statePath)) { throw "Fixture did not start: $path" }
        $state = [IO.File]::ReadAllText($statePath) | ConvertFrom-Json
        foreach ($ownedId in @($state.worker, $state.child) | Where-Object { $_ }) {
            $process = $null
            try { $process = [Diagnostics.Process]::GetProcessById([int]$ownedId) } catch { continue }
            try {
                if (-not $process.HasExited) { throw "Owned fixture process $ownedId survived pool failure." }
            } finally { $process.Dispose() }
        }
    }
}

try {
    $env:LLM_WIKI_SMOKE_MAX_CONCURRENCY = $null
    $env:LLM_WIKI_SMOKE_CANCEL_PATH = $null
    @'
param([string]$CorpusPath, [switch]$SkipBuild, [string]$Format)
$ErrorActionPreference = 'Stop'
if (-not $SkipBuild -or $Format -ne 'Json') { throw 'Worker invocation lost SkipBuild or JSON format.' }
$source = [IO.File]::ReadAllText($CorpusPath) | ConvertFrom-Json
$childId = $null
if ($source.mode -in @('wait', 'cancel')) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = (Get-Process -Id $PID).Path
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    foreach ($argument in @('-NoLogo', '-NoProfile', '-Command', 'Start-Sleep -Seconds 120')) { $start.ArgumentList.Add($argument) }
    $child = [Diagnostics.Process]::Start($start)
    $childId = $child.Id
}
[IO.File]::WriteAllText("$CorpusPath.started.json", (@{worker=$PID;child=$childId} | ConvertTo-Json))
if ($source.peer) {
    $deadline = [DateTime]::UtcNow.AddSeconds(8)
    while (-not (Test-Path -LiteralPath $source.peer)) {
        if ([DateTime]::UtcNow -gt $deadline) { throw 'Concurrent peer did not start.' }
        Start-Sleep -Milliseconds 20
    }
}
if ($source.mode -eq 'fail') { [Console]::Error.WriteLine('fixture exit failure'); exit 17 }
if ($source.mode -eq 'cancel') { [IO.File]::WriteAllText($env:LLM_WIKI_SMOKE_CANCEL_PATH, 'cancel') }
if ($source.mode -in @('wait', 'cancel')) { $child.WaitForExit(); exit 0 }
if ($source.mode -eq 'invalid') { 'invalid JSON'; exit 0 }
if ($source.mode -eq 'slow') { Start-Sleep -Milliseconds 250 }
$results = @($source.cases | ForEach-Object { @{id=$_.id;value='Вики: поиск и проверка'} })
if ($source.mode -eq 'order') { [array]::Reverse($results) }
$path = if ($source.mode -eq 'identity') { 'wrong-corpus.json' } else { $CorpusPath.Replace('\', '/') }
$count = $results.Count + [int]($source.mode -eq 'count')
@{corpusPath=$path;caseCount=$count;results=$results} | ConvertTo-Json -Depth 5
'@ | Set-Content -LiteralPath $evaluator -Encoding utf8

    $first = New-CorpusFixture '00 медленный.json' 'slow' 2
    $second = New-CorpusFixture '01 быстрый.json' 'success' 1 "$first.started.json"
    $third = New-CorpusFixture '02 последний.json'
    $pool = Invoke-LlmWikiCorpusEvaluation -RepositoryRoot $repositoryRoot -CorpusPath @($third, $first, $second) -EvaluatorPath $evaluator
    if ($pool.CorpusCount -ne 3 -or $pool.PeakConcurrency -ne 2) { throw 'Pool lost a corpus or failed to bound concurrent workers to two.' }
    foreach ($path in @($first, $second, $third)) {
        $result = $pool.Evaluations[$path] | ConvertFrom-Json
        if ($result.results[0].value -cne 'Вики: поиск и проверка') { throw 'Worker output lost Unicode text.' }
        $report = Join-Path $repositoryRoot ".artifacts/llm-wiki/context-evaluation/$($pool.RunId)-$([IO.Path]::GetFileName($path))"
        if ([IO.File]::ReadAllText($report) -cne $pool.Evaluations[$path]) { throw 'Pool changed raw per-case diagnostics.' }
    }
    $env:LLM_WIKI_SMOKE_MAX_CONCURRENCY = '1'
    $serial = Invoke-LlmWikiCorpusEvaluation -RepositoryRoot $repositoryRoot -CorpusPath @($first, $third) -EvaluatorPath $evaluator
    if ($serial.PeakConcurrency -ne 1 -or $serial.CorpusCount -ne 2) { throw 'Outer serial smoke did not constrain the nested corpus pool.' }
    $env:LLM_WIKI_SMOKE_MAX_CONCURRENCY = $null

    $duplicateA = New-CorpusFixture 'one/same.json'
    $duplicateB = New-CorpusFixture 'two/same.json'
    Assert-PoolFailure @($duplicateA, $duplicateB) 'Duplicate corpus name'
    if (Test-Path -LiteralPath "$duplicateA.started.json") { throw 'Invalid corpus set launched a worker.' }
    foreach ($mode in @('invalid', 'identity', 'count', 'order')) {
        $bad = New-CorpusFixture "$mode.json" $mode 2
        $message = if ($mode -eq 'invalid') { 'invalid JSON' } else { 'case coverage or result order' }
        Assert-PoolFailure @($bad) $message
    }
    $waiting = New-CorpusFixture '00 waiting.json' 'wait' 3
    $failing = New-CorpusFixture '01 failing.json' 'fail' 2 "$waiting.started.json"
    $pending = New-CorpusFixture '02 pending.json'
    Assert-PoolFailure @($waiting, $failing, $pending) 'exit code 17'
    Assert-FixtureProcessesStopped @($waiting, $failing)
    if (Test-Path -LiteralPath "$pending.started.json") { throw 'Pool launched pending work after a worker failed.' }

    $timed = New-CorpusFixture 'timeout.json' 'wait'
    Assert-PoolFailure @($timed) 'timed out' 3
    Assert-FixtureProcessesStopped @($timed)
    $env:LLM_WIKI_SMOKE_CANCEL_PATH = Join-Path $fixtureRoot 'cancel.signal'
    $cancelled = New-CorpusFixture 'cancelled.json' 'cancel'
    Assert-PoolFailure @($cancelled) 'cancelled by the smoke supervisor'
    Assert-FixtureProcessesStopped @($cancelled)
    Write-Host 'LLM Wiki corpus evaluation pool passed: complete ordered coverage, bounded/serial workers, Unicode diagnostics, failure, timeout and cancellation cleanup.'
} finally {
    $env:LLM_WIKI_SMOKE_MAX_CONCURRENCY = $originalConcurrency
    $env:LLM_WIKI_SMOKE_CANCEL_PATH = $originalCancellation
    $resolvedFixtureRoot = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolvedFixtureRoot.StartsWith(($fixtureParent + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Corpus fixture cleanup escaped its artifact scope.'
    }
    Remove-Item -LiteralPath $resolvedFixtureRoot -Recurse -Force
}
