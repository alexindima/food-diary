[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('observe', 'candidates', 'list', 'show', 'approve', 'reject', 'supersede', 'plan', 'apply', 'rollback', 'verify')]
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
. (Join-Path $PSScriptRoot 'Format-LlmWikiLearningResults.ps1')
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
$registryPath = Join-Path $knowledgeRoot 'learning-promotions.json'
$policy = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/workspace-policies.json') -Raw | ConvertFrom-Json
$promotionPolicy = $policy.scheduler.learningPromotion

function Get-RawHash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    if ($null -eq $json) { $json = 'null' }
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-StableHash([object]$Value) {
    # Hash the same JSON-compatible shape that a later process will read.
    # A recursive PowerShell normalizer is subtly affected by pipeline
    # enumeration (especially empty and singleton arrays), while a JSON
    # round-trip gives us one stable persistence boundary for every action.
    $json = ConvertTo-Json -InputObject $Value -Depth 50 -Compress
    if ($null -eq $json) { $json = 'null' }
    Get-RawHash ($json | ConvertFrom-Json)
}
function Get-Hash([object]$Value) { Get-RawHash $Value }
function Read-Registry {
    $registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    if ($registry.schemaVersion -ne 1 -or $null -eq $registry.events) { throw 'Unsupported learning-promotion registry schema.' }
    $registry
}
function Write-Registry([object]$Registry) {
    # Seal the exact JSON representation that subsequent processes will read.
    # PowerShell's JSON round-trip can change nested numeric/date CLR types; hashing
    # the pre-serialization objects would therefore produce a chain that fails on
    # the next invocation even though the persisted data is semantically unchanged.
    $temporaryRegistryPath = "$registryPath.$PID.writing"
    try {
        $persisted = $Registry
        $stable = $false
        foreach ($attempt in 1..3) {
            $persisted = ($persisted | ConvertTo-Json -Depth 50 -Compress) | ConvertFrom-Json
            $persisted | Add-Member -NotePropertyName hashSchemaVersion -NotePropertyValue 2 -Force
            Update-LegacyDerivedSnapshots $persisted
            $previousHash = ''
            foreach ($event in @($persisted.events)) {
                $event.previousHash = $previousHash
                $event.eventHash = Get-StableHash (Get-EventPayload $event)
                $previousHash = [string]$event.eventHash
            }
            [IO.File]::WriteAllText($temporaryRegistryPath, (($persisted | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
            $roundTripped = Get-Content -LiteralPath $temporaryRegistryPath -Raw | ConvertFrom-Json
            $issues = @(Test-Registry $roundTripped)
            if ($issues.Count -eq 0) {
                $persisted = $roundTripped
                $stable = $true
                break
            }
            $persisted = $roundTripped
        }
        if (-not $stable) { throw "Refusing to persist an unstable learning-promotion registry: $($issues -join ' ')" }
        Move-Item -LiteralPath $temporaryRegistryPath -Destination $registryPath -Force
    } finally {
        Remove-Item -LiteralPath $temporaryRegistryPath -Force -ErrorAction SilentlyContinue
    }
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Event.schemaVersion
        sequence = [int]$Event.sequence
        kind = [string]$Event.kind
        id = [string]$Event.id
        createdAtUtc = ([DateTimeOffset]$Event.createdAtUtc).ToUniversalTime().ToString('o')
        previousHash = [string]$Event.previousHash
        observation = $Event.observation
        decision = $Event.decision
        targetId = $Event.targetId
        reason = $Event.reason
    }
}
function Get-LegacyEventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = $Event.schemaVersion
        sequence = $Event.sequence
        kind = $Event.kind
        id = $Event.id
        createdAtUtc = $Event.createdAtUtc
        previousHash = $Event.previousHash
        observation = $Event.observation
        decision = $Event.decision
        targetId = $Event.targetId
        reason = $Event.reason
    }
}
function Add-Event([object]$Registry, [string]$Kind, [string]$CandidateId, [object]$Observation, [object]$Decision, [string]$TargetId, [string]$EventReason, [string]$CreatedAtUtc) {
    $existingEvents = @($Registry.events)
    if ($existingEvents.Count -gt 0 -and (-not $existingEvents[-1].PSObject.Properties['eventHash'] -or [string]::IsNullOrWhiteSpace([string]$existingEvents[-1].eventHash))) {
        $null = Initialize-LegacyHashChain $Registry
        $existingEvents = @($Registry.events)
    }
    $previousHash = if ($existingEvents.Count -eq 0) { '' } else { [string]$existingEvents[-1].eventHash }
    $event = [pscustomobject][ordered]@{
        schemaVersion = 1
        sequence = @($Registry.events).Count + 1
        kind = $Kind
        id = $CandidateId
        createdAtUtc = $CreatedAtUtc
        previousHash = $previousHash
        observation = $Observation
        decision = $Decision
        targetId = $TargetId
        reason = $EventReason
        eventHash = ''
    }
    $event.eventHash = Get-StableHash (Get-EventPayload $event)
    $Registry.events = @($Registry.events) + $event
    # Property assignments can leak values into a PowerShell function's output
    # pipeline. Return exactly the event object so callers never append a hash
    # string or a partial event array to the registry.
    Write-Output -NoEnumerate $event
}
function Get-CandidateId([object]$Candidate) {
    $candidateTags = if ($null -ne $Candidate.PSObject.Properties['suggestedTags']) {
        @($Candidate.suggestedTags)
    } else {
        @($Candidate.tags)
    }
    $identity = [pscustomobject][ordered]@{
        type = [string]$Candidate.type
        statement = ([string]$Candidate.statement).Trim().ToLowerInvariant()
        tags = @($candidateTags | ForEach-Object { ([string]$_).ToLowerInvariant() } | Sort-Object -Unique)
    }
    "learning-$((Get-Hash $identity).Substring(0, 20))"
}
function Get-View([object]$Registry) {
    $byId = [ordered]@{}
    foreach ($event in @($Registry.events)) {
        $candidateId = if ($event.kind -in @('superseded', 'rolled-back')) { [string]$event.targetId } else { [string]$event.id }
        if (-not $byId.Contains($candidateId)) {
            $byId[$candidateId] = [pscustomobject][ordered]@{
                id = $candidateId
                type = ''
                target = ''
                statement = ''
                rationale = ''
                tags = @()
                observations = @()
                decision = 'pending'
                decisionReason = ''
                decidedAtUtc = $null
                superseded = $false
                materialization = 'not-applied'
                application = $null
            }
        }
        $view = $byId[$candidateId]
        if ($event.kind -eq 'observed') {
            $view.type = [string]$event.observation.type
            $view.target = [string]$event.observation.target
            $view.statement = [string]$event.observation.statement
            $view.rationale = [string]$event.observation.rationale
            $view.tags = @($event.observation.tags)
            $view.observations = @($view.observations) + $event.observation
        } elseif ($event.kind -in @('approved', 'rejected')) {
            $view.decision = if ($event.kind -eq 'approved') { 'approved' } else { 'rejected' }
            $view.decisionReason = [string]$event.reason
            $view.decidedAtUtc = [string]$event.createdAtUtc
        } elseif ($event.kind -eq 'superseded') {
            $view.superseded = $true
            $view.decision = 'superseded'
            $view.decisionReason = [string]$event.reason
            $view.decidedAtUtc = [string]$event.createdAtUtc
        } elseif ($event.kind -eq 'applied') {
            $view.materialization = 'applied'
            $view.application = $event.decision
        } elseif ($event.kind -eq 'rolled-back') {
            $view.materialization = 'rolled-back'
            $view.application = $event.decision
        }
    }
    @($byId.Values | ForEach-Object {
        $item = $_
        $tasks = @($item.observations | ForEach-Object { [string]$_.workspace } | Where-Object { $_ } | Sort-Object -Unique)
        $evidence = @($item.observations | ForEach-Object { @($_.evidence) } | Where-Object { $_ } | Sort-Object -Unique | Select-Object -First ([int]$promotionPolicy.maximumEvidenceItems))
        $scores = @($item.observations | ForEach-Object { [double]$_.score })
        $average = if ($scores.Count -eq 0) { 0 } else { [Math]::Round([double](($scores | Measure-Object -Average).Average), 2) }
        [pscustomobject][ordered]@{
            id = $item.id
            type = $item.type
            target = $item.target
            statement = $item.statement
            rationale = $item.rationale
            tags = @($item.tags)
            observationCount = @($item.observations).Count
            distinctTaskCount = $tasks.Count
            workspaces = $tasks
            observations = @($item.observations)
            averageScore = $average
            evidence = $evidence
            eligible = (
                -not $item.superseded -and
                $item.decision -eq 'pending' -and
                $tasks.Count -ge [int]$promotionPolicy.minimumDistinctTasks -and
                $average -ge [double]$promotionPolicy.minimumObservationScore
            )
            decision = $item.decision
            decisionReason = $item.decisionReason
            decidedAtUtc = $item.decidedAtUtc
            materialization = $item.materialization
            application = $item.application
        }
    } | Sort-Object @{ Expression = 'eligible'; Descending = $true }, @{ Expression = 'averageScore'; Descending = $true }, id)
}
function Get-Application([object]$Candidate) {
    $observations = @($Candidate.observations)
    $scopePaths = @($observations | ForEach-Object { @($_.changedPaths) } | Where-Object { $_ } | Sort-Object -Unique | ForEach-Object {
        '^' + [regex]::Escape([string]$_) + '$'
    })
    $subjectIds = @($observations | ForEach-Object { @($_.subjectIds) } | Where-Object { $_ } | Sort-Object -Unique)
    $recommendedValues = @($observations | ForEach-Object {
        if ($null -ne $_.data -and [double]$_.data.recommendedSeconds -gt 0) { [double]$_.data.recommendedSeconds }
    })
    [pscustomobject][ordered]@{
        target = [string]$Candidate.target
        statement = [string]$Candidate.statement
        rationale = [string]$Candidate.rationale
        scopePaths = $scopePaths
        subjectIds = $subjectIds
        recommendedSeconds = $(if ($recommendedValues.Count -eq 0) { $null } else { [Math]::Round([double](($recommendedValues | Measure-Object -Average).Average), 2) })
        observationCount = [int]$Candidate.observationCount
        distinctTaskCount = [int]$Candidate.distinctTaskCount
        averageScore = [double]$Candidate.averageScore
        evidence = @($Candidate.evidence)
        evidenceHash = Get-StableHash @($Candidate.evidence)
    }
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    $previous = ''
    $observationKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $known = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $decided = @{}
    $observationsByCandidate = @{}
    $sequence = 0
    foreach ($event in @($Registry.events)) {
        $sequence++
        if ([int]$event.sequence -ne $sequence) { $issues.Add("Event sequence is invalid at $sequence.") }
        if ([string]$event.previousHash -cne $previous) { $issues.Add("Event $sequence has invalid previousHash.") }
        if ([string]$event.eventHash -cne (Get-StableHash (Get-EventPayload $event))) { $issues.Add("Event $sequence has invalid eventHash.") }
        if ($event.kind -eq 'observed') {
            if ([string]$event.id -ne (Get-CandidateId $event.observation)) { $issues.Add("Observation $sequence has an invalid candidate id.") }
            $known.Add([string]$event.id) | Out-Null
            if (-not $observationsByCandidate.ContainsKey([string]$event.id)) {
                $observationsByCandidate[[string]$event.id] = [Collections.Generic.List[object]]::new()
            }
            $observationsByCandidate[[string]$event.id].Add($event.observation)
            $observationKey = "$($event.observation.retrospectiveHash):$($event.observation.sourceCandidateId)"
            if (-not $observationKeys.Add($observationKey)) { $issues.Add("Duplicate observation: $observationKey") }
            if ([string]::IsNullOrWhiteSpace([string]$event.observation.workspace)) { $issues.Add("Observation $sequence has no workspace.") }
            if ([double]$event.observation.score -lt 1 -or [double]$event.observation.score -gt 100) { $issues.Add("Observation $sequence has an invalid score.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.observation.statement)) { $issues.Add("Observation $sequence has no statement.") }
            if ($event.observation.target -notin @('durable-memory', 'verification-calibration')) { $issues.Add("Observation $sequence has an invalid target.") }
        } elseif ($event.kind -in @('approved', 'rejected')) {
            if (-not $known.Contains([string]$event.id)) { $issues.Add("Decision $sequence targets an unknown candidate.") }
            if ($decided.ContainsKey([string]$event.id)) { $issues.Add("Candidate '$($event.id)' has multiple active decisions.") }
            $decided[[string]$event.id] = $event.kind
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Decision $sequence has no reason.") }
            $observations = @($observationsByCandidate[[string]$event.id])
            $distinctTaskCount = @($observations | ForEach-Object { [string]$_.workspace } | Where-Object { $_ } | Sort-Object -Unique).Count
            $averageScore = if ($observations.Count -eq 0) { 0 } else {
                [Math]::Round([double](($observations.score | Measure-Object -Average).Average), 2)
            }
            $evidence = @($observations.evidence | Where-Object { $_ } | Sort-Object -Unique | Select-Object -First ([int]$promotionPolicy.maximumEvidenceItems))
            $expectedTarget = if ($observations.Count -eq 0) { '' } else { [string]$observations[-1].target }
            if ([string]$event.decision.target -cne $expectedTarget) { $issues.Add("Decision $sequence has an invalid target snapshot.") }
            if ([int]$event.decision.distinctTaskCount -ne $distinctTaskCount) { $issues.Add("Decision $sequence has an invalid task-count snapshot.") }
            if ([double]$event.decision.averageScore -ne $averageScore) { $issues.Add("Decision $sequence has an invalid score snapshot.") }
            if ([string]$event.decision.evidenceHash -cne (Get-StableHash $evidence)) { $issues.Add("Decision $sequence has an invalid evidence snapshot.") }
            if (
                $event.kind -eq 'approved' -and (
                    $distinctTaskCount -lt [int]$promotionPolicy.minimumDistinctTasks -or
                    $averageScore -lt [double]$promotionPolicy.minimumObservationScore
                )
            ) {
                $issues.Add("Approval $sequence did not have sufficient independent evidence.")
            }
        } elseif ($event.kind -eq 'superseded') {
            if (-not $known.Contains([string]$event.targetId)) { $issues.Add("Supersedence $sequence targets an unknown candidate.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Supersedence $sequence has no reason.") }
        } elseif ($event.kind -eq 'applied') {
            if (-not $known.Contains([string]$event.id)) { $issues.Add("Application $sequence targets an unknown candidate.") }
            $candidate = Get-View ([pscustomobject]@{ schemaVersion = 1; events = @($Registry.events | Select-Object -First ($sequence - 1)) }) | Where-Object id -eq $event.id
            if ($null -eq $candidate -or $candidate.decision -ne 'approved' -or $candidate.materialization -eq 'applied') {
                $issues.Add("Application $sequence did not target an approved, unapplied candidate.")
            } else {
                $expectedApplication = Get-Application $candidate
                if ((Get-StableHash $event.decision) -cne (Get-StableHash $expectedApplication)) { $issues.Add("Application $sequence has an invalid materialization snapshot.") }
                if ($event.decision.target -eq 'durable-memory' -and @($event.decision.scopePaths).Count -eq 0) { $issues.Add("Application $sequence has no durable-memory scope.") }
                if ($event.decision.target -eq 'verification-calibration' -and (@($event.decision.subjectIds).Count -eq 0 -or [double]$event.decision.recommendedSeconds -le 0)) {
                    $issues.Add("Application $sequence has no usable verification calibration.")
                }
            }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Application $sequence has no reason.") }
        } elseif ($event.kind -eq 'rolled-back') {
            if (-not $known.Contains([string]$event.targetId)) { $issues.Add("Rollback $sequence targets an unknown candidate.") }
            $candidate = Get-View ([pscustomobject]@{ schemaVersion = 1; events = @($Registry.events | Select-Object -First ($sequence - 1)) }) | Where-Object id -eq $event.targetId
            if ($null -eq $candidate -or $candidate.materialization -ne 'applied') { $issues.Add("Rollback $sequence did not target an applied candidate.") }
            if ($null -eq $event.decision -or (Get-StableHash $event.decision) -cne (Get-StableHash $candidate.application)) { $issues.Add("Rollback $sequence has an invalid application snapshot.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Rollback $sequence has no reason.") }
        } else {
            $issues.Add("Unknown learning-promotion event kind '$($event.kind)'.")
        }
        $previous = [string]$event.eventHash
    }
    if (@(Get-View $Registry).Count -gt [int]$promotionPolicy.maximumCandidates) { $issues.Add('Learning-promotion registry exceeds maximumCandidates.') }
    if (@(Get-View $Registry | Where-Object materialization -eq 'applied').Count -gt [int]$promotionPolicy.materialization.maximumAppliedLearnings) {
        $issues.Add('Learning-promotion registry exceeds maximumAppliedLearnings.')
    }
    @($issues)
}

function Initialize-LegacyHashChain([object]$Registry) {
    $events = @($Registry.events)
    if ($events.Count -eq 0) { return $false }
    $hashedCount = @($events | Where-Object {
        $_.PSObject.Properties['eventHash'] -and -not [string]::IsNullOrWhiteSpace([string]$_.eventHash)
    }).Count
    if ($hashedCount -eq $events.Count) { return $false }
    if ($hashedCount -ne 0) {
        throw "Learning-promotion registry has a partially hashed event chain ($hashedCount/$($events.Count)); restore it from a trusted copy instead of migrating it."
    }

    $previousHash = ''
    foreach ($event in $events) {
        if ($event.PSObject.Properties['previousHash']) {
            $event.previousHash = $previousHash
        } else {
            $event | Add-Member -NotePropertyName previousHash -NotePropertyValue $previousHash
        }
        $eventHash = Get-StableHash (Get-EventPayload $event)
        if ($event.PSObject.Properties['eventHash']) {
            $event.eventHash = $eventHash
        } else {
            $event | Add-Member -NotePropertyName eventHash -NotePropertyValue $eventHash
        }
        $previousHash = $eventHash
    }
    $true
}

function Test-LegacyHashChain([object]$Registry) {
    $previousHash = ''
    foreach ($event in @($Registry.events)) {
        if ([string]$event.previousHash -cne $previousHash) { return $false }
        if ([string]$event.eventHash -cne (Get-RawHash (Get-LegacyEventPayload $event))) { return $false }
        $previousHash = [string]$event.eventHash
    }
    $true
}

function Update-LegacyDerivedSnapshots([object]$Registry) {
    $events = @($Registry.events)
    for ($index = 0; $index -lt $events.Count; $index++) {
        $event = $events[$index]
        $priorRegistry = [pscustomobject]@{ schemaVersion = 1; events = @($events | Select-Object -First $index) }
        if ($event.kind -in @('approved', 'rejected')) {
            $candidate = Get-View $priorRegistry | Where-Object id -eq $event.id | Select-Object -First 1
            if ($null -eq $candidate) { throw "Legacy decision $($index + 1) targets an unknown candidate." }
            $event.decision = [pscustomobject][ordered]@{
                target = [string]$candidate.target
                distinctTaskCount = [int]$candidate.distinctTaskCount
                averageScore = [double]$candidate.averageScore
                evidenceHash = Get-StableHash @($candidate.evidence)
            }
        } elseif ($event.kind -eq 'applied') {
            $candidate = Get-View $priorRegistry | Where-Object id -eq $event.id | Select-Object -First 1
            if ($null -eq $candidate -or $candidate.decision -ne 'approved') { throw "Legacy application $($index + 1) does not target an approved candidate." }
            $event.decision = Get-Application $candidate
        } elseif ($event.kind -eq 'rolled-back') {
            $candidate = Get-View $priorRegistry | Where-Object id -eq $event.targetId | Select-Object -First 1
            if ($null -eq $candidate -or $candidate.materialization -ne 'applied') { throw "Legacy rollback $($index + 1) does not target an applied candidate." }
            $event.decision = $candidate.application
        }
    }
}

function Convert-LegacyRegistry([object]$Registry) {
    $events = @($Registry.events)
    if ($events.Count -eq 0) {
        $Registry | Add-Member -NotePropertyName hashSchemaVersion -NotePropertyValue 2 -Force
        return $true
    }
    $hashedCount = @($events | Where-Object { $_.PSObject.Properties['eventHash'] -and -not [string]::IsNullOrWhiteSpace([string]$_.eventHash) }).Count
    if ($hashedCount -eq 0) {
        $null = Initialize-LegacyHashChain $Registry
    } elseif ($hashedCount -ne $events.Count) {
        throw "Learning-promotion registry has a partially hashed event chain ($hashedCount/$($events.Count)); restore it from a trusted copy instead of migrating it."
    } elseif (-not (Test-LegacyHashChain $Registry)) {
        throw 'Learning-promotion legacy registry hash chain is invalid; restore it from a trusted copy instead of migrating it.'
    }
    Update-LegacyDerivedSnapshots $Registry
    $previousHash = ''
    foreach ($event in @($Registry.events)) {
        $event.previousHash = $previousHash
        $event.eventHash = Get-StableHash (Get-EventPayload $event)
        $previousHash = [string]$event.eventHash
    }
    $Registry | Add-Member -NotePropertyName hashSchemaVersion -NotePropertyValue 2 -Force
    $true
}

$registry = Read-Registry
$legacyHashChainMigrated = if (-not $registry.PSObject.Properties['hashSchemaVersion']) {
    Convert-LegacyRegistry $registry
} elseif ([int]$registry.hashSchemaVersion -eq 2) {
    $false
} else {
    throw "Unsupported learning-promotion hash schema version: $($registry.hashSchemaVersion)"
}
$registryIssues = @(Test-Registry $registry)
if ($registryIssues.Count -gt 0 -and ($FailOnInvalid -or $Action -notin @('verify', 'list', 'show', 'candidates'))) {
    throw "Learning-promotion registry is invalid: $($registryIssues -join ' ')"
}
if ($legacyHashChainMigrated) {
    $temporaryRegistryPath = "$registryPath.$PID.migrating"
    try {
        [IO.File]::WriteAllText($temporaryRegistryPath, (($registry | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryRegistryPath -Destination $registryPath -Force
    } finally {
        Remove-Item -LiteralPath $temporaryRegistryPath -Force -ErrorAction SilentlyContinue
    }
}
$result = $null
$now = $AsOfUtc.ToUniversalTime().ToString('o')
if ($Action -eq 'observe') {
    if ([string]::IsNullOrWhiteSpace($WorkspacePath)) { throw 'WorkspacePath is required for observe.' }
    $normalizedWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
    if ([IO.Path]::IsPathRooted($WorkspacePath) -or $normalizedWorkspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') { throw 'WorkspacePath must identify one task workspace.' }
    $retrospective = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') verify -WorkspacePath $normalizedWorkspace -FailOnInvalid -Format Json | ConvertFrom-Json
    $packet = Get-Content -LiteralPath (Join-Path $repositoryRoot "$normalizedWorkspace/change-packet.json") -Raw | ConvertFrom-Json
    $evidenceBundle = Get-Content -LiteralPath (Join-Path $repositoryRoot "$normalizedWorkspace/evidence.json") -Raw | ConvertFrom-Json
    $existingKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($event in @($registry.events | Where-Object kind -eq 'observed')) {
        $existingKeys.Add("$($event.observation.retrospectiveHash):$($event.observation.sourceCandidateId)") | Out-Null
    }
    $added = [Collections.Generic.List[object]]::new()
    foreach ($candidate in @($retrospective.retrospective.learningCandidates | Where-Object eligible)) {
        $observationKey = "$($retrospective.retrospective.retrospectiveHash):$($candidate.id)"
        if ($existingKeys.Contains($observationKey)) { continue }
        $observation = [pscustomobject][ordered]@{
            workspace = $normalizedWorkspace
            retrospectiveHash = [string]$retrospective.retrospective.retrospectiveHash
            completionFingerprint = [string]$retrospective.retrospective.completionFingerprint
            packetFingerprint = [string]$retrospective.retrospective.packetFingerprint
            sourceCandidateId = [string]$candidate.id
            type = [string]$candidate.type
            target = [string]$promotionPolicy.targets.([string]$candidate.type)
            statement = [string]$candidate.statement
            rationale = [string]$candidate.rationale
            score = [int]$candidate.score
            evidence = @($candidate.evidence)
            tags = @($candidate.suggestedTags)
            data = $candidate.data
            changedPaths = @($packet.diff.changedPaths)
            subjectIds = @($candidate.evidence | Where-Object { $_ -in @($evidenceBundle.checks | ForEach-Object { [string]$_.id }) } | Sort-Object -Unique)
        }
        $event = Add-Event $registry 'observed' (Get-CandidateId $candidate) $observation $null '' '' $now
        $added.Add($event)
        $existingKeys.Add($observationKey) | Out-Null
    }
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0 -and $added.Count -gt 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{
        action = 'observe'; valid = $issues.Count -eq 0; addedCount = $added.Count
        observationEventHashes = @($added | ForEach-Object { [string]$_.eventHash }); candidates = @(Get-View $registry); issues = $issues
    }
} elseif ($Action -in @('approve', 'reject')) {
    if ([string]::IsNullOrWhiteSpace($Id)) { throw 'Id is required.' }
    if ([string]::IsNullOrWhiteSpace($Reason)) { throw 'A review reason is required.' }
    $candidate = Get-View $registry | Where-Object id -eq $Id
    if ($null -eq $candidate) { throw "Learning candidate not found: $Id" }
    if ($candidate.decision -ne 'pending') { throw "Learning candidate is already $($candidate.decision): $Id" }
    if ($Action -eq 'approve' -and -not $candidate.eligible) { throw "Learning candidate has insufficient independent task evidence: $Id" }
    $decision = [pscustomobject][ordered]@{
        target = [string]$candidate.target
        distinctTaskCount = [int]$candidate.distinctTaskCount
        averageScore = [double]$candidate.averageScore
        evidenceHash = Get-StableHash @($candidate.evidence)
    }
    $event = Add-Event $registry $(if ($Action -eq 'approve') { 'approved' } else { 'rejected' }) $Id $null $decision '' $Reason $now
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $issues.Count -eq 0; candidate = (Get-View $registry | Where-Object id -eq $Id); eventHash = $event.eventHash; issues = $issues }
} elseif ($Action -eq 'supersede') {
    if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($Reason)) { throw 'Id and Reason are required.' }
    $candidate = Get-View $registry | Where-Object id -eq $Id
    if ($null -eq $candidate -or $candidate.decision -eq 'superseded') { throw "Active learning candidate not found: $Id" }
    $event = Add-Event $registry 'superseded' '' $null $null $Id $Reason $now
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{ action = 'supersede'; valid = $issues.Count -eq 0; candidate = (Get-View $registry | Where-Object id -eq $Id); eventHash = $event.eventHash; issues = $issues }
} elseif ($Action -in @('plan', 'apply')) {
    if ([string]::IsNullOrWhiteSpace($Id)) { throw 'Id is required.' }
    $candidateInternal = Get-View $registry | Where-Object id -eq $Id
    if ($null -eq $candidateInternal) { throw "Learning candidate not found: $Id" }
    if ($candidateInternal.decision -ne 'approved') { throw "Learning candidate must be approved before materialization: $Id" }
    if ($candidateInternal.materialization -eq 'applied') { throw "Learning candidate is already applied: $Id" }
    $application = Get-Application $candidateInternal
    if ($application.target -eq 'durable-memory' -and @($application.scopePaths).Count -eq 0) { throw 'Durable-memory materialization requires observed changed paths.' }
    if ($application.target -eq 'verification-calibration' -and (@($application.subjectIds).Count -eq 0 -or [double]$application.recommendedSeconds -le 0)) {
        throw 'Verification-calibration materialization requires a check id and positive recommended duration.'
    }
    if (
        $application.target -eq 'verification-calibration' -and (
            [double]$application.recommendedSeconds -lt [double]$promotionPolicy.materialization.minimumCalibrationSeconds -or
            [double]$application.recommendedSeconds -gt [double]$promotionPolicy.materialization.maximumCalibrationSeconds
        )
    ) {
        throw 'Verification-calibration recommendation is outside the governed duration bounds.'
    }
    if ($Action -eq 'plan') {
        $result = [pscustomobject][ordered]@{ action = 'plan'; valid = $true; candidateId = $Id; application = $application; issues = @() }
    } else {
        if ([string]::IsNullOrWhiteSpace($Reason)) { throw 'A materialization reason is required.' }
        if ([bool]$promotionPolicy.materialization.requireSuccessfulExperiment) {
            $experiment = $null
            try {
                $experimentResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningExperiment.ps1') show -Id $Id -Format Json | ConvertFrom-Json
                $experiment = @($experimentResult.experiments)[0]
            } catch { $experiment = $null }
            if ($null -eq $experiment -or -not [bool]$experiment.successful) {
                throw "Learning candidate requires a successful shadow or canary experiment before materialization: $Id"
            }
        }
        $event = Add-Event $registry 'applied' $Id $null $application '' $Reason $now
        $issues = @(Test-Registry $registry)
        if ($issues.Count -eq 0) { Write-Registry $registry }
        $result = [pscustomobject][ordered]@{ action = 'apply'; valid = $issues.Count -eq 0; candidate = (Get-View $registry | Where-Object id -eq $Id); application = $application; eventHash = $event.eventHash; issues = $issues }
    }
} elseif ($Action -eq 'rollback') {
    if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($Reason)) { throw 'Id and Reason are required.' }
    $candidate = Get-View $registry | Where-Object id -eq $Id
    if ($null -eq $candidate -or $candidate.materialization -ne 'applied') { throw "Applied learning candidate not found: $Id" }
    $event = Add-Event $registry 'rolled-back' '' $null $candidate.application $Id $Reason $now
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{ action = 'rollback'; valid = $issues.Count -eq 0; candidate = (Get-View $registry | Where-Object id -eq $Id); eventHash = $event.eventHash; issues = $issues }
} else {
    $issues = @(Test-Registry $registry)
    $view = @(Get-View $registry)
    if ($Action -eq 'show') {
        if ([string]::IsNullOrWhiteSpace($Id)) { throw 'Id is required for show.' }
        $view = @($view | Where-Object id -eq $Id)
        if ($view.Count -eq 0) { throw "Learning candidate not found: $Id" }
    } elseif ($Action -eq 'candidates') {
        $view = @($view | Where-Object { $_.decision -eq 'pending' })
        if (-not [string]::IsNullOrWhiteSpace($WorkspacePath)) {
            $normalizedWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
            $view = @($view | Where-Object { @($_.workspaces) -contains $normalizedWorkspace })
        }
    }
    $result = [pscustomobject][ordered]@{
        action = $Action; valid = $issues.Count -eq 0; totalCount = @(Get-View $registry).Count
        eligibleCount = @($view | Where-Object eligible).Count; approvedCount = @($view | Where-Object decision -eq 'approved').Count
        rejectedCount = @($view | Where-Object decision -eq 'rejected').Count
        appliedCount = @($view | Where-Object materialization -eq 'applied').Count
        rolledBackCount = @($view | Where-Object materialization -eq 'rolled-back').Count
        candidates = $view
        registryFingerprint = Get-Hash @($registry.events); issues = $issues
    }
}
if ($FailOnInvalid -and -not $result.valid) { throw "Learning-promotion registry is invalid: $(@($result.issues) -join ' ')" }
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 50 } else {
    Write-LlmWikiLearningPromotionResult $result
}
