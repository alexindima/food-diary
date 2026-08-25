[CmdletBinding()]
param(
    [ValidateRange(1, 8)]
    [int]$IndexConcurrency = 4,
    [switch]$FullTools,
    [switch]$CoreTools,
    [switch]$ResumePassedStages,
    [ValidateRange(30, 3600)]
    # The explicit legacy Full audit includes extended orchestration and has a
    # measured Windows runtime slightly above 900 seconds. Keep the daily
    # focused gate unchanged while allowing the opt-in exhaustive profile to
    # finish and report a real assertion result.
    [int]$GroupTimeoutSeconds = 1500
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$toolsRoot = [System.IO.Path]::GetFullPath($PSScriptRoot)
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot '../..'))
$shellPath = [System.IO.Path]::GetFullPath((Get-Process -Id $PID).Path)

function Get-VerificationFingerprint {
    $head = (Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', 'HEAD') -FailureMessage 'Unable to resolve HEAD for verification resume.').Lines[0].Trim()
    $status = @((Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('status', '--porcelain=v1', '--untracked-files=all') -FailureMessage 'Unable to resolve working-tree state for verification resume.').Lines)
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
    $gitDirectory = (Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -FailureMessage 'Unable to resolve the Git directory for verification resume.').Lines[0].Trim()
    Join-Path $gitDirectory "llm-wiki/verification-stages/$verificationFingerprint"
} else { $null }
if ($receiptRoot) { $null = New-Item -ItemType Directory -Path $receiptRoot -Force }

if ($FullTools -and $CoreTools) { throw 'FullTools and CoreTools cannot be used together.' }
$toolsProfile = if ($FullTools) { 'Full' } elseif ($CoreTools) { 'Core' } else { 'Focused' }
$verificationRunId = "full-$PID-$([guid]::NewGuid().ToString('N'))"
Write-Host "LLM Wiki tool verification profile: $toolsProfile. Monolithic Core/Full profiles are explicit audit-only modes; focused regressions are the default full gate."

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
        name = 'indexes'
        script = Join-Path $toolsRoot 'Invoke-LlmWikiIndexPipeline.ps1'
        arguments = "-Check -MaxConcurrency $IndexConcurrency"
    }
    [pscustomobject]@{
        name = 'focused tool regressions'
        script = Join-Path $toolsRoot 'Invoke-LlmWikiParallelSmoke.ps1'
        arguments = "-AllGroups -MaxConcurrency $IndexConcurrency"
    }
)
if ($FullTools -or $CoreTools) {
    $checks += [pscustomobject]@{
        name = "monolithic tools audit ($toolsProfile)"
        script = Join-Path $toolsRoot 'Test-LlmWikiTools.ps1'
        arguments = "-Profile $toolsProfile"
    }
}

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

    $groupOutcome = 'passed'
    $failureCategory = $null
    try {
        $nextProgressAt = 30
        while (-not $process.WaitForExit(1000)) {
            if ($groupStopwatch.Elapsed.TotalSeconds -ge $nextProgressAt) {
                Write-Host "LLM Wiki full verification group still running: $($check.name) ($([Math]::Round($groupStopwatch.Elapsed.TotalSeconds))s)"
                $nextProgressAt += 30
            }
            if ($groupStopwatch.Elapsed.TotalSeconds -ge $GroupTimeoutSeconds) {
                Stop-LlmWikiProcessTree -Process $process
                $groupOutcome = 'timed-out'
                $failureCategory = 'group-timeout'
                throw "LLM Wiki full verification group timed out: $($check.name) after ${GroupTimeoutSeconds}s. Run separately: pwsh -NoProfile -File `"$($check.script)`" $($check.arguments)"
            }
        }
        if ($process.ExitCode -ne 0) {
            $groupOutcome = 'failed'
            $failureCategory = 'nonzero-exit'
            throw "LLM Wiki full verification failed: $($check.name) (exit=$($process.ExitCode))"
        }
    } catch {
        if ($groupOutcome -eq 'passed') {
            $groupOutcome = 'failed'
            $failureCategory = 'runner-error'
        }
        $groupStopwatch.Stop()
        & (Join-Path $toolsRoot 'Write-LlmWikiWorkflowMetric.ps1') `
            -Operation 'verify-full-group' `
            -Outcome $groupOutcome `
            -DurationSeconds $groupStopwatch.Elapsed.TotalSeconds `
            -Phase $check.name `
            -Profile $toolsProfile `
            -RunId $verificationRunId `
            -FailureCategory $failureCategory
        throw
    } finally {
        $process.Dispose()
    }

    $groupStopwatch.Stop()
    & (Join-Path $toolsRoot 'Write-LlmWikiWorkflowMetric.ps1') `
        -Operation 'verify-full-group' `
        -Outcome passed `
        -DurationSeconds $groupStopwatch.Elapsed.TotalSeconds `
        -Phase $check.name `
        -Profile $toolsProfile `
        -RunId $verificationRunId
    if ($receiptPath) {
        [IO.File]::WriteAllText($receiptPath, ([DateTime]::UtcNow.ToString('o') + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    }
    Write-Host "LLM Wiki full verification group passed: $($check.name) ($([Math]::Round($groupStopwatch.Elapsed.TotalSeconds, 2))s)"
}

Write-Host 'LLM Wiki full verification passed.'
