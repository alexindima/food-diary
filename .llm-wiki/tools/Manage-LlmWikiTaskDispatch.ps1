[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('list', 'start', 'heartbeat', 'complete', 'fail', 'verify', 'reconcile', 'prune')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [string]$Owner,
    [string]$DispatchId,
    [string]$AgentId,
    [string[]]$RequiredCapability,
    [string]$SchedulePlanId,
    [string]$SchedulePlanHash,
    [string]$ScheduleClaimId,
    [Nullable[int]]$Lane,
    [Nullable[double]]$RoutingScore,
    [Nullable[int]]$LeaseMinutes,
    [string]$Result,
    [Nullable[int]]$RetentionDays,
    [switch]$Apply,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$dispatchRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler/dispatches'
$now = $AsOfUtc.ToUniversalTime()
$workspacePolicy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 15 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Normalize-Workspace([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value) -or [System.IO.Path]::IsPathRooted($Value)) { throw 'WorkspacePath must be repository-relative.' }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') { throw 'WorkspacePath must identify one non-hidden task workspace.' }
    $absolutePath = Join-Path $repositoryRoot $normalized
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Container)) { throw "Task workspace does not exist: $normalized" }
    if (Test-Path -LiteralPath (Join-Path $absolutePath 'completion.json') -PathType Leaf) { throw 'A sealed workspace cannot be dispatched.' }
    return $normalized
}
function Get-ReceiptPath([string]$Id) {
    if ($Id -notmatch '^[a-f0-9]{32}$') { throw 'DispatchId must be a 32-character lowercase hexadecimal identifier.' }
    return Join-Path $dispatchRoot "$Id.json"
}
function Read-Receipt([string]$Id) {
    $path = Get-ReceiptPath $Id
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Dispatch receipt does not exist: $Id" }
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}
function Write-Receipt([object]$Receipt) {
    if (-not (Test-Path -LiteralPath $dispatchRoot)) { New-Item -ItemType Directory -Path $dispatchRoot | Out-Null }
    $path = Get-ReceiptPath ([string]$Receipt.dispatchId)
    $temporaryPath = Join-Path $dispatchRoot ('.dispatch-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [System.IO.File]::WriteAllText($temporaryPath, (($Receipt | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination $path -Force
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [System.IO.File]::Delete($temporaryPath) }
    }
}
function New-Event([object[]]$Existing, [string]$Type, [object]$Details) {
    $previousHash = if ($Existing.Count -gt 0) { [string]$Existing[-1].eventHash } else { '' }
    $payload = [ordered]@{
        sequence = $Existing.Count + 1
        type = $Type
        atUtc = $now.ToString('o')
        details = $Details
        previousHash = $previousHash
    }
    $event = [ordered]@{}
    foreach ($property in $payload.GetEnumerator()) { $event[$property.Key] = $property.Value }
    $event.eventHash = Get-Hash $payload
    return [pscustomobject]$event
}
function Test-Receipt([object]$Receipt) {
    $issues = [System.Collections.Generic.List[string]]::new()
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.dispatchId -notmatch '^[a-f0-9]{32}$') { $issues.Add('dispatchId is invalid.') }
    $lineageValues = @([string]$Receipt.schedulePlanId, [string]$Receipt.schedulePlanHash, [string]$Receipt.scheduleClaimId)
    $lineageCount = @($lineageValues | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    if ($lineageCount -notin @(0, 3)) { $issues.Add('Schedule lineage must be either absent or complete.') }
    if ($lineageCount -eq 3) {
        if ([string]$Receipt.schedulePlanId -notmatch '^[a-f0-9]{32}$') { $issues.Add('schedulePlanId is invalid.') }
        if ([string]$Receipt.schedulePlanHash -notmatch '^[a-f0-9]{64}$') { $issues.Add('schedulePlanHash is invalid.') }
        if ([string]$Receipt.scheduleClaimId -notmatch '^[a-f0-9]{32}$') { $issues.Add('scheduleClaimId is invalid.') }
    }
    $contextValues = @([string]$Receipt.contextBundlePath, [string]$Receipt.contextBundleHash)
    $contextCount = @($contextValues | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    if ($contextCount -notin @(0, 2)) { $issues.Add('Context bundle lineage must be either absent or complete.') }
    if ($contextCount -eq 2) {
        if ([string]$Receipt.contextBundlePath -cne "$($Receipt.workspace)/context-bundle.json") { $issues.Add('contextBundlePath is invalid.') }
        if ([string]$Receipt.contextBundleHash -notmatch '^[a-f0-9]{64}$') { $issues.Add('contextBundleHash is invalid.') }
    }
    $previousHash = ''
    $expectedSequence = 1
    foreach ($event in @($Receipt.events)) {
        if ([int]$event.sequence -ne $expectedSequence) { $issues.Add("Event sequence $($event.sequence) is not contiguous.") }
        if ([string]$event.previousHash -cne $previousHash) { $issues.Add("Event $expectedSequence previousHash is invalid.") }
        $payload = [ordered]@{
            sequence = $event.sequence
            type = $event.type
            atUtc = $event.atUtc
            details = $event.details
            previousHash = $event.previousHash
        }
        $expectedHash = Get-Hash $payload
        if ([string]$event.eventHash -cne $expectedHash) { $issues.Add("Event $expectedSequence hash is invalid.") }
        $previousHash = [string]$event.eventHash
        $expectedSequence++
    }
    if (@($Receipt.events).Count -eq 0 -or [string]$Receipt.events[0].type -ne 'started') { $issues.Add('First event must be started.') }
    $terminalCount = @($Receipt.events | Where-Object type -in @('completed', 'failed')).Count
    if ($terminalCount -gt 1) { $issues.Add('Dispatch has multiple terminal events.') }
    if ($terminalCount -eq 1 -and [string]$Receipt.events[-1].type -notin @('completed', 'failed')) { $issues.Add('Terminal event must be last.') }
    [pscustomobject][ordered]@{
        valid = $issues.Count -eq 0
        issues = @($issues)
        eventCount = @($Receipt.events).Count
        terminal = $terminalCount -eq 1
        status = $(if ($terminalCount -eq 1) { [string]$Receipt.events[-1].type } else { 'active' })
        headEventHash = $previousHash
    }
}
function Append-Event([object]$Receipt, [string]$Type, [object]$Details) {
    $validation = Test-Receipt $Receipt
    if (-not $validation.valid) { throw "Dispatch receipt is invalid: $(@($validation.issues) -join ' ')" }
    if ($validation.terminal) { throw "Dispatch is already terminal: $($validation.status)." }
    $Receipt.events = @($Receipt.events) + (New-Event @($Receipt.events) $Type $Details)
}
function Get-DispatchView([object]$Receipt, [object]$LeaseRegistry) {
    $validation = Test-Receipt $Receipt
    $lease = $LeaseRegistry.leases | Where-Object { $_.active -and $_.leaseId -eq $Receipt.leaseId } | Select-Object -First 1
    $descriptorPath = Join-Path $repositoryRoot "$($Receipt.workspace)/workspace.json"
    $currentFingerprint = if (Test-Path -LiteralPath $descriptorPath -PathType Leaf) {
        [string](Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json).currentPacketFingerprint
    } else { '' }
    $packetDrift = $currentFingerprint -cne [string]$Receipt.packetFingerprint
    $contextBundlePath = Join-Path $repositoryRoot "$($Receipt.workspace)/context-bundle.json"
    $currentContextBundleHash = ''
    $contextBundleValid = $false
    if (Test-Path -LiteralPath $contextBundlePath -PathType Leaf) {
        try {
            $contextValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') verify -WorkspacePath ([string]$Receipt.workspace) -Format Json | ConvertFrom-Json
            $contextBundle = Get-Content -LiteralPath $contextBundlePath -Raw | ConvertFrom-Json
            $currentContextBundleHash = [string]$contextBundle.bundleHash
            $contextBundleValid = [bool]$contextValidation.valid
        } catch { $contextBundleValid = $false }
    }
    $contextDrift = -not [string]::IsNullOrWhiteSpace([string]$Receipt.contextBundleHash) -and
        (-not $contextBundleValid -or $currentContextBundleHash -cne [string]$Receipt.contextBundleHash)
    $state = if (-not $validation.valid) {
        'invalid'
    } elseif ($validation.terminal) {
        $validation.status
    } elseif ($null -eq $lease) {
        'orphaned'
    } elseif ($packetDrift) {
        'packet-drift'
    } elseif ($contextDrift) {
        'context-drift'
    } else {
        'running'
    }
    [pscustomobject][ordered]@{
        dispatchId = [string]$Receipt.dispatchId
        workspace = [string]$Receipt.workspace
        owner = [string]$Receipt.owner
        agentId = [string]$Receipt.agentId
        agentCapabilities = @($Receipt.agentCapabilities)
        requiredCapabilities = @($Receipt.requiredCapabilities)
        leaseId = [string]$Receipt.leaseId
        lane = $Receipt.lane
        routingScore = $Receipt.routingScore
        schedulePlanId = [string]$Receipt.schedulePlanId
        schedulePlanHash = [string]$Receipt.schedulePlanHash
        scheduleClaimId = [string]$Receipt.scheduleClaimId
        state = $state
        startedAtUtc = [string]$Receipt.startedAtUtc
        packetFingerprint = [string]$Receipt.packetFingerprint
        currentPacketFingerprint = $currentFingerprint
        packetDrift = $packetDrift
        contextBundlePath = [string]$Receipt.contextBundlePath
        contextBundleHash = [string]$Receipt.contextBundleHash
        currentContextBundleHash = $currentContextBundleHash
        contextDrift = $contextDrift
        eventCount = $validation.eventCount
        headEventHash = $validation.headEventHash
        valid = $validation.valid
        issues = @($validation.issues)
        lease = $lease
        result = $(if ($validation.terminal) { [string]$Receipt.events[-1].details.result } else { '' })
    }
}

if ($Action -eq 'reconcile') {
    $leaseRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
    $candidates = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @(Get-ChildItem -LiteralPath $dispatchRoot -File -Filter '*.json' -ErrorAction SilentlyContinue | Sort-Object Name)) {
        try {
            $receiptRaw = Get-Content -LiteralPath $file.FullName -Raw
            $receipt = $receiptRaw | ConvertFrom-Json
            $dispatchView = Get-DispatchView $receipt $leaseRegistry
            if (-not $dispatchView.valid -or $dispatchView.state -notin @('orphaned', 'packet-drift', 'context-drift')) { continue }
            $candidate = [pscustomobject][ordered]@{
                dispatchId = $dispatchView.dispatchId
                workspace = $dispatchView.workspace
                owner = $dispatchView.owner
                previousState = $dispatchView.state
                action = 'fail'
                applied = [bool]$Apply
            }
            if ($Apply) {
                $reason = "Automatically reconciled $($dispatchView.state) dispatch at $($now.ToString('o'))."
                Append-Event $receipt 'failed' ([pscustomobject][ordered]@{
                    result = $reason
                    reconciled = $true
                    previousState = $dispatchView.state
                })
                Write-Receipt $receipt
                try {
                    $activeLease = $leaseRegistry.leases | Where-Object { $_.active -and $_.leaseId -eq $receipt.leaseId } | Select-Object -First 1
                    if ($null -ne $activeLease) {
                        & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') release -LeaseId $receipt.leaseId -Owner $receipt.owner -AsOfUtc $now | Out-Null
                    }
                } catch {
                    [System.IO.File]::WriteAllText($file.FullName, $receiptRaw, [System.Text.UTF8Encoding]::new($false))
                    throw
                }
            }
            $candidates.Add($candidate)
        } catch {
            if ($_.Exception.Message -notmatch '^Dispatch receipt is invalid:') { throw }
        }
    }
    $response = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = 'reconcile'
        asOfUtc = $now.ToString('o')
        apply = [bool]$Apply
        candidateCount = $candidates.Count
        changedCount = $(if ($Apply) { $candidates.Count } else { 0 })
        candidates = @($candidates)
    }
} elseif ($Action -eq 'prune') {
    $effectiveRetentionDays = if ($null -ne $RetentionDays) { [int]$RetentionDays } else { [int]$workspacePolicy.scheduler.terminalReceiptRetentionDays }
    if ($effectiveRetentionDays -lt 1 -or $effectiveRetentionDays -gt [int]$workspacePolicy.scheduler.maximumReceiptRetentionDays) {
        throw "RetentionDays must be between 1 and $($workspacePolicy.scheduler.maximumReceiptRetentionDays)."
    }
    $cutoff = $now.AddDays(-$effectiveRetentionDays)
    $retainedClaimIds = @{}
    $claimRoot = Join-Path (Split-Path -Parent $dispatchRoot) 'claims'
    foreach ($claimFile in @(Get-ChildItem -LiteralPath $claimRoot -File -Filter '*.json' -ErrorAction SilentlyContinue)) {
        try {
            $claim = Get-Content -LiteralPath $claimFile.FullName -Raw | ConvertFrom-Json
            if ([string]$claim.claimId -match '^[a-f0-9]{32}$') { $retainedClaimIds[[string]$claim.claimId] = $true }
        } catch {}
    }
    $candidates = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @(Get-ChildItem -LiteralPath $dispatchRoot -File -Filter '*.json' -ErrorAction SilentlyContinue | Sort-Object Name)) {
        try {
            $receipt = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
            $validation = Test-Receipt $receipt
            if (-not $validation.valid -or -not $validation.terminal) { continue }
            if (-not [string]::IsNullOrWhiteSpace([string]$receipt.scheduleClaimId) -and $retainedClaimIds.ContainsKey([string]$receipt.scheduleClaimId)) { continue }
            $terminalAtUtc = ([DateTimeOffset]$receipt.events[-1].atUtc).UtcDateTime
            if ($terminalAtUtc -gt $cutoff) { continue }
            $candidates.Add([pscustomobject][ordered]@{
                dispatchId = [string]$receipt.dispatchId
                workspace = [string]$receipt.workspace
                state = $validation.status
                terminalAtUtc = $terminalAtUtc.ToString('o')
                headEventHash = $validation.headEventHash
                applied = [bool]$Apply
            })
            if ($Apply) { [System.IO.File]::Delete($file.FullName) }
        } catch {
            continue
        }
    }
    $response = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = 'prune'
        asOfUtc = $now.ToString('o')
        apply = [bool]$Apply
        retentionDays = $effectiveRetentionDays
        cutoffUtc = $cutoff.ToString('o')
        candidateCount = $candidates.Count
        changedCount = $(if ($Apply) { $candidates.Count } else { 0 })
        candidates = @($candidates)
    }
} elseif ($Action -eq 'start') {
    $normalizedWorkspace = Normalize-Workspace $WorkspacePath
    if ([string]::IsNullOrWhiteSpace($Owner) -or $Owner.Length -gt 200) { throw 'Owner is required and must not exceed 200 characters.' }
    if ($null -ne $Lane -and ([int]$Lane -lt 1 -or [int]$Lane -gt 32)) { throw 'Lane must be between 1 and 32.' }
    if ($null -ne $RoutingScore -and ([double]$RoutingScore -lt 0 -or [double]$RoutingScore -gt 100)) { throw 'RoutingScore must be between 0 and 100.' }
    $scheduleLineageCount = @(@($SchedulePlanId, $SchedulePlanHash, $ScheduleClaimId) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count
    if ($scheduleLineageCount -notin @(0, 3)) { throw 'SchedulePlanId, SchedulePlanHash, and ScheduleClaimId must be supplied together.' }
    if ($scheduleLineageCount -eq 3) {
        if ($SchedulePlanId -notmatch '^[a-f0-9]{32}$') { throw 'SchedulePlanId is invalid.' }
        if ($SchedulePlanHash -notmatch '^[a-f0-9]{64}$') { throw 'SchedulePlanHash is invalid.' }
        if ($ScheduleClaimId -notmatch '^[a-f0-9]{32}$') { throw 'ScheduleClaimId is invalid.' }
    }
    $descriptor = Get-Content -LiteralPath (Join-Path $repositoryRoot "$normalizedWorkspace/workspace.json") -Raw | ConvertFrom-Json
    $requiredCapabilities = @($RequiredCapability | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique | Sort-Object)
    foreach ($capability in $requiredCapabilities) {
        if ($capability -notin @($workspacePolicy.scheduler.agentRegistry.allowedCapabilities)) { throw "Unsupported required capability: $capability" }
    }
    $registeredAgent = $null
    if (-not [string]::IsNullOrWhiteSpace($AgentId)) {
        $agentRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAgentRegistry.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
        $registeredAgent = $agentRegistry.agents | Where-Object { $_.active -and $_.agentId -eq $AgentId } | Select-Object -First 1
        if ($null -eq $registeredAgent) { throw "Active agent registration was not found: $AgentId" }
        if ([string]$registeredAgent.owner -cne $Owner) { throw 'Dispatch owner does not match the registered agent owner.' }
        if ([int]$registeredAgent.availableCapacity -lt 1) { throw 'Registered agent has no available capacity.' }
    }
    $contextBundleAbsolutePath = Join-Path $repositoryRoot "$normalizedWorkspace/context-bundle.json"
    $contextBundleResult = if (Test-Path -LiteralPath $contextBundleAbsolutePath -PathType Leaf) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') verify -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
    } else { $null }
    if ($null -eq $contextBundleResult -or -not $contextBundleResult.valid) {
        $contextBundleResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') create `
            -WorkspacePath $normalizedWorkspace `
            -AsOfUtc $now `
            -Format Json | ConvertFrom-Json
    }
    $contextBundle = if ($null -ne $contextBundleResult.bundle) {
        $contextBundleResult.bundle
    } else {
        Get-Content -LiteralPath $contextBundleAbsolutePath -Raw | ConvertFrom-Json
    }
    $leaseArguments = @{
        Action = 'acquire'
        WorkspacePath = $normalizedWorkspace
        Owner = $Owner
        AsOfUtc = $now
        Format = 'Json'
    }
    if ($null -ne $LeaseMinutes) { $leaseArguments.LeaseMinutes = $LeaseMinutes }
    $leaseResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') @leaseArguments | ConvertFrom-Json
    $dispatchId = [guid]::NewGuid().ToString('N')
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        dispatchId = $dispatchId
        workspace = $normalizedWorkspace
        owner = $Owner
        agentId = $(if ($null -ne $registeredAgent) { [string]$registeredAgent.agentId } else { '' })
        agentCapabilities = @(if ($null -ne $registeredAgent) { @($registeredAgent.capabilities) } else { @() })
        requiredCapabilities = $requiredCapabilities
        leaseId = [string]$leaseResult.lease.leaseId
        lane = $Lane
        routingScore = $RoutingScore
        schedulePlanId = $SchedulePlanId
        schedulePlanHash = $SchedulePlanHash
        scheduleClaimId = $ScheduleClaimId
        startedAtUtc = $now.ToString('o')
        packetFingerprint = [string]$descriptor.currentPacketFingerprint
        contextBundlePath = "$normalizedWorkspace/context-bundle.json"
        contextBundleHash = [string]$contextBundle.bundleHash
        events = @()
    }
    $receipt.events = @(New-Event @() 'started' ([pscustomobject][ordered]@{
        owner = $Owner
        agentId = $(if ($null -ne $registeredAgent) { [string]$registeredAgent.agentId } else { '' })
        agentCapabilities = @(if ($null -ne $registeredAgent) { @($registeredAgent.capabilities) } else { @() })
        requiredCapabilities = $requiredCapabilities
        leaseId = [string]$leaseResult.lease.leaseId
        lane = $Lane
        routingScore = $RoutingScore
        schedulePlanId = $SchedulePlanId
        schedulePlanHash = $SchedulePlanHash
        scheduleClaimId = $ScheduleClaimId
        packetFingerprint = [string]$descriptor.currentPacketFingerprint
        contextBundlePath = "$normalizedWorkspace/context-bundle.json"
        contextBundleHash = [string]$contextBundle.bundleHash
    }))
    try {
        Write-Receipt $receipt
    } catch {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') release -LeaseId $leaseResult.lease.leaseId -Owner $Owner -AsOfUtc $now | Out-Null
        throw
    }
    $leaseRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
    $view = Get-DispatchView $receipt $leaseRegistry
} elseif ($Action -in @('heartbeat', 'complete', 'fail')) {
    if ([string]::IsNullOrWhiteSpace($DispatchId)) { throw "$Action requires DispatchId." }
    $receiptPath = Get-ReceiptPath $DispatchId
    $receiptRaw = Get-Content -LiteralPath $receiptPath -Raw
    $receipt = $receiptRaw | ConvertFrom-Json
    if (-not [string]::IsNullOrWhiteSpace($Owner) -and [string]$receipt.owner -cne $Owner) { throw 'Dispatch owner does not match.' }
    if ($Action -eq 'heartbeat') {
        $leaseArguments = @{
            Action = 'heartbeat'
            LeaseId = [string]$receipt.leaseId
            Owner = [string]$receipt.owner
            AsOfUtc = $now
            Format = 'Json'
        }
        if ($null -ne $LeaseMinutes) { $leaseArguments.LeaseMinutes = $LeaseMinutes }
        $leaseResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') @leaseArguments | ConvertFrom-Json
        Append-Event $receipt 'heartbeat' ([pscustomobject][ordered]@{ expiresAtUtc = [string]$leaseResult.lease.expiresAtUtc })
        Write-Receipt $receipt
    } else {
        if ([string]::IsNullOrWhiteSpace($Result) -or $Result.Length -gt 2000) { throw "$Action requires Result with at most 2000 characters." }
        $eventType = if ($Action -eq 'complete') { 'completed' } else { 'failed' }
        Append-Event $receipt $eventType ([pscustomobject][ordered]@{ result = $Result })
        Write-Receipt $receipt
        try {
            $leaseRegistryBeforeRelease = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
            $activeLease = $leaseRegistryBeforeRelease.leases | Where-Object { $_.active -and $_.leaseId -eq $receipt.leaseId } | Select-Object -First 1
            if ($null -ne $activeLease) {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') release -LeaseId $receipt.leaseId -Owner $receipt.owner -AsOfUtc $now | Out-Null
            }
        } catch {
            [System.IO.File]::WriteAllText($receiptPath, $receiptRaw, [System.Text.UTF8Encoding]::new($false))
            throw
        }
    }
    $leaseRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
    $view = Get-DispatchView $receipt $leaseRegistry
} elseif ($Action -eq 'verify') {
    if ([string]::IsNullOrWhiteSpace($DispatchId)) { throw 'verify requires DispatchId.' }
    $receipt = Read-Receipt $DispatchId
    $leaseRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
    $view = Get-DispatchView $receipt $leaseRegistry
} else {
    $leaseRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
    $views = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @(Get-ChildItem -LiteralPath $dispatchRoot -File -Filter '*.json' -ErrorAction SilentlyContinue | Sort-Object Name)) {
        try {
            $receipt = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
            $views.Add((Get-DispatchView $receipt $leaseRegistry))
        } catch {
            $views.Add([pscustomobject][ordered]@{ dispatchId = $file.BaseName; state = 'invalid'; valid = $false; issues = @($_.Exception.Message) })
        }
    }
    $response = [pscustomobject][ordered]@{
        schemaVersion = 1
        asOfUtc = $now.ToString('o')
        totalCount = $views.Count
        runningCount = @($views | Where-Object state -eq 'running').Count
        orphanedCount = @($views | Where-Object state -eq 'orphaned').Count
        driftedCount = @($views | Where-Object state -eq 'packet-drift').Count
        terminalCount = @($views | Where-Object state -in @('completed', 'failed')).Count
        invalidCount = @($views | Where-Object state -eq 'invalid').Count
        dispatches = @($views)
    }
}
if ($Action -notin @('list', 'reconcile', 'prune')) {
    $response = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = $Action
        valid = [bool]$view.valid
        dispatch = $view
    }
}
if ($Format -eq 'Json') {
    $response | ConvertTo-Json -Depth 12
} else {
    if ($Action -eq 'list') {
        Write-Host "Task dispatches: total=$($response.totalCount), running=$($response.runningCount), orphaned=$($response.orphanedCount), drifted=$($response.driftedCount), invalid=$($response.invalidCount)"
        foreach ($dispatch in $response.dispatches) { Write-Host " - $($dispatch.dispatchId): $($dispatch.workspace) [$($dispatch.state)]" }
    } elseif ($Action -in @('reconcile', 'prune')) {
        Write-Host "Task dispatch $Action`: candidates=$($response.candidateCount), changed=$($response.changedCount), apply=$($response.apply)"
        foreach ($candidate in $response.candidates) { Write-Host " - $($candidate.dispatchId): $($candidate.workspace)" }
    } else {
        Write-Host "Task dispatch: action=$Action, id=$($view.dispatchId), state=$($view.state), valid=$($view.valid)"
        foreach ($issue in @($view.issues)) { Write-Host " - $issue" }
    }
}
$shouldFail = if ($Action -eq 'list') {
    $response.invalidCount -gt 0 -or $response.orphanedCount -gt 0 -or $response.driftedCount -gt 0
} elseif ($Action -in @('reconcile', 'prune')) {
    $false
} else {
    -not $response.valid
}
if ($FailOnInvalid -and $shouldFail) { exit 1 }
