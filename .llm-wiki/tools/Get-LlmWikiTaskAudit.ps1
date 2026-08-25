[CmdletBinding()]
param(
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [Nullable[int]]$StaleAfterDays,
    [Nullable[int]]$EvidenceMaxAgeDays,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnAttention,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiTaskAuditHelpers.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$workspacePolicy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json
$effectiveStaleAfterDays = if ($null -ne $StaleAfterDays) { [int]$StaleAfterDays } else { [int]$workspacePolicy.audit.staleAfterDays }
$effectiveEvidenceMaxAgeDays = if ($null -ne $EvidenceMaxAgeDays) { [int]$EvidenceMaxAgeDays } else { [int]$workspacePolicy.audit.evidenceMaxAgeDays }
$maximumAuditDays = [int]$workspacePolicy.audit.maximumDays
if ($effectiveStaleAfterDays -lt 1 -or $effectiveStaleAfterDays -gt $maximumAuditDays) {
    throw "StaleAfterDays must be between 1 and $maximumAuditDays."
}
if ($effectiveEvidenceMaxAgeDays -lt 1 -or $effectiveEvidenceMaxAgeDays -gt $maximumAuditDays) {
    throw "EvidenceMaxAgeDays must be between 1 and $maximumAuditDays."
}

if ([System.IO.Path]::IsPathRooted($TasksPath)) { throw 'TasksPath must be repository-relative.' }
$normalizedTasksPath = $TasksPath.Replace('\', '/').TrimEnd('/')
if ($normalizedTasksPath -notmatch '^\.artifacts/llm-wiki/tasks(?:/.*)?$') {
    throw 'TasksPath must be inside .artifacts/llm-wiki/tasks.'
}
$absoluteTasksPath = Join-Path $repositoryRoot $normalizedTasksPath
$auditTime = $AsOfUtc.ToUniversalTime()
$items = [System.Collections.Generic.List[object]]::new()

$currentHead = [string](Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', 'HEAD') -FailureMessage 'Unable to resolve repository HEAD.').Lines[0]
$taskGraph = if ($normalizedTasksPath -ceq '.artifacts/llm-wiki/tasks') {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskGraph.ps1') -TasksPath $normalizedTasksPath -Format Json | ConvertFrom-Json
} else { $null }
$taskLeases = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$taskDispatches = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') list -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$dispatchMetrics = & (Join-Path $PSScriptRoot 'Get-LlmWikiDispatchMetrics.ps1') -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$agentRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAgentRegistry.ps1') list -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$circuitRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiWorkspaceCircuit.ps1') list -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$decompositionRegistry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDecomposition.ps1') list -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$contextFeedback = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextFeedback.ps1') metrics -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$contextOutcomeHealth = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') health -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$durableMemory = & (Join-Path $PSScriptRoot 'Manage-LlmWikiMemory.ps1') verify -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$repairLearning = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLearning.ps1') verify -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$learningPromotion = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') verify -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$learningExperiments = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningExperiment.ps1') verify -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$learningHealth = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningHealth.ps1') verify -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$evalPromotion = & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvalPromotion.ps1') verify -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$verificationTelemetryValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') verify -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
$verificationTelemetry = if ($verificationTelemetryValidation.valid) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -AsOfUtc $auditTime -Format Json | ConvertFrom-Json
} else {
    [pscustomobject]@{
        valid = $false; totalCount = $verificationTelemetryValidation.totalCount; flakyCount = 0
        registryHash = $verificationTelemetryValidation.registryHash; metrics = @(); issues = @($verificationTelemetryValidation.issues)
    }
}
$orchestrationLineage = & (Join-Path $PSScriptRoot 'Test-LlmWikiOrchestrationLineage.ps1') -AsOfUtc $auditTime -Format Json | ConvertFrom-Json

