[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('observe', 'list', 'verify', 'metrics', 'health')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$registryPath = Join-Path $wikiRoot 'knowledge/model-routing-outcomes.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$outcomePolicy = $policy.scheduler.verificationPlanner.modelRouting.outcomes

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
        schemaVersion = [int]$Event.schemaVersion
        eventId = [string]$Event.eventId
        workspace = [string]$Event.workspace
        recordedAtUtc = ([DateTimeOffset]$Event.recordedAtUtc).ToUniversalTime().ToString('o')
        completionFingerprint = [string]$Event.completionFingerprint
        retrospectiveHash = [string]$Event.retrospectiveHash
        routeReceiptHash = [string]$Event.routeReceiptHash
        routeId = [string]$Event.routeId
        routeRank = [int]$Event.routeRank
        model = [string]$Event.model
        reasoningEffort = [string]$Event.reasoningEffort
        relativeCostUnits = [int]$Event.relativeCostUnits
        complexityScore = [int]$Event.complexityScore
        riskLevel = [string]$Event.riskLevel
        actualOutcome = $Event.actualOutcome
        success = [bool]$Event.success
        policyFingerprint = [string]$Event.policyFingerprint
        previousEventHash = [string]$Event.previousEventHash
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
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([int]$Registry.schemaVersion -ne 1) { $issues.Add('Registry schemaVersion must be 1.') }
    $previous = ''
    $completionIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($event in @($Registry.events)) {
        if ([int]$event.schemaVersion -ne 1) { $issues.Add("Event '$($event.eventId)' schemaVersion must be 1.") }
        if ([string]$event.eventId -notmatch '^[a-f0-9]{32}$') { $issues.Add('Model outcome eventId is invalid.') }
        if (-not $completionIds.Add([string]$event.completionFingerprint)) { $issues.Add("Duplicate model outcome for completion '$($event.completionFingerprint)'.") }
        if ([string]$event.previousEventHash -cne $previous) { $issues.Add("Model outcome chain is invalid at '$($event.eventId)'.") }
        if ([double]$event.actualOutcome.score -lt 0 -or [double]$event.actualOutcome.score -gt 100) { $issues.Add("Model outcome score is invalid at '$($event.eventId)'.") }
        if ([int]$event.routeRank -lt 1) { $issues.Add("Model outcome route rank is invalid at '$($event.eventId)'.") }
        if ([bool]$event.success -ne ([double]$event.actualOutcome.score -ge [double]$outcomePolicy.successScoreThreshold)) {
            $issues.Add("Model outcome success classification is invalid at '$($event.eventId)'.")
        }
        if ([string]$event.policyFingerprint -notmatch '^[a-f0-9]{64}$') { $issues.Add("Model outcome policy fingerprint is invalid at '$($event.eventId)'.") }
        $expectedHash = Get-Hash (Get-EventPayload $event)
        if ([string]$event.eventHash -cne $expectedHash) { $issues.Add("Model outcome event hash is invalid at '$($event.eventId)'.") }
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
function New-Outcome([object]$Completion, [object]$Retrospective) {
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
        flakyCheck = [int]$Retrospective.outcome.flakyCheckCount * [int]$outcomePolicy.penalties.flakyCheck
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
        repairAttempts = [int]$Retrospective.outcome.repair.totalAttempts
    }
}
function Get-ProfileStatistics([object[]]$Events) {
    $ordered = @($Events | Sort-Object { [DateTime]$_.recordedAtUtc })
    $count = $ordered.Count
    $prior = [double]$outcomePolicy.successScoreThreshold
    $priorStrength = [int]$outcomePolicy.priorStrength
    $average = [Math]::Round([double](($ordered.actualOutcome.score | Measure-Object -Average).Average), 2)
    $posterior = [Math]::Round((([double](($ordered.actualOutcome.score | Measure-Object -Sum).Sum)) + ($prior * $priorStrength)) / ($count + $priorStrength), 2)
    $recent = @($ordered | Select-Object -Last ([Math]::Min($count, [int]$outcomePolicy.recentWindowSamples)))
    $baselineCount = $count - $recent.Count
    $baseline = if ($baselineCount -gt 0) { @($ordered | Select-Object -First $baselineCount) } else { @() }
    $recentAverage = [Math]::Round([double](($recent.actualOutcome.score | Measure-Object -Average).Average), 2)
    $recentSuccessRate = [Math]::Round(100.0 * @($recent | Where-Object success).Count / $recent.Count, 2)
    $baselineAverage = if ($baseline.Count -eq 0) { $null } else { [Math]::Round([double](($baseline.actualOutcome.score | Measure-Object -Average).Average), 2) }
    $drop = if ($null -eq $baselineAverage) { 0.0 } else { [Math]::Round([double]$baselineAverage - $recentAverage, 2) }
    $degradationReasons = [Collections.Generic.List[string]]::new()
    if ($recent.Count -ge [int]$outcomePolicy.minimumDriftSamples -and $baseline.Count -ge [int]$outcomePolicy.minimumDriftSamples -and $drop -gt [double]$outcomePolicy.maximumRecentOutcomeDropPoints) {
        $degradationReasons.Add('recent-outcome-drop')
    }
    if ($recent.Count -ge [int]$outcomePolicy.minimumDriftSamples -and $recentSuccessRate -lt [double]$outcomePolicy.minimumRecentSuccessRatePercent) {
        $degradationReasons.Add('recent-success-rate')
    }
    $eligible = $count -ge [int]$outcomePolicy.minimumSamples
    [pscustomobject][ordered]@{
        sampleCount = $count
        successCount = @($ordered | Where-Object success).Count
        successRatePercent = [Math]::Round(100.0 * @($ordered | Where-Object success).Count / $count, 2)
        averageOutcomeScore = $average
        posteriorOutcomeScore = $posterior
        confidencePercent = [Math]::Round(100.0 * $count / ($count + $priorStrength), 2)
        recentSampleCount = $recent.Count
        recentAverageOutcomeScore = $recentAverage
        recentSuccessRatePercent = $recentSuccessRate
        baselineSampleCount = $baseline.Count
        baselineAverageOutcomeScore = $baselineAverage
        recentOutcomeDropPoints = $drop
        averageRepairAttempts = [Math]::Round([double](($ordered.actualOutcome.repairAttempts | Measure-Object -Average).Average), 2)
        eligible = $eligible
        health = $(if ($eligible -and $degradationReasons.Count -gt 0) { 'degraded' } elseif ($eligible) { 'healthy' } else { 'insufficient-data' })
        degradationReasons = @($degradationReasons)
    }
}
function Get-Metrics([object]$Registry, [object]$Validation) {
    $profiles = @($Registry.events | Group-Object routeId | ForEach-Object {
        $events = @($_.Group)
        $statistics = Get-ProfileStatistics $events
        $profile = [pscustomobject][ordered]@{
            routeId = [string]$_.Name
            routeRank = [int]$events[0].routeRank
            model = [string]$events[0].model
            reasoningEffort = [string]$events[0].reasoningEffort
            relativeCostUnits = [int]$events[0].relativeCostUnits
        }
        foreach ($property in @($statistics.PSObject.Properties)) { $profile | Add-Member -NotePropertyName $property.Name -NotePropertyValue $property.Value }
        $profile
    } | Sort-Object routeRank)
    [pscustomobject][ordered]@{
        schemaVersion = 1
        validEventCount = @($Registry.events).Count
        minimumSamples = [int]$outcomePolicy.minimumSamples
        registryFingerprint = [string]$Validation.registryFingerprint
        headHash = [string]$Validation.headHash
        profiles = $profiles
        degradedProfileCount = @($profiles | Where-Object health -eq 'degraded').Count
    }
}
function Write-Registry([object]$Registry) {
    [IO.File]::WriteAllText($registryPath, (($Registry | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}

$registry = Read-Registry
$validation = Test-Registry $registry
if ($Action -eq 'observe') {
    if (-not $validation.valid) { throw "Model outcome registry is invalid: $(@($validation.issues) -join ' ')" }
    $workspace = Normalize-Workspace $WorkspacePath
    $absoluteWorkspace = Join-Path $repositoryRoot $workspace
    foreach ($name in @('completion.json', 'retrospective.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace $name) -PathType Leaf)) { throw "Model outcome input is absent: $workspace/$name" }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace 'model-routing.json') -PathType Leaf)) {
        $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 0; eventHash = ''; reason = 'No governed model route was used.' }
    } else {
    $routeValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRouting.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if (-not $routeValidation.valid) { throw "Model route is invalid: $(@($routeValidation.issues) -join ' ')" }
    $completion = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'completion.json') -Raw | ConvertFrom-Json
    $retrospectiveValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if (-not $retrospectiveValidation.valid) { throw "Task retrospective is invalid: $(@($retrospectiveValidation.issues) -join ' ')" }
    if ([string]$completion.completionFingerprint -in @($registry.events | ForEach-Object { [string]$_.completionFingerprint })) {
        $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 0; eventHash = ''; reason = 'Completion outcome was already observed.' }
    } else {
        $route = $routeValidation.route
        $actualOutcome = New-Outcome $completion $retrospectiveValidation.retrospective
        $event = [pscustomobject][ordered]@{
            schemaVersion = 1
            eventId = [guid]::NewGuid().ToString('N')
            workspace = $workspace
            recordedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
            completionFingerprint = [string]$completion.completionFingerprint
            retrospectiveHash = [string]$retrospectiveValidation.retrospective.retrospectiveHash
            routeReceiptHash = [string]$route.receiptHash
            routeId = [string]$route.recommendation.routeId
            routeRank = [int]$route.recommendation.rank
            model = [string]$route.recommendation.model
            reasoningEffort = [string]$route.recommendation.reasoningEffort
            relativeCostUnits = [int]$route.recommendation.relativeCostUnits
            complexityScore = [int]$route.signals.complexityScore
            riskLevel = [string]$route.signals.riskLevel
            actualOutcome = $actualOutcome
            success = [bool]($actualOutcome.score -ge [double]$outcomePolicy.successScoreThreshold)
            policyFingerprint = Get-FileSha $policyPath
            previousEventHash = [string]$validation.headHash
            eventHash = ''
        }
        $event.eventHash = Get-Hash (Get-EventPayload $event)
        $registry.events = @($registry.events) + @($event)
        if (@($registry.events).Count -gt [int]$outcomePolicy.maximumEvents) { throw 'Model outcome registry reached maximumEvents.' }
        $postValidation = Test-Registry $registry
        if (-not $postValidation.valid) { throw "New model outcome is invalid: $(@($postValidation.issues) -join ' ')" }
        Write-Registry $registry
        [IO.File]::WriteAllText(
            (Join-Path $absoluteWorkspace 'model-routing-outcome.json'),
            (([pscustomobject][ordered]@{ schemaVersion = 1; registryEvent = $event; registryFingerprint = $postValidation.registryFingerprint } | ConvertTo-Json -Depth 20) + [Environment]::NewLine),
            [Text.UTF8Encoding]::new($false)
        )
        $result = [pscustomobject][ordered]@{ action = 'observe'; valid = $true; addedCount = 1; eventHash = $event.eventHash; outcome = $event }
    }
    }
} elseif ($Action -eq 'metrics') {
    $result = [pscustomobject][ordered]@{ action = 'metrics'; valid = $validation.valid; issues = @($validation.issues); metrics = Get-Metrics $registry $validation }
} elseif ($Action -eq 'health') {
    $metrics = Get-Metrics $registry $validation
    $result = [pscustomobject][ordered]@{
        action = 'health'; valid = $validation.valid; issues = @($validation.issues)
        degradedProfileCount = [int]$metrics.degradedProfileCount
        escalationRecommended = [int]$metrics.degradedProfileCount -gt 0
        degradedProfiles = @($metrics.profiles | Where-Object health -eq 'degraded')
        registryFingerprint = [string]$metrics.registryFingerprint
    }
} elseif ($Action -eq 'verify') {
    $verifyIssues = [Collections.Generic.List[string]]::new()
    foreach ($issue in @($validation.issues)) { $verifyIssues.Add($issue) }
    $workspaceOutcome = $null
    if (-not [string]::IsNullOrWhiteSpace($WorkspacePath)) {
        $workspace = Normalize-Workspace $WorkspacePath
        $absoluteWorkspace = Join-Path $repositoryRoot $workspace
        $workspaceReceiptPath = Join-Path $absoluteWorkspace 'model-routing-outcome.json'
        if (-not (Test-Path -LiteralPath $workspaceReceiptPath -PathType Leaf)) {
            $verifyIssues.Add('model-routing-outcome.json is absent.')
        } else {
            $workspaceOutcome = Get-Content -LiteralPath $workspaceReceiptPath -Raw | ConvertFrom-Json
            $matching = $registry.events | Where-Object eventHash -eq $workspaceOutcome.registryEvent.eventHash | Select-Object -First 1
            if ($null -eq $matching -or (Get-Hash (Get-EventPayload $matching)) -cne (Get-Hash (Get-EventPayload $workspaceOutcome.registryEvent))) {
                $verifyIssues.Add('Workspace model outcome does not match the registry.')
            }
        }
    }
    $result = [pscustomobject][ordered]@{
        action = 'verify'; valid = $verifyIssues.Count -eq 0; issues = @($verifyIssues)
        registryFingerprint = $validation.registryFingerprint; headHash = $validation.headHash; outcome = $workspaceOutcome
    }
} else {
    $result = [pscustomobject][ordered]@{
        action = 'list'; valid = $validation.valid; issues = @($validation.issues)
        registryFingerprint = $validation.registryFingerprint; events = @($registry.events)
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Model outcomes: action=$Action, valid=$($result.valid), registry=$($validation.registryFingerprint)"
    if ($null -ne $result.metrics) {
        foreach ($profile in @($result.metrics.profiles)) { Write-Host " - $($profile.routeId): samples=$($profile.sampleCount), score=$($profile.posteriorOutcomeScore), health=$($profile.health)" }
    }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
