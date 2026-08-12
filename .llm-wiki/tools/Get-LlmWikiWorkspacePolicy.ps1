[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('get', 'validate')]
    [string]$Action = 'get',
    [string]$Path = '.llm-wiki/policies/workspace-policies.json',
    [switch]$WithFingerprint,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
if ([System.IO.Path]::IsPathRooted($Path)) { throw 'Policy path must be repository-relative.' }
$normalizedPath = $Path.Replace('\', '/')
if ($normalizedPath -notmatch '^\.llm-wiki/policies/[^/]+\.json$|^\.artifacts/llm-wiki/[^/]+\.json$') {
    throw 'Policy path must be a JSON file directly inside .llm-wiki/policies or .artifacts/llm-wiki.'
}
$absolutePath = Join-Path $repositoryRoot $normalizedPath
$issues = [System.Collections.Generic.List[string]]::new()
$policy = $null
if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
    $issues.Add('Workspace policy file is absent.')
} else {
    try {
        $policy = Get-Content -LiteralPath $absolutePath -Raw | ConvertFrom-Json
    } catch {
        $issues.Add("Workspace policy is invalid JSON: $($_.Exception.Message)")
    }
}

function Add-Issue([bool]$Condition, [string]$Message) {
    if (-not $Condition) { $issues.Add($Message) }
}
function Test-Regex([string]$Pattern, [string]$Label) {
    if ([string]::IsNullOrWhiteSpace($Pattern)) {
        $issues.Add("$Label is empty.")
        return
    }
    try { $null = [regex]::new($Pattern) } catch { $issues.Add("$Label is invalid: $($_.Exception.Message)") }
}
function Get-Fingerprint([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}

if ($null -ne $policy) {
    Add-Issue ($policy.schemaVersion -eq 1) 'schemaVersion must be 1.'
    Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$policy.workspace.format)) 'workspace.format is required.'
    Add-Issue ($policy.workspace.latestSchemaVersion -eq 4) 'workspace.latestSchemaVersion must remain 4 until a new migration step is implemented.'
    $requiredArtifacts = @('packet', 'taskContract', 'manifest', 'acceptance', 'evidence', 'journal', 'report')
    foreach ($artifact in $requiredArtifacts) {
        Add-Issue ($policy.workspace.artifactSchemaVersions.($artifact) -ge 1) "workspace.artifactSchemaVersions.$artifact must be positive."
    }
    $actualArtifacts = @($policy.workspace.artifactSchemaVersions.PSObject.Properties.Name)
    foreach ($extra in @($actualArtifacts | Where-Object { $_ -notin $requiredArtifacts })) {
        $issues.Add("Unknown artifact schema key: $extra")
    }
    Add-Issue ($policy.audit.staleAfterDays -ge 1) 'audit.staleAfterDays must be positive.'
    Add-Issue ($policy.audit.evidenceMaxAgeDays -ge 1) 'audit.evidenceMaxAgeDays must be positive.'
    Add-Issue ($policy.audit.maximumDays -ge $policy.audit.staleAfterDays) 'audit.maximumDays must cover staleAfterDays.'
    Add-Issue ($policy.audit.maximumDays -ge $policy.audit.evidenceMaxAgeDays) 'audit.maximumDays must cover evidenceMaxAgeDays.'
    Add-Issue ($policy.planConformance.maximumUnplannedAllowedPaths -ge 0 -and $policy.planConformance.maximumUnplannedAllowedPaths -le 10000) 'planConformance.maximumUnplannedAllowedPaths must be between 0 and 10000.'
    Add-Issue ($policy.planConformance.maximumMissingPlannedPaths -ge 0 -and $policy.planConformance.maximumMissingPlannedPaths -le 10000) 'planConformance.maximumMissingPlannedPaths must be between 0 and 10000.'
    foreach ($property in @('blockOutOfScope', 'blockNewChecks', 'blockNewReviews')) {
        Add-Issue ($policy.planConformance.$property -is [bool]) "planConformance.$property must be boolean."
    }
    Add-Issue ($policy.proofOfChange.minimumChangedPathsPerSatisfiedCriterion -ge 1 -and $policy.proofOfChange.minimumChangedPathsPerSatisfiedCriterion -le 1000) 'proofOfChange.minimumChangedPathsPerSatisfiedCriterion must be between 1 and 1000.'
    foreach ($property in @('requireVerifiedEvidencePerSatisfiedCriterion', 'requireMappedPathsInCurrentDiff', 'requireMappedTestPathsToExist')) {
        Add-Issue ($policy.proofOfChange.$property -is [bool]) "proofOfChange.$property must be boolean."
    }
    Add-Issue ($policy.requirementModel.minimumCriterionWords -ge 1 -and $policy.requirementModel.minimumCriterionWords -le 50) 'requirementModel.minimumCriterionWords must be between 1 and 50.'
    Add-Issue ($policy.requirementModel.maximumCompoundConnectors -ge 0 -and $policy.requirementModel.maximumCompoundConnectors -le 20) 'requirementModel.maximumCompoundConnectors must be between 0 and 20.'
    Add-Issue ($policy.requirementModel.duplicateSimilarityPercent -ge 1 -and $policy.requirementModel.duplicateSimilarityPercent -le 100) 'requirementModel.duplicateSimilarityPercent must be between 1 and 100.'
    Add-Issue ($policy.requirementModel.maximumRecommendations -ge 1 -and $policy.requirementModel.maximumRecommendations -le 100) 'requirementModel.maximumRecommendations must be between 1 and 100.'
    foreach ($property in @('blockVagueCriteria', 'blockCompoundCriteria')) {
        Add-Issue ($policy.requirementModel.$property -is [bool]) "requirementModel.$property must be boolean."
    }
    foreach ($property in @(
        'maximumUnexpectedScopes',
        'maximumUnexpectedModules',
        'maximumUnexpectedContracts',
        'maximumUnexpectedConsumers',
        'maximumUnexpectedRuntimeBindings',
        'maximumUnexpectedDataBindings',
        'maximumUnexpectedFrontendBindings'
    )) {
        Add-Issue ($policy.impactSimulation.$property -ge 0 -and $policy.impactSimulation.$property -le 10000) "impactSimulation.$property must be between 0 and 10000."
    }
    Add-Issue ($policy.repairLoop.maximumRepairPaths -ge 1 -and $policy.repairLoop.maximumRepairPaths -le 100) 'repairLoop.maximumRepairPaths must be between 1 and 100.'
    Add-Issue ($policy.repairLoop.maximumAttemptsPerFailure -ge 1 -and $policy.repairLoop.maximumAttemptsPerFailure -le 100) 'repairLoop.maximumAttemptsPerFailure must be between 1 and 100.'
    Add-Issue ($policy.repairLoop.maximumRepeatedAttemptFingerprint -ge 1 -and $policy.repairLoop.maximumRepeatedAttemptFingerprint -le $policy.repairLoop.maximumAttemptsPerFailure) 'repairLoop.maximumRepeatedAttemptFingerprint must fit maximumAttemptsPerFailure.'
    Add-Issue ($policy.repairLoop.maximumTotalAttempts -ge $policy.repairLoop.maximumAttemptsPerFailure -and $policy.repairLoop.maximumTotalAttempts -le 10000) 'repairLoop.maximumTotalAttempts must cover per-failure attempts and not exceed 10000.'
    Add-Issue ($policy.repairLoop.learningMinimumConfidence -ge 1 -and $policy.repairLoop.learningMinimumConfidence -le 100) 'repairLoop.learningMinimumConfidence must be between 1 and 100.'
    Add-Issue ($policy.repairLoop.learningMaximumMatches -ge 1 -and $policy.repairLoop.learningMaximumMatches -le 100) 'repairLoop.learningMaximumMatches must be between 1 and 100.'
    $repairCategories = @($policy.repairLoop.allowedCategories)
    Add-Issue (@($repairCategories).Count -gt 0 -and @($repairCategories).Count -eq @($repairCategories | Sort-Object -Unique).Count) 'repairLoop.allowedCategories must be non-empty and unique.'
    Add-Issue ($policy.scheduler.defaultConcurrency -ge 1) 'scheduler.defaultConcurrency must be positive.'
    Add-Issue ($policy.scheduler.maximumConcurrency -ge $policy.scheduler.defaultConcurrency -and $policy.scheduler.maximumConcurrency -le 32) 'scheduler.maximumConcurrency must cover the default and not exceed 32.'
    Add-Issue ($policy.scheduler.defaultLeaseMinutes -ge 1) 'scheduler.defaultLeaseMinutes must be positive.'
    Add-Issue ($policy.scheduler.maximumLeaseMinutes -ge $policy.scheduler.defaultLeaseMinutes -and $policy.scheduler.maximumLeaseMinutes -le 1440) 'scheduler.maximumLeaseMinutes must cover the default and not exceed one day.'
    Add-Issue ($policy.scheduler.terminalReceiptRetentionDays -ge 1) 'scheduler.terminalReceiptRetentionDays must be positive.'
    Add-Issue ($policy.scheduler.maximumReceiptRetentionDays -ge $policy.scheduler.terminalReceiptRetentionDays -and $policy.scheduler.maximumReceiptRetentionDays -le 36500) 'scheduler.maximumReceiptRetentionDays must cover terminalReceiptRetentionDays and not exceed 100 years.'
    Add-Issue ($policy.scheduler.defaultMetricsWindowDays -ge 1 -and $policy.scheduler.defaultMetricsWindowDays -le $policy.scheduler.terminalReceiptRetentionDays) 'scheduler.defaultMetricsWindowDays must fit terminalReceiptRetentionDays.'
    Add-Issue ($policy.scheduler.slo.minimumTerminalSamples -ge 1 -and $policy.scheduler.slo.minimumTerminalSamples -le 10000) 'scheduler.slo.minimumTerminalSamples must be between 1 and 10000.'
    Add-Issue ($policy.scheduler.slo.minimumSuccessRatePercent -ge 0 -and $policy.scheduler.slo.minimumSuccessRatePercent -le 100) 'scheduler.slo.minimumSuccessRatePercent must be between 0 and 100.'
    Add-Issue ($policy.scheduler.slo.minimumHeartbeatCoveragePercent -ge 0 -and $policy.scheduler.slo.minimumHeartbeatCoveragePercent -le 100) 'scheduler.slo.minimumHeartbeatCoveragePercent must be between 0 and 100.'
    Add-Issue ($policy.scheduler.slo.maximumReconciliationRatePercent -ge 0 -and $policy.scheduler.slo.maximumReconciliationRatePercent -le 100) 'scheduler.slo.maximumReconciliationRatePercent must be between 0 and 100.'
    Add-Issue ($policy.scheduler.slo.maximumP95DurationMinutes -ge 1 -and $policy.scheduler.slo.maximumP95DurationMinutes -le 525600) 'scheduler.slo.maximumP95DurationMinutes must be between 1 minute and 1 year.'
    Add-Issue ($policy.scheduler.metricsSnapshots.retentionCount -ge 2 -and $policy.scheduler.metricsSnapshots.retentionCount -le 10000) 'scheduler.metricsSnapshots.retentionCount must be between 2 and 10000.'
    Add-Issue ($policy.scheduler.orchestrationCycles.retentionCount -ge 2 -and $policy.scheduler.orchestrationCycles.retentionCount -le 10000) 'scheduler.orchestrationCycles.retentionCount must be between 2 and 10000.'
    Add-Issue ($policy.scheduler.decomposition.minimumChangedPaths -ge 2 -and $policy.scheduler.decomposition.minimumChangedPaths -le 10000) 'scheduler.decomposition.minimumChangedPaths must be between 2 and 10000.'
    Add-Issue ($policy.scheduler.decomposition.defaultMaximumShards -ge 2) 'scheduler.decomposition.defaultMaximumShards must be at least 2.'
    Add-Issue ($policy.scheduler.decomposition.maximumShards -ge $policy.scheduler.decomposition.defaultMaximumShards -and $policy.scheduler.decomposition.maximumShards -le 100) 'scheduler.decomposition.maximumShards must cover the default and not exceed 100.'
    Add-Issue ($policy.scheduler.decomposition.retentionCount -ge 2 -and $policy.scheduler.decomposition.retentionCount -le 10000) 'scheduler.decomposition.retentionCount must be between 2 and 10000.'
    Add-Issue ($policy.scheduler.verificationPlanner.defaultPriority -ge 0 -and $policy.scheduler.verificationPlanner.defaultPriority -le 1000) 'scheduler.verificationPlanner.defaultPriority must be between 0 and 1000.'
    $riskCalibration = $policy.scheduler.verificationPlanner.riskCalibration
    Add-Issue ($riskCalibration.mediumThreshold -ge 0 -and $riskCalibration.mediumThreshold -lt $riskCalibration.highThreshold) 'scheduler.verificationPlanner.riskCalibration mediumThreshold must be below highThreshold.'
    Add-Issue ($riskCalibration.highThreshold -lt $riskCalibration.criticalThreshold -and $riskCalibration.criticalThreshold -le 100) 'scheduler.verificationPlanner.riskCalibration thresholds must increase through 100.'
    foreach ($property in @('packetRiskPointWeight', 'changedPathPointWeight', 'maximumChangedPathPoints', 'additionalScopePointWeight', 'securityReviewPoints', 'databaseScopePoints', 'apiScopePoints', 'negativeQualityAdjustmentPoints', 'maximumHistoricalPoints')) {
        Add-Issue ($riskCalibration.$property -ge 0 -and $riskCalibration.$property -le 100) "scheduler.verificationPlanner.riskCalibration.$property must be between 0 and 100."
    }
    Add-Issue ($riskCalibration.forceIncludePassedAt -in @('low', 'medium', 'high', 'critical')) 'scheduler.verificationPlanner.riskCalibration.forceIncludePassedAt is invalid.'
    Add-Issue ($riskCalibration.requireSequentialExecutionAt -in @('low', 'medium', 'high', 'critical')) 'scheduler.verificationPlanner.riskCalibration.requireSequentialExecutionAt is invalid.'
    $modelRouting = $policy.scheduler.verificationPlanner.modelRouting
    Add-Issue ($modelRouting.enabled -eq $true) 'scheduler.verificationPlanner.modelRouting.enabled must remain true.'
    Add-Issue ($modelRouting.maximumComplexityScore -eq 100) 'scheduler.verificationPlanner.modelRouting.maximumComplexityScore must be 100.'
    foreach ($property in @('predictedFailureWeightPercent', 'verificationExecutionPoints', 'maximumVerificationExecutionPoints', 'additionalScopePoints', 'maximumScopePoints', 'securityReviewPoints', 'databaseScopePoints', 'apiScopePoints')) {
        Add-Issue ($modelRouting.$property -ge 0 -and $modelRouting.$property -le 100) "scheduler.verificationPlanner.modelRouting.$property must be between 0 and 100."
    }
    $routeRanks = @($modelRouting.routes | ForEach-Object { [int]$_.rank } | Sort-Object)
    $routeIds = @($modelRouting.routes | ForEach-Object { [string]$_.id })
    Add-Issue (@($modelRouting.routes).Count -ge 2) 'scheduler.verificationPlanner.modelRouting requires at least two routes.'
    Add-Issue (@($routeIds | Sort-Object -Unique).Count -eq @($modelRouting.routes).Count) 'scheduler.verificationPlanner.modelRouting route IDs must be unique.'
    Add-Issue (@($routeRanks | Sort-Object -Unique).Count -eq @($modelRouting.routes).Count) 'scheduler.verificationPlanner.modelRouting route ranks must be unique.'
    Add-Issue (($routeRanks -join ',') -eq ((1..@($modelRouting.routes).Count) -join ',')) 'scheduler.verificationPlanner.modelRouting route ranks must be contiguous from 1.'
    $priorMinimum = -1
    foreach ($route in @($modelRouting.routes | Sort-Object rank)) {
        Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$route.id)) 'model routing route id must be non-empty.'
        Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$route.model)) "model routing route '$($route.id)' model must be non-empty."
        Add-Issue ($route.reasoningEffort -in @('low', 'medium', 'high', 'xhigh', 'max', 'ultra')) "model routing route '$($route.id)' reasoningEffort is invalid."
        Add-Issue ($route.minimumComplexityScore -ge 0 -and $route.minimumComplexityScore -le 100 -and $route.minimumComplexityScore -gt $priorMinimum) "model routing route '$($route.id)' minimumComplexityScore must increase through 100."
        Add-Issue ($route.relativeCostUnits -gt 0 -and $route.relativeCostUnits -le 100) "model routing route '$($route.id)' relativeCostUnits must be between 1 and 100."
        $priorMinimum = [int]$route.minimumComplexityScore
    }
    foreach ($riskName in @('low', 'medium', 'high', 'critical')) {
        Add-Issue ($modelRouting.riskFloorRank.$riskName -ge 1 -and $modelRouting.riskFloorRank.$riskName -le @($modelRouting.routes).Count) "model routing risk floor '$riskName' is invalid."
    }
    $modelOutcomes = $modelRouting.outcomes
    Add-Issue ($modelOutcomes.minimumSamples -ge 2 -and $modelOutcomes.minimumSamples -le 100) 'model routing outcomes minimumSamples must be between 2 and 100.'
    Add-Issue ($modelOutcomes.successScoreThreshold -ge 1 -and $modelOutcomes.successScoreThreshold -le 100) 'model routing outcomes successScoreThreshold must be between 1 and 100.'
    Add-Issue ($modelOutcomes.maximumEvents -ge 100 -and $modelOutcomes.maximumEvents -le 100000) 'model routing outcomes maximumEvents must be between 100 and 100000.'
    Add-Issue ($modelOutcomes.priorStrength -ge 1 -and $modelOutcomes.priorStrength -le 100) 'model routing outcomes priorStrength must be between 1 and 100.'
    Add-Issue ($modelOutcomes.recentWindowSamples -ge 2 -and $modelOutcomes.recentWindowSamples -le 100) 'model routing outcomes recentWindowSamples must be between 2 and 100.'
    Add-Issue ($modelOutcomes.minimumDriftSamples -ge 2 -and $modelOutcomes.minimumDriftSamples -le $modelOutcomes.recentWindowSamples) 'model routing outcomes minimumDriftSamples is invalid.'
    Add-Issue ($modelOutcomes.maximumRecentOutcomeDropPoints -ge 0 -and $modelOutcomes.maximumRecentOutcomeDropPoints -le 100) 'model routing outcomes maximumRecentOutcomeDropPoints is invalid.'
    Add-Issue ($modelOutcomes.minimumRecentSuccessRatePercent -ge 0 -and $modelOutcomes.minimumRecentSuccessRatePercent -le 100) 'model routing outcomes minimumRecentSuccessRatePercent is invalid.'
    Add-Issue ($modelOutcomes.escalateDegradedRoutes -eq $true) 'model routing outcomes escalateDegradedRoutes must remain true.'
    Add-Issue ([int](($modelOutcomes.weights.PSObject.Properties.Value | Measure-Object -Sum).Sum) -eq 100) 'model routing outcome weights must sum to 100.'
    foreach ($weight in @($modelOutcomes.weights.PSObject.Properties)) {
        Add-Issue ($weight.Value -ge 0 -and $weight.Value -le 100) "model routing outcome weight '$($weight.Name)' is invalid."
    }
    foreach ($penalty in @($modelOutcomes.penalties.PSObject.Properties)) {
        Add-Issue ($penalty.Value -ge 0 -and $penalty.Value -le 100) "model routing outcome penalty '$($penalty.Name)' is invalid."
    }
    $modelOptimization = $modelRouting.optimization
    Add-Issue ($modelOptimization.enabled -eq $true) 'model routing optimization enabled must remain true.'
    Add-Issue ($modelOptimization.minimumSamplesPerRoute -ge $modelOutcomes.minimumSamples -and $modelOptimization.minimumSamplesPerRoute -le 100) 'model routing optimization minimumSamplesPerRoute is invalid.'
    Add-Issue ($modelOptimization.qualityWeightPercent -ge 0 -and $modelOptimization.qualityWeightPercent -le 100) 'model routing optimization qualityWeightPercent is invalid.'
    Add-Issue ($modelOptimization.costWeightPercent -ge 0 -and $modelOptimization.costWeightPercent -le 100) 'model routing optimization costWeightPercent is invalid.'
    Add-Issue ($modelOptimization.qualityWeightPercent + $modelOptimization.costWeightPercent -eq 100) 'model routing optimization weights must sum to 100.'
    Add-Issue ($modelOptimization.minimumQualityGainPoints -ge 0 -and $modelOptimization.minimumQualityGainPoints -le 100) 'model routing optimization minimumQualityGainPoints is invalid.'
    Add-Issue ($modelOptimization.maximumEscalationRanks -ge 0 -and $modelOptimization.maximumEscalationRanks -le @($modelRouting.routes).Count - 1) 'model routing optimization maximumEscalationRanks is invalid.'
    $instructionOutcomes = $policy.scheduler.verificationPlanner.instructionOutcomes
    Add-Issue ($instructionOutcomes.enabled -eq $true) 'instruction outcomes enabled must remain true.'
    Add-Issue ($instructionOutcomes.minimumSamples -ge 2 -and $instructionOutcomes.minimumSamples -le 100) 'instruction outcomes minimumSamples is invalid.'
    Add-Issue ($instructionOutcomes.successScoreThreshold -ge 1 -and $instructionOutcomes.successScoreThreshold -le 100) 'instruction outcomes successScoreThreshold is invalid.'
    Add-Issue ($instructionOutcomes.recentWindowSamples -ge 2 -and $instructionOutcomes.recentWindowSamples -le 100) 'instruction outcomes recentWindowSamples is invalid.'
    Add-Issue ($instructionOutcomes.minimumDriftSamples -ge 2 -and $instructionOutcomes.minimumDriftSamples -le $instructionOutcomes.recentWindowSamples) 'instruction outcomes minimumDriftSamples is invalid.'
    Add-Issue ($instructionOutcomes.maximumRecentOutcomeDropPoints -ge 0 -and $instructionOutcomes.maximumRecentOutcomeDropPoints -le 100) 'instruction outcomes maximumRecentOutcomeDropPoints is invalid.'
    Add-Issue ($instructionOutcomes.minimumRecentSuccessRatePercent -ge 0 -and $instructionOutcomes.minimumRecentSuccessRatePercent -le 100) 'instruction outcomes minimumRecentSuccessRatePercent is invalid.'
    Add-Issue ($instructionOutcomes.maximumEvents -ge 100 -and $instructionOutcomes.maximumEvents -le 100000) 'instruction outcomes maximumEvents is invalid.'
    Add-Issue ($instructionOutcomes.maximumSourcesPerEvent -ge 1 -and $instructionOutcomes.maximumSourcesPerEvent -le 1000) 'instruction outcomes maximumSourcesPerEvent is invalid.'
    Add-Issue ($instructionOutcomes.candidateScore -ge 1 -and $instructionOutcomes.candidateScore -le 100) 'instruction outcomes candidateScore is invalid.'
    $instructionBands = @($instructionOutcomes.complexityBandUpperBounds | ForEach-Object { [int]$_ })
    Add-Issue (@($instructionBands).Count -ge 2 -and @($instructionBands)[-1] -eq 100) 'instruction outcome complexity bands must contain at least two bounds and end at 100.'
    Add-Issue (($instructionBands -join ',') -eq (($instructionBands | Sort-Object -Unique) -join ',')) 'instruction outcome complexity bands must be unique and ascending.'
    $instructionExperiments = $policy.scheduler.verificationPlanner.instructionExperiments
    Add-Issue ($instructionExperiments.enabled -eq $true) 'instruction experiments enabled must remain true.'
    Add-Issue ($instructionExperiments.minimumSamplesPerCohort -ge $instructionOutcomes.minimumSamples -and $instructionExperiments.minimumSamplesPerCohort -le 100) 'instruction experiments minimumSamplesPerCohort is invalid.'
    Add-Issue ($instructionExperiments.minimumOutcomeGainPoints -ge 0 -and $instructionExperiments.minimumOutcomeGainPoints -le 100) 'instruction experiments minimumOutcomeGainPoints is invalid.'
    Add-Issue ($instructionExperiments.maximumSuccessRateDropPoints -ge 0 -and $instructionExperiments.maximumSuccessRateDropPoints -le 100) 'instruction experiments maximumSuccessRateDropPoints is invalid.'
    Add-Issue ($instructionExperiments.minimumMatchedSamplesPerCohort -ge 1 -and $instructionExperiments.minimumMatchedSamplesPerCohort -le $instructionExperiments.minimumSamplesPerCohort) 'instruction experiments minimumMatchedSamplesPerCohort is invalid.'
    Add-Issue ($instructionExperiments.confidenceZScore -ge 1 -and $instructionExperiments.confidenceZScore -le 5) 'instruction experiments confidenceZScore is invalid.'
    $instructionSequential = $instructionExperiments.sequentialMonitoring
    Add-Issue ($instructionSequential.enabled -eq $true) 'instruction experiment sequential monitoring must remain enabled.'
    Add-Issue ($instructionSequential.maximumLooks -ge 1 -and $instructionSequential.maximumLooks -le 100) 'instruction experiment maximumLooks is invalid.'
    Add-Issue ($instructionSequential.minimumNewCandidateSamples -ge 1 -and $instructionSequential.minimumNewCandidateSamples -le 100) 'instruction experiment minimumNewCandidateSamples is invalid.'
    Add-Issue ($instructionSequential.adjustedZScore -ge $instructionExperiments.confidenceZScore -and $instructionSequential.adjustedZScore -le 6) 'instruction experiment adjustedZScore is invalid.'
    $instructionPower = $instructionExperiments.powerPlanning
    Add-Issue ($instructionPower.enabled -eq $true) 'instruction experiment power planning must remain enabled.'
    Add-Issue ($instructionPower.powerZScore -gt 0 -and $instructionPower.powerZScore -le 5) 'instruction experiment powerZScore is invalid.'
    Add-Issue ($instructionPower.defaultOutcomeStandardDeviation -gt 0 -and $instructionPower.defaultOutcomeStandardDeviation -le 100) 'instruction experiment defaultOutcomeStandardDeviation is invalid.'
    Add-Issue ($instructionPower.maximumRequiredSamplesPerCohort -ge $instructionExperiments.minimumSamplesPerCohort -and $instructionPower.maximumRequiredSamplesPerCohort -le 100000) 'instruction experiment maximumRequiredSamplesPerCohort is invalid.'
    Add-Issue ($instructionExperiments.maximumActiveExperiments -ge 1 -and $instructionExperiments.maximumActiveExperiments -le 100) 'instruction experiments maximumActiveExperiments is invalid.'
    Add-Issue ($instructionExperiments.maximumEvents -ge 100 -and $instructionExperiments.maximumEvents -le 100000) 'instruction experiments maximumEvents is invalid.'
    $failurePrediction = $policy.scheduler.verificationPlanner.failurePrediction
    foreach ($property in @('predictedFailureThresholdPercent', 'riskScoreWeightPercent', 'maximumChangeBreadthPoints', 'historicalLearningPoints', 'maximumHistoricalPoints', 'priorRepairAttemptPoints', 'maximumPriorRepairPoints')) {
        Add-Issue ($failurePrediction.$property -ge 0 -and $failurePrediction.$property -le 100) "scheduler.verificationPlanner.failurePrediction.$property must be between 0 and 100."
    }
    Add-Issue ($failurePrediction.pointsPerChangedPath -ge 0 -and $failurePrediction.pointsPerChangedPath -le 100) 'scheduler.verificationPlanner.failurePrediction.pointsPerChangedPath must be between 0 and 100.'
    Add-Issue ($failurePrediction.priorityBoostDivisor -ge 1 -and $failurePrediction.priorityBoostDivisor -le 100) 'scheduler.verificationPlanner.failurePrediction.priorityBoostDivisor must be between 1 and 100.'
    $costModel = $failurePrediction.costModel
    Add-Issue ($costModel.defaultVerificationSeconds -ge 1 -and $costModel.defaultVerificationSeconds -le 86400) 'failurePrediction.costModel.defaultVerificationSeconds must be between 1 and 86400.'
    Add-Issue ($costModel.maximumPriorityBoost -ge 0 -and $costModel.maximumPriorityBoost -le 100) 'failurePrediction.costModel.maximumPriorityBoost must be between 0 and 100.'
    Add-Issue ($costModel.valueDensityBoostMultiplier -gt 0 -and $costModel.valueDensityBoostMultiplier -le 100) 'failurePrediction.costModel.valueDensityBoostMultiplier must be above 0 and at most 100.'
    foreach ($rule in @($costModel.verificationSecondsByPattern)) {
        try { $null = [regex]::new([string]$rule.pattern) } catch { Add-Issue $false "failurePrediction cost pattern '$($rule.pattern)' is invalid." }
        Add-Issue ($rule.seconds -ge 1 -and $rule.seconds -le 86400) "failurePrediction cost seconds for '$($rule.pattern)' must be between 1 and 86400."
    }
    foreach ($category in @($policy.repairLoop.allowedCategories)) {
        Add-Issue ($null -ne $costModel.repairSecondsByCategory.$category -and $costModel.repairSecondsByCategory.$category -ge 1 -and $costModel.repairSecondsByCategory.$category -le 604800) "failurePrediction repair cost for '$category' must be between 1 and 604800."
    }
    $telemetry = $costModel.telemetry
    Add-Issue ($telemetry.minimumSamples -ge 2 -and $telemetry.minimumSamples -le 100) 'verification telemetry minimumSamples must be between 2 and 100.'
    foreach ($property in @('flakyTransitionPercent', 'historicalBlendPercent', 'maximumHistoricalFailurePoints')) {
        Add-Issue ($telemetry.$property -ge 0 -and $telemetry.$property -le 100) "verification telemetry $property must be between 0 and 100."
    }
    Add-Issue ($telemetry.retentionCount -ge 100 -and $telemetry.retentionCount -le 100000) 'verification telemetry retentionCount must be between 100 and 100000.'
    $contextSecurity = $policy.scheduler.contextBundles.security
    Add-Issue ($contextSecurity.maximumScanCharactersPerFile -ge 1000 -and $contextSecurity.maximumScanCharactersPerFile -le 5000000) 'context security maximumScanCharactersPerFile must be between 1000 and 5000000.'
    Add-Issue ($contextSecurity.defaultTrust -in @('trusted-instructions', 'governed-context', 'reviewed-context', 'untrusted-data')) 'context security defaultTrust is invalid.'
    foreach ($zone in @($contextSecurity.trustZones)) {
        Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$zone.id)) 'context security trust zone id must be non-empty.'
        Add-Issue ($zone.trust -in @('trusted-instructions', 'governed-context', 'reviewed-context', 'untrusted-data')) "context security trust zone '$($zone.id)' has invalid trust."
        try { [void][regex]::new([string]$zone.pattern) } catch { Add-Issue $false "context security trust zone '$($zone.id)' has invalid regex." }
    }
    Add-Issue (@($contextSecurity.trustZones.id | Sort-Object -Unique).Count -eq @($contextSecurity.trustZones).Count) 'context security trust zone IDs must be unique.'
    foreach ($definition in @($contextSecurity.promptInjectionPatterns)) {
        Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$definition.id)) 'context security pattern id must be non-empty.'
        Add-Issue ($definition.severity -in @('low', 'medium', 'high', 'critical')) "context security pattern '$($definition.id)' has invalid severity."
        Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$definition.replacement)) "context security pattern '$($definition.id)' needs a replacement."
        try { [void][regex]::new([string]$definition.pattern) } catch { Add-Issue $false "context security pattern '$($definition.id)' has invalid regex." }
    }
    Add-Issue (@($contextSecurity.promptInjectionPatterns.id | Sort-Object -Unique).Count -eq @($contextSecurity.promptInjectionPatterns).Count) 'context security pattern IDs must be unique.'
    $confidence = $policy.scheduler.confidenceLedger
    foreach ($status in @('pass', 'warning', 'not-assessed', 'fail')) {
        Add-Issue ($null -ne $confidence.statusMultipliers.$status -and $confidence.statusMultipliers.$status -ge 0 -and $confidence.statusMultipliers.$status -le 1) "confidence status multiplier '$status' must be between 0 and 1."
    }
    $confidenceWeight = [int](($confidence.dimensions.PSObject.Properties.Value | Measure-Object -Sum).Sum)
    Add-Issue ($confidenceWeight -eq 100) 'confidence dimension weights must sum to 100.'
    foreach ($dimension in @($confidence.dimensions.PSObject.Properties)) {
        Add-Issue ($dimension.Value -gt 0 -and $dimension.Value -le 100) "confidence dimension '$($dimension.Name)' weight must be between 1 and 100."
    }
    foreach ($cap in @($confidence.hardCaps.PSObject.Properties)) {
        Add-Issue ($cap.Value -ge 0 -and $cap.Value -le 100) "confidence hard cap '$($cap.Name)' must be between 0 and 100."
    }
    Add-Issue ($confidence.levels.guardedAt -gt 0 -and $confidence.levels.guardedAt -lt $confidence.levels.substantialAt) 'confidence guardedAt must be below substantialAt.'
    Add-Issue ($confidence.levels.substantialAt -lt $confidence.levels.highAt -and $confidence.levels.highAt -le 100) 'confidence level thresholds must increase through 100.'
    $critique = $policy.scheduler.changeCritique
    Add-Issue ($critique.minimumApprovalScore -ge 1 -and $critique.minimumApprovalScore -le 100) 'changeCritique.minimumApprovalScore must be between 1 and 100.'
    Add-Issue ($critique.maximumWarningsForApproval -ge 0 -and $critique.maximumWarningsForApproval -le 100) 'changeCritique.maximumWarningsForApproval must be between 0 and 100.'
    $critiqueSeverities = @('critical', 'major', 'warning', 'info')
    foreach ($severity in $critiqueSeverities) {
        Add-Issue ($null -ne $critique.severityPenalties.$severity -and $critique.severityPenalties.$severity -ge 0 -and $critique.severityPenalties.$severity -le 100) "changeCritique severity penalty '$severity' must be between 0 and 100."
    }
    Add-Issue (@($critique.blockingSeverities).Count -gt 0 -and @($critique.blockingSeverities | Where-Object { $_ -notin $critiqueSeverities }).Count -eq 0) 'changeCritique.blockingSeverities must contain known severities.'
    Add-Issue (@($critique.blockingSeverities | Sort-Object -Unique).Count -eq @($critique.blockingSeverities).Count) 'changeCritique.blockingSeverities must be unique.'
    $requiredReviewAreas = @('intent', 'scope', 'proof', 'verification', 'architecture', 'security', 'operability')
    Add-Issue (@($critique.requiredReviewAreas | Sort-Object -Unique).Count -eq @($requiredReviewAreas).Count -and @(Compare-Object @($critique.requiredReviewAreas | Sort-Object) @($requiredReviewAreas | Sort-Object)).Count -eq 0) 'changeCritique.requiredReviewAreas must cover every governed review area exactly once.'
    $retrospective = $policy.scheduler.retrospective
    Add-Issue ($retrospective.minimumLearningScore -ge 1 -and $retrospective.minimumLearningScore -le 100) 'retrospective.minimumLearningScore must be between 1 and 100.'
    Add-Issue ($retrospective.maximumLearningCandidates -ge 1 -and $retrospective.maximumLearningCandidates -le 100) 'retrospective.maximumLearningCandidates must be between 1 and 100.'
    Add-Issue ($retrospective.costVarianceWarningPercent -ge 1 -and $retrospective.costVarianceWarningPercent -le 1000) 'retrospective.costVarianceWarningPercent must be between 1 and 1000.'
    foreach ($candidateType in @('impactDrift', 'falseNegative', 'failedRepair', 'flakyVerification', 'costVariance', 'contextQuarantine')) {
        Add-Issue ($null -ne $retrospective.candidateScores.$candidateType -and $retrospective.candidateScores.$candidateType -ge 1 -and $retrospective.candidateScores.$candidateType -le 100) "retrospective candidate score '$candidateType' must be between 1 and 100."
    }
    $learningPromotion = $policy.scheduler.learningPromotion
    Add-Issue ($learningPromotion.minimumDistinctTasks -ge 2 -and $learningPromotion.minimumDistinctTasks -le 100) 'learningPromotion.minimumDistinctTasks must be between 2 and 100.'
    Add-Issue ($learningPromotion.minimumObservationScore -ge 1 -and $learningPromotion.minimumObservationScore -le 100) 'learningPromotion.minimumObservationScore must be between 1 and 100.'
    Add-Issue ($learningPromotion.maximumCandidates -ge 1 -and $learningPromotion.maximumCandidates -le 100000) 'learningPromotion.maximumCandidates must be between 1 and 100000.'
    Add-Issue ($learningPromotion.maximumEvidenceItems -ge 1 -and $learningPromotion.maximumEvidenceItems -le 1000) 'learningPromotion.maximumEvidenceItems must be between 1 and 1000.'
    Add-Issue ($learningPromotion.approvalRequiresHumanReason -is [bool]) 'learningPromotion.approvalRequiresHumanReason must be boolean.'
    Add-Issue ($learningPromotion.materialization.minimumCalibrationSeconds -ge 1 -and $learningPromotion.materialization.minimumCalibrationSeconds -lt $learningPromotion.materialization.maximumCalibrationSeconds) 'learningPromotion materialization calibration minimum must be positive and below maximum.'
    Add-Issue ($learningPromotion.materialization.maximumCalibrationSeconds -le 604800) 'learningPromotion materialization calibration maximum must not exceed one week.'
    Add-Issue ($learningPromotion.materialization.maximumAppliedLearnings -ge 1 -and $learningPromotion.materialization.maximumAppliedLearnings -le $learningPromotion.maximumCandidates) 'learningPromotion maximumAppliedLearnings must fit maximumCandidates.'
    Add-Issue ($learningPromotion.materialization.requireSuccessfulExperiment -is [bool]) 'learningPromotion requireSuccessfulExperiment must be boolean.'
    Add-Issue ($learningPromotion.experiments.minimumShadowImprovementPercent -ge 0 -and $learningPromotion.experiments.minimumShadowImprovementPercent -le 100) 'learningPromotion minimumShadowImprovementPercent must be between 0 and 100.'
    Add-Issue ($learningPromotion.experiments.minimumCanarySamples -ge 1 -and $learningPromotion.experiments.minimumCanarySamples -le 1000) 'learningPromotion minimumCanarySamples must be between 1 and 1000.'
    Add-Issue ($learningPromotion.experiments.maximumCanaryDegradationPercent -ge 0 -and $learningPromotion.experiments.maximumCanaryDegradationPercent -le 100) 'learningPromotion maximumCanaryDegradationPercent must be between 0 and 100.'
    Add-Issue ($learningPromotion.experiments.defaultCanaryPercentage -ge 1 -and $learningPromotion.experiments.defaultCanaryPercentage -le $learningPromotion.experiments.maximumCanaryPercentage) 'learningPromotion defaultCanaryPercentage must fit maximumCanaryPercentage.'
    Add-Issue ($learningPromotion.experiments.maximumCanaryPercentage -le 100) 'learningPromotion maximumCanaryPercentage must not exceed 100.'
    Add-Issue ($learningPromotion.experiments.maximumActiveCanaries -ge 1 -and $learningPromotion.experiments.maximumActiveCanaries -le 1000) 'learningPromotion maximumActiveCanaries must be between 1 and 1000.'
    Add-Issue ($learningPromotion.health.minimumSamples -ge 1 -and $learningPromotion.health.minimumSamples -le 1000) 'learningPromotion health minimumSamples must be between 1 and 1000.'
    Add-Issue ($learningPromotion.health.degradedScoreThreshold -ge 0 -and $learningPromotion.health.degradedScoreThreshold -le 100) 'learningPromotion health degradedScoreThreshold must be between 0 and 100.'
    Add-Issue ($learningPromotion.health.maximumDegradationPercent -ge 0 -and $learningPromotion.health.maximumDegradationPercent -le 100) 'learningPromotion health maximumDegradationPercent must be between 0 and 100.'
    foreach ($penalty in @('failedRepairPenalty', 'falseNegativePenalty', 'impactDriftPenalty', 'contextQuarantinePenalty', 'maximumPenalty')) {
        Add-Issue ($learningPromotion.health.$penalty -ge 0 -and $learningPromotion.health.$penalty -le 100) "learningPromotion health $penalty must be between 0 and 100."
    }
    Add-Issue ($learningPromotion.health.maximumObservations -ge 1 -and $learningPromotion.health.maximumObservations -le 1000000) 'learningPromotion health maximumObservations must be between 1 and 1000000.'
    $learningTypes = @('impact-drift', 'failure-prediction', 'repair-learning', 'flaky-verification', 'cost-calibration', 'context-security')
    foreach ($learningType in $learningTypes) {
        Add-Issue ($learningPromotion.targets.$learningType -in @('durable-memory', 'verification-calibration')) "learningPromotion target '$learningType' is invalid."
    }
    $evalPromotion = $policy.scheduler.evalPromotion
    Add-Issue ($evalPromotion.minimumSignalScore -ge 1 -and $evalPromotion.minimumSignalScore -le 100) 'evalPromotion.minimumSignalScore must be between 1 and 100.'
    Add-Issue ($evalPromotion.maximumCandidates -ge 1 -and $evalPromotion.maximumCandidates -le 100000) 'evalPromotion.maximumCandidates must be between 1 and 100000.'
    Add-Issue ($evalPromotion.maximumChangedPaths -ge 1 -and $evalPromotion.maximumChangedPaths -le 1000) 'evalPromotion.maximumChangedPaths must be between 1 and 1000.'
    Add-Issue ($evalPromotion.maximumSignals -ge 1 -and $evalPromotion.maximumSignals -le 1000) 'evalPromotion.maximumSignals must be between 1 and 1000.'
    Add-Issue ($evalPromotion.approvalRequiresHumanReason -is [bool]) 'evalPromotion.approvalRequiresHumanReason must be boolean.'
    Add-Issue ($evalPromotion.requirePassingBeforeApply -is [bool]) 'evalPromotion.requirePassingBeforeApply must be boolean.'
    $taskSimilarity = $policy.scheduler.taskSimilarity
    Add-Issue ($taskSimilarity.minimumCandidateScore -ge 0 -and $taskSimilarity.minimumCandidateScore -le 100) 'taskSimilarity.minimumCandidateScore must be between 0 and 100.'
    Add-Issue ($taskSimilarity.minimumPlanReuseScore -ge $taskSimilarity.minimumCandidateScore -and $taskSimilarity.minimumPlanReuseScore -le 100) 'taskSimilarity.minimumPlanReuseScore must be between minimumCandidateScore and 100.'
    Add-Issue ($taskSimilarity.maximumRiskScoreDelta -ge 0 -and $taskSimilarity.maximumRiskScoreDelta -le 100) 'taskSimilarity.maximumRiskScoreDelta must be between 0 and 100.'
    Add-Issue ($taskSimilarity.maximumCandidates -ge 1 -and $taskSimilarity.maximumCandidates -le 1000) 'taskSimilarity.maximumCandidates must be between 1 and 1000.'
    $similarityWeightNames = @('modules', 'scopes', 'rules', 'checks', 'pathAreas')
    foreach ($weightName in $similarityWeightNames) {
        Add-Issue ($taskSimilarity.weights.$weightName -ge 0 -and $taskSimilarity.weights.$weightName -le 100) "taskSimilarity weight '$weightName' must be between 0 and 100."
    }
    Add-Issue ([int](($similarityWeightNames | ForEach-Object { [int]$taskSimilarity.weights.$_ } | Measure-Object -Sum).Sum) -eq 100) 'taskSimilarity weights must sum to 100.'
    foreach ($priorityRule in @($policy.scheduler.verificationPlanner.priorityPatterns)) {
        Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$priorityRule.pattern)) 'scheduler.verificationPlanner priority patterns must be non-empty.'
        Add-Issue ($priorityRule.priority -ge 0 -and $priorityRule.priority -le 1000) 'scheduler.verificationPlanner priorities must be between 0 and 1000.'
        try { [void][regex]::new([string]$priorityRule.pattern) } catch { $issues.Add("Invalid verification priority regex: $($priorityRule.pattern)") }
    }
    foreach ($coverageRule in @($policy.scheduler.verificationPlanner.supersedes.PSObject.Properties)) {
        Add-Issue (-not [string]::IsNullOrWhiteSpace($coverageRule.Name)) 'scheduler.verificationPlanner superseding check ids must be non-empty.'
        Add-Issue (@($coverageRule.Value).Count -gt 0) "scheduler.verificationPlanner supersedes.$($coverageRule.Name) must contain at least one check id."
        Add-Issue ($coverageRule.Name -notin @($coverageRule.Value)) "scheduler.verificationPlanner supersedes.$($coverageRule.Name) cannot cover itself."
    }
    Add-Issue ($policy.scheduler.contextBundles.maximumItems -ge 1 -and $policy.scheduler.contextBundles.maximumItems -le 1000) 'scheduler.contextBundles.maximumItems must be between 1 and 1000.'
    Add-Issue ($policy.scheduler.contextBundles.defaultItems -ge 1 -and $policy.scheduler.contextBundles.defaultItems -le $policy.scheduler.contextBundles.maximumItems) 'scheduler.contextBundles.defaultItems must fit maximumItems.'
    Add-Issue ($policy.scheduler.contextBundles.maximumTotalCharacters -ge 1000 -and $policy.scheduler.contextBundles.maximumTotalCharacters -le 10000000) 'scheduler.contextBundles.maximumTotalCharacters must be between 1000 and 10000000.'
    Add-Issue ($policy.scheduler.contextBundles.defaultTotalCharacters -ge 1000 -and $policy.scheduler.contextBundles.defaultTotalCharacters -le $policy.scheduler.contextBundles.maximumTotalCharacters) 'scheduler.contextBundles.defaultTotalCharacters must fit maximumTotalCharacters.'
    Add-Issue ($policy.scheduler.contextBundles.maximumItemCharacters -ge 100 -and $policy.scheduler.contextBundles.maximumItemCharacters -le $policy.scheduler.contextBundles.maximumTotalCharacters) 'scheduler.contextBundles.maximumItemCharacters must fit the total character budget.'
    Add-Issue ($policy.scheduler.contextBundles.symbolContextLines -ge 1 -and $policy.scheduler.contextBundles.symbolContextLines -le 500) 'scheduler.contextBundles.symbolContextLines must be between 1 and 500.'
    Add-Issue ($policy.scheduler.contextBundles.optimizer.minimumScoreCoveragePercent -ge 0 -and $policy.scheduler.contextBundles.optimizer.minimumScoreCoveragePercent -le 100) 'scheduler.contextBundles.optimizer.minimumScoreCoveragePercent must be between 0 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.optimizer.minimumCharacterUtilizationPercent -ge 0 -and $policy.scheduler.contextBundles.optimizer.minimumCharacterUtilizationPercent -le 100) 'scheduler.contextBundles.optimizer.minimumCharacterUtilizationPercent must be between 0 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.optimizer.maximumTruncationPercent -ge 0 -and $policy.scheduler.contextBundles.optimizer.maximumTruncationPercent -le 100) 'scheduler.contextBundles.optimizer.maximumTruncationPercent must be between 0 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.optimizer.minimumKindDiversity -ge 1 -and $policy.scheduler.contextBundles.optimizer.minimumKindDiversity -le 20) 'scheduler.contextBundles.optimizer.minimumKindDiversity must be between 1 and 20.'
    Add-Issue ($policy.scheduler.contextBundles.optimizer.recommendedHeadroomPercent -ge 0 -and $policy.scheduler.contextBundles.optimizer.recommendedHeadroomPercent -le 100) 'scheduler.contextBundles.optimizer.recommendedHeadroomPercent must be between 0 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.optimizer.maximumRecommendations -ge 1 -and $policy.scheduler.contextBundles.optimizer.maximumRecommendations -le 100) 'scheduler.contextBundles.optimizer.maximumRecommendations must be between 1 and 100.'
    $contextBenchmark = $policy.scheduler.contextBundles.benchmark
    Add-Issue ($contextBenchmark.minimumComparabilityPercent -ge 0 -and $contextBenchmark.minimumComparabilityPercent -le 100) 'scheduler.contextBundles.benchmark.minimumComparabilityPercent must be between 0 and 100.'
    Add-Issue ($contextBenchmark.minimumImprovementPoints -ge 0 -and $contextBenchmark.minimumImprovementPoints -le 100) 'scheduler.contextBundles.benchmark.minimumImprovementPoints must be between 0 and 100.'
    Add-Issue ($contextBenchmark.maximumRequiredCoverageRegressionPoints -ge 0 -and $contextBenchmark.maximumRequiredCoverageRegressionPoints -le 100) 'scheduler.contextBundles.benchmark.maximumRequiredCoverageRegressionPoints must be between 0 and 100.'
    Add-Issue ($contextBenchmark.maximumSecurityFindingIncrease -ge 0 -and $contextBenchmark.maximumSecurityFindingIncrease -le 10000) 'scheduler.contextBundles.benchmark.maximumSecurityFindingIncrease must be between 0 and 10000.'
    $contextBenchmarkWeightNames = @('requiredCoverage', 'scoreCoverage', 'lowTruncation', 'contentYield', 'kindDiversity', 'budgetFit')
    foreach ($weightName in $contextBenchmarkWeightNames) {
        Add-Issue ($contextBenchmark.weights.$weightName -ge 0 -and $contextBenchmark.weights.$weightName -le 100) "contextBundles.benchmark weight '$weightName' must be between 0 and 100."
    }
    Add-Issue ([int](($contextBenchmarkWeightNames | ForEach-Object { [int]$contextBenchmark.weights.$_ } | Measure-Object -Sum).Sum) -eq 100) 'contextBundles.benchmark weights must sum to 100.'
    $contextExperiments = $policy.scheduler.contextBundles.experiments
    Add-Issue ($contextExperiments.maximumVariants -ge 1 -and $contextExperiments.maximumVariants -le 20) 'scheduler.contextBundles.experiments.maximumVariants must be between 1 and 20.'
    Add-Issue ($contextExperiments.minimumItemLimit -ge 1 -and $contextExperiments.minimumItemLimit -le $policy.scheduler.contextBundles.maximumItems) 'scheduler.contextBundles.experiments.minimumItemLimit must fit maximumItems.'
    Add-Issue ($contextExperiments.minimumCharacterBudget -ge 1000 -and $contextExperiments.minimumCharacterBudget -le $policy.scheduler.contextBundles.maximumTotalCharacters) 'scheduler.contextBundles.experiments.minimumCharacterBudget must fit maximumTotalCharacters.'
    foreach ($percentName in @('compactCharacterPercent', 'coverageItemPercent', 'coverageCharacterPercent', 'depthCharacterPercent')) {
        Add-Issue ($contextExperiments.$percentName -ge 10 -and $contextExperiments.$percentName -le 500) "scheduler.contextBundles.experiments.$percentName must be between 10 and 500."
    }
    $strategyApplication = $policy.scheduler.contextBundles.strategyApplication
    Add-Issue ($strategyApplication.approvalRequiresHumanReason -eq $true) 'scheduler.contextBundles.strategyApplication.approvalRequiresHumanReason must remain true.'
    Add-Issue ($strategyApplication.minimumApprovalReasonWords -ge 1 -and $strategyApplication.minimumApprovalReasonWords -le 100) 'scheduler.contextBundles.strategyApplication.minimumApprovalReasonWords must be between 1 and 100.'
    Add-Issue ($strategyApplication.maximumQualityDeviationPoints -ge 0 -and $strategyApplication.maximumQualityDeviationPoints -le 100) 'scheduler.contextBundles.strategyApplication.maximumQualityDeviationPoints must be between 0 and 100.'
    Add-Issue ($strategyApplication.maximumSecurityFindingIncrease -ge 0 -and $strategyApplication.maximumSecurityFindingIncrease -le 10000) 'scheduler.contextBundles.strategyApplication.maximumSecurityFindingIncrease must be between 0 and 10000.'
    Add-Issue ($strategyApplication.maximumQuarantineMatchIncrease -ge 0 -and $strategyApplication.maximumQuarantineMatchIncrease -le 10000) 'scheduler.contextBundles.strategyApplication.maximumQuarantineMatchIncrease must be between 0 and 10000.'
    $strategyOutcomes = $policy.scheduler.contextBundles.strategyOutcomes
    Add-Issue ($strategyOutcomes.minimumSamples -ge 1 -and $strategyOutcomes.minimumSamples -le 10000) 'scheduler.contextBundles.strategyOutcomes.minimumSamples must be between 1 and 10000.'
    Add-Issue ($strategyOutcomes.successScoreThreshold -ge 0 -and $strategyOutcomes.successScoreThreshold -le 100) 'scheduler.contextBundles.strategyOutcomes.successScoreThreshold must be between 0 and 100.'
    Add-Issue ($strategyOutcomes.maximumAbsoluteExperimentAdjustmentPoints -ge 0 -and $strategyOutcomes.maximumAbsoluteExperimentAdjustmentPoints -le 25) 'scheduler.contextBundles.strategyOutcomes.maximumAbsoluteExperimentAdjustmentPoints must be between 0 and 25.'
    Add-Issue ($strategyOutcomes.maximumEvents -ge 1 -and $strategyOutcomes.maximumEvents -le 1000000) 'scheduler.contextBundles.strategyOutcomes.maximumEvents must be between 1 and 1000000.'
    Add-Issue ($strategyOutcomes.priorStrength -ge 1 -and $strategyOutcomes.priorStrength -le 1000) 'scheduler.contextBundles.strategyOutcomes.priorStrength must be between 1 and 1000.'
    Add-Issue ($strategyOutcomes.recentWindowSamples -ge 1 -and $strategyOutcomes.recentWindowSamples -le 10000) 'scheduler.contextBundles.strategyOutcomes.recentWindowSamples must be between 1 and 10000.'
    Add-Issue ($strategyOutcomes.minimumDriftSamples -ge 2 -and $strategyOutcomes.minimumDriftSamples -le $strategyOutcomes.recentWindowSamples) 'scheduler.contextBundles.strategyOutcomes.minimumDriftSamples must fit recentWindowSamples.'
    Add-Issue ($strategyOutcomes.maximumRecentOutcomeDropPoints -ge 0 -and $strategyOutcomes.maximumRecentOutcomeDropPoints -le 100) 'scheduler.contextBundles.strategyOutcomes.maximumRecentOutcomeDropPoints must be between 0 and 100.'
    Add-Issue ($strategyOutcomes.minimumRecentSuccessRatePercent -ge 0 -and $strategyOutcomes.minimumRecentSuccessRatePercent -le 100) 'scheduler.contextBundles.strategyOutcomes.minimumRecentSuccessRatePercent must be between 0 and 100.'
    Add-Issue ($strategyOutcomes.blockDegradedAdoption -eq $true) 'scheduler.contextBundles.strategyOutcomes.blockDegradedAdoption must remain true.'
    $strategyOutcomeWeightNames = @('readiness', 'confidence', 'critique', 'verification')
    foreach ($weightName in $strategyOutcomeWeightNames) {
        Add-Issue ($strategyOutcomes.weights.$weightName -ge 0 -and $strategyOutcomes.weights.$weightName -le 100) "scheduler.contextBundles.strategyOutcomes weight '$weightName' must be between 0 and 100."
    }
    Add-Issue ([int](($strategyOutcomeWeightNames | ForEach-Object { [int]$strategyOutcomes.weights.$_ } | Measure-Object -Sum).Sum) -eq 100) 'scheduler.contextBundles.strategyOutcomes weights must sum to 100.'
    foreach ($penaltyName in @('failedRepair', 'falseNegative', 'impactDrift', 'quarantinedContextSource', 'rolledBack', 'maximum')) {
        Add-Issue ($strategyOutcomes.penalties.$penaltyName -ge 0 -and $strategyOutcomes.penalties.$penaltyName -le 100) "scheduler.contextBundles.strategyOutcomes penalty '$penaltyName' must be between 0 and 100."
    }
    Add-Issue ($policy.scheduler.contextBundles.feedback.minimumPathSamples -ge 1 -and $policy.scheduler.contextBundles.feedback.minimumPathSamples -le 1000) 'scheduler.contextBundles.feedback.minimumPathSamples must be between 1 and 1000.'
    Add-Issue ($policy.scheduler.contextBundles.feedback.helpfulWeight -gt 0 -and $policy.scheduler.contextBundles.feedback.helpfulWeight -le 100) 'scheduler.contextBundles.feedback.helpfulWeight must be between 1 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.feedback.noisyWeight -lt 0 -and $policy.scheduler.contextBundles.feedback.noisyWeight -ge -100) 'scheduler.contextBundles.feedback.noisyWeight must be between -100 and -1.'
    Add-Issue ($policy.scheduler.contextBundles.feedback.missingWeight -gt 0 -and $policy.scheduler.contextBundles.feedback.missingWeight -le 100) 'scheduler.contextBundles.feedback.missingWeight must be between 1 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.feedback.maximumAbsoluteAdjustment -ge 1 -and $policy.scheduler.contextBundles.feedback.maximumAbsoluteAdjustment -le 100) 'scheduler.contextBundles.feedback.maximumAbsoluteAdjustment must be between 1 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.feedback.retentionCount -ge 10 -and $policy.scheduler.contextBundles.feedback.retentionCount -le 100000) 'scheduler.contextBundles.feedback.retentionCount must be between 10 and 100000.'
    foreach ($qualityDeltaName in @('reworkDelta', 'rollbackDelta', 'regressionDelta')) {
        $qualityDelta = [int]$policy.scheduler.contextBundles.feedback.qualityAdjustments.$qualityDeltaName
        Add-Issue ($qualityDelta -lt 0 -and $qualityDelta -ge -100) "scheduler.contextBundles.feedback.qualityAdjustments.$qualityDeltaName must be between -100 and -1."
    }
    Add-Issue ($policy.scheduler.contextBundles.feedback.qualityAdjustments.recoveryDelta -gt 0 -and $policy.scheduler.contextBundles.feedback.qualityAdjustments.recoveryDelta -le 100) 'scheduler.contextBundles.feedback.qualityAdjustments.recoveryDelta must be between 1 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.feedback.qualityAdjustments.maximumEventsPerDispatch -ge 1 -and $policy.scheduler.contextBundles.feedback.qualityAdjustments.maximumEventsPerDispatch -le 1000) 'scheduler.contextBundles.feedback.qualityAdjustments.maximumEventsPerDispatch must be between 1 and 1000.'
    Add-Issue ($policy.scheduler.contextBundles.feedback.qualityAdjustments.retentionCount -ge 10 -and $policy.scheduler.contextBundles.feedback.qualityAdjustments.retentionCount -le 100000) 'scheduler.contextBundles.feedback.qualityAdjustments.retentionCount must be between 10 and 100000.'
    Add-Issue ($policy.scheduler.contextBundles.memory.defaultReviewAfterDays -ge 1 -and $policy.scheduler.contextBundles.memory.defaultReviewAfterDays -le $policy.scheduler.contextBundles.memory.maximumReviewAfterDays) 'scheduler.contextBundles.memory.defaultReviewAfterDays must fit maximumReviewAfterDays.'
    Add-Issue ($policy.scheduler.contextBundles.memory.maximumReviewAfterDays -ge 1 -and $policy.scheduler.contextBundles.memory.maximumReviewAfterDays -le 36500) 'scheduler.contextBundles.memory.maximumReviewAfterDays must be between 1 and 36500.'
    Add-Issue ($policy.scheduler.contextBundles.memory.maximumRelevantItems -ge 1 -and $policy.scheduler.contextBundles.memory.maximumRelevantItems -le 100) 'scheduler.contextBundles.memory.maximumRelevantItems must be between 1 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.memory.minimumCandidateScore -ge 1 -and $policy.scheduler.contextBundles.memory.minimumCandidateScore -le 100) 'scheduler.contextBundles.memory.minimumCandidateScore must be between 1 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.memory.duplicateSimilarityPercent -ge 1 -and $policy.scheduler.contextBundles.memory.duplicateSimilarityPercent -le 100) 'scheduler.contextBundles.memory.duplicateSimilarityPercent must be between 1 and 100.'
    Add-Issue ($policy.scheduler.contextBundles.memory.maximumCandidates -ge 1 -and $policy.scheduler.contextBundles.memory.maximumCandidates -le 100) 'scheduler.contextBundles.memory.maximumCandidates must be between 1 and 100.'
    $changePolicyPath = Join-Path $wikiRoot 'policies/change-policies.json'
    if (Test-Path -LiteralPath $changePolicyPath -PathType Leaf) {
        $changePolicy = Get-Content -LiteralPath $changePolicyPath -Raw | ConvertFrom-Json
        $knownCheckIds = @($changePolicy.rules.requiredChecks.id | Where-Object { $_ } | Sort-Object -Unique)
        $coveredCheckIds = [Collections.Generic.HashSet[string]]::new()
        foreach ($coverageRule in @($policy.scheduler.verificationPlanner.supersedes.PSObject.Properties)) {
            Add-Issue ($coverageRule.Name -in $knownCheckIds) "Unknown superseding verification check: $($coverageRule.Name)."
            foreach ($coveredId in @($coverageRule.Value)) {
                Add-Issue ([string]$coveredId -in $knownCheckIds) "Unknown covered verification check: $coveredId."
                Add-Issue ($coveredCheckIds.Add([string]$coveredId)) "Verification check '$coveredId' is covered by more than one superseding check."
                $reverse = $policy.scheduler.verificationPlanner.supersedes.PSObject.Properties[[string]$coveredId]
                Add-Issue ($null -eq $reverse -or $coverageRule.Name -notin @($reverse.Value)) "Verification supersedence cycle detected between '$($coverageRule.Name)' and '$coveredId'."
            }
        }
    }
    Add-Issue ($policy.scheduler.watchdog.silentDispatchMinutes -ge 1 -and $policy.scheduler.watchdog.silentDispatchMinutes -le $policy.scheduler.maximumLeaseMinutes) 'scheduler.watchdog.silentDispatchMinutes must fit the maximum lease duration.'
    Add-Issue ($policy.scheduler.watchdog.retryWindowMinutes -ge $policy.scheduler.watchdog.silentDispatchMinutes -and $policy.scheduler.watchdog.retryWindowMinutes -le 10080) 'scheduler.watchdog.retryWindowMinutes must cover silence detection and not exceed one week.'
    Add-Issue ($policy.scheduler.watchdog.maximumRetriesPerWorkspace -ge 0 -and $policy.scheduler.watchdog.maximumRetriesPerWorkspace -le 100) 'scheduler.watchdog.maximumRetriesPerWorkspace must be between 0 and 100.'
    Add-Issue ($policy.scheduler.watchdog.agentFailureThreshold -ge 1 -and $policy.scheduler.watchdog.agentFailureThreshold -le 100) 'scheduler.watchdog.agentFailureThreshold must be between 1 and 100.'
    Add-Issue ($policy.scheduler.watchdog.defaultQuarantineMinutes -ge 1) 'scheduler.watchdog.defaultQuarantineMinutes must be positive.'
    Add-Issue ($policy.scheduler.watchdog.maximumQuarantineMinutes -ge $policy.scheduler.watchdog.defaultQuarantineMinutes -and $policy.scheduler.watchdog.maximumQuarantineMinutes -le 10080) 'scheduler.watchdog.maximumQuarantineMinutes must cover the default and not exceed one week.'
    Add-Issue ($policy.scheduler.watchdog.retentionCount -ge 2 -and $policy.scheduler.watchdog.retentionCount -le 10000) 'scheduler.watchdog.retentionCount must be between 2 and 10000.'
    Add-Issue ($policy.scheduler.watchdog.circuitBreaker.defaultCooldownMinutes -ge 1) 'scheduler.watchdog.circuitBreaker.defaultCooldownMinutes must be positive.'
    Add-Issue ($policy.scheduler.watchdog.circuitBreaker.maximumCooldownMinutes -ge $policy.scheduler.watchdog.circuitBreaker.defaultCooldownMinutes -and $policy.scheduler.watchdog.circuitBreaker.maximumCooldownMinutes -le 10080) 'scheduler.watchdog.circuitBreaker.maximumCooldownMinutes must cover the default and not exceed one week.'
    Add-Issue ($policy.scheduler.watchdog.circuitBreaker.retentionCount -ge 2 -and $policy.scheduler.watchdog.circuitBreaker.retentionCount -le 10000) 'scheduler.watchdog.circuitBreaker.retentionCount must be between 2 and 10000.'
    Add-Issue ($policy.scheduler.metricsSnapshots.regression.minimumTerminalSamples -ge 1 -and $policy.scheduler.metricsSnapshots.regression.minimumTerminalSamples -le 10000) 'scheduler.metricsSnapshots.regression.minimumTerminalSamples must be between 1 and 10000.'
    foreach ($regressionKey in @(
        'maximumSuccessRateDropPoints',
        'maximumHeartbeatCoverageDropPoints',
        'maximumReconciliationRateIncreasePoints',
        'maximumP95DurationIncreasePercent',
        'maximumThroughputDropPercent'
    )) {
        $regressionValue = [double]$policy.scheduler.metricsSnapshots.regression.$regressionKey
        Add-Issue ($regressionValue -ge 0 -and $regressionValue -le 1000) "scheduler.metricsSnapshots.regression.$regressionKey must be between 0 and 1000."
    }
    $allowedCapabilities = @($policy.scheduler.agentRegistry.allowedCapabilities)
    Add-Issue (@($allowedCapabilities).Count -ge 1 -and @($allowedCapabilities).Count -eq @($allowedCapabilities | Select-Object -Unique).Count) 'scheduler.agentRegistry.allowedCapabilities must be non-empty and unique.'
    foreach ($capability in $allowedCapabilities) {
        Add-Issue ([string]$capability -cmatch '^[a-z][a-z0-9-]{1,31}$') "Invalid scheduler agent capability: $capability"
    }
    Add-Issue ($policy.scheduler.agentRegistry.defaultCapacity -ge 1) 'scheduler.agentRegistry.defaultCapacity must be positive.'
    Add-Issue ($policy.scheduler.agentRegistry.maximumCapacity -ge $policy.scheduler.agentRegistry.defaultCapacity -and $policy.scheduler.agentRegistry.maximumCapacity -le 32) 'scheduler.agentRegistry.maximumCapacity must cover the default and not exceed 32.'
    Add-Issue ($policy.scheduler.agentRegistry.defaultRegistrationMinutes -ge 1) 'scheduler.agentRegistry.defaultRegistrationMinutes must be positive.'
    Add-Issue ($policy.scheduler.agentRegistry.maximumRegistrationMinutes -ge $policy.scheduler.agentRegistry.defaultRegistrationMinutes -and $policy.scheduler.agentRegistry.maximumRegistrationMinutes -le 10080) 'scheduler.agentRegistry.maximumRegistrationMinutes must cover the default and not exceed one week.'
    Add-Issue ($policy.scheduler.agentRegistry.routing.minimumReliabilitySamples -ge 1 -and $policy.scheduler.agentRegistry.routing.minimumReliabilitySamples -le 10000) 'scheduler.agentRegistry.routing.minimumReliabilitySamples must be between 1 and 10000.'
    Add-Issue ($policy.scheduler.agentRegistry.routing.minimumCapabilitySamples -ge 1 -and $policy.scheduler.agentRegistry.routing.minimumCapabilitySamples -le 10000) 'scheduler.agentRegistry.routing.minimumCapabilitySamples must be between 1 and 10000.'
    Add-Issue ($policy.scheduler.agentRegistry.routing.coldStartScore -ge 0 -and $policy.scheduler.agentRegistry.routing.coldStartScore -le 100) 'scheduler.agentRegistry.routing.coldStartScore must be between 0 and 100.'
    Add-Issue ($policy.scheduler.agentRegistry.routing.capabilityReliabilityBlendPercent -ge 0 -and $policy.scheduler.agentRegistry.routing.capabilityReliabilityBlendPercent -le 100) 'scheduler.agentRegistry.routing.capabilityReliabilityBlendPercent must be between 0 and 100.'
    $routingWeights = @(
        [double]$policy.scheduler.agentRegistry.routing.successWeight,
        [double]$policy.scheduler.agentRegistry.routing.heartbeatWeight,
        [double]$policy.scheduler.agentRegistry.routing.durationWeight,
        [double]$policy.scheduler.agentRegistry.routing.qualityWeight,
        [double]$policy.scheduler.agentRegistry.routing.availableCapacityWeight,
        [double]$policy.scheduler.agentRegistry.routing.specializationWeight,
        [double]$policy.scheduler.agentRegistry.routing.fairnessWeight
    )
    Add-Issue (@($routingWeights | Where-Object { $_ -lt 0 -or $_ -gt 100 }).Count -eq 0) 'scheduler.agentRegistry.routing weights must be between 0 and 100.'
    Add-Issue ([Math]::Abs((($routingWeights | Measure-Object -Sum).Sum) - 100) -lt 0.001) 'scheduler.agentRegistry.routing weights must sum to 100.'
    Add-Issue ($policy.scheduler.agentRegistry.routing.highRiskReliabilityWeight -ge 0 -and $policy.scheduler.agentRegistry.routing.highRiskReliabilityWeight -le 100) 'scheduler.agentRegistry.routing.highRiskReliabilityWeight must be between 0 and 100.'
    Add-Issue ($policy.scheduler.agentRegistry.routing.criticalRiskReliabilityWeight -ge $policy.scheduler.agentRegistry.routing.highRiskReliabilityWeight -and $policy.scheduler.agentRegistry.routing.criticalRiskReliabilityWeight -le 100) 'scheduler.agentRegistry.routing.criticalRiskReliabilityWeight must be between highRiskReliabilityWeight and 100.'
    Add-Issue ($policy.scheduler.agentRegistry.schedulePlans.defaultTtlMinutes -ge 1) 'scheduler.agentRegistry.schedulePlans.defaultTtlMinutes must be positive.'
    Add-Issue ($policy.scheduler.agentRegistry.schedulePlans.maximumTtlMinutes -ge $policy.scheduler.agentRegistry.schedulePlans.defaultTtlMinutes -and $policy.scheduler.agentRegistry.schedulePlans.maximumTtlMinutes -le 1440) 'scheduler.agentRegistry.schedulePlans.maximumTtlMinutes must cover the default and not exceed one day.'
    Add-Issue ($policy.scheduler.agentRegistry.schedulePlans.retentionCount -ge 2 -and $policy.scheduler.agentRegistry.schedulePlans.retentionCount -le 10000) 'scheduler.agentRegistry.schedulePlans.retentionCount must be between 2 and 10000.'
    Add-Issue ($policy.export.maximumContextItems -ge 1 -and $policy.export.maximumContextItems -le 1000) 'export.maximumContextItems must be between 1 and 1000.'
    Add-Issue ($policy.export.defaultContextItems -ge 1 -and $policy.export.defaultContextItems -le $policy.export.maximumContextItems) 'export.defaultContextItems must fit maximumContextItems.'
    Test-Regex ([string]$policy.export.pathPattern) 'export.pathPattern'
    Test-Regex ([string]$policy.import.pathPattern) 'import.pathPattern'
    Test-Regex ([string]$policy.import.workspacePattern) 'import.workspacePattern'
    Add-Issue ([string]$policy.import.stagingPrefix -match '^\.task-[a-z0-9-]+-$') 'import.stagingPrefix must be a hidden .task- prefix ending in a hyphen.'
    Add-Issue ($policy.import.allowPartialScopeByDefault -eq $false) 'import.allowPartialScopeByDefault must remain false.'
    Add-Issue ($policy.import.revalidateAcceptance -eq $true) 'import.revalidateAcceptance must remain true.'
    Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$policy.export.redaction.policyId)) 'export.redaction.policyId is required.'
    $patterns = @($policy.export.redaction.patterns)
    Add-Issue (@($patterns).Count -gt 0) 'At least one redaction pattern is required.'
    $ids = @($patterns.id)
    Add-Issue (@($ids | Sort-Object -Unique).Count -eq @($ids).Count) 'Redaction pattern IDs must be unique.'
    foreach ($pattern in $patterns) {
        Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$pattern.id)) 'Every redaction pattern needs an ID.'
        Add-Issue (-not [string]::IsNullOrWhiteSpace([string]$pattern.category)) "Redaction pattern '$($pattern.id)' needs a category."
        Add-Issue ([string]$pattern.replacementMode -in @('full', 'preserve-group-1', 'preserve-groups-1-2')) "Redaction pattern '$($pattern.id)' has an unsupported replacementMode."
        Test-Regex ([string]$pattern.pattern) "Redaction pattern '$($pattern.id)'"
        if ([string]$pattern.replacementMode -eq 'preserve-group-1') {
            try { Add-Issue (@([regex]::new([string]$pattern.pattern).GetGroupNumbers()).Count -ge 2) "Redaction pattern '$($pattern.id)' must define capture group 1." } catch {}
        }
        if ([string]$pattern.replacementMode -eq 'preserve-groups-1-2') {
            try { Add-Issue (@([regex]::new([string]$pattern.pattern).GetGroupNumbers()).Count -ge 3) "Redaction pattern '$($pattern.id)' must define capture groups 1 and 2." } catch {}
        }
    }
}
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    path = $normalizedPath
    valid = $issues.Count -eq 0
    fingerprint = $(if ($null -ne $policy) { Get-Fingerprint $policy } else { '' })
    issues = @($issues)
}
if ($Action -eq 'get') {
    if (-not $result.valid) { throw "Workspace policy is invalid: $(@($result.issues) -join ' ')" }
    $getResult = if ($WithFingerprint) {
        [pscustomobject][ordered]@{
            policy = $policy
            fingerprint = $result.fingerprint
        }
    } else {
        $policy
    }
    if ($Format -eq 'Json') { $getResult | ConvertTo-Json -Depth 20 } else { $getResult }
} elseif ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 8
} else {
    Write-Host "Workspace policy: valid=$($result.valid), fingerprint=$($result.fingerprint)"
    foreach ($issue in $result.issues) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
