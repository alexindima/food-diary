[CmdletBinding()]
param(
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [object]$PacketInput,
    [object]$StatusInput,
    [ValidateRange(1, 100)]
    [int]$Limit = 20,
    [switch]$Compact,
    [ValidateSet('Markdown', 'Json')]
    [string]$Format = 'Markdown',
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path

function Resolve-RepositoryPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return Join-Path $repositoryRoot $Path
}
function ConvertTo-MarkdownText([object]$Value) {
    if ($null -eq $Value) { return '' }
    return ([string]$Value).Replace("`r", ' ').Replace("`n", ' ').Replace('|', '\|')
}

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+(?:/.*)?$') {
    throw 'WorkspacePath must be inside .artifacts/llm-wiki/tasks/<task-name>.'
}
$absoluteWorkspacePath = Resolve-RepositoryPath $normalizedWorkspacePath
$descriptorPath = Join-Path $absoluteWorkspacePath 'workspace.json'
if (-not (Test-Path -LiteralPath $descriptorPath -PathType Leaf)) {
    throw "Task workspace does not exist or is incomplete: $normalizedWorkspacePath"
}
$descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
$taskContract = Get-Content -LiteralPath (Join-Path $absoluteWorkspacePath 'task-contract.json') -Raw | ConvertFrom-Json
$acceptance = Get-Content -LiteralPath (Join-Path $absoluteWorkspacePath 'acceptance-matrix.json') -Raw | ConvertFrom-Json
$evidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspacePath 'evidence.json') -Raw | ConvertFrom-Json
$journal = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') show `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json

$packet = if ($null -ne $PacketInput) {
    $PacketInput
} else {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiChangePacket.ps1') `
        -BaseRef $taskContract.git.base `
        -Objective $descriptor.objective `
        -Format Json | ConvertFrom-Json
}
$status = if ($null -ne $StatusInput) {
    $StatusInput
} else {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') status `
        -WorkspacePath $normalizedWorkspacePath `
        -PacketInput $packet `
        -Format Json | ConvertFrom-Json
}

