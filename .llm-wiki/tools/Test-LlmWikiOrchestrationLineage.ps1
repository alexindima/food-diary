[CmdletBinding()]
param(
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$schedulerRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler'
$planRoot = Join-Path $schedulerRoot 'plans'
$claimRoot = Join-Path $schedulerRoot 'claims'
$dispatchRoot = Join-Path $schedulerRoot 'dispatches'
$cycleRoot = Join-Path $schedulerRoot 'cycles'
$watchdogRoot = Join-Path $schedulerRoot 'watchdog'
$circuitRoot = Join-Path $schedulerRoot 'circuits'
$auditTime = $AsOfUtc.ToUniversalTime()

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Read-JsonFiles([string]$Root) {
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -File -Filter '*.json' -ErrorAction SilentlyContinue | Sort-Object Name)) {
        try {
            $items.Add([pscustomobject]@{ file = $file; value = (Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json); parseIssue = '' })
        } catch {
            $items.Add([pscustomobject]@{ file = $file; value = $null; parseIssue = $_.Exception.Message })
        }
    }
    return @($items)
}
function Get-PlanPayload([object]$Plan) {
    [ordered]@{
        schemaVersion = $Plan.schemaVersion
        planId = $Plan.planId
        createdAtUtc = $Plan.createdAtUtc
        expiresAtUtc = $Plan.expiresAtUtc
        policyFingerprint = $Plan.policyFingerprint
        tasksPath = $Plan.tasksPath
        maxConcurrency = $Plan.maxConcurrency
        routingMode = $Plan.routingMode
        assignments = $Plan.assignments
    }
}
function Get-ClaimPayload([object]$Claim) {
    [ordered]@{
        schemaVersion = $Claim.schemaVersion
        claimId = $Claim.claimId
        planId = $Claim.planId
        planHash = $Claim.planHash
        claimedAtUtc = $Claim.claimedAtUtc
        apply = $Claim.apply
        state = $Claim.state
        dispatchIds = $Claim.dispatchIds
        issue = $Claim.issue
    }
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
function Get-WatchdogPayload([object]$Receipt) {
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
function Get-CircuitPayload([object]$Receipt) {
    [ordered]@{
        schemaVersion = $Receipt.schemaVersion
        circuitId = $Receipt.circuitId
        event = $Receipt.event
        workspace = $Receipt.workspace
        packetFingerprint = $Receipt.packetFingerprint
        occurredAtUtc = $Receipt.occurredAtUtc
        openUntilUtc = $Receipt.openUntilUtc
        reason = $Receipt.reason
        policyFingerprint = $Receipt.policyFingerprint
        predecessorCircuitId = $Receipt.predecessorCircuitId
        sourceWatchdogId = $Receipt.sourceWatchdogId
    }
}

$planFiles = @(Read-JsonFiles $planRoot)
$claimFiles = @(Read-JsonFiles $claimRoot)
$dispatchFiles = @(Read-JsonFiles $dispatchRoot)
$cycleFiles = @(Read-JsonFiles $cycleRoot)
$watchdogFiles = @(Read-JsonFiles $watchdogRoot)
$circuitFiles = @(Read-JsonFiles $circuitRoot)
$plans = @{}
$claims = @{}
$dispatches = @{}
$watchdogs = @{}
$circuits = @{}
$issues = [System.Collections.Generic.List[object]]::new()

foreach ($entry in $planFiles) {
    if ($null -eq $entry.value) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-plan-json'; artifactId = $entry.file.BaseName; detail = $entry.parseIssue })
        continue
    }
    $plan = $entry.value
    if ([string]$plan.planId -notmatch '^[a-f0-9]{32}$' -or [string]$plan.planHash -cne (Get-Hash (Get-PlanPayload $plan))) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-plan'; artifactId = [string]$plan.planId; detail = 'Plan identity or content hash is invalid.' })
    }
    if ($plans.ContainsKey([string]$plan.planId)) {
        $issues.Add([pscustomobject][ordered]@{ type = 'duplicate-plan'; artifactId = [string]$plan.planId; detail = 'More than one plan has this identifier.' })
    } else {
        $plans[[string]$plan.planId] = $plan
    }
}

