[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('start', 'forecast', 'evaluate', 'stop', 'list', 'show', 'verify')]
    [string]$Action = 'list',
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
$registryPath = Join-Path $wikiRoot 'knowledge/instruction-experiments.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$experimentPolicy = $policy.scheduler.verificationPlanner.instructionExperiments

function ConvertTo-CanonicalHashValue([object]$Value) {
    if ($null -eq $Value) { return $null }
    if ($Value -is [DateTime] -or $Value -is [DateTimeOffset]) {
        return ([DateTimeOffset]$Value).ToUniversalTime().ToString('o')
    }
    if ($Value -is [string]) {
        $parsed = [DateTimeOffset]::MinValue
        if ([DateTimeOffset]::TryParseExact(
            $Value,
            'o',
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$parsed
        )) {
            return $parsed.ToUniversalTime().ToString('o')
        }
        return $Value
    }
    if ($Value -is [Collections.IDictionary]) {
        $result = [ordered]@{}
        foreach ($key in $Value.Keys) { $result[$key] = ConvertTo-CanonicalHashValue $Value[$key] }
        return [pscustomobject]$result
    }
    if ($Value -is [Collections.IEnumerable]) {
        return @($Value | ForEach-Object { ConvertTo-CanonicalHashValue $_ })
    }
    if ($Value -is [psobject] -and @($Value.PSObject.Properties).Count -gt 0) {
        $result = [ordered]@{}
        foreach ($property in $Value.PSObject.Properties) {
            $result[$property.Name] = ConvertTo-CanonicalHashValue $property.Value
        }
        return [pscustomobject]$result
    }
    $Value
}
function Get-Hash([object]$Value) {
    $isEmptyCollection = $Value -is [Collections.IEnumerable] -and
        $Value -isnot [string] -and
        @($Value).Count -eq 0
    # Windows PowerShell 5 emits no pipeline object for an empty array. Passing
    # that result through ConvertTo-Json produces $null instead of JSON.
    $json = if ($null -eq $Value -or $isEmptyCollection) {
        '[]'
    } else {
        ConvertTo-Json -InputObject (ConvertTo-CanonicalHashValue $Value) -Depth 30 -Compress
    }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-PropertySum([object[]]$Items, [string]$Property) {
    $measure = $Items | Measure-Object -Property $Property -Sum
    if ($null -eq $measure -or $null -eq $measure.PSObject.Properties['Sum'] -or $null -eq $measure.Sum) {
        return 0
    }
    return $measure.Sum
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Event.schemaVersion
        sequence = [int]$Event.sequence
        kind = [string]$Event.kind
        experimentId = [string]$Event.experimentId
        createdAtUtc = ([DateTimeOffset]$Event.createdAtUtc).ToUniversalTime().ToString('o')
        previousEventHash = [string]$Event.previousEventHash
        definition = $Event.definition
        evaluation = $Event.evaluation
        reason = $Event.reason
    }
}
function Read-Registry {
    Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
}
function Add-Event([object]$Registry, [string]$Kind, [string]$ExperimentId, [object]$Definition, [object]$Evaluation, [string]$EventReason, [string]$CreatedAtUtc) {
    $event = [pscustomobject][ordered]@{
        schemaVersion = 1
        sequence = @($Registry.events).Count + 1
        kind = $Kind
        experimentId = $ExperimentId
        createdAtUtc = $CreatedAtUtc
        previousEventHash = $(if (@($Registry.events).Count -eq 0) { '' } else { [string]$Registry.events[-1].eventHash })
        definition = $Definition
        evaluation = $Evaluation
        reason = $EventReason
        eventHash = ''
    }
    $event.eventHash = Get-Hash (Get-EventPayload $event)
    $Registry.events = @($Registry.events) + @($event)
    $event
}
function Get-View([object]$Registry) {
    $states = [ordered]@{}
    foreach ($event in @($Registry.events)) {
        $id = [string]$event.experimentId
        if (-not $states.Contains($id)) {
            $states[$id] = [pscustomobject][ordered]@{
                experimentId = $id
                state = 'unknown'
                definition = $null
                startedAtUtc = $null
                stoppedAtUtc = $null
                lookCount = 0
                evaluations = @()
                currentEvaluation = $null
                finalEvaluation = $null
                stopReason = ''
                eventHash = ''
            }
        }
        $state = $states[$id]
        if ($event.kind -eq 'started') {
            $state.state = 'active'
            $state.definition = $event.definition
            $state.startedAtUtc = $event.createdAtUtc
        } elseif ($event.kind -eq 'evaluated') {
            $state.lookCount = [int]$event.evaluation.lookNumber
            $state.evaluations = @($state.evaluations) + @($event.evaluation)
            $state.currentEvaluation = $event.evaluation
        } elseif ($event.kind -eq 'stopped') {
            $state.state = 'stopped'
            $state.stoppedAtUtc = $event.createdAtUtc
            $state.finalEvaluation = $event.evaluation
            $state.stopReason = $event.reason
        }
        $state.eventHash = $event.eventHash
    }
    @($states.Values | Sort-Object startedAtUtc, experimentId)
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([int]$Registry.schemaVersion -ne 1) { $issues.Add('Registry schemaVersion must be 1.') }
    $previous = ''
    $active = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $known = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $sequence = 0
    foreach ($event in @($Registry.events)) {
        $sequence++
        if ([int]$event.schemaVersion -ne 1 -or [int]$event.sequence -ne $sequence) { $issues.Add("Instruction experiment sequence $sequence is invalid.") }
        if ([string]$event.previousEventHash -cne $previous) { $issues.Add("Instruction experiment chain is invalid at sequence $sequence.") }
        if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $issues.Add("Instruction experiment hash is invalid at sequence $sequence.") }
        if ($event.kind -eq 'started') {
            if (-not $known.Add([string]$event.experimentId)) { $issues.Add("Instruction experiment '$($event.experimentId)' was started twice.") }
            $null = $active.Add([string]$event.experimentId)
            if ([string]$event.definition.path -notmatch '(^|/)AGENTS\.md$') { $issues.Add("Instruction experiment path is invalid at sequence $sequence.") }
            if ([string]$event.definition.baselineFingerprint -notmatch '^[a-f0-9]{64}$' -or [string]$event.definition.candidateFingerprint -notmatch '^[a-f0-9]{64}$') { $issues.Add("Instruction experiment fingerprints are invalid at sequence $sequence.") }
            if ([string]$event.definition.baselineFingerprint -ceq [string]$event.definition.candidateFingerprint) { $issues.Add("Instruction experiment cohorts are identical at sequence $sequence.") }
        } elseif ($event.kind -eq 'evaluated') {
            if (-not $active.Contains([string]$event.experimentId)) { $issues.Add("Instruction experiment '$($event.experimentId)' was not active when evaluated.") }
            $priorState = Get-View ([pscustomobject]@{ schemaVersion = 1; events = @($Registry.events | Select-Object -First ($sequence - 1)) }) |
                Where-Object experimentId -eq $event.experimentId |
                Select-Object -First 1
            $expectedLook = if ($null -eq $priorState) { 1 } else { [int]$priorState.lookCount + 1 }
            if ([int]$event.evaluation.lookNumber -ne $expectedLook) { $issues.Add("Instruction experiment look number is invalid at sequence $sequence.") }
            if ([int]$event.evaluation.lookNumber -gt [int]$experimentPolicy.sequentialMonitoring.maximumLooks) { $issues.Add("Instruction experiment exceeded maximum looks at sequence $sequence.") }
            if ([string]$event.evaluation.outcomeRegistryFingerprint -notmatch '^[a-f0-9]{64}$') { $issues.Add("Instruction experiment evaluation fingerprint is invalid at sequence $sequence.") }
            if ($event.evaluation.verdict -notin @('adopt', 'rollback', 'inconclusive')) { $issues.Add("Instruction experiment evaluation verdict is invalid at sequence $sequence.") }
            if ($null -ne $priorState.currentEvaluation -and
                [int]$event.evaluation.candidate.sampleCount - [int]$priorState.currentEvaluation.candidate.sampleCount -lt [int]$experimentPolicy.sequentialMonitoring.minimumNewCandidateSamples) {
                $issues.Add("Instruction experiment evaluation has insufficient new candidate samples at sequence $sequence.")
            }
        } elseif ($event.kind -eq 'stopped') {
            if (-not $active.Remove([string]$event.experimentId)) { $issues.Add("Instruction experiment '$($event.experimentId)' was not active when stopped.") }
            if ($event.evaluation.verdict -notin @('adopt', 'rollback', 'inconclusive')) { $issues.Add("Instruction experiment evaluation is invalid at sequence $sequence.") }
            $priorState = Get-View ([pscustomobject]@{ schemaVersion = 1; events = @($Registry.events | Select-Object -First ($sequence - 1)) }) |
                Where-Object experimentId -eq $event.experimentId |
                Select-Object -First 1
            if ($null -eq $priorState.currentEvaluation -or (Get-Hash $event.evaluation) -cne (Get-Hash $priorState.currentEvaluation)) {
                $issues.Add("Instruction experiment stop does not bind the latest recorded evaluation at sequence $sequence.")
            }
            if ([string]::IsNullOrWhiteSpace([string]$event.reason)) { $issues.Add("Instruction experiment stop reason is absent at sequence $sequence.") }
        } else {
            $issues.Add("Instruction experiment kind '$($event.kind)' is invalid.")
        }
        $previous = [string]$event.eventHash
    }
    [pscustomobject][ordered]@{
        valid = $issues.Count -eq 0
        issues = @($issues)
        headHash = $previous
        registryFingerprint = Get-Hash @($Registry.events | ForEach-Object { "$($_.sequence)|$($_.eventHash)" })
    }
}
function Get-Evaluation([object]$Experiment, [int]$LookNumber, [string]$EvaluatedAtUtc) {
    $metrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionOutcome.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $metrics.valid) { throw "Instruction outcome metrics are invalid: $(@($metrics.issues) -join ' ')" }
    $baseline = $metrics.metrics.profiles |
        Where-Object { $_.path -eq $Experiment.definition.path -and $_.fingerprint -eq $Experiment.definition.baselineFingerprint } |
        Select-Object -First 1
    $candidate = $metrics.metrics.profiles |
        Where-Object { $_.path -eq $Experiment.definition.path -and $_.fingerprint -eq $Experiment.definition.candidateFingerprint } |
        Select-Object -First 1
    $baselineSamples = if ($null -eq $baseline) { 0 } else { [int]$baseline.sampleCount }
    $candidateSamples = if ($null -eq $candidate) { 0 } else { [int]$candidate.sampleCount }
    $enough = $baselineSamples -ge [int]$experimentPolicy.minimumSamplesPerCohort -and $candidateSamples -ge [int]$experimentPolicy.minimumSamplesPerCohort
    $matchedCohorts = @()
    if ($null -ne $baseline -and $null -ne $candidate) {
        $matchedCohorts = @($baseline.cohorts | ForEach-Object {
            $baselineCohort = $_
            $candidateCohort = $candidate.cohorts | Where-Object key -eq $baselineCohort.key | Select-Object -First 1
            if ($null -ne $candidateCohort -and
                [int]$baselineCohort.sampleCount -ge [int]$experimentPolicy.minimumMatchedSamplesPerCohort -and
                [int]$candidateCohort.sampleCount -ge [int]$experimentPolicy.minimumMatchedSamplesPerCohort) {
                $weight = [Math]::Min([int]$baselineCohort.sampleCount, [int]$candidateCohort.sampleCount)
                [pscustomobject][ordered]@{
                    key = [string]$baselineCohort.key
                    weight = $weight
                    baselineSampleCount = [int]$baselineCohort.sampleCount
                    candidateSampleCount = [int]$candidateCohort.sampleCount
                    baselineOutcomeScore = [double]$baselineCohort.averageOutcomeScore
                    candidateOutcomeScore = [double]$candidateCohort.averageOutcomeScore
                    baselineOutcomeStandardDeviation = [double]$baselineCohort.outcomeStandardDeviation
                    candidateOutcomeStandardDeviation = [double]$candidateCohort.outcomeStandardDeviation
                    outcomeGainPoints = [Math]::Round([double]$candidateCohort.averageOutcomeScore - [double]$baselineCohort.averageOutcomeScore, 2)
                    outcomeDifferenceVariance = [Math]::Round(
                        ([Math]::Pow([double]$baselineCohort.outcomeStandardDeviation, 2) / [int]$baselineCohort.sampleCount) +
                        ([Math]::Pow([double]$candidateCohort.outcomeStandardDeviation, 2) / [int]$candidateCohort.sampleCount),
                        8
                    )
                    baselineSuccessRatePercent = [double]$baselineCohort.successRatePercent
                    candidateSuccessRatePercent = [double]$candidateCohort.successRatePercent
                    successRateDeltaPoints = [Math]::Round([double]$candidateCohort.successRatePercent - [double]$baselineCohort.successRatePercent, 2)
                    successDifferenceVariance = [Math]::Round(
                        (
                            (([int]$baselineCohort.successCount + 1.0) * ([int]$baselineCohort.sampleCount - [int]$baselineCohort.successCount + 1.0)) /
                            ([Math]::Pow([int]$baselineCohort.sampleCount + 2.0, 2) * ([int]$baselineCohort.sampleCount + 3.0))
                        ) + (
                            (([int]$candidateCohort.successCount + 1.0) * ([int]$candidateCohort.sampleCount - [int]$candidateCohort.successCount + 1.0)) /
                            ([Math]::Pow([int]$candidateCohort.sampleCount + 2.0, 2) * ([int]$candidateCohort.sampleCount + 3.0))
                        ),
                        8
                    )
                }
            }
        })
    }
    $matchedWeight = [int](Get-PropertySum $matchedCohorts 'weight')
    $outcomeDelta = if ($matchedWeight -eq 0) { $null } else {
        [Math]::Round([double](($matchedCohorts | ForEach-Object { [double]$_.outcomeGainPoints * [int]$_.weight } | Measure-Object -Sum).Sum) / $matchedWeight, 2)
    }
    $successDelta = if ($matchedWeight -eq 0) { $null } else {
        [Math]::Round([double](($matchedCohorts | ForEach-Object { [double]$_.successRateDeltaPoints * [int]$_.weight } | Measure-Object -Sum).Sum) / $matchedWeight, 2)
    }
    $outcomeStandardError = if ($matchedWeight -eq 0) { $null } else {
        [Math]::Sqrt([double](($matchedCohorts | ForEach-Object { [Math]::Pow([int]$_.weight, 2) * [double]$_.outcomeDifferenceVariance } | Measure-Object -Sum).Sum) / [Math]::Pow($matchedWeight, 2))
    }
    $successStandardErrorPoints = if ($matchedWeight -eq 0) { $null } else {
        100.0 * [Math]::Sqrt([double](($matchedCohorts | ForEach-Object { [Math]::Pow([int]$_.weight, 2) * [double]$_.successDifferenceVariance } | Measure-Object -Sum).Sum) / [Math]::Pow($matchedWeight, 2))
    }
    $z = [double]$experimentPolicy.sequentialMonitoring.adjustedZScore
    $outcomeInterval = if ($null -eq $outcomeDelta) { $null } else {
        [pscustomobject][ordered]@{
            lower = [Math]::Round([Math]::Max(-100.0, [double]$outcomeDelta - $z * $outcomeStandardError), 2)
            upper = [Math]::Round([Math]::Min(100.0, [double]$outcomeDelta + $z * $outcomeStandardError), 2)
            standardError = [Math]::Round($outcomeStandardError, 4)
        }
    }
    $successInterval = if ($null -eq $successDelta) { $null } else {
        [pscustomobject][ordered]@{
            lower = [Math]::Round([Math]::Max(-100.0, [double]$successDelta - $z * $successStandardErrorPoints), 2)
            upper = [Math]::Round([Math]::Min(100.0, [double]$successDelta + $z * $successStandardErrorPoints), 2)
            standardError = [Math]::Round($successStandardErrorPoints, 4)
        }
    }
    $verdict = if (-not $enough -or $matchedCohorts.Count -eq 0) {
        'inconclusive'
    } elseif ([double]$successInterval.upper -lt -[double]$experimentPolicy.maximumSuccessRateDropPoints -or [double]$outcomeInterval.upper -lt 0) {
        'rollback'
    } elseif ([double]$outcomeInterval.lower -ge [double]$experimentPolicy.minimumOutcomeGainPoints -and [double]$successInterval.lower -ge -[double]$experimentPolicy.maximumSuccessRateDropPoints) {
        'adopt'
    } else {
        'inconclusive'
    }
    [pscustomobject][ordered]@{
        evaluatedAtUtc = $EvaluatedAtUtc
        lookNumber = $LookNumber
        maximumLooks = [int]$experimentPolicy.sequentialMonitoring.maximumLooks
        outcomeRegistryFingerprint = [string]$metrics.metrics.registryFingerprint
        baseline = [pscustomobject][ordered]@{
            fingerprint = [string]$Experiment.definition.baselineFingerprint
            sampleCount = $baselineSamples
            averageOutcomeScore = $(if ($null -eq $baseline) { $null } else { [double]$baseline.averageOutcomeScore })
            successRatePercent = $(if ($null -eq $baseline) { $null } else { [double]$baseline.successRatePercent })
        }
        candidate = [pscustomobject][ordered]@{
            fingerprint = [string]$Experiment.definition.candidateFingerprint
            sampleCount = $candidateSamples
            averageOutcomeScore = $(if ($null -eq $candidate) { $null } else { [double]$candidate.averageOutcomeScore })
            successRatePercent = $(if ($null -eq $candidate) { $null } else { [double]$candidate.successRatePercent })
        }
        minimumSamplesPerCohort = [int]$experimentPolicy.minimumSamplesPerCohort
        minimumMatchedSamplesPerCohort = [int]$experimentPolicy.minimumMatchedSamplesPerCohort
        matchedCohortCount = $matchedCohorts.Count
        matchedWeight = $matchedWeight
        matchedCohorts = $matchedCohorts
        outcomeGainPoints = $outcomeDelta
        successRateDeltaPoints = $successDelta
        nominalConfidenceZScore = [double]$experimentPolicy.confidenceZScore
        sequentialAdjustedZScore = $z
        outcomeGainInterval = $outcomeInterval
        successRateDeltaInterval = $successInterval
        verdict = $verdict
        recommendation = $(if ($verdict -eq 'adopt') { 'Keep the candidate instruction and proceed through governed learning approval; matched task cohorts improved.' } elseif ($verdict -eq 'rollback') { 'Restore the baseline instruction version; matched task cohorts regressed.' } else { 'Collect enough comparable risk/complexity cohorts or revise the candidate.' })
    }
}
function Get-Forecast([object]$Experiment) {
    $metrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionOutcome.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $metrics.valid) { throw "Instruction outcome metrics are invalid: $(@($metrics.issues) -join ' ')" }
    $baseline = $metrics.metrics.profiles |
        Where-Object { $_.path -eq $Experiment.definition.path -and $_.fingerprint -eq $Experiment.definition.baselineFingerprint } |
        Select-Object -First 1
    $candidate = $metrics.metrics.profiles |
        Where-Object { $_.path -eq $Experiment.definition.path -and $_.fingerprint -eq $Experiment.definition.candidateFingerprint } |
        Select-Object -First 1
    $zAlpha = [double]$experimentPolicy.sequentialMonitoring.adjustedZScore
    $zPower = [double]$experimentPolicy.powerPlanning.powerZScore
    $effect = [double]$experimentPolicy.minimumOutcomeGainPoints
    $maximum = [int]$experimentPolicy.powerPlanning.maximumRequiredSamplesPerCohort
    $cohorts = @()
    if ($null -ne $baseline) {
        $cohorts = @($baseline.cohorts | ForEach-Object {
            $baselineCohort = $_
            $candidateCohort = if ($null -eq $candidate) { $null } else { $candidate.cohorts | Where-Object key -eq $baselineCohort.key | Select-Object -First 1 }
            $baselineSd = [double]$baselineCohort.outcomeStandardDeviation
            $candidateSd = if ($null -eq $candidateCohort) { 0.0 } else { [double]$candidateCohort.outcomeStandardDeviation }
            $sigma = if ($baselineSd -gt 0 -and $candidateSd -gt 0) {
                [Math]::Sqrt(([Math]::Pow($baselineSd, 2) + [Math]::Pow($candidateSd, 2)) / 2.0)
            } elseif ($baselineSd -gt 0) {
                $baselineSd
            } elseif ($candidateSd -gt 0) {
                $candidateSd
            } else {
                [double]$experimentPolicy.powerPlanning.defaultOutcomeStandardDeviation
            }
            $rawRequired = [Math]::Ceiling(2.0 * [Math]::Pow(($zAlpha + $zPower) * $sigma / $effect, 2))
            $required = [Math]::Min(
                $maximum,
                [Math]::Max([int]$experimentPolicy.minimumMatchedSamplesPerCohort, [int]$rawRequired)
            )
            $baselineCount = [int]$baselineCohort.sampleCount
            $candidateCount = if ($null -eq $candidateCohort) { 0 } else { [int]$candidateCohort.sampleCount }
            [pscustomobject][ordered]@{
                key = [string]$baselineCohort.key
                assumedOutcomeStandardDeviation = [Math]::Round($sigma, 4)
                minimumDetectableGainPoints = $effect
                requiredSamplesPerSide = $required
                capped = $rawRequired -gt $maximum
                baselineSampleCount = $baselineCount
                candidateSampleCount = $candidateCount
                remainingBaselineSamples = [Math]::Max(0, $required - $baselineCount)
                remainingCandidateSamples = [Math]::Max(0, $required - $candidateCount)
            }
        } | Sort-Object key)
    }
    [pscustomobject][ordered]@{
        generatedAtUtc = [DateTime]::UtcNow.ToString('o')
        outcomeRegistryFingerprint = [string]$metrics.metrics.registryFingerprint
        adjustedZScore = $zAlpha
        powerZScore = $zPower
        minimumOutcomeGainPoints = $effect
        cohortCount = $cohorts.Count
        cohorts = $cohorts
        remainingCandidateSamples = [int](Get-PropertySum $cohorts 'remainingCandidateSamples')
        planningStatus = $(if ($cohorts.Count -eq 0) { 'no-baseline-cohorts' } elseif (@($cohorts | Where-Object capped).Count -gt 0) { 'capped-estimate' } elseif ([int](Get-PropertySum $cohorts 'remainingCandidateSamples') -eq 0) { 'target-reached' } else { 'collecting' })
    }
}
function Write-Registry([object]$Registry) {
    [IO.File]::WriteAllText($registryPath, (($Registry | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}

$registry = Read-Registry
$validation = Test-Registry $registry
if ($Action -eq 'start') {
    if (-not $validation.valid) { throw "Instruction experiment registry is invalid: $(@($validation.issues) -join ' ')" }
    if ([string]::IsNullOrWhiteSpace($Id)) { throw 'Id is required.' }
    if ([string]::IsNullOrWhiteSpace($Reason)) { throw 'Reason is required.' }
    if (@(Get-View $registry | Where-Object state -eq 'active').Count -ge [int]$experimentPolicy.maximumActiveExperiments) { throw 'Maximum active instruction experiments reached.' }
    $candidateResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionOutcome.ps1') candidates -Format Json | ConvertFrom-Json
    $candidate = $candidateResult.candidates | Where-Object id -eq $Id | Select-Object -First 1
    if ($null -eq $candidate) { throw "Instruction candidate not found: $Id" }
    if ([string]$candidate.currentFingerprint -notmatch '^[a-f0-9]{64}$') { throw "Current instruction file is absent: $($candidate.path)" }
    if ([string]$candidate.currentFingerprint -ceq [string]$candidate.observedFingerprint) { throw 'Edit the instruction candidate before starting an experiment; baseline and candidate fingerprints are identical.' }
    $definition = [pscustomobject][ordered]@{
        candidateId = [string]$candidate.id
        path = [string]$candidate.path
        baselineFingerprint = [string]$candidate.observedFingerprint
        candidateFingerprint = [string]$candidate.currentFingerprint
        baselineEvidence = @($candidate.evidence)
        policyFingerprint = Get-FileSha $policyPath
        reason = $Reason
    }
    $experimentId = "instruction-experiment-$((Get-Hash $definition).Substring(0, 20))"
    if (@(Get-View $registry | Where-Object experimentId -eq $experimentId).Count -gt 0) { throw "Instruction experiment already exists: $experimentId" }
    $event = Add-Event $registry 'started' $experimentId $definition $null $Reason ($AsOfUtc.ToUniversalTime().ToString('o'))
    if (@($registry.events).Count -gt [int]$experimentPolicy.maximumEvents) { throw 'Instruction experiment registry reached maximumEvents.' }
    $post = Test-Registry $registry
    if (-not $post.valid) { throw "New instruction experiment is invalid: $(@($post.issues) -join ' ')" }
    Write-Registry $registry
    $result = [pscustomobject][ordered]@{ action = 'start'; valid = $true; experiment = (Get-View $registry | Where-Object experimentId -eq $experimentId); eventHash = $event.eventHash; issues = @() }
} elseif ($Action -in @('forecast', 'evaluate', 'stop')) {
    $experiment = Get-View $registry | Where-Object experimentId -eq $Id | Select-Object -First 1
    if ($null -eq $experiment -or $experiment.state -ne 'active') { throw "Active instruction experiment not found: $Id" }
    if ($Action -eq 'forecast') {
        $forecast = Get-Forecast $experiment
        $result = [pscustomobject][ordered]@{ action = 'forecast'; valid = $true; forecast = $forecast; experiment = $experiment; issues = @() }
    } elseif ($Action -eq 'stop') {
        if ([string]::IsNullOrWhiteSpace($Reason)) { throw 'Reason is required to stop an instruction experiment.' }
        if ($null -eq $experiment.currentEvaluation) { throw 'Record at least one instruction experiment evaluation before stopping.' }
        $currentMetrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionOutcome.ps1') metrics -Format Json | ConvertFrom-Json
        if (-not $currentMetrics.valid) { throw "Instruction outcome metrics are invalid: $(@($currentMetrics.issues) -join ' ')" }
        if ([string]$experiment.currentEvaluation.outcomeRegistryFingerprint -cne [string]$currentMetrics.metrics.registryFingerprint) {
            throw 'Instruction outcomes changed after the latest evaluation; record a fresh evaluation before stopping.'
        }
        $evaluation = $experiment.currentEvaluation
        $event = Add-Event $registry 'stopped' $Id $null $evaluation $Reason ($AsOfUtc.ToUniversalTime().ToString('o'))
        $post = Test-Registry $registry
        if (-not $post.valid) { throw "Stopped instruction experiment is invalid: $(@($post.issues) -join ' ')" }
        Write-Registry $registry
        $result = [pscustomobject][ordered]@{ action = 'stop'; valid = $true; evaluation = $evaluation; experiment = (Get-View $registry | Where-Object experimentId -eq $Id); eventHash = $event.eventHash; issues = @() }
    } else {
        $lookNumber = [int]$experiment.lookCount + 1
        if ($lookNumber -gt [int]$experimentPolicy.sequentialMonitoring.maximumLooks) { throw 'Instruction experiment reached the maximum sequential looks.' }
        $evaluation = Get-Evaluation $experiment $lookNumber ($AsOfUtc.ToUniversalTime().ToString('o'))
        if ($null -ne $experiment.currentEvaluation -and
            [int]$evaluation.candidate.sampleCount - [int]$experiment.currentEvaluation.candidate.sampleCount -lt [int]$experimentPolicy.sequentialMonitoring.minimumNewCandidateSamples) {
            throw 'A new instruction experiment look requires additional candidate samples.'
        }
        $event = Add-Event $registry 'evaluated' $Id $null $evaluation '' ($AsOfUtc.ToUniversalTime().ToString('o'))
        $post = Test-Registry $registry
        if (-not $post.valid) { throw "Instruction experiment evaluation is invalid: $(@($post.issues) -join ' ')" }
        Write-Registry $registry
        $result = [pscustomobject][ordered]@{ action = 'evaluate'; valid = $true; evaluation = $evaluation; experiment = (Get-View $registry | Where-Object experimentId -eq $Id); eventHash = $event.eventHash; issues = @() }
    }
} elseif ($Action -eq 'verify') {
    $result = [pscustomobject][ordered]@{ action = 'verify'; valid = $validation.valid; issues = @($validation.issues); registryFingerprint = $validation.registryFingerprint; headHash = $validation.headHash; experiments = @(Get-View $registry) }
} else {
    $experiments = @(Get-View $registry)
    if ($Action -eq 'show') {
        $experiments = @($experiments | Where-Object experimentId -eq $Id)
        if ($experiments.Count -eq 0) { throw "Instruction experiment not found: $Id" }
    }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; issues = @($validation.issues); registryFingerprint = $validation.registryFingerprint; activeCount = @($experiments | Where-Object state -eq 'active').Count; experiments = $experiments }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Instruction experiments: action=$Action, valid=$($result.valid)"
    if ($result.PSObject.Properties['experiments']) {
        foreach ($experiment in @($result.experiments)) {
            $verdict = if ($null -eq $experiment.finalEvaluation) { '' } else { [string]$experiment.finalEvaluation.verdict }
            Write-Host " - $($experiment.experimentId): state=$($experiment.state), path=$($experiment.definition.path), verdict=$verdict"
        }
    }
    if ($result.PSObject.Properties['evaluation']) { Write-Host "Verdict=$($result.evaluation.verdict), outcome delta=$($result.evaluation.outcomeGainPoints), success delta=$($result.evaluation.successRateDeltaPoints)" }
    if ($result.PSObject.Properties['forecast']) { Write-Host "Power forecast=$($result.forecast.planningStatus), cohorts=$($result.forecast.cohortCount), remaining candidate samples=$($result.forecast.remainingCandidateSamples)" }
    if ($result.PSObject.Properties['issues']) { foreach ($issue in @($result.issues)) { Write-Host " - $issue" } }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
