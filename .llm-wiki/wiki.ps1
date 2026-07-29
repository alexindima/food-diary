[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet(
        'help', 'update', 'lint', 'smoke', 'verify', 'verify-full', 'context', 'trace', 'packet', 'brief', 'implementation-plan', 'plan', 'test-plan', 'decision',
        'dependencies', 'rollout', 'readiness', 'report', 'topology', 'privacy', 'ui', 'domain', 'contracts', 'health', 'hotspots', 'test-gaps', 'debt',
        'diff', 'impact', 'ownership', 'api-compat', 'policy',
        'evidence-init', 'evidence-run', 'evidence-check', 'evidence-review', 'evidence-validate',
        'task-circuit-list', 'task-circuit-open', 'task-circuit-reset', 'task-circuit-verify', 'task-circuit-prune',
        'task-decompose-list', 'task-decompose-plan', 'task-decompose-verify', 'task-decompose-apply', 'task-decompose-prune',
        'task-verification-plan', 'task-verification-show', 'task-verification-verify', 'task-verification-run',
        'task-model-route-create', 'task-model-route-show', 'task-model-route-verify',
        'model-route-outcome-list', 'model-route-outcome-metrics', 'model-route-outcome-health', 'model-route-outcome-verify', 'model-route-outcome-observe',
        'instruction-outcome-list', 'instruction-outcome-metrics', 'instruction-outcome-candidates', 'instruction-outcome-verify', 'instruction-outcome-observe',
        'instruction-experiment-start', 'instruction-experiment-forecast', 'instruction-experiment-evaluate', 'instruction-experiment-stop', 'instruction-experiment-list', 'instruction-experiment-show', 'instruction-experiment-verify',
        'task-risk-calibrate', 'task-risk-show', 'task-risk-verify',
        'task-conformance-assess', 'task-conformance-replan', 'task-conformance-seal', 'task-conformance-show', 'task-conformance-verify',
        'task-proof-assess', 'task-proof-seal', 'task-proof-show', 'task-proof-verify',
        'task-requirements-assess', 'task-requirements-expand', 'task-requirements-seal', 'task-requirements-show', 'task-requirements-verify',
        'impact-simulate', 'task-impact-assess', 'task-impact-seal', 'task-impact-show', 'task-impact-verify',
        'task-repair-suggest', 'task-repair-start', 'task-repair-complete', 'task-repair-fail', 'task-repair-show', 'task-repair-verify',
        'repair-learning-candidates', 'repair-learning-promote', 'repair-learning-list', 'repair-learning-verify', 'repair-learning-relevant',
        'task-failure-assess', 'task-failure-predict', 'task-failure-show', 'task-failure-verify',
        'task-cost-assess', 'task-cost-forecast', 'task-cost-show', 'task-cost-verify',
        'task-confidence-assess', 'task-confidence-create', 'task-confidence-show', 'task-confidence-verify',
        'task-critique-assess', 'task-critique-create', 'task-critique-show', 'task-critique-verify',
        'task-retrospective-assess', 'task-retrospective-create', 'task-retrospective-show', 'task-retrospective-verify',
        'learning-observe', 'learning-candidates', 'learning-list', 'learning-show', 'learning-approve', 'learning-reject', 'learning-supersede', 'learning-plan', 'learning-apply', 'learning-rollback', 'learning-verify',
        'learning-shadow', 'learning-canary-start', 'learning-canary-record', 'learning-canary-evaluate', 'learning-canary-stop', 'learning-experiment-list', 'learning-experiment-show', 'learning-experiment-verify',
        'learning-health-observe', 'learning-health-list', 'learning-health-show', 'learning-health-waive', 'learning-health-reopen', 'learning-health-verify',
        'verification-telemetry-list', 'verification-telemetry-metrics', 'verification-telemetry-verify',
        'task-context-create', 'task-context-show', 'task-context-verify', 'task-context-compare',
        'task-context-budget-assess', 'task-context-budget-create', 'task-context-budget-show', 'task-context-budget-verify',
        'task-context-benchmark', 'task-context-benchmark-create', 'task-context-benchmark-show', 'task-context-benchmark-verify',
        'task-context-experiment-plan', 'task-context-experiment-run', 'task-context-experiment-show', 'task-context-experiment-verify',
        'task-context-strategy-preview', 'task-context-strategy-approve', 'task-context-strategy-apply', 'task-context-strategy-show', 'task-context-strategy-verify', 'task-context-strategy-rollback',
        'context-outcome-observe', 'context-outcome-profile', 'context-outcome-list', 'context-outcome-verify', 'context-outcome-metrics', 'context-outcome-health', 'context-outcome-prune',
        'task-context-security-assess', 'task-context-security-create', 'task-context-security-show', 'task-context-security-verify',
        'task-context-feedback', 'task-context-feedback-list', 'task-context-feedback-verify', 'task-context-feedback-metrics', 'task-context-feedback-prune',
        'task-quality-adjustment', 'task-quality-adjustment-list', 'task-quality-adjustment-verify', 'task-quality-adjustment-metrics', 'task-quality-adjustment-prune',
        'memory-promote', 'memory-candidates', 'memory-list', 'memory-show', 'memory-verify', 'memory-relevant', 'memory-supersede',
        'eval-observe', 'eval-candidates', 'eval-list', 'eval-show', 'eval-approve', 'eval-reject', 'eval-apply', 'eval-rollback', 'eval-verify',
        'task-similarity-profile', 'task-similarity-find', 'task-similarity-clusters', 'task-similarity-reuse', 'task-similarity-show', 'task-similarity-verify',
        'handoff', 'evals', 'failures', 'failure-add', 'workspace-policy', 'task-list', 'task-graph', 'task-schedule', 'task-orchestrate', 'task-orchestration-cycle-list', 'task-orchestration-cycle-verify', 'task-orchestration-cycle-prune', 'task-orchestration-audit', 'task-watchdog', 'task-watchdog-list', 'task-watchdog-verify', 'task-watchdog-prune', 'task-schedule-plan-list', 'task-schedule-plan-create', 'task-schedule-plan-verify', 'task-schedule-plan-claim', 'task-schedule-plan-prune', 'task-agent-list', 'task-agent-register', 'task-agent-heartbeat', 'task-agent-quarantine', 'task-agent-unquarantine', 'task-agent-unregister', 'task-agent-prune', 'task-agent-coverage', 'task-dispatch-list', 'task-dispatch-start', 'task-dispatch-heartbeat', 'task-dispatch-complete', 'task-dispatch-fail', 'task-dispatch-verify', 'task-dispatch-reconcile', 'task-dispatch-prune', 'task-dispatch-metrics', 'task-dispatch-snapshot-list', 'task-dispatch-snapshot-save', 'task-dispatch-snapshot-verify', 'task-dispatch-snapshot-compare', 'task-dispatch-snapshot-prune', 'task-lease-list', 'task-lease-acquire', 'task-lease-heartbeat', 'task-lease-release', 'task-lease-prune', 'task-audit', 'task-start', 'task-status', 'task-refresh', 'task-run', 'task-lineage', 'task-cache-find', 'task-cache-reuse', 'task-handoff', 'task-export', 'task-export-verify', 'task-import', 'task-note', 'task-resolve-note', 'task-journal', 'task-doctor', 'task-migrate', 'task-policy-impact', 'task-policy-sync', 'task-finish', 'task-verify', 'task-init', 'task-show', 'task-validate',
        'manifest-init', 'manifest-show', 'manifest-validate',
        'acceptance-init', 'acceptance-show', 'acceptance-map', 'acceptance-resolve', 'acceptance-validate',
        'catalog', 'symbols', 'frontend', 'frontend-contract', 'backend-contract', 'domain-data', 'configuration', 'quality', 'runtime', 'sensitive-data', 'architecture-health', 'modules'
    )]
    [string]$Command = 'help',

    [string]$Module,
    [string]$Query,
    [ValidateSet('Any', 'Api', 'Backend', 'Frontend', 'Database', 'Tests')]
    [string]$ChangeType = 'Any',
    [ValidateSet('all', 'credential', 'identity', 'health', 'financial', 'privateContent', 'logging', 'boundaries')]
    [string]$PrivacyCategory = 'all',
    [ValidateSet('all', 'components', 'consumers', 'api', 'translations', 'spec-gaps')]
    [string]$FrontendView = 'all',
    [ValidateSet('all', 'types', 'invariants', 'mappings', 'indexes', 'relationships')]
    [string]$DomainView = 'all',
    [ValidateSet('all', 'contracts', 'consumers', 'production', 'tests', 'ambiguous', 'unconsumed')]
    [string]$BackendContractView = 'all',
    [ValidateSet('all', 'drift', 'allowances', 'untracked', 'cycles', 'ambiguous', 'dead-candidates', 'spec-gaps', 'test-gaps', 'debt')]
    [string]$HealthView = 'all',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateSet('portable', 'linux', 'tools')]
    [string]$SmokeGroup = 'portable',
    [ValidateRange(1, 50)]
    [int]$Limit = 12,
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string[]]$ProposedPath,
    [switch]$FailOnUnreviewed,
    [switch]$Check,
    [string]$EvidencePath = '.artifacts/llm-wiki/evidence.json',
    [string]$TaskPath = '.artifacts/llm-wiki/task-contract.json',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$SourceWorkspacePath,
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [ValidateSet('decision', 'assumption', 'blocker', 'learning', 'note')]
    [string]$JournalType = 'note',
    [string]$Text,
    [string]$NoteId,
    [string]$Resolution,
    [string]$ManifestPath = '.artifacts/llm-wiki/change-manifest.json',
    [string]$AcceptancePath = '.artifacts/llm-wiki/acceptance-matrix.json',
    [string]$Id,
    [ValidateSet('pending', 'passed', 'failed', 'completed', 'not-applicable')]
    [string]$Status,
    [string]$EvidenceCommand,
    [string]$Reason,
    [string]$Symptom,
    [string]$RepairAttemptId,
    [string]$RepairCandidateId,
    [string]$RepairHypothesis,
    [string[]]$RepairPath,
    [string]$Cause,
    [string]$Fix,
    [string[]]$PathPattern,
    [string[]]$Verification,
    [string]$Objective,
    [string[]]$Criterion,
    [string]$CriterionId,
    [string[]]$ScenarioId,
    [string[]]$CheckId,
    [string[]]$ReviewId,
    [string[]]$TestPath,
    [ValidateSet('pending', 'satisfied', 'not-applicable', 'rejected')]
    [string]$AcceptanceStatus = 'pending',
    [string]$EvidenceNote,
    [string[]]$AllowedPath,
    [string[]]$ExcludedPath,
    [double]$DurationSeconds,
    [string]$OutputPath,
    [string]$ExportPath,
    [string]$ImportPath,
    [string]$PolicyPath = '.llm-wiki/policies/workspace-policies.json',
    [switch]$Detailed,
    [switch]$IncludeSealed,
    [Nullable[int]]$StaleAfterDays,
    [Nullable[int]]$EvidenceMaxAgeDays,
    [Nullable[int]]$MaxConcurrency,
    [Nullable[int]]$LeaseMinutes,
    [Nullable[int]]$RetentionDays,
    [Nullable[int]]$WindowDays,
    [string]$Owner,
    [string]$LeaseId,
    [string]$DispatchId,
    [string]$AdjustmentId,
    [ValidateSet('rework', 'rollback', 'regression', 'recovery')]
    [string]$QualityAdjustmentType,
    [string[]]$QualityEvidence,
    [ValidateSet('improved', 'neutral', 'degraded')]
    [string]$CanaryOutcome,
    [string[]]$CanaryEvidence,
    [Nullable[int]]$CanaryPercentage,
    [string]$SnapshotId,
    [string]$AgentId,
    [string]$PlanId,
    [string]$CycleId,
    [string[]]$Capability,
    [string[]]$RequiredCapability,
    [Nullable[int]]$Capacity,
    [Nullable[int]]$RegistrationMinutes,
    [Nullable[int]]$QuarantineMinutes,
    [Nullable[int]]$SilentMinutes,
    [string]$WatchdogId,
    [string]$CircuitId,
    [string]$DecompositionId,
    [Nullable[int]]$CooldownMinutes,
    [Nullable[int]]$MaxShards,
    [Nullable[int]]$ContextCharacterBudget,
    [string[]]$HelpfulContextPath,
    [string[]]$NoisyContextPath,
    [string[]]$MissingContextPath,
    [string[]]$MemoryScopePath,
    [string[]]$MemoryTag,
    [string[]]$MemoryEvidence,
    [Nullable[int]]$ReviewAfterDays,
    [switch]$AllowMemoryDuplicate,
    [Nullable[int]]$TtlMinutes,
    [Nullable[int]]$Lane,
    [Nullable[double]]$RoutingScore,
    [string]$Result,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnAttention,
    [switch]$FailOnSensitive,
    [switch]$Overwrite,
    [switch]$AllowPartialScope,
    [switch]$SkipJournal,
    [switch]$RequireEvidence,
    [switch]$FailOnBreaking,
    [switch]$FailOnOutOfScope,
    [switch]$FailOnInvalid,
    [switch]$RequireManifest,
    [switch]$RequireAcceptance,
    [switch]$FailOnNotReady,
    [switch]$FailOnBlocked,
    [switch]$DryRun,
    [switch]$Apply,
    [switch]$AcceptPolicyImpact,
    [switch]$IncludePassed,
    [switch]$ContinueOnFailure,
    [switch]$FailOnFailure,
    [switch]$FailOnRegression,
    [switch]$FailOnGap
)

