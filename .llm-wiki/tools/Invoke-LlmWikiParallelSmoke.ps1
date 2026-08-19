[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath,
    [string[]]$RequestedGroup,
    [switch]$AllGroups,
    [switch]$NoCache,
    [ValidateRange(1, 8)]
    [int]$MaxConcurrency = 4,
    [ValidateRange(30, 3600)]
    [int]$CodeGraphTimeoutSeconds = 600,
    [ValidateRange(5, 120)]
    [int]$HeartbeatSeconds = 15,
    [ValidateRange(1, 30)]
    [int]$CancellationGraceSeconds = 5,
    [switch]$Plan,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$planner = Join-Path $PSScriptRoot 'Invoke-LlmWikiAffectedSmoke.ps1'
$smokeCatalog = Import-PowerShellDataFile -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/policies/affected-smoke-catalog.psd1')
$catalogGroups = @($smokeCatalog.Groups)
$allFocusedGroups = @($catalogGroups | Where-Object { [bool]$_.IncludeInAll } | ForEach-Object { [string]$_.Id })

if ($AllGroups) {
    $groups = $allFocusedGroups
} else {
    $planArguments = @{ BaseRef = $BaseRef; Plan = $true; Format = 'Json' }
    if ($PSBoundParameters.ContainsKey('ChangedPath')) { $planArguments.ChangedPath = $ChangedPath }
    elseif ($PSBoundParameters.ContainsKey('RequestedGroup')) { $planArguments.ChangedPath = @() }
    if ($PSBoundParameters.ContainsKey('RequestedGroup')) { $planArguments.RequestedGroup = $RequestedGroup }
    $smokePlan = & $planner @planArguments | ConvertFrom-Json
    $groups = @($smokePlan.groups)
}
$groups = @($groups | Where-Object { $_ -and $_ -ne 'full-tools' } | Sort-Object -Unique)
$durationPriority = @{}
foreach ($catalogGroup in $catalogGroups) { $durationPriority[[string]$catalogGroup.Id] = [int]$catalogGroup.Priority }
function Get-SmokePriority([string]$Group) {
    if ($durationPriority.ContainsKey($Group)) { return [int]$durationPriority[$Group] }
    0
}
$parallelSafeGroups = @($catalogGroups | Where-Object { [bool]$_.ParallelSafe } | ForEach-Object { [string]$_.Id })
$parallelGroups = @(
    $groups |
        Where-Object { $_ -in $parallelSafeGroups } |
        Sort-Object @{ Expression = { Get-SmokePriority $_ }; Descending = $true }, @{ Expression = { $_ } }
)
$serialGroups = @(
    $groups |
        Where-Object { $_ -notin $parallelSafeGroups } |
        Sort-Object @{ Expression = { Get-SmokePriority $_ }; Descending = $true }, @{ Expression = { $_ } }
)
$planResult = [pscustomobject][ordered]@{
    groupCount = $groups.Count
    maxConcurrency = $MaxConcurrency
    parallelGroups = $parallelGroups
    serialGroups = $serialGroups
    groups = $groups
}
if ($Plan) {
    if ($Format -eq 'Json') { $planResult | ConvertTo-Json -Depth 4 } else { $planResult | Format-List | Out-String | Write-Host }
    exit 0
}
if ($groups.Count -eq 0) {
    Write-Host 'Parallel affected smoke: no focused groups selected.'
    exit 0
}

$aggregateReceiptPath = $null
$aggregateFingerprint = $null
if (-not $NoCache) {
    $groupMaterial = @($groups | Sort-Object) -join '|'
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $groupKey = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($groupMaterial))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
    $gitDirectory = (& git -C $repositoryRoot rev-parse --absolute-git-dir).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the Git directory for the parallel smoke receipt.' }
    $aggregateReceiptRoot = Join-Path $gitDirectory 'llm-wiki/parallel-smoke'
    $aggregateReceiptPath = Join-Path $aggregateReceiptRoot "$groupKey.json"
    $aggregateFingerprint = & (Join-Path $PSScriptRoot 'Get-LlmWikiVerificationStageFingerprint.ps1') `
        -Stage 'affected smoke' `
        -Arguments @{ groups = @($groups | Sort-Object) } `
        -Format Text
    if (Test-Path -LiteralPath $aggregateReceiptPath -PathType Leaf) {
        try {
            $aggregateReceipt = Get-Content -LiteralPath $aggregateReceiptPath -Raw | ConvertFrom-Json
            if ([string]$aggregateReceipt.fingerprint -ceq [string]$aggregateFingerprint) {
                Write-Host "Parallel affected smoke aggregate cache hit: $($groups.Count) group(s), previous duration $($aggregateReceipt.durationSeconds)s."
                exit 0
            }
        } catch { }
    }
}

$graphDependentGroups = @($catalogGroups | Where-Object { [bool]$_.GraphDependent } | ForEach-Object { [string]$_.Id })
$runId = "$PID-$([guid]::NewGuid().ToString('N'))"
$runRoot = Join-Path $repositoryRoot ".artifacts/llm-wiki/parallel-smoke/$runId"
$null = New-Item -ItemType Directory -Path $runRoot -Force
$cancelPath = Join-Path $runRoot 'cancel.requested.json'
$worktreeBaseline = @(& git -C $repositoryRoot status --porcelain=v1 --untracked-files=all)
if ($LASTEXITCODE -ne 0) { throw 'Unable to capture the pre-smoke worktree state.' }

function Request-SmokeCancellation([object[]]$Items, [string]$Reason) {
    [IO.File]::WriteAllText($cancelPath, (([ordered]@{
        reason = $Reason
        requestedAtUtc = [DateTime]::UtcNow.ToString('o')
    } | ConvertTo-Json -Compress) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    foreach ($item in @($Items)) {
        if ($null -eq $item.Process -or $item.Process.HasExited) { continue }
        try { $null = $item.Process.CloseMainWindow() } catch { }
    }
    $deadline = [DateTime]::UtcNow.AddSeconds($CancellationGraceSeconds)
    do {
        if (@($Items | Where-Object { $null -ne $_.Process -and -not $_.Process.HasExited }).Count -eq 0) { return }
        Start-Sleep -Milliseconds 200
    } while ([DateTime]::UtcNow -lt $deadline)
    foreach ($item in @($Items)) {
        if ($null -eq $item.Process -or $item.Process.HasExited) { continue }
        try { Stop-LlmWikiProcessTree -Process $item.Process } catch { }
    }
}

if (@($groups | Where-Object { $_ -in $graphDependentGroups }).Count -gt 0) {
    $graphManager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
    Write-Host 'Parallel affected smoke analyzing code graph cache state...'
    $graphPlan = & $graphManager -Action build-plan -Format Json | ConvertFrom-Json
    $graphLogPath = Join-Path $runRoot 'code-graph-prewarm.log'
    [IO.File]::WriteAllText($graphLogPath, '', [Text.UTF8Encoding]::new($false))
    Write-Host "Parallel affected smoke prewarming code graph: reason=$($graphPlan.reason); estimated=$($graphPlan.estimatedSeconds)s; timeout=${CodeGraphTimeoutSeconds}s; log=$graphLogPath"
    $graphStartInfo = [Diagnostics.ProcessStartInfo]::new()
    $graphStartInfo.FileName = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
    $graphStartInfo.WorkingDirectory = $repositoryRoot
    $graphStartInfo.UseShellExecute = $false
    $graphStartInfo.RedirectStandardOutput = $true
    $graphStartInfo.RedirectStandardError = $true
    $graphStartInfo.Environment['LLM_WIKI_SMOKE_SANDBOX'] = (Join-Path $runRoot 'sandbox/code-graph-prewarm')
    $graphStartInfo.Environment['LLM_WIKI_SMOKE_CANCEL_PATH'] = $cancelPath
    $graphStartInfo.Arguments = "-NoLogo -NoProfile -File `"$graphManager`" -Action build -Format Json"
    $graphProcess = [Diagnostics.Process]::new()
    $graphProcess.StartInfo = $graphStartInfo
    if (-not $graphProcess.Start()) { throw 'Unable to start code graph prewarm.' }
    $graphOutputTask = $graphProcess.StandardOutput.ReadToEndAsync()
    $graphErrorTask = $graphProcess.StandardError.ReadToEndAsync()
    $graphStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $nextHeartbeat = $HeartbeatSeconds
    while (-not $graphProcess.WaitForExit(500)) {
        if ($graphStopwatch.Elapsed.TotalSeconds -ge $nextHeartbeat) {
            Write-Host "Code graph prewarm still running: $([Math]::Round($graphStopwatch.Elapsed.TotalSeconds))s elapsed; estimate=$($graphPlan.estimatedSeconds)s; log=$graphLogPath"
            $nextHeartbeat += $HeartbeatSeconds
        }
        if ($graphStopwatch.Elapsed.TotalSeconds -ge $CodeGraphTimeoutSeconds) {
            Request-SmokeCancellation @([pscustomobject]@{ Process = $graphProcess }) 'code-graph-prewarm-timeout'
            throw "Code graph prewarm timed out after ${CodeGraphTimeoutSeconds}s. Diagnostic log: $graphLogPath"
        }
    }
    $graphOutput = [string]$graphOutputTask.GetAwaiter().GetResult()
    $graphError = [string]$graphErrorTask.GetAwaiter().GetResult()
    [IO.File]::WriteAllText($graphLogPath, ($graphOutput + $graphError), [Text.UTF8Encoding]::new($false))
    $graphExitCode = $graphProcess.ExitCode
    $graphProcess.Dispose()
    if ($graphExitCode -ne 0) { throw "Code graph prewarm failed with exit code $graphExitCode. Diagnostic log: $graphLogPath" }
    Write-Host "Code graph prewarm completed in $([Math]::Round($graphStopwatch.Elapsed.TotalSeconds, 2))s."
}