foreach ($entry in $claimFiles) {
    if ($null -eq $entry.value) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-claim-json'; artifactId = $entry.file.BaseName; detail = $entry.parseIssue })
        continue
    }
    $claim = $entry.value
    if ([string]$claim.claimId -notmatch '^[a-f0-9]{32}$' -or [string]$claim.claimHash -cne (Get-Hash (Get-ClaimPayload $claim))) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-claim'; artifactId = [string]$claim.claimId; detail = 'Claim identity or content hash is invalid.' })
    }
    if ($claims.ContainsKey([string]$claim.claimId)) {
        $issues.Add([pscustomobject][ordered]@{ type = 'duplicate-claim'; artifactId = [string]$claim.claimId; detail = 'More than one claim has this identifier.' })
    } else {
        $claims[[string]$claim.claimId] = $claim
    }
    if (-not $plans.ContainsKey([string]$claim.planId)) {
        $issues.Add([pscustomobject][ordered]@{ type = 'missing-plan'; artifactId = [string]$claim.claimId; detail = "Referenced plan does not exist: $($claim.planId)" })
    } elseif ([string]$plans[[string]$claim.planId].planHash -cne [string]$claim.planHash) {
        $issues.Add([pscustomobject][ordered]@{ type = 'plan-hash-mismatch'; artifactId = [string]$claim.claimId; detail = 'Claim references a different plan hash.' })
    }
    if (-not [bool]$claim.apply -and @($claim.dispatchIds).Count -gt 0) {
        $issues.Add([pscustomobject][ordered]@{ type = 'dry-run-dispatch'; artifactId = [string]$claim.claimId; detail = 'A dry-run claim must not reference dispatches.' })
    }
}

foreach ($entry in $dispatchFiles) {
    if ($null -eq $entry.value) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-dispatch-json'; artifactId = $entry.file.BaseName; detail = $entry.parseIssue })
        continue
    }
    $dispatch = $entry.value
    $dispatches[[string]$dispatch.dispatchId] = $dispatch
    $verification = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') verify -DispatchId $dispatch.dispatchId -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
    if (-not $verification.valid) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-dispatch'; artifactId = [string]$dispatch.dispatchId; detail = (@($verification.dispatch.issues) -join ' ') })
    }
    $hasLineage = -not [string]::IsNullOrWhiteSpace([string]$dispatch.scheduleClaimId)
    if ($hasLineage) {
        if (-not $claims.ContainsKey([string]$dispatch.scheduleClaimId)) {
            $issues.Add([pscustomobject][ordered]@{ type = 'missing-claim'; artifactId = [string]$dispatch.dispatchId; detail = "Referenced claim does not exist: $($dispatch.scheduleClaimId)" })
        } else {
            $claim = $claims[[string]$dispatch.scheduleClaimId]
            if ([string]$dispatch.schedulePlanId -cne [string]$claim.planId -or [string]$dispatch.schedulePlanHash -cne [string]$claim.planHash) {
                $issues.Add([pscustomobject][ordered]@{ type = 'dispatch-plan-mismatch'; artifactId = [string]$dispatch.dispatchId; detail = 'Dispatch and claim reference different plans.' })
            }
            if (@($claim.dispatchIds) -notcontains [string]$dispatch.dispatchId) {
                $issues.Add([pscustomobject][ordered]@{ type = 'claim-backlink-missing'; artifactId = [string]$dispatch.dispatchId; detail = 'Claim does not link back to this dispatch.' })
            }
        }
    }
}

