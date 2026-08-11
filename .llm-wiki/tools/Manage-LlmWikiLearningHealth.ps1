[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('observe', 'list', 'show', 'verify', 'waive', 'reopen')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [string]$Id,
    [string]$Reason,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$knowledgeRoot = if ([string]::IsNullOrWhiteSpace($env:LLM_WIKI_TEST_KNOWLEDGE_ROOT)) {
    Join-Path $wikiRoot 'knowledge'
} else {
    $candidate = [IO.Path]::GetFullPath($env:LLM_WIKI_TEST_KNOWLEDGE_ROOT)
    $artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki'))
    if (-not $candidate.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'LLM_WIKI_TEST_KNOWLEDGE_ROOT must resolve under .artifacts/llm-wiki.' }
    $candidate
}
$registryPath = Join-Path $knowledgeRoot 'learning-health.json'
$policy = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/workspace-policies.json') -Raw | ConvertFrom-Json
$healthPolicy = $policy.scheduler.learningPromotion.health

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 50 -Compress
    if ($null -eq $json) { $json = 'null' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Read-Registry {
    $value = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    if ($value.schemaVersion -ne 1 -or $null -eq $value.events) { throw 'Unsupported learning-health registry schema.' }
    $value
}
function Write-Registry([object]$Value) {
    [IO.File]::WriteAllText($registryPath, (($Value | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Event.schemaVersion
        sequence = [int]$Event.sequence
        kind = [string]$Event.kind
        candidateId = [string]$Event.candidateId
        createdAtUtc = ([DateTimeOffset]$Event.createdAtUtc).ToUniversalTime().ToString('o')
        previousHash = [string]$Event.previousHash
        observation = $Event.observation
        reason = $Event.reason
    }
}
function Add-Event([object]$Registry, [string]$Kind, [string]$CandidateId, [object]$Observation, [string]$EventReason, [string]$CreatedAtUtc) {
    $event = [pscustomobject][ordered]@{
        schemaVersion = 1
        sequence = @($Registry.events).Count + 1
        kind = $Kind
        candidateId = $CandidateId
        createdAtUtc = $CreatedAtUtc
        previousHash = $(if (@($Registry.events).Count -eq 0) { '' } else { [string]$Registry.events[-1].eventHash })
        observation = $Observation
        reason = $EventReason
        eventHash = ''
    }
    $event.eventHash = Get-Hash (Get-EventPayload $event)
    $Registry.events = @($Registry.events) + $event
    $event
}
function Get-Outcome([object]$Metrics) {
    $baseScore = [Math]::Round(([double]$Metrics.readinessScore + [double]$Metrics.confidenceScore + [double]$Metrics.critiqueScore) / 3, 2)
    $penalty = [Math]::Min([double]$healthPolicy.maximumPenalty, (
        [int]$Metrics.failedRepairCount * [double]$healthPolicy.failedRepairPenalty +
        [int]$Metrics.falseNegativeCount * [double]$healthPolicy.falseNegativePenalty +
        [int]$Metrics.impactDriftCount * [double]$healthPolicy.impactDriftPenalty +
        [int]$Metrics.contextQuarantineCount * [double]$healthPolicy.contextQuarantinePenalty
    ))
    $score = [Math]::Round([Math]::Max(0, $baseScore - $penalty), 2)
    [pscustomobject][ordered]@{
        baseScore = $baseScore
        penalty = $penalty
        score = $score
        degraded = $score -lt [double]$healthPolicy.degradedScoreThreshold -or [string]$Metrics.quality -eq 'poor'
    }
}
function Get-Recommendation([object[]]$Observations, [bool]$Waived) {
    $sampleCount = @($Observations).Count
    $degradedCount = @($Observations | Where-Object { $_.outcome.degraded }).Count
    $degradationPercent = if ($sampleCount -eq 0) { 0 } else { [Math]::Round(100 * $degradedCount / $sampleCount, 2) }
    $meanScore = if ($sampleCount -eq 0) { $null } else { [Math]::Round([double](($Observations.outcome.score | Measure-Object -Average).Average), 2) }
    $verdict = if ($sampleCount -lt [int]$healthPolicy.minimumSamples) {
        'insufficient-data'
    } elseif ($degradationPercent -gt [double]$healthPolicy.maximumDegradationPercent) {
        'rollback'
    } else { 'healthy' }
    [pscustomobject][ordered]@{
        verdict = $verdict
        effectiveVerdict = $(if ($verdict -eq 'rollback' -and $Waived) { 'waived' } else { $verdict })
        sampleCount = $sampleCount
        degradedCount = $degradedCount
        degradationPercent = $degradationPercent
        meanScore = $meanScore
    }
}
function Get-View([object]$Registry) {
    $states = [ordered]@{}
    foreach ($event in @($Registry.events)) {
        $id = [string]$event.candidateId
        if (-not $states.Contains($id)) {
            $states[$id] = [pscustomobject][ordered]@{ id = $id; observations = @(); waived = $false; waiverReason = ''; headEventHash = '' }
        }
        $state = $states[$id]
        if ($event.kind -eq 'observed') {
            $state.observations = @($state.observations) + $event.observation
        } elseif ($event.kind -eq 'waived') {
            $state.waived = $true; $state.waiverReason = [string]$event.reason
        } elseif ($event.kind -eq 'reopened') {
            $state.waived = $false; $state.waiverReason = [string]$event.reason
        }
        $state.headEventHash = [string]$event.eventHash
    }
    @($states.Values | ForEach-Object {
        [pscustomobject][ordered]@{
            id = $_.id
            observations = @($_.observations)
            recommendation = Get-Recommendation @($_.observations) ([bool]$_.waived)
            waived = [bool]$_.waived
            waiverReason = $_.waiverReason
            headEventHash = $_.headEventHash
        }
    } | Sort-Object id)
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    $previous = ''
    $sequence = 0
    $observationKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $states = @{}
    $observationCount = 0
    foreach ($event in @($Registry.events)) {
        $sequence++
        if ([int]$event.sequence -ne $sequence) { $issues.Add("Event sequence is invalid at $sequence.") }
        if ([string]$event.previousHash -cne $previous) { $issues.Add("Event $sequence has invalid previousHash.") }
        if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $issues.Add("Event $sequence has invalid eventHash.") }
        $id = [string]$event.candidateId
        if ([string]::IsNullOrWhiteSpace($id)) { $issues.Add("Event $sequence has no candidateId.") }
        $waived = if ($states.ContainsKey($id)) { [bool]$states[$id] } else { $false }
        if ($event.kind -eq 'observed') {
            $observationCount++
            if ($null -eq $event.observation) {
                $issues.Add("Observation $sequence is absent.")
            } else {
                $key = "$id|$($event.observation.retrospectiveHash)"
                if (-not $observationKeys.Add($key)) { $issues.Add("Duplicate learning-health observation: $key") }
                if ([string]::IsNullOrWhiteSpace([string]$event.observation.workspace) -or [string]::IsNullOrWhiteSpace([string]$event.observation.retrospectiveHash)) { $issues.Add("Observation $sequence provenance is incomplete.") }
                if (@($event.observation.exposureSources).Count -eq 0) { $issues.Add("Observation $sequence has no exposure source.") }
                $expectedOutcome = Get-Outcome $event.observation.metrics
                if ((Get-Hash $event.observation.outcome) -cne (Get-Hash $expectedOutcome)) { $issues.Add("Observation $sequence outcome is invalid.") }
            }
        } elseif ($event.kind -eq 'waived') {
            if ($waived) { $issues.Add("Waiver $sequence is already active.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Waiver $sequence has no reason.") }
            $states[$id] = $true
        } elseif ($event.kind -eq 'reopened') {
            if (-not $waived) { $issues.Add("Reopen $sequence has no active waiver.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Reopen $sequence has no reason.") }
            $states[$id] = $false
        } else {
            $issues.Add("Unknown learning-health event kind '$($event.kind)'.")
        }
        $previous = [string]$event.eventHash
    }
    if ($observationCount -gt [int]$healthPolicy.maximumObservations) { $issues.Add('Learning-health registry exceeds maximumObservations.') }
    @($issues)
}
function New-Observations([string]$Workspace) {
    $normalized = $Workspace.Replace('\', '/').TrimEnd('/')
    if ([IO.Path]::IsPathRooted($Workspace) -or $normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') { throw 'WorkspacePath must identify one task workspace.' }
    $retrospectiveResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') verify -WorkspacePath $normalized -Format Json | ConvertFrom-Json
    if (-not $retrospectiveResult.valid) { throw "A valid sealed retrospective is required: $(@($retrospectiveResult.issues) -join ' ')" }
    $absolute = Join-Path $repositoryRoot $normalized
    $candidateSources = [ordered]@{}
    $contextPath = Join-Path $absolute 'context-bundle.json'
    if (Test-Path -LiteralPath $contextPath -PathType Leaf) {
        $context = Get-Content -LiteralPath $contextPath -Raw | ConvertFrom-Json
        foreach ($memory in @($context.memories | Where-Object { $_.source.kind -eq 'approved-learning' })) {
            $candidateSources[[string]$memory.source.candidateId] = @($candidateSources[[string]$memory.source.candidateId]) + 'context-bundle'
        }
    }
    $costPath = Join-Path $absolute 'verification-cost.json'
    if (Test-Path -LiteralPath $costPath -PathType Leaf) {
        $cost = Get-Content -LiteralPath $costPath -Raw | ConvertFrom-Json
        foreach ($learning in @($cost.appliedLearningSnapshot | Where-Object source -eq 'applied')) {
            $candidateSources[[string]$learning.id] = @($candidateSources[[string]$learning.id]) + 'verification-cost'
        }
    }
    $retrospective = $retrospectiveResult.retrospective
    $metrics = [pscustomobject][ordered]@{
        quality = [string]$retrospective.outcome.quality
        readinessScore = [double]$retrospective.outcome.readinessScore
        confidenceScore = [double]$retrospective.outcome.confidenceScore
        critiqueScore = [double]$retrospective.outcome.critiqueScore
        failedRepairCount = [int]$retrospective.outcome.repair.failedAttempts
        falseNegativeCount = [int]$retrospective.outcome.prediction.falseNegativeCount
        impactDriftCount = [int]$retrospective.outcome.impactDriftCount
        contextQuarantineCount = [int]$retrospective.outcome.quarantinedContextSourceCount
    }
    @($candidateSources.Keys | Sort-Object | ForEach-Object {
        [pscustomobject][ordered]@{
            candidateId = [string]$_
            workspace = $normalized
            retrospectiveHash = [string]$retrospective.retrospectiveHash
            completionFingerprint = [string]$retrospective.completionFingerprint
            exposureSources = @($candidateSources[$_] | Sort-Object -Unique)
            metrics = $metrics
            outcome = Get-Outcome $metrics
        }
    })
}

$registry = Read-Registry
$now = $AsOfUtc.ToUniversalTime().ToString('o')
$result = $null
if ($Action -eq 'observe') {
    if ([string]::IsNullOrWhiteSpace($WorkspacePath)) { throw 'WorkspacePath is required.' }
    $added = [Collections.Generic.List[string]]::new()
    $existingKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($view in @(Get-View $registry)) {
        foreach ($observation in @($view.observations)) { [void]$existingKeys.Add("$($view.id)|$($observation.retrospectiveHash)") }
    }
    foreach ($observation in @(New-Observations $WorkspacePath)) {
        $key = "$($observation.candidateId)|$($observation.retrospectiveHash)"
        if (-not $existingKeys.Add($key)) { continue }
        $event = Add-Event $registry 'observed' $observation.candidateId $observation '' $now
        $added.Add([string]$event.eventHash)
    }
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0 -and $added.Count -gt 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $issues.Count -eq 0; addedCount = $added.Count; eventHashes = @($added); health = @(Get-View $registry); issues = $issues }
} elseif ($Action -in @('waive', 'reopen')) {
    if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($Reason)) { throw 'Id and Reason are required.' }
    $health = Get-View $registry | Where-Object id -eq $Id
    if ($null -eq $health) { throw "Learning health not found: $Id" }
    if ($Action -eq 'waive' -and $health.recommendation.verdict -ne 'rollback') { throw 'Only a rollback recommendation can be waived.' }
    if ($Action -eq 'waive' -and $health.waived) { throw 'Rollback recommendation is already waived.' }
    if ($Action -eq 'reopen' -and -not $health.waived) { throw 'Learning health has no active waiver.' }
    $event = Add-Event $registry $(if ($Action -eq 'waive') { 'waived' } else { 'reopened' }) $Id $null $Reason $now
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $issues.Count -eq 0; health = (Get-View $registry | Where-Object id -eq $Id); eventHash = $event.eventHash; issues = $issues }
} else {
    $issues = @(Test-Registry $registry)
    $health = @(Get-View $registry)
    if ($Action -eq 'show') {
        if ([string]::IsNullOrWhiteSpace($Id)) { throw 'Id is required.' }
        $health = @($health | Where-Object id -eq $Id)
        if ($health.Count -eq 0) { throw "Learning health not found: $Id" }
    }
    $learning = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') list -Format Json | ConvertFrom-Json
    $appliedIds = @($learning.candidates | Where-Object materialization -eq 'applied' | ForEach-Object id)
    foreach ($item in $health) { $item | Add-Member -NotePropertyName currentlyApplied -NotePropertyValue (@($appliedIds) -contains $item.id) }
    $result = [pscustomobject][ordered]@{
        action = $Action
        valid = $issues.Count -eq 0
        totalCount = @(Get-View $registry).Count
        observationCount = @($registry.events | Where-Object kind -eq 'observed').Count
        rollbackRecommendationCount = @($health | Where-Object { $_.currentlyApplied -and $_.recommendation.effectiveVerdict -eq 'rollback' }).Count
        health = $health
        registryFingerprint = Get-Hash @($registry.events)
        issues = $issues
    }
}
if ($FailOnInvalid -and -not $result.valid) { throw "Learning-health registry is invalid: $(@($result.issues) -join ' ')" }
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 50 } else {
    Write-Host "Learning health: action=$Action, valid=$($result.valid)"
    foreach ($item in @($result.health | Where-Object { $null -ne $_ })) {
        Write-Host " - $($item.id): verdict=$($item.recommendation.effectiveVerdict), samples=$($item.recommendation.sampleCount), degraded=$($item.recommendation.degradationPercent)%"
    }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