$wrapper = Join-Path $PSScriptRoot 'Invoke-LlmWikiObservedStage.ps1'
$shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)

function Start-SmokeGroup([string]$Group) {
    $arguments = [ordered]@{
        BaseRef = $BaseRef
        ChangedPath = @($ChangedPath | Where-Object { $_ })
        RequestedGroup = @($Group)
        NoCache = [bool]$NoCache
    }
    $argumentsPath = Join-Path $runRoot "$Group.arguments.json"
    $logPath = Join-Path $runRoot "$Group.log"
    [IO.File]::WriteAllText($argumentsPath, (($arguments | ConvertTo-Json -Depth 4) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $shellPath
    $startInfo.WorkingDirectory = $repositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Arguments = "-NoLogo -NoProfile -File `"$wrapper`" -ToolPath `"$planner`" -ArgumentsPath `"$argumentsPath`" -StageName `"affected smoke: $Group`" -LogPath `"$logPath`""
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $sandboxPath = Join-Path $runRoot "sandbox/$Group"
    $null = New-Item -ItemType Directory -Path $sandboxPath -Force
    $process.StartInfo.Environment['LLM_WIKI_SMOKE_SANDBOX'] = $sandboxPath
    $process.StartInfo.Environment['LLM_WIKI_SMOKE_TASK_PREFIX'] = "$runId-$Group"
    $process.StartInfo.Environment['LLM_WIKI_SMOKE_CANCEL_PATH'] = $cancelPath
    $process.StartInfo.Environment['TEMP'] = $sandboxPath
    $process.StartInfo.Environment['TMP'] = $sandboxPath
    if (-not $process.Start()) { throw "Unable to start focused smoke group '$Group'." }
    [pscustomobject]@{
        Group = $Group
        Process = $process
        ArgumentsPath = $argumentsPath
        LogPath = $logPath
        StartedAt = [DateTime]::UtcNow
        StandardOutput = $process.StandardOutput.ReadToEndAsync()
        StandardError = $process.StandardError.ReadToEndAsync()
    }
}

function Wait-SmokeBatch([string[]]$BatchGroups, [int]$Concurrency) {
    $pending = [Collections.Generic.Queue[string]]::new()
    foreach ($group in $BatchGroups) { $pending.Enqueue($group) }
    $running = [Collections.Generic.List[object]]::new()
    while ($pending.Count -gt 0 -or $running.Count -gt 0) {
        while ($pending.Count -gt 0 -and $running.Count -lt $Concurrency) {
            $group = $pending.Dequeue()
            Write-Host "Parallel affected smoke starting: $group"
            $running.Add((Start-SmokeGroup $group))
        }
        foreach ($item in @($running.ToArray())) {
            if (-not $item.Process.HasExited) { continue }
            $exitCode = $item.Process.ExitCode
            $standardOutput = [string]$item.StandardOutput.GetAwaiter().GetResult()
            $standardError = [string]$item.StandardError.GetAwaiter().GetResult()
            $duration = [Math]::Round(([DateTime]::UtcNow - $item.StartedAt).TotalSeconds, 2)
            $item.Process.Dispose()
            $running.Remove($item) | Out-Null
            Remove-Item -LiteralPath $item.ArgumentsPath -Force -ErrorAction SilentlyContinue
            if ($exitCode -ne 0) {
                Request-SmokeCancellation @($running.ToArray()) "group-failed:$($item.Group)"
                foreach ($active in @($running.ToArray())) { $active.Process.Dispose() }
                $tail = @(($standardOutput + [Environment]::NewLine + $standardError) -split '\r?\n' | Where-Object { $_ } | Select-Object -Last 12)
                throw "Focused smoke group '$($item.Group)' failed after ${duration}s. Log: $($item.LogPath)`n$($tail -join [Environment]::NewLine)"
            }
            Write-Host "Parallel affected smoke passed: $($item.Group) (${duration}s)"
        }
        if ($running.Count -gt 0) { Start-Sleep -Milliseconds 200 }
    }
}

$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$completed = $false
try {
    Wait-SmokeBatch $parallelGroups $MaxConcurrency
    foreach ($group in $serialGroups) { Wait-SmokeBatch @($group) 1 }
    $completed = $true
} finally {
    if ($completed) {
        Remove-Item -LiteralPath $runRoot -Recurse -Force -ErrorAction SilentlyContinue
    } else {
        Write-Warning "Focused smoke logs preserved after failure: $runRoot"
    }
    $taskSandboxRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks'))
    if (Test-Path -LiteralPath $taskSandboxRoot -PathType Container) {
        foreach ($taskSandbox in Get-ChildItem -LiteralPath $taskSandboxRoot -Directory -Filter ".smoke-$runId-*" -ErrorAction SilentlyContinue) {
            $resolvedTaskSandbox = [IO.Path]::GetFullPath($taskSandbox.FullName)
            if (-not $resolvedTaskSandbox.StartsWith(($taskSandboxRoot.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)) {
                throw "Refusing to clean smoke task sandbox outside task root: $resolvedTaskSandbox"
            }
            Remove-Item -LiteralPath $resolvedTaskSandbox -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    $worktreeAfter = @(& git -C $repositoryRoot status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to capture the post-smoke worktree state.' }
    if (($worktreeBaseline -join "`n") -cne ($worktreeAfter -join "`n")) {
        $addedState = @($worktreeAfter | Where-Object { $_ -notin $worktreeBaseline })
        Write-Warning "The worktree changed concurrently outside owned smoke sandboxes; those paths are not attributed to this run: $($addedState -join '; '). Sandbox/logs: $runRoot"
    }
}
$stopwatch.Stop()
if ($aggregateReceiptPath) {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $aggregateReceiptPath) -Force
    $aggregateReceipt = [ordered]@{
        schemaVersion = 1
        fingerprint = [string]$aggregateFingerprint
        groups = @($groups | Sort-Object)
        durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
        recordedAtUtc = [DateTime]::UtcNow.ToString('o')
    }
    $temporaryReceiptPath = "$aggregateReceiptPath.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temporaryReceiptPath, (($aggregateReceipt | ConvertTo-Json -Depth 5) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryReceiptPath -Destination $aggregateReceiptPath -Force
    } finally {
        Remove-Item -LiteralPath $temporaryReceiptPath -Force -ErrorAction SilentlyContinue
    }
}
Write-Host "Parallel affected smoke completed: $($groups.Count) group(s) in $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 2))s (max concurrency $MaxConcurrency)."