foreach ($claim in @($claims.Values)) {
    foreach ($dispatchId in @($claim.dispatchIds)) {
        if (-not $dispatches.ContainsKey([string]$dispatchId)) {
            $issues.Add([pscustomobject][ordered]@{ type = 'missing-dispatch'; artifactId = [string]$claim.claimId; detail = "Referenced dispatch does not exist: $dispatchId" })
            continue
        }
        $dispatch = $dispatches[[string]$dispatchId]
        if ([string]$dispatch.scheduleClaimId -cne [string]$claim.claimId) {
            $issues.Add([pscustomobject][ordered]@{ type = 'dispatch-claim-mismatch'; artifactId = [string]$dispatchId; detail = 'Dispatch does not link back to its claim.' })
        }
    }
}

foreach ($group in @($claims.Values | Where-Object { $_.apply -and $_.state -eq 'claimed' } | Group-Object planId | Where-Object Count -gt 1)) {
    $issues.Add([pscustomobject][ordered]@{ type = 'duplicate-successful-claim'; artifactId = [string]$group.Name; detail = "$($group.Count) successful claims reference the same plan." })
}

foreach ($entry in $watchdogFiles) {
    if ($null -eq $entry.value) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-watchdog-json'; artifactId = $entry.file.BaseName; detail = $entry.parseIssue })
        continue
    }
    if ([string]$entry.value.watchdogId -notmatch '^[a-f0-9]{32}$' -or [string]$entry.value.watchdogHash -cne (Get-Hash (Get-WatchdogPayload $entry.value))) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-watchdog'; artifactId = [string]$entry.value.watchdogId; detail = 'Watchdog identity or content hash is invalid.' })
    } else {
        $watchdogs[[string]$entry.value.watchdogId] = $entry.value
    }
}

foreach ($entry in $circuitFiles) {
    if ($null -eq $entry.value) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-circuit-json'; artifactId = $entry.file.BaseName; detail = $entry.parseIssue })
        continue
    }
    $circuit = $entry.value
    if ([string]$circuit.circuitId -notmatch '^[a-f0-9]{32}$' -or [string]$circuit.circuitHash -cne (Get-Hash (Get-CircuitPayload $circuit))) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-circuit'; artifactId = [string]$circuit.circuitId; detail = 'Circuit identity or content hash is invalid.' })
    } elseif ($circuits.ContainsKey([string]$circuit.circuitId)) {
        $issues.Add([pscustomobject][ordered]@{ type = 'duplicate-circuit'; artifactId = [string]$circuit.circuitId; detail = 'More than one circuit receipt has this identifier.' })
    } else {
        $circuits[[string]$circuit.circuitId] = $circuit
    }
}
foreach ($circuit in @($circuits.Values)) {
    if (-not [string]::IsNullOrWhiteSpace([string]$circuit.predecessorCircuitId) -and -not $circuits.ContainsKey([string]$circuit.predecessorCircuitId)) {
        $issues.Add([pscustomobject][ordered]@{ type = 'circuit-predecessor-missing'; artifactId = [string]$circuit.circuitId; detail = "Referenced predecessor does not exist: $($circuit.predecessorCircuitId)" })
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$circuit.sourceWatchdogId)) {
        if (-not $watchdogs.ContainsKey([string]$circuit.sourceWatchdogId)) {
            $issues.Add([pscustomobject][ordered]@{ type = 'circuit-watchdog-missing'; artifactId = [string]$circuit.circuitId; detail = "Referenced watchdog does not exist: $($circuit.sourceWatchdogId)" })
        } else {
            $watchdogAction = $watchdogs[[string]$circuit.sourceWatchdogId].actions | Where-Object circuitId -eq $circuit.circuitId | Select-Object -First 1
            if ($null -eq $watchdogAction) {
                $issues.Add([pscustomobject][ordered]@{ type = 'watchdog-circuit-backlink-missing'; artifactId = [string]$circuit.circuitId; detail = 'Source watchdog does not link back to this circuit.' })
            }
        }
    }
}

