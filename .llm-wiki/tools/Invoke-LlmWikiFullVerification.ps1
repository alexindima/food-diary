[CmdletBinding()]
param(
    [ValidateRange(1, 8)]
    [int]$IndexConcurrency = 4
)

$ErrorActionPreference = 'Stop'
$toolsRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))
$shellPath = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)

$checks = @(
    [pscustomobject]@{
        name = 'indexes'
        script = Join-Path $toolsRoot 'Invoke-LlmWikiIndexPipeline.ps1'
        arguments = "-Check -MaxConcurrency $IndexConcurrency"
    }
    [pscustomobject]@{
        name = 'tools'
        script = Join-Path $toolsRoot 'Test-LlmWikiTools.ps1'
        arguments = ''
    }
)

$workers = [System.Collections.Generic.List[object]]::new()
foreach ($check in $checks) {
    Write-Host "Starting LLM Wiki full verification group: $($check.name)"
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $shellPath
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.Arguments = "-NoLogo -NoProfile -File `"$($check.script)`" $($check.arguments)"
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw "Unable to start LLM Wiki verification group '$($check.name)'."
    }
    $workers.Add([pscustomobject]@{ name = $check.name; process = $process })
}

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($worker in $workers) {
    $worker.process.WaitForExit()
    $exitCode = $worker.process.ExitCode
    $worker.process.Dispose()
    if ($exitCode -eq 0) {
        Write-Host "LLM Wiki full verification group passed: $($worker.name)"
    } else {
        $failures.Add("$($worker.name) (exit=$exitCode)")
    }
}

if ($failures.Count -gt 0) {
    throw "LLM Wiki full verification failed: $($failures -join ', ')"
}

Write-Host 'LLM Wiki parallel full verification passed.'
