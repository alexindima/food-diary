[CmdletBinding()]
param(
    [ValidateRange(1, 8)]
    [int]$IndexConcurrency = 4,
    [switch]$FullTools,
    [switch]$CoreTools,
    [switch]$ResumePassedStages,
    [ValidateRange(30, 3600)]
    [int]$GroupTimeoutSeconds = 900
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
$toolsRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))
$shellPath = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)

function Get-VerificationFingerprint {
    $head = (& git -C $repositoryRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD for verification resume.' }
    $status = @(& git -C $repositoryRoot status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve working-tree state for verification resume.' }
    $material = [Text.StringBuilder]::new()
    $null = $material.AppendLine($head)
    foreach ($line in $status) {
        $null = $material.AppendLine([string]$line)
        $path = ([string]$line).Substring(3).Trim('"').Replace('/', [IO.Path]::DirectorySeparatorChar)
        if ($path -match ' -> ') { $path = ($path -split ' -> ')[-1] }
        $absolutePath = Join-Path $repositoryRoot $path
        if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
            $null = $material.AppendLine((Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256).Hash)
        }
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes($material.ToString())
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}

$verificationFingerprint = if ($ResumePassedStages) { Get-VerificationFingerprint } else { $null }
$receiptRoot = if ($ResumePassedStages) {
    $gitDirectory = (& git -C $repositoryRoot rev-parse --absolute-git-dir).Trim()
    Join-Path $gitDirectory "llm-wiki/verification-stages/$verificationFingerprint"
} else { $null }
if ($receiptRoot) { $null = New-Item -ItemType Directory -Path $receiptRoot -Force }

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
        name = 'session resolution'
        script = Join-Path $toolsRoot 'Test-LlmWikiSessionResolution.ps1'
        arguments = ''
    }
    [pscustomobject]@{
        name = 'process tree cleanup'
        script = Join-Path $toolsRoot 'Test-LlmWikiProcess.ps1'
        arguments = ''
    }
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
        name = 'query cache'
        script = Join-Path $toolsRoot 'Test-LlmWikiQueryCache.ps1'
        arguments = ''
    }
    [pscustomobject]@{
        name = 'governed delivery'
        script = Join-Path $toolsRoot 'Test-LlmWikiGovernedDeliveryRegression.ps1'
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
    $receiptPath = if ($receiptRoot) { Join-Path $receiptRoot (($check.name -replace '[^a-zA-Z0-9_.-]', '-') + '.passed') } else { $null }
    if ($receiptPath -and (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        Write-Host "Resuming LLM Wiki full verification: $($check.name) already passed for unchanged inputs."
        continue
    }
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
            if ($groupStopwatch.Elapsed.TotalSeconds -ge $GroupTimeoutSeconds) {
                Stop-LlmWikiProcessTree -Process $process
                throw "LLM Wiki full verification group timed out: $($check.name) after ${GroupTimeoutSeconds}s. Run separately: pwsh -NoProfile -File `"$($check.script)`" $($check.arguments)"
            }
        }
        if ($process.ExitCode -ne 0) {
            throw "LLM Wiki full verification failed: $($check.name) (exit=$($process.ExitCode))"
        }
    } finally {
        $process.Dispose()
    }

    $groupStopwatch.Stop()
    if ($receiptPath) {
        [IO.File]::WriteAllText($receiptPath, ([DateTime]::UtcNow.ToString('o') + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    }
    Write-Host "LLM Wiki full verification group passed: $($check.name) ($([Math]::Round($groupStopwatch.Elapsed.TotalSeconds, 2))s)"
}

Write-Host 'LLM Wiki full verification passed.'
