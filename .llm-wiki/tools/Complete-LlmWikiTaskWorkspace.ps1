[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('finish', 'verify')]
    [string]$Action = 'finish',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [switch]$DryRun,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policySnapshot = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json | ConvertFrom-Json

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+(?:/.*)?$') {
    throw 'WorkspacePath must be inside .artifacts/llm-wiki/tasks/<task-name>.'
}
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
if (-not (Test-Path -LiteralPath $absoluteWorkspacePath -PathType Container)) {
    throw "Task workspace does not exist: $normalizedWorkspacePath"
}

$sealedArtifactNames = @(
    'workspace.json'
    'change-packet.json'
    'task-contract.json'
    'change-manifest.json'
    'acceptance-matrix.json'
    'evidence.json'
    'journal.json'
    'review-report.md'
)
$completionPath = Join-Path $absoluteWorkspacePath 'completion.json'
$completionMarkdownPath = Join-Path $absoluteWorkspacePath 'completion.md'

function Get-Sha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-ObjectFingerprint([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 12 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
}

function Write-Result([object]$Result) {
    if ($Format -eq 'Json') {
        $Result | ConvertTo-Json -Depth 12
    } else {
        Write-Host "Task completion seal: valid=$($Result.valid), workspace=$($Result.workspace)"
        foreach ($issue in @($Result.issues)) { Write-Host " - $issue" }
    }
}

if ($Action -eq 'verify') {
    $issues = [System.Collections.Generic.List[string]]::new()
    $completion = $null
    $storedPolicyFingerprint = ''
    $policyDrift = $false
    if (-not (Test-Path -LiteralPath $completionPath -PathType Leaf)) {
        $issues.Add('completion.json is absent.')
    } else {
        try {
            $completion = Get-Content -LiteralPath $completionPath -Raw | ConvertFrom-Json
            if ($null -ne $completion.PSObject.Properties['policyFingerprint']) {
                $storedPolicyFingerprint = [string]$completion.policyFingerprint
            }
            $fingerprintPayload = [ordered]@{
                schemaVersion = $completion.schemaVersion
                objective = $completion.objective
                finishedAtUtc = $completion.finishedAtUtc
                git = $completion.git
                packetFingerprint = $completion.packetFingerprint
                readiness = $completion.readiness
                artifactHashes = $completion.artifactHashes
            }
            if ($completion.schemaVersion -ge 2) {
                $fingerprintPayload.policyFingerprint = $completion.policyFingerprint
                $policyDrift = $storedPolicyFingerprint -cne [string]$policySnapshot.fingerprint
            }
            $expectedCompletionFingerprint = Get-ObjectFingerprint $fingerprintPayload
            if ($expectedCompletionFingerprint -cne [string]$completion.completionFingerprint) {
                $issues.Add('completion.json fingerprint is invalid.')
            }
            foreach ($artifactProperty in @($completion.artifactHashes.PSObject.Properties)) {
                $artifactPath = Join-Path $absoluteWorkspacePath $artifactProperty.Name
                if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
                    $issues.Add("Sealed artifact is missing: $($artifactProperty.Name)")
                } elseif ((Get-Sha256 $artifactPath) -cne [string]$artifactProperty.Value) {
                    $issues.Add("Sealed artifact changed: $($artifactProperty.Name)")
                }
            }
            if ([string]$completion.readiness.verdict -ne 'ready') {
                $issues.Add("Completion was not sealed from a ready verdict: $($completion.readiness.verdict)")
            }
            $retrospectivePath = Join-Path $absoluteWorkspacePath 'retrospective.json'
            if (Test-Path -LiteralPath $retrospectivePath -PathType Leaf) {
                $retrospectiveValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') verify `
                    -WorkspacePath $normalizedWorkspacePath `
                    -Format Json | ConvertFrom-Json
                foreach ($issue in @($retrospectiveValidation.issues)) {
                    $issues.Add("Retrospective: $issue")
                }
            }
        } catch {
            $issues.Add("Unable to read completion seal: $($_.Exception.Message)")
        }
    }
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspacePath
        valid = $issues.Count -eq 0
        storedPolicyFingerprint = $storedPolicyFingerprint
        currentPolicyFingerprint = [string]$policySnapshot.fingerprint
        policyDrift = $policyDrift
        issues = @($issues)
    }
    Write-Result $result
    if ($FailOnInvalid -and -not $result.valid) { exit 1 }
    return
}

if (Test-Path -LiteralPath $completionPath -PathType Leaf) {
    throw "Task workspace is already sealed: $normalizedWorkspacePath/completion.json"
}
foreach ($artifactName in $sealedArtifactNames) {
    if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspacePath $artifactName) -PathType Leaf)) {
        throw "Task workspace is incomplete; missing $artifactName."
    }
}

$status = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') status `
    -WorkspacePath $normalizedWorkspacePath `
    -Detailed `
    -Format Json | ConvertFrom-Json
if ($status.verdict -ne 'ready') {
    $actionText = @($status.nextActions) -join ' | '
    throw "Task workspace is not ready ($($status.verdict), $($status.score)/100). $actionText"
}
if ($DryRun) {
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspacePath
        valid = $true
        dryRun = $true
        verdict = $status.verdict
        score = $status.score
        issues = @()
    }
    Write-Result $result
    return
}

& (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') refresh `
    -WorkspacePath $normalizedWorkspacePath | Out-Null
& (Join-Path $PSScriptRoot 'Manage-LlmWikiPlanConformance.ps1') create `
    -WorkspacePath $normalizedWorkspacePath `
    -FailOnInvalid | Out-Null
& (Join-Path $PSScriptRoot 'Manage-LlmWikiRequirementModel.ps1') create `
    -WorkspacePath $normalizedWorkspacePath `
    -FailOnInvalid | Out-Null
& (Join-Path $PSScriptRoot 'Manage-LlmWikiImpactSimulation.ps1') create `
    -WorkspacePath $normalizedWorkspacePath `
    -FailOnInvalid | Out-Null
& (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') create `
    -WorkspacePath $normalizedWorkspacePath `
    -FailOnInvalid | Out-Null
$failurePredictionPath = Join-Path $absoluteWorkspacePath 'failure-prediction.json'
if (Test-Path -LiteralPath $failurePredictionPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -FailOnInvalid | Out-Null
} else {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') create `
        -WorkspacePath $normalizedWorkspacePath `
        -FailOnInvalid | Out-Null
}
$sealedArtifactNames += 'risk-calibration.json'
$sealedArtifactNames += 'failure-prediction.json'
$verificationCostPath = Join-Path $absoluteWorkspacePath 'verification-cost.json'
if (Test-Path -LiteralPath $verificationCostPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationCost.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -FailOnInvalid | Out-Null
} else {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationCost.ps1') create `
        -WorkspacePath $normalizedWorkspacePath `
        -FailOnInvalid | Out-Null
}
$sealedArtifactNames += 'verification-cost.json'
$modelRoutingPath = Join-Path $absoluteWorkspacePath 'model-routing.json'
if (Test-Path -LiteralPath $modelRoutingPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRouting.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -FailOnInvalid | Out-Null
    $sealedArtifactNames += 'model-routing.json'
}
$repairLoopPath = Join-Path $absoluteWorkspacePath 'repair-loop.json'
if (Test-Path -LiteralPath $repairLoopPath -PathType Leaf) {
    $repairLoop = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLoop.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -FailOnInvalid `
        -Format Json | ConvertFrom-Json
    if (@($repairLoop.activeAttempts).Count -gt 0 -or @($repairLoop.unresolvedAttempts).Count -gt 0) {
        throw 'Task completion requires every repair attempt chain to end with a completed attempt.'
    }
    $sealedArtifactNames += 'repair-loop.json'
}
$sealedArtifactNames += 'plan-conformance.json'
$sealedArtifactNames += 'requirement-model.json'
$sealedArtifactNames += 'impact-simulation.json'
$sealedArtifactNames += 'proof-of-change.json'
$confidenceLedger = & (Join-Path $PSScriptRoot 'Manage-LlmWikiConfidenceLedger.ps1') create `
    -WorkspacePath $normalizedWorkspacePath `
    -FailOnInvalid `
    -Format Json | ConvertFrom-Json
if ($confidenceLedger.ledger.verdict -eq 'blocked') {
    throw "Task completion cannot seal a blocked confidence ledger at $($confidenceLedger.ledger.score)/100."
}
$sealedArtifactNames += 'confidence-ledger.json'
$changeCritique = & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeCritique.ps1') create `
    -WorkspacePath $normalizedWorkspacePath `
    -FailOnInvalid `
    -Format Json | ConvertFrom-Json
if ($changeCritique.critique.verdict -in @('reject', 'request-changes')) {
    throw "Task completion cannot seal critique verdict '$($changeCritique.critique.verdict)' at $($changeCritique.critique.score)/100."
}
$sealedArtifactNames += 'change-critique.json'
$contextBundlePath = Join-Path $absoluteWorkspacePath 'context-bundle.json'
if (Test-Path -LiteralPath $contextBundlePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -FailOnInvalid | Out-Null
    $contextBudgetPath = Join-Path $absoluteWorkspacePath 'context-budget.json'
    if (Test-Path -LiteralPath $contextBudgetPath -PathType Leaf) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBudget.ps1') verify `
            -WorkspacePath $normalizedWorkspacePath `
            -FailOnInvalid | Out-Null
    } else {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBudget.ps1') create `
            -WorkspacePath $normalizedWorkspacePath `
            -FailOnInvalid | Out-Null
    }
    $sealedArtifactNames += 'context-bundle.json'
    $sealedArtifactNames += 'context-security.json'
    $sealedArtifactNames += 'context-budget.json'
    $contextBenchmarkPath = Join-Path $absoluteWorkspacePath 'context-benchmark.json'
    if (Test-Path -LiteralPath $contextBenchmarkPath -PathType Leaf) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBenchmark.ps1') verify `
            -WorkspacePath $normalizedWorkspacePath `
            -FailOnInvalid `
            -FailOnRegression | Out-Null
        $sealedArtifactNames += 'context-benchmark.json'
    }
    $contextExperimentPath = Join-Path $absoluteWorkspacePath 'context-experiment.json'
    if (Test-Path -LiteralPath $contextExperimentPath -PathType Leaf) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextExperiment.ps1') verify `
            -WorkspacePath $normalizedWorkspacePath `
            -FailOnInvalid | Out-Null
        $sealedArtifactNames += 'context-experiment.json'
    }
    $contextStrategyApprovalPath = Join-Path $absoluteWorkspacePath 'context-strategy-approval.json'
    if (Test-Path -LiteralPath $contextStrategyApprovalPath -PathType Leaf) {
        throw 'Task completion cannot seal a context strategy approval that has not been applied.'
    }
    $contextStrategyApplicationPath = Join-Path $absoluteWorkspacePath 'context-strategy-application.json'
    if (Test-Path -LiteralPath $contextStrategyApplicationPath -PathType Leaf) {
        $contextStrategy = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextStrategy.ps1') verify `
            -WorkspacePath $normalizedWorkspacePath `
            -FailOnInvalid `
            -Format Json | ConvertFrom-Json
        if ([string]$contextStrategy.strategy.state -eq 'rollback-recommended') {
            throw 'Task completion cannot seal a context strategy with a rollback recommendation.'
        }
        $sealedArtifactNames += 'context-strategy-application.json'
    }
}
$planReusePath = Join-Path $absoluteWorkspacePath 'plan-reuse.json'
if (Test-Path -LiteralPath $planReusePath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskSimilarity.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -FailOnInvalid | Out-Null
    $sealedArtifactNames += 'plan-reuse.json'
    if (Test-Path -LiteralPath (Join-Path $absoluteWorkspacePath 'verification-plan.json') -PathType Leaf) {
        $sealedArtifactNames += 'verification-plan.json'
    }
}
$status = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') status `
    -WorkspacePath $normalizedWorkspacePath `
    -Detailed `
    -Format Json | ConvertFrom-Json
if ($status.verdict -ne 'ready') {
    throw "Task readiness changed during final refresh: $($status.verdict)."
}

$artifactHashes = [ordered]@{}
foreach ($artifactName in $sealedArtifactNames) {
    $artifactHashes[$artifactName] = Get-Sha256 (Join-Path $absoluteWorkspacePath $artifactName)
}
$head = git rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD.' }
$completionPayload = [ordered]@{
    schemaVersion = 2
    objective = $status.objective
    finishedAtUtc = [DateTime]::UtcNow.ToString('o')
    git = [ordered]@{
        head = [string]$head
    }
    packetFingerprint = $status.currentPacketFingerprint
    readiness = [ordered]@{
        verdict = $status.verdict
        score = $status.score
    }
    artifactHashes = $artifactHashes
    policyFingerprint = [string]$policySnapshot.fingerprint
}
$completion = [ordered]@{}
foreach ($property in $completionPayload.GetEnumerator()) { $completion[$property.Key] = $property.Value }
$completion['completionFingerprint'] = Get-ObjectFingerprint $completionPayload

$lines = @(
    '# LLM Wiki Task Completion'
    ''
    "- Objective: $($completion.objective)"
    "- Finished UTC: $($completion.finishedAtUtc)"
    "- Git HEAD: ``$($completion.git.head)``"
    "- Packet: ``$($completion.packetFingerprint)``"
    "- Readiness: **$($completion.readiness.verdict)** ($($completion.readiness.score)/100)"
    "- Completion fingerprint: ``$($completion.completionFingerprint)``"
    ''
    '## Sealed Artifacts'
    ''
)
foreach ($artifact in $artifactHashes.GetEnumerator()) {
    $lines += "- ``$($artifact.Key)``: ``$($artifact.Value)``"
}

$temporaryId = [guid]::NewGuid().ToString('N')
$temporaryJsonPath = Join-Path $absoluteWorkspacePath ".completion-$temporaryId.json"
$temporaryMarkdownPath = Join-Path $absoluteWorkspacePath ".completion-$temporaryId.md"
try {
    [System.IO.File]::WriteAllText(
        $temporaryJsonPath,
        (($completion | ConvertTo-Json -Depth 12) + [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText(
        $temporaryMarkdownPath,
        (($lines -join [Environment]::NewLine) + [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temporaryJsonPath -Destination $completionPath
    Move-Item -LiteralPath $temporaryMarkdownPath -Destination $completionMarkdownPath
} finally {
    if (Test-Path -LiteralPath $temporaryJsonPath) { [System.IO.File]::Delete($temporaryJsonPath) }
    if (Test-Path -LiteralPath $temporaryMarkdownPath) { [System.IO.File]::Delete($temporaryMarkdownPath) }
}

$retrospective = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRetrospective.ps1') create `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
if (-not $retrospective.valid) {
    if (Test-Path -LiteralPath $completionPath) { [System.IO.File]::Delete($completionPath) }
    if (Test-Path -LiteralPath $completionMarkdownPath) { [System.IO.File]::Delete($completionMarkdownPath) }
    throw "Task retrospective could not be sealed: $(@($retrospective.issues) -join ' ')"
}
$learningObservation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') observe `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
if (-not $learningObservation.valid) {
    if (Test-Path -LiteralPath $completionPath) { [System.IO.File]::Delete($completionPath) }
    if (Test-Path -LiteralPath $completionMarkdownPath) { [System.IO.File]::Delete($completionMarkdownPath) }
    if (Test-Path -LiteralPath (Join-Path $absoluteWorkspacePath 'retrospective.json')) {
        [System.IO.File]::Delete((Join-Path $absoluteWorkspacePath 'retrospective.json'))
    }
    throw "Task learning observations could not be recorded: $(@($learningObservation.issues) -join ' ')"
}
$evalObservation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvalPromotion.ps1') observe `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
if (-not $evalObservation.valid) {
    if (Test-Path -LiteralPath $completionPath) { [System.IO.File]::Delete($completionPath) }
    if (Test-Path -LiteralPath $completionMarkdownPath) { [System.IO.File]::Delete($completionMarkdownPath) }
    if (Test-Path -LiteralPath (Join-Path $absoluteWorkspacePath 'retrospective.json')) {
        [System.IO.File]::Delete((Join-Path $absoluteWorkspacePath 'retrospective.json'))
    }
    throw "Task eval observation could not be recorded: $(@($evalObservation.issues) -join ' ')"
}
$learningHealthObservation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningHealth.ps1') observe `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
if (-not $learningHealthObservation.valid) {
    throw "Task learning-health observations could not be recorded: $(@($learningHealthObservation.issues) -join ' ')"
}
$contextStrategyOutcome = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextOutcome.ps1') observe `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
if (-not $contextStrategyOutcome.valid) {
    throw "Context strategy outcome could not be recorded: $(@($contextStrategyOutcome.issues) -join ' ')"
}
$modelRoutingOutcome = & (Join-Path $PSScriptRoot 'Manage-LlmWikiModelRoutingOutcome.ps1') observe `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
if (-not $modelRoutingOutcome.valid) {
    throw "Model routing outcome could not be recorded: $(@($modelRoutingOutcome.issues) -join ' ')"
}
$instructionOutcome = & (Join-Path $PSScriptRoot 'Manage-LlmWikiInstructionOutcome.ps1') observe `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
if (-not $instructionOutcome.valid) {
    throw "Instruction outcome could not be recorded: $(@($instructionOutcome.issues) -join ' ')"
}
$workspaceOutcome = & (Join-Path $PSScriptRoot 'Write-LlmWikiWorkspaceOutcome.ps1') `
    -WorkspacePath $normalizedWorkspacePath `
    -Completion ([pscustomobject]$completion)

Write-Result ([pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $normalizedWorkspacePath
    valid = $true
    dryRun = $false
    verdict = 'ready'
    score = $status.score
    completionFingerprint = $completion.completionFingerprint
    retrospectiveHash = $retrospective.retrospective.retrospectiveHash
    learningCandidateCount = $retrospective.retrospective.summary.candidateCount
    learningObservationCount = $learningObservation.addedCount
    learningObservationEventHashes = @($learningObservation.observationEventHashes)
    evalObservationCount = [int]$evalObservation.addedCount
    evalObservationEventHash = [string]$evalObservation.eventHash
    learningHealthObservationCount = [int]$learningHealthObservation.addedCount
    learningHealthObservationEventHashes = @($learningHealthObservation.eventHashes)
    contextStrategyOutcomeCount = [int]$contextStrategyOutcome.addedCount
    contextStrategyOutcomeEventHash = [string]$contextStrategyOutcome.eventHash
    modelRoutingOutcomeCount = [int]$modelRoutingOutcome.addedCount
    modelRoutingOutcomeEventHash = [string]$modelRoutingOutcome.eventHash
    instructionOutcomeCount = [int]$instructionOutcome.addedCount
    instructionOutcomeEventHash = [string]$instructionOutcome.eventHash
    workspaceOutcomeFingerprint = [string]$workspaceOutcome.completionFingerprint
    issues = @()
})