$changedPaths = @($packet.diff.changedPaths)
$includedPaths = @($changedPaths | Select-Object -First $Limit)
$checks = @($evidence.checks | ForEach-Object {
    [pscustomobject][ordered]@{
        id = $_.id
        status = $_.status
        command = $_.command
        logPath = $_.logPath
        reason = $_.reason
    }
})
$reviews = @($evidence.reviews | ForEach-Object {
    [pscustomobject][ordered]@{
        id = $_.id
        status = $_.status
        description = $_.description
        reason = $_.reason
    }
})
$criteria = @($acceptance.criteria | ForEach-Object {
    [pscustomobject][ordered]@{
        id = $_.id
        text = $_.text
        status = $_.status
        scenarioIds = @($_.mapping.scenarioIds)
        checkIds = @($_.mapping.checkIds)
        reviewIds = @($_.mapping.reviewIds)
        testPaths = @($_.mapping.testPaths)
        evidenceNote = $_.evidenceNote
    }
})
$sealed = Test-Path -LiteralPath (Join-Path $absoluteWorkspacePath 'completion.json') -PathType Leaf
$taskGraph = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskGraph.ps1') -IncludeSealed:$sealed -Format Json | ConvertFrom-Json
$graphNode = $taskGraph.nodes | Where-Object name -eq (Split-Path -Leaf $normalizedWorkspacePath) | Select-Object -First 1
$taskLeases = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -Format Json | ConvertFrom-Json
$activeLease = $taskLeases.leases | Where-Object { $_.active -and $_.workspace -eq $normalizedWorkspacePath } | Select-Object -First 1
$taskDispatches = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') list -Format Json | ConvertFrom-Json
$workspaceDispatches = @($taskDispatches.dispatches | Where-Object workspace -eq $normalizedWorkspacePath | Sort-Object startedAtUtc -Descending)
$currentDispatch = $workspaceDispatches | Where-Object state -in @('running', 'orphaned', 'packet-drift', 'context-drift', 'invalid') | Select-Object -First 1
if ($null -eq $currentDispatch) { $currentDispatch = $workspaceDispatches | Select-Object -First 1 }
$dispatchMetrics = & (Join-Path $PSScriptRoot 'Get-LlmWikiDispatchMetrics.ps1') -Format Json | ConvertFrom-Json
$ownerReliability = if ($null -ne $currentDispatch) {
    $dispatchMetrics.owners | Where-Object owner -eq $currentDispatch.owner | Select-Object -First 1
} else { $null }
$capabilityReliability = if ($null -ne $currentDispatch) {
    @($dispatchMetrics.capabilityProfiles | Where-Object {
        $_.owner -eq $currentDispatch.owner -and $_.capability -in @($currentDispatch.requiredCapabilities)
    })
} else { @() }
$agentRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAgentRegistry.ps1') list -Format Json | ConvertFrom-Json
$registeredAgent = if ($null -ne $currentDispatch) {
    $agentRegistry.agents | Where-Object { $_.active -and $_.owner -eq $currentDispatch.owner } | Select-Object -First 1
} else { $null }
$orchestrationLineage = & (Join-Path $PSScriptRoot 'Test-LlmWikiOrchestrationLineage.ps1') -Format Json | ConvertFrom-Json
$circuitRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiWorkspaceCircuit.ps1') list -Format Json | ConvertFrom-Json
$workspaceCircuit = $circuitRegistry.circuits | Where-Object workspace -eq $normalizedWorkspacePath | Select-Object -First 1
$verificationPlanPath = Join-Path $absoluteWorkspacePath 'verification-plan.json'
$verificationPlan = if (Test-Path -LiteralPath $verificationPlanPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationPlan.ps1') show `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$modelRoutingPath = Join-Path $absoluteWorkspacePath 'model-routing.json'
$modelRouting = if (Test-Path -LiteralPath $modelRoutingPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRouting.ps1') show `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$modelRoutingOutcomePath = Join-Path $absoluteWorkspacePath 'model-routing-outcome.json'
$modelRoutingOutcome = if (Test-Path -LiteralPath $modelRoutingOutcomePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRoutingOutcome.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$modelRoutingOutcomeMetrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRoutingOutcome.ps1') metrics -Format Json | ConvertFrom-Json
$modelRoutingOutcomeHealth = & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRoutingOutcome.ps1') health -Format Json | ConvertFrom-Json
$instructionOutcomePath = Join-Path $absoluteWorkspacePath 'instruction-outcome.json'
$instructionOutcome = if (Test-Path -LiteralPath $instructionOutcomePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionOutcome.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$instructionOutcomeMetrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionOutcome.ps1') metrics -Format Json | ConvertFrom-Json
$instructionOutcomeCandidates = & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionOutcome.ps1') candidates -Format Json | ConvertFrom-Json
$instructionExperiments = & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionExperiment.ps1') list -Format Json | ConvertFrom-Json
$instructionExperimentForecasts = @($instructionExperiments.experiments | Where-Object state -eq 'active' | ForEach-Object {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionExperiment.ps1') forecast -Id $_.experimentId -Format Json | ConvertFrom-Json
})
$planReusePath = Join-Path $absoluteWorkspacePath 'plan-reuse.json'
$planReuse = if (Test-Path -LiteralPath $planReusePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskSimilarity.ps1') show `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$riskCalibrationPath = Join-Path $absoluteWorkspacePath 'risk-calibration.json'
$riskCalibration = if (Test-Path -LiteralPath $riskCalibrationPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiRiskCalibration.ps1') show `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$planConformancePath = Join-Path $absoluteWorkspacePath 'plan-conformance.json'
$planConformance = if (Test-Path -LiteralPath $planConformancePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiPlanConformance.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiPlanConformance.ps1') assess `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
}
$proofOfChangePath = Join-Path $absoluteWorkspacePath 'proof-of-change.json'
$proofOfChange = if (Test-Path -LiteralPath $proofOfChangePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') assess `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
}
$requirementModelPath = Join-Path $absoluteWorkspacePath 'requirement-model.json'
$requirementModel = if (Test-Path -LiteralPath $requirementModelPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiRequirementModel.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiRequirementModel.ps1') assess `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
}
$impactSimulationPath = Join-Path $absoluteWorkspacePath 'impact-simulation.json'
$impactSimulation = if (Test-Path -LiteralPath $impactSimulationPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiImpactSimulation.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiImpactSimulation.ps1') assess `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
}
$repairLoopPath = Join-Path $absoluteWorkspacePath 'repair-loop.json'
$repairLoop = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLoop.ps1') `
    $(if (Test-Path -LiteralPath $repairLoopPath -PathType Leaf) { 'verify' } else { 'show' }) `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$repairLearningCandidates = if (Test-Path -LiteralPath $repairLoopPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLearning.ps1') candidates `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$failurePrediction = & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') assess `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$verificationCost = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationCost.ps1') assess `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$verificationTelemetryValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') verify -Format Json | ConvertFrom-Json
$verificationTelemetry = if ($verificationTelemetryValidation.valid) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -Format Json | ConvertFrom-Json
} else {
    [pscustomobject]@{ valid = $false; registryHash = $verificationTelemetryValidation.registryHash; metrics = @(); issues = @($verificationTelemetryValidation.issues) }
}
$contextBundlePath = Join-Path $absoluteWorkspacePath 'context-bundle.json'
$contextBundle = if (Test-Path -LiteralPath $contextBundlePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') show `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$contextBudgetPath = Join-Path $absoluteWorkspacePath 'context-budget.json'
$contextBudget = if (Test-Path -LiteralPath $contextBudgetPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBudget.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$contextBenchmarkPath = Join-Path $absoluteWorkspacePath 'context-benchmark.json'
$contextBenchmark = if (Test-Path -LiteralPath $contextBenchmarkPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBenchmark.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$contextExperimentPath = Join-Path $absoluteWorkspacePath 'context-experiment.json'
$contextExperiment = if (Test-Path -LiteralPath $contextExperimentPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextExperiment.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$contextStrategyPath = Join-Path $absoluteWorkspacePath 'context-strategy-application.json'
$contextStrategy = if (Test-Path -LiteralPath $contextStrategyPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextStrategy.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$contextStrategyOutcomePath = Join-Path $absoluteWorkspacePath 'context-strategy-outcome.json'
$contextStrategyOutcome = if (Test-Path -LiteralPath $contextStrategyOutcomePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$contextStrategyOutcomeMetrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') metrics -Format Json | ConvertFrom-Json
$contextStrategyOutcomeHealth = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') health -Format Json | ConvertFrom-Json
$contextSecurityPath = Join-Path $absoluteWorkspacePath 'context-security.json'
$contextSecurity = if (Test-Path -LiteralPath $contextSecurityPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$contextFeedback = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextFeedback.ps1') metrics -Format Json | ConvertFrom-Json
$contextFeedbackProfiles = if ($null -ne $contextBundle) {
    @($contextFeedback.metrics.profiles | Where-Object path -in @($contextBundle.bundle.items.path))
} else { @() }
$confidenceLedgerPath = Join-Path $absoluteWorkspacePath 'confidence-ledger.json'
$confidenceLedger = if (Test-Path -LiteralPath $confidenceLedgerPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiConfidenceLedger.ps1') verify -WorkspacePath $normalizedWorkspacePath -Format Json | ConvertFrom-Json
} else {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiConfidenceLedger.ps1') assess -WorkspacePath $normalizedWorkspacePath -Format Json | ConvertFrom-Json
}
$changeCritiquePath = Join-Path $absoluteWorkspacePath 'change-critique.json'
$changeCritique = if (Test-Path -LiteralPath $changeCritiquePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeCritique.ps1') verify -WorkspacePath $normalizedWorkspacePath -Format Json | ConvertFrom-Json
} else {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeCritique.ps1') assess -WorkspacePath $normalizedWorkspacePath -Format Json | ConvertFrom-Json
}
$retrospectivePath = Join-Path $absoluteWorkspacePath 'retrospective.json'
$retrospective = if (Test-Path -LiteralPath $retrospectivePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') verify -WorkspacePath $normalizedWorkspacePath -Format Json | ConvertFrom-Json
} else { $null }
$durableMemory = & (Join-Path $PSScriptRoot 'Manage-LlmWikiMemory.ps1') relevant `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$memoryCandidates = & (Join-Path $PSScriptRoot 'Manage-LlmWikiMemory.ps1') candidates `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$learningCandidates = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') candidates `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$learningExperiments = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningExperiment.ps1') active `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$learningHealth = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningHealth.ps1') list -Format Json | ConvertFrom-Json
$workspaceLearningHealth = @($learningHealth.health | Where-Object { @($_.observations.workspace) -contains $normalizedWorkspacePath })
$evalPromotion = & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvalPromotion.ps1') list -Format Json | ConvertFrom-Json
$workspaceEvalCandidates = @($evalPromotion.candidates | Where-Object workspace -eq $normalizedWorkspacePath)
$sourceAnchors = @(
    if ($null -ne $contextBundle -and $contextBundle.PSObject.Properties['bundle']) {
        foreach ($item in @($contextBundle.bundle.items | Select-Object -First $Limit)) {
            if (-not $item.PSObject.Properties['path'] -or [string]::IsNullOrWhiteSpace([string]$item.path)) { continue }
            $line = if ($item.PSObject.Properties['line'] -and $null -ne $item.line -and [int]$item.line -gt 0) {
                [int]$item.line
            } elseif ($item.PSObject.Properties['excerpt'] -and $null -ne $item.excerpt -and $item.excerpt.PSObject.Properties['startLine'] -and [int]$item.excerpt.startLine -gt 0) {
                [int]$item.excerpt.startLine
            } else { $null }
            [pscustomobject][ordered]@{
                path = [string]$item.path
                line = $line
                anchorStatus = $(if ($null -ne $line) { 'line' } else { 'path' })
                kind = $(if ($item.PSObject.Properties['kind']) { [string]$item.kind } else { 'context' })
                reasons = $(if ($item.PSObject.Properties['reasons']) { @($item.reasons) } else { @() })
            }
        }
    }
)
if ($sourceAnchors.Count -eq 0) {
    $sourceAnchors = @($includedPaths | ForEach-Object {
        [pscustomobject][ordered]@{ path = [string]$_; line = $null; anchorStatus = 'path'; kind = 'changed-path'; reasons = @('Changed path in the current task packet.') }
    })
}
$handoff = [pscustomobject][ordered]@{
    schemaVersion = 1
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    workspace = $normalizedWorkspacePath
    objective = $descriptor.objective
    state = $(if ($sealed) { 'sealed' } elseif ($null -ne $descriptor.decomposition -and [string]$descriptor.decomposition.state -eq 'applied') { 'decomposed' } else { 'in-progress' })
    decomposition = $(if ($null -ne $descriptor.decomposition) { $descriptor.decomposition } else { $null })
    readiness = [pscustomobject][ordered]@{
        verdict = $status.verdict
        score = $status.score
        risk = $status.risk
        blockingDimensions = @($status.blockingDimensions)
        unassessedDimensions = @($status.unassessedDimensions)
    }
    continuity = [pscustomobject][ordered]@{
        initialPacketFingerprint = $status.initialPacketFingerprint
        currentPacketFingerprint = $status.currentPacketFingerprint
        fingerprintChanged = $status.fingerprintChanged
        gitBase = $taskContract.git.base
        gitHeadAtStart = $taskContract.git.headAtStart
    }
    scope = [pscustomobject][ordered]@{
        scopes = @($packet.diff.scopes)
        modules = @($packet.diff.modules)
        changedPathCount = $changedPaths.Count
        changedPaths = $includedPaths
        omittedChangedPathCount = [Math]::Max(0, $changedPaths.Count - $includedPaths.Count)
        outOfScopePaths = @($status.outOfScopePaths)
        sourceAnchors = $sourceAnchors
    }
    taskGraph = [pscustomobject][ordered]@{
        valid = [bool]$taskGraph.valid
        edgeCount = $(if ($null -ne $graphNode) { [int]$graphNode.edgeCount } else { 0 })
        blockingConflictCount = $(if ($null -ne $graphNode) { [int]$graphNode.blockingConflictCount } else { 0 })
        prerequisiteTasks = $(if ($null -ne $graphNode) { @($graphNode.prerequisiteTasks) } else { @() })
        dependentTasks = $(if ($null -ne $graphNode) { @($graphNode.dependentTasks) } else { @() })
        relatedEdges = @($taskGraph.edges | Where-Object { $_.left -eq (Split-Path -Leaf $normalizedWorkspacePath) -or $_.right -eq (Split-Path -Leaf $normalizedWorkspacePath) })
    }
    lease = $activeLease
    dispatch = $currentDispatch
    dispatchHistoryCount = $workspaceDispatches.Count
    circuit = $workspaceCircuit
    verificationPlan = $verificationPlan
    modelRouting = $modelRouting
    modelRoutingOutcome = $modelRoutingOutcome
    modelRoutingOutcomeMetrics = $modelRoutingOutcomeMetrics
    modelRoutingOutcomeHealth = $modelRoutingOutcomeHealth
    instructionOutcome = $instructionOutcome
    instructionOutcomeMetrics = $instructionOutcomeMetrics
    instructionOutcomeCandidates = $instructionOutcomeCandidates
    instructionExperiments = $instructionExperiments
    instructionExperimentForecasts = $instructionExperimentForecasts
    planReuse = $planReuse
    riskCalibration = $riskCalibration
    planConformance = $planConformance
    proofOfChange = $proofOfChange
    requirementModel = $requirementModel
    impactSimulation = $impactSimulation
    repairLoop = $repairLoop
    repairLearningCandidates = $repairLearningCandidates
    failurePrediction = $failurePrediction
    verificationCost = $verificationCost
    verificationTelemetry = [pscustomobject][ordered]@{
        valid = $verificationTelemetry.valid
        registryHash = $verificationTelemetry.registryHash
        metrics = @($verificationTelemetry.metrics | Where-Object checkId -in @($evidence.checks.id))
    }
    contextBundle = $contextBundle
    contextBudget = $contextBudget
    contextBenchmark = $contextBenchmark
    contextExperiment = $contextExperiment
    contextStrategy = $contextStrategy
    contextStrategyOutcome = $contextStrategyOutcome
    contextStrategyOutcomeMetrics = $contextStrategyOutcomeMetrics
    contextStrategyOutcomeHealth = $contextStrategyOutcomeHealth
    contextSecurity = $contextSecurity
    confidenceLedger = $confidenceLedger
    changeCritique = $changeCritique
    retrospective = $retrospective
    learningPromotion = [pscustomobject][ordered]@{
        valid = $learningCandidates.valid
        registryFingerprint = $learningCandidates.registryFingerprint
        eligibleCandidateCount = [int]$learningCandidates.eligibleCount
        appliedCandidateCount = [int]$learningCandidates.appliedCount
        rolledBackCandidateCount = [int]$learningCandidates.rolledBackCount
        candidates = @($learningCandidates.candidates)
    }
    learningExperiments = [pscustomobject][ordered]@{
        valid = $learningExperiments.valid
        registryFingerprint = $learningExperiments.registryFingerprint
        activeCandidateCount = @($learningExperiments.experiments).Count
        experiments = @($learningExperiments.experiments)
    }
    learningHealth = [pscustomobject][ordered]@{
        valid = $learningHealth.valid
        registryFingerprint = $learningHealth.registryFingerprint
        rollbackRecommendationCount = @($workspaceLearningHealth | Where-Object { $_.currentlyApplied -and $_.recommendation.effectiveVerdict -eq 'rollback' }).Count
        health = $workspaceLearningHealth
    }
    evalPromotion = [pscustomobject][ordered]@{
        valid = $evalPromotion.valid
        registryFingerprint = $evalPromotion.registryFingerprint
        pendingCandidateCount = @($workspaceEvalCandidates | Where-Object decision -eq 'pending').Count
        appliedCandidateCount = @($workspaceEvalCandidates | Where-Object materialization -eq 'applied').Count
        candidates = $workspaceEvalCandidates
    }
    contextFeedback = [pscustomobject][ordered]@{
        valid = $contextFeedback.valid
        feedbackFingerprint = $contextFeedback.metrics.feedbackFingerprint
        qualityAdjustmentFingerprint = $contextFeedback.metrics.qualityAdjustmentFingerprint
        validReceiptCount = $contextFeedback.metrics.validReceiptCount
        validQualityAdjustmentCount = $contextFeedback.metrics.validQualityAdjustmentCount
        qualityAdjustments = $(if ($null -ne $currentDispatch) { @($contextFeedback.metrics.qualityAdjustmentProfiles | Where-Object dispatchId -eq $currentDispatch.dispatchId) } else { @() })
        profiles = $contextFeedbackProfiles
        ownerQuality = $(if ($null -ne $currentDispatch) { $contextFeedback.metrics.ownerQualityProfiles | Where-Object owner -eq $currentDispatch.owner | Select-Object -First 1 } else { $null })
        capabilityQuality = $(if ($null -ne $currentDispatch) { @($contextFeedback.metrics.capabilityQualityProfiles | Where-Object { $_.owner -eq $currentDispatch.owner -and $_.capability -in @($currentDispatch.requiredCapabilities) }) } else { @() })
    }
    durableMemory = [pscustomobject][ordered]@{
        valid = $durableMemory.valid
        registryFingerprint = $durableMemory.registryFingerprint
        memories = @($durableMemory.memories)
        candidates = @($memoryCandidates.candidates)
        eligibleCandidateCount = [int]$memoryCandidates.eligibleCount
        duplicateCandidateCount = [int]$memoryCandidates.duplicateCandidateCount
    }
    orchestrationLineage = [pscustomobject][ordered]@{
        valid = $orchestrationLineage.valid
        issueCount = $orchestrationLineage.summary.issueCount
        planId = $(if ($null -ne $currentDispatch) { [string]$currentDispatch.schedulePlanId } else { '' })
        planHash = $(if ($null -ne $currentDispatch) { [string]$currentDispatch.schedulePlanHash } else { '' })
        claimId = $(if ($null -ne $currentDispatch) { [string]$currentDispatch.scheduleClaimId } else { '' })
    }
    registeredAgent = $registeredAgent
    dispatchReliability = [pscustomobject][ordered]@{
        windowDays = $dispatchMetrics.windowDays
        globalSuccessRatePercent = $dispatchMetrics.successRatePercent
        globalReconciliationRatePercent = $dispatchMetrics.reconciliationRatePercent
        globalHeartbeatCoveragePercent = $dispatchMetrics.heartbeatCoveragePercent
        globalSloVerdict = $dispatchMetrics.slo.verdict
        globalSloViolations = @($dispatchMetrics.slo.violations)
        owner = $ownerReliability
        capabilityProfiles = $capabilityReliability
    }
    instructions = @($packet.brief.instructions)
    contextPages = @($packet.brief.contextPages | Select-Object -First $Limit)
    acceptanceCriteria = $criteria
    checks = $checks
    reviews = $reviews
    journal = [pscustomobject][ordered]@{
        entryCount = $journal.entryCount
        openCount = $journal.openCount
        openBlockerCount = $journal.openBlockerCount
        entries = @($journal.entries | Select-Object -Last $Limit)
        omittedEntryCount = [Math]::Max(0, $journal.entryCount - [Math]::Min($journal.entryCount, $Limit))
    }
    nextActions = @($status.nextActions)
    resumeCommands = @(
        "./.llm-wiki/wiki.ps1 task-status -WorkspacePath $normalizedWorkspacePath"
        $(if ($null -ne $verificationPlan) { "./.llm-wiki/wiki.ps1 task-verification-run -WorkspacePath $normalizedWorkspacePath -DryRun" } else { "./.llm-wiki/wiki.ps1 task-verification-plan -WorkspacePath $normalizedWorkspacePath" })
        $(if ($null -ne $contextBundle) { "./.llm-wiki/wiki.ps1 task-context-verify -WorkspacePath $normalizedWorkspacePath" } else { "./.llm-wiki/wiki.ps1 task-context-create -WorkspacePath $normalizedWorkspacePath" })
        "./.llm-wiki/wiki.ps1 task-refresh -WorkspacePath $normalizedWorkspacePath"
        "./.llm-wiki/wiki.ps1 task-finish -WorkspacePath $normalizedWorkspacePath -DryRun"
    )
}

$compactJournalEntries = @($handoff.journal.entries | Where-Object { $_.status -eq 'open' -or $_.type -in @('decision', 'blocker') } | Select-Object -Last $Limit)
$compactHandoff = [pscustomobject][ordered]@{
    schemaVersion = $handoff.schemaVersion
    view = 'compact'
    generatedAtUtc = $handoff.generatedAtUtc
    workspace = $handoff.workspace
    objective = $handoff.objective
    state = $handoff.state
    readiness = $handoff.readiness
    continuity = $handoff.continuity
    scope = [pscustomobject][ordered]@{
        scopes = @($handoff.scope.scopes)
        modules = @($handoff.scope.modules)
        changedPathCount = $handoff.scope.changedPathCount
        sourceAnchors = @($handoff.scope.sourceAnchors)
        outOfScopePaths = @($handoff.scope.outOfScopePaths)
    }
    acceptanceCriteria = @($handoff.acceptanceCriteria | Where-Object status -notin @('satisfied', 'not-applicable'))
    checks = @($handoff.checks | Where-Object status -notin @('passed', 'passed-with-known-baseline-failures', 'not-applicable'))
    reviews = @($handoff.reviews | Where-Object status -notin @('completed', 'not-applicable'))
    journal = [pscustomobject][ordered]@{
        openCount = $handoff.journal.openCount
        openBlockerCount = $handoff.journal.openBlockerCount
        entries = $compactJournalEntries
    }
    nextActions = @($handoff.nextActions)
    resumeCommands = @($handoff.resumeCommands)
    authority = 'Executable code, tests, manifests, current documentation, and applicable AGENTS.md files remain authoritative; this handoff is derived context.'
}

if ($Format -eq 'Json') {
    $content = $(if ($Compact) { $compactHandoff } else { $handoff }) | ConvertTo-Json -Depth 12
} elseif ($Compact) {
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('# AI Task Handoff (Compact)')
    $lines.Add('')
    $lines.Add("**Objective:** $(ConvertTo-MarkdownText $compactHandoff.objective)")
    $lines.Add('')
    $lines.Add("**State:** $($compactHandoff.state) | **Readiness:** $($compactHandoff.readiness.verdict) ($($compactHandoff.readiness.score)/100) | **Risk:** $($compactHandoff.readiness.risk.level)")
    $lines.Add('')
    $lines.Add("**Continuity fingerprint:** ``$($compactHandoff.continuity.currentPacketFingerprint)``")
    $lines.Add('')
    $lines.Add("> $($compactHandoff.authority)")
    $lines.Add('')
    $lines.Add('## Source anchors')
    $lines.Add('')
    foreach ($anchor in $compactHandoff.scope.sourceAnchors) {
        $reference = if ($anchor.anchorStatus -eq 'line') { "$($anchor.path):$($anchor.line)" } else { [string]$anchor.path }
        $lines.Add("- ``$reference`` - $(ConvertTo-MarkdownText $anchor.kind)")
    }
    $lines.Add('')
    $lines.Add('## Open acceptance, checks, and reviews')
    $lines.Add('')
    foreach ($criterion in $compactHandoff.acceptanceCriteria) { $lines.Add("- **acceptance/$($criterion.id) [$($criterion.status)]:** $(ConvertTo-MarkdownText $criterion.text)") }
    foreach ($check in $compactHandoff.checks) { $lines.Add("- **check/$($check.id) [$($check.status)]:** ``$(ConvertTo-MarkdownText $check.command)``") }
    foreach ($review in $compactHandoff.reviews) { $lines.Add("- **review/$($review.id) [$($review.status)]:** $(ConvertTo-MarkdownText $review.description)") }
    if (@($compactHandoff.acceptanceCriteria).Count + @($compactHandoff.checks).Count + @($compactHandoff.reviews).Count -eq 0) { $lines.Add('- None.') }
    $lines.Add('')
    $lines.Add('## Decisions and blockers')
    $lines.Add('')
    foreach ($entry in $compactHandoff.journal.entries) { $lines.Add("- **$($entry.id) [$($entry.status)/$($entry.type)]:** $(ConvertTo-MarkdownText $entry.text)") }
    if (@($compactHandoff.journal.entries).Count -eq 0) { $lines.Add('- None.') }
    $lines.Add('')
    $lines.Add('## Next actions')
    $lines.Add('')
    foreach ($action in $compactHandoff.nextActions) { $lines.Add("- $(ConvertTo-MarkdownText $action)") }
    $lines.Add('')
    $lines.Add('## Resume')
    $lines.Add('')
    foreach ($command in $compactHandoff.resumeCommands) { $lines.Add("- ``$command``") }
    $content = $lines -join [Environment]::NewLine
} else {
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('# AI Task Handoff')
    $lines.Add('')
    $lines.Add("**Objective:** $(ConvertTo-MarkdownText $handoff.objective)")
    $lines.Add('')
    $lines.Add("**State:** $($handoff.state) | **Readiness:** $($handoff.readiness.verdict) ($($handoff.readiness.score)/100) | **Risk:** $($handoff.readiness.risk.level)")
    if ($handoff.state -eq 'decomposed') {
        $lines.Add("**Decomposition:** ``$($handoff.decomposition.decompositionId)`` | children=$(@($handoff.decomposition.childWorkspaces).Count)")
    }
    if ($null -ne $handoff.verificationPlan) {
        $lines.Add("**Verification plan:** valid=$($handoff.verificationPlan.valid) | mode=$($handoff.verificationPlan.plan.executionMode) | required=$($handoff.verificationPlan.plan.selectionSummary.requiredCheckCount) | executions=$($handoff.verificationPlan.plan.selectionSummary.executionCount) | reused=$($handoff.verificationPlan.plan.selectionSummary.reusedEvidenceCount) | consolidated=$($handoff.verificationPlan.plan.selectionSummary.consolidatedCheckCount) | savings=$($handoff.verificationPlan.plan.selectionSummary.totalSavingsSeconds)s ($($handoff.verificationPlan.plan.selectionSummary.totalSavingsPercent)%) | hash=``$($handoff.verificationPlan.plan.planHash)``")
    }
    if ($null -ne $handoff.modelRouting) {
        $lines.Add("**Model route:** valid=$($handoff.modelRouting.valid) | route=$($handoff.modelRouting.route.recommendation.routeId) | model=$($handoff.modelRouting.route.recommendation.model) | effort=$($handoff.modelRouting.route.recommendation.reasoningEffort) | complexity=$($handoff.modelRouting.route.signals.complexityScore) | risk-floor=$($handoff.modelRouting.route.signals.riskFloorRank) | receipt=``$($handoff.modelRouting.route.receiptHash)``")
    }
    if ($null -ne $handoff.modelRoutingOutcome) {
        $modelOutcomeEvent = $handoff.modelRoutingOutcome.outcome.registryEvent
        $lines.Add("**Model route outcome:** valid=$($handoff.modelRoutingOutcome.valid) | actual=$($modelOutcomeEvent.actualOutcome.score)/100 | success=$($modelOutcomeEvent.success) | repairs=$($modelOutcomeEvent.actualOutcome.repairAttempts) | event=``$($modelOutcomeEvent.eventHash)``")
    }
    $lines.Add("**Model route learning:** outcomes=$($handoff.modelRoutingOutcomeMetrics.metrics.validEventCount) | degraded routes=$($handoff.modelRoutingOutcomeHealth.degradedProfileCount) | escalation recommended=$($handoff.modelRoutingOutcomeHealth.escalationRecommended) | registry=``$($handoff.modelRoutingOutcomeMetrics.metrics.registryFingerprint)``")
    if ($null -ne $handoff.instructionOutcome) {
        $instructionEvent = $handoff.instructionOutcome.outcome.registryEvent
        $lines.Add("**Instruction outcome:** valid=$($handoff.instructionOutcome.valid) | sources=$(@($instructionEvent.sources).Count) | actual=$($instructionEvent.outcome.score)/100 | success=$($instructionEvent.success) | event=``$($instructionEvent.eventHash)``")
    }
    $lines.Add("**Instruction learning:** outcomes=$($handoff.instructionOutcomeMetrics.metrics.validEventCount) | profiles=$($handoff.instructionOutcomeMetrics.metrics.profileCount) | degraded=$($handoff.instructionOutcomeMetrics.metrics.degradedProfileCount) | candidates=$($handoff.instructionOutcomeCandidates.eligibleCount) | registry=``$($handoff.instructionOutcomeMetrics.metrics.registryFingerprint)``")
    $lines.Add("**Instruction experiments:** active=$($handoff.instructionExperiments.activeCount) | total=$(@($handoff.instructionExperiments.experiments).Count) | registry=``$($handoff.instructionExperiments.registryFingerprint)``")
    if (@($handoff.instructionExperimentForecasts).Count -gt 0) {
        $remainingInstructionSamples = [int](($handoff.instructionExperimentForecasts.forecast.remainingCandidateSamples | Measure-Object -Sum).Sum)
        $lines.Add("**Instruction experiment power:** active forecasts=$(@($handoff.instructionExperimentForecasts).Count) | estimated remaining candidate samples=$remainingInstructionSamples")
    }
    if ($null -ne $handoff.planReuse) {
        $lines.Add("**Plan reuse:** valid=$($handoff.planReuse.valid) | source=``$($handoff.planReuse.receipt.sourceWorkspace)`` | similarity=$($handoff.planReuse.receipt.similarity.score)/100 | receipt=``$($handoff.planReuse.receipt.receiptHash)``")
    }
    if ($null -ne $handoff.contextBundle) {
        $lines.Add("**Context bundle:** valid=$($handoff.contextBundle.valid) | items=$(@($handoff.contextBundle.bundle.items).Count) | hash=``$($handoff.contextBundle.bundle.bundleHash)``")
    }
    if ($null -ne $handoff.contextBudget) {
        $lines.Add("**Context budget:** valid=$($handoff.contextBudget.valid) | verdict=$($handoff.contextBudget.receipt.verdict) | coverage=$($handoff.contextBudget.receipt.metrics.scoreCoveragePercent)% | utilization=$($handoff.contextBudget.receipt.metrics.characterUtilizationPercent)% | receipt=``$($handoff.contextBudget.receipt.receiptHash)``")
    }
    if ($null -ne $handoff.contextBenchmark) {
        $lines.Add("**Context benchmark:** valid=$($handoff.contextBenchmark.valid) | verdict=$($handoff.contextBenchmark.receipt.verdict) | baseline=$($handoff.contextBenchmark.receipt.baseline.qualityScore) | candidate=$($handoff.contextBenchmark.receipt.candidate.qualityScore) | delta=$($handoff.contextBenchmark.receipt.deltas.qualityScore) | receipt=``$($handoff.contextBenchmark.receipt.receiptHash)``")
    }
    if ($null -ne $handoff.contextExperiment) {
        $blockedVariants = @($handoff.contextExperiment.receipt.results | Where-Object { -not $_.adoptionEligible })
        $healthBlockedVariants = @($blockedVariants | Where-Object { 'degraded-outcome-history' -in @($_.adoptionBlocks) })
        $lines.Add("**Context experiment:** valid=$($handoff.contextExperiment.valid) | winner=$($handoff.contextExperiment.receipt.recommendation.variantId) | blocked=$($blockedVariants.Count) | health-blocked=$($healthBlockedVariants.Count) | items=$($handoff.contextExperiment.receipt.recommendation.itemLimit) | characters=$($handoff.contextExperiment.receipt.recommendation.characterBudget) | receipt=``$($handoff.contextExperiment.receipt.receiptHash)``")
    }
    if ($null -ne $handoff.contextStrategy) {
        $lines.Add("**Context strategy:** valid=$($handoff.contextStrategy.valid) | state=$($handoff.contextStrategy.strategy.state) | variant=$($handoff.contextStrategy.strategy.applied.variantId) | quality=$($handoff.contextStrategy.strategy.applied.qualityScore) | failed gates=$(@($handoff.contextStrategy.strategy.postApply.failedGates).Count) | receipt=``$($handoff.contextStrategy.strategy.applicationHash)``")
    }
    if ($null -ne $handoff.contextStrategyOutcome) {
        $outcomeEvent = $handoff.contextStrategyOutcome.outcome.registryEvent
        $lines.Add("**Context strategy outcome:** valid=$($handoff.contextStrategyOutcome.valid) | actual=$($outcomeEvent.actualOutcome.score)/100 | success=$($outcomeEvent.success) | event=``$($outcomeEvent.eventHash)``")
    }
    $lines.Add("**Context strategy learning:** outcomes=$($handoff.contextStrategyOutcomeMetrics.metrics.validEventCount) | cohorts=$(@($handoff.contextStrategyOutcomeMetrics.metrics.cohortProfiles).Count) | registry=``$($handoff.contextStrategyOutcomeMetrics.metrics.registryFingerprint)``")
    $lines.Add("**Context strategy health:** degraded variants=$($handoff.contextStrategyOutcomeHealth.degradedProfileCount) | degraded cohorts=$($handoff.contextStrategyOutcomeHealth.degradedCohortProfileCount) | rollback recommended=$($handoff.contextStrategyOutcomeHealth.rollbackRecommended)")
    if ($null -ne $handoff.contextSecurity) {
        $lines.Add("**Context security:** valid=$($handoff.contextSecurity.valid) | findings=$($handoff.contextSecurity.assessment.summary.findingCount) | quarantined=$($handoff.contextSecurity.assessment.summary.quarantineCount) | hash=``$($handoff.contextSecurity.assessment.assessmentHash)``")
    }
    if ($null -ne $handoff.confidenceLedger) {
        $lines.Add("**Confidence:** score=$($handoff.confidenceLedger.ledger.score)/100 | level=$($handoff.confidenceLedger.ledger.level) | verdict=$($handoff.confidenceLedger.ledger.verdict) | hash=``$($handoff.confidenceLedger.ledger.ledgerHash)``")
    }
    if ($null -ne $handoff.changeCritique) {
        $lines.Add("**Independent critique:** score=$($handoff.changeCritique.critique.score)/100 | verdict=$($handoff.changeCritique.critique.verdict) | findings=$($handoff.changeCritique.critique.summary.findingCount) | hash=``$($handoff.changeCritique.critique.critiqueHash)``")
    }
    if ($null -ne $handoff.retrospective) {
        $lines.Add("**Retrospective:** quality=$($handoff.retrospective.retrospective.outcome.quality) | learning candidates=$($handoff.retrospective.retrospective.summary.candidateCount) | eligible=$($handoff.retrospective.retrospective.summary.eligibleCandidateCount) | hash=``$($handoff.retrospective.retrospective.retrospectiveHash)``")
    }
    $lines.Add("**Learning promotion:** observed=$(@($handoff.learningPromotion.candidates).Count) | eligible for review=$($handoff.learningPromotion.eligibleCandidateCount) | applied=$($handoff.learningPromotion.appliedCandidateCount) | rolled back=$($handoff.learningPromotion.rolledBackCandidateCount) | registry=``$($handoff.learningPromotion.registryFingerprint)``")
    $lines.Add("**Learning experiments:** exposed active canaries=$($handoff.learningExperiments.activeCandidateCount) | registry=``$($handoff.learningExperiments.registryFingerprint)``")
    $lines.Add("**Learning health:** rollback recommendations=$($handoff.learningHealth.rollbackRecommendationCount) | registry=``$($handoff.learningHealth.registryFingerprint)``")
    $lines.Add("**Learned evals:** pending=$($handoff.evalPromotion.pendingCandidateCount) | applied=$($handoff.evalPromotion.appliedCandidateCount) | registry=``$($handoff.evalPromotion.registryFingerprint)``")
    $lines.Add('')
    $lines.Add("**Continuity fingerprint:** ``$($handoff.continuity.currentPacketFingerprint)``")
    if ($null -ne $handoff.lease) {
        $lines.Add('')
        $lines.Add("**Lease:** $($handoff.lease.owner) until ``$($handoff.lease.expiresAtUtc)``")
    }
    if ($null -ne $handoff.dispatch) {
        $lines.Add('')
        $lines.Add("**Dispatch:** ``$($handoff.dispatch.dispatchId)`` | **State:** $($handoff.dispatch.state) | **Events:** $($handoff.dispatch.eventCount)")
        $lines.Add('')
        $lines.Add("**Dispatch chain head:** ``$($handoff.dispatch.headEventHash)``")
        if (-not [string]::IsNullOrWhiteSpace($handoff.orchestrationLineage.planId)) {
            $lines.Add('')
            $lines.Add("**Schedule lineage:** plan ``$($handoff.orchestrationLineage.planId)`` | claim ``$($handoff.orchestrationLineage.claimId)`` | audit valid=$($handoff.orchestrationLineage.valid)")
        }
        if (@($handoff.dispatch.agentCapabilities).Count -gt 0) {
            $lines.Add('')
            $lines.Add("**Dispatched capabilities:** $(@($handoff.dispatch.agentCapabilities) -join ', ')")
        }
        if ($null -ne $handoff.dispatchReliability.owner) {
            $lines.Add('')
            $lines.Add("**Owner reliability ($($handoff.dispatchReliability.windowDays)d):** success=$($handoff.dispatchReliability.owner.successRatePercent)%, heartbeat=$($handoff.dispatchReliability.owner.heartbeatCoveragePercent)%")
        }
        foreach ($profile in @($handoff.dispatchReliability.capabilityProfiles)) {
            $lines.Add("**Capability reliability ($($profile.capability)):** samples=$($profile.terminalCount), success=$($profile.successRatePercent)%, avg=$($profile.averageDurationMinutes)m")
        }
        $lines.Add('')
        $lines.Add("**Dispatch SLO:** $($handoff.dispatchReliability.globalSloVerdict)")
    }
    $lines.Add('')
    if ($null -ne $handoff.circuit) {
        $lines.Add("**Circuit:** $($handoff.circuit.state) | ``$($handoff.circuit.circuitId)`` | until ``$($handoff.circuit.openUntilUtc)``")
        $lines.Add('')
    }
    $lines.Add('> Treat executable code, tests, manifests, current documentation, and applicable AGENTS.md files as authoritative. This handoff is derived context.')
    $lines.Add('')
    $lines.Add('## Scope')
    $lines.Add('')
    $lines.Add("- Areas: $(@($handoff.scope.scopes) -join ', ')")
    $lines.Add("- Modules: $(@($handoff.scope.modules) -join ', ')")
    $lines.Add("- Changed paths: $($handoff.scope.changedPathCount)")
    foreach ($path in $handoff.scope.changedPaths) { $lines.Add("- ``$path``") }
    if ($handoff.scope.omittedChangedPathCount -gt 0) {
        $lines.Add("- ... $($handoff.scope.omittedChangedPathCount) additional path(s) omitted by the context limit.")
    }
    if (@($handoff.scope.outOfScopePaths).Count -gt 0) {
        $lines.Add('')
        $lines.Add('### Out of scope')
        foreach ($path in $handoff.scope.outOfScopePaths) { $lines.Add("- ``$path``") }
    }
    $lines.Add('')
    $lines.Add('## Parallel-task coordination')
    $lines.Add('')
    $lines.Add("- Blocking conflicts: $($handoff.taskGraph.blockingConflictCount)")
    $lines.Add("- Prerequisites: $(@($handoff.taskGraph.prerequisiteTasks) -join ', ')")
    $lines.Add("- Dependents: $(@($handoff.taskGraph.dependentTasks) -join ', ')")
    foreach ($edge in @($handoff.taskGraph.relatedEdges)) {
        $lines.Add("- **$($edge.type)/$($edge.severity):** $(ConvertTo-MarkdownText $edge.recommendation)")
    }
    $lines.Add('')
    $lines.Add('## Applicable instructions and context')
    $lines.Add('')
    foreach ($path in @($handoff.instructions + $handoff.contextPages | Select-Object -Unique)) {
        $lines.Add("- ``$path``")
    }
    $lines.Add('')
    $lines.Add('## Acceptance')
    $lines.Add('')
    foreach ($criterion in $handoff.acceptanceCriteria) {
        $lines.Add("- **$($criterion.id) [$($criterion.status)]:** $(ConvertTo-MarkdownText $criterion.text)")
    }
    $lines.Add('')
    $lines.Add('## Checks and reviews')
    $lines.Add('')
    foreach ($check in $handoff.checks) {
        $lines.Add("- **check/$($check.id) [$($check.status)]:** ``$(ConvertTo-MarkdownText $check.command)``")
    }
    foreach ($review in $handoff.reviews) {
        $lines.Add("- **review/$($review.id) [$($review.status)]:** $(ConvertTo-MarkdownText $review.description)")
    }
    $lines.Add('')
    $lines.Add('## Task journal')
    $lines.Add('')
    foreach ($entry in $handoff.journal.entries) {
        $lines.Add("- **$($entry.id) [$($entry.status)/$($entry.type)]:** $(ConvertTo-MarkdownText $entry.text)")
        if (-not [string]::IsNullOrWhiteSpace([string]$entry.resolution)) {
            $lines.Add("  - Resolution: $(ConvertTo-MarkdownText $entry.resolution)")
        }
    }
    if ($handoff.journal.omittedEntryCount -gt 0) {
        $lines.Add("- ... $($handoff.journal.omittedEntryCount) older journal entry/entries omitted.")
    }
    $lines.Add('')
    $lines.Add('## Next actions')
    $lines.Add('')
    foreach ($action in $handoff.nextActions) { $lines.Add("- $(ConvertTo-MarkdownText $action)") }
    $lines.Add('')
    $lines.Add('## Resume')
    $lines.Add('')
    foreach ($command in $handoff.resumeCommands) { $lines.Add("- ``$command``") }
    $content = $lines -join [Environment]::NewLine
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $absoluteOutputPath = Resolve-RepositoryPath $OutputPath
    $outputDirectory = Split-Path -Parent $absoluteOutputPath
    if (-not (Test-Path -LiteralPath $outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }
    [System.IO.File]::WriteAllText($absoluteOutputPath, $content + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
    Write-Host "Generated AI task handoff: $OutputPath"
} else {
    $content
}
