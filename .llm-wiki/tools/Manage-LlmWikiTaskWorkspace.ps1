[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('status', 'refresh')]
    [string]$Action = 'status',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [object]$PacketInput,
    [switch]$DryRun,
    [switch]$FailOnBlocked,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) {
    throw 'WorkspacePath must be repository-relative.'
}
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+(?:/.*)?$') {
    throw 'WorkspacePath must be inside .artifacts/llm-wiki/tasks/<task-name>.'
}
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
if (-not (Test-Path -LiteralPath $absoluteWorkspacePath -PathType Container)) {
    throw "Task workspace does not exist: $normalizedWorkspacePath"
}

$artifactNames = [ordered]@{
    descriptor = 'workspace.json'
    packet = 'change-packet.json'
    taskContract = 'task-contract.json'
    manifest = 'change-manifest.json'
    acceptance = 'acceptance-matrix.json'
    evidence = 'evidence.json'
    journal = 'journal.json'
    report = 'review-report.md'
}
foreach ($artifact in $artifactNames.GetEnumerator()) {
    $artifactPath = Join-Path $absoluteWorkspacePath $artifact.Value
    if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
        throw "Task workspace is incomplete; missing $($artifact.Value)."
    }
}

$descriptorPath = Join-Path $absoluteWorkspacePath $artifactNames.descriptor
$descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
$taskContract = Get-Content -LiteralPath (Join-Path $absoluteWorkspacePath $artifactNames.taskContract) -Raw | ConvertFrom-Json
$acceptance = Get-Content -LiteralPath (Join-Path $absoluteWorkspacePath $artifactNames.acceptance) -Raw | ConvertFrom-Json
$evidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspacePath $artifactNames.evidence) -Raw | ConvertFrom-Json
$journalView = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') show `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json

function Test-PathMatch([string]$Value, [object[]]$Patterns) {
    foreach ($pattern in @($Patterns)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$pattern) -and $Value -match $pattern) { return $true }
    }
    return $false
}

$packet = if ($null -ne $PacketInput) {
    $PacketInput
} else {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiChangePacket.ps1') `
        -BaseRef $taskContract.git.base `
        -Objective $descriptor.objective `
        -Format Json | ConvertFrom-Json
}

$manifestRelative = "$normalizedWorkspacePath/$($artifactNames.manifest)"
$acceptanceRelative = "$normalizedWorkspacePath/$($artifactNames.acceptance)"
$evidenceRelative = "$normalizedWorkspacePath/$($artifactNames.evidence)"
$readiness = & (Join-Path $PSScriptRoot 'Get-LlmWikiReleaseReadiness.ps1') `
    -PacketInput $packet `
    -ManifestPath $manifestRelative `
    -AcceptancePath $acceptanceRelative `
    -EvidencePath $evidenceRelative `
    -RequireManifest `
    -RequireAcceptance `
    -RequireEvidence `
    -Format Json | ConvertFrom-Json

