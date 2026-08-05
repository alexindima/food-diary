[CmdletBinding()]
param(
    [ValidateRange(1, 8)]
    [int]$IndexConcurrency = 4,
    [switch]$FullTools,
    [switch]$CoreTools
)

$ErrorActionPreference = 'Stop'
$toolsRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))
$shellPath = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)

$changedPaths = @(& git diff --name-only HEAD^ HEAD 2>$null | ForEach-Object { ([string]$_).Replace('\', '/') })
$extendedToolPatterns = @(
    '^\.llm-wiki/tools/(Get-LlmWikiAgentFleetCoverage|Get-LlmWikiDispatchMetrics|Get-LlmWikiTaskAudit|Get-LlmWikiTaskHandoff|Get-LlmWikiTaskSchedule|Manage-LlmWikiAgentRegistry|Manage-LlmWikiContextFeedback|Manage-LlmWikiContextOutcome|Manage-LlmWikiOrchestrationCycle|Manage-LlmWikiQualityAdjustment|Manage-LlmWikiSchedulePlan|Manage-LlmWikiTaskDecomposition|Manage-LlmWikiTaskDispatch|Manage-LlmWikiTaskLease|Manage-LlmWikiWorkspaceCircuit|Test-LlmWikiOrchestrationLineage|Test-LlmWikiTools)\.ps1$'
    '^\.llm-wiki/policies/workspace-policies\.json$'
)
if ($FullTools -and $CoreTools) { throw 'FullTools and CoreTools cannot be used together.' }
$requiresExtendedTools = $FullTools -or (-not $CoreTools -and @($changedPaths | Where-Object {
    $path = $_
    @($extendedToolPatterns | Where-Object { $path -match $_ }).Count -gt 0
}).Count -gt 0)
$toolsProfile = if ($requiresExtendedTools) { 'Full' } else { 'Core' }
Write-Host "LLM Wiki tool verification profile: $toolsProfile (changed paths: $($changedPaths.Count))."

$checks = @(
    [pscustomobject]@{
        name = 'task baseline'
        script = Join-Path $toolsRoot 'Test-LlmWikiTaskBaseline.ps1'
        arguments = ''
    }
    [pscustomobject]@{
        name = 'verification cache'
        script = Join-Path $toolsRoot 'Test-LlmWikiVerificationCache.ps1'
        arguments = ''
    }
    [pscustomobject]@{
        name = 'indexes'
        script = Join-Path $toolsRoot 'Invoke-LlmWikiIndexPipeline.ps1'
        arguments = "-Check -MaxConcurrency $IndexConcurrency"
    }
    [pscustomobject]@{
        name = 'durable memory isolation'
        script = Join-Path $toolsRoot 'Test-LlmWikiMemoryIsolation.ps1'
        arguments = ''
    }
    [pscustomobject]@{
        name = 'tools'
        script = Join-Path $toolsRoot 'Test-LlmWikiTools.ps1'
        arguments = "-Profile $toolsProfile"
    }
)

foreach ($check in $checks) {
    Write-Host "Starting LLM Wiki full verification group: $($check.name)"
    $groupStopwatch = [Diagnostics.Stopwatch]::StartNew()
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
        $nextProgressAt = 30
        while (-not $process.WaitForExit(1000)) {
            if ($groupStopwatch.Elapsed.TotalSeconds -ge $nextProgressAt) {
                Write-Host "LLM Wiki full verification group still running: $($check.name) ($([Math]::Round($groupStopwatch.Elapsed.TotalSeconds))s)"
                $nextProgressAt += 30
            }
        }
        if ($process.ExitCode -ne 0) {
            throw "LLM Wiki full verification failed: $($check.name) (exit=$($process.ExitCode))"
        }
    } finally {
        $process.Dispose()
    }

    $groupStopwatch.Stop()
    Write-Host "LLM Wiki full verification group passed: $($check.name) ($([Math]::Round($groupStopwatch.Elapsed.TotalSeconds, 2))s)"
}

Write-Host 'LLM Wiki full verification passed.'
