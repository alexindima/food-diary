[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('list', 'create', 'verify', 'claim', 'prune')]
    [string]$Action = 'list',
    [string]$PlanId,
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [Nullable[int]]$MaxConcurrency,
    [Nullable[int]]$TtlMinutes,
    [Nullable[int]]$SimulateFailureAfter,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$Apply,
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
$lockPath = Join-Path $schedulerRoot '.schedule-plan-lock'
$now = $AsOfUtc.ToUniversalTime()
$policySnapshot = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json | ConvertFrom-Json
$policy = $policySnapshot.policy
$policyFingerprint = [string]$policySnapshot.fingerprint
$planPolicy = $policy.scheduler.agentRegistry.schedulePlans
$effectiveTtlMinutes = if ($null -ne $TtlMinutes) { [int]$TtlMinutes } else { [int]$planPolicy.defaultTtlMinutes }
if ($effectiveTtlMinutes -lt 1 -or $effectiveTtlMinutes -gt [int]$planPolicy.maximumTtlMinutes) { throw "TtlMinutes must be between 1 and $($planPolicy.maximumTtlMinutes)." }
if ($null -ne $SimulateFailureAfter -and [int]$SimulateFailureAfter -lt 1) { throw 'SimulateFailureAfter must be positive.' }

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
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
function Test-PlanHash([object]$Plan) {
    $issues = [System.Collections.Generic.List[string]]::new()
    if ($Plan.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Plan.planId -notmatch '^[a-f0-9]{32}$') { $issues.Add('planId is invalid.') }
    $expectedHash = Get-Hash (Get-PlanPayload $Plan)
    if ([string]$Plan.planHash -cne $expectedHash) { $issues.Add('planHash is invalid.') }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues); expectedHash = $expectedHash }
}
function Get-PlanFiles {
    if (-not (Test-Path -LiteralPath $planRoot -PathType Container)) { return @() }
    return @(Get-ChildItem -LiteralPath $planRoot -File -Filter '*.json' | Sort-Object Name)
}
function Get-ClaimFiles {
    if (-not (Test-Path -LiteralPath $claimRoot -PathType Container)) { return @() }
    return @(Get-ChildItem -LiteralPath $claimRoot -File -Filter '*.json' | Sort-Object Name)
}
function Test-Claim([object]$Claim) {
    $payload = [ordered]@{
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
    $issues = [System.Collections.Generic.List[string]]::new()
    if ($Claim.schemaVersion -ne 1) { $issues.Add('claim schemaVersion must be 1.') }
    if ([string]$Claim.claimId -notmatch '^[a-f0-9]{32}$') { $issues.Add('claimId is invalid.') }
    if ([string]$Claim.claimHash -cne (Get-Hash $payload)) { $issues.Add('claimHash is invalid.') }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}
function Read-Plan([string]$Id) {
    if ($Id -notmatch '^[a-f0-9]{32}$') { throw 'PlanId must be a 32-character lowercase hexadecimal identifier.' }
    $matches = @(Get-PlanFiles | Where-Object BaseName -like "*-$Id")
    if ($matches.Count -ne 1) { throw "Schedule plan does not exist or is ambiguous: $Id" }
    return Get-Content -LiteralPath $matches[0].FullName -Raw | ConvertFrom-Json
}
function Write-JsonAtomic([string]$Directory, [string]$FileName, [object]$Value, [string]$Prefix) {
    if (-not (Test-Path -LiteralPath $Directory)) { New-Item -ItemType Directory -Path $Directory | Out-Null }
    $temporaryPath = Join-Path $Directory (".$Prefix-" + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [System.IO.File]::WriteAllText($temporaryPath, (($Value | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination (Join-Path $Directory $FileName)
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [System.IO.File]::Delete($temporaryPath) }
    }
}
function Test-PlanCurrent([object]$Plan) {
    $hashValidation = Test-PlanHash $Plan
    $issues = [System.Collections.Generic.List[string]]::new()
    foreach ($issue in @($hashValidation.issues)) { $issues.Add([string]$issue) }
    $expired = ([DateTimeOffset]$Plan.expiresAtUtc).UtcDateTime -le $now
    if ($expired) { $issues.Add('Schedule plan has expired.') }
    if ([string]$Plan.policyFingerprint -cne $policyFingerprint) { $issues.Add('Workspace policy fingerprint changed after plan creation.') }
    foreach ($claimFile in Get-ClaimFiles) {
        try {
            $existingClaim = Get-Content -LiteralPath $claimFile.FullName -Raw | ConvertFrom-Json
            if ((Test-Claim $existingClaim).valid -and
                [string]$existingClaim.planId -eq [string]$Plan.planId -and
                [bool]$existingClaim.apply -and
                [string]$existingClaim.state -eq 'claimed') {
                $issues.Add("Schedule plan was already claimed by $($existingClaim.claimId).")
                break
            }
        } catch {
            continue
        }
    }
    $leaseRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
    $agentRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAgentRegistry.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
    $assignmentCounts = @{}
    foreach ($assignment in @($Plan.assignments)) {
        $descriptorPath = Join-Path $repositoryRoot "$($assignment.workspace)/workspace.json"
        if (-not (Test-Path -LiteralPath $descriptorPath -PathType Leaf)) {
            $issues.Add("Workspace disappeared: $($assignment.workspace).")
            continue
        }
        $currentFingerprint = [string](Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json).currentPacketFingerprint
        if ($currentFingerprint -cne [string]$assignment.packetFingerprint) { $issues.Add("Packet fingerprint changed: $($assignment.workspace).") }
        if ($null -ne ($leaseRegistry.leases | Where-Object { $_.active -and $_.workspace -eq $assignment.workspace } | Select-Object -First 1)) { $issues.Add("Workspace is already leased: $($assignment.workspace).") }
        if (-not [string]::IsNullOrWhiteSpace([string]$assignment.agentId)) {
            $agent = $agentRegistry.agents | Where-Object { $_.active -and $_.agentId -eq $assignment.agentId } | Select-Object -First 1
            if ($null -eq $agent) { $issues.Add("Assigned agent is no longer active: $($assignment.agentId)."); continue }
            if ([string]$agent.owner -cne [string]$assignment.owner) { $issues.Add("Assigned agent owner changed: $($assignment.agentId).") }
            $assignmentCounts[[string]$agent.agentId] = $(if ($assignmentCounts.ContainsKey([string]$agent.agentId)) { [int]$assignmentCounts[[string]$agent.agentId] + 1 } else { 1 })
            if ([int]$assignmentCounts[[string]$agent.agentId] -gt [int]$agent.availableCapacity) { $issues.Add("Assigned agent capacity is insufficient: $($agent.agentId).") }
        }
    }
    [pscustomobject][ordered]@{
        valid = $issues.Count -eq 0
        expired = $expired
        issueCount = $issues.Count
        issues = @($issues)
        planHash = [string]$Plan.planHash
        assignmentCount = @($Plan.assignments).Count
    }
}

$mutating = $Action -in @('create', 'claim', 'prune')
$lockStream = $null
if ($mutating) {
    if (-not (Test-Path -LiteralPath $schedulerRoot)) { New-Item -ItemType Directory -Path $schedulerRoot | Out-Null }
    if (Test-Path -LiteralPath $lockPath -PathType Leaf) {
        if (([DateTime]::UtcNow - [System.IO.File]::GetLastWriteTimeUtc($lockPath)).TotalMinutes -gt 5) { [System.IO.File]::Delete($lockPath) }
    }
    try {
        $lockStream = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    } catch {
        throw 'Schedule plan registry is busy; retry after the current mutation completes.'
    }
}
try {
    if ($Action -eq 'create') {
        $scheduleArguments = @{ TasksPath = $TasksPath; AsOfUtc = $now; Format = 'Json' }
        if ($null -ne $MaxConcurrency) { $scheduleArguments.MaxConcurrency = $MaxConcurrency }
        $schedule = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskSchedule.ps1') @scheduleArguments | ConvertFrom-Json
        $assignments = @($schedule.selectedTasks | ForEach-Object {
            $descriptor = Get-Content -LiteralPath (Join-Path $repositoryRoot "$($_.path)/workspace.json") -Raw | ConvertFrom-Json
            [pscustomobject][ordered]@{
                workspace = [string]$_.path
                taskName = [string]$_.name
                packetFingerprint = [string]$descriptor.currentPacketFingerprint
                lane = $_.lane
                owner = $(if ($null -ne $_.assignedAgent) { [string]$_.assignedAgent.owner } else { '<agent>' })
                agentId = $(if ($null -ne $_.assignedAgent) { [string]$_.assignedAgent.agentId } else { '' })
                agentCapabilities = @(if ($null -ne $_.assignedAgent) { @($_.assignedAgent.capabilities) } else { @() })
                routingScore = $(if ($null -ne $_.assignmentRationale) { $_.assignmentRationale.selectedScore } else { $null })
                requiredCapabilities = @($_.requiredCapabilities)
            }
        })
        $planIdValue = [guid]::NewGuid().ToString('N')
        $plan = [pscustomobject][ordered]@{
            schemaVersion = 1
            planId = $planIdValue
            createdAtUtc = $now.ToString('o')
            expiresAtUtc = $now.AddMinutes($effectiveTtlMinutes).ToString('o')
            policyFingerprint = $policyFingerprint
            tasksPath = $TasksPath
            maxConcurrency = $schedule.maxConcurrency
            routingMode = $schedule.routingMode
            assignments = $assignments
            planHash = ''
        }
        $plan.planHash = Get-Hash (Get-PlanPayload $plan)
        $fileName = "$($now.ToString('yyyyMMddTHHmmssfffZ'))-$planIdValue.json"
        Write-JsonAtomic $planRoot $fileName $plan 'plan'
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'create'; valid = $true; plan = $plan; path = ".artifacts/llm-wiki/scheduler/plans/$fileName" }
    } elseif ($Action -eq 'verify') {
        if ([string]::IsNullOrWhiteSpace($PlanId)) { throw 'verify requires PlanId.' }
        $plan = Read-Plan $PlanId
        $validation = Test-PlanCurrent $plan
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'verify'; valid = $validation.valid; validation = $validation; plan = $plan }
    } elseif ($Action -eq 'claim') {
        if ([string]::IsNullOrWhiteSpace($PlanId)) { throw 'claim requires PlanId.' }
        $plan = Read-Plan $PlanId
        $validation = Test-PlanCurrent $plan
        $claimId = [guid]::NewGuid().ToString('N')
        $started = [System.Collections.Generic.List[object]]::new()
        $state = if ($validation.valid) { 'ready' } else { 'invalid' }
        $claimIssue = ''
        if ($Apply -and $validation.valid) {
            try {
                foreach ($assignment in @($plan.assignments)) {
                    if ([string]$assignment.owner -eq '<agent>') { throw 'Fallback plans require explicit agent registration before apply.' }
                    $dispatchArguments = @{
                        Action = 'start'
                        WorkspacePath = [string]$assignment.workspace
                        Owner = [string]$assignment.owner
                        AgentId = [string]$assignment.agentId
                        RequiredCapability = @($assignment.requiredCapabilities)
                        Lane = $assignment.lane
                        RoutingScore = $assignment.routingScore
                        SchedulePlanId = [string]$plan.planId
                        SchedulePlanHash = [string]$plan.planHash
                        ScheduleClaimId = $claimId
                        AsOfUtc = $now
                        Format = 'Json'
                    }
                    $dispatchResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') @dispatchArguments | ConvertFrom-Json
                    $started.Add($dispatchResult.dispatch)
                    if ($null -ne $SimulateFailureAfter -and $started.Count -ge [int]$SimulateFailureAfter) {
                        throw "Injected batch claim failure after $($started.Count) dispatch(es)."
                    }
                }
                $state = 'claimed'
            } catch {
                $claimIssue = $_.Exception.Message
                foreach ($dispatch in @($started)) {
                    try {
                        & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') fail `
                            -DispatchId $dispatch.dispatchId `
                            -Owner $dispatch.owner `
                            -Result "Compensated batch claim for plan $PlanId after failure: $claimIssue" `
                            -AsOfUtc $now | Out-Null
                    } catch {}
                }
                $state = 'compensated'
            }
        }
        $claimPayload = [ordered]@{
            schemaVersion = 1
            claimId = $claimId
            planId = $PlanId
            planHash = [string]$plan.planHash
            claimedAtUtc = $now.ToString('o')
            apply = [bool]$Apply
            state = $state
            dispatchIds = @($started | ForEach-Object { [string]$_.dispatchId })
            issue = $claimIssue
        }
        $claim = [pscustomobject][ordered]@{}
        foreach ($entry in $claimPayload.GetEnumerator()) { $claim | Add-Member -NotePropertyName $entry.Key -NotePropertyValue $entry.Value }
        $claim | Add-Member -NotePropertyName claimHash -NotePropertyValue (Get-Hash $claimPayload)
        $claimFileName = "$($now.ToString('yyyyMMddTHHmmssfffZ'))-$claimId.json"
        Write-JsonAtomic $claimRoot $claimFileName $claim 'claim'
        $response = [pscustomobject][ordered]@{
            schemaVersion = 1
            action = 'claim'
            valid = $validation.valid -and $state -notin @('invalid', 'compensated')
            apply = [bool]$Apply
            state = $state
            validation = $validation
            claim = $claim
            dispatches = @($started)
            path = ".artifacts/llm-wiki/scheduler/claims/$claimFileName"
        }
    } elseif ($Action -eq 'prune') {
        $files = Get-PlanFiles
        $retainedPlanIds = @{}
        $retainedClaimIds = @{}
        $cycleRoot = Join-Path $schedulerRoot 'cycles'
        foreach ($cycleFile in @(Get-ChildItem -LiteralPath $cycleRoot -File -Filter '*.json' -ErrorAction SilentlyContinue)) {
            try {
                $cycle = Get-Content -LiteralPath $cycleFile.FullName -Raw | ConvertFrom-Json
                if (-not [string]::IsNullOrWhiteSpace([string]$cycle.plan.planId)) { $retainedPlanIds[[string]$cycle.plan.planId] = $true }
                if (-not [string]::IsNullOrWhiteSpace([string]$cycle.claim.claimId)) { $retainedClaimIds[[string]$cycle.claim.claimId] = $true }
            } catch {}
        }
        $candidates = @($files | Sort-Object Name -Descending | Select-Object -Skip ([int]$planPolicy.retentionCount) | Where-Object {
            $planIdFromFile = ($_.BaseName -split '-')[-1]
            -not $retainedPlanIds.ContainsKey($planIdFromFile)
        })
        $claimCandidates = @((Get-ClaimFiles) | Sort-Object Name -Descending | Select-Object -Skip ([int]$planPolicy.retentionCount) | Where-Object {
            $claimIdFromFile = ($_.BaseName -split '-')[-1]
            -not $retainedClaimIds.ContainsKey($claimIdFromFile)
        })
        if ($Apply) { foreach ($file in @($candidates + $claimCandidates)) { [System.IO.File]::Delete($file.FullName) } }
        $candidateCount = $candidates.Count + $claimCandidates.Count
        $response = [pscustomobject][ordered]@{
            schemaVersion = 1
            action = 'prune'
            apply = [bool]$Apply
            retentionCount = [int]$planPolicy.retentionCount
            candidateCount = $candidateCount
            changedCount = $(if ($Apply) { $candidateCount } else { 0 })
            planCandidates = @($candidates.BaseName)
            claimCandidates = @($claimCandidates.BaseName)
        }
    } else {
        $items = @((Get-PlanFiles) | ForEach-Object {
            try {
                $plan = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
                $hashValidation = Test-PlanHash $plan
                [pscustomobject][ordered]@{ planId = [string]$plan.planId; createdAtUtc = [string]$plan.createdAtUtc; expiresAtUtc = [string]$plan.expiresAtUtc; assignmentCount = @($plan.assignments).Count; valid = $hashValidation.valid; issues = @($hashValidation.issues) }
            } catch {
                [pscustomobject][ordered]@{ planId = $_.BaseName; valid = $false; issues = @($_.Exception.Message) }
            }
        })
        $claims = @((Get-ClaimFiles) | ForEach-Object {
            try {
                $claim = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
                $claimValidation = Test-Claim $claim
                [pscustomobject][ordered]@{ claimId = [string]$claim.claimId; planId = [string]$claim.planId; state = [string]$claim.state; apply = [bool]$claim.apply; dispatchCount = @($claim.dispatchIds).Count; valid = $claimValidation.valid; issues = @($claimValidation.issues) }
            } catch {
                [pscustomobject][ordered]@{ claimId = $_.BaseName; valid = $false; issues = @($_.Exception.Message) }
            }
        })
        $response = [pscustomobject][ordered]@{
            schemaVersion = 1
            action = 'list'
            totalCount = $items.Count
            invalidCount = @($items | Where-Object { -not $_.valid }).Count
            claimCount = $claims.Count
            invalidClaimCount = @($claims | Where-Object { -not $_.valid }).Count
            plans = $items
            claims = $claims
        }
    }
} finally {
    if ($null -ne $lockStream) { $lockStream.Dispose() }
    if ($mutating -and (Test-Path -LiteralPath $lockPath)) { [System.IO.File]::Delete($lockPath) }
}

if ($Format -eq 'Json') {
    $response | ConvertTo-Json -Depth 20
} else {
    Write-Host "Schedule plans: action=$Action"
    if ($Action -eq 'list') { Write-Host "Plans: total=$($response.totalCount), invalid=$($response.invalidCount); claims=$($response.claimCount), invalid claims=$($response.invalidClaimCount)" }
    elseif ($Action -eq 'claim') { Write-Host "Claim: state=$($response.state), apply=$($response.apply), dispatches=$(@($response.dispatches).Count)" }
    elseif ($Action -eq 'prune') { Write-Host "Prune: candidates=$($response.candidateCount), changed=$($response.changedCount)" }
    else { Write-Host "Plan: $($response.plan.planId), valid=$($response.valid)" }
}
$invalid = if ($Action -eq 'list') { $response.invalidCount -gt 0 -or $response.invalidClaimCount -gt 0 } elseif ($Action -eq 'prune') { $false } else { -not $response.valid }
if ($FailOnInvalid -and $invalid) { exit 1 }
