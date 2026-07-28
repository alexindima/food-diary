[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('create', 'show', 'verify')]
    [string]$Action = 'show',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$routingPolicy = $policy.scheduler.verificationPlanner.modelRouting
$outcomePolicy = $routingPolicy.outcomes
$optimizationPolicy = $routingPolicy.optimization
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') {
    throw 'WorkspacePath must identify one non-hidden task workspace.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$packetPath = Join-Path $absoluteWorkspace 'change-packet.json'
$receiptPath = Join-Path $absoluteWorkspace 'model-routing.json'
foreach ($name in @('change-packet.json', 'risk-calibration.json', 'failure-prediction.json', 'verification-plan.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace $name) -PathType Leaf)) {
        throw "Model routing input is absent: $workspace/$name"
    }
}

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = $Receipt.schemaVersion
        workspace = $Receipt.workspace
        createdAtUtc = $Receipt.createdAtUtc
        policyFingerprint = $Receipt.policyFingerprint
        generatorFingerprint = $Receipt.generatorFingerprint
        inputs = $Receipt.inputs
        signals = $Receipt.signals
        alternatives = @($Receipt.alternatives)
        recommendation = $Receipt.recommendation
    }
}
function Get-Current {
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $risk = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRiskCalibration.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $prediction = & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $verification = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationPlan.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $outcomes = & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRoutingOutcome.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $risk.valid) { throw "Risk calibration is invalid: $(@($risk.issues) -join ' ')" }
    if (-not $prediction.valid) { throw "Failure prediction is invalid: $(@($prediction.issues) -join ' ')" }
    if (-not $verification.valid) { throw "Verification plan is invalid: $(@($verification.issues) -join ' ')" }
    if (-not $outcomes.valid) { throw "Model routing outcome history is invalid: $(@($outcomes.issues) -join ' ')" }
    [pscustomobject]@{ packet = $packet; risk = $risk; prediction = $prediction; verification = $verification; outcomes = $outcomes }
}
function Get-CanonicalRoute([object]$Current) {
    $scopes = @($Current.packet.brief.change.scopes | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $reviewIds = @($Current.packet.policy.reviewObligations.id)
    $maximumFailure = [int](($Current.prediction.prediction.predictions | Measure-Object probabilityPercent -Maximum).Maximum)
    $executionCount = @($Current.verification.plan.executions).Count
    $failurePoints = [int][Math]::Round($maximumFailure * [double]$routingPolicy.predictedFailureWeightPercent / 100)
    $executionPoints = [Math]::Min(
        [int]$routingPolicy.maximumVerificationExecutionPoints,
        $executionCount * [int]$routingPolicy.verificationExecutionPoints
    )
    $scopePoints = [Math]::Min(
        [int]$routingPolicy.maximumScopePoints,
        [Math]::Max(0, $scopes.Count - 1) * [int]$routingPolicy.additionalScopePoints
    )
    $securityPoints = if (@($reviewIds | Where-Object { $_ -match '(?i)security|privacy|auth' }).Count -gt 0 -or $scopes -contains 'Security') { [int]$routingPolicy.securityReviewPoints } else { 0 }
    $databasePoints = if ($scopes -contains 'Database') { [int]$routingPolicy.databaseScopePoints } else { 0 }
    $apiPoints = if ($scopes -contains 'Api' -or $scopes -contains 'Contracts') { [int]$routingPolicy.apiScopePoints } else { 0 }
    $complexityScore = [Math]::Min(
        [int]$routingPolicy.maximumComplexityScore,
        [int]$Current.risk.calibration.score + $failurePoints + $executionPoints + $scopePoints + $securityPoints + $databasePoints + $apiPoints
    )
    $riskLevel = [string]$Current.risk.calibration.level
    $riskFloorRank = [int]$routingPolicy.riskFloorRank.$riskLevel
    $complexityRoute = $routingPolicy.routes |
        Where-Object { [int]$_.minimumComplexityScore -le $complexityScore } |
        Sort-Object rank -Descending |
        Select-Object -First 1
    $baseRequiredRank = [Math]::Max([int]$complexityRoute.rank, $riskFloorRank)
    $baseRoute = $routingPolicy.routes | Where-Object { [int]$_.rank -eq $baseRequiredRank } | Select-Object -First 1
    $outcomeProfile = $Current.outcomes.metrics.profiles | Where-Object routeId -eq $baseRoute.id | Select-Object -First 1
    $outcomeHealth = if ($null -eq $outcomeProfile) { 'insufficient-data' } else { [string]$outcomeProfile.health }
    $healthEscalated = [bool]($outcomePolicy.escalateDegradedRoutes -and $outcomeHealth -eq 'degraded' -and $baseRequiredRank -lt @($routingPolicy.routes).Count)
    $healthRequiredRank = if ($healthEscalated) { $baseRequiredRank + 1 } else { $baseRequiredRank }
    $maximumCandidateRank = [Math]::Min(
        @($routingPolicy.routes).Count,
        $healthRequiredRank + [int]$optimizationPolicy.maximumEscalationRanks
    )
    $maximumCost = [double](($routingPolicy.routes | Measure-Object relativeCostUnits -Maximum).Maximum)
    $minimumCost = [double](($routingPolicy.routes | Measure-Object relativeCostUnits -Minimum).Minimum)
    $costRange = [Math]::Max(1.0, $maximumCost - $minimumCost)
    $baseQuality = if ($null -eq $outcomeProfile -or [int]$outcomeProfile.sampleCount -lt [int]$optimizationPolicy.minimumSamplesPerRoute) {
        $null
    } else {
        [double]$outcomeProfile.posteriorOutcomeScore
    }
    $candidateScores = @($routingPolicy.routes | Sort-Object rank | ForEach-Object {
        $candidate = $_
        $profile = $Current.outcomes.metrics.profiles | Where-Object routeId -eq $candidate.id | Select-Object -First 1
        $sampleCount = if ($null -eq $profile) { 0 } else { [int]$profile.sampleCount }
        $quality = if ($null -eq $profile) { $null } else { [double]$profile.posteriorOutcomeScore }
        $qualityGain = if ($null -eq $baseQuality -or $null -eq $quality) { $null } else { [Math]::Round($quality - $baseQuality, 2) }
        $costScore = [Math]::Round(100.0 * ($maximumCost - [double]$candidate.relativeCostUnits) / $costRange, 2)
        $utility = if ($null -eq $quality) { $null } else {
            [Math]::Round(
                ($quality * [double]$optimizationPolicy.qualityWeightPercent / 100.0) +
                ($costScore * [double]$optimizationPolicy.costWeightPercent / 100.0),
                2
            )
        }
        $optimizationEligible = [bool](
            $optimizationPolicy.enabled -and
            [int]$candidate.rank -ge $healthRequiredRank -and
            [int]$candidate.rank -le $maximumCandidateRank -and
            $sampleCount -ge [int]$optimizationPolicy.minimumSamplesPerRoute -and
            [string]$profile.health -eq 'healthy' -and
            ($candidate.id -eq $baseRoute.id -or ($null -ne $qualityGain -and $qualityGain -ge [double]$optimizationPolicy.minimumQualityGainPoints))
        )
        [pscustomobject][ordered]@{
            routeId = [string]$candidate.id
            sampleCount = $sampleCount
            health = $(if ($null -eq $profile) { 'insufficient-data' } else { [string]$profile.health })
            posteriorOutcomeScore = $quality
            qualityGainPoints = $qualityGain
            costScore = $costScore
            utilityScore = $utility
            optimizationEligible = $optimizationEligible
        }
    })
    $selectedScore = $candidateScores |
        Where-Object optimizationEligible |
        Sort-Object @{ Expression = 'utilityScore'; Descending = $true }, @{ Expression = 'routeId'; Descending = $false } |
        Select-Object -First 1
    $optimized = [bool]($null -ne $selectedScore -and $selectedScore.routeId -ne $baseRoute.id -and [int](($routingPolicy.routes | Where-Object id -eq $selectedScore.routeId).rank) -ge $healthRequiredRank)
    $requiredRank = if ($null -ne $selectedScore) {
        [int](($routingPolicy.routes | Where-Object id -eq $selectedScore.routeId | Select-Object -First 1).rank)
    } else {
        $healthRequiredRank
    }
    $selected = $routingPolicy.routes | Where-Object { [int]$_.rank -eq $requiredRank } | Select-Object -First 1
    $signals = [pscustomobject][ordered]@{
        complexityScore = $complexityScore
        riskScore = [int]$Current.risk.calibration.score
        riskLevel = $riskLevel
        riskFloorRank = $riskFloorRank
        baseRequiredRank = $baseRequiredRank
        outcomeRegistryFingerprint = [string]$Current.outcomes.metrics.registryFingerprint
        outcomeHealth = $outcomeHealth
        outcomeSampleCount = $(if ($null -eq $outcomeProfile) { 0 } else { [int]$outcomeProfile.sampleCount })
        healthEscalated = $healthEscalated
        optimizationEnabled = [bool]$optimizationPolicy.enabled
        optimizationApplied = $optimized
        optimizationMaximumCandidateRank = $maximumCandidateRank
        optimizationQualityWeightPercent = [int]$optimizationPolicy.qualityWeightPercent
        optimizationCostWeightPercent = [int]$optimizationPolicy.costWeightPercent
        maximumPredictedFailurePercent = $maximumFailure
        predictedFailurePoints = $failurePoints
        verificationExecutionCount = $executionCount
        verificationExecutionPoints = $executionPoints
        scopes = $scopes
        scopeBreadthPoints = $scopePoints
        securityPoints = $securityPoints
        databasePoints = $databasePoints
        apiPoints = $apiPoints
    }
    $alternatives = @($routingPolicy.routes | Sort-Object rank | ForEach-Object {
        $blocks = @()
        if ([int]$_.rank -lt $riskFloorRank) { $blocks += "below-$riskLevel-risk-floor" }
        if ([int]$_.rank -lt [int]$complexityRoute.rank) { $blocks += 'below-complexity-floor' }
        if ($healthEscalated -and [int]$_.rank -eq $baseRequiredRank) { $blocks += 'degraded-route-history' }
        $candidateScore = $candidateScores | Where-Object routeId -eq $_.id | Select-Object -First 1
        if ([int]$_.rank -gt $maximumCandidateRank) { $blocks += 'above-optimization-escalation-cap' }
        $optimizationBlocks = @()
        if ([int]$_.rank -lt $healthRequiredRank) { $optimizationBlocks += 'below-required-rank' }
        if ([int]$_.rank -gt $maximumCandidateRank) { $optimizationBlocks += 'above-escalation-cap' }
        if ([int]$candidateScore.sampleCount -lt [int]$optimizationPolicy.minimumSamplesPerRoute) { $optimizationBlocks += 'insufficient-outcome-samples' }
        if ([string]$candidateScore.health -ne 'healthy') { $optimizationBlocks += "outcome-health-$($candidateScore.health)" }
        if ([int]$_.rank -gt $baseRequiredRank -and ($null -eq $candidateScore.qualityGainPoints -or [double]$candidateScore.qualityGainPoints -lt [double]$optimizationPolicy.minimumQualityGainPoints)) { $optimizationBlocks += 'insufficient-quality-gain' }
        [pscustomobject][ordered]@{
            id = [string]$_.id
            rank = [int]$_.rank
            model = [string]$_.model
            reasoningEffort = [string]$_.reasoningEffort
            relativeCostUnits = [int]$_.relativeCostUnits
            eligible = @($blocks).Count -eq 0
            blocks = @($blocks)
            outcomeSampleCount = [int]$candidateScore.sampleCount
            outcomeHealth = [string]$candidateScore.health
            posteriorOutcomeScore = $candidateScore.posteriorOutcomeScore
            qualityGainPoints = $candidateScore.qualityGainPoints
            costScore = [double]$candidateScore.costScore
            utilityScore = $candidateScore.utilityScore
            optimizationEligible = [bool]$candidateScore.optimizationEligible
            optimizationBlocks = @($optimizationBlocks)
        }
    })
    $recommendation = [pscustomobject][ordered]@{
        routeId = [string]$selected.id
        rank = [int]$selected.rank
        model = [string]$selected.model
        reasoningEffort = [string]$selected.reasoningEffort
        relativeCostUnits = [int]$selected.relativeCostUnits
        rationale = $(if ($optimized) {
            "Selected route '$($selected.id)' from governed quality-cost optimization: sufficient healthy evidence, at least $($optimizationPolicy.minimumQualityGainPoints) quality-gain points, and the best eligible utility score; complexity and $riskLevel-risk floors remain enforced."
        } elseif ($healthEscalated) {
            "Escalated one rank because route '$($baseRoute.id)' has degraded real-task outcomes; still satisfies complexity score $complexityScore and $riskLevel-risk floor rank $riskFloorRank."
        } else {
            "Minimum governed route satisfying complexity score $complexityScore and $riskLevel-risk floor rank $riskFloorRank."
        })
    }
    [pscustomobject]@{ signals = $signals; alternatives = $alternatives; recommendation = $recommendation }
}
function New-Receipt([string]$CreatedAtUtc) {
    $current = Get-Current
    $canonical = Get-CanonicalRoute $current
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $workspace
        createdAtUtc = $CreatedAtUtc
        policyFingerprint = Get-FileSha $policyPath
        generatorFingerprint = Get-FileSha $PSCommandPath
        inputs = [pscustomobject][ordered]@{
            packetFingerprint = [string]$current.packet.fingerprint
            riskCalibrationHash = [string]$current.risk.calibration.calibrationHash
            failurePredictionHash = [string]$current.prediction.prediction.predictionHash
            verificationPlanHash = [string]$current.verification.plan.planHash
            outcomeRegistryFingerprint = [string]$current.outcomes.metrics.registryFingerprint
        }
        signals = $canonical.signals
        alternatives = @($canonical.alternatives)
        recommendation = $canonical.recommendation
        receiptHash = ''
    }
    $receipt.receiptHash = Get-Hash (Get-Payload $receipt)
    $receipt
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    $current = Get-Current
    $canonical = Get-CanonicalRoute $current
    if ([int]$Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $workspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Receipt.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Model routing policy drifted.') }
    if ([string]$Receipt.generatorFingerprint -cne (Get-FileSha $PSCommandPath)) { $issues.Add('Model routing generator changed.') }
    if ([string]$Receipt.inputs.packetFingerprint -cne [string]$current.packet.fingerprint) { $issues.Add('Task packet drifted.') }
    if ([string]$Receipt.inputs.riskCalibrationHash -cne [string]$current.risk.calibration.calibrationHash) { $issues.Add('Risk calibration drifted.') }
    if ([string]$Receipt.inputs.failurePredictionHash -cne [string]$current.prediction.prediction.predictionHash) { $issues.Add('Failure prediction drifted.') }
    if ([string]$Receipt.inputs.verificationPlanHash -cne [string]$current.verification.plan.planHash) { $issues.Add('Verification plan drifted.') }
    if ([string]$Receipt.inputs.outcomeRegistryFingerprint -cne [string]$current.outcomes.metrics.registryFingerprint) { $issues.Add('Model routing outcome history drifted.') }
    if ((Get-Hash $Receipt.signals) -cne (Get-Hash $canonical.signals)) { $issues.Add('Model routing signals are invalid.') }
    if ((Get-Hash @($Receipt.alternatives)) -cne (Get-Hash @($canonical.alternatives))) { $issues.Add('Model routing alternatives are invalid.') }
    if ((Get-Hash $Receipt.recommendation) -cne (Get-Hash $canonical.recommendation)) { $issues.Add('Model routing recommendation is not canonical.') }
    if ([int]$Receipt.recommendation.rank -lt [int]$Receipt.signals.riskFloorRank) { $issues.Add('Model routing recommendation violates the risk floor.') }
    if ([string]$Receipt.receiptHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Model routing receipt hash is invalid.') }
    @($issues)
}

$receipt = $null
$issues = @()
$savedPath = $null
if ($Action -eq 'create') {
    $receipt = New-Receipt ($AsOfUtc.ToUniversalTime().ToString('o'))
    $issues = @(Test-Receipt $receipt)
    if ($issues.Count -eq 0) {
        [IO.File]::WriteAllText($receiptPath, (($receipt | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        $savedPath = "$workspace/model-routing.json"
    }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        $issues = @('model-routing.json is absent.')
    } else {
        try {
            $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
            $issues = @(Test-Receipt $receipt)
        } catch { $issues = @($_.Exception.Message) }
    }
}
$valid = $issues.Count -eq 0
$result = [pscustomobject][ordered]@{ action = $Action; valid = $valid; issues = @($issues); route = $receipt; savedPath = $savedPath }
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 30
} else {
    Write-Host "Model route: action=$Action, valid=$valid"
    if ($null -ne $receipt) {
        Write-Host "Route=$($receipt.recommendation.routeId), model=$($receipt.recommendation.model), effort=$($receipt.recommendation.reasoningEffort), complexity=$($receipt.signals.complexityScore), risk=$($receipt.signals.riskLevel), hash=$($receipt.receiptHash)"
        foreach ($alternative in @($receipt.alternatives)) { Write-Host " - $($alternative.id): eligible=$($alternative.eligible), blocks=$(@($alternative.blocks) -join ',')" }
    }
    foreach ($issue in @($issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $valid) { exit 1 }