$outOfScope = @($packet.diff.changedPaths | Where-Object {
    -not (Test-PathMatch $_ @($taskContract.scope.allowedPathPatterns)) -or
    (Test-PathMatch $_ @($taskContract.scope.excludedPathPatterns))
})
$pendingCriteria = @($acceptance.criteria | Where-Object status -eq 'pending')
$rejectedCriteria = @($acceptance.criteria | Where-Object status -eq 'rejected')
$unresolvedChecks = @($evidence.checks | Where-Object status -notin @('passed', 'not-applicable'))
$unresolvedReviews = @($evidence.reviews | Where-Object status -notin @('completed', 'not-applicable'))
$initialFingerprint = if ($null -ne $descriptor.initialPacketFingerprint) {
    [string]$descriptor.initialPacketFingerprint
} else {
    [string]$descriptor.packetFingerprint
}
$fingerprintChanged = $initialFingerprint -ne $packet.fingerprint
$lastCompiledFingerprint = if (-not [string]::IsNullOrWhiteSpace([string]$descriptor.currentPacketFingerprint)) {
    [string]$descriptor.currentPacketFingerprint
} else {
    $initialFingerprint
}
$refreshRequired = $lastCompiledFingerprint -cne [string]$packet.fingerprint
$planConformance = & (Join-Path $PSScriptRoot 'Manage-LlmWikiPlanConformance.ps1') assess `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$proofOfChange = & (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') assess `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$requirementModel = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRequirementModel.ps1') assess `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$impactSimulation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiImpactSimulation.ps1') assess `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$repairLoop = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLoop.ps1') show `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$repairLearningCandidates = if (Test-Path -LiteralPath (Join-Path $absoluteWorkspacePath 'repair-loop.json') -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLearning.ps1') candidates `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else {
    [pscustomobject]@{ valid = $true; eligibleCount = 0; candidates = @() }
}
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
$contextSecurityPath = Join-Path $absoluteWorkspacePath 'context-security.json'
$contextSecurity = if (Test-Path -LiteralPath $contextSecurityPath -PathType Leaf) {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') verify -WorkspacePath $normalizedWorkspacePath -Format Json | ConvertFrom-Json
} else { $null }
$relevantTelemetry = @($verificationTelemetry.metrics | Where-Object checkId -in @($evidence.checks.id))
$confidenceLedger = & (Join-Path $PSScriptRoot 'Manage-LlmWikiConfidenceLedger.ps1') assess `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json
$changeCritique = & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeCritique.ps1') assess `
    -WorkspacePath $normalizedWorkspacePath `
    -Format Json | ConvertFrom-Json

$nextActions = [System.Collections.Generic.List[string]]::new()
if ($refreshRequired) {
    $nextActions.Add("./.llm-wiki/wiki.ps1 task-refresh -WorkspacePath $normalizedWorkspacePath -DryRun")
}
foreach ($path in $outOfScope) {
    $nextActions.Add("Bring '$path' back into scope or intentionally update task-contract.json.")
}
foreach ($path in @($planConformance.conformance.classification.unplannedAllowedPaths)) {
    $nextActions.Add("Replan manifest scope for unplanned changed path '$path' or revert it.")
}
foreach ($path in @($planConformance.conformance.classification.missingPlannedPaths)) {
    $nextActions.Add("Implement planned path '$path' or intentionally replan the manifest.")
}
foreach ($criterion in $pendingCriteria) {
    $nextActions.Add("Map and resolve acceptance criterion $($criterion.id): $($criterion.text)")
}
foreach ($criterion in $rejectedCriteria) {
    $nextActions.Add("Resolve rejected acceptance criterion $($criterion.id): $($criterion.text)")
}
if ($proofOfChange.applicable -and -not $proofOfChange.valid) {
    foreach ($finding in @($proofOfChange.proof.findings)) {
        $nextActions.Add("Resolve proof-of-change finding '$($finding.id)' for $($finding.criterionId).")
    }
}
foreach ($finding in @($requirementModel.model.findings)) {
    $nextActions.Add("Refine acceptance criterion $($finding.criterionId) to resolve '$($finding.id)'.")
}
if (@($requirementModel.model.recommendations).Count -gt 0) {
    $nextActions.Add("./.llm-wiki/wiki.ps1 task-requirements-expand -WorkspacePath $normalizedWorkspacePath -Reason <rationale>")
}
foreach ($finding in @($impactSimulation.simulation.findings)) {
    $nextActions.Add("Replan or review forecast drift '$($finding.id)' ($($finding.count) unexpected impact(s)).")
}
foreach ($check in $unresolvedChecks) {
    $nextActions.Add("Record check '$($check.id)' as passed, failed, or not-applicable.")
    if ($check.status -eq 'failed') {
        $nextActions.Add("./.llm-wiki/wiki.ps1 task-repair-suggest -WorkspacePath $normalizedWorkspacePath -CheckId $($check.id)")
    }
}
foreach ($attempt in @($repairLoop.activeAttempts)) {
    $nextActions.Add("Complete or fail active repair attempt '$($attempt.id)' for check '$($attempt.checkId)'.")
}
foreach ($attempt in @($repairLoop.unresolvedAttempts | Where-Object state -eq 'failed')) {
    $nextActions.Add("Start a distinct repair attempt for unresolved check '$($attempt.checkId)'; do not repeat fingerprint '$($attempt.attemptFingerprint)'.")
}
foreach ($candidate in @($repairLearningCandidates.candidates | Where-Object eligible)) {
    $nextActions.Add("./.llm-wiki/wiki.ps1 repair-learning-promote -WorkspacePath $normalizedWorkspacePath -RepairCandidateId $($candidate.id) -Owner <owner>")
}
foreach ($outcome in @($failurePrediction.calibration.outcomes | Where-Object classification -eq 'false-negative')) {
    $nextActions.Add("Review false-negative failure prediction for '$($outcome.checkId)' and promote proven repair learning when available.")
}
foreach ($metric in @($relevantTelemetry | Where-Object flaky)) {
    $nextActions.Add("Investigate flaky verification '$($metric.checkId)' ($($metric.transitionPercent)% outcome transitions across $($metric.sampleCount) samples).")
}
if ($null -ne $contextSecurity -and [int]$contextSecurity.assessment.summary.quarantineCount -gt 0) {
    $nextActions.Add("Review $($contextSecurity.assessment.summary.quarantineCount) quarantined context instruction match(es) before trusting the affected sources.")
}
foreach ($finding in @($changeCritique.critique.findings | Where-Object blocking)) {
    $nextActions.Add("Resolve critique finding '$($finding.id)' in $($finding.area): $($finding.recommendation)")
}
foreach ($review in $unresolvedReviews) {
    $nextActions.Add("Complete or explicitly waive review '$($review.id)'.")
}
foreach ($blocker in @($journalView.entries | Where-Object { $_.type -eq 'blocker' -and $_.status -eq 'open' })) {
    $nextActions.Add("Resolve task journal blocker '$($blocker.id)': $($blocker.text)")
}
foreach ($dimension in @($readiness.dimensions | Where-Object status -eq 'fail')) {
    if (-not (@($nextActions) -match [regex]::Escape([string]$dimension.id))) {
        $nextActions.Add("Resolve blocking readiness dimension '$($dimension.id)': $($dimension.summary)")
    }
}
$proofBlocks = $proofOfChange.applicable -and -not $proofOfChange.valid
$repairBlocks = -not $repairLoop.valid -or @($repairLoop.unresolvedAttempts).Count -gt 0
$critiqueBlocks = -not $changeCritique.valid -or $changeCritique.critique.verdict -in @('reject', 'request-changes')
$effectiveVerdict = if ($journalView.openBlockerCount -gt 0 -or -not $planConformance.valid -or $proofBlocks -or -not $requirementModel.valid -or -not $impactSimulation.valid -or $repairBlocks -or -not $failurePrediction.valid -or -not $verificationCost.valid -or -not $verificationTelemetry.valid -or ($null -ne $contextSecurity -and -not $contextSecurity.valid) -or $critiqueBlocks) { 'blocked' } else { $readiness.verdict }
if ($nextActions.Count -eq 0 -and $effectiveVerdict -eq 'ready') {
    $nextActions.Add('No governance action remains; the workspace is release-ready.')
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $normalizedWorkspacePath
    objective = $descriptor.objective
    verdict = $effectiveVerdict
    score = $readiness.score
    risk = $readiness.risk
    initialPacketFingerprint = $initialFingerprint
    currentPacketFingerprint = $packet.fingerprint
    fingerprintChanged = $fingerprintChanged
    refreshRequired = $refreshRequired
    lastCompiledPacketFingerprint = $lastCompiledFingerprint
    changedPathCount = @($packet.diff.changedPaths).Count
    outOfScopePaths = @($outOfScope)
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
        metrics = $relevantTelemetry
    }
    contextSecurity = $contextSecurity
    confidenceLedger = $confidenceLedger
    changeCritique = $changeCritique
    pendingCriteria = @($pendingCriteria.id)
    rejectedCriteria = @($rejectedCriteria.id)
    unresolvedChecks = @($unresolvedChecks.id)
    unresolvedReviews = @($unresolvedReviews.id)
    blockingDimensions = @($readiness.blockingDimensions) +
        $(if ($journalView.openBlockerCount -gt 0) { @('task-journal') } else { @() }) +
        $(if (-not $planConformance.valid) { @('plan-conformance') } else { @() }) +
        $(if ($proofBlocks) { @('proof-of-change') } else { @() }) +
        $(if (-not $requirementModel.valid) { @('requirement-model') } else { @() }) +
        $(if (-not $impactSimulation.valid) { @('impact-simulation') } else { @() }) +
        $(if ($repairBlocks) { @('repair-loop') } else { @() }) +
        $(if (-not $failurePrediction.valid) { @('failure-prediction') } else { @() }) +
        $(if (-not $verificationCost.valid) { @('verification-cost') } else { @() }) +
        $(if (-not $verificationTelemetry.valid) { @('verification-telemetry') } else { @() }) +
        $(if ($null -ne $contextSecurity -and -not $contextSecurity.valid) { @('context-security') } else { @() }) +
        $(if ($critiqueBlocks) { @('change-critique') } else { @() })
    unassessedDimensions = @($readiness.unassessedDimensions)
    journalEntryCount = $journalView.entryCount
    openJournalEntries = $journalView.openCount
    openJournalBlockers = $journalView.openBlockerCount
    nextActions = @($nextActions)
    invalidation = $null
}

if ($Action -eq 'refresh') {
    $refreshId = [guid]::NewGuid().ToString('N')
    $temporaryPacketName = ".refresh-$refreshId-packet.json"
    $temporaryReportName = ".refresh-$refreshId-report.md"
    $temporaryPacketRelative = "$normalizedWorkspacePath/$temporaryPacketName"
    $temporaryReportRelative = "$normalizedWorkspacePath/$temporaryReportName"
    $temporaryPacketAbsolute = Join-Path $absoluteWorkspacePath $temporaryPacketName
    $temporaryReportAbsolute = Join-Path $absoluteWorkspacePath $temporaryReportName
    $packetArtifactPath = Join-Path $absoluteWorkspacePath $artifactNames.packet
    $reportArtifactPath = Join-Path $absoluteWorkspacePath $artifactNames.report
    $evidenceArtifactPath = Join-Path $absoluteWorkspacePath $artifactNames.evidence
    $acceptanceArtifactPath = Join-Path $absoluteWorkspacePath $artifactNames.acceptance
    $originalPacket = Get-Content -LiteralPath $packetArtifactPath -Raw
    $originalReport = Get-Content -LiteralPath $reportArtifactPath -Raw
    $originalDescriptor = Get-Content -LiteralPath $descriptorPath -Raw
    $originalEvidence = Get-Content -LiteralPath $evidenceArtifactPath -Raw
    $originalAcceptance = Get-Content -LiteralPath $acceptanceArtifactPath -Raw
    $derivedArtifactNames = @('risk-calibration.json', 'failure-prediction.json', 'verification-cost.json', 'verification-plan.json', 'model-routing.json', 'model-routing-outcome.json', 'instruction-outcome.json', 'plan-reuse.json', 'context-security.json', 'context-bundle.json', 'context-budget.json', 'context-benchmark.json', 'context-experiment.json', 'context-strategy-approval.json', 'context-strategy-application.json', 'context-strategy-outcome.json', 'confidence-ledger.json', 'change-critique.json')
    $originalDerivedArtifacts = @{}
    foreach ($derivedArtifactName in $derivedArtifactNames) {
        $derivedArtifactPath = Join-Path $absoluteWorkspacePath $derivedArtifactName
        if (Test-Path -LiteralPath $derivedArtifactPath -PathType Leaf) {
            $originalDerivedArtifacts[$derivedArtifactName] = Get-Content -LiteralPath $derivedArtifactPath -Raw
        }
    }
    try {
        $packet | ConvertTo-Json -Depth 15 | ForEach-Object {
            [System.IO.File]::WriteAllText($temporaryPacketAbsolute, $_ + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
        }
        $invalidation = & (Join-Path $PSScriptRoot 'Update-LlmWikiTaskEvidence.ps1') `
            -WorkspacePath $normalizedWorkspacePath `
            -PacketPath $temporaryPacketRelative `
            -Apply:(-not $DryRun) `
            -Format Json | ConvertFrom-Json
        $result.invalidation = $invalidation
        if ($DryRun) {
            if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 10 } else {
                Write-Host "Task refresh preview: $(@($invalidation.invalidatedChecks).Count) checks, $(@($invalidation.invalidatedReviews).Count) reviews, $(@($invalidation.invalidatedCriteria).Count) criteria would be invalidated."
            }
            return
        }
        & (Join-Path $PSScriptRoot 'Get-LlmWikiReviewReport.ps1') `
            -PacketInput $packet `
            -ManifestPath $manifestRelative `
            -AcceptancePath $acceptanceRelative `
            -EvidencePath $evidenceRelative `
            -OutputPath $temporaryReportRelative | Out-Null
        [System.IO.File]::Copy($temporaryPacketAbsolute, $packetArtifactPath, $true)
        [System.IO.File]::Copy($temporaryReportAbsolute, $reportArtifactPath, $true)
        if ($null -eq $descriptor.initialPacketFingerprint) {
            $descriptor | Add-Member -NotePropertyName initialPacketFingerprint -NotePropertyValue $initialFingerprint
        }
        if ($null -eq $descriptor.currentPacketFingerprint) {
            $descriptor | Add-Member -NotePropertyName currentPacketFingerprint -NotePropertyValue $packet.fingerprint
        } else {
            $descriptor.currentPacketFingerprint = $packet.fingerprint
        }
        if ($null -eq $descriptor.lastRefreshedAtUtc) {
            $descriptor | Add-Member -NotePropertyName lastRefreshedAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o'))
        } else {
            $descriptor.lastRefreshedAtUtc = [DateTime]::UtcNow.ToString('o')
        }
        [System.IO.File]::WriteAllText(
            $descriptorPath,
            (($descriptor | ConvertTo-Json -Depth 20) + [Environment]::NewLine),
            [System.Text.UTF8Encoding]::new($false))
        if ([string]$packet.fingerprint -cne $lastCompiledFingerprint) {
            foreach ($derivedArtifactName in $derivedArtifactNames) {
                $derivedArtifactPath = Join-Path $absoluteWorkspacePath $derivedArtifactName
                if (Test-Path -LiteralPath $derivedArtifactPath -PathType Leaf) {
                    [System.IO.File]::Delete($derivedArtifactPath)
                }
            }
        }
        $result.refreshRequired = $false
        $result.lastCompiledPacketFingerprint = [string]$packet.fingerprint
        $result.nextActions = @($result.nextActions | Where-Object { $_ -cne "./.llm-wiki/wiki.ps1 task-refresh -WorkspacePath $normalizedWorkspacePath -DryRun" })
    } catch {
        [System.IO.File]::WriteAllText($packetArtifactPath, $originalPacket, [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($reportArtifactPath, $originalReport, [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($descriptorPath, $originalDescriptor, [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($evidenceArtifactPath, $originalEvidence, [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($acceptanceArtifactPath, $originalAcceptance, [System.Text.UTF8Encoding]::new($false))
        foreach ($derivedArtifactName in $derivedArtifactNames) {
            $derivedArtifactPath = Join-Path $absoluteWorkspacePath $derivedArtifactName
            if ($originalDerivedArtifacts.ContainsKey($derivedArtifactName)) {
                [System.IO.File]::WriteAllText($derivedArtifactPath, $originalDerivedArtifacts[$derivedArtifactName], [System.Text.UTF8Encoding]::new($false))
            } elseif (Test-Path -LiteralPath $derivedArtifactPath -PathType Leaf) {
                [System.IO.File]::Delete($derivedArtifactPath)
            }
        }
        throw
    } finally {
        if (Test-Path -LiteralPath $temporaryPacketAbsolute) { [System.IO.File]::Delete($temporaryPacketAbsolute) }
        if (Test-Path -LiteralPath $temporaryReportAbsolute) { [System.IO.File]::Delete($temporaryReportAbsolute) }
    }
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
} else {
    Write-Host "Task workspace: $($result.workspace)"
    Write-Host "Readiness: $($result.verdict) ($($result.score)/100), risk=$($result.risk.level)"
    Write-Host "Change: $($result.changedPathCount) path(s), since start=$($result.fingerprintChanged), refresh required=$($result.refreshRequired)"
    Write-Host "Open: $(@($result.pendingCriteria).Count) acceptance, $(@($result.unresolvedChecks).Count) checks, $(@($result.unresolvedReviews).Count) reviews, $($result.openJournalBlockers) journal blockers, $(@($result.outOfScopePaths).Count) out of scope"
    if ($null -ne $result.invalidation) {
        Write-Host "Invalidation: $(@($result.invalidation.invalidatedChecks).Count) checks, $(@($result.invalidation.invalidatedReviews).Count) reviews, $(@($result.invalidation.invalidatedCriteria).Count) criteria; applied=$($result.invalidation.applied)"
    }
    Write-Host 'Next actions:'
    foreach ($nextAction in $result.nextActions) { Write-Host " - $nextAction" }
    if ($Action -eq 'refresh' -and -not $DryRun) { Write-Host 'Refreshed change packet, evidence, acceptance, review report, and workspace metadata.' }
}

if ($FailOnBlocked -and $result.verdict -eq 'blocked') { exit 1 }
