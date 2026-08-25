[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('list', 'run', 'verify', 'prune')]
    [string]$Action = 'list',
    [string]$CycleId,
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [Nullable[int]]$MaxConcurrency,
    [Nullable[int]]$TtlMinutes,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$Apply,
    [switch]$FailOnAttention,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$schedulerRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler'
$cycleRoot = Join-Path $schedulerRoot 'cycles'
$lockPath = Join-Path $schedulerRoot '.orchestration-cycle-lock'
$now = $AsOfUtc.ToUniversalTime()
$policySnapshot = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json | ConvertFrom-Json
$policy = $policySnapshot.policy
$policyFingerprint = [string]$policySnapshot.fingerprint

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-CyclePayload([object]$Cycle) {
    [ordered]@{
        schemaVersion = $Cycle.schemaVersion
        cycleId = $Cycle.cycleId
        startedAtUtc = $Cycle.startedAtUtc
        completedAtUtc = $Cycle.completedAtUtc
        apply = $Cycle.apply
        tasksPath = $Cycle.tasksPath
        maxConcurrency = $Cycle.maxConcurrency
        policyFingerprint = $Cycle.policyFingerprint
        state = $Cycle.state
        preflight = $Cycle.preflight
        watchdog = $Cycle.watchdog
        reconciliation = $Cycle.reconciliation
        plan = $Cycle.plan
        claim = $Cycle.claim
        postflight = $Cycle.postflight
        issues = $Cycle.issues
    }
}
function Test-Cycle([object]$Cycle) {
    $issues = [System.Collections.Generic.List[string]]::new()
    if ($Cycle.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Cycle.cycleId -notmatch '^[a-f0-9]{32}$') { $issues.Add('cycleId is invalid.') }
    if ([string]$Cycle.cycleHash -cne (Get-Hash (Get-CyclePayload $Cycle))) { $issues.Add('cycleHash is invalid.') }
    if ([bool]$Cycle.apply -and [string]$Cycle.state -eq 'preview') { $issues.Add('An applied cycle cannot be in preview state.') }
    if (-not [bool]$Cycle.apply -and [int]$Cycle.claim.dispatchCount -gt 0) { $issues.Add('A preview cycle cannot create dispatches.') }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}
function Get-CycleFiles {
    if (-not (Test-Path -LiteralPath $cycleRoot -PathType Container)) { return @() }
    return @(Get-ChildItem -LiteralPath $cycleRoot -File -Filter '*.json' | Sort-Object Name)
}
function Read-Cycle([string]$Id) {
    if ($Id -notmatch '^[a-f0-9]{32}$') { throw 'CycleId must be a 32-character lowercase hexadecimal identifier.' }
    $matches = @(Get-CycleFiles | Where-Object BaseName -like "*-$Id")
    if ($matches.Count -ne 1) { throw "Orchestration cycle does not exist or is ambiguous: $Id" }
    return Get-Content -LiteralPath $matches[0].FullName -Raw | ConvertFrom-Json
}
function Write-Cycle([object]$Cycle) {
    if (-not (Test-Path -LiteralPath $cycleRoot)) { New-Item -ItemType Directory -Path $cycleRoot | Out-Null }
    $fileName = "$($now.ToString('yyyyMMddTHHmmssfffZ'))-$($Cycle.cycleId).json"
    $temporaryPath = Join-Path $cycleRoot ('.cycle-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [System.IO.File]::WriteAllText($temporaryPath, (($Cycle | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination (Join-Path $cycleRoot $fileName)
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [System.IO.File]::Delete($temporaryPath) }
    }
    return ".artifacts/llm-wiki/scheduler/cycles/$fileName"
}

$mutating = $Action -in @('run', 'prune')
$lockStream = $null
if ($mutating) {
    if (-not (Test-Path -LiteralPath $schedulerRoot)) { New-Item -ItemType Directory -Path $schedulerRoot | Out-Null }
    if (Test-Path -LiteralPath $lockPath -PathType Leaf) {
        if (([DateTime]::UtcNow - [System.IO.File]::GetLastWriteTimeUtc($lockPath)).TotalMinutes -gt 10) { [System.IO.File]::Delete($lockPath) }
    }
    try {
        $lockStream = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    } catch {
        throw 'Orchestration cycle is already running; retry after it completes.'
    }
}