$ErrorActionPreference = 'Stop'
$toolsRoot = Join-Path $PSScriptRoot 'tools'
. (Join-Path $toolsRoot 'LlmWikiJson.ps1')
Enable-LlmWikiStringDateJsonParsing

function Invoke-WikiTool {
    param(
        [string]$Name,
        [hashtable]$ToolArguments = @{}
    )

    $toolPath = Join-Path $toolsRoot $Name
    $global:LASTEXITCODE = 0
    & $toolPath @ToolArguments
    if (-not $?) {
        exit 1
    }
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

switch ($Command) {
    'update' {
        Invoke-WikiTool 'Invoke-LlmWikiIndexPipeline.ps1'
    }
    'lint' {
        Invoke-WikiTool 'Test-LlmWiki.ps1' @{ Format = $Format }
    }
    'smoke' {
        switch ($SmokeGroup) {
            'portable' { Invoke-WikiTool 'Test-LlmWikiPortable.ps1' }
            'linux' { Invoke-WikiTool 'Test-LlmWikiLinux.ps1' }
            'tools' { Invoke-WikiTool 'Test-LlmWikiTools.ps1' }
        }
    }
    'verify' {
        Invoke-WikiTool 'Get-LlmWikiWorkspacePolicy.ps1' @{ Action = 'validate'; FailOnInvalid = $true }
        Invoke-WikiTool 'Test-LlmWiki.ps1'
        Invoke-WikiTool 'Test-LlmWikiLint.ps1'
        Invoke-WikiTool 'Invoke-LlmWikiIndexPipeline.ps1' @{ Check = $true }
        Invoke-WikiTool 'Invoke-LlmWikiEvals.ps1'
        Invoke-WikiTool 'Manage-LlmWikiFailures.ps1' @{ Action = 'validate' }
        Invoke-WikiTool 'Test-LlmWikiChangePolicy.ps1' @{ FailOnViolation = $true }
        Invoke-WikiTool 'Get-LlmWikiImpact.ps1' @{ FailOnUnreviewed = $true }
    }
    'verify-full' {
        Invoke-WikiTool 'Get-LlmWikiWorkspacePolicy.ps1' @{ Action = 'validate'; FailOnInvalid = $true }
        Invoke-WikiTool 'Test-LlmWiki.ps1'
        Invoke-WikiTool 'Test-LlmWikiLint.ps1'
        Invoke-WikiTool 'Test-LlmWikiPortable.ps1'
        Invoke-WikiTool 'Invoke-LlmWikiFullVerification.ps1'
        Invoke-WikiTool 'Invoke-LlmWikiEvals.ps1'
        Invoke-WikiTool 'Manage-LlmWikiFailures.ps1' @{ Action = 'validate' }
        Invoke-WikiTool 'Test-LlmWikiChangePolicy.ps1' @{ FailOnViolation = $true }
        Invoke-WikiTool 'Get-LlmWikiImpact.ps1' @{ FailOnUnreviewed = $true }
    }
    'context' {
        Invoke-WikiTool 'Find-LlmWikiContext.ps1' @{
            Module = $Module
            Query = $Query
            ChangeType = $ChangeType
            Format = $Format
            Limit = $Limit
        }
    }
    'trace' {
        Invoke-WikiTool 'Find-LlmWikiTrace.ps1' @{
            Query = $Query
            Format = $Format
            Limit = [Math]::Min($Limit, 30)
        }
    }
    'packet' {
        $packetArguments = @{
            BaseRef = $BaseRef
            Format = $Format
            Limit = $Limit
            Objective = $Objective
            OutputPath = $OutputPath
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $packetArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $packetArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Get-LlmWikiChangePacket.ps1' $packetArguments
    }
    'brief' {
        $briefArguments = @{ BaseRef = $BaseRef; Format = $Format; Limit = [Math]::Min($Limit, 20) }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $briefArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $briefArguments.ChangedPath = $ChangedPath }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $briefArguments.ProposedPath = $ProposedPath }
        Invoke-WikiTool 'Get-LlmWikiTaskBrief.ps1' $briefArguments
    }
    { $_ -in @('implementation-plan', 'plan') } {
        $implementationPlanArguments = @{
            BaseRef = $BaseRef
            Format = $Format
            Limit = $Limit
            Objective = $Objective
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $implementationPlanArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $implementationPlanArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Get-LlmWikiImplementationPlan.ps1' $implementationPlanArguments
    }
    'test-plan' {
        $testPlanArguments = @{ BaseRef = $BaseRef; Format = $Format; Limit = [Math]::Min($Limit, 30) }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $testPlanArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $testPlanArguments.ChangedPath = $ChangedPath }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $testPlanArguments.ProposedPath = $ProposedPath }
        Invoke-WikiTool 'Get-LlmWikiTestPlan.ps1' $testPlanArguments
    }
    'decision' {
        $decisionArguments = @{ BaseRef = $BaseRef; Format = $Format }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $decisionArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $decisionArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Get-LlmWikiDecisionContext.ps1' $decisionArguments
    }
    'dependencies' {
        Invoke-WikiTool 'Get-LlmWikiDependencyChanges.ps1' @{ BaseRef = $BaseRef; Format = $Format }
    }
    'rollout' {
        $rolloutArguments = @{ BaseRef = $BaseRef; Format = $Format }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $rolloutArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $rolloutArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Get-LlmWikiRolloutPlan.ps1' $rolloutArguments
    }
    'readiness' {
        $readinessArguments = @{
            BaseRef = $BaseRef
            Objective = $Objective
            ManifestPath = $ManifestPath
            AcceptancePath = $AcceptancePath
            EvidencePath = $EvidencePath
            RequireManifest = $RequireManifest
            RequireAcceptance = $RequireAcceptance
            RequireEvidence = $RequireEvidence
            FailOnNotReady = $FailOnNotReady
            Format = $Format
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $readinessArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $readinessArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Get-LlmWikiReleaseReadiness.ps1' $readinessArguments
    }
    'report' {
        $reportArguments = @{
            BaseRef = $BaseRef
            Objective = $Objective
            ManifestPath = $ManifestPath
            AcceptancePath = $AcceptancePath
            EvidencePath = $EvidencePath
            RequireManifest = $RequireManifest
            RequireAcceptance = $RequireAcceptance
            RequireEvidence = $RequireEvidence
            Format = $(if ($Format -eq 'Json') { 'Json' } else { 'Markdown' })
            OutputPath = $OutputPath
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $reportArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $reportArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Get-LlmWikiReviewReport.ps1' $reportArguments
    }
    'task-start' {
        $taskStartArguments = @{
            Objective = $Objective
            Criterion = $Criterion
            WorkspacePath = $WorkspacePath
            BaseRef = $BaseRef
            AllowedPath = $AllowedPath
            ExcludedPath = $ExcludedPath
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $taskStartArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $taskStartArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Initialize-LlmWikiTaskWorkspace.ps1' $taskStartArguments
    }
    'task-list' {
        Invoke-WikiTool 'Get-LlmWikiTaskWorkspaces.ps1' @{
            TasksPath = $TasksPath
            Detailed = $Detailed
            Format = $Format
        }
    }
    'workspace-policy' {
        Invoke-WikiTool 'Get-LlmWikiWorkspacePolicy.ps1' @{
            Action = 'validate'
            Path = $PolicyPath
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    'task-audit' {
        Invoke-WikiTool 'Get-LlmWikiTaskAudit.ps1' @{
            TasksPath = $TasksPath
            StaleAfterDays = $StaleAfterDays
            EvidenceMaxAgeDays = $EvidenceMaxAgeDays
            AsOfUtc = $AsOfUtc
            FailOnAttention = $FailOnAttention
            Format = $Format
        }
    }
    'task-orchestration-audit' {
        Invoke-WikiTool 'Test-LlmWikiOrchestrationLineage.ps1' @{
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-orchestrate', 'task-orchestration-cycle-list', 'task-orchestration-cycle-verify', 'task-orchestration-cycle-prune') } {
        $cycleAction = @{
            'task-orchestrate' = 'run'
            'task-orchestration-cycle-list' = 'list'
            'task-orchestration-cycle-verify' = 'verify'
            'task-orchestration-cycle-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiOrchestrationCycle.ps1' @{
            Action = $cycleAction
            CycleId = $CycleId
            TasksPath = $TasksPath
            MaxConcurrency = $MaxConcurrency
            TtlMinutes = $TtlMinutes
            AsOfUtc = $AsOfUtc
            Apply = $Apply
            FailOnAttention = $FailOnAttention
            Format = $Format
        }
    }
    'task-graph' {
        Invoke-WikiTool 'Get-LlmWikiTaskGraph.ps1' @{
            TasksPath = $TasksPath
            IncludeSealed = $IncludeSealed
            FailOnConflict = $FailOnBlocked
            Format = $Format
        }
    }
    'task-schedule' {
        Invoke-WikiTool 'Get-LlmWikiTaskSchedule.ps1' @{
            TasksPath = $TasksPath
            MaxConcurrency = $MaxConcurrency
            AgentId = $AgentId
            AsOfUtc = $AsOfUtc
            FailOnBlocked = $FailOnBlocked
            Format = $Format
        }
    }
    { $_ -in @('task-schedule-plan-list', 'task-schedule-plan-create', 'task-schedule-plan-verify', 'task-schedule-plan-claim', 'task-schedule-plan-prune') } {
        $planAction = @{
            'task-schedule-plan-list' = 'list'
            'task-schedule-plan-create' = 'create'
            'task-schedule-plan-verify' = 'verify'
            'task-schedule-plan-claim' = 'claim'
            'task-schedule-plan-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiSchedulePlan.ps1' @{
            Action = $planAction
            PlanId = $PlanId
            TasksPath = $TasksPath
            MaxConcurrency = $MaxConcurrency
            TtlMinutes = $TtlMinutes
            AsOfUtc = $AsOfUtc
            Apply = $Apply
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-agent-list', 'task-agent-register', 'task-agent-heartbeat', 'task-agent-quarantine', 'task-agent-unquarantine', 'task-agent-unregister', 'task-agent-prune') } {
        $agentAction = @{
            'task-agent-list' = 'list'
            'task-agent-register' = 'register'
            'task-agent-heartbeat' = 'heartbeat'
            'task-agent-quarantine' = 'quarantine'
            'task-agent-unquarantine' = 'unquarantine'
            'task-agent-unregister' = 'unregister'
            'task-agent-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiAgentRegistry.ps1' @{
            Action = $agentAction
            Owner = $Owner
            AgentId = $AgentId
            Capability = $Capability
            Capacity = $Capacity
            RegistrationMinutes = $RegistrationMinutes
            QuarantineMinutes = $QuarantineMinutes
            Reason = $Reason
            AsOfUtc = $AsOfUtc
            Format = $Format
        }
    }
    { $_ -in @('task-watchdog', 'task-watchdog-list', 'task-watchdog-verify', 'task-watchdog-prune') } {
        $watchdogAction = @{
            'task-watchdog' = 'run'
            'task-watchdog-list' = 'list'
            'task-watchdog-verify' = 'verify'
            'task-watchdog-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiDispatchWatchdog.ps1' @{
            Action = $watchdogAction
            WatchdogId = $WatchdogId
            SilentMinutes = $SilentMinutes
            QuarantineMinutes = $QuarantineMinutes
            AsOfUtc = $AsOfUtc
            Apply = $Apply
            FailOnAttention = $FailOnAttention
            Format = $Format
        }
    }
    { $_ -in @('task-circuit-list', 'task-circuit-open', 'task-circuit-reset', 'task-circuit-verify', 'task-circuit-prune') } {
        $circuitAction = @{
            'task-circuit-list' = 'list'
            'task-circuit-open' = 'open'
            'task-circuit-reset' = 'reset'
            'task-circuit-verify' = 'verify'
            'task-circuit-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiWorkspaceCircuit.ps1' @{
            Action = $circuitAction
            WorkspacePath = $WorkspacePath
            CircuitId = $CircuitId
            Reason = $Reason
            CooldownMinutes = $CooldownMinutes
            AsOfUtc = $AsOfUtc
            FailOnOpen = $FailOnAttention
            Format = $Format
        }
    }
    { $_ -in @('task-decompose-list', 'task-decompose-plan', 'task-decompose-verify', 'task-decompose-apply', 'task-decompose-prune') } {
        $decompositionAction = @{
            'task-decompose-list' = 'list'
            'task-decompose-plan' = 'create'
            'task-decompose-verify' = 'verify'
            'task-decompose-apply' = 'apply'
            'task-decompose-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiTaskDecomposition.ps1' @{
            Action = $decompositionAction
            WorkspacePath = $WorkspacePath
            DecompositionId = $DecompositionId
            MaxShards = $MaxShards
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-verification-plan', 'task-verification-show', 'task-verification-verify', 'task-verification-run') } {
        $verificationAction = @{
            'task-verification-plan' = 'create'
            'task-verification-show' = 'show'
            'task-verification-verify' = 'verify'
            'task-verification-run' = 'run'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiVerificationPlan.ps1' @{
            Action = $verificationAction
            WorkspacePath = $WorkspacePath
            IncludePassed = $IncludePassed
            ContinueOnFailure = $ContinueOnFailure
            DryRun = $DryRun
            FailOnInvalid = $FailOnInvalid
            FailOnFailure = $FailOnFailure
            AsOfUtc = $AsOfUtc
            Format = $Format
        }
    }
    { $_ -in @('task-model-route-create', 'task-model-route-show', 'task-model-route-verify') } {
        $modelRouteAction = @{
            'task-model-route-create' = 'create'
            'task-model-route-show' = 'show'
            'task-model-route-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiModelRouting.ps1' @{
            Action = $modelRouteAction
            WorkspacePath = $WorkspacePath
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('model-route-outcome-list', 'model-route-outcome-metrics', 'model-route-outcome-health', 'model-route-outcome-verify', 'model-route-outcome-observe') } {
        $modelOutcomeAction = @{
            'model-route-outcome-list' = 'list'
            'model-route-outcome-metrics' = 'metrics'
            'model-route-outcome-health' = 'health'
            'model-route-outcome-verify' = 'verify'
            'model-route-outcome-observe' = 'observe'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiModelRoutingOutcome.ps1' @{
            Action = $modelOutcomeAction
            WorkspacePath = $(if ($Command -eq 'model-route-outcome-observe' -or ($Command -eq 'model-route-outcome-verify' -and $PSBoundParameters.ContainsKey('WorkspacePath'))) { $WorkspacePath } else { $null })
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('instruction-outcome-list', 'instruction-outcome-metrics', 'instruction-outcome-candidates', 'instruction-outcome-verify', 'instruction-outcome-observe') } {
        $instructionOutcomeAction = @{
            'instruction-outcome-list' = 'list'
            'instruction-outcome-metrics' = 'metrics'
            'instruction-outcome-candidates' = 'candidates'
            'instruction-outcome-verify' = 'verify'
            'instruction-outcome-observe' = 'observe'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiInstructionOutcome.ps1' @{
            Action = $instructionOutcomeAction
            WorkspacePath = $(if ($Command -eq 'instruction-outcome-observe' -or ($Command -eq 'instruction-outcome-verify' -and $PSBoundParameters.ContainsKey('WorkspacePath'))) { $WorkspacePath } else { $null })
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('instruction-experiment-start', 'instruction-experiment-forecast', 'instruction-experiment-evaluate', 'instruction-experiment-stop', 'instruction-experiment-list', 'instruction-experiment-show', 'instruction-experiment-verify') } {
        $instructionExperimentAction = @{
            'instruction-experiment-start' = 'start'
            'instruction-experiment-forecast' = 'forecast'
            'instruction-experiment-evaluate' = 'evaluate'
            'instruction-experiment-stop' = 'stop'
            'instruction-experiment-list' = 'list'
            'instruction-experiment-show' = 'show'
            'instruction-experiment-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiInstructionExperiment.ps1' @{
            Action = $instructionExperimentAction
            Id = $Id
            Reason = $Reason
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-context-create', 'task-context-show', 'task-context-verify', 'task-context-compare') } {
        $contextAction = @{
            'task-context-create' = 'create'
            'task-context-show' = 'show'
            'task-context-verify' = 'verify'
            'task-context-compare' = 'compare'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiContextBundle.ps1' @{
            Action = $contextAction
            WorkspacePath = $WorkspacePath
            SourceWorkspacePath = $SourceWorkspacePath
            Limit = $(if ($PSBoundParameters.ContainsKey('Limit')) { $Limit } else { $null })
            CharacterBudget = $ContextCharacterBudget
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-context-budget-assess', 'task-context-budget-create', 'task-context-budget-show', 'task-context-budget-verify') } {
        $contextBudgetAction = @{
            'task-context-budget-assess' = 'assess'
            'task-context-budget-create' = 'create'
            'task-context-budget-show' = 'show'
            'task-context-budget-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiContextBudget.ps1' @{
            Action = $contextBudgetAction
            WorkspacePath = $WorkspacePath
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-context-benchmark', 'task-context-benchmark-create', 'task-context-benchmark-show', 'task-context-benchmark-verify') } {
        $contextBenchmarkAction = @{
            'task-context-benchmark' = 'compare'
            'task-context-benchmark-create' = 'create'
            'task-context-benchmark-show' = 'show'
            'task-context-benchmark-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiContextBenchmark.ps1' @{
            Action = $contextBenchmarkAction
            WorkspacePath = $WorkspacePath
            SourceWorkspacePath = $SourceWorkspacePath
            FailOnRegression = $FailOnRegression
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-context-experiment-plan', 'task-context-experiment-run', 'task-context-experiment-show', 'task-context-experiment-verify') } {
        $contextExperimentAction = @{
            'task-context-experiment-plan' = 'plan'
            'task-context-experiment-run' = 'run'
            'task-context-experiment-show' = 'show'
            'task-context-experiment-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiContextExperiment.ps1' @{
            Action = $contextExperimentAction
            WorkspacePath = $WorkspacePath
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-context-strategy-preview', 'task-context-strategy-approve', 'task-context-strategy-apply', 'task-context-strategy-show', 'task-context-strategy-verify', 'task-context-strategy-rollback') } {
        $contextStrategyAction = @{
            'task-context-strategy-preview' = 'preview'
            'task-context-strategy-approve' = 'approve'
            'task-context-strategy-apply' = 'apply'
            'task-context-strategy-show' = 'show'
            'task-context-strategy-verify' = 'verify'
            'task-context-strategy-rollback' = 'rollback'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiContextStrategy.ps1' @{
            Action = $contextStrategyAction
            WorkspacePath = $WorkspacePath
            Reason = $Reason
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('context-outcome-observe', 'context-outcome-profile', 'context-outcome-list', 'context-outcome-verify', 'context-outcome-metrics', 'context-outcome-health', 'context-outcome-prune') } {
        $contextOutcomeAction = @{
            'context-outcome-observe' = 'observe'
            'context-outcome-profile' = 'profile'
            'context-outcome-list' = 'list'
            'context-outcome-verify' = 'verify'
            'context-outcome-metrics' = 'metrics'
            'context-outcome-health' = 'health'
            'context-outcome-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiContextOutcome.ps1' @{
            Action = $contextOutcomeAction
            WorkspacePath = $(if ($Command -in @('context-outcome-observe', 'context-outcome-profile') -or ($Command -eq 'context-outcome-verify' -and $PSBoundParameters.ContainsKey('WorkspacePath'))) { $WorkspacePath } else { $null })
            AsOfUtc = $AsOfUtc
            Apply = $Apply
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-context-security-assess', 'task-context-security-create', 'task-context-security-show', 'task-context-security-verify') } {
        $contextSecurityAction = @{
            'task-context-security-assess' = 'assess'
            'task-context-security-create' = 'create'
            'task-context-security-show' = 'show'
            'task-context-security-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiContextSecurity.ps1' @{
            Action = $contextSecurityAction
            WorkspacePath = $WorkspacePath
            Path = $ChangedPath
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-risk-calibrate', 'task-risk-show', 'task-risk-verify') } {
        $riskAction = @{
            'task-risk-calibrate' = 'create'
            'task-risk-show' = 'show'
            'task-risk-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiRiskCalibration.ps1' @{
            Action = $riskAction
            WorkspacePath = $WorkspacePath
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-conformance-assess', 'task-conformance-replan', 'task-conformance-seal', 'task-conformance-show', 'task-conformance-verify') } {
        $conformanceAction = @{
            'task-conformance-assess' = 'assess'
            'task-conformance-replan' = 'replan'
            'task-conformance-seal' = 'create'
            'task-conformance-show' = 'show'
            'task-conformance-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiPlanConformance.ps1' @{
            Action = $conformanceAction
            WorkspacePath = $WorkspacePath
            Reason = $Reason
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-proof-assess', 'task-proof-seal', 'task-proof-show', 'task-proof-verify') } {
        $proofAction = @{
            'task-proof-assess' = 'assess'
            'task-proof-seal' = 'create'
            'task-proof-show' = 'show'
            'task-proof-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiProofOfChange.ps1' @{
            Action = $proofAction
            WorkspacePath = $WorkspacePath
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-requirements-assess', 'task-requirements-expand', 'task-requirements-seal', 'task-requirements-show', 'task-requirements-verify') } {
        $requirementAction = @{
            'task-requirements-assess' = 'assess'
            'task-requirements-expand' = 'expand'
            'task-requirements-seal' = 'create'
            'task-requirements-show' = 'show'
            'task-requirements-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiRequirementModel.ps1' @{
            Action = $requirementAction
            WorkspacePath = $WorkspacePath
            Reason = $Reason
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('impact-simulate', 'task-impact-assess', 'task-impact-seal', 'task-impact-show', 'task-impact-verify') } {
        $impactAction = @{
            'impact-simulate' = 'simulate'
            'task-impact-assess' = 'assess'
            'task-impact-seal' = 'create'
            'task-impact-show' = 'show'
            'task-impact-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiImpactSimulation.ps1' @{
            Action = $impactAction
            WorkspacePath = $WorkspacePath
            ProposedPath = $ProposedPath
            Objective = $Objective
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-repair-suggest', 'task-repair-start', 'task-repair-complete', 'task-repair-fail', 'task-repair-show', 'task-repair-verify') } {
        $repairAction = @{
            'task-repair-suggest' = 'suggest'
            'task-repair-start' = 'start'
            'task-repair-complete' = 'complete'
            'task-repair-fail' = 'fail'
            'task-repair-show' = 'show'
            'task-repair-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiRepairLoop.ps1' @{
            Action = $repairAction
            WorkspacePath = $WorkspacePath
            CheckId = $(if (@($CheckId).Count -gt 0) { [string]$CheckId[0] } else { $null })
            AttemptId = $RepairAttemptId
            Symptom = $Symptom
            Hypothesis = $RepairHypothesis
            RepairPath = $RepairPath
            Owner = $Owner
            Resolution = $Resolution
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('repair-learning-candidates', 'repair-learning-promote', 'repair-learning-list', 'repair-learning-verify', 'repair-learning-relevant') } {
        $repairLearningAction = @{
            'repair-learning-candidates' = 'candidates'
            'repair-learning-promote' = 'promote'
            'repair-learning-list' = 'list'
            'repair-learning-verify' = 'verify'
            'repair-learning-relevant' = 'relevant'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiRepairLearning.ps1' @{
            Action = $repairLearningAction
            WorkspacePath = $WorkspacePath
            CandidateId = $RepairCandidateId
            CheckId = $(if (@($CheckId).Count -gt 0) { [string]$CheckId[0] } else { $null })
            Category = $Query
            Path = $ChangedPath
            Owner = $Owner
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-failure-assess', 'task-failure-predict', 'task-failure-show', 'task-failure-verify') } {
        $failureAction = @{
            'task-failure-assess' = 'assess'
            'task-failure-predict' = 'create'
            'task-failure-show' = 'show'
            'task-failure-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiFailurePrediction.ps1' @{
            Action = $failureAction
            WorkspacePath = $WorkspacePath
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-cost-assess', 'task-cost-forecast', 'task-cost-show', 'task-cost-verify') } {
        $costAction = @{
            'task-cost-assess' = 'assess'
            'task-cost-forecast' = 'create'
            'task-cost-show' = 'show'
            'task-cost-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiVerificationCost.ps1' @{
            Action = $costAction
            WorkspacePath = $WorkspacePath
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-confidence-assess', 'task-confidence-create', 'task-confidence-show', 'task-confidence-verify') } {
        $confidenceAction = @{
            'task-confidence-assess' = 'assess'
            'task-confidence-create' = 'create'
            'task-confidence-show' = 'show'
            'task-confidence-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiConfidenceLedger.ps1' @{
            Action = $confidenceAction
            WorkspacePath = $WorkspacePath
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-critique-assess', 'task-critique-create', 'task-critique-show', 'task-critique-verify') } {
        $critiqueAction = @{
            'task-critique-assess' = 'assess'
            'task-critique-create' = 'create'
            'task-critique-show' = 'show'
            'task-critique-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiChangeCritique.ps1' @{
            Action = $critiqueAction
            WorkspacePath = $WorkspacePath
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-retrospective-assess', 'task-retrospective-create', 'task-retrospective-show', 'task-retrospective-verify') } {
        $retrospectiveAction = @{
            'task-retrospective-assess' = 'assess'
            'task-retrospective-create' = 'create'
            'task-retrospective-show' = 'show'
            'task-retrospective-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiRetrospective.ps1' @{
            Action = $retrospectiveAction
            WorkspacePath = $WorkspacePath
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('learning-observe', 'learning-candidates', 'learning-list', 'learning-show', 'learning-approve', 'learning-reject', 'learning-supersede', 'learning-plan', 'learning-apply', 'learning-rollback', 'learning-verify') } {
        $learningAction = @{
            'learning-observe' = 'observe'
            'learning-candidates' = 'candidates'
            'learning-list' = 'list'
            'learning-show' = 'show'
            'learning-approve' = 'approve'
            'learning-reject' = 'reject'
            'learning-supersede' = 'supersede'
            'learning-plan' = 'plan'
            'learning-apply' = 'apply'
            'learning-rollback' = 'rollback'
            'learning-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiLearningPromotion.ps1' @{
            Action = $learningAction
            WorkspacePath = $WorkspacePath
            Id = $Id
            Reason = $Reason
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('learning-shadow', 'learning-canary-start', 'learning-canary-record', 'learning-canary-evaluate', 'learning-canary-stop', 'learning-experiment-list', 'learning-experiment-show', 'learning-experiment-verify') } {
        $experimentAction = @{
            'learning-shadow' = 'shadow'
            'learning-canary-start' = 'canary-start'
            'learning-canary-record' = 'canary-record'
            'learning-canary-evaluate' = 'canary-evaluate'
            'learning-canary-stop' = 'canary-stop'
            'learning-experiment-list' = 'list'
            'learning-experiment-show' = 'show'
            'learning-experiment-verify' = 'verify'
        }[$Command]
        $experimentArguments = @{
            Action = $experimentAction
            Id = $Id
            WorkspacePath = $WorkspacePath
            Evidence = $CanaryEvidence
            Percentage = $CanaryPercentage
            Reason = $Reason
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
        if (-not [string]::IsNullOrWhiteSpace($CanaryOutcome)) {
            $experimentArguments.Outcome = $CanaryOutcome
        }
        Invoke-WikiTool 'Manage-LlmWikiLearningExperiment.ps1' $experimentArguments
    }
    { $_ -in @('learning-health-observe', 'learning-health-list', 'learning-health-show', 'learning-health-waive', 'learning-health-reopen', 'learning-health-verify') } {
        $healthAction = @{
            'learning-health-observe' = 'observe'
            'learning-health-list' = 'list'
            'learning-health-show' = 'show'
            'learning-health-waive' = 'waive'
            'learning-health-reopen' = 'reopen'
            'learning-health-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiLearningHealth.ps1' @{
            Action = $healthAction
            WorkspacePath = $WorkspacePath
            Id = $Id
            Reason = $Reason
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('verification-telemetry-list', 'verification-telemetry-metrics', 'verification-telemetry-verify') } {
        $telemetryAction = @{
            'verification-telemetry-list' = 'list'
            'verification-telemetry-metrics' = 'metrics'
            'verification-telemetry-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiVerificationTelemetry.ps1' @{
            Action = $telemetryAction
            CheckId = $(if (@($CheckId).Count -gt 0) { [string]$CheckId[0] } else { $null })
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-context-feedback', 'task-context-feedback-list', 'task-context-feedback-verify', 'task-context-feedback-metrics', 'task-context-feedback-prune') } {
        $feedbackAction = @{
            'task-context-feedback' = 'record'
            'task-context-feedback-list' = 'list'
            'task-context-feedback-verify' = 'verify'
            'task-context-feedback-metrics' = 'metrics'
            'task-context-feedback-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiContextFeedback.ps1' @{
            Action = $feedbackAction
            DispatchId = $DispatchId
            Owner = $Owner
            HelpfulPath = $HelpfulContextPath
            NoisyPath = $NoisyContextPath
            MissingPath = $MissingContextPath
            Reason = $Reason
            AsOfUtc = $AsOfUtc
            Apply = $Apply
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-quality-adjustment', 'task-quality-adjustment-list', 'task-quality-adjustment-verify', 'task-quality-adjustment-metrics', 'task-quality-adjustment-prune') } {
        $adjustmentAction = @{
            'task-quality-adjustment' = 'record'
            'task-quality-adjustment-list' = 'list'
            'task-quality-adjustment-verify' = 'verify'
            'task-quality-adjustment-metrics' = 'metrics'
            'task-quality-adjustment-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiQualityAdjustment.ps1' @{
            Action = $adjustmentAction
            AdjustmentId = $AdjustmentId
            DispatchId = $DispatchId
            Owner = $Owner
            AdjustmentType = $QualityAdjustmentType
            Reason = $Reason
            Evidence = $QualityEvidence
            AsOfUtc = $AsOfUtc
            Apply = $Apply
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('memory-promote', 'memory-candidates', 'memory-list', 'memory-show', 'memory-verify', 'memory-relevant', 'memory-supersede') } {
        $memoryAction = @{
            'memory-promote' = 'promote'
            'memory-candidates' = 'candidates'
            'memory-list' = 'list'
            'memory-show' = 'show'
            'memory-verify' = 'verify'
            'memory-relevant' = 'relevant'
            'memory-supersede' = 'supersede'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiMemory.ps1' @{
            Action = $memoryAction
            WorkspacePath = $WorkspacePath
            JournalId = $NoteId
            Id = $Id
            ScopePath = $MemoryScopePath
            Tag = $MemoryTag
            Evidence = $MemoryEvidence
            Reason = $Reason
            ReviewAfterDays = $ReviewAfterDays
            AllowDuplicate = $AllowMemoryDuplicate
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    'task-agent-coverage' {
        Invoke-WikiTool 'Get-LlmWikiAgentFleetCoverage.ps1' @{
            TasksPath = $TasksPath
            FailOnGap = $FailOnGap
            Format = $Format
        }
    }
    'task-dispatch-metrics' {
        Invoke-WikiTool 'Get-LlmWikiDispatchMetrics.ps1' @{
            WindowDays = $WindowDays
            AsOfUtc = $AsOfUtc
            FailOnAttention = $FailOnAttention
            Format = $Format
        }
    }
    { $_ -in @('task-dispatch-snapshot-list', 'task-dispatch-snapshot-save', 'task-dispatch-snapshot-verify', 'task-dispatch-snapshot-compare', 'task-dispatch-snapshot-prune') } {
        $snapshotAction = @{
            'task-dispatch-snapshot-list' = 'list'
            'task-dispatch-snapshot-save' = 'save'
            'task-dispatch-snapshot-verify' = 'verify'
            'task-dispatch-snapshot-compare' = 'compare'
            'task-dispatch-snapshot-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiDispatchMetricsSnapshot.ps1' @{
            Action = $snapshotAction
            SnapshotId = $SnapshotId
            WindowDays = $WindowDays
            AsOfUtc = $AsOfUtc
            Apply = $Apply
            FailOnInvalid = $FailOnInvalid
            FailOnRegression = $FailOnRegression
            Format = $Format
        }
    }
    { $_ -in @('task-dispatch-list', 'task-dispatch-start', 'task-dispatch-heartbeat', 'task-dispatch-complete', 'task-dispatch-fail', 'task-dispatch-verify', 'task-dispatch-reconcile', 'task-dispatch-prune') } {
        $dispatchAction = @{
            'task-dispatch-list' = 'list'
            'task-dispatch-start' = 'start'
            'task-dispatch-heartbeat' = 'heartbeat'
            'task-dispatch-complete' = 'complete'
            'task-dispatch-fail' = 'fail'
            'task-dispatch-verify' = 'verify'
            'task-dispatch-reconcile' = 'reconcile'
            'task-dispatch-prune' = 'prune'
        }[$Command]
        $dispatchArguments = @{
            Action = $dispatchAction
            WorkspacePath = $WorkspacePath
            Owner = $Owner
            DispatchId = $DispatchId
            AgentId = $AgentId
            RequiredCapability = $RequiredCapability
            Lane = $Lane
            RoutingScore = $RoutingScore
            LeaseMinutes = $LeaseMinutes
            Result = $Result
            RetentionDays = $RetentionDays
            Apply = $Apply
            AsOfUtc = $AsOfUtc
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
        Invoke-WikiTool 'Manage-LlmWikiTaskDispatch.ps1' $dispatchArguments
    }
    { $_ -in @('task-lease-list', 'task-lease-acquire', 'task-lease-heartbeat', 'task-lease-release', 'task-lease-prune') } {
        $leaseAction = @{
            'task-lease-list' = 'list'
            'task-lease-acquire' = 'acquire'
            'task-lease-heartbeat' = 'heartbeat'
            'task-lease-release' = 'release'
            'task-lease-prune' = 'prune'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiTaskLease.ps1' @{
            Action = $leaseAction
            WorkspacePath = $WorkspacePath
            Owner = $Owner
            LeaseId = $LeaseId
            LeaseMinutes = $LeaseMinutes
            AsOfUtc = $AsOfUtc
            Format = $Format
        }
    }
    { $_ -in @('task-status', 'task-refresh') } {
        Invoke-WikiTool 'Manage-LlmWikiTaskWorkspace.ps1' @{
            Action = $(if ($Command -eq 'task-refresh') { 'refresh' } else { 'status' })
            WorkspacePath = $WorkspacePath
            FailOnBlocked = $FailOnBlocked
            DryRun = $DryRun
            Format = $Format
        }
    }
    'task-run' {
        Invoke-WikiTool 'Invoke-LlmWikiTaskChecks.ps1' @{
            WorkspacePath = $WorkspacePath
            CheckId = $CheckId
            IncludePassed = $IncludePassed
            ContinueOnFailure = $ContinueOnFailure
            DryRun = $DryRun
            FailOnFailure = $FailOnFailure
            Format = $Format
        }
    }
    'task-lineage' {
        Invoke-WikiTool 'Test-LlmWikiEvidenceLineage.ps1' @{
            WorkspacePath = $WorkspacePath
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    { $_ -in @('task-cache-find', 'task-cache-reuse') } {
        Invoke-WikiTool 'Manage-LlmWikiEvidenceCache.ps1' @{
            Action = $(if ($Command -eq 'task-cache-reuse') { 'reuse' } else { 'find' })
            WorkspacePath = $WorkspacePath
            CheckId = $CheckId
            SourceWorkspacePath = $SourceWorkspacePath
            DryRun = $DryRun
            Format = $Format
        }
    }
    { $_ -in @('task-similarity-profile', 'task-similarity-find', 'task-similarity-clusters', 'task-similarity-reuse', 'task-similarity-show', 'task-similarity-verify') } {
        $similarityAction = @{
            'task-similarity-profile' = 'profile'
            'task-similarity-find' = 'find'
            'task-similarity-clusters' = 'clusters'
            'task-similarity-reuse' = 'reuse'
            'task-similarity-show' = 'show'
            'task-similarity-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiTaskSimilarity.ps1' @{
            Action = $similarityAction
            WorkspacePath = $WorkspacePath
            SourceWorkspacePath = $SourceWorkspacePath
            DryRun = $DryRun
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    'task-handoff' {
        Invoke-WikiTool 'Get-LlmWikiTaskHandoff.ps1' @{
            WorkspacePath = $WorkspacePath
            Limit = [Math]::Min(100, $Limit)
            Format = $(if ($Format -eq 'Json') { 'Json' } else { 'Markdown' })
            OutputPath = $OutputPath
        }
    }
    { $_ -in @('task-export', 'task-export-verify') } {
        $taskExportArguments = @{
            Action = $(if ($Command -eq 'task-export-verify') { 'verify' } else { 'export' })
            WorkspacePath = $WorkspacePath
            Path = $ExportPath
            FailOnSensitive = $FailOnSensitive
            Overwrite = $Overwrite
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
        if ($PSBoundParameters.ContainsKey('Limit')) { $taskExportArguments.Limit = $Limit }
        Invoke-WikiTool 'Export-LlmWikiTaskWorkspace.ps1' $taskExportArguments
    }
    'task-import' {
        Invoke-WikiTool 'Import-LlmWikiTaskWorkspace.ps1' @{
            ImportPath = $ImportPath
            WorkspacePath = $WorkspacePath
            BaseRef = $BaseRef
            AllowPartialScope = $AllowPartialScope
            SkipJournal = $SkipJournal
            DryRun = $DryRun
            Format = $Format
        }
    }
    'task-note' {
        Invoke-WikiTool 'Manage-LlmWikiTaskJournal.ps1' @{
            Action = 'add'
            WorkspacePath = $WorkspacePath
            JournalType = $JournalType
            Text = $Text
            Rationale = $Reason
        }
    }
    'task-resolve-note' {
        Invoke-WikiTool 'Manage-LlmWikiTaskJournal.ps1' @{
            Action = 'resolve'
            WorkspacePath = $WorkspacePath
            NoteId = $NoteId
            Resolution = $Resolution
        }
    }
    'task-journal' {
        Invoke-WikiTool 'Manage-LlmWikiTaskJournal.ps1' @{
            Action = $(if ($Check) { 'validate' } else { 'show' })
            WorkspacePath = $WorkspacePath
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    'task-doctor' {
        Invoke-WikiTool 'Test-LlmWikiTaskWorkspace.ps1' @{
            WorkspacePath = $WorkspacePath
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    'task-migrate' {
        Invoke-WikiTool 'Update-LlmWikiTaskWorkspace.ps1' @{
            WorkspacePath = $WorkspacePath
            DryRun = $DryRun
            Format = $Format
        }
    }
    'task-policy-sync' {
        Invoke-WikiTool 'Sync-LlmWikiTaskPolicy.ps1' @{
            WorkspacePath = $WorkspacePath
            DryRun = $DryRun
            AcceptImpact = $AcceptPolicyImpact
            Format = $Format
        }
    }
    'task-policy-impact' {
        Invoke-WikiTool 'Compare-LlmWikiTaskPolicy.ps1' @{
            WorkspacePath = $WorkspacePath
            Format = $Format
        }
    }
    { $_ -in @('task-finish', 'task-verify') } {
        Invoke-WikiTool 'Complete-LlmWikiTaskWorkspace.ps1' @{
            Action = $(if ($Command -eq 'task-verify') { 'verify' } else { 'finish' })
            WorkspacePath = $WorkspacePath
            DryRun = $DryRun
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    'topology' {
        Invoke-WikiTool 'Find-LlmWikiRuntimeTopology.ps1' @{ Query = $Query; Limit = $Limit; Format = $Format }
    }
    'privacy' {
        Invoke-WikiTool 'Find-LlmWikiSensitiveData.ps1' @{
            Query = $Query
            Category = $PrivacyCategory
            Limit = $Limit
            Format = $Format
        }
    }
    'ui' {
        Invoke-WikiTool 'Find-LlmWikiFrontendContract.ps1' @{
            Query = $Query
            View = $FrontendView
            Limit = $Limit
            Format = $Format
        }
    }
    'domain' {
        Invoke-WikiTool 'Find-LlmWikiDomainData.ps1' @{
            Query = $Query
            View = $DomainView
            Limit = $Limit
            Format = $Format
        }
    }
    'contracts' {
        Invoke-WikiTool 'Find-LlmWikiBackendContract.ps1' @{
            Query = $Query
            View = $BackendContractView
            Limit = $Limit
            Format = $Format
        }
    }
    'health' {
        Invoke-WikiTool 'Find-LlmWikiArchitectureHealth.ps1' @{
            Query = $Query
            View = $HealthView
            Limit = $Limit
            Format = $Format
        }
    }
    'hotspots' {
        Invoke-WikiTool 'Find-LlmWikiQualityRisk.ps1' @{ View = 'hotspots'; Query = $Query; Limit = $Limit; Format = $Format }
    }
    'test-gaps' {
        Invoke-WikiTool 'Find-LlmWikiQualityRisk.ps1' @{ View = 'test-gaps'; Query = $Query; Limit = $Limit; Format = $Format }
    }
    'debt' {
        Invoke-WikiTool 'Find-LlmWikiQualityRisk.ps1' @{ View = 'debt'; Query = $Query; Limit = $Limit; Format = $Format }
    }
    'diff' {
        $diffArguments = @{
            BaseRef = $BaseRef
            Format = $Format
            Limit = [Math]::Min($Limit, 20)
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) {
            $diffArguments.HeadRef = $HeadRef
        }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) {
            $diffArguments.ChangedPath = $ChangedPath
        }
        Invoke-WikiTool 'Get-LlmWikiDiffContext.ps1' $diffArguments
    }
    'impact' {
        $impactArguments = @{
            BaseRef = $BaseRef
            FailOnUnreviewed = $FailOnUnreviewed
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) {
            $impactArguments.HeadRef = $HeadRef
        }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) {
            $impactArguments.ChangedPath = $ChangedPath
        }
        Invoke-WikiTool 'Get-LlmWikiImpact.ps1' $impactArguments
    }
    'ownership' {
        $ownershipArguments = @{ BaseRef = $BaseRef; Format = $Format }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $ownershipArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $ownershipArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Get-LlmWikiOwnershipImpact.ps1' $ownershipArguments
    }
    'api-compat' {
        Invoke-WikiTool 'Test-LlmWikiApiCompatibility.ps1' @{
            BaseRef = $BaseRef
            Format = $Format
            FailOnBreaking = $FailOnBreaking
        }
    }
    'policy' {
        $policyArguments = @{
            BaseRef = $BaseRef
            EvidencePath = $EvidencePath
            RequireEvidence = $RequireEvidence
            FailOnViolation = $true
            Format = $Format
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $policyArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $policyArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Test-LlmWikiChangePolicy.ps1' $policyArguments
    }
    'evidence-init' {
        $evidenceArguments = @{
            Action = 'init'
            Path = $EvidencePath
            BaseRef = $BaseRef
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $evidenceArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $evidenceArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Manage-LlmWikiEvidence.ps1' $evidenceArguments
    }
    'evidence-check' {
        $evidenceArguments = @{
            Action = 'check'
            Path = $EvidencePath
            Id = $Id
            Status = $Status
            Command = $EvidenceCommand
            Reason = $Reason
        }
        if ($PSBoundParameters.ContainsKey('DurationSeconds')) {
            $evidenceArguments.DurationSeconds = $DurationSeconds
        }
        Invoke-WikiTool 'Manage-LlmWikiEvidence.ps1' $evidenceArguments
    }
    'evidence-run' {
        Invoke-WikiTool 'Manage-LlmWikiEvidence.ps1' @{
            Action = 'run'
            Path = $EvidencePath
            Id = $Id
            Command = $EvidenceCommand
        }
    }
    'evidence-review' {
        Invoke-WikiTool 'Manage-LlmWikiEvidence.ps1' @{
            Action = 'review'
            Path = $EvidencePath
            Id = $Id
            Status = $Status
            Reason = $Reason
        }
    }
    'evidence-validate' {
        Invoke-WikiTool 'Manage-LlmWikiEvidence.ps1' @{
            Action = 'validate'
            Path = $EvidencePath
        }
    }
    'handoff' {
        Invoke-WikiTool 'Manage-LlmWikiEvidence.ps1' @{
            Action = 'summary'
            Path = $EvidencePath
            OutputPath = $OutputPath
        }
    }
    'evals' {
        Invoke-WikiTool 'Invoke-LlmWikiEvals.ps1' @{ Detailed = $Detailed }
    }
    { $_ -in @('eval-observe', 'eval-candidates', 'eval-list', 'eval-show', 'eval-approve', 'eval-reject', 'eval-apply', 'eval-rollback', 'eval-verify') } {
        $evalAction = @{
            'eval-observe' = 'observe'
            'eval-candidates' = 'candidates'
            'eval-list' = 'list'
            'eval-show' = 'show'
            'eval-approve' = 'approve'
            'eval-reject' = 'reject'
            'eval-apply' = 'apply'
            'eval-rollback' = 'rollback'
            'eval-verify' = 'verify'
        }[$Command]
        Invoke-WikiTool 'Manage-LlmWikiEvalPromotion.ps1' @{
            Action = $evalAction
            WorkspacePath = $WorkspacePath
            Id = $Id
            Reason = $Reason
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    'failures' {
        Invoke-WikiTool 'Manage-LlmWikiFailures.ps1' @{ Action = 'search'; Query = $Query }
    }
    'failure-add' {
        Invoke-WikiTool 'Manage-LlmWikiFailures.ps1' @{
            Action = 'add'
            Id = $Id
            Symptom = $Symptom
            Cause = $Cause
            Fix = $Fix
            PathPattern = $PathPattern
            Verification = $Verification
        }
    }
    'task-init' {
        Invoke-WikiTool 'Manage-LlmWikiTaskContract.ps1' @{
            Action = 'init'
            Path = $TaskPath
            Objective = $Objective
            BaseRef = $BaseRef
            AllowedPath = $AllowedPath
            ExcludedPath = $ExcludedPath
        }
    }
    'task-show' {
        Invoke-WikiTool 'Manage-LlmWikiTaskContract.ps1' @{ Action = 'show'; Path = $TaskPath }
    }
    'task-validate' {
        Invoke-WikiTool 'Manage-LlmWikiTaskContract.ps1' @{
            Action = 'validate'
            Path = $TaskPath
            FailOnOutOfScope = $FailOnOutOfScope
        }
    }
    'manifest-init' {
        $manifestArguments = @{
            Action = 'init'
            Path = $ManifestPath
            Objective = $Objective
            BaseRef = $BaseRef
            AllowedPath = $AllowedPath
            ExcludedPath = $ExcludedPath
            EvidencePath = $EvidencePath
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $manifestArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $manifestArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Manage-LlmWikiChangeManifest.ps1' $manifestArguments
    }
    'manifest-show' {
        Invoke-WikiTool 'Manage-LlmWikiChangeManifest.ps1' @{
            Action = 'show'
            Path = $ManifestPath
            Format = $Format
        }
    }
    'manifest-validate' {
        $manifestArguments = @{
            Action = 'validate'
            Path = $ManifestPath
            EvidencePath = $EvidencePath
            RequireEvidence = $RequireEvidence
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $manifestArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $manifestArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Manage-LlmWikiChangeManifest.ps1' $manifestArguments
    }
    'acceptance-init' {
        $acceptanceArguments = @{
            Action = 'init'
            Path = $AcceptancePath
            Objective = $Objective
            Criterion = $Criterion
            BaseRef = $BaseRef
            EvidencePath = $EvidencePath
        }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $acceptanceArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $acceptanceArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Manage-LlmWikiAcceptanceMatrix.ps1' $acceptanceArguments
    }
    'acceptance-show' {
        Invoke-WikiTool 'Manage-LlmWikiAcceptanceMatrix.ps1' @{ Action = 'show'; Path = $AcceptancePath; Format = $Format }
    }
    'acceptance-map' {
        Invoke-WikiTool 'Manage-LlmWikiAcceptanceMatrix.ps1' @{
            Action = 'map'
            Path = $AcceptancePath
            CriterionId = $CriterionId
            ChangedPath = $ChangedPath
            ScenarioId = $ScenarioId
            CheckId = $CheckId
            ReviewId = $ReviewId
            TestPath = $TestPath
        }
    }
    'acceptance-resolve' {
        Invoke-WikiTool 'Manage-LlmWikiAcceptanceMatrix.ps1' @{
            Action = 'resolve'
            Path = $AcceptancePath
            CriterionId = $CriterionId
            AcceptanceStatus = $AcceptanceStatus
            Reason = $Reason
            EvidenceNote = $EvidenceNote
        }
    }
    'acceptance-validate' {
        Invoke-WikiTool 'Manage-LlmWikiAcceptanceMatrix.ps1' @{
            Action = 'validate'
            Path = $AcceptancePath
            EvidencePath = $EvidencePath
            RequireEvidence = $RequireEvidence
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
    }
    'catalog' {
        Invoke-WikiTool 'Build-LlmWikiCatalog.ps1' @{ Check = $Check }
    }
    'symbols' {
        Invoke-WikiTool 'Build-LlmWikiSymbolIndex.ps1' @{ Check = $Check }
    }
    'frontend' {
        Invoke-WikiTool 'Build-LlmWikiFrontendIndex.ps1' @{ Check = $Check }
    }
    'frontend-contract' {
        Invoke-WikiTool 'Build-LlmWikiFrontendContractIndex.ps1' @{ Check = $Check }
    }
    'backend-contract' {
        Invoke-WikiTool 'Build-LlmWikiBackendContractIndex.ps1' @{ Check = $Check }
    }
    'architecture-health' {
        Invoke-WikiTool 'Build-LlmWikiArchitectureHealthIndex.ps1' @{ Check = $Check }
    }
    'domain-data' {
        Invoke-WikiTool 'Build-LlmWikiDomainDataIndex.ps1' @{ Check = $Check }
    }
    'configuration' {
        Invoke-WikiTool 'Build-LlmWikiConfigurationIndex.ps1' @{ Check = $Check }
    }
    'quality' {
        Invoke-WikiTool 'Build-LlmWikiQualityIndex.ps1' @{ Check = $Check }
    }
    'runtime' {
        Invoke-WikiTool 'Build-LlmWikiRuntimeTopology.ps1' @{ Check = $Check }
    }
    'sensitive-data' {
        Invoke-WikiTool 'Build-LlmWikiSensitiveDataIndex.ps1' @{ Check = $Check }
    }
    'modules' {
        Invoke-WikiTool 'Build-LlmWikiModulePages.ps1' @{ Check = $Check }
    }
    default {
        Write-Host 'FoodDiary LLM Wiki'
        Write-Host ''
        Write-Host 'Usage:'
        Write-Host '  ./.llm-wiki/wiki.ps1 update'
        Write-Host '  ./.llm-wiki/wiki.ps1 lint [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 smoke -SmokeGroup portable|linux|tools'
        Write-Host '  ./.llm-wiki/wiki.ps1 verify'
        Write-Host '  ./.llm-wiki/wiki.ps1 verify-full'
        Write-Host '  ./.llm-wiki/wiki.ps1 context -Module Billing -ChangeType Api'
        Write-Host '  ./.llm-wiki/wiki.ps1 trace -Query StartPremiumTrial'
        Write-Host '  ./.llm-wiki/wiki.ps1 packet -Objective <text> [-OutputPath <path>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 brief'
        Write-Host '  ./.llm-wiki/wiki.ps1 plan -Objective <text> [-ChangedPath <path>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 test-plan'
        Write-Host '  ./.llm-wiki/wiki.ps1 decision'
        Write-Host '  ./.llm-wiki/wiki.ps1 dependencies -BaseRef origin/master'
        Write-Host '  ./.llm-wiki/wiki.ps1 rollout'
        Write-Host '  ./.llm-wiki/wiki.ps1 readiness -RequireManifest -RequireAcceptance -RequireEvidence -FailOnNotReady'
        Write-Host '  ./.llm-wiki/wiki.ps1 report -OutputPath .artifacts/llm-wiki/review-report.md'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-start -Objective <text> -Criterion <text> -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 workspace-policy [-FailOnInvalid] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-list [-Detailed] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-graph [-IncludeSealed] [-FailOnBlocked] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-schedule [-MaxConcurrency <n>] [-FailOnBlocked] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-orchestrate [-MaxConcurrency <n>] [-Apply] [-FailOnAttention]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-orchestration-cycle-list'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-orchestration-cycle-verify -CycleId <id> [-FailOnAttention]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-orchestration-cycle-prune [-Apply]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-orchestration-audit [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-watchdog [-SilentMinutes <n>] [-Apply] [-FailOnAttention]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-watchdog-verify -WatchdogId <id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-circuit-list [-FailOnAttention]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-circuit-open -WorkspacePath <path> -Reason <text> [-CooldownMinutes <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-circuit-reset -WorkspacePath <path> [-Reason <text>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-circuit-verify -CircuitId <id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-decompose-plan -WorkspacePath <path> [-MaxShards <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-decompose-verify -DecompositionId <id> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-decompose-apply -DecompositionId <id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-decompose-list'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-verification-plan -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-IncludePassed]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-verification-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-verification-run -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-DryRun] [-ContinueOnFailure]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-model-route-show -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-model-route-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 model-route-outcome-metrics'
        Write-Host '  ./.llm-wiki/wiki.ps1 model-route-outcome-health'
        Write-Host '  ./.llm-wiki/wiki.ps1 model-route-outcome-verify [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 instruction-outcome-metrics'
        Write-Host '  ./.llm-wiki/wiki.ps1 instruction-outcome-candidates'
        Write-Host '  ./.llm-wiki/wiki.ps1 instruction-outcome-verify [-WorkspacePath <completed-task>] [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 instruction-experiment-start -Id <candidate-id> -Reason <hypothesis>'
        Write-Host '  ./.llm-wiki/wiki.ps1 instruction-experiment-forecast -Id <experiment-id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 instruction-experiment-evaluate -Id <experiment-id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 instruction-experiment-stop -Id <experiment-id> -Reason <decision>'
        Write-Host '  ./.llm-wiki/wiki.ps1 instruction-experiment-verify [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-risk-calibrate -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-risk-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-cost-forecast -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-cost-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-confidence-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-confidence-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-critique-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-critique-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-retrospective-show -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-retrospective-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-observe -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-candidates [-WorkspacePath .artifacts/llm-wiki/tasks/<name>] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-approve -Id <learning-id> -Reason <review-rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-reject -Id <learning-id> -Reason <review-rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-supersede -Id <learning-id> -Reason <reason>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-plan -Id <approved-learning-id> -Format Json'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-apply -Id <approved-learning-id> -Reason <materialization-rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-rollback -Id <applied-learning-id> -Reason <rollback-rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-shadow -Id <approved-learning-id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-canary-start -Id <approved-learning-id> -Reason <reason> [-CanaryPercentage 25]'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-canary-record -Id <id> -WorkspacePath <task> -CanaryOutcome improved -CanaryEvidence <evidence>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-canary-evaluate -Id <id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-canary-stop -Id <id> -Reason <reason>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-experiment-verify [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-health-list | learning-health-show -Id <id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-health-waive|learning-health-reopen -Id <id> -Reason <rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-health-verify [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 learning-verify [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 verification-telemetry-metrics [-CheckId <id>] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 verification-telemetry-verify [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-conformance-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-conformance-replan -WorkspacePath .artifacts/llm-wiki/tasks/<name> -Reason <rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-conformance-seal -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-proof-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-proof-seal -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-requirements-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-requirements-expand -WorkspacePath .artifacts/llm-wiki/tasks/<name> -Reason <rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 impact-simulate -ProposedPath <path> [-Objective <text>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-impact-assess -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-repair-suggest -WorkspacePath <path> -CheckId <failed-check>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-repair-start -WorkspacePath <path> -CheckId <failed-check> -RepairHypothesis <text> -RepairPath <path> -Owner <agent>'
        Write-Host '  ./.llm-wiki/wiki.ps1 repair-learning-candidates -WorkspacePath <path>'
        Write-Host '  ./.llm-wiki/wiki.ps1 repair-learning-promote -WorkspacePath <path> -RepairCandidateId <id> -Owner <agent>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-create -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-Limit <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-compare -SourceWorkspacePath <source> -WorkspacePath <target>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-budget-create -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-budget-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> -FailOnInvalid'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-benchmark -SourceWorkspacePath <baseline> -WorkspacePath <candidate> [-FailOnRegression]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-benchmark-create -SourceWorkspacePath <baseline> -WorkspacePath <candidate>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-benchmark-verify -WorkspacePath <candidate> -FailOnInvalid [-FailOnRegression]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-experiment-plan -WorkspacePath <task>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-experiment-run -WorkspacePath <task>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-experiment-verify -WorkspacePath <task> -FailOnInvalid'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-strategy-preview -WorkspacePath <task>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-strategy-approve -WorkspacePath <task> -Reason <review rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-strategy-apply -WorkspacePath <task>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-strategy-verify -WorkspacePath <task> -FailOnInvalid'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-strategy-rollback -WorkspacePath <task> -Reason <rollback rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 context-outcome-metrics'
        Write-Host '  ./.llm-wiki/wiki.ps1 context-outcome-health'
        Write-Host '  ./.llm-wiki/wiki.ps1 context-outcome-profile -WorkspacePath <task>'
        Write-Host '  ./.llm-wiki/wiki.ps1 context-outcome-verify [-WorkspacePath <completed-task>] -FailOnInvalid'
        Write-Host '  ./.llm-wiki/wiki.ps1 context-outcome-observe -WorkspacePath <completed-task>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-security-assess -WorkspacePath <path> [-ChangedPath <path>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-security-verify -WorkspacePath <path> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-feedback -DispatchId <id> -Owner <agent> [-HelpfulContextPath <path>] [-NoisyContextPath <path>] [-MissingContextPath <path>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-feedback-metrics [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-context-feedback-verify -DispatchId <id> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-quality-adjustment -DispatchId <id> -Owner <agent> -QualityAdjustmentType <rework|rollback|regression|recovery> -Reason <text> -QualityEvidence <evidence>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-quality-adjustment-metrics [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-quality-adjustment-verify -AdjustmentId <id> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 memory-promote -WorkspacePath <path> -NoteId <J-id> -Id <id> -MemoryEvidence <evidence> [-MemoryScopePath <regex>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 memory-candidates -WorkspacePath <path> [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 memory-relevant -WorkspacePath <path> [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 memory-verify [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 memory-supersede -Id <id> -Reason <reason>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-schedule-plan-create [-MaxConcurrency <n>] [-TtlMinutes <n>] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-schedule-plan-verify -PlanId <id> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-schedule-plan-claim -PlanId <id> [-Apply] [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-agent-register -Owner <agent> -Capability backend,tests [-Capacity <n>] [-RegistrationMinutes <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-agent-heartbeat -AgentId <id> [-Owner <agent>] [-RegistrationMinutes <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-agent-quarantine -AgentId <id> -Owner <agent> -Reason <text> [-QuarantineMinutes <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-agent-unquarantine -AgentId <id> -Owner <agent>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-agent-unregister -AgentId <id> [-Owner <agent>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-agent-list [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-agent-coverage [-FailOnGap] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-start -WorkspacePath <path> -Owner <agent> [-Lane <n>] [-LeaseMinutes <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-heartbeat -DispatchId <id> [-Owner <agent>] [-LeaseMinutes <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-complete -DispatchId <id> -Result <summary> [-Owner <agent>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-fail -DispatchId <id> -Result <summary> [-Owner <agent>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-list [-FailOnInvalid] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-reconcile [-Apply] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-prune [-RetentionDays <n>] [-Apply] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-metrics [-WindowDays <n>] [-FailOnAttention] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-snapshot-save [-WindowDays <n>] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-snapshot-compare [-SnapshotId <id>] [-FailOnRegression] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-snapshot-verify -SnapshotId <id> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-dispatch-snapshot-prune [-Apply]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-lease-acquire -WorkspacePath <path> -Owner <agent> [-LeaseMinutes <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-lease-heartbeat -LeaseId <id> [-Owner <agent>] [-LeaseMinutes <n>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-lease-release -LeaseId <id> [-Owner <agent>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-audit [-StaleAfterDays 7] [-EvidenceMaxAgeDays 3] [-FailOnAttention]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-status -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnBlocked]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-refresh -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-DryRun]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-lineage -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-cache-find -WorkspacePath <target> -CheckId <id> [-SourceWorkspacePath <source>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-cache-reuse -WorkspacePath <target> -CheckId <id> [-SourceWorkspacePath <source>] [-DryRun]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-similarity-profile|task-similarity-find -WorkspacePath <target>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-similarity-clusters'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-similarity-reuse -WorkspacePath <target> [-SourceWorkspacePath <source>] [-DryRun]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-similarity-show|task-similarity-verify -WorkspacePath <target>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-run -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-CheckId <id>] [-DryRun]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-handoff -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-OutputPath <path>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-export -WorkspacePath <path> [-ExportPath .artifacts/llm-wiki/exports/<name>.json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-export-verify -ExportPath .artifacts/llm-wiki/exports/<name>.json [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-import -ImportPath .artifacts/llm-wiki/exports/<name>.json -WorkspacePath .artifacts/llm-wiki/tasks/<new-name> [-DryRun]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-note -WorkspacePath <path> -JournalType decision -Text <text> [-Reason <text>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-resolve-note -WorkspacePath <path> -NoteId J-0001 -Resolution <text>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-journal -WorkspacePath <path> [-Check] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-doctor -WorkspacePath <path> [-FailOnInvalid] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-migrate -WorkspacePath <path> [-DryRun] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-policy-sync -WorkspacePath <path> [-DryRun] [-AcceptPolicyImpact] [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-policy-impact -WorkspacePath <path> [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-finish -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-DryRun]'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-verify -WorkspacePath .artifacts/llm-wiki/tasks/<name> [-FailOnInvalid]'
        Write-Host '  ./.llm-wiki/wiki.ps1 topology [-Query <text>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 privacy -PrivacyCategory credential'
        Write-Host '  ./.llm-wiki/wiki.ps1 ui -FrontendView components -Query autocomplete'
        Write-Host '  ./.llm-wiki/wiki.ps1 domain -DomainView invariants -Query weight'
        Write-Host '  ./.llm-wiki/wiki.ps1 contracts -BackendContractView consumers -Query StartFastingCommand'
        Write-Host '  ./.llm-wiki/wiki.ps1 health -HealthView dead-candidates'
        Write-Host '  ./.llm-wiki/wiki.ps1 hotspots [-Query <text>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 test-gaps [-Query <text>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 debt'
        Write-Host '  ./.llm-wiki/wiki.ps1 diff'
        Write-Host '  ./.llm-wiki/wiki.ps1 impact -FailOnUnreviewed'
        Write-Host '  ./.llm-wiki/wiki.ps1 ownership'
        Write-Host '  ./.llm-wiki/wiki.ps1 api-compat -BaseRef HEAD -FailOnBreaking'
        Write-Host '  ./.llm-wiki/wiki.ps1 policy [-RequireEvidence]'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-init'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-run -Id <id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-check -Id <id> -Status passed'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-review -Id <id> -Status completed -Reason <text>'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-validate'
        Write-Host '  ./.llm-wiki/wiki.ps1 handoff [-OutputPath <path>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 evals [-Detailed]'
        Write-Host '  ./.llm-wiki/wiki.ps1 eval-observe -WorkspacePath <task>'
        Write-Host '  ./.llm-wiki/wiki.ps1 eval-candidates | eval-list | eval-show -Id <id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 eval-approve|eval-reject -Id <id> -Reason <review>'
        Write-Host '  ./.llm-wiki/wiki.ps1 eval-apply|eval-rollback -Id <id> -Reason <rationale>'
        Write-Host '  ./.llm-wiki/wiki.ps1 eval-verify -FailOnInvalid'
        Write-Host '  ./.llm-wiki/wiki.ps1 failures [-Query <text>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 failure-add -Id <id> -Symptom <text> -Cause <text> -Fix <text>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-init -Objective <text> -AllowedPath <regex>'
        Write-Host '  ./.llm-wiki/wiki.ps1 task-validate -FailOnOutOfScope'
        Write-Host '  ./.llm-wiki/wiki.ps1 manifest-init -Objective <text> [-AllowedPath <regex>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 manifest-validate [-RequireEvidence] -FailOnInvalid'
        Write-Host '  ./.llm-wiki/wiki.ps1 acceptance-init -Objective <text> -Criterion <text>'
        Write-Host '  ./.llm-wiki/wiki.ps1 acceptance-map -CriterionId AC-001 -ScenarioId <id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 acceptance-resolve -CriterionId AC-001 -AcceptanceStatus satisfied -EvidenceNote <text>'
        Write-Host '  ./.llm-wiki/wiki.ps1 acceptance-validate [-RequireEvidence] -FailOnInvalid'
        Write-Host '  ./.llm-wiki/wiki.ps1 catalog [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 symbols [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 frontend [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 frontend-contract [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 backend-contract [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 architecture-health [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 domain-data [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 configuration [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 quality [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 runtime [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 sensitive-data [-Check]'
        Write-Host '  ./.llm-wiki/wiki.ps1 modules [-Check]'
    }
}
