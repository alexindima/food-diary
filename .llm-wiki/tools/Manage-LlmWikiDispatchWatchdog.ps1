[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('list', 'run', 'verify', 'prune')]
    [string]$Action = 'list',
    [string]$WatchdogId,
    [Nullable[int]]$SilentMinutes,
    [Nullable[int]]$QuarantineMinutes,
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
$dispatchRoot = Join-Path $schedulerRoot 'dispatches'
$watchdogRoot = Join-Path $schedulerRoot 'watchdog'
$lockPath = Join-Path $schedulerRoot '.watchdog-lock'
$policy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json
$watchdogPolicy = $policy.scheduler.watchdog
$now = $AsOfUtc.ToUniversalTime()
$effectiveSilentMinutes = if ($null -ne $SilentMinutes) { [int]$SilentMinutes } else { [int]$watchdogPolicy.silentDispatchMinutes }
$effectiveQuarantineMinutes = if ($null -ne $QuarantineMinutes) { [int]$QuarantineMinutes } else { [int]$watchdogPolicy.defaultQuarantineMinutes }
if ($effectiveSilentMinutes -lt 1 -or $effectiveSilentMinutes -gt [int]$policy.scheduler.maximumLeaseMinutes) { throw "SilentMinutes must be between 1 and $($policy.scheduler.maximumLeaseMinutes)." }
if ($effectiveQuarantineMinutes -lt 1 -or $effectiveQuarantineMinutes -gt [int]$watchdogPolicy.maximumQuarantineMinutes) { throw "QuarantineMinutes must be between 1 and $($watchdogPolicy.maximumQuarantineMinutes)." }

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-Payload([object]$Receipt) {
    [ordered]@{
        schemaVersion = $Receipt.schemaVersion
        watchdogId = $Receipt.watchdogId
        inspectedAtUtc = $Receipt.inspectedAtUtc
        apply = $Receipt.apply
        policyFingerprint = $Receipt.policyFingerprint
        thresholds = $Receipt.thresholds
        summary = $Receipt.summary
        candidates = $Receipt.candidates
        actions = $Receipt.actions
    }
}
function Test-Receipt([object]$Receipt) {
    $issues = [System.Collections.Generic.List[string]]::new()
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.watchdogId -notmatch '^[a-f0-9]{32}$') { $issues.Add('watchdogId is invalid.') }
    if ([string]$Receipt.watchdogHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('watchdogHash is invalid.') }
    if (-not [bool]$Receipt.apply -and [int]$Receipt.summary.changedDispatchCount -gt 0) { $issues.Add('Preview watchdog receipt reports changed dispatches.') }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}
function Get-Files {
    if (-not (Test-Path -LiteralPath $watchdogRoot -PathType Container)) { return @() }
    return @(Get-ChildItem -LiteralPath $watchdogRoot -File -Filter '*.json' | Sort-Object Name)
}
function Read-Receipt([string]$Id) {
    if ($Id -notmatch '^[a-f0-9]{32}$') { throw 'WatchdogId must be a 32-character lowercase hexadecimal identifier.' }
    $matches = @(Get-Files | Where-Object BaseName -like "*-$Id")
    if ($matches.Count -ne 1) { throw "Watchdog receipt does not exist or is ambiguous: $Id" }
    Get-Content -LiteralPath $matches[0].FullName -Raw | ConvertFrom-Json
}
function Write-Receipt([object]$Receipt) {
    if (-not (Test-Path -LiteralPath $watchdogRoot)) { New-Item -ItemType Directory -Path $watchdogRoot | Out-Null }
    $fileName = "$($now.ToString('yyyyMMddTHHmmssfffZ'))-$($Receipt.watchdogId).json"
    $temporaryPath = Join-Path $watchdogRoot ('.watchdog-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [System.IO.File]::WriteAllText($temporaryPath, (($Receipt | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination (Join-Path $watchdogRoot $fileName)
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [System.IO.File]::Delete($temporaryPath) }
    }
    ".artifacts/llm-wiki/scheduler/watchdog/$fileName"
}
function Convert-ToUtc([object]$Value) {
    [DateTime]::Parse([string]$Value).ToUniversalTime()
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
        throw 'Dispatch watchdog is already running; retry after it completes.'
    }
}

