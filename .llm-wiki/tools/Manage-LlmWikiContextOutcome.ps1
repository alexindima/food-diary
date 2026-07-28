[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('observe', 'profile', 'list', 'verify', 'metrics', 'health', 'prune')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$Apply,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$registryPath = Join-Path $wikiRoot 'knowledge/context-strategy-outcomes.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$outcomePolicy = $policy.scheduler.contextBundles.strategyOutcomes

function Get-Hash([object]$Value) {
    if ($null -eq $Value) { $Value = @() }
    $json = ConvertTo-Json -InputObject $Value -Depth 20 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = $Event.schemaVersion
        eventId = $Event.eventId
        workspace = $Event.workspace
        recordedAtUtc = $Event.recordedAtUtc
        completionFingerprint = $Event.completionFingerprint
        retrospectiveHash = $Event.retrospectiveHash
        strategyApplicationHash = $Event.strategyApplicationHash
        strategyState = $Event.strategyState
        variantId = $Event.variantId
        itemLimit = $Event.itemLimit
        characterBudget = $Event.characterBudget
        syntheticQualityScore = $Event.syntheticQualityScore
        taskProfile = $Event.taskProfile
        actualOutcome = $Event.actualOutcome
        success = $Event.success
        policyFingerprint = $Event.policyFingerprint
        previousEventHash = $Event.previousEventHash
    }
}
function Read-Registry {
    if (-not (Test-Path -LiteralPath $registryPath -PathType Leaf)) {
        [pscustomobject][ordered]@{ schemaVersion = 1; events = @() }
    } else {
        Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    }
}
function Normalize-Workspace([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value) -or [IO.Path]::IsPathRooted($Value)) { throw 'WorkspacePath must be repository-relative.' }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') { throw 'WorkspacePath must identify one non-hidden task workspace.' }
    $normalized
}
function Get-TaskProfile([string]$Workspace) {
    $absoluteWorkspace = Join-Path $repositoryRoot $Workspace
    $packetPath = Join-Path $absoluteWorkspace 'change-packet.json'
    if (-not (Test-Path -LiteralPath $packetPath -PathType Leaf)) { throw "Context outcome profile input is absent: $Workspace/change-packet.json" }
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $scopes = @($packet.diff.scopes | Where-Object { $_ } | Sort-Object -Unique)
    $modules = @($packet.diff.modules.name | Where-Object { $_ } | Sort-Object -Unique)
    $changeClass = if ($scopes -contains 'Frontend') { 'frontend' } elseif ($scopes -contains 'Api') { 'api' } elseif ($scopes -contains 'Database') { 'database' } else { 'backend' }
    $riskPath = Join-Path $absoluteWorkspace 'risk-calibration.json'
    $riskLevel = if (Test-Path -LiteralPath $riskPath -PathType Leaf) {
        [string](Get-Content -LiteralPath $riskPath -Raw | ConvertFrom-Json).level
    } elseif ($null -ne $packet.brief.risk.level) { [string]$packet.brief.risk.level } else { 'unknown' }
    if ($riskLevel -notin @('low', 'medium', 'high', 'critical')) { $riskLevel = 'unknown' }
    [pscustomobject][ordered]@{
        cohortKey = "$changeClass|$riskLevel"
        changeClass = $changeClass
        riskLevel = $riskLevel
        scopes = $scopes
        modules = $modules
    }
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([int]$Registry.schemaVersion -ne 1) { $issues.Add('Registry schemaVersion must be 1.') }
    $previous = ''
    $completionIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($event in @($Registry.events)) {
        if ([int]$event.schemaVersion -ne 1) { $issues.Add("Event '$($event.eventId)' schemaVersion must be 1.") }
        if ([string]$event.eventId -notmatch '^[a-f0-9]{32}$') { $issues.Add('Outcome eventId is invalid.') }
        if (-not $completionIds.Add([string]$event.completionFingerprint)) { $issues.Add("Duplicate strategy outcome for completion '$($event.completionFingerprint)'.") }
        if ([string]$event.previousEventHash -cne $previous) { $issues.Add("Outcome chain is invalid at '$($event.eventId)'.") }
        if ([string]$event.policyFingerprint -notmatch '^[a-f0-9]{64}$') { $issues.Add("Outcome policy fingerprint is invalid at '$($event.eventId)'.") }
        if ([double]$event.actualOutcome.score -lt 0 -or [double]$event.actualOutcome.score -gt 100) { $issues.Add("Outcome score is invalid at '$($event.eventId)'.") }
        if ([string]$event.taskProfile.cohortKey -cne "$($event.taskProfile.changeClass)|$($event.taskProfile.riskLevel)") { $issues.Add("Outcome cohort is invalid at '$($event.eventId)'.") }
        if ([string]$event.taskProfile.changeClass -notin @('frontend', 'api', 'database', 'backend')) { $issues.Add("Outcome change class is invalid at '$($event.eventId)'.") }
        if ([string]$event.taskProfile.riskLevel -notin @('low', 'medium', 'high', 'critical', 'unknown')) { $issues.Add("Outcome risk level is invalid at '$($event.eventId)'.") }
        if ([bool]$event.success -ne ([double]$event.actualOutcome.score -ge [double]$outcomePolicy.successScoreThreshold -and [string]$event.strategyState -ne 'rolled-back')) {
            $issues.Add("Outcome success classification is invalid at '$($event.eventId)'.")
        }
        $expectedHash = Get-Hash (Get-EventPayload $event)
        if ([string]$event.eventHash -cne $expectedHash) { $issues.Add("Outcome event hash is invalid at '$($event.eventId)'.") }
        $previous = [string]$event.eventHash
    }
    [pscustomobject][ordered]@{
        valid = $issues.Count -eq 0
        issues = @($issues)
        headHash = $previous
        registryFingerprint = Get-Hash @($Registry.events | ForEach-Object { "$($_.eventId)|$($_.eventHash)" })
    }
}
function Get-VerificationScore([object]$Retrospective) {
    $resolved = [int]$Retrospective.outcome.prediction.resolvedCount
    if ($resolved -eq 0) { return 100.0 }
    $bad = [int]$Retrospective.outcome.prediction.falseNegativeCount + [int]$Retrospective.outcome.prediction.falsePositiveCount
    [Math]::Round([Math]::Max(0, 100.0 * ($resolved - $bad) / $resolved), 2)
}
function New-Outcome([object]$Strategy, [object]$Completion, [object]$Retrospective) {
    $components = [pscustomobject][ordered]@{
        readiness = [double]$Retrospective.outcome.readinessScore
        confidence = [double]$Retrospective.outcome.confidenceScore
        critique = [double]$Retrospective.outcome.critiqueScore
        verification = Get-VerificationScore $Retrospective
    }
    $baseScore = 0.0
    foreach ($property in @($components.PSObject.Properties)) {
        $baseScore += [double]$property.Value * [double]$outcomePolicy.weights.($property.Name) / 100
    }
    $penaltyBreakdown = [pscustomobject][ordered]@{
        failedRepair = [int]$Retrospective.outcome.repair.failedAttempts * [int]$outcomePolicy.penalties.failedRepair
        falseNegative = [int]$Retrospective.outcome.prediction.falseNegativeCount * [int]$outcomePolicy.penalties.falseNegative
        impactDrift = [int]$Retrospective.outcome.impactDriftCount * [int]$outcomePolicy.penalties.impactDrift
        quarantinedContextSource = [int]$Retrospective.outcome.quarantinedContextSourceCount * [int]$outcomePolicy.penalties.quarantinedContextSource
        rolledBack = $(if ([string]$Strategy.state -eq 'rolled-back') { [int]$outcomePolicy.penalties.rolledBack } else { 0 })
    }
    $penalty = [Math]::Min([int]$outcomePolicy.penalties.maximum, [int](($penaltyBreakdown.PSObject.Properties.Value | Measure-Object -Sum).Sum))
    [pscustomobject][ordered]@{
        score = [Math]::Round([Math]::Max(0, [Math]::Min(100, $baseScore - $penalty)), 2)
        baseScore = [Math]::Round($baseScore, 2)
        components = $components
        penalty = $penalty
        penaltyBreakdown = $penaltyBreakdown
        quality = [string]$Retrospective.outcome.quality
        completionVerdict = [string]$Completion.readiness.verdict
    }
}
function Get-ProfileStatistics([object[]]$Events) {
    $ordered = @($Events | Sort-Object { [DateTime]$_.recordedAtUtc })
    $count = $ordered.Count
    $average = [Math]::Round([double](($ordered.actualOutcome.score | Measure-Object -Average).Average), 2)
    $prior = [double]$outcomePolicy.successScoreThreshold
    $priorStrength = [int]$outcomePolicy.priorStrength
    $posterior = [Math]::Round((([double](($ordered.actualOutcome.score | Measure-Object -Sum).Sum)) + ($prior * $priorStrength)) / ($count + $priorStrength), 2)
    $confidence = [Math]::Round(100.0 * $count / ($count + $priorStrength), 2)
    $recent = @($ordered | Select-Object -Last ([Math]::Min($count, [int]$outcomePolicy.recentWindowSamples)))
    $baselineCount = $count - $recent.Count
    $baseline = if ($baselineCount -gt 0) { @($ordered | Select-Object -First $baselineCount) } else { @() }
    $recentAverage = [Math]::Round([double](($recent.actualOutcome.score | Measure-Object -Average).Average), 2)
    $recentSuccessRate = [Math]::Round(100.0 * @($recent | Where-Object success).Count / $recent.Count, 2)
    $baselineAverage = if ($baseline.Count -eq 0) { $null } else { [Math]::Round([double](($baseline.actualOutcome.score | Measure-Object -Average).Average), 2) }
    $drop = if ($null -eq $baselineAverage) { 0.0 } else { [Math]::Round([double]$baselineAverage - $recentAverage, 2) }
    $driftComparable = $recent.Count -ge [int]$outcomePolicy.minimumDriftSamples -and $baseline.Count -ge [int]$outcomePolicy.minimumDriftSamples
    $degradationReasons = [Collections.Generic.List[string]]::new()
    if ($driftComparable -and $drop -gt [double]$outcomePolicy.maximumRecentOutcomeDropPoints) { $degradationReasons.Add('recent-outcome-drop') }
    if ($recent.Count -ge [int]$outcomePolicy.minimumDriftSamples -and $recentSuccessRate -lt [double]$outcomePolicy.minimumRecentSuccessRatePercent) { $degradationReasons.Add('recent-success-rate') }
    $eligible = $count -ge [int]$outcomePolicy.minimumSamples
    $rawAdjustment = [Math]::Round(($posterior - [double]$outcomePolicy.successScoreThreshold) / 5, 2)
    $cap = [double]$outcomePolicy.maximumAbsoluteExperimentAdjustmentPoints
    $adjustment = if ($eligible) { [Math]::Max(-$cap, [Math]::Min($cap, $rawAdjustment)) } else { 0.0 }
    if ($degradationReasons.Count -gt 0) { $adjustment = [Math]::Min(0, $adjustment) }
    [pscustomobject][ordered]@{
        sampleCount = $count
        successCount = @($ordered | Where-Object success).Count
        successRatePercent = [Math]::Round(100.0 * @($ordered | Where-Object success).Count / $count, 2)
        averageOutcomeScore = $average
        posteriorOutcomeScore = $posterior
        confidencePercent = $confidence
        recentSampleCount = $recent.Count
        recentAverageOutcomeScore = $recentAverage
        recentSuccessRatePercent = $recentSuccessRate
        baselineSampleCount = $baseline.Count
        baselineAverageOutcomeScore = $baselineAverage
        recentOutcomeDropPoints = $drop
        driftComparable = $driftComparable
        health = $(if ($degradationReasons.Count -gt 0) { 'degraded' } elseif ($eligible) { 'healthy' } else { 'insufficient-data' })
        degradationReasons = @($degradationReasons)
        rollbackCount = @($ordered | Where-Object strategyState -eq 'rolled-back').Count
        eligible = $eligible
        experimentAdjustmentPoints = $adjustment
    }
}
function Get-Metrics([object]$Registry, [object]$Validation) {
    $profiles = @($Registry.events | Group-Object variantId | ForEach-Object {
        $events = @($_.Group)
        $statistics = Get-ProfileStatistics $events
        $profile = [pscustomobject][ordered]@{
            variantId = [string]$_.Name
            averageSyntheticQualityScore = [Math]::Round([double](($events.syntheticQualityScore | Measure-Object -Average).Average), 2)
        }
        foreach ($property in @($statistics.PSObject.Properties)) { $profile | Add-Member -NotePropertyName $property.Name -NotePropertyValue $property.Value }
        $profile
    } | Sort-Object variantId)
    $cohortProfiles = @($Registry.events | Group-Object { "$($_.variantId)`n$($_.taskProfile.cohortKey)" } | ForEach-Object {
        $events = @($_.Group)
        $statistics = Get-ProfileStatistics $events
        $profile = [pscustomobject][ordered]@{
            variantId = [string]$events[0].variantId
            cohortKey = [string]$events[0].taskProfile.cohortKey
            changeClass = [string]$events[0].taskProfile.changeClass
            riskLevel = [string]$events[0].taskProfile.riskLevel
        }
        foreach ($property in @($statistics.PSObject.Properties)) { $profile | Add-Member -NotePropertyName $property.Name -NotePropertyValue $property.Value }
        $profile
    } | Sort-Object cohortKey, variantId)
    [pscustomobject][ordered]@{
        schemaVersion = 1
        validEventCount = @($Registry.events).Count
        minimumSamples = [int]$outcomePolicy.minimumSamples
        registryFingerprint = [string]$Validation.registryFingerprint
        headHash = [string]$Validation.headHash
        profiles = $profiles
        cohortProfiles = $cohortProfiles
        degradedProfileCount = @($profiles | Where-Object health -eq 'degraded').Count
        degradedCohortProfileCount = @($cohortProfiles | Where-Object health -eq 'degraded').Count
    }
}
function Write-Registry([object]$Registry) {
    [IO.File]::WriteAllText($registryPath, (($Registry | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}

$registry = Read-Registry
$validation = Test-Registry $registry
if ($Action -eq 'observe') {
    if (-not $validation.valid) { throw "Context outcome registry is invalid: $(@($validation.issues) -join ' ')" }
    $workspace = Normalize-Workspace $WorkspacePath
    $absoluteWorkspace = Join-Path $repositoryRoot $workspace
    $required = @('completion.json', 'retrospective.json')
    foreach ($name in $required) {
        if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace $name) -PathType Leaf)) { throw "Context outcome input is absent: $workspace/$name" }
    }
    $strategyPath = Join-Path $absoluteWorkspace 'context-strategy-application.json'
    if (-not (Test-Path -LiteralPath $strategyPath -PathType Leaf)) {
        $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 0; eventHash = ''; reason = 'No context strategy application was used.' }
    } else {
        $strategyValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextStrategy.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
        if (-not $strategyValidation.valid) { throw "Context strategy application is invalid: $(@($strategyValidation.issues) -join ' ')" }
        $completion = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'completion.json') -Raw | ConvertFrom-Json
        $retrospectiveValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
        if (-not $retrospectiveValidation.valid) { throw "Task retrospective is invalid: $(@($retrospectiveValidation.issues) -join ' ')" }
        if ([string]$completion.completionFingerprint -in @($registry.events.completionFingerprint)) {
            $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 0; eventHash = ''; reason = 'Completion outcome was already observed.' }
        } else {
            $strategy = $strategyValidation.strategy
            $actualOutcome = New-Outcome $strategy $completion $retrospectiveValidation.retrospective
            $event = [pscustomobject][ordered]@{
                schemaVersion = 1
                eventId = [guid]::NewGuid().ToString('N')
                workspace = $workspace
                recordedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
                completionFingerprint = [string]$completion.completionFingerprint
                retrospectiveHash = [string]$retrospectiveValidation.retrospective.retrospectiveHash
                strategyApplicationHash = [string]$strategy.applicationHash
                strategyState = [string]$strategy.state
                variantId = [string]$strategy.applied.variantId
                itemLimit = [int]$strategy.applied.itemLimit
                characterBudget = [int]$strategy.applied.characterBudget
                syntheticQualityScore = [double]$strategy.applied.qualityScore
                taskProfile = Get-TaskProfile $workspace
                actualOutcome = $actualOutcome
                success = [bool]($actualOutcome.score -ge [double]$outcomePolicy.successScoreThreshold -and [string]$strategy.state -ne 'rolled-back')
                policyFingerprint = Get-FileSha $policyPath
                previousEventHash = [string]$validation.headHash
                eventHash = ''
            }
            $event.eventHash = Get-Hash (Get-EventPayload $event)
            $registry.events = @($registry.events) + @($event)
            if (@($registry.events).Count -gt [int]$outcomePolicy.maximumEvents) { throw 'Context outcome registry reached maximumEvents; prune it before observing more tasks.' }
            $postValidation = Test-Registry $registry
            if (-not $postValidation.valid) { throw "New context outcome is invalid: $(@($postValidation.issues) -join ' ')" }
            Write-Registry $registry
            $workspaceReceipt = [pscustomobject][ordered]@{
                schemaVersion = 1
                registryEvent = $event
                registryFingerprint = $postValidation.registryFingerprint
            }
            [IO.File]::WriteAllText((Join-Path $absoluteWorkspace 'context-strategy-outcome.json'), (($workspaceReceipt | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
            $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 1; eventHash = $event.eventHash; outcome = $event }
        }
    }
} elseif ($Action -eq 'profile') {
    $profileWorkspace = Normalize-Workspace $WorkspacePath
    $result = [pscustomobject][ordered]@{ action = 'profile'; valid = $true; workspace = $profileWorkspace; profile = Get-TaskProfile $profileWorkspace }
} elseif ($Action -eq 'prune') {
    if (-not $validation.valid) { throw "Context outcome registry is invalid: $(@($validation.issues) -join ' ')" }
    $candidates = [Math]::Max(0, @($registry.events).Count - [int]$outcomePolicy.maximumEvents)
    if ($Apply -and $candidates -gt 0) {
        $kept = @($registry.events | Select-Object -Last ([int]$outcomePolicy.maximumEvents))
        $previous = ''
        foreach ($event in $kept) {
            $event.previousEventHash = $previous
            $event.eventHash = Get-Hash (Get-EventPayload $event)
            $previous = $event.eventHash
        }
        $registry.events = $kept
        Write-Registry $registry
    }
    $result = [pscustomobject][ordered]@{ action = 'prune'; valid = $true; apply = [bool]$Apply; candidateCount = $candidates; changedCount = $(if ($Apply) { $candidates } else { 0 }) }
} elseif ($Action -eq 'metrics') {
    $result = [pscustomobject][ordered]@{ action = 'metrics'; valid = $validation.valid; issues = @($validation.issues); metrics = Get-Metrics $registry $validation }
} elseif ($Action -eq 'health') {
    $metrics = Get-Metrics $registry $validation
    $result = [pscustomobject][ordered]@{
        action = 'health'
        valid = $validation.valid
        issues = @($validation.issues)
        degradedProfileCount = [int]$metrics.degradedProfileCount
        degradedCohortProfileCount = [int]$metrics.degradedCohortProfileCount
        rollbackRecommended = ([int]$metrics.degradedProfileCount + [int]$metrics.degradedCohortProfileCount) -gt 0
        degradedProfiles = @($metrics.profiles | Where-Object health -eq 'degraded')
        degradedCohortProfiles = @($metrics.cohortProfiles | Where-Object health -eq 'degraded')
        registryFingerprint = [string]$metrics.registryFingerprint
    }
} elseif ($Action -eq 'verify') {
    $verifyIssues = [Collections.Generic.List[string]]::new()
    foreach ($issue in @($validation.issues)) { $verifyIssues.Add($issue) }
    $workspaceOutcome = $null
    if (-not [string]::IsNullOrWhiteSpace($WorkspacePath)) {
        if ([IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
        $verifyWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
        if ($verifyWorkspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') { throw 'WorkspacePath must identify one non-hidden task workspace.' }
        $receiptPath = Join-Path $repositoryRoot "$verifyWorkspace/context-strategy-outcome.json"
        if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
            $verifyIssues.Add('context-strategy-outcome.json is absent.')
        } else {
            try {
                $workspaceOutcome = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
                $event = $workspaceOutcome.registryEvent
                $canonical = $registry.events | Where-Object eventId -eq $event.eventId | Select-Object -First 1
                if ($null -eq $canonical) { $verifyIssues.Add('Workspace outcome is absent from the governed registry.') }
                elseif ([string]$canonical.eventHash -cne [string]$event.eventHash) { $verifyIssues.Add('Workspace outcome drifted from the governed registry.') }
                if ([string]$event.workspace -cne $verifyWorkspace) { $verifyIssues.Add('Workspace outcome path does not match.') }
                if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $verifyIssues.Add('Workspace outcome event hash is invalid.') }
            } catch { $verifyIssues.Add($_.Exception.Message) }
        }
    }
    $result = [pscustomobject][ordered]@{
        action = 'verify'
        valid = $verifyIssues.Count -eq 0
        issues = @($verifyIssues)
        registryFingerprint = $validation.registryFingerprint
        headHash = $validation.headHash
        outcome = $workspaceOutcome
    }
} else {
    $result = [pscustomobject][ordered]@{ action = 'list'; valid = $validation.valid; issues = @($validation.issues); totalCount = @($registry.events).Count; events = @($registry.events) }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 20 } else {
    Write-Host "Context strategy outcomes: action=$($result.action), valid=$($result.valid)"
    if ($null -ne $result.metrics) {
        Write-Host "Events=$($result.metrics.validEventCount), fingerprint=$($result.metrics.registryFingerprint)"
        foreach ($profile in @($result.metrics.profiles)) { Write-Host " - $($profile.variantId): samples=$($profile.sampleCount), posterior=$($profile.posteriorOutcomeScore), confidence=$($profile.confidencePercent)%, health=$($profile.health), adjustment=$($profile.experimentAdjustmentPoints)" }
        foreach ($profile in @($result.metrics.cohortProfiles)) { Write-Host " - cohort $($profile.cohortKey)/$($profile.variantId): samples=$($profile.sampleCount), posterior=$($profile.posteriorOutcomeScore), health=$($profile.health), adjustment=$($profile.experimentAdjustmentPoints)" }
    } elseif ($result.action -eq 'health') {
        Write-Host "Degraded profiles=$($result.degradedProfileCount), degraded cohorts=$($result.degradedCohortProfileCount), rollbackRecommended=$($result.rollbackRecommended)"
    } elseif ($null -ne $result.profile) {
        Write-Host "Cohort=$($result.profile.cohortKey), modules=$(@($result.profile.modules) -join ', ')"
    } elseif ($null -ne $result.totalCount) { Write-Host "Events=$($result.totalCount)" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
