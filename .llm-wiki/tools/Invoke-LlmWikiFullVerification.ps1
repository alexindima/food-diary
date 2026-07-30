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

    try {
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) {
            throw "LLM Wiki full verification failed: $($check.name) (exit=$($process.ExitCode))"
        }
    } finally {
        $process.Dispose()
    }

    Write-Host "LLM Wiki full verification group passed: $($check.name)"
}

Write-Host 'LLM Wiki full verification passed.'
