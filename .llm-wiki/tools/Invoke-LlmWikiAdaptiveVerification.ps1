[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$toolsRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))
$shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$checks = @(
    [pscustomobject]@{ name = 'adaptive routing'; script = Join-Path $toolsRoot 'Test-LlmWikiAdaptiveWorkflow.ps1'; arguments = '-Group Routing' }
    [pscustomobject]@{ name = 'adaptive experience'; script = Join-Path $toolsRoot 'Test-LlmWikiAdaptiveWorkflow.ps1'; arguments = '-Group Experience' }
    [pscustomobject]@{ name = 'integration scan'; script = Join-Path $toolsRoot 'Test-LlmWikiIntegrationScan.ps1'; arguments = '' }
    [pscustomobject]@{ name = 'evals 1/3'; script = Join-Path $toolsRoot 'Invoke-LlmWikiEvals.ps1'; arguments = '-ShardIndex 0 -ShardCount 3' }
    [pscustomobject]@{ name = 'evals 2/3'; script = Join-Path $toolsRoot 'Invoke-LlmWikiEvals.ps1'; arguments = '-ShardIndex 1 -ShardCount 3' }
    [pscustomobject]@{ name = 'evals 3/3'; script = Join-Path $toolsRoot 'Invoke-LlmWikiEvals.ps1'; arguments = '-ShardIndex 2 -ShardCount 3' }
)
$workers = [Collections.Generic.List[object]]::new()

foreach ($check in $checks) {
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $shellPath
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.Arguments = "-NoLogo -NoProfile -File `"$($check.script)`" $($check.arguments)"
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (-not $process.Start()) { throw "Unable to start LLM Wiki $($check.name) verification." }
    Write-Host " - started $($check.name) (pid=$($process.Id))"
    $workers.Add([pscustomobject]@{ check = $check; process = $process; stopwatch = [Diagnostics.Stopwatch]::StartNew() })
}

$failures = [Collections.Generic.List[string]]::new()
foreach ($worker in $workers) {
    try {
        $worker.process.WaitForExit()
        $worker.stopwatch.Stop()
        $actualSeconds = if ($worker.process.HasExited) { ($worker.process.ExitTime - $worker.process.StartTime).TotalSeconds } else { $worker.stopwatch.Elapsed.TotalSeconds }
        if ($worker.process.ExitCode -ne 0) { $failures.Add("$($worker.check.name) (exit=$($worker.process.ExitCode))") }
        Write-Host " - $($worker.check.name): $([Math]::Round($actualSeconds, 2))s"
    } finally {
        $worker.process.Dispose()
    }
}

if ($failures.Count -gt 0) { throw "LLM Wiki adaptive verification failed: $($failures -join ', ')" }
Write-Host 'LLM Wiki adaptive workflow and eval verification passed in parallel.'