try {
    if ($Action -eq 'run') {
        $dispatchList = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
        $agentRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAgentRegistry.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
        $receipts = @{}
        foreach ($file in @(Get-ChildItem -LiteralPath $dispatchRoot -File -Filter '*.json' -ErrorAction SilentlyContinue)) {
            try {
                $receipt = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
                $receipts[[string]$receipt.dispatchId] = $receipt
            } catch {}
        }
        $windowStart = $now.AddMinutes(-[int]$watchdogPolicy.retryWindowMinutes)
        $failedReceipts = @($receipts.Values | Where-Object {
            $events = @($_.events)
            $terminal = $events | Where-Object type -eq 'failed' | Select-Object -Last 1
            $null -ne $terminal -and (Convert-ToUtc $terminal.atUtc) -ge $windowStart
        })
        $candidates = [System.Collections.Generic.List[object]]::new()
        foreach ($dispatch in @($dispatchList.dispatches | Where-Object state -eq 'running')) {
            $receipt = $receipts[[string]$dispatch.dispatchId]
            if ($null -eq $receipt) { continue }
            $lastEvent = @($receipt.events) | Select-Object -Last 1
            $silentForMinutes = [Math]::Round([Math]::Max(0, ($now - (Convert-ToUtc $lastEvent.atUtc)).TotalMinutes), 2)
            $workspaceFailures = @($failedReceipts | Where-Object workspace -eq $receipt.workspace).Count
            $ownerFailures = @($failedReceipts | Where-Object owner -eq $receipt.owner).Count
            $silent = $silentForMinutes -ge $effectiveSilentMinutes
            $projectedFailureCount = $workspaceFailures + $(if ($silent) { 1 } else { 0 })
            $retryRemaining = [Math]::Max(0, [int]$watchdogPolicy.maximumRetriesPerWorkspace - $projectedFailureCount)
            if (-not $silent -and $retryRemaining -gt 0) { continue }
            $candidates.Add([pscustomobject][ordered]@{
                dispatchId = [string]$receipt.dispatchId
                workspace = [string]$receipt.workspace
                owner = [string]$receipt.owner
                agentId = [string]$receipt.agentId
                lastEventAtUtc = [string]$lastEvent.atUtc
                silentForMinutes = $silentForMinutes
                silent = $silent
                workspaceFailureCount = $workspaceFailures
                projectedWorkspaceFailureCount = $projectedFailureCount
                ownerFailureCount = $ownerFailures
                retryRemaining = $retryRemaining
                retryExhausted = $retryRemaining -eq 0
                quarantineRecommended = ($ownerFailures + $(if ($silent) { 1 } else { 0 })) -ge [int]$watchdogPolicy.agentFailureThreshold
            })
        }

        $actions = [System.Collections.Generic.List[object]]::new()
        $changedDispatchCount = 0
        $quarantinedAgentIds = [System.Collections.Generic.HashSet[string]]::new()
        $openedCircuitIds = [System.Collections.Generic.HashSet[string]]::new()
        $watchdogIdValue = [guid]::NewGuid().ToString('N')
        foreach ($candidate in $candidates) {
            $dispatchChanged = $false
            if ($Apply -and $candidate.silent) {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') fail `
                    -DispatchId $candidate.dispatchId `
                    -Owner $candidate.owner `
                    -Result "Watchdog terminated silent dispatch after $($candidate.silentForMinutes) minute(s); retryRemaining=$($candidate.retryRemaining)." `
                    -AsOfUtc $now | Out-Null
                $dispatchChanged = $true
                $changedDispatchCount++
            }
            $agentQuarantined = $false
            if ($Apply -and $candidate.quarantineRecommended -and -not [string]::IsNullOrWhiteSpace($candidate.agentId)) {
                $agent = $agentRegistry.agents | Where-Object { $_.agentId -eq $candidate.agentId -and $_.registered } | Select-Object -First 1
                if ($null -ne $agent -and $quarantinedAgentIds.Add($candidate.agentId)) {
                    & (Join-Path $PSScriptRoot 'Manage-LlmWikiAgentRegistry.ps1') quarantine `
                        -AgentId $candidate.agentId `
                        -Owner $candidate.owner `
                        -Reason "Watchdog failure threshold reached for owner '$($candidate.owner)'." `
                        -QuarantineMinutes $effectiveQuarantineMinutes `
                        -AsOfUtc $now | Out-Null
                    $agentQuarantined = $true
                }
            }
            $circuitOpened = $false
            $circuitId = ''
            if ($Apply -and $candidate.retryExhausted) {
                $circuitResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiWorkspaceCircuit.ps1') open `
                    -WorkspacePath $candidate.workspace `
                    -SourceWatchdogId $watchdogIdValue `
                    -Reason "Retry budget exhausted after $($candidate.projectedWorkspaceFailureCount) failure(s) within $($watchdogPolicy.retryWindowMinutes) minute(s)." `
                    -AsOfUtc $now `
                    -Format Json | ConvertFrom-Json
                $circuitId = [string]$circuitResult.circuit.circuitId
                $circuitOpened = [bool]$circuitResult.changed
                if ($circuitOpened) { [void]$openedCircuitIds.Add($circuitId) }
            }
            $actions.Add([pscustomobject][ordered]@{
                dispatchId = $candidate.dispatchId
                dispatchFailed = $dispatchChanged
                agentId = $candidate.agentId
                agentQuarantined = $agentQuarantined
                circuitId = $circuitId
                circuitOpened = $circuitOpened
            })
        }
        $receipt = [pscustomobject][ordered]@{
            schemaVersion = 1
            watchdogId = $watchdogIdValue
            inspectedAtUtc = $now.ToString('o')
            apply = [bool]$Apply
            policyFingerprint = [string]$policy.fingerprint
            thresholds = [pscustomobject][ordered]@{
                silentDispatchMinutes = $effectiveSilentMinutes
                retryWindowMinutes = [int]$watchdogPolicy.retryWindowMinutes
                maximumRetriesPerWorkspace = [int]$watchdogPolicy.maximumRetriesPerWorkspace
                agentFailureThreshold = [int]$watchdogPolicy.agentFailureThreshold
                quarantineMinutes = $effectiveQuarantineMinutes
            }
            summary = [pscustomobject][ordered]@{
                runningDispatchCount = @($dispatchList.dispatches | Where-Object state -eq 'running').Count
                candidateCount = $candidates.Count
                silentCount = @($candidates | Where-Object silent).Count
                retryExhaustedCount = @($candidates | Where-Object retryExhausted).Count
                changedDispatchCount = $changedDispatchCount
                quarantinedAgentCount = $quarantinedAgentIds.Count
                openedCircuitCount = $openedCircuitIds.Count
            }
            candidates = @($candidates)
            actions = @($actions)
            watchdogHash = ''
        }
        $receipt.watchdogHash = Get-Hash (Get-Payload $receipt)
        $path = Write-Receipt $receipt
        $validation = Test-Receipt $receipt
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'run'; valid = $validation.valid; attention = $candidates.Count -gt 0; receipt = $receipt; path = $path }
    } elseif ($Action -eq 'verify') {
        if ([string]::IsNullOrWhiteSpace($WatchdogId)) { throw 'verify requires WatchdogId.' }
        $receipt = Read-Receipt $WatchdogId
        $validation = Test-Receipt $receipt
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'verify'; valid = $validation.valid; validation = $validation; receipt = $receipt }
    } elseif ($Action -eq 'prune') {
        $retentionCount = [int]$watchdogPolicy.retentionCount
        $retainedWatchdogIds = @{}
        $cycleRoot = Join-Path $schedulerRoot 'cycles'
        foreach ($cycleFile in @(Get-ChildItem -LiteralPath $cycleRoot -File -Filter '*.json' -ErrorAction SilentlyContinue)) {
            try {
                $cycle = Get-Content -LiteralPath $cycleFile.FullName -Raw | ConvertFrom-Json
                if (-not [string]::IsNullOrWhiteSpace([string]$cycle.watchdog.watchdogId)) { $retainedWatchdogIds[[string]$cycle.watchdog.watchdogId] = $true }
            } catch {}
        }
        $circuitRoot = Join-Path $schedulerRoot 'circuits'
        foreach ($circuitFile in @(Get-ChildItem -LiteralPath $circuitRoot -File -Filter '*.json' -ErrorAction SilentlyContinue)) {
            try {
                $circuit = Get-Content -LiteralPath $circuitFile.FullName -Raw | ConvertFrom-Json
                if (-not [string]::IsNullOrWhiteSpace([string]$circuit.sourceWatchdogId)) { $retainedWatchdogIds[[string]$circuit.sourceWatchdogId] = $true }
            } catch {}
        }
        $candidates = @(Get-Files | Sort-Object Name -Descending | Select-Object -Skip $retentionCount | Where-Object {
            $watchdogIdFromFile = ($_.BaseName -split '-')[-1]
            -not $retainedWatchdogIds.ContainsKey($watchdogIdFromFile)
        })
        if ($Apply) { foreach ($file in $candidates) { [System.IO.File]::Delete($file.FullName) } }
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'prune'; apply = [bool]$Apply; retentionCount = $retentionCount; candidateCount = $candidates.Count; changedCount = $(if ($Apply) { $candidates.Count } else { 0 }); candidates = @($candidates.BaseName) }
    } else {
        $items = @((Get-Files) | ForEach-Object {
            try {
                $receipt = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
                $validation = Test-Receipt $receipt
                [pscustomobject][ordered]@{ watchdogId = [string]$receipt.watchdogId; inspectedAtUtc = [string]$receipt.inspectedAtUtc; apply = [bool]$receipt.apply; candidateCount = [int]$receipt.summary.candidateCount; changedDispatchCount = [int]$receipt.summary.changedDispatchCount; valid = $validation.valid; issues = @($validation.issues) }
            } catch {
                [pscustomobject][ordered]@{ watchdogId = $_.BaseName; valid = $false; issues = @($_.Exception.Message) }
            }
        })
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'list'; totalCount = $items.Count; invalidCount = @($items | Where-Object { -not $_.valid }).Count; receipts = $items }
    }
} finally {
    if ($null -ne $lockStream) { $lockStream.Dispose() }
    if ($mutating -and (Test-Path -LiteralPath $lockPath)) { [System.IO.File]::Delete($lockPath) }
}

if ($Format -eq 'Json') {
    $response | ConvertTo-Json -Depth 20
} else {
    if ($Action -eq 'list') { Write-Host "Dispatch watchdog receipts: total=$($response.totalCount), invalid=$($response.invalidCount)" }
    elseif ($Action -eq 'prune') { Write-Host "Dispatch watchdog prune: candidates=$($response.candidateCount), changed=$($response.changedCount)" }
    else { Write-Host "Dispatch watchdog: action=$Action, valid=$($response.valid), candidates=$($response.receipt.summary.candidateCount), changed=$($response.receipt.summary.changedDispatchCount)" }
}
$invalid = if ($Action -eq 'list') { $response.invalidCount -gt 0 } elseif ($Action -eq 'prune') { $false } else { -not $response.valid -or ($Action -eq 'run' -and $response.attention) }
if ($FailOnAttention -and $invalid) { exit 1 }
