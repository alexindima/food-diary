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
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$registryPath = Join-Path $wikiRoot 'knowledge/learning-promotions.json'
$policy = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/workspace-policies.json') -Raw | ConvertFrom-Json
$promotionPolicy = $policy.scheduler.learningPromotion

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    if ($null -eq $json) { $json = 'null' }
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Read-Registry {
    $registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    if ($registry.schemaVersion -ne 1 -or $null -eq $registry.events) { throw 'Unsupported learning-promotion registry schema.' }
    $registry
}
function Write-Registry([object]$Registry) {
    [IO.File]::WriteAllText($registryPath, (($Registry | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}
function Get-EventPayload([object]$Event) {
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
    $previousHash = if (@($Registry.events).Count -eq 0) { '' } else { [string]$Registry.events[-1].eventHash }
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
    $event.eventHash = Get-Hash (Get-EventPayload $event)
    $Registry.events = @($Registry.events) + $event
    $event
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
        $tasks = @($item.observations.workspace | Sort-Object -Unique)
        $evidence = @($item.observations.evidence | Where-Object { $_ } | Sort-Object -Unique | Select-Object -First ([int]$promotionPolicy.maximumEvidenceItems))
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
    $scopePaths = @($observations.changedPaths | Where-Object { $_ } | Sort-Object -Unique | ForEach-Object {
        '^' + [regex]::Escape([string]$_) + '$'
    })
    $subjectIds = @($observations.subjectIds | Where-Object { $_ } | Sort-Object -Unique)
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
        evidenceHash = Get-Hash @($Candidate.evidence)
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
        if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $issues.Add("Event $sequence has invalid eventHash.") }
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
            $distinctTaskCount = @($observations.workspace | Sort-Object -Unique).Count
            $averageScore = if ($observations.Count -eq 0) { 0 } else {
                [Math]::Round([double](($observations.score | Measure-Object -Average).Average), 2)
            }
            $evidence = @($observations.evidence | Where-Object { $_ } | Sort-Object -Unique | Select-Object -First ([int]$promotionPolicy.maximumEvidenceItems))
            $expectedTarget = if ($observations.Count -eq 0) { '' } else { [string]$observations[-1].target }
            if ([string]$event.decision.target -cne $expectedTarget) { $issues.Add("Decision $sequence has an invalid target snapshot.") }
            if ([int]$event.decision.distinctTaskCount -ne $distinctTaskCount) { $issues.Add("Decision $sequence has an invalid task-count snapshot.") }
            if ([double]$event.decision.averageScore -ne $averageScore) { $issues.Add("Decision $sequence has an invalid score snapshot.") }
            if ([string]$event.decision.evidenceHash -cne (Get-Hash $evidence)) { $issues.Add("Decision $sequence has an invalid evidence snapshot.") }
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
                if ((Get-Hash $event.decision) -cne (Get-Hash $expectedApplication)) { $issues.Add("Application $sequence has an invalid materialization snapshot.") }
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
            if ($null -eq $event.decision -or (Get-Hash $event.decision) -cne (Get-Hash $candidate.application)) { $issues.Add("Rollback $sequence has an invalid application snapshot.") }
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

$registry = Read-Registry
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
            subjectIds = @($candidate.evidence | Where-Object { $_ -in @($evidenceBundle.checks.id) } | Sort-Object -Unique)
        }
        $event = Add-Event $registry 'observed' (Get-CandidateId $candidate) $observation $null '' '' $now
        $added.Add($event)
        $existingKeys.Add($observationKey) | Out-Null
    }
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0 -and $added.Count -gt 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{
        action = 'observe'; valid = $issues.Count -eq 0; addedCount = $added.Count
        observationEventHashes = @($added.eventHash); candidates = @(Get-View $registry); issues = $issues
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
        evidenceHash = Get-Hash @($candidate.evidence)
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
    Write-Host "Learning promotion: action=$Action, valid=$($result.valid)"
    if ($null -ne $result.addedCount) { Write-Host "Observed=$($result.addedCount)" }
    foreach ($candidate in @($result.candidates | Where-Object { $null -ne $_ })) {
        Write-Host " - [$($candidate.decision)/$($candidate.materialization)] $($candidate.id): tasks=$($candidate.distinctTaskCount), score=$($candidate.averageScore), eligible=$($candidate.eligible), target=$($candidate.target)"
    }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