foreach ($entry in $cycleFiles) {
    if ($null -eq $entry.value) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-cycle-json'; artifactId = $entry.file.BaseName; detail = $entry.parseIssue })
        continue
    }
    $cycle = $entry.value
    if ([string]$cycle.cycleId -notmatch '^[a-f0-9]{32}$' -or [string]$cycle.cycleHash -cne (Get-Hash (Get-CyclePayload $cycle))) {
        $issues.Add([pscustomobject][ordered]@{ type = 'invalid-cycle'; artifactId = [string]$cycle.cycleId; detail = 'Cycle identity or content hash is invalid.' })
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$cycle.plan.planId)) {
        if (-not $plans.ContainsKey([string]$cycle.plan.planId)) {
            $issues.Add([pscustomobject][ordered]@{ type = 'cycle-plan-missing'; artifactId = [string]$cycle.cycleId; detail = "Referenced plan does not exist: $($cycle.plan.planId)" })
        } elseif ([string]$plans[[string]$cycle.plan.planId].planHash -cne [string]$cycle.plan.planHash) {
            $issues.Add([pscustomobject][ordered]@{ type = 'cycle-plan-mismatch'; artifactId = [string]$cycle.cycleId; detail = 'Cycle references a different plan hash.' })
        }
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$cycle.claim.claimId)) {
        if (-not $claims.ContainsKey([string]$cycle.claim.claimId)) {
            $issues.Add([pscustomobject][ordered]@{ type = 'cycle-claim-missing'; artifactId = [string]$cycle.cycleId; detail = "Referenced claim does not exist: $($cycle.claim.claimId)" })
        } else {
            $cycleClaim = $claims[[string]$cycle.claim.claimId]
            if ([string]$cycleClaim.claimHash -cne [string]$cycle.claim.claimHash) {
                $issues.Add([pscustomobject][ordered]@{ type = 'cycle-claim-mismatch'; artifactId = [string]$cycle.cycleId; detail = 'Cycle references a different claim hash.' })
            }
            $claimDispatchSet = @($cycleClaim.dispatchIds | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { [string]$_ } | Sort-Object)
            $cycleDispatchSet = @($cycle.claim.dispatchIds | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { [string]$_ } | Sort-Object)
            if (($claimDispatchSet -join "`n") -cne ($cycleDispatchSet -join "`n")) {
                $issues.Add([pscustomobject][ordered]@{ type = 'cycle-dispatch-mismatch'; artifactId = [string]$cycle.cycleId; detail = 'Cycle and claim reference different dispatch sets.' })
            }
        }
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$cycle.watchdog.watchdogId)) {
        if (-not $watchdogs.ContainsKey([string]$cycle.watchdog.watchdogId)) {
            $issues.Add([pscustomobject][ordered]@{ type = 'cycle-watchdog-missing'; artifactId = [string]$cycle.cycleId; detail = "Referenced watchdog receipt does not exist: $($cycle.watchdog.watchdogId)" })
        } elseif ([string]$watchdogs[[string]$cycle.watchdog.watchdogId].watchdogHash -cne [string]$cycle.watchdog.watchdogHash) {
            $issues.Add([pscustomobject][ordered]@{ type = 'cycle-watchdog-mismatch'; artifactId = [string]$cycle.cycleId; detail = 'Cycle references a different watchdog hash.' })
        }
    }
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    auditedAtUtc = $auditTime.ToString('o')
    valid = $issues.Count -eq 0
    summary = [pscustomobject][ordered]@{
        planCount = $plans.Count
        claimCount = $claims.Count
        dispatchCount = $dispatches.Count
        cycleCount = $cycleFiles.Count
        watchdogCount = $watchdogFiles.Count
        circuitCount = $circuits.Count
        linkedDispatchCount = @($dispatches.Values | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.scheduleClaimId) }).Count
        issueCount = $issues.Count
    }
    issues = @($issues)
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 12
} else {
    Write-Host "Orchestration lineage: valid=$($result.valid), plans=$($result.summary.planCount), claims=$($result.summary.claimCount), dispatches=$($result.summary.dispatchCount), circuits=$($result.summary.circuitCount), linked=$($result.summary.linkedDispatchCount), issues=$($result.summary.issueCount)"
    foreach ($issue in $result.issues) { Write-Host "- [$($issue.type)] $($issue.artifactId): $($issue.detail)" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
