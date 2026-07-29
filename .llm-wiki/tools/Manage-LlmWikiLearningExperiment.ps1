[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('shadow', 'canary-start', 'canary-record', 'canary-evaluate', 'canary-stop', 'active', 'list', 'show', 'verify')]
    [string]$Action = 'list',
    [string]$Id,
    [string]$WorkspacePath,
    [ValidateSet('improved', 'neutral', 'degraded')]
    [string]$Outcome,
    [string[]]$Evidence = @(),
    [Nullable[int]]$Percentage,
    [string]$Reason,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$registryPath = Join-Path $wikiRoot 'knowledge/learning-experiments.json'
$policy = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/workspace-policies.json') -Raw | ConvertFrom-Json
$experimentPolicy = $policy.scheduler.learningPromotion.experiments

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 50 -Compress
    if ($null -eq $json) { $json = 'null' }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Read-Registry {
    $registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    if ($registry.schemaVersion -ne 1 -or $null -eq $registry.events) { throw 'Unsupported learning-experiment registry schema.' }
    $registry
}
function Write-Registry([object]$Registry) {
    [IO.File]::WriteAllText($registryPath, (($Registry | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Event.schemaVersion
        sequence = [int]$Event.sequence
        kind = [string]$Event.kind
        candidateId = [string]$Event.candidateId
        createdAtUtc = ([DateTimeOffset]$Event.createdAtUtc).ToUniversalTime().ToString('o')
        previousHash = [string]$Event.previousHash
        shadow = $Event.shadow
        canary = $Event.canary
        observation = $Event.observation
        evaluation = $Event.evaluation
        reason = $Event.reason
    }
}
function Add-Event([object]$Registry, [string]$Kind, [string]$CandidateId, [object]$Shadow, [object]$Canary, [object]$Observation, [object]$Evaluation, [string]$EventReason, [string]$CreatedAtUtc) {
    $previousHash = if (@($Registry.events).Count -eq 0) { '' } else { [string]$Registry.events[-1].eventHash }
    $event = [pscustomobject][ordered]@{
        schemaVersion = 1
        sequence = @($Registry.events).Count + 1
        kind = $Kind
        candidateId = $CandidateId
        createdAtUtc = $CreatedAtUtc
        previousHash = $previousHash
        shadow = $Shadow
        canary = $Canary
        observation = $Observation
        evaluation = $Evaluation
        reason = $EventReason
        eventHash = ''
    }
    $event.eventHash = Get-Hash (Get-EventPayload $event)
    $Registry.events = @($Registry.events) + $event
    $event
}
function Get-Learning([string]$CandidateId) {
    $learning = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') show -Id $CandidateId -Format Json | ConvertFrom-Json
    if (-not $learning.valid) { throw "Learning candidate is invalid: $CandidateId" }
    @($learning.candidates)[0]
}
function Get-Application([string]$CandidateId) {
    (& (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') plan -Id $CandidateId -Format Json | ConvertFrom-Json).application
}
function Get-Shadow([object]$Candidate, [object]$Application) {
    if ($Application.target -eq 'durable-memory') {
        return [pscustomobject][ordered]@{
            method = 'independent-evidence'
            sampleCount = [int]$Candidate.distinctTaskCount
            baselineMeanAbsoluteError = $null
            proposalMeanAbsoluteError = $null
            improvementPercent = $null
            verdict = $(if ($Candidate.eligible -or $Candidate.decision -eq 'approved') { 'pass' } else { 'fail' })
            rationale = 'Durable guidance is shadowed against independently repeated task evidence and scoped changed paths.'
        }
    }
    $samples = @($Candidate.observations | Where-Object {
        $null -ne $_.data -and
        [double]$_.data.recommendedSeconds -gt 0 -and
        [double]$_.data.expectedSeconds -gt 0
    })
    if ($samples.Count -eq 0) {
        return [pscustomobject][ordered]@{
            method = 'historical-error-replay'
            sampleCount = 0
            baselineMeanAbsoluteError = $null
            proposalMeanAbsoluteError = $null
            improvementPercent = $null
            verdict = 'inconclusive'
            rationale = 'No observation contains both historical forecast and actual duration; use a canary.'
        }
    }
    $baselineError = [Math]::Round([double](($samples | ForEach-Object {
        [Math]::Abs([double]$_.data.expectedSeconds - [double]$_.data.recommendedSeconds)
    } | Measure-Object -Average).Average), 2)
    $proposalError = [Math]::Round([double](($samples | ForEach-Object {
        [Math]::Abs([double]$Application.recommendedSeconds - [double]$_.data.recommendedSeconds)
    } | Measure-Object -Average).Average), 2)
    $improvement = if ($baselineError -le 0) {
        $(if ($proposalError -le 0) { 100 } else { -100 })
    } else {
        [Math]::Round(($baselineError - $proposalError) / $baselineError * 100, 2)
    }
    [pscustomobject][ordered]@{
        method = 'historical-error-replay'
        sampleCount = $samples.Count
        baselineMeanAbsoluteError = $baselineError
        proposalMeanAbsoluteError = $proposalError
        improvementPercent = $improvement
        verdict = $(if ($improvement -ge [double]$experimentPolicy.minimumShadowImprovementPercent) { 'pass' } else { 'fail' })
        rationale = "Proposal must improve historical mean absolute error by at least $($experimentPolicy.minimumShadowImprovementPercent)%."
    }
}
function Get-CanaryEvaluation([object[]]$Observations) {
    $sampleCount = @($Observations).Count
    $improved = @($Observations | Where-Object outcome -eq 'improved').Count
    $neutral = @($Observations | Where-Object outcome -eq 'neutral').Count
    $degraded = @($Observations | Where-Object outcome -eq 'degraded').Count
    $degradationPercent = if ($sampleCount -eq 0) { 0 } else { [Math]::Round(100 * $degraded / $sampleCount, 2) }
    $verdict = if ($sampleCount -lt [int]$experimentPolicy.minimumCanarySamples) {
        'inconclusive'
    } elseif ($degradationPercent -le [double]$experimentPolicy.maximumCanaryDegradationPercent) {
        'pass'
    } else { 'fail' }
    [pscustomobject][ordered]@{
        sampleCount = $sampleCount
        distinctWorkspaceCount = @($Observations.workspace | Sort-Object -Unique).Count
        improvedCount = $improved
        neutralCount = $neutral
        degradedCount = $degraded
        degradationPercent = $degradationPercent
        verdict = $verdict
    }
}
function Get-View([object]$Registry) {
    $states = [ordered]@{}
    foreach ($event in @($Registry.events)) {
        $candidateId = [string]$event.candidateId
        if (-not $states.Contains($candidateId)) {
            $states[$candidateId] = [pscustomobject][ordered]@{
                candidateId = $candidateId
                shadow = $null
                shadowEventHash = ''
                canaryState = 'not-started'
                canary = $null
                canaryEventHash = ''
                observations = @()
                evaluation = $null
                successful = $false
            }
        }
        $state = $states[$candidateId]
        if ($event.kind -eq 'shadowed') {
            $state.shadow = $event.shadow
            $state.shadowEventHash = [string]$event.eventHash
            $state.successful = $event.shadow.verdict -eq 'pass'
        } elseif ($event.kind -eq 'canary-started') {
            $state.canaryState = 'active'
            $state.canary = $event.canary
            $state.canaryEventHash = [string]$event.eventHash
            $state.observations = @()
            $state.evaluation = $null
            $state.successful = $false
        } elseif ($event.kind -eq 'canary-observed') {
            $state.observations = @($state.observations) + $event.observation
        } elseif ($event.kind -eq 'canary-stopped') {
            $state.canaryState = 'stopped'
            $state.evaluation = $event.evaluation
            $state.successful = $event.evaluation.verdict -eq 'pass'
        }
    }
    @($states.Values | ForEach-Object {
        $state = $_
        [pscustomobject][ordered]@{
            candidateId = $state.candidateId
            shadow = $state.shadow
            shadowEventHash = $state.shadowEventHash
            canaryState = $state.canaryState
            canary = $state.canary
            canaryEventHash = $state.canaryEventHash
            observations = @($state.observations)
            currentEvaluation = Get-CanaryEvaluation @($state.observations)
            finalEvaluation = $state.evaluation
            successful = [bool]$state.successful
        }
    } | Sort-Object candidateId)
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    $previous = ''
    $sequence = 0
    $active = @{}
    $observationKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($event in @($Registry.events)) {
        $sequence++
        if ([int]$event.sequence -ne $sequence) { $issues.Add("Event sequence is invalid at $sequence.") }
        if ([string]$event.previousHash -cne $previous) { $issues.Add("Event $sequence has invalid previousHash.") }
        if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $issues.Add("Event $sequence has invalid eventHash.") }
        if ([string]::IsNullOrWhiteSpace([string]$event.candidateId)) { $issues.Add("Event $sequence has no candidateId.") }
        $prefix = [pscustomobject]@{ schemaVersion = 1; events = @($Registry.events | Select-Object -First ($sequence - 1)) }
        $prior = Get-View $prefix | Where-Object candidateId -eq $event.candidateId
        if ($event.kind -eq 'shadowed') {
            if ($null -eq $event.shadow -or $event.shadow.verdict -notin @('pass', 'fail', 'inconclusive')) { $issues.Add("Shadow $sequence is invalid.") }
            if ($null -eq $event.shadow.application) {
                $issues.Add("Shadow $sequence has no application snapshot.")
            } else {
                try {
                    $candidate = Get-Learning ([string]$event.candidateId)
                    $expectedShadow = Get-Shadow $candidate $event.shadow.application
                    $expectedShadow | Add-Member -NotePropertyName application -NotePropertyValue $event.shadow.application
                    $expectedShadow | Add-Member -NotePropertyName applicationHash -NotePropertyValue (Get-Hash $event.shadow.application)
                    if ((Get-Hash $event.shadow) -cne (Get-Hash $expectedShadow)) { $issues.Add("Shadow $sequence result is invalid.") }
                } catch {
                    $issues.Add("Shadow $sequence cannot be replayed: $($_.Exception.Message)")
                }
            }
        } elseif ($event.kind -eq 'canary-started') {
            if ($null -ne $prior -and $prior.canaryState -eq 'active') { $issues.Add("Canary $sequence was already active.") }
            if ([int]$event.canary.percentage -lt 1 -or [int]$event.canary.percentage -gt [int]$experimentPolicy.maximumCanaryPercentage) { $issues.Add("Canary $sequence percentage is invalid.") }
            if ($null -eq $event.canary.application) { $issues.Add("Canary $sequence has no application snapshot.") }
            $active[[string]$event.candidateId] = $true
        } elseif ($event.kind -eq 'canary-observed') {
            if ($null -eq $prior -or $prior.canaryState -ne 'active') { $issues.Add("Canary observation $sequence has no active canary.") }
            if ($event.observation.outcome -notin @('improved', 'neutral', 'degraded')) { $issues.Add("Canary observation $sequence outcome is invalid.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.observation.workspace)) { $issues.Add("Canary observation $sequence has no workspace.") }
            if (@($event.observation.evidence).Count -eq 0) { $issues.Add("Canary observation $sequence has no evidence.") }
            $key = "$($event.candidateId):$($event.observation.workspace)"
            if (-not $observationKeys.Add($key)) { $issues.Add("Duplicate canary observation: $key") }
        } elseif ($event.kind -eq 'canary-stopped') {
            if ($null -eq $prior -or $prior.canaryState -ne 'active') { $issues.Add("Canary stop $sequence has no active canary.") }
            $expected = Get-CanaryEvaluation @($prior.observations)
            if ((Get-Hash $event.evaluation) -cne (Get-Hash $expected)) { $issues.Add("Canary stop $sequence evaluation is invalid.") }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Canary stop $sequence has no reason.") }
            $active.Remove([string]$event.candidateId)
        } else {
            $issues.Add("Unknown learning-experiment event kind '$($event.kind)'.")
        }
        $previous = [string]$event.eventHash
    }
    if (@($active.Keys).Count -gt [int]$experimentPolicy.maximumActiveCanaries) { $issues.Add('Too many active learning canaries.') }
    @($issues)
}
function Test-Exposure([string]$CandidateId, [string]$Workspace, [int]$CanaryPercentage) {
    $value = Get-Hash "$CandidateId|$Workspace"
    $bucket = [Convert]::ToUInt32($value.Substring(0, 8), 16) % 100
    $bucket -lt $CanaryPercentage
}

$registry = Read-Registry
$now = $AsOfUtc.ToUniversalTime().ToString('o')
$result = $null
if ($Action -eq 'shadow') {
    if ([string]::IsNullOrWhiteSpace($Id)) { throw 'Id is required.' }
    $candidate = Get-Learning $Id
    if ($candidate.decision -ne 'approved') { throw 'Shadow evaluation requires an approved learning candidate.' }
    $application = Get-Application $Id
    $shadow = Get-Shadow $candidate $application
    $shadow | Add-Member -NotePropertyName application -NotePropertyValue $application
    $shadow | Add-Member -NotePropertyName applicationHash -NotePropertyValue (Get-Hash $application)
    $event = Add-Event $registry 'shadowed' $Id $shadow $null $null $null '' $now
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{ action = 'shadow'; valid = $issues.Count -eq 0; shadow = $shadow; eventHash = $event.eventHash; experiment = (Get-View $registry | Where-Object candidateId -eq $Id); issues = $issues }
} elseif ($Action -eq 'canary-start') {
    if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($Reason)) { throw 'Id and Reason are required.' }
    $candidate = Get-Learning $Id
    if ($candidate.decision -ne 'approved') { throw 'Canary requires an approved learning candidate.' }
    $percentageValue = if ($null -eq $Percentage) { [int]$experimentPolicy.defaultCanaryPercentage } else { [int]$Percentage }
    if ($percentageValue -lt 1 -or $percentageValue -gt [int]$experimentPolicy.maximumCanaryPercentage) { throw 'Canary percentage is outside policy bounds.' }
    if (@(Get-View $registry | Where-Object canaryState -eq 'active').Count -ge [int]$experimentPolicy.maximumActiveCanaries) { throw 'Maximum active learning canaries reached.' }
    $canary = [pscustomobject][ordered]@{ percentage = $percentageValue; application = Get-Application $Id; reason = $Reason }
    $event = Add-Event $registry 'canary-started' $Id $null $canary $null $null $Reason $now
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{ action = 'canary-start'; valid = $issues.Count -eq 0; canary = $canary; eventHash = $event.eventHash; experiment = (Get-View $registry | Where-Object candidateId -eq $Id); issues = $issues }
} elseif ($Action -eq 'canary-record') {
    if ([string]::IsNullOrWhiteSpace($Id) -or [string]::IsNullOrWhiteSpace($WorkspacePath) -or [string]::IsNullOrWhiteSpace($Outcome)) { throw 'Id, WorkspacePath, and Outcome are required.' }
    if ($Evidence.Count -eq 0) { throw 'Canary evidence is required.' }
    $workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
    if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') { throw 'WorkspacePath must identify one task workspace.' }
    $experiment = Get-View $registry | Where-Object candidateId -eq $Id
    if ($null -eq $experiment -or $experiment.canaryState -ne 'active') { throw "Active canary not found: $Id" }
    if (-not (Test-Exposure $Id $workspace ([int]$experiment.canary.percentage))) { throw 'Workspace was not exposed to this canary.' }
    $observation = [pscustomobject][ordered]@{ workspace = $workspace; outcome = $Outcome; evidence = @($Evidence | Sort-Object -Unique) }
    $event = Add-Event $registry 'canary-observed' $Id $null $null $observation $null '' $now
    $issues = @(Test-Registry $registry)
    if ($issues.Count -eq 0) { Write-Registry $registry }
    $result = [pscustomobject][ordered]@{ action = 'canary-record'; valid = $issues.Count -eq 0; observation = $observation; eventHash = $event.eventHash; experiment = (Get-View $registry | Where-Object candidateId -eq $Id); issues = $issues }
} elseif ($Action -in @('canary-evaluate', 'canary-stop')) {
    if ([string]::IsNullOrWhiteSpace($Id)) { throw 'Id is required.' }
    $experiment = Get-View $registry | Where-Object candidateId -eq $Id
    if ($null -eq $experiment -or $experiment.canaryState -ne 'active') { throw "Active canary not found: $Id" }
    $evaluation = Get-CanaryEvaluation @($experiment.observations)
    if ($Action -eq 'canary-evaluate') {
        $result = [pscustomobject][ordered]@{ action = 'canary-evaluate'; valid = $true; evaluation = $evaluation; experiment = $experiment; issues = @() }
    } else {
        if ([string]::IsNullOrWhiteSpace($Reason)) { throw 'Reason is required to stop a canary.' }
        $event = Add-Event $registry 'canary-stopped' $Id $null $null $null $evaluation $Reason $now
        $issues = @(Test-Registry $registry)
        if ($issues.Count -eq 0) { Write-Registry $registry }
        $result = [pscustomobject][ordered]@{ action = 'canary-stop'; valid = $issues.Count -eq 0; evaluation = $evaluation; eventHash = $event.eventHash; experiment = (Get-View $registry | Where-Object candidateId -eq $Id); issues = $issues }
    }
} else {
    $issues = @(Test-Registry $registry)
    $experiments = @(Get-View $registry)
    if ($Action -eq 'show') {
        if ([string]::IsNullOrWhiteSpace($Id)) { throw 'Id is required.' }
        $experiments = @($experiments | Where-Object candidateId -eq $Id)
        if ($experiments.Count -eq 0) { throw "Learning experiment not found: $Id" }
    } elseif ($Action -eq 'active') {
        $experiments = @($experiments | Where-Object canaryState -eq 'active')
        if (-not [string]::IsNullOrWhiteSpace($WorkspacePath)) {
            $workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
            $experiments = @($experiments | Where-Object { Test-Exposure $_.candidateId $workspace ([int]$_.canary.percentage) })
        }
    }
    $result = [pscustomobject][ordered]@{
        action = $Action
        valid = $issues.Count -eq 0
        totalCount = @(Get-View $registry).Count
        activeCount = @(Get-View $registry | Where-Object canaryState -eq 'active').Count
        successfulCount = @(Get-View $registry | Where-Object successful).Count
        experiments = $experiments
        registryFingerprint = Get-Hash @($registry.events)
        issues = $issues
    }
}
if ($FailOnInvalid -and -not $result.valid) { throw "Learning-experiment registry is invalid: $(@($result.issues) -join ' ')" }
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 50 } else {
    Write-Host "Learning experiment: action=$Action, valid=$($result.valid)"
    foreach ($experiment in @($result.experiments | Where-Object { $null -ne $_ })) {
        Write-Host " - $($experiment.candidateId): shadow=$($experiment.shadow.verdict), canary=$($experiment.canaryState), current=$($experiment.currentEvaluation.verdict), successful=$($experiment.successful)"
    }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