if (Test-Path -LiteralPath $absoluteTasksPath -PathType Container) {
    foreach ($directory in @(Get-ChildItem -LiteralPath $absoluteTasksPath -Directory -Force | Sort-Object Name)) {
        if ($directory.Name -like '.task-start-*' -or $directory.Name -like "$($workspacePolicy.import.stagingPrefix)*") { continue }
        $workspacePath = "$normalizedTasksPath/$($directory.Name)"
        $descriptorPath = Join-Path $directory.FullName 'workspace.json'
        $packetPath = Join-Path $directory.FullName 'change-packet.json'
        $contractPath = Join-Path $directory.FullName 'task-contract.json'
        $evidencePath = Join-Path $directory.FullName 'evidence.json'
        $verificationPlanPath = Join-Path $directory.FullName 'verification-plan.json'
        $modelRoutingPath = Join-Path $directory.FullName 'model-routing.json'
        $modelRoutingOutcomePath = Join-Path $directory.FullName 'model-routing-outcome.json'
        $instructionOutcomePath = Join-Path $directory.FullName 'instruction-outcome.json'
        $planReusePath = Join-Path $directory.FullName 'plan-reuse.json'
        $riskCalibrationPath = Join-Path $directory.FullName 'risk-calibration.json'
        $planConformancePath = Join-Path $directory.FullName 'plan-conformance.json'
        $proofOfChangePath = Join-Path $directory.FullName 'proof-of-change.json'
        $requirementModelPath = Join-Path $directory.FullName 'requirement-model.json'
        $impactSimulationPath = Join-Path $directory.FullName 'impact-simulation.json'
        $repairLoopPath = Join-Path $directory.FullName 'repair-loop.json'
        $failurePredictionPath = Join-Path $directory.FullName 'failure-prediction.json'
        $contextBundlePath = Join-Path $directory.FullName 'context-bundle.json'
        $contextBudgetPath = Join-Path $directory.FullName 'context-budget.json'
        $contextBenchmarkPath = Join-Path $directory.FullName 'context-benchmark.json'
        $contextExperimentPath = Join-Path $directory.FullName 'context-experiment.json'
        $contextStrategyApprovalPath = Join-Path $directory.FullName 'context-strategy-approval.json'
        $contextStrategyApplicationPath = Join-Path $directory.FullName 'context-strategy-application.json'
        $contextStrategyOutcomePath = Join-Path $directory.FullName 'context-strategy-outcome.json'
        $contextSecurityPath = Join-Path $directory.FullName 'context-security.json'
        $confidenceLedgerPath = Join-Path $directory.FullName 'confidence-ledger.json'
        $changeCritiquePath = Join-Path $directory.FullName 'change-critique.json'
        $retrospectivePath = Join-Path $directory.FullName 'retrospective.json'
        $completionPath = Join-Path $directory.FullName 'completion.json'
        $descriptor = Read-Json $descriptorPath
        $packet = Read-Json $packetPath
        $contract = Read-Json $contractPath
        $evidence = Read-Json $evidencePath
        $doctor = $null
        try {
            $doctor = & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') `
                -WorkspacePath $workspacePath `
                -Format Json | ConvertFrom-Json
        } catch {
            $doctor = [pscustomobject]@{
                valid = $false
                migrationRequired = $false
                policyDrift = $false
                workspaceSchemaVersion = 0
                latestWorkspaceSchemaVersion = 0
                storedPolicyFingerprint = ''
                currentPolicyFingerprint = ''
                policyImpact = [pscustomobject]@{
                    snapshotAvailable = $false
                    changeCount = 0
                    affectingChangeCount = 0
                    highestSeverity = 'none'
                    requiredChecks = @()
                }
                errors = @($_.Exception.Message)
            }
        }

        $knownFiles = @(
            'workspace.json'
            'change-packet.json'
            'task-contract.json'
            'change-manifest.json'
            'acceptance-matrix.json'
            'evidence.json'
            'journal.json'
            'review-report.md'
            'verification-plan.json'
            'model-routing.json'
            'model-routing-outcome.json'
            'instruction-outcome.json'
            'plan-reuse.json'
            'risk-calibration.json'
            'plan-conformance.json'
            'proof-of-change.json'
            'requirement-model.json'
            'impact-simulation.json'
            'repair-loop.json'
            'failure-prediction.json'
            'verification-cost.json'
            'context-bundle.json'
            'context-budget.json'
            'context-benchmark.json'
            'context-experiment.json'
            'context-strategy-approval.json'
            'context-strategy-application.json'
            'context-strategy-outcome.json'
            'context-security.json'
            'confidence-ledger.json'
            'change-critique.json'
            'retrospective.json'
            'completion.json'
            'completion.md'
        ) | ForEach-Object { Join-Path $directory.FullName $_ } | Where-Object {
            Test-Path -LiteralPath $_ -PathType Leaf
        } | ForEach-Object { Get-Item -LiteralPath $_ }
        $lastActivity = if (@($knownFiles).Count -gt 0) {
            (@($knownFiles) | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc
        } else {
            $directory.LastWriteTimeUtc
        }
        $contextTimestamp = if ($null -ne $descriptor) {
            $createdAtUtc = Convert-ToUtc (Get-PropertyValue $descriptor 'createdAtUtc') $lastActivity
            Convert-ToUtc (Get-PropertyValue $descriptor 'lastRefreshedAtUtc') $createdAtUtc
        } else {
            $lastActivity
        }
        $evidenceTimestamp = if (Test-Path -LiteralPath $evidencePath -PathType Leaf) {
            (Get-Item -LiteralPath $evidencePath).LastWriteTimeUtc
        } else {
            $lastActivity
        }
        $inactivityDays = Get-AgeDays $lastActivity
        $contextAgeDays = Get-AgeDays $contextTimestamp
        $evidenceAgeDays = Get-AgeDays $evidenceTimestamp

        $baseRef = [string]$contract.git.base
        $baseResolvable = $false
        if (-not [string]::IsNullOrWhiteSpace($baseRef)) {
            $savedErrorActionPreference = $ErrorActionPreference
            try {
                $ErrorActionPreference = 'Continue'
                $null = & git rev-parse --verify "$baseRef^{commit}" 2>$null
                $baseResolvable = $LASTEXITCODE -eq 0
            } finally {
                $ErrorActionPreference = $savedErrorActionPreference
            }
        }
        $packetHead = [string]$packet.inputs.gitHead
        if ([string]::IsNullOrWhiteSpace($packetHead)) { $packetHead = [string]$evidence.git.head }
        $headChanged = -not [string]::IsNullOrWhiteSpace($packetHead) -and $packetHead -cne $currentHead
        $resolvedEvidenceCount = @($evidence.checks | Where-Object status -in @('passed', 'passed-with-known-baseline-failures')).Count +
            @($evidence.reviews | Where-Object status -eq 'completed').Count
        $evidenceExpired = $resolvedEvidenceCount -gt 0 -and $evidenceAgeDays -ge $effectiveEvidenceMaxAgeDays
        $verificationPlan = $null
        if (Test-Path -LiteralPath $verificationPlanPath -PathType Leaf) {
            try {
                $verificationPlan = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationPlan.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $verificationPlan = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $modelRouting = $null
        if (Test-Path -LiteralPath $modelRoutingPath -PathType Leaf) {
            try {
                $modelRouting = & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRouting.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $modelRouting = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $modelRoutingOutcome = $null
        if (Test-Path -LiteralPath $modelRoutingOutcomePath -PathType Leaf) {
            try {
                $modelRoutingOutcome = & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRoutingOutcome.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $modelRoutingOutcome = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $instructionOutcome = $null
        if (Test-Path -LiteralPath $instructionOutcomePath -PathType Leaf) {
            try {
                $instructionOutcome = & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionOutcome.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $instructionOutcome = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $planReuse = $null
        if (Test-Path -LiteralPath $planReusePath -PathType Leaf) {
            try {
                $planReuse = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskSimilarity.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $planReuse = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $riskCalibration = $null
        if (Test-Path -LiteralPath $riskCalibrationPath -PathType Leaf) {
            try {
                $riskCalibration = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRiskCalibration.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $riskCalibration = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $planConformance = $null
        try {
            $planConformance = if (Test-Path -LiteralPath $planConformancePath -PathType Leaf) {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiPlanConformance.ps1') verify -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            } else {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiPlanConformance.ps1') assess -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            }
        } catch {
            $planConformance = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
        }
        $proofOfChange = $null
        try {
            $proofOfChange = if (Test-Path -LiteralPath $proofOfChangePath -PathType Leaf) {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') verify -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            } else {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') assess -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            }
        } catch {
            $proofOfChange = [pscustomobject]@{ valid = $false; applicable = $true; issues = @($_.Exception.Message) }
        }
        $requirementModel = $null
        try {
            $requirementModel = if (Test-Path -LiteralPath $requirementModelPath -PathType Leaf) {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiRequirementModel.ps1') verify -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            } else {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiRequirementModel.ps1') assess -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            }
        } catch {
            $requirementModel = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
        }
        $impactSimulation = $null
        try {
            $impactSimulation = if (Test-Path -LiteralPath $impactSimulationPath -PathType Leaf) {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiImpactSimulation.ps1') verify -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            } else {
                & (Join-Path $PSScriptRoot 'Manage-LlmWikiImpactSimulation.ps1') assess -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            }
        } catch {
            $impactSimulation = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
        }
        $repairLoop = $null
        try {
            $repairLoop = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLoop.ps1') `
                $(if (Test-Path -LiteralPath $repairLoopPath -PathType Leaf) { 'verify' } else { 'show' }) `
                -WorkspacePath $workspacePath `
                -Format Json | ConvertFrom-Json
        } catch {
            $repairLoop = [pscustomobject]@{ valid = $false; activeAttempts = @(); unresolvedAttempts = @(); issues = @($_.Exception.Message) }
        }
        $failurePrediction = $null
        try {
            $failurePrediction = & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') assess `
                -WorkspacePath $workspacePath `
                -Format Json | ConvertFrom-Json
        } catch {
            $failurePrediction = [pscustomobject]@{ valid = $false; calibration = [pscustomobject]@{ falseNegativeCount = 0 }; issues = @($_.Exception.Message) }
        }
        $verificationCost = $null
        try {
            $verificationCost = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationCost.ps1') assess `
                -WorkspacePath $workspacePath `
                -Format Json | ConvertFrom-Json
        } catch {
            $verificationCost = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
        }
        $contextBundle = $null
        if (Test-Path -LiteralPath $contextBundlePath -PathType Leaf) {
            try {
                $contextBundle = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $contextBundle = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $contextBudget = $null
        if (Test-Path -LiteralPath $contextBudgetPath -PathType Leaf) {
            try {
                $contextBudget = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBudget.ps1') verify `
                    -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            } catch {
                $contextBudget = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $contextBenchmark = $null
        if (Test-Path -LiteralPath $contextBenchmarkPath -PathType Leaf) {
            try {
                $contextBenchmark = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBenchmark.ps1') verify `
                    -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            } catch {
                $contextBenchmark = [pscustomobject]@{ valid = $false; regression = $false; issues = @($_.Exception.Message) }
            }
        }
        $contextExperiment = $null
        if (Test-Path -LiteralPath $contextExperimentPath -PathType Leaf) {
            try {
                $contextExperiment = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextExperiment.ps1') verify `
                    -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            } catch {
                $contextExperiment = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $contextStrategy = $null
        if (Test-Path -LiteralPath $contextStrategyApplicationPath -PathType Leaf) {
            try {
                $contextStrategy = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextStrategy.ps1') verify `
                    -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            } catch {
                $contextStrategy = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        } elseif (Test-Path -LiteralPath $contextStrategyApprovalPath -PathType Leaf) {
            $contextStrategy = [pscustomobject]@{ valid = $true; pendingApproval = $true; strategy = $null; issues = @() }
        }
        $contextStrategyOutcome = $null
        if (Test-Path -LiteralPath $contextStrategyOutcomePath -PathType Leaf) {
            try {
                $contextStrategyOutcome = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') verify `
                    -WorkspacePath $workspacePath -Format Json | ConvertFrom-Json
            } catch {
                $contextStrategyOutcome = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $contextSecurity = $null
        if (Test-Path -LiteralPath $contextSecurityPath -PathType Leaf) {
            try {
                $contextSecurity = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $contextSecurity = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $confidenceLedger = $null
        if (Test-Path -LiteralPath $confidenceLedgerPath -PathType Leaf) {
            try {
                $confidenceLedger = & (Join-Path $PSScriptRoot 'Manage-LlmWikiConfidenceLedger.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $confidenceLedger = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $changeCritique = $null
        if (Test-Path -LiteralPath $changeCritiquePath -PathType Leaf) {
            try {
                $changeCritique = & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeCritique.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $changeCritique = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $retrospective = $null
        if (Test-Path -LiteralPath $retrospectivePath -PathType Leaf) {
            try {
                $retrospective = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') verify `
                    -WorkspacePath $workspacePath `
                    -Format Json | ConvertFrom-Json
            } catch {
                $retrospective = [pscustomobject]@{ valid = $false; issues = @($_.Exception.Message) }
            }
        }
        $memoryCandidates = $null
        try {
            $memoryCandidates = & (Join-Path $PSScriptRoot 'Manage-LlmWikiMemory.ps1') candidates `
                -WorkspacePath $workspacePath `
                -AsOfUtc $auditTime `
                -Format Json | ConvertFrom-Json
        } catch {
            $memoryCandidates = [pscustomobject]@{ valid = $false; eligibleCount = 0; duplicateCandidateCount = 0; issues = @($_.Exception.Message); candidates = @() }
        }
        $learningCandidates = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') candidates `
            -WorkspacePath $workspacePath `
            -AsOfUtc $auditTime `
            -Format Json | ConvertFrom-Json
        $workspaceLearningExperiments = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningExperiment.ps1') active `
            -WorkspacePath $workspacePath `
            -AsOfUtc $auditTime `
            -Format Json | ConvertFrom-Json

        $reasons = [System.Collections.Generic.List[string]]::new()
        $actions = [System.Collections.Generic.List[string]]::new()
        $sealed = Test-Path -LiteralPath $completionPath -PathType Leaf
        $status = 'healthy'
        $graphNode = if ($null -ne $taskGraph) { $taskGraph.nodes | Where-Object name -eq $directory.Name | Select-Object -First 1 } else { $null }
        if (-not $doctor.valid) {
            if ($doctor.migrationRequired -and -not $sealed) {
                $status = 'migration-required'
                $reasons.Add("Workspace schema v$($doctor.workspaceSchemaVersion) must be upgraded to v$($doctor.latestWorkspaceSchemaVersion).")
                $actions.Add("./.llm-wiki/wiki.ps1 task-migrate -WorkspacePath $workspacePath")
            } elseif ($doctor.policyDrift) {
                $status = 'policy-drift'
                $reasons.Add("Workspace policy changed from $($doctor.storedPolicyFingerprint) to $($doctor.currentPolicyFingerprint).")
                if ($doctor.policyImpact.snapshotAvailable) {
                    $reasons.Add("Semantic impact: $($doctor.policyImpact.changeCount) change(s), $($doctor.policyImpact.affectingChangeCount) affecting the current task; highest severity $($doctor.policyImpact.highestSeverity).")
                }
                if ($sealed) {
                    $actions.Add('Preserve the sealed history and start or import a new task under the current policy.')
                } else {
                    $actions.Add("./.llm-wiki/wiki.ps1 task-policy-impact -WorkspacePath $workspacePath")
                    $actions.Add("./.llm-wiki/wiki.ps1 task-policy-sync -WorkspacePath $workspacePath -DryRun")
                    if ($doctor.policyImpact.affectingChangeCount -gt 0) {
                        $actions.Add("./.llm-wiki/wiki.ps1 task-policy-sync -WorkspacePath $workspacePath -AcceptPolicyImpact")
                    }
                }
            } else {
                $status = 'invalid'
                foreach ($issue in @($doctor.errors | Select-Object -First 5)) { $reasons.Add([string]$issue) }
                $actions.Add("./.llm-wiki/wiki.ps1 task-doctor -WorkspacePath $workspacePath -FailOnInvalid")
            }
        } elseif ($sealed) {
            $status = 'sealed'
        } else {
            if ($inactivityDays -ge $effectiveStaleAfterDays -or $contextAgeDays -ge $effectiveStaleAfterDays) {
                $status = 'stale'
                $reasons.Add("Task context is at least $([Math]::Max($inactivityDays, $contextAgeDays)) day(s) old.")
            }
            if (-not $baseResolvable) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Git base '$baseRef' cannot be resolved.")
                $actions.Add("Fetch or restore Git base '$baseRef', or intentionally recreate the task workspace.")
            }
            if ($headChanged) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add('Repository HEAD changed since the current packet was compiled.')
            }
            if ($evidenceExpired) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Resolved evidence is $evidenceAgeDays day(s) old; SLA is $effectiveEvidenceMaxAgeDays day(s).")
                $actions.Add("./.llm-wiki/wiki.ps1 task-run -WorkspacePath $workspacePath -IncludePassed")
            }
            if ($null -ne $verificationPlan -and -not $verificationPlan.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Verification plan is invalid: $(@($verificationPlan.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-verification-plan -WorkspacePath $workspacePath")
            }
            if ($null -ne $modelRouting -and -not $modelRouting.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Model routing receipt is invalid: $(@($modelRouting.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-model-route-create -WorkspacePath $workspacePath")
            } elseif ($null -ne $verificationPlan -and $verificationPlan.valid -and $null -eq $modelRouting) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add('Verification plan has no governed model route.')
                $actions.Add("./.llm-wiki/wiki.ps1 task-model-route-create -WorkspacePath $workspacePath")
            }
            if ($null -ne $modelRoutingOutcome -and -not $modelRoutingOutcome.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Model routing outcome is invalid: $(@($modelRoutingOutcome.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 model-route-outcome-verify -WorkspacePath $workspacePath -FailOnInvalid")
            }
            if ($null -ne $instructionOutcome -and -not $instructionOutcome.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Instruction outcome is invalid: $(@($instructionOutcome.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 instruction-outcome-verify -WorkspacePath $workspacePath -FailOnInvalid")
            }
            if ($null -ne $planReuse -and -not $planReuse.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Plan reuse receipt is invalid: $(@($planReuse.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-similarity-verify -WorkspacePath $workspacePath -FailOnInvalid")
            }
            if ($null -ne $riskCalibration -and -not $riskCalibration.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Risk calibration is invalid: $(@($riskCalibration.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-risk-calibrate -WorkspacePath $workspacePath")
            }
            if ($null -ne $planConformance -and -not $planConformance.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Plan conformance is invalid: $(@($planConformance.conformance.policyFindings.id) -join ', ') $(@($planConformance.issues) -join ' ')".Trim())
                $actions.Add("./.llm-wiki/wiki.ps1 task-conformance-replan -WorkspacePath $workspacePath -Reason <rationale>")
            }
            if ($null -ne $proofOfChange -and $proofOfChange.applicable -and -not $proofOfChange.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Proof of change is invalid: $(@($proofOfChange.proof.findings.id) -join ', ') $(@($proofOfChange.issues) -join ' ')".Trim())
                $actions.Add("./.llm-wiki/wiki.ps1 task-proof-assess -WorkspacePath $workspacePath")
            }
            if ($null -ne $requirementModel -and -not $requirementModel.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Requirement model is invalid: $(@($requirementModel.model.findings.id) -join ', ') $(@($requirementModel.issues) -join ' ')".Trim())
                $actions.Add("./.llm-wiki/wiki.ps1 task-requirements-assess -WorkspacePath $workspacePath")
            }
            if ($null -ne $impactSimulation -and -not $impactSimulation.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Impact simulation is invalid: $(@($impactSimulation.simulation.findings.id) -join ', ') $(@($impactSimulation.issues) -join ' ')".Trim())
                $actions.Add("./.llm-wiki/wiki.ps1 task-impact-assess -WorkspacePath $workspacePath")
            }
            if ($null -ne $repairLoop -and (-not $repairLoop.valid -or @($repairLoop.unresolvedAttempts).Count -gt 0)) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Repair loop needs attention: $(@($repairLoop.issues) -join ' ') unresolved=$(@($repairLoop.unresolvedAttempts).Count)".Trim())
                $actions.Add("./.llm-wiki/wiki.ps1 task-repair-show -WorkspacePath $workspacePath")
            }
            if ($null -ne $failurePrediction -and -not $failurePrediction.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Failure prediction is invalid: $(@($failurePrediction.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-failure-predict -WorkspacePath $workspacePath")
            } elseif ([int]$failurePrediction.calibration.falseNegativeCount -gt 0) {
                $reasons.Add("Failure prediction has $($failurePrediction.calibration.falseNegativeCount) false negative(s).")
                $actions.Add("./.llm-wiki/wiki.ps1 repair-learning-candidates -WorkspacePath $workspacePath")
            }
            if ($null -ne $verificationCost -and -not $verificationCost.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Verification cost forecast is invalid: $(@($verificationCost.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-cost-forecast -WorkspacePath $workspacePath")
            }
            if ($null -ne $contextBundle -and -not $contextBundle.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context bundle is invalid: $(@($contextBundle.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-create -WorkspacePath $workspacePath")
            }
            if ($null -ne $contextBudget -and -not $contextBudget.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context budget receipt is invalid: $(@($contextBudget.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-budget-create -WorkspacePath $workspacePath")
            } elseif ($null -ne $contextBudget -and $contextBudget.receipt.verdict -eq 'tune') {
                $reasons.Add("Context budget has tuning recommendations: $(@($contextBudget.receipt.recommendations.id) -join ', ').")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-budget-show -WorkspacePath $workspacePath")
            }
            if ($null -ne $contextBenchmark -and -not $contextBenchmark.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context benchmark receipt is invalid: $(@($contextBenchmark.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-benchmark-create -SourceWorkspacePath <baseline> -WorkspacePath $workspacePath")
            } elseif ($null -ne $contextBenchmark -and $contextBenchmark.regression) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context benchmark regressed by $($contextBenchmark.receipt.deltas.qualityScore) quality point(s).")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-benchmark-show -WorkspacePath $workspacePath")
            }
            if ($null -ne $contextExperiment -and -not $contextExperiment.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context experiment receipt is invalid: $(@($contextExperiment.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-experiment-run -WorkspacePath $workspacePath")
            } elseif ($null -ne $contextExperiment -and $contextExperiment.receipt.recommendation.verdict -eq 'no-safe-variant') {
                if ($status -eq 'healthy') { $status = 'attention' }
                $healthBlockedCount = @($contextExperiment.receipt.results | Where-Object { 'degraded-outcome-history' -in @($_.adoptionBlocks) }).Count
                $reasons.Add("Context experiment found no safe variant; $healthBlockedCount variant(s) were blocked by degraded real-task outcomes.")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-experiment-show -WorkspacePath $workspacePath")
            }
            if ($null -ne $contextStrategy -and -not $contextStrategy.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context strategy application is invalid: $(@($contextStrategy.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-strategy-verify -WorkspacePath $workspacePath")
            } elseif ($null -ne $contextStrategy -and $contextStrategy.pendingApproval) {
                $reasons.Add('Context strategy is approved but not yet applied.')
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-strategy-apply -WorkspacePath $workspacePath")
            } elseif ($null -ne $contextStrategy -and $contextStrategy.strategy.state -eq 'rollback-recommended') {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context strategy post-apply gates failed: $(@($contextStrategy.strategy.postApply.failedGates) -join ', ').")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-strategy-rollback -WorkspacePath $workspacePath -Reason <rationale>")
            }
            if ($null -ne $contextStrategyOutcome -and -not $contextStrategyOutcome.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context strategy outcome is invalid: $(@($contextStrategyOutcome.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 context-outcome-verify -WorkspacePath $workspacePath")
            }
            if ($null -ne $contextSecurity -and -not $contextSecurity.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context security assessment is invalid: $(@($contextSecurity.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-security-create -WorkspacePath $workspacePath")
            } elseif ($null -ne $contextSecurity -and [int]$contextSecurity.assessment.summary.quarantineCount -gt 0) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Context security quarantined $($contextSecurity.assessment.summary.quarantineCount) instruction match(es).")
                $actions.Add("./.llm-wiki/wiki.ps1 task-context-security-show -WorkspacePath $workspacePath")
            }
            if ($null -ne $confidenceLedger -and -not $confidenceLedger.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Confidence ledger is invalid: $(@($confidenceLedger.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-confidence-create -WorkspacePath $workspacePath")
            } elseif ($null -ne $confidenceLedger -and $confidenceLedger.ledger.verdict -ne 'trusted') {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Confidence is $($confidenceLedger.ledger.verdict) at $($confidenceLedger.ledger.score)/100.")
                $actions.Add("./.llm-wiki/wiki.ps1 task-confidence-show -WorkspacePath $workspacePath")
            }
            if ($null -ne $changeCritique -and -not $changeCritique.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Change critique is invalid: $(@($changeCritique.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-critique-create -WorkspacePath $workspacePath")
            } elseif ($null -ne $changeCritique -and $changeCritique.critique.verdict -in @('reject', 'request-changes')) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Independent critique verdict is $($changeCritique.critique.verdict) at $($changeCritique.critique.score)/100.")
                $actions.Add("./.llm-wiki/wiki.ps1 task-critique-show -WorkspacePath $workspacePath")
            }
            if ($null -ne $retrospective -and -not $retrospective.valid) {
                if ($status -eq 'healthy') { $status = 'attention' }
                $reasons.Add("Task retrospective is invalid: $(@($retrospective.issues) -join ' ')")
                $actions.Add("./.llm-wiki/wiki.ps1 task-retrospective-create -WorkspacePath $workspacePath")
            } elseif ($null -ne $retrospective -and [int]$retrospective.retrospective.summary.eligibleCandidateCount -gt 0) {
                $reasons.Add("Retrospective produced $($retrospective.retrospective.summary.eligibleCandidateCount) eligible learning candidate(s).")
                $actions.Add("./.llm-wiki/wiki.ps1 task-retrospective-show -WorkspacePath $workspacePath")
            }
            if (($status -eq 'stale' -or $headChanged) -and
                -not (@($actions) -contains "./.llm-wiki/wiki.ps1 task-refresh -WorkspacePath $workspacePath")) {
                $actions.Insert(0, "./.llm-wiki/wiki.ps1 task-refresh -WorkspacePath $workspacePath")
            }
        }
        if ($null -ne $graphNode -and $graphNode.blockingConflictCount -gt 0 -and
            $status -notin @('invalid', 'migration-required', 'policy-drift', 'sealed')) {
            $status = 'conflict'
            $reasons.Add("Task graph reports $($graphNode.blockingConflictCount) blocking write conflict(s).")
            $actions.Insert(0, './.llm-wiki/wiki.ps1 task-graph -FailOnBlocked')
        }
        $activeLease = $taskLeases.leases | Where-Object { $_.active -and $_.workspace -eq $workspacePath } | Select-Object -First 1
        $workspaceDispatches = @($taskDispatches.dispatches | Where-Object workspace -eq $workspacePath | Sort-Object startedAtUtc -Descending)
        $activeDispatch = $workspaceDispatches | Where-Object state -in @('running', 'orphaned', 'packet-drift', 'context-drift', 'invalid') | Select-Object -First 1
        if ($null -ne $activeDispatch -and $activeDispatch.state -eq 'orphaned' -and
            $status -notin @('invalid', 'migration-required', 'policy-drift', 'sealed')) {
            $status = 'orphaned-dispatch'
            $reasons.Add("Dispatch '$($activeDispatch.dispatchId)' has no active lease.")
            $actions.Insert(0, "./.llm-wiki/wiki.ps1 task-dispatch-fail -DispatchId $($activeDispatch.dispatchId) -Result <reason>")
        } elseif ($null -ne $activeDispatch -and $activeDispatch.state -eq 'packet-drift' -and
            $status -notin @('invalid', 'migration-required', 'policy-drift', 'sealed')) {
            $status = 'dispatch-drift'
            $reasons.Add("Dispatch '$($activeDispatch.dispatchId)' uses packet $($activeDispatch.packetFingerprint), but the workspace now uses $($activeDispatch.currentPacketFingerprint).")
            $actions.Insert(0, "./.llm-wiki/wiki.ps1 task-dispatch-fail -DispatchId $($activeDispatch.dispatchId) -Result <reason>")
        } elseif ($null -ne $activeDispatch -and $activeDispatch.state -eq 'context-drift' -and
            $status -notin @('invalid', 'migration-required', 'policy-drift', 'sealed')) {
            $status = 'dispatch-drift'
            $reasons.Add("Dispatch '$($activeDispatch.dispatchId)' context bundle changed from $($activeDispatch.contextBundleHash) to $($activeDispatch.currentContextBundleHash).")
            $actions.Insert(0, "./.llm-wiki/wiki.ps1 task-dispatch-fail -DispatchId $($activeDispatch.dispatchId) -Result <reason>")
        } elseif ($null -ne $activeDispatch -and $activeDispatch.state -eq 'running' -and $status -eq 'healthy') {
            $status = 'running'
        } elseif ($null -ne $activeLease -and $status -eq 'healthy') {
            $status = 'running'
        }
        $workspaceCircuit = $circuitRegistry.circuits | Where-Object { $_.workspace -eq $workspacePath -and $_.open } | Select-Object -First 1
        if ($null -ne $workspaceCircuit -and $status -notin @('sealed', 'invalid', 'migration-required', 'policy-drift')) {
            $status = 'circuit-open'
            $reasons.Insert(0, "Workspace circuit is open until $($workspaceCircuit.openUntilUtc): $($workspaceCircuit.reason)")
            $actions.Insert(0, "./.llm-wiki/wiki.ps1 task-circuit-reset -WorkspacePath $workspacePath -Reason <reason>")
        }
        $descriptorDecomposition = Get-PropertyValue $descriptor 'decomposition'
        if ($null -ne $descriptorDecomposition -and [string]$descriptorDecomposition.state -eq 'applied') {
            $status = 'decomposed'
            $reasons.Clear()
            $actions.Clear()
            $reasons.Add("Workspace was decomposed into $(@($descriptorDecomposition.childWorkspaces).Count) child workspace(s).")
            $actions.Add('./.llm-wiki/wiki.ps1 task-graph')
        }

        $items.Add([pscustomobject][ordered]@{
            name = $directory.Name
            path = $workspacePath
            objective = [string](Get-PropertyValue $descriptor 'objective')
            status = $status
            auditedAtUtc = $auditTime.ToString('o')
            lastActivityUtc = $lastActivity.ToUniversalTime().ToString('o')
            inactivityDays = $inactivityDays
            contextAgeDays = $contextAgeDays
            evidenceAgeDays = $evidenceAgeDays
            git = [pscustomobject][ordered]@{
                base = $baseRef
                baseResolvable = $baseResolvable
                packetHead = $packetHead
                currentHead = $currentHead
                headChanged = $headChanged
            }
            resolvedEvidenceCount = $resolvedEvidenceCount
            evidenceExpired = $evidenceExpired
            verificationPlan = $verificationPlan
            modelRouting = $modelRouting
            modelRoutingOutcome = $modelRoutingOutcome
            instructionOutcome = $instructionOutcome
            planReuse = $planReuse
            riskCalibration = $riskCalibration
            planConformance = $planConformance
            proofOfChange = $proofOfChange
            requirementModel = $requirementModel
            impactSimulation = $impactSimulation
            repairLoop = $repairLoop
            failurePrediction = $failurePrediction
            verificationCost = $verificationCost
            contextBundle = $contextBundle
            contextBudget = $contextBudget
            contextBenchmark = $contextBenchmark
            contextExperiment = $contextExperiment
            contextStrategy = $contextStrategy
            contextStrategyOutcome = $contextStrategyOutcome
            contextSecurity = $contextSecurity
            confidenceLedger = $confidenceLedger
            changeCritique = $changeCritique
            retrospective = $retrospective
            memoryCandidates = $memoryCandidates
            learningCandidates = $learningCandidates
            learningExperiments = $workspaceLearningExperiments
            learningHealth = @($learningHealth.health | Where-Object { @($_.observations.workspace) -contains $workspacePath })
            evalCandidates = @($evalPromotion.candidates | Where-Object workspace -eq $workspacePath)
            policyImpact = $doctor.policyImpact
            lease = $activeLease
            dispatch = $activeDispatch
            registeredAgent = $(if ($null -ne $activeDispatch) { $agentRegistry.agents | Where-Object owner -eq $activeDispatch.owner | Select-Object -First 1 } else { $null })
            circuit = $workspaceCircuit
            dispatchHistoryCount = $workspaceDispatches.Count
            taskGraph = [pscustomobject][ordered]@{
                edgeCount = $(if ($null -ne $graphNode) { [int]$graphNode.edgeCount } else { 0 })
                blockingConflictCount = $(if ($null -ne $graphNode) { [int]$graphNode.blockingConflictCount } else { 0 })
                prerequisiteTasks = @(if ($null -ne $graphNode) { @($graphNode.prerequisiteTasks) } else { @() })
                dependentTasks = @(if ($null -ne $graphNode) { @($graphNode.dependentTasks) } else { @() })
            }
            reasons = @($reasons)
            remediation = @($actions)
        })
    }
}

$attentionStates = @('attention', 'stale', 'conflict', 'invalid', 'migration-required', 'policy-drift', 'orphaned-dispatch', 'dispatch-drift', 'circuit-open')
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    tasksPath = $normalizedTasksPath
    auditedAtUtc = $auditTime.ToString('o')
    policy = [pscustomobject][ordered]@{
        staleAfterDays = $effectiveStaleAfterDays
        evidenceMaxAgeDays = $effectiveEvidenceMaxAgeDays
    }
    totalCount = $items.Count
    healthyCount = @($items | Where-Object status -eq 'healthy').Count
    runningCount = @($items | Where-Object status -eq 'running').Count
    sealedCount = @($items | Where-Object status -eq 'sealed').Count
    decomposedCount = @($items | Where-Object status -eq 'decomposed').Count
    attentionCount = @($items | Where-Object status -in $attentionStates).Count + [int]$taskDispatches.invalidCount + [int]$dispatchMetrics.slo.violationCount + [int]$orchestrationLineage.summary.issueCount + [int]$circuitRegistry.invalidReceiptCount + [int]$decompositionRegistry.invalidCount + [int]$contextFeedback.metrics.invalidReceiptCount + [int]$contextFeedback.metrics.invalidQualityAdjustmentCount + [int]$contextOutcomeHealth.degradedProfileCount + [int]$contextOutcomeHealth.degradedCohortProfileCount + @($durableMemory.issues).Count + [int]$durableMemory.staleCount + @($repairLearning.issues).Count + @($learningPromotion.issues).Count + @($learningExperiments.issues).Count + @($learningHealth.issues).Count + [int]$learningHealth.rollbackRecommendationCount + @($evalPromotion.issues).Count + @($verificationTelemetry.issues).Count
    staleCount = @($items | Where-Object status -eq 'stale').Count
    invalidCount = @($items | Where-Object status -eq 'invalid').Count
    migrationRequiredCount = @($items | Where-Object status -eq 'migration-required').Count
    policyDriftCount = @($items | Where-Object status -eq 'policy-drift').Count
    conflictCount = @($items | Where-Object status -eq 'conflict').Count
    orphanedDispatchCount = @($items | Where-Object status -eq 'orphaned-dispatch').Count
    dispatchDriftCount = @($items | Where-Object status -eq 'dispatch-drift').Count
    invalidDispatchCount = [int]$taskDispatches.invalidCount
    dispatchSloViolationCount = [int]$dispatchMetrics.slo.violationCount
    invalidCircuitReceiptCount = [int]$circuitRegistry.invalidReceiptCount
    invalidDecompositionReceiptCount = [int]$decompositionRegistry.invalidCount
    invalidContextFeedbackCount = [int]$contextFeedback.metrics.invalidReceiptCount
    invalidQualityAdjustmentCount = [int]$contextFeedback.metrics.invalidQualityAdjustmentCount
    degradedContextStrategyCount = [int]$contextOutcomeHealth.degradedProfileCount
    degradedContextStrategyCohortCount = [int]$contextOutcomeHealth.degradedCohortProfileCount
    contextStrategyRollbackRecommended = [bool]$contextOutcomeHealth.rollbackRecommended
    invalidMemoryCount = @($durableMemory.issues).Count
    staleMemoryCount = [int]$durableMemory.staleCount
    invalidRepairLearningCount = @($repairLearning.issues).Count
    invalidLearningPromotionCount = @($learningPromotion.issues).Count
    eligibleLearningPromotionCount = [int]$learningPromotion.eligibleCount
    approvedLearningPromotionCount = [int]$learningPromotion.approvedCount
    appliedLearningPromotionCount = [int]$learningPromotion.appliedCount
    rolledBackLearningPromotionCount = [int]$learningPromotion.rolledBackCount
    invalidLearningExperimentCount = @($learningExperiments.issues).Count
    activeLearningExperimentCount = [int]$learningExperiments.activeCount
    successfulLearningExperimentCount = [int]$learningExperiments.successfulCount
    invalidLearningHealthCount = @($learningHealth.issues).Count
    learningRollbackRecommendationCount = [int]$learningHealth.rollbackRecommendationCount
    invalidEvalPromotionCount = @($evalPromotion.issues).Count
    pendingEvalPromotionCount = [int]$evalPromotion.pendingCount
    appliedEvalPromotionCount = [int]$evalPromotion.appliedCount
    invalidVerificationTelemetryCount = @($verificationTelemetry.issues).Count
    flakyVerificationCount = [int]$verificationTelemetry.flakyCount
    eligibleMemoryCandidateCount = [int](($items | ForEach-Object { [int]$_.memoryCandidates.eligibleCount } | Measure-Object -Sum).Sum)
    duplicateMemoryCandidateCount = [int](($items | ForEach-Object { [int]$_.memoryCandidates.duplicateCandidateCount } | Measure-Object -Sum).Sum)
    dispatchMetrics = $dispatchMetrics
    agentRegistry = $agentRegistry
    circuitRegistry = $circuitRegistry
    decompositionRegistry = $decompositionRegistry
    contextFeedback = $contextFeedback.metrics
    durableMemory = $durableMemory
    repairLearning = $repairLearning
    learningPromotion = $learningPromotion
    learningExperiments = $learningExperiments
    learningHealth = $learningHealth
    evalPromotion = $evalPromotion
    verificationTelemetry = $verificationTelemetry
    orchestrationLineage = $orchestrationLineage
    valid = @($items | Where-Object status -in $attentionStates).Count -eq 0 -and [int]$taskDispatches.invalidCount -eq 0 -and [int]$dispatchMetrics.slo.violationCount -eq 0 -and [int]$circuitRegistry.invalidReceiptCount -eq 0 -and [int]$decompositionRegistry.invalidCount -eq 0 -and [int]$contextFeedback.metrics.invalidReceiptCount -eq 0 -and [int]$contextFeedback.metrics.invalidQualityAdjustmentCount -eq 0 -and @($durableMemory.issues).Count -eq 0 -and [int]$durableMemory.staleCount -eq 0 -and @($repairLearning.issues).Count -eq 0 -and @($learningPromotion.issues).Count -eq 0 -and @($learningExperiments.issues).Count -eq 0 -and @($learningHealth.issues).Count -eq 0 -and [int]$learningHealth.rollbackRecommendationCount -eq 0 -and @($evalPromotion.issues).Count -eq 0 -and @($verificationTelemetry.issues).Count -eq 0 -and $orchestrationLineage.valid
    workspaces = @($items)
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
} else {
    Write-Host "Task audit: $($result.totalCount) total, $($result.healthyCount) healthy, $($result.sealedCount) sealed, $($result.attentionCount) need attention."
    foreach ($item in $items) {
        Write-Host " - [$($item.status)] $($item.name): inactive=$($item.inactivityDays)d, context=$($item.contextAgeDays)d, evidence=$($item.evidenceAgeDays)d"
        foreach ($reason in $item.reasons) { Write-Host "   - $reason" }
        foreach ($action in $item.remediation) { Write-Host "   > $action" }
    }
}
if ($FailOnAttention -and -not $result.valid) { exit 1 }
