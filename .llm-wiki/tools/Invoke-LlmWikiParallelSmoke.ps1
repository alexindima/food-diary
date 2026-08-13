[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath,
    [string[]]$RequestedGroup,
    [switch]$AllGroups,
    [switch]$NoCache,
    [ValidateRange(1, 8)]
    [int]$MaxConcurrency = 4,
    [switch]$Plan,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$planner = Join-Path $PSScriptRoot 'Invoke-LlmWikiAffectedSmoke.ps1'
$allFocusedGroups = @(
    'adaptive-evals', 'change-policy', 'dependency-analysis',
    'facade-contract', 'read-only-guard', 'trace-output', 'task-baseline', 'git-paths', 'task-scope',
    'index-selection', 'ui-continuation', 'research-confidence', 'implementation-plan',
    'reporting', 'verification-cache', 'verification-receipts', 'query-cache', 'contract-consumers', 'extraction-readiness',
    'knowledge-isolation', 'memory', 'context-bundle', 'context-feedback',
    'strict-shapes', 'test-only-governance', 'governed-delivery', 'code-graph'
)

if ($AllGroups) {
    $groups = $allFocusedGroups
} else {
    $planArguments = @{ BaseRef = $BaseRef; Plan = $true; Format = 'Json' }
    if ($PSBoundParameters.ContainsKey('ChangedPath')) { $planArguments.ChangedPath = $ChangedPath }
    if ($PSBoundParameters.ContainsKey('RequestedGroup')) { $planArguments.RequestedGroup = $RequestedGroup }
    $smokePlan = & $planner @planArguments | ConvertFrom-Json
    $groups = @($smokePlan.groups)
}
$groups = @($groups | Where-Object { $_ -and $_ -ne 'full-tools' } | Sort-Object -Unique)
$durationPriority = @{
    'adaptive-evals' = 100
    'governed-delivery' = 90
    'research-confidence' = 80
    'task-scope' = 75
    'test-only-governance' = 70
    'trace-output' = 65
    'ui-continuation' = 60
    'contract-consumers' = 55
    'extraction-readiness' = 85
    'context-bundle' = 50
    'implementation-plan' = 45
}
function Get-SmokePriority([string]$Group) {
    if ($durationPriority.ContainsKey($Group)) { return [int]$durationPriority[$Group] }
    0
}
$parallelSafeGroups = @(
    'adaptive-routing', 'adaptive-experience', 'adaptive-evals', 'change-policy', 'dependency-analysis',
    'facade-contract', 'trace-output', 'task-baseline', 'git-paths', 'index-selection',
    'ui-continuation', 'research-confidence', 'implementation-plan', 'reporting',
    'verification-cache', 'verification-receipts', 'query-cache', 'contract-consumers', 'extraction-readiness', 'knowledge-isolation',
    'memory', 'context-bundle', 'context-feedback', 'strict-shapes', 'test-only-governance',
    'governed-delivery', 'task-scope', 'tool-contract', 'code-graph'
)
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

$runRoot = Join-Path $repositoryRoot ".artifacts/llm-wiki/parallel-smoke/$PID"
$null = New-Item -ItemType Directory -Path $runRoot -Force
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
                foreach ($active in @($running.ToArray())) {
                    try { Stop-LlmWikiProcessTree -Process $active.Process } catch { }
                    $active.Process.Dispose()
                }
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
}
$stopwatch.Stop()
Write-Host "Parallel affected smoke completed: $($groups.Count) group(s) in $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 2))s (max concurrency $MaxConcurrency)."
