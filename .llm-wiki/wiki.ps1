[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet(
        'help', 'start', 'update', 'repair-verify', 'completion', 'lint', 'smoke', 'verify-fast', 'verify-strict-affected', 'verify', 'verify-full', 'develop', 'continue-ui', 'ui-finalize', 'status', 'next', 'research', 'integration-scan', 'precedents', 'solutions', 'design', 'phase-status', 'phase-next', 'phase-complete', 'qa', 'visual-qa', 'workflow-metrics', 'pause', 'resume', 'journeys', 'ui-trace', 'delivery-status', 'delivery-replan', 'delivery-validate', 'delivery-critique', 'context', 'trace', 'packet', 'brief', 'implementation-plan', 'plan', 'test-plan', 'decision',
        'dependencies', 'rollout', 'readiness', 'report', 'topology', 'privacy', 'contract-consumers', 'extraction', 'ui', 'domain', 'contracts', 'health', 'hotspots', 'test-gaps', 'debt',
        'diff', 'impact', 'review', 'review-affected', 'ownership', 'api-compat', 'policy', 'verification-record', 'verification-list',
        'evidence-init', 'evidence-run', 'evidence-check', 'evidence-review', 'evidence-artifact', 'evidence-validate',
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
    [ValidateSet('Auto', 'Backend', 'Frontend')]
    [string]$TraceView = 'Auto',
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
    [Alias('PlannedPath')]
    [string[]]$ProposedPath,
    [switch]$AffectedOnly,
    [switch]$ContractIndexesOnly,
    [ValidateSet('All', 'Backend', 'Frontend')]
    [string]$Area = 'All',
    [string]$Stage,
    [switch]$VisualUiCompletion,
    [switch]$FullTrace,
    [switch]$Fast,
    [switch]$Compact,
    [switch]$FailOnUnreviewed,
    [switch]$Check,
    [string]$EvidencePath = '.artifacts/llm-wiki/evidence.json',
    [string]$TaskPath = '.artifacts/llm-wiki/task-contract.json',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$TaskSessionId,
    [string]$SourceWorkspacePath,
    [string]$TasksPath = '.artifacts/llm-wiki/tasks',
    [ValidateSet('decision', 'assumption', 'blocker', 'learning', 'note')]
    [string]$JournalType = 'note',
    [string]$Text,
    [string]$NoteId,
    [string]$Resolution,
    [string[]]$Decision,
    [string[]]$Option,
    [string[]]$BoundaryEvidence,
    [string]$PhaseId,
    [string]$ManifestPath = '.artifacts/llm-wiki/change-manifest.json',
    [string]$AcceptancePath = '.artifacts/llm-wiki/acceptance-matrix.json',
    [string]$Id,
    [ValidateSet('pending', 'passed', 'failed', 'completed', 'not-applicable')]
    [string]$Status,
    [string]$EvidenceCommand,
    [string[]]$CoverageScope,
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
    [Alias('Intent')]
    [string]$Objective,
    [ValidateSet('Auto', 'Assessment', 'Implementation')]
    [string]$ResearchPurpose = 'Auto',
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
    [string]$Url,
    [string]$FixturePath,
    [string]$ResultSelector,
    [string]$TriggerSelector,
    [string]$FileSelector = 'input[type=file]',
    [string]$StorageStatePath,
    [Nullable[int]]$ViewportWidth,
    [Nullable[int]]$ViewportHeight,
    [Nullable[int]]$TimeoutMs,
    [switch]$Run,
    [ValidateSet('screenshot', 'browser-log', 'accessibility-report', 'video')]
    [string]$EvidenceKind = 'screenshot',
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
    [switch]$ResumePassedStages,
    [switch]$FailOnFailure,
    [switch]$FailOnRegression,
    [switch]$FailOnGap
)

$ErrorActionPreference = 'Stop'
$gitConfigCount = 0
if (-not [string]::IsNullOrWhiteSpace($env:GIT_CONFIG_COUNT)) {
    $parsedGitConfigCount = 0
    if ([int]::TryParse($env:GIT_CONFIG_COUNT, [ref]$parsedGitConfigCount)) {
        $gitConfigCount = $parsedGitConfigCount
    }
}
Set-Item -LiteralPath "Env:GIT_CONFIG_KEY_$gitConfigCount" -Value 'core.safecrlf'
Set-Item -LiteralPath "Env:GIT_CONFIG_VALUE_$gitConfigCount" -Value 'false'
$env:GIT_CONFIG_COUNT = [string]($gitConfigCount + 1)
$toolsRoot = Join-Path $PSScriptRoot 'tools'
. (Join-Path $toolsRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $toolsRoot 'LlmWikiJson.ps1')
. (Join-Path $toolsRoot 'LlmWikiProcess.ps1')
Enable-LlmWikiStringDateJsonParsing

$sessionWorkspaceCommands = @(
    'task-start', 'task-status', 'task-refresh', 'task-run', 'task-finish', 'task-verify',
    'status', 'next', 'phase-status', 'phase-next', 'phase-complete', 'pause', 'resume',
    'delivery-status', 'delivery-replan', 'delivery-validate', 'delivery-critique'
)
if (-not $PSBoundParameters.ContainsKey('WorkspacePath') -and $Command -in $sessionWorkspaceCommands) {
    $resolvedSession = & (Join-Path $toolsRoot 'Resolve-LlmWikiSession.ps1') `
        -SessionId $TaskSessionId `
        -Create:($Command -eq 'task-start') `
        -Format Object
    $candidateWorkspace = [string]$resolvedSession.workspacePath
    $candidateAbsolutePath = Join-Path (Resolve-Path (Join-Path $toolsRoot '../..')).Path $candidateWorkspace
    if ($Command -eq 'task-start' -or (Test-Path -LiteralPath $candidateAbsolutePath)) {
        $WorkspacePath = $candidateWorkspace
        Write-Host "LLM Wiki session workspace: $WorkspacePath"
    }
}

function Expand-LlmWikiPathList {
    param([string[]]$Path)
    return @(
        $Path |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ -split '[;,]' } |
            ForEach-Object { $_.Trim() } |
            Where-Object { $_.Length -gt 0 } |
            Sort-Object -Unique
    )
}

if ($PSBoundParameters.ContainsKey('ProposedPath')) {
    $ProposedPath = @(Expand-LlmWikiPathList $ProposedPath)
}

if ($Fast) {
    if ($Command -ne 'verify') { throw '-Fast is supported only with the verify command.' }
    $Command = 'verify-fast'
    Write-Host 'Compatibility alias: verify -Fast -> verify-fast'
}

# Verification receipts are content-addressed by the inputs of each stage. Reuse
# them by default so a timeout or a source-impact failure resumes instead of
# replaying the expensive stages. Callers may still force an uncached gate via
# verify-strict-affected.
if ($Command -in @('verify', 'verify-full') -and $env:CI -ne 'true' -and -not $PSBoundParameters.ContainsKey('ResumePassedStages')) {
    $ResumePassedStages = $true
}

$deltaAwareCommands = @('update', 'repair-verify', 'completion', 'smoke', 'verify', 'verify-fast', 'verify-strict-affected', 'verify-full', 'continue-ui', 'ui-finalize', 'research', 'context', 'packet', 'brief', 'design', 'journeys', 'implementation-plan', 'plan', 'test-plan', 'decision', 'dependencies', 'rollout', 'readiness', 'report', 'diff', 'impact', 'review', 'review-affected', 'ownership', 'policy')
$explicitScopePlanningCommands = @('research', 'context', 'packet', 'brief', 'design', 'journeys', 'implementation-plan', 'plan', 'test-plan', 'decision')
$taskBaselineContext = $null
if ($Command -in @('develop', 'start')) {
    & (Join-Path $toolsRoot 'Manage-LlmWikiTaskBaseline.ps1') -Action Capture -SessionId $TaskSessionId -Format Text
} elseif ($Command -in $deltaAwareCommands -and -not $PSBoundParameters.ContainsKey('ChangedPath') -and
    -not ($Command -in $explicitScopePlanningCommands -and $PSBoundParameters.ContainsKey('ProposedPath'))) {
    $taskBaseline = & (Join-Path $toolsRoot 'Manage-LlmWikiTaskBaseline.ps1') -Action ChangedPaths -SessionId $TaskSessionId -Format Object
    if ($taskBaseline.available) {
        $taskBaselineContext = $taskBaseline
        $ChangedPath = @($taskBaseline.changedPaths)
        $repositoryRoot = (Resolve-Path (Join-Path $toolsRoot '../..')).Path
        $workingTreePaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('diff', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--') -FailureMessage 'Unable to enumerate the current working delta.')
        $workingTreePaths += @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to enumerate untracked working paths.')
        $workingTreePaths = @($workingTreePaths | Where-Object { $_ } | Sort-Object -Unique)
        if ($workingTreePaths.Count -gt 0 -and $ChangedPath.Count -gt [Math]::Max(($workingTreePaths.Count * 3), ($workingTreePaths.Count + 50))) {
            Write-Warning "Task baseline contains $($ChangedPath.Count) paths but the current working delta contains $($workingTreePaths.Count). Using the current delta to avoid stale-session over-expansion."
            $ChangedPath = $workingTreePaths
        }
        $PSBoundParameters['ChangedPath'] = $ChangedPath
    }
}

function Invoke-WikiTool {
    param(
        [string]$Name,
        [hashtable]$ToolArguments = @{}
    )

    $toolPath = Join-Path $toolsRoot $Name
    $global:LASTEXITCODE = 0
    $readOnlyFacadeCommands = @('develop', 'research', 'context', 'trace', 'packet', 'brief', 'integration-scan', 'precedents', 'solutions', 'design', 'journeys', 'ui-trace', 'implementation-plan', 'plan', 'test-plan', 'decision', 'dependencies', 'rollout', 'topology', 'privacy', 'diff', 'ownership', 'api-compat')
    if ($Command -in $readOnlyFacadeCommands) {
        & (Join-Path $toolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $toolPath -ToolArguments $ToolArguments
    } else {
        & $toolPath @ToolArguments
    }
    if (-not $?) {
        exit 1
    }
    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

function Invoke-ObservedWikiStage {
    param(
        [string]$Name,
        [string]$ToolName,
        [hashtable]$ToolArguments = @{},
        [int]$TimeoutSeconds = 120,
        [string]$StandaloneCommand
    )
    $toolPath = Join-Path $toolsRoot $ToolName
    $script:verifyStageOrdinal++
    $expectedSeconds = if ($script:verifyStageExpectedSeconds.ContainsKey($Name)) { [int]$script:verifyStageExpectedSeconds[$Name] } else { $TimeoutSeconds }
    $repositoryRoot = (Resolve-Path (Join-Path $toolsRoot '../..')).Path
    $progressPath = Join-Path $repositoryRoot '.artifacts/llm-wiki/verify-progress.json'
    $logRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/verify-logs'
    $null = New-Item -ItemType Directory -Path $logRoot -Force
    $safeStageName = $Name -replace '[^a-zA-Z0-9_.-]', '-'
    $logPath = Join-Path $logRoot "$safeStageName.log"
    function Write-VerifyProgress([string]$Status, [int]$ElapsedSeconds, [string]$Detail = '') {
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $progressPath) -Force
        $payload = [ordered]@{
            schemaVersion = 1; status = $Status; stage = $Name; ordinal = $script:verifyStageOrdinal; stageCount = 8
            elapsedSeconds = $ElapsedSeconds; expectedSeconds = $expectedSeconds; timeoutSeconds = $TimeoutSeconds
            detail = $Detail; resumeCommand = "./.llm-wiki/wiki.ps1 verify -Stage '$Name'"
            ownerProcessId = $PID; childProcessId = if ($null -ne $process -and -not $process.HasExited) { $process.Id } else { $null }
            logPath = $logPath.Replace($repositoryRoot + [IO.Path]::DirectorySeparatorChar, '').Replace('\', '/')
            updatedAtUtc = [DateTime]::UtcNow.ToString('o')
        }
        $temporary = "$progressPath.$PID.tmp"
        [IO.File]::WriteAllText($temporary, (($payload | ConvertTo-Json -Depth 4) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporary -Destination $progressPath -Force
    }
    $receiptPath = $null
    if ($ResumePassedStages) {
        $repositoryRoot = (Resolve-Path (Join-Path $toolsRoot '../..')).Path
        $fingerprint = & (Join-Path $toolsRoot 'Get-LlmWikiVerificationStageFingerprint.ps1') -Stage $Name -Arguments $ToolArguments
        $gitDirectory = (& git -C $repositoryRoot rev-parse --absolute-git-dir).Trim()
        $script:verifyReceiptRoot = Join-Path $gitDirectory 'llm-wiki/verification-stages/wiki'
        $null = New-Item -ItemType Directory -Path $script:verifyReceiptRoot -Force
        $receiptName = (($Name -replace '[^a-zA-Z0-9_.-]', '-') + '-' + $fingerprint + '.passed')
        $receiptPath = Join-Path $script:verifyReceiptRoot $receiptName
        if (Test-Path -LiteralPath $receiptPath -PathType Leaf) {
            Write-Host "[$script:verifyStageOrdinal/$script:verifyStageTotal] Resuming Wiki verify: $Name already passed for unchanged stage inputs (0s replay)."
            Write-VerifyProgress 'resumed' 0 'Unchanged successful stage receipt reused.'
            return
        }
    }
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    Write-Host "[$script:verifyStageOrdinal/$script:verifyStageTotal] Starting Wiki verify stage: $Name (expected~${expectedSeconds}s, timeout=${TimeoutSeconds}s)"
    Write-VerifyProgress 'running' 0
    $serializableArguments = @{}
    foreach ($entry in $ToolArguments.GetEnumerator()) {
        $serializableArguments[$entry.Key] = if ($entry.Value -is [Management.Automation.SwitchParameter]) { [bool]$entry.Value } else { $entry.Value }
    }
    $argumentsJson = $serializableArguments | ConvertTo-Json -Depth 5 -Compress
    $argumentsPath = Join-Path $logRoot "$safeStageName.arguments.$PID.json"
    [IO.File]::WriteAllText($argumentsPath, $argumentsJson, [Text.UTF8Encoding]::new($false))
    $stageWrapper = Join-Path $toolsRoot 'Invoke-LlmWikiObservedStage.ps1'
    $shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
    $startInfo = New-Object Diagnostics.ProcessStartInfo
    $startInfo.FileName = $shellPath
    $startInfo.WorkingDirectory = (Resolve-Path (Join-Path $toolsRoot '../..')).Path
    $startInfo.UseShellExecute = $false
    $startInfo.Arguments = "-NoLogo -NoProfile -File `"$stageWrapper`" -ToolPath `"$toolPath`" -ArgumentsPath `"$argumentsPath`" -StageName `"$Name`" -LogPath `"$logPath`""
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $startInfo
    if (-not $process.Start()) { throw "Unable to start Wiki verify stage: $Name" }
    $nextHeartbeat = 30
    try {
        while (-not $process.WaitForExit(1000)) {
            if ($stopwatch.Elapsed.TotalSeconds -ge $nextHeartbeat) {
                Write-Host "[$script:verifyStageOrdinal/$script:verifyStageTotal] Wiki verify stage still running: $Name ($([Math]::Round($stopwatch.Elapsed.TotalSeconds))s elapsed, expected~${expectedSeconds}s)"
                Write-VerifyProgress 'running' ([int][Math]::Round($stopwatch.Elapsed.TotalSeconds)) 'Heartbeat: child process is still active.'
                $nextHeartbeat += 30
            }
            if ($stopwatch.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
                Stop-LlmWikiProcessTree -Process $process
                Write-VerifyProgress 'timed-out' ([int][Math]::Round($stopwatch.Elapsed.TotalSeconds)) "Run separately: $StandaloneCommand"
                throw "Wiki verify stage timed out: $Name after ${TimeoutSeconds}s. Run separately: $StandaloneCommand"
            }
        }
        if ($process.ExitCode -ne 0) {
            Write-VerifyProgress 'failed' ([int][Math]::Round($stopwatch.Elapsed.TotalSeconds)) "Run separately: $StandaloneCommand"
            throw "Wiki verify stage failed: $Name (exit=$($process.ExitCode)). Run separately: $StandaloneCommand"
        }
    } finally {
        $process.Dispose()
        $stopwatch.Stop()
        Remove-Item -LiteralPath $argumentsPath -Force -ErrorAction SilentlyContinue
    }
    if ($receiptPath) {
        [IO.File]::WriteAllText($receiptPath, ([DateTime]::UtcNow.ToString('o') + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        $stagePrefix = (($Name -replace '[^a-zA-Z0-9_.-]', '-') + '-')
        Get-ChildItem -LiteralPath $script:verifyReceiptRoot -Filter "$stagePrefix*.passed" -File |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -Skip 5 |
            Remove-Item -Force
    }
    Write-Host "[$script:verifyStageOrdinal/$script:verifyStageTotal] Wiki verify stage passed: $Name ($([Math]::Round($stopwatch.Elapsed.TotalSeconds, 2))s)"
    Write-VerifyProgress 'passed' ([int][Math]::Round($stopwatch.Elapsed.TotalSeconds))
}

switch ($Command) {
    'update' {
        $indexArguments = @{ AffectedOnly = $AffectedOnly; BaseRef = $BaseRef; ReuseUnchangedChecks = $true; RequiredOnly = $ContractIndexesOnly }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $indexArguments.ChangedPath = $ChangedPath }
        Invoke-WikiTool 'Invoke-LlmWikiIndexPipeline.ps1' $indexArguments
    }
    'lint' {
        Invoke-WikiTool 'Test-LlmWiki.ps1' @{ Format = $Format }
    }
    'smoke' {
        switch ($SmokeGroup) {
            'portable' { Invoke-WikiTool 'Test-LlmWikiPortable.ps1' }
            'linux' { Invoke-WikiTool 'Test-LlmWikiLinux.ps1' }
            'tools' {
                if ($AffectedOnly) {
                    $smokeArguments = @{ BaseRef = $BaseRef }
                    if ($PSBoundParameters.ContainsKey('ChangedPath')) { $smokeArguments.ChangedPath = $ChangedPath }
                    Invoke-WikiTool 'Invoke-LlmWikiAffectedSmoke.ps1' $smokeArguments
                } else {
                    Invoke-WikiTool 'Test-LlmWikiTools.ps1'
                }
            }
        }
    }
    'verify' {
        $progressPath = Join-Path (Resolve-Path (Join-Path $toolsRoot '../..')).Path '.artifacts/llm-wiki/verify-progress.json'
        if (Test-Path -LiteralPath $progressPath -PathType Leaf) {
            try {
                $previousProgress = Get-Content -LiteralPath $progressPath -Raw | ConvertFrom-Json
                if ($previousProgress.status -eq 'running') {
                    $ownerAlive = $false
                    if ($previousProgress.ownerProcessId) {
                        $ownerAlive = $null -ne (Get-Process -Id ([int]$previousProgress.ownerProcessId) -ErrorAction SilentlyContinue)
                    }
                    if (-not $ownerAlive) {
                        if ($previousProgress.childProcessId) {
                            $orphanedChild = Get-Process -Id ([int]$previousProgress.childProcessId) -ErrorAction SilentlyContinue
                            if ($orphanedChild) {
                                try { Stop-LlmWikiProcessTree -Process $orphanedChild } catch {
                                    Write-Warning "Orphaned Wiki child cleanup reported: $($_.Exception.Message)"
                                }
                            }
                        }
                        $previousProgress.status = 'interrupted'
                        $previousProgress.detail = 'Previous verify owner is no longer running. Its recorded child tree was cleaned up; resume only the unfinished stage.'
                        $previousProgress.updatedAtUtc = [DateTime]::UtcNow.ToString('o')
                        [IO.File]::WriteAllText($progressPath, (($previousProgress | ConvertTo-Json -Depth 6) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
                        Write-Warning "Recovered stale Wiki verify receipt at stage '$($previousProgress.stage)'. Resume: $($previousProgress.resumeCommand)"
                    }
                }
            } catch { Write-Warning "Unable to inspect prior Wiki verify progress: $($_.Exception.Message)" }
        }
        $script:verifyStageOrdinal = 0
        $script:verifyReceiptRoot = $null
        $script:verifyRunStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $script:verifyStageExpectedSeconds = @{
            'workspace policy' = 2; 'page contracts' = 2; 'lint regression' = 10; 'indexes' = 45
            'affected tool regression' = 15; 'failure knowledge' = 2; 'change policy' = 3; 'source impact' = 3
        }
        if (@($ChangedPath | Where-Object { $_ -match '^\.llm-wiki/(tools/(Get-LlmWikiAdaptiveWorkflow|Start-LlmWikiDevelopment|Invoke-LlmWikiAdaptiveVerification)|evals/)' }).Count -gt 0) {
            $script:verifyStageExpectedSeconds['affected tool regression'] = 240
        }
        $indexArguments = @{ Check = $true; AffectedOnly = $true; BaseRef = $BaseRef; ReuseUnchangedChecks = $true; RequiredOnly = $ContractIndexesOnly; Area = $Area }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $indexArguments.ChangedPath = $ChangedPath }
        $policyArguments = @{ FailOnViolation = $true }
        $impactArguments = @{ FailOnUnreviewed = $true }
        $affectedSmokeArguments = @{ BaseRef = $BaseRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) {
            $policyArguments.ChangedPath = $ChangedPath
            $impactArguments.ChangedPath = $ChangedPath
            $affectedSmokeArguments.ChangedPath = $ChangedPath
        }
        $smokePlanArguments = @{} + $affectedSmokeArguments
        $smokePlanArguments.Plan = $true
        $smokePlanArguments.Format = 'Json'
        $smokePlan = & (Join-Path $toolsRoot 'Invoke-LlmWikiAffectedSmoke.ps1') @smokePlanArguments | ConvertFrom-Json
        $smokeStages = @(@($smokePlan.groups) | ForEach-Object {
            $groupName = [string]$_
            $stageName = "affected smoke: $groupName"
            $script:verifyStageExpectedSeconds[$stageName] = 60
            $groupArguments = @{} + $affectedSmokeArguments
            if ($groupArguments.ContainsKey('ChangedPath')) {
                $groupArguments.ChangedPath = @($groupArguments.ChangedPath | Where-Object { $_ -match '^\.llm-wiki/(?:tools|policies|workflows|evals)/|^\.llm-wiki/wiki\.ps1$' })
            }
            $groupArguments.Group = @($groupName)
            [pscustomobject]@{ Name = $stageName; Tool = 'Invoke-LlmWikiAffectedSmoke.ps1'; Arguments = $groupArguments; Timeout = 300; Standalone = "./.llm-wiki/tools/Invoke-LlmWikiAffectedSmoke.ps1 -Group $groupName" }
        })
        $stages = @(
            [pscustomobject]@{ Name = 'workspace policy'; Tool = 'Get-LlmWikiWorkspacePolicy.ps1'; Arguments = @{ Action = 'validate'; FailOnInvalid = $true }; Timeout = 60; Standalone = './.llm-wiki/wiki.ps1 workspace-policy' }
            [pscustomobject]@{ Name = 'page contracts'; Tool = 'Test-LlmWiki.ps1'; Arguments = @{}; Timeout = 60; Standalone = './.llm-wiki/wiki.ps1 lint' }
            [pscustomobject]@{ Name = 'lint regression'; Tool = 'Test-LlmWikiLint.ps1'; Arguments = @{}; Timeout = 120; Standalone = './.llm-wiki/tools/Test-LlmWikiLint.ps1' }
            [pscustomobject]@{ Name = 'indexes'; Tool = 'Invoke-LlmWikiIndexPipeline.ps1'; Arguments = $indexArguments; Timeout = 300; Standalone = './.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1 -Check' }
            $smokeStages
            [pscustomobject]@{ Name = 'failure knowledge'; Tool = 'Manage-LlmWikiFailures.ps1'; Arguments = @{ Action = 'validate' }; Timeout = 60; Standalone = './.llm-wiki/wiki.ps1 failures -Check' }
            [pscustomobject]@{ Name = 'change policy'; Tool = 'Test-LlmWikiChangePolicy.ps1'; Arguments = $policyArguments; Timeout = 60; Standalone = './.llm-wiki/wiki.ps1 policy -FailOnViolation' }
            [pscustomobject]@{ Name = 'source impact'; Tool = 'Get-LlmWikiImpact.ps1'; Arguments = $impactArguments; Timeout = 60; Standalone = './.llm-wiki/wiki.ps1 impact -FailOnUnreviewed' }
        )
        $script:verifyStageTotal = $stages.Count
        $expectedVerifySeconds = ($script:verifyStageExpectedSeconds.Values | Measure-Object -Sum).Sum
        Write-Host "Wiki verify: $script:verifyStageTotal observable stages, expected cold duration ~${expectedVerifySeconds}s; content-addressed stage resume is enabled=$([bool]$ResumePassedStages)."
        Write-Host 'Buffered-shell progress receipt: .artifacts/llm-wiki/verify-progress.json'
        Write-Host 'Wiki verify mode: affected/resumable. Use verify-full only for an explicit local full-repository gate; CI remains full and uncached.'
        $selectedStages = @(if ($PSBoundParameters.ContainsKey('Stage')) { $stages | Where-Object Name -eq $Stage } else { $stages })
        if ($Stage -and $selectedStages.Count -eq 0) { throw "Unknown verify stage '$Stage'. Available stages: $($stages.Name -join ', ')." }
        if ($Stage) { Write-Host "Wiki verify single-stage mode: $Stage" }
        foreach ($stageDefinition in $selectedStages) {
            Invoke-ObservedWikiStage $stageDefinition.Name $stageDefinition.Tool $stageDefinition.Arguments $stageDefinition.Timeout $stageDefinition.Standalone
        }
        $script:verifyRunStopwatch.Stop()
        Write-Host "Wiki verify completed: $($selectedStages.Count)/$script:verifyStageTotal selected stage(s) in $([Math]::Round($script:verifyRunStopwatch.Elapsed.TotalSeconds, 2))s."
    }
    'verify-fast' {
        $verificationMode = if ($VisualUiCompletion) { 'visual-ui' } else { 'default' }
        $verificationCacheArguments = @{ Action = 'Check'; BaseRef = $BaseRef; ChangedPath = @($ChangedPath); Mode = $verificationMode }
        $verificationCache = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') @verificationCacheArguments
        if ($verificationCache.hit) {
            Write-Host "Fast scoped verification cache hit: repository state and scope are unchanged ($($verificationCache.fingerprint.Substring(0, 12)))."
            Write-Host 'Local completion remains valid. Strict publication verification is enforced by pre-push and CI.'
            break
        }
        $effectiveChangedPath = if ($verificationCache.incrementalStyleOnly) {
            Write-Host "Visual iteration delta: $(@($verificationCache.incrementalPaths).Count) stylesheet path(s); reusing prior source-index and review evidence."
            @($verificationCache.incrementalPaths)
        } else { @($ChangedPath) }
        Invoke-WikiTool 'Get-LlmWikiWorkspacePolicy.ps1' @{ Action = 'validate'; FailOnInvalid = $true }
        Invoke-WikiTool 'Test-LlmWiki.ps1'
        Invoke-WikiTool 'Test-LlmWikiLint.ps1'
        $indexArguments = @{ Check = $true; AffectedOnly = $true; BaseRef = $BaseRef; DeferPossiblyConcurrentStale = $true; ReuseUnchangedChecks = $true }
        if ($verificationCache.incrementalStyleOnly -or $PSBoundParameters.ContainsKey('ChangedPath')) { $indexArguments.ChangedPath = $effectiveChangedPath }
        $deferredUiIndexes = [bool]$VisualUiCompletion
        if ($deferredUiIndexes) {
            $indexArguments.Remove('Check')
            $indexArguments.Remove('DeferPossiblyConcurrentStale')
            $indexArguments.Remove('ReuseUnchangedChecks')
            $indexArguments.Plan = $true
            Write-Host 'Visual UI iteration: index regeneration is deferred until ui-finalize.'
            Invoke-WikiTool 'Invoke-LlmWikiIndexPipeline.ps1' $indexArguments
            $indexResult = @()
        } else {
            $indexResult = @(Invoke-WikiTool 'Invoke-LlmWikiIndexPipeline.ps1' $indexArguments)
        }
        $deferredStale = $deferredUiIndexes -or @($indexResult | Where-Object { $_.deferredStale }).Count -gt 0
        $policyArguments = @{ FailOnViolation = $true }
        $impactArguments = @{ FailOnUnreviewed = -not $deferredStale }
        if ($verificationCache.incrementalStyleOnly -or $PSBoundParameters.ContainsKey('ChangedPath')) {
            $policyArguments.ChangedPath = $effectiveChangedPath
            $impactArguments.ChangedPath = $effectiveChangedPath
        }
        Invoke-WikiTool 'Test-LlmWikiChangePolicy.ps1' $policyArguments
        Invoke-WikiTool 'Get-LlmWikiImpact.ps1' $impactArguments
        if ($deferredStale -and -not $deferredUiIndexes) {
            Write-Warning 'Fast source-impact enforcement was deferred with the possibly concurrent stale indexes. Strict verify remains required in the integration session.'
        } else {
            $verificationCacheArguments.Action = 'Record'
            $null = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') @verificationCacheArguments
        }
        if ($VisualUiCompletion) {
            Write-Host 'Visual UI iteration gate passed. Run ./.llm-wiki/wiki.ps1 ui-finalize once before the final commit; pre-push and CI remain full publication gates.'
        } else {
            Write-Host 'Fast scoped verification passed as the local completion gate. Strict publication verification remains enforced by pre-push and CI.'
        }
    }
    'verify-strict-affected' {
        Write-Host 'Strict affected verification: read-only, uncached, and scoped to the current task delta.'
        Invoke-WikiTool 'Get-LlmWikiWorkspacePolicy.ps1' @{ Action = 'validate'; FailOnInvalid = $true }
        Invoke-WikiTool 'Test-LlmWiki.ps1'
        Invoke-WikiTool 'Test-LlmWikiLint.ps1'
        Invoke-WikiTool 'Test-LlmWikiPortable.ps1'
        $strictIndexArguments = @{ Check = $true; AffectedOnly = $true; BaseRef = $BaseRef }
        $strictSmokeArguments = @{ BaseRef = $BaseRef }
        $policyArguments = @{ FailOnViolation = $true }
        $impactArguments = @{ FailOnUnreviewed = $true }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) {
            $strictIndexArguments.ChangedPath = $ChangedPath
            $strictSmokeArguments.ChangedPath = $ChangedPath
            $policyArguments.ChangedPath = $ChangedPath
            $impactArguments.ChangedPath = $ChangedPath
        }
        Invoke-WikiTool 'Invoke-LlmWikiIndexPipeline.ps1' $strictIndexArguments
        Invoke-WikiTool 'Invoke-LlmWikiAffectedSmoke.ps1' $strictSmokeArguments
        Invoke-WikiTool 'Manage-LlmWikiFailures.ps1' @{ Action = 'validate' }
        Invoke-WikiTool 'Test-LlmWikiChangePolicy.ps1' $policyArguments
        Invoke-WikiTool 'Get-LlmWikiImpact.ps1' $impactArguments
        Write-Host 'Strict affected verification passed. Full repository verification remains the CI gate.'
    }
    { $_ -in @('repair-verify', 'completion') } {
        $repairPaths = @($ChangedPath)
        if ($repairPaths.Count -eq 0) { Write-Host 'Repair verify: no task-delta paths; nothing to repair.'; break }
        Write-Host 'Repair verify [0/3]: checking source formatting before hashing and generation.'
        Invoke-WikiTool 'Test-LlmWikiFormattingReady.ps1' @{ ChangedPath = $repairPaths }
        Write-Host 'Repair verify [1/3]: atomically updating affected indexes.'
        Invoke-WikiTool 'Invoke-LlmWikiIndexPipeline.ps1' @{ AffectedOnly = $true; ChangedPath = $repairPaths; BaseRef = $BaseRef; ReuseUnchangedChecks = $true }
        Write-Host 'Repair verify [2/3]: resolving source-impact reviews.'
        $impactJson = & (Join-Path $toolsRoot 'Get-LlmWikiImpact.ps1') -ChangedPath $repairPaths -Format Json
        $impactResult = $impactJson | ConvertFrom-Json
        $pendingReviewIds = @($impactResult.impacts | Where-Object { -not $_.Reviewed } | ForEach-Object { [string]$_.Id })
        if ($pendingReviewIds.Count -gt 0) {
            if ([string]::IsNullOrWhiteSpace($Reason)) {
                throw "Source-impact review is required for $($pendingReviewIds -join ', '). Re-run completion -Reason '<one evidence-based reason>' to record the grouped review and continue."
            }
            foreach ($pendingPageId in $pendingReviewIds) {
                & (Join-Path $toolsRoot 'Add-LlmWikiSourceReview.ps1') -Id $pendingPageId -Reason $Reason -ChangedPath $repairPaths
                if (-not $?) { exit 1 }
            }
            Write-Host "Recorded one grouped source-impact decision for $($pendingReviewIds.Count) page(s)."
        }
        Write-Host 'Repair verify [3/3]: running resumable affected verification.'
        & $PSCommandPath verify -AffectedOnly -ChangedPath $repairPaths -BaseRef $BaseRef
        if (-not $?) { exit 1 }
        Write-Host 'Completion passed. Each stage is cached independently; an interrupted rerun resumes unchanged successful work.'
    }
    'verify-full' {
        Invoke-WikiTool 'Get-LlmWikiWorkspacePolicy.ps1' @{ Action = 'validate'; FailOnInvalid = $true }
        Invoke-WikiTool 'Test-LlmWiki.ps1'
        Invoke-WikiTool 'Test-LlmWikiLint.ps1'
        Invoke-WikiTool 'Test-LlmWikiPortable.ps1'
        Invoke-WikiTool 'Invoke-LlmWikiFullVerification.ps1' @{ ResumePassedStages = $ResumePassedStages }
        Invoke-WikiTool 'Invoke-LlmWikiAdaptiveVerification.ps1'
        Invoke-WikiTool 'Manage-LlmWikiFailures.ps1' @{ Action = 'validate' }
        $policyArguments = @{ FailOnViolation = $true }
        $impactArguments = @{ FailOnUnreviewed = $true }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) {
            $policyArguments.ChangedPath = $ChangedPath
            $impactArguments.ChangedPath = $ChangedPath
        }
        Invoke-WikiTool 'Test-LlmWikiChangePolicy.ps1' $policyArguments
        Invoke-WikiTool 'Get-LlmWikiImpact.ps1' $impactArguments
    }
    'context' {
        $contextArguments = @{
            Module = $Module
            Query = $Query
            ChangeType = $ChangeType
            Format = $Format
            Limit = $Limit
        }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $contextArguments.ScopePath = $ProposedPath }
        Invoke-WikiTool 'Find-LlmWikiContext.ps1' $contextArguments
    }
    'trace' {
        $traceArguments = @{
            Query = $Query; Format = $Format; Limit = [Math]::Min($Limit, 30)
        }
        if (-not $FullTrace -and $Format -eq 'Text') { $traceArguments.Compact = $true }
        if ($TraceView -eq 'Frontend') {
            Invoke-WikiTool 'Find-LlmWikiFrontendTrace.ps1' $traceArguments
        } elseif ($TraceView -eq 'Backend') {
            Invoke-WikiTool 'Find-LlmWikiTrace.ps1' $traceArguments
        } else {
            $frontendProbe = & (Join-Path $toolsRoot 'Find-LlmWikiFrontendTrace.ps1') -Query $Query -Format Json -Limit ([Math]::Min($Limit, 30)) | ConvertFrom-Json
            if ([bool]$frontendProbe.matched) {
                Invoke-WikiTool 'Find-LlmWikiFrontendTrace.ps1' $traceArguments
            } else {
                Invoke-WikiTool 'Find-LlmWikiTrace.ps1' $traceArguments
            }
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
        if ($PSBoundParameters.ContainsKey('Objective')) { $briefArguments.Intent = $Objective }
        if ($Compact) { $briefArguments.Compact = $true }
        Invoke-WikiTool 'Get-LlmWikiTaskBrief.ps1' $briefArguments
    }
    'develop' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'develop requires -Intent <task description>.' }
        $workflowArguments = @{ Objective = $Objective; BaseRef = $BaseRef; Format = $Format; Limit = $Limit }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $workflowArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $workflowArguments.ChangedPath = $ChangedPath }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $workflowArguments.ProposedPath = $ProposedPath }
        Invoke-WikiTool 'Get-LlmWikiAdaptiveWorkflow.ps1' $workflowArguments
    }
    'start' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'start requires -Intent <task description>.' }
        $startArguments = @{ Objective = $Objective; BaseRef = $BaseRef; WorkspacePath = $WorkspacePath; Format = $Format; Limit = [Math]::Min($Limit, 30) }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $startArguments.ProposedPath = $ProposedPath }
        Invoke-WikiTool 'Start-LlmWikiDevelopment.ps1' $startArguments
    }
    'continue-ui' {
        $continueArguments = @{ BaseRef = $BaseRef; Format = $Format }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $continueArguments.ChangedPath = $ChangedPath }
        if ($PSBoundParameters.ContainsKey('Objective')) { $continueArguments.Intent = $Objective }
        Invoke-WikiTool 'Get-LlmWikiUiContinuation.ps1' $continueArguments
    }
    'ui-finalize' {
        Write-Host 'Finalizing the accumulated UI delta: updating affected indexes once, then running the strict affected gate.'
        $finalizeIndexArguments = @{ AffectedOnly = $true; BaseRef = $BaseRef }
        $finalizeSmokeArguments = @{ BaseRef = $BaseRef }
        $finalizePolicyArguments = @{ FailOnViolation = $true }
        $finalizeImpactArguments = @{ FailOnUnreviewed = $true }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) {
            $finalizeIndexArguments.ChangedPath = $ChangedPath
            $finalizeSmokeArguments.ChangedPath = $ChangedPath
            $finalizePolicyArguments.ChangedPath = $ChangedPath
            $finalizeImpactArguments.ChangedPath = $ChangedPath
        }
        Invoke-WikiTool 'Invoke-LlmWikiIndexPipeline.ps1' $finalizeIndexArguments
        Invoke-WikiTool 'Get-LlmWikiWorkspacePolicy.ps1' @{ Action = 'validate'; FailOnInvalid = $true }
        Invoke-WikiTool 'Test-LlmWiki.ps1'
        Invoke-WikiTool 'Test-LlmWikiLint.ps1'
        $finalizeIndexArguments.Check = $true
        Invoke-WikiTool 'Invoke-LlmWikiIndexPipeline.ps1' $finalizeIndexArguments
        Invoke-WikiTool 'Invoke-LlmWikiAffectedSmoke.ps1' $finalizeSmokeArguments
        Invoke-WikiTool 'Test-LlmWikiChangePolicy.ps1' $finalizePolicyArguments
        Invoke-WikiTool 'Get-LlmWikiImpact.ps1' $finalizeImpactArguments
        Write-Host 'UI finalization passed: affected indexes are synchronized and the accumulated UI delta is publication-ready.'
    }
    { $_ -in @('status', 'next') } {
        $experienceArguments = @{ Action = $Command; WorkspacePath = $WorkspacePath; Format = $Format }
        if ($PSBoundParameters.ContainsKey('Objective')) { $experienceArguments.Objective = $Objective }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $experienceArguments.ProposedPath = $ProposedPath }
        Invoke-WikiTool 'Get-LlmWikiExperience.ps1' $experienceArguments
    }
    'research' {
        if ([string]::IsNullOrWhiteSpace($Objective) -and -not [string]::IsNullOrWhiteSpace($Query)) { $Objective = $Query }
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'research requires -Intent <task description> (compatible alias: -Query).' }
        $researchArguments = @{ Objective = $Objective; Purpose = $ResearchPurpose; BaseRef = $BaseRef; Format = $Format; Limit = $Limit }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $researchArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $researchArguments.ChangedPath = $ChangedPath }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $researchArguments.ProposedPath = $ProposedPath }
        Invoke-WikiTool 'Get-LlmWikiResearchPacket.ps1' $researchArguments
        if ($Format -eq 'Text' -and $Objective -match '\b(I[A-Z][A-Za-z0-9]+(?:Service|Repository))\b') {
            Write-Host ''
            Invoke-WikiTool 'Get-LlmWikiContractConsumers.ps1' @{ Contract = $Matches[1]; Format = 'Text' }
        }
        if ($Format -eq 'Text' -and -not (Test-Path -LiteralPath (Join-Path (Resolve-Path (Join-Path $toolsRoot '../..')).Path $WorkspacePath))) {
            Write-Host 'Delivery note: this is an ordinary research run; delivery-* commands require wiki start/task-start to create governed state.'
        }
    }
    'integration-scan' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'integration-scan requires -Intent <task description>.' }
        $integrationArguments = @{ Objective = $Objective; BaseRef = $BaseRef; Format = $Format; Limit = $Limit }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $integrationArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $integrationArguments.ChangedPath = $ChangedPath }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $integrationArguments.ProposedPath = $ProposedPath }
        Invoke-WikiTool 'Get-LlmWikiIntegrationScan.ps1' $integrationArguments
    }
    'precedents' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'precedents requires -Intent <task description>.' }
        $precedentArguments = @{ Objective = $Objective; Limit = [Math]::Min($Limit, 20); Format = $Format }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $precedentArguments.ScopePath = $ProposedPath }
        Invoke-WikiTool 'Get-LlmWikiGitPrecedents.ps1' $precedentArguments
    }
    'solutions' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'solutions requires -Intent <task description>.' }
        $solutionArguments = @{ Objective = $Objective; Format = $Format }
        if ($PSBoundParameters.ContainsKey('Option')) { $solutionArguments.Option = $Option }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $solutionArguments.ProposedPath = $ProposedPath }
        if ($PSBoundParameters.ContainsKey('BoundaryEvidence')) { $solutionArguments.BoundaryEvidence = $BoundaryEvidence }
        Invoke-WikiTool 'Get-LlmWikiSolutionComparison.ps1' $solutionArguments
    }
    'design' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'design requires -Intent <task description>.' }
        $designArguments = @{ Objective = $Objective; BaseRef = $BaseRef; Format = $Format; Limit = $Limit }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $designArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $designArguments.ChangedPath = $ChangedPath }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $designArguments.ProposedPath = $ProposedPath }
        if ($PSBoundParameters.ContainsKey('Decision')) { $designArguments.Decision = $Decision }
        Invoke-WikiTool 'Get-LlmWikiDesignCheckpoint.ps1' $designArguments
        if ($Format -eq 'Text' -and -not (Test-Path -LiteralPath (Join-Path (Resolve-Path (Join-Path $toolsRoot '../..')).Path $WorkspacePath))) {
            Write-Host 'Delivery note: design does not create governed state. Use wiki start/task-start only when the selected workflow requires delivery validation.'
        }
    }
    'contract-consumers' {
        $contractName = if (-not [string]::IsNullOrWhiteSpace($Query)) { $Query } elseif (-not [string]::IsNullOrWhiteSpace($Objective)) { $Objective } else { $null }
        if (-not $contractName) { throw 'contract-consumers requires -Query <contract type>.' }
        Invoke-WikiTool 'Get-LlmWikiContractConsumers.ps1' @{ Contract = $contractName; Format = $Format }
    }
    'extraction' {
        if ([string]::IsNullOrWhiteSpace($Module)) { throw 'extraction requires -Module <module name>.' }
        Invoke-WikiTool 'Get-LlmWikiExtractionReadiness.ps1' @{ Module = $Module; Format = $Format }
    }
    { $_ -in @('phase-status', 'phase-next', 'phase-complete') } {
        $phaseAction = @{ 'phase-status' = 'status'; 'phase-next' = 'next'; 'phase-complete' = 'complete' }[$Command]
        $phaseArguments = @{ Action = $phaseAction; WorkspacePath = $WorkspacePath; Format = $Format; FailOnInvalid = $FailOnInvalid }
        if ($PSBoundParameters.ContainsKey('PhaseId')) { $phaseArguments.PhaseId = $PhaseId }
        Invoke-WikiTool 'Get-LlmWikiPhaseStatus.ps1' $phaseArguments
    }
    'qa' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'qa requires -Intent <task description>.' }
        $qaArguments = @{ Objective = $Objective; Format = $Format }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $qaArguments.ProposedPath = $ProposedPath }
        Invoke-WikiTool 'Get-LlmWikiManualQaPlan.ps1' $qaArguments
    }
    'visual-qa' {
        foreach ($requiredValue in @(@{ name = 'Url'; value = $Url }, @{ name = 'FixturePath'; value = $FixturePath }, @{ name = 'ResultSelector'; value = $ResultSelector })) {
            if ([string]::IsNullOrWhiteSpace([string]$requiredValue.value)) { throw "visual-qa requires -$($requiredValue.name)." }
        }
        $visualQaArguments = @{ Url = $Url; FixturePath = $FixturePath; ResultSelector = $ResultSelector; FileSelector = $FileSelector; Run = $Run; Format = $Format }
        if ($TriggerSelector) { $visualQaArguments.TriggerSelector = $TriggerSelector }
        if ($StorageStatePath) { $visualQaArguments.StorageStatePath = $StorageStatePath }
        if ($OutputPath) { $visualQaArguments.ScreenshotPath = $OutputPath }
        if ($null -ne $ViewportWidth) { $visualQaArguments.ViewportWidth = $ViewportWidth }
        if ($null -ne $ViewportHeight) { $visualQaArguments.ViewportHeight = $ViewportHeight }
        if ($null -ne $TimeoutMs) { $visualQaArguments.TimeoutMs = $TimeoutMs }
        Invoke-WikiTool 'Invoke-LlmWikiVisualQa.ps1' $visualQaArguments
    }
    'workflow-metrics' {
        Invoke-WikiTool 'Get-LlmWikiWorkflowMetrics.ps1' @{ TasksPath = $TasksPath; Format = $Format }
    }
    { $_ -in @('pause', 'resume') } {
        Invoke-WikiTool 'Manage-LlmWikiAdaptiveSession.ps1' @{
            Action = $Command
            WorkspacePath = $WorkspacePath
            Limit = $Limit
            Overwrite = $Overwrite
            Format = $Format
        }
    }
    'journeys' {
        $journeyArguments = @{ Query = $(if (-not [string]::IsNullOrWhiteSpace($Query)) { $Query } else { $Objective }); Limit = $Limit; Format = $Format }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $journeyArguments.ChangedPath = $ChangedPath }
        elseif ($PSBoundParameters.ContainsKey('ProposedPath')) { $journeyArguments.ChangedPath = $ProposedPath }
        Invoke-WikiTool 'Find-LlmWikiProductJourney.ps1' $journeyArguments
    }
    'ui-trace' {
        $uiTraceArguments = @{ Query = $(if (-not [string]::IsNullOrWhiteSpace($Query)) { $Query } else { $Objective }); Limit = $Limit; Format = $Format }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $uiTraceArguments.CandidatePath = $ChangedPath }
        elseif ($PSBoundParameters.ContainsKey('ProposedPath')) { $uiTraceArguments.CandidatePath = $ProposedPath }
        Invoke-WikiTool 'Get-LlmWikiFrontendRuntimeOwner.ps1' $uiTraceArguments
    }
    { $_ -in @('delivery-status', 'delivery-replan', 'delivery-validate', 'delivery-critique') } {
        $deliveryAction = @{
            'delivery-status' = 'status'
            'delivery-replan' = 'replan'
            'delivery-validate' = 'validate'
            'delivery-critique' = 'critique'
        }[$Command]
        $repositoryRoot = (Resolve-Path (Join-Path $toolsRoot '../..')).Path
        $absoluteDeliveryWorkspace = if ([IO.Path]::IsPathRooted($WorkspacePath)) { $WorkspacePath } else { Join-Path $repositoryRoot $WorkspacePath }
        if (-not (Test-Path -LiteralPath $absoluteDeliveryWorkspace -PathType Container)) {
            if ($Command -eq 'delivery-status') {
                Write-Host "No governed task workspace exists at $WorkspacePath. develop/research/design do not imply governed delivery state."
                Write-Host 'Continue with diff + test-plan for an ordinary feature, or run task-start first when the adaptive workflow explicitly requires a workspace.'
                break
            }
            throw "Governed delivery command '$Command' requires a task workspace. Run the workspace-stage task-start command emitted by develop first."
        }
        Invoke-WikiTool 'Invoke-LlmWikiDeliveryWorkflow.ps1' @{
            Action = $deliveryAction
            WorkspacePath = $WorkspacePath
            Reason = $Reason
            DryRun = $DryRun
            FailOnInvalid = $FailOnInvalid
            Format = $Format
        }
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
        if ($PSBoundParameters.ContainsKey('Objective')) { $testPlanArguments.Intent = $Objective }
        if ($Compact) { $testPlanArguments.Compact = $true }
        Invoke-WikiTool 'Get-LlmWikiTestPlan.ps1' $testPlanArguments
    }
    { $_ -in @('verification-record', 'verification-list') } {
        $receiptArguments = @{
            Action = $(if ($Command -eq 'verification-record') { 'Record' } else { 'List' })
            Format = $Format
        }
        if ($Command -eq 'verification-record') {
            $receiptArguments.Command = $EvidenceCommand
            $receiptArguments.Result = $(if ($Status -eq 'failed') { 'failed' } else { 'passed' })
            $receiptArguments.DurationSeconds = $DurationSeconds
            $receiptArguments.CoverageScope = @($CoverageScope)
        }
        Invoke-WikiTool 'Manage-LlmWikiVerificationReceipts.ps1' $receiptArguments
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
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $taskStartArguments.PlannedPath = $ProposedPath }
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
        $privacyArguments = @{
            Query = $Query
            Category = $PrivacyCategory
            Limit = $Limit
            Format = $Format
        }
        if ($PSBoundParameters.ContainsKey('ProposedPath')) { $privacyArguments.ScopePath = $ProposedPath }
        Invoke-WikiTool 'Find-LlmWikiSensitiveData.ps1' $privacyArguments
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
        if ($null -ne $taskBaselineContext -and @($taskBaselineContext.excludedChangedPaths).Count -gt 0) {
            $diffArguments.BaselineExcludedPath = @($taskBaselineContext.excludedChangedPaths)
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
    'review' {
        $pageReviewIds = if ($PSBoundParameters.ContainsKey('ReviewId')) { @($ReviewId) } elseif ($PSBoundParameters.ContainsKey('Id')) { @($Id) } else { @() }
        $pageReviewIds = @($pageReviewIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
        if ($pageReviewIds.Count -eq 0) { throw 'review requires -Id or -ReviewId.' }
        foreach ($pageReviewId in $pageReviewIds) {
            $reviewArguments = @{ Id = $pageReviewId; Reason = $Reason; BaseRef = $BaseRef }
            if ($PSBoundParameters.ContainsKey('HeadRef')) { $reviewArguments.HeadRef = $HeadRef }
            if ($PSBoundParameters.ContainsKey('ChangedPath')) { $reviewArguments.ChangedPath = $ChangedPath }
            Invoke-WikiTool 'Add-LlmWikiSourceReview.ps1' $reviewArguments
        }
        if ($pageReviewIds.Count -gt 1) { Write-Host "Grouped source-impact review recorded for $($pageReviewIds.Count) pages with one shared rationale." }
    }
    'review-affected' {
        if ([string]::IsNullOrWhiteSpace($Reason)) { throw 'review-affected requires -Reason describing the shared source review.' }
        $impactArguments = @{ BaseRef = $BaseRef; Format = 'Json' }
        if ($PSBoundParameters.ContainsKey('HeadRef')) { $impactArguments.HeadRef = $HeadRef }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $impactArguments.ChangedPath = $ChangedPath }
        $impact = & (Join-Path $toolsRoot 'Get-LlmWikiImpact.ps1') @impactArguments | ConvertFrom-Json
        $pending = @($impact.impacts | Where-Object { -not $_.Reviewed -and [string]::IsNullOrWhiteSpace([string]$_.GeneratedBy) })
        $protected = @($pending | Where-Object { $_.Id -match '(?i)privacy|security|architecture|decision' -or $_.Path -match '(?i)privacy|security|architecture|adr' })
        $protectedIds = @($protected | ForEach-Object { if ($_.PSObject.Properties['Id']) { $_.Id } })
        $safe = @($pending | Where-Object { $_.PSObject.Properties['Id'] -and $_.Id -notin $protectedIds })
        foreach ($item in $pending) { Write-Host " - $($item.Id): $($item.Path) <- $($item.ChangedSources -join ', ')" }
        foreach ($item in $safe) {
            $reviewArguments = @{ Id = [string]$item.Id; Reason = $Reason; BaseRef = $BaseRef }
            if ($PSBoundParameters.ContainsKey('HeadRef')) { $reviewArguments.HeadRef = $HeadRef }
            if ($PSBoundParameters.ContainsKey('ChangedPath')) { $reviewArguments.ChangedPath = $ChangedPath }
            Invoke-WikiTool 'Add-LlmWikiSourceReview.ps1' $reviewArguments
        }
        if ($protected.Count -gt 0) { Write-Warning "$($protected.Count) architecture/privacy/security page(s) require explicit wiki.ps1 review -ReviewId <id> with a page-specific rationale." }
        Write-Host "Affected reviews: recorded=$($safe.Count), explicit-required=$($protected.Count), already-reviewed=$([int]$impact.impactCount - $pending.Count)."
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
    'evidence-artifact' {
        Invoke-WikiTool 'Manage-LlmWikiEvidence.ps1' @{
            Action = 'artifact'
            Path = $EvidencePath
            Id = $Id
            OutputPath = $OutputPath
            ArtifactKind = $EvidenceKind
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
        Write-Host '  ./.llm-wiki/wiki.ps1 update [-AffectedOnly] [-BaseRef <ref>] [-ChangedPath <path[]>]'
        Write-Host "  ./.llm-wiki/wiki.ps1 completion [-Reason '<grouped source-review rationale>'] [-ChangedPath <path[]>]  # update -> reviews -> resumable verify"
        Write-Host "  ./.llm-wiki/wiki.ps1 repair-verify ...  # compatibility alias for completion"
        Write-Host '  ./.llm-wiki/wiki.ps1 lint [-Format Json]'
        Write-Host '  ./.llm-wiki/wiki.ps1 smoke -SmokeGroup portable|linux|tools [-AffectedOnly] [-ChangedPath <path[]>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 verify-fast [-BaseRef <ref>] [-ChangedPath <path[]>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 verify-strict-affected [-BaseRef <ref>] [-ChangedPath <path[]>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 verify [-Fast] [-AffectedOnly] [-BaseRef <ref>] [-ChangedPath <path[]>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 verify-full'
        Write-Host '  ./.llm-wiki/wiki.ps1 context -Module Billing -ChangeType Api'
        Write-Host '  ./.llm-wiki/wiki.ps1 trace -Query <backend-request-or-frontend-symbol> [-TraceView Auto|Backend|Frontend] [-FullTrace]'
        Write-Host '  ./.llm-wiki/wiki.ps1 packet -Objective <text> [-OutputPath <path>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 brief'
        Write-Host '  ./.llm-wiki/wiki.ps1 plan -Objective <text> [-ChangedPath <path>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 test-plan'
        Write-Host "  ./.llm-wiki/wiki.ps1 verification-record -EvidenceCommand '<command>' -Status passed -DurationSeconds <seconds> [-CoverageScope <scope>]"
        Write-Host '  ./.llm-wiki/wiki.ps1 verification-list'
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
        Write-Host "  ./.llm-wiki/wiki.ps1 impact-simulate -PlannedPath @('path/one','path/two') [-Objective <text>]"
        Write-Host "  Multiple paths also accept: -PlannedPath 'path/one;path/two'"
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
        Write-Host "  ./.llm-wiki/wiki.ps1 develop -Intent '<task>' [-PlannedPath 'path/one','path/two']"
        Write-Host "  ./.llm-wiki/wiki.ps1 start -Intent '<large task>' [-PlannedPath 'path/one','path/two']  # baseline + research + checklist + governed workspace"
        Write-Host "  ./.llm-wiki/wiki.ps1 continue-ui [-Intent '<iteration>'] [-ChangedPath <path[]>]"
        Write-Host "  ./.llm-wiki/wiki.ps1 ui-finalize [-ChangedPath <accumulated-ui-path[]>]"
        Write-Host "  ./.llm-wiki/wiki.ps1 next|status [-WorkspacePath <task>] [-Intent '<new task>']"
        Write-Host "  ./.llm-wiki/wiki.ps1 research -Intent '<task>' [-Query '<compatible alias>'] [-ResearchPurpose Assessment|Implementation] [-PlannedPath 'path/one','path/two']"
        Write-Host "  ./.llm-wiki/wiki.ps1 extraction -Module Users  # contract and boundary-wide aggregate readiness"
        Write-Host "  ./.llm-wiki/wiki.ps1 integration-scan -Intent '<task>' [-PlannedPath 'path/one','path/two']"
        Write-Host "  ./.llm-wiki/wiki.ps1 precedents -Intent '<task>' [-PlannedPath 'path/one','path/two']"
        Write-Host "  ./.llm-wiki/wiki.ps1 solutions -Intent '<task>' [-Option '<option one>','<option two>'] [-BoundaryEvidence '<current-source proof>']"
        Write-Host "  ./.llm-wiki/wiki.ps1 design -Intent '<task>' [-PlannedPath 'path/one','path/two']"
        Write-Host '  ./.llm-wiki/wiki.ps1 phase-status|phase-next|phase-complete -WorkspacePath <task> [-PhaseId <id>]'
        Write-Host "  ./.llm-wiki/wiki.ps1 qa -Intent '<task>' [-PlannedPath 'path/one','path/two']"
        Write-Host "  ./.llm-wiki/wiki.ps1 visual-qa -Url <url> -FixturePath <file> -ResultSelector <selector> [-TriggerSelector <selector>] [-StorageStatePath <file>] [-Run]"
        Write-Host '  ./.llm-wiki/wiki.ps1 workflow-metrics [-TasksPath .artifacts/llm-wiki/tasks]'
        Write-Host '  ./.llm-wiki/wiki.ps1 pause -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 resume -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host "  ./.llm-wiki/wiki.ps1 journeys -Intent '<task>' [-PlannedPath 'path/one','path/two']"
        Write-Host '  ./.llm-wiki/wiki.ps1 delivery-status -WorkspacePath .artifacts/llm-wiki/tasks/<name>'
        Write-Host '  ./.llm-wiki/wiki.ps1 delivery-replan -WorkspacePath <task> -Reason <evidence> [-DryRun]'
        Write-Host '  ./.llm-wiki/wiki.ps1 delivery-validate -WorkspacePath <task> -FailOnInvalid'
        Write-Host '  ./.llm-wiki/wiki.ps1 delivery-critique -WorkspacePath <task> -FailOnInvalid'
        Write-Host '  ./.llm-wiki/wiki.ps1 topology [-Query <text>]'
        Write-Host "  ./.llm-wiki/wiki.ps1 privacy -PrivacyCategory credential [-PlannedPath @('path/one','path/two')]"
        Write-Host '  ./.llm-wiki/wiki.ps1 ui -FrontendView components -Query autocomplete'
        Write-Host '  ./.llm-wiki/wiki.ps1 domain -DomainView invariants -Query weight'
        Write-Host '  ./.llm-wiki/wiki.ps1 contracts -BackendContractView consumers -Query StartFastingCommand'
        Write-Host '  ./.llm-wiki/wiki.ps1 health -HealthView dead-candidates'
        Write-Host '  ./.llm-wiki/wiki.ps1 hotspots [-Query <text>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 test-gaps [-Query <text>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 debt'
        Write-Host '  ./.llm-wiki/wiki.ps1 diff'
        Write-Host '  ./.llm-wiki/wiki.ps1 impact -FailOnUnreviewed'
        Write-Host '  ./.llm-wiki/wiki.ps1 review -Id <page-id> -Reason <reason> [-BaseRef <ref>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 review -ReviewId <page-id[]> -Reason <shared-reason> [-ChangedPath <path[]>]'
        Write-Host '  ./.llm-wiki/wiki.ps1 ownership'
        Write-Host '  ./.llm-wiki/wiki.ps1 api-compat -BaseRef HEAD -FailOnBreaking'
        Write-Host '  ./.llm-wiki/wiki.ps1 policy [-RequireEvidence]'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-init'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-run -Id <id>'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-check -Id <id> -Status passed'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-review -Id <id> -Status completed -Reason <text>'
        Write-Host '  ./.llm-wiki/wiki.ps1 evidence-artifact -Id <review-id> -OutputPath <file> -EvidenceKind screenshot -Reason <text>'
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