try {
    if ($Action -eq 'run') {
        $cycleIdValue = [guid]::NewGuid().ToString('N')
        $issues = [System.Collections.Generic.List[string]]::new()
        $preAudit = & (Join-Path $PSScriptRoot 'Test-LlmWikiOrchestrationLineage.ps1') -AsOfUtc $now -Format Json | ConvertFrom-Json
        $coverage = & (Join-Path $PSScriptRoot 'Get-LlmWikiAgentFleetCoverage.ps1') -TasksPath $TasksPath -Format Json | ConvertFrom-Json
        if (-not $preAudit.valid) { $issues.Add("Preflight lineage audit has $($preAudit.summary.issueCount) issue(s).") }
        if (-not $coverage.valid) { $issues.Add("Agent fleet has $($coverage.taskGapCount) task gap(s).") }

        $watchdogArguments = @{ Action = 'run'; AsOfUtc = $now; Format = 'Json' }
        if ($Apply -and $issues.Count -eq 0) { $watchdogArguments.Apply = $true }
        $watchdog = & (Join-Path $PSScriptRoot 'Manage-LlmWikiDispatchWatchdog.ps1') @watchdogArguments | ConvertFrom-Json
        $reconciliation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') reconcile -AsOfUtc $now -Format Json | ConvertFrom-Json
        if ($Apply -and $issues.Count -eq 0 -and $reconciliation.candidateCount -gt 0) {
            $reconciliation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') reconcile -Apply -AsOfUtc $now -Format Json | ConvertFrom-Json
        }

        $planResult = $null
        $claimResult = $null
        $state = 'blocked'
        if ($issues.Count -eq 0) {
            $planArguments = @{ Action = 'create'; TasksPath = $TasksPath; AsOfUtc = $now; Format = 'Json' }
            if ($null -ne $MaxConcurrency) { $planArguments.MaxConcurrency = $MaxConcurrency }
            if ($null -ne $TtlMinutes) { $planArguments.TtlMinutes = $TtlMinutes }
            $planResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiSchedulePlan.ps1') @planArguments | ConvertFrom-Json
            $claimArguments = @{ Action = 'claim'; PlanId = [string]$planResult.plan.planId; AsOfUtc = $now; Format = 'Json' }
            if ($Apply -and @($planResult.plan.assignments).Count -gt 0) { $claimArguments.Apply = $true }
            $claimResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiSchedulePlan.ps1') @claimArguments | ConvertFrom-Json
            if (@($planResult.plan.assignments).Count -eq 0) {
                $state = 'idle'
            } elseif (-not $Apply) {
                $state = 'preview'
            } elseif ($claimResult.state -eq 'claimed') {
                $state = 'dispatched'
            } else {
                $state = [string]$claimResult.state
                $issues.Add("Schedule claim ended in state '$($claimResult.state)'.")
            }
        }
        $postAudit = & (Join-Path $PSScriptRoot 'Test-LlmWikiOrchestrationLineage.ps1') -AsOfUtc $now -Format Json | ConvertFrom-Json
        if (-not $postAudit.valid) { $issues.Add("Postflight lineage audit has $($postAudit.summary.issueCount) issue(s).") }
        $cycleDispatchIds = @(
            if ($null -ne $claimResult) {
                $claimResult.dispatches |
                    ForEach-Object { [string]$_.dispatchId } |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
            }
        )
        $cycle = [pscustomobject][ordered]@{
            schemaVersion = 1
            cycleId = $cycleIdValue
            startedAtUtc = $now.ToString('o')
            completedAtUtc = [DateTime]::UtcNow.ToString('o')
            apply = [bool]$Apply
            tasksPath = $TasksPath
            maxConcurrency = $(if ($null -ne $planResult) { $planResult.plan.maxConcurrency } else { $MaxConcurrency })
            policyFingerprint = $policyFingerprint
            state = $state
            preflight = [pscustomobject][ordered]@{
                lineageValid = [bool]$preAudit.valid
                lineageHash = Get-Hash $preAudit
                coverageValid = [bool]$coverage.valid
                taskGapCount = [int]$coverage.taskGapCount
                routingMode = [string]$coverage.routingMode
            }
            watchdog = [pscustomobject][ordered]@{
                watchdogId = [string]$watchdog.receipt.watchdogId
                watchdogHash = [string]$watchdog.receipt.watchdogHash
                candidateCount = [int]$watchdog.receipt.summary.candidateCount
                changedDispatchCount = [int]$watchdog.receipt.summary.changedDispatchCount
                quarantinedAgentCount = [int]$watchdog.receipt.summary.quarantinedAgentCount
                openedCircuitCount = [int]$watchdog.receipt.summary.openedCircuitCount
            }
            reconciliation = [pscustomobject][ordered]@{
                candidateCount = [int]$reconciliation.candidateCount
                changedCount = [int]$reconciliation.changedCount
            }
            plan = [pscustomobject][ordered]@{
                planId = $(if ($null -ne $planResult) { [string]$planResult.plan.planId } else { '' })
                planHash = $(if ($null -ne $planResult) { [string]$planResult.plan.planHash } else { '' })
                assignmentCount = $(if ($null -ne $planResult) { @($planResult.plan.assignments).Count } else { 0 })
            }
            claim = [pscustomobject][ordered]@{
                claimId = $(if ($null -ne $claimResult) { [string]$claimResult.claim.claimId } else { '' })
                claimHash = $(if ($null -ne $claimResult) { [string]$claimResult.claim.claimHash } else { '' })
                state = $(if ($null -ne $claimResult) { [string]$claimResult.state } else { '' })
                dispatchCount = @($cycleDispatchIds).Count
                dispatchIds = @($cycleDispatchIds)
            }
            postflight = [pscustomobject][ordered]@{
                lineageValid = [bool]$postAudit.valid
                lineageHash = Get-Hash $postAudit
                issueCount = [int]$postAudit.summary.issueCount
            }
            issues = @($issues)
            cycleHash = ''
        }
        $cycle.cycleHash = Get-Hash (Get-CyclePayload $cycle)
        $path = Write-Cycle $cycle
        $validation = Test-Cycle $cycle
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'run'; valid = $validation.valid -and $issues.Count -eq 0; cycle = $cycle; path = $path }
    } elseif ($Action -eq 'verify') {
        if ([string]::IsNullOrWhiteSpace($CycleId)) { throw 'verify requires CycleId.' }
        $cycle = Read-Cycle $CycleId
        $validation = Test-Cycle $cycle
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'verify'; valid = $validation.valid; validation = $validation; cycle = $cycle }
    } elseif ($Action -eq 'prune') {
        $retentionCount = [int]$policy.scheduler.orchestrationCycles.retentionCount
        $candidates = @(Get-CycleFiles | Sort-Object Name -Descending | Select-Object -Skip $retentionCount)
        if ($Apply) { foreach ($file in $candidates) { [System.IO.File]::Delete($file.FullName) } }
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'prune'; apply = [bool]$Apply; retentionCount = $retentionCount; candidateCount = $candidates.Count; changedCount = $(if ($Apply) { $candidates.Count } else { 0 }); candidates = @($candidates.BaseName) }
    } else {
        $cycles = @((Get-CycleFiles) | ForEach-Object {
            try {
                $cycle = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
                $validation = Test-Cycle $cycle
                [pscustomobject][ordered]@{ cycleId = [string]$cycle.cycleId; startedAtUtc = [string]$cycle.startedAtUtc; apply = [bool]$cycle.apply; state = [string]$cycle.state; assignmentCount = [int]$cycle.plan.assignmentCount; dispatchCount = [int]$cycle.claim.dispatchCount; valid = $validation.valid; issues = @($validation.issues) }
            } catch {
                [pscustomobject][ordered]@{ cycleId = $_.BaseName; valid = $false; issues = @($_.Exception.Message) }
            }
        })
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'list'; totalCount = $cycles.Count; invalidCount = @($cycles | Where-Object { -not $_.valid }).Count; cycles = $cycles }
    }
} finally {
    if ($null -ne $lockStream) { $lockStream.Dispose() }
    if ($mutating -and (Test-Path -LiteralPath $lockPath)) { [System.IO.File]::Delete($lockPath) }
}

if ($Format -eq 'Json') {
    $response | ConvertTo-Json -Depth 20
} else {
    if ($Action -eq 'list') { Write-Host "Orchestration cycles: total=$($response.totalCount), invalid=$($response.invalidCount)" }
    elseif ($Action -eq 'prune') { Write-Host "Orchestration cycle prune: candidates=$($response.candidateCount), changed=$($response.changedCount)" }
    else { Write-Host "Orchestration cycle: action=$Action, id=$($response.cycle.cycleId), state=$($response.cycle.state), valid=$($response.valid)" }
}
$invalid = if ($Action -eq 'list') { $response.invalidCount -gt 0 } elseif ($Action -eq 'prune') { $false } else { -not $response.valid }
if ($FailOnAttention -and $invalid) { exit 1 }
