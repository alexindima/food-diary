[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('assess', 'create', 'show', 'verify')]
    [string]$Action = 'assess',
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
$critiquePolicy = $policy.scheduler.changeCritique
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$receiptPath = Join-Path $absoluteWorkspace 'change-critique.json'
$requiredArtifacts = @('workspace.json', 'change-packet.json', 'change-manifest.json', 'acceptance-matrix.json', 'evidence.json')
foreach ($artifact in $requiredArtifacts) {
    if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace $artifact) -PathType Leaf)) {
        throw "Critique input is absent: $workspace/$artifact"
    }
}

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 50 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Value) {
    (Get-FileHash -LiteralPath $Value -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-Payload([object]$Critique) {
    [pscustomobject][ordered]@{
        schemaVersion = $Critique.schemaVersion
        workspace = $Critique.workspace
        createdAtUtc = $Critique.createdAtUtc
        packetFingerprint = $Critique.packetFingerprint
        policyFingerprint = $Critique.policyFingerprint
        inputs = $Critique.inputs
        reviewAreas = @($Critique.reviewAreas)
        findings = @($Critique.findings)
        summary = $Critique.summary
        score = $Critique.score
        verdict = $Critique.verdict
    }
}
function Add-Finding(
    [Collections.Generic.List[object]]$Findings,
    [string]$Id,
    [string]$Area,
    [string]$Severity,
    [string]$Summary,
    [string]$Recommendation,
    [object[]]$Evidence = @()
) {
    $Findings.Add([pscustomobject][ordered]@{
        id = $Id
        area = $Area
        severity = $Severity
        blocking = $Severity -in @($critiquePolicy.blockingSeverities)
        summary = $Summary
        recommendation = $Recommendation
        evidence = @($Evidence | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
    })
}
function New-Critique([string]$CreatedAtUtc) {
    $descriptor = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'workspace.json') -Raw | ConvertFrom-Json
    $packet = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-packet.json') -Raw | ConvertFrom-Json
    $acceptance = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'acceptance-matrix.json') -Raw | ConvertFrom-Json
    $evidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'evidence.json') -Raw | ConvertFrom-Json
    $requirements = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRequirementModel.ps1') assess -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $conformance = & (Join-Path $PSScriptRoot 'Manage-LlmWikiPlanConformance.ps1') assess -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $proof = & (Join-Path $PSScriptRoot 'Manage-LlmWikiProofOfChange.ps1') assess -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $impact = & (Join-Path $PSScriptRoot 'Manage-LlmWikiImpactSimulation.ps1') assess -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $repair = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLoop.ps1') show -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $prediction = & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') assess -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $telemetryValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') verify -Format Json | ConvertFrom-Json
    $telemetry = if ($telemetryValidation.valid) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -Format Json | ConvertFrom-Json
    } else {
        [pscustomobject]@{ valid = $false; registryHash = $telemetryValidation.registryHash; metrics = @(); issues = @($telemetryValidation.issues) }
    }
    $contextSecurityPath = Join-Path $absoluteWorkspace 'context-security.json'
    $contextSecurity = if (Test-Path -LiteralPath $contextSecurityPath -PathType Leaf) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    } else { $null }
    $sensitiveContextRequired = @($packet.privacyImpact.fields).Count -gt 0 -or
        @($packet.privacyImpact.boundaries).Count -gt 0 -or
        @($packet.privacyImpact.externalTransfers).Count -gt 0 -or
        @($packet.policy.matchedRules.id | Where-Object { $_ -in @('security-sensitive', 'privacy-data-lifecycle') }).Count -gt 0 -or
        [bool]$packet.rolloutFlags.externalIntegrations
    $confidencePath = Join-Path $absoluteWorkspace 'confidence-ledger.json'
    $confidence = & (Join-Path $PSScriptRoot 'Manage-LlmWikiConfidenceLedger.ps1') `
        $(if (Test-Path -LiteralPath $confidencePath -PathType Leaf) { 'verify' } else { 'assess' }) `
        -WorkspacePath $workspace `
        -Format Json | ConvertFrom-Json

    $findings = [Collections.Generic.List[object]]::new()
    if (-not $requirements.valid) {
        Add-Finding $findings 'intent-requirements-invalid' 'intent' 'major' 'Requirements contain ambiguity or weak acceptance criteria.' 'Clarify, split, or explicitly resolve every requirement-model finding.' @($requirements.model.findings.id)
    }
    if (-not $conformance.valid) {
        Add-Finding $findings 'scope-plan-drift' 'scope' 'major' 'Observed changes do not conform to the declared implementation plan.' 'Replan the task or bring the changed paths back into the approved scope.' @($conformance.conformance.policyFindings.id)
    }
    if ($proof.applicable -and -not $proof.valid) {
        Add-Finding $findings 'proof-insufficient' 'proof' 'major' 'Satisfied criteria are not backed by current change and verification evidence.' 'Link each satisfied criterion to current changed paths and verified evidence.' @($proof.proof.findings.id)
    }
    $unresolvedChecks = @($evidence.checks | Where-Object status -notin @('passed', 'not-applicable'))
    $unresolvedReviews = @($evidence.reviews | Where-Object status -notin @('completed', 'not-applicable'))
    $unresolvedCriteria = @($acceptance.criteria | Where-Object status -notin @('satisfied', 'not-applicable'))
    if ($unresolvedChecks.Count + $unresolvedReviews.Count + $unresolvedCriteria.Count -gt 0) {
        Add-Finding $findings 'verification-unresolved' 'verification' 'critical' 'Checks, reviews, or acceptance criteria remain unresolved.' 'Resolve every required check, review, and acceptance criterion before completion.' @(@($unresolvedChecks.id) + @($unresolvedReviews.id) + @($unresolvedCriteria.id))
    }
    if (-not $impact.valid) {
        Add-Finding $findings 'architecture-impact-drift' 'architecture' 'major' 'Observed architectural impact exceeds the forecast.' 'Review unexpected impact and replan or narrow the implementation.' @($impact.simulation.findings.id)
    }
    $repairUnresolved = -not $repair.valid -or @($repair.activeAttempts).Count -gt 0 -or @($repair.unresolvedAttempts).Count -gt 0
    if ($repairUnresolved) {
        Add-Finding $findings 'operability-repair-open' 'operability' 'critical' 'Controlled repair history contains active or unresolved attempts.' 'End every repair chain with a distinct, evidence-backed completed attempt.' @(@($repair.activeAttempts.id) + @($repair.unresolvedAttempts.id) + @($repair.issues))
    }
    if (-not $prediction.valid) {
        Add-Finding $findings 'verification-prediction-invalid' 'verification' 'warning' 'Failure prediction could not be validated.' 'Regenerate and validate the failure prediction before relying on verification prioritization.' @($prediction.issues)
    } elseif ([int]$prediction.calibration.falseNegativeCount -gt 0) {
        Add-Finding $findings 'verification-false-negatives' 'verification' 'warning' 'Failure prediction missed one or more observed failures.' 'Review false negatives and promote proven repair knowledge.' @($prediction.calibration.outcomes | Where-Object classification -eq 'false-negative' | Select-Object -ExpandProperty checkId)
    }
    if (-not $telemetry.valid) {
        Add-Finding $findings 'verification-telemetry-invalid' 'verification' 'major' 'Verification telemetry integrity is invalid.' 'Repair or rebuild the verification telemetry registry.' @($telemetry.issues)
    } else {
        $flakyChecks = @($telemetry.metrics | Where-Object { $_.checkId -in @($evidence.checks.id) -and $_.flaky })
        if ($flakyChecks.Count -gt 0) {
            Add-Finding $findings 'verification-flaky-history' 'verification' 'warning' 'Relevant checks have flaky historical transitions.' 'Obtain fresh stable evidence or fix the flaky checks before high-confidence approval.' @($flakyChecks.checkId)
        }
    }
    if ($sensitiveContextRequired -and $null -eq $contextSecurity) {
        Add-Finding $findings 'security-context-unassessed' 'security' 'warning' 'The selected AI context has no trust assessment.' 'Create and review context-security evidence before delegating or sealing sensitive work.'
    } elseif (-not $contextSecurity.valid) {
        Add-Finding $findings 'security-context-invalid' 'security' 'critical' 'AI context security evidence is invalid.' 'Regenerate the trust assessment and resolve integrity or quarantine issues.' @($contextSecurity.issues)
    } elseif ([int]$contextSecurity.assessment.summary.quarantineCount -gt 0) {
        Add-Finding $findings 'security-context-quarantined' 'security' 'warning' 'Potential prompt-injection instructions were quarantined.' 'Review quarantined sources and confirm they are data rather than instructions.' @($contextSecurity.assessment.sources | Where-Object quarantineCount -gt 0 | Select-Object -ExpandProperty path)
    }
    if (-not $confidence.valid) {
        Add-Finding $findings 'operability-confidence-invalid' 'operability' 'major' 'The aggregate confidence ledger is invalid.' 'Regenerate the confidence ledger from current task evidence.' @($confidence.issues)
    }

    $orderedFindings = @($findings | Sort-Object area, severity, id)
    $reviewAreas = foreach ($area in @($critiquePolicy.requiredReviewAreas)) {
        $areaFindings = @($orderedFindings | Where-Object area -eq $area)
        [pscustomobject][ordered]@{
            id = [string]$area
            status = if (@($areaFindings | Where-Object blocking).Count -gt 0) { 'block' } elseif ($areaFindings.Count -gt 0) { 'attention' } else { 'pass' }
            findingIds = @($areaFindings.id)
        }
    }
    $penalty = [double](($orderedFindings | ForEach-Object { [double]$critiquePolicy.severityPenalties.($_.severity) } | Measure-Object -Sum).Sum)
    $score = [Math]::Round([Math]::Max(0, [double]$confidence.ledger.score - $penalty), 2)
    $criticalCount = @($orderedFindings | Where-Object severity -eq 'critical').Count
    $blockingCount = @($orderedFindings | Where-Object blocking).Count
    $warningCount = @($orderedFindings | Where-Object severity -eq 'warning').Count
    $verdict = if ($criticalCount -gt 0) {
        'reject'
    } elseif ($blockingCount -gt 0 -or $score -lt [int]$critiquePolicy.minimumApprovalScore) {
        'request-changes'
    } elseif ($warningCount -gt [int]$critiquePolicy.maximumWarningsForApproval -or $warningCount -gt 0) {
        'approve-with-notes'
    } else { 'approve' }

    $artifactHashes = [ordered]@{}
    foreach ($artifact in $requiredArtifacts) { $artifactHashes[$artifact] = Get-FileSha (Join-Path $absoluteWorkspace $artifact) }
    foreach ($artifact in @('plan-conformance.json', 'requirement-model.json', 'proof-of-change.json', 'impact-simulation.json', 'repair-loop.json', 'failure-prediction.json', 'verification-cost.json', 'context-security.json', 'context-bundle.json', 'confidence-ledger.json')) {
        $path = Join-Path $absoluteWorkspace $artifact
        if (Test-Path -LiteralPath $path -PathType Leaf) { $artifactHashes[$artifact] = Get-FileSha $path }
    }
    $critique = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $workspace
        createdAtUtc = $CreatedAtUtc
        packetFingerprint = [string]$packet.fingerprint
        policyFingerprint = Get-FileSha $policyPath
        inputs = [pscustomobject][ordered]@{
            artifactHashes = [pscustomobject]$artifactHashes
            telemetryRegistryHash = [string]$telemetry.registryHash
            descriptorPacketFingerprint = [string]$descriptor.currentPacketFingerprint
            confidenceLedgerHash = $(if (Test-Path -LiteralPath $confidencePath -PathType Leaf) { [string]$confidence.ledger.ledgerHash } else { '' })
        }
        reviewAreas = @($reviewAreas)
        findings = @($orderedFindings)
        summary = [pscustomobject][ordered]@{
            findingCount = $orderedFindings.Count
            criticalCount = $criticalCount
            blockingCount = $blockingCount
            warningCount = $warningCount
            penalty = $penalty
            baseConfidenceScore = [double]$confidence.ledger.score
        }
        score = $score
        verdict = $verdict
        critiqueHash = ''
    }
    $critique.critiqueHash = Get-Hash (Get-Payload $critique)
    $critique
}
function Test-Critique([object]$Critique) {
    $issues = [Collections.Generic.List[string]]::new()
    $packet = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-packet.json') -Raw | ConvertFrom-Json
    if ($Critique.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Critique.workspace -cne $workspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Critique.packetFingerprint -cne [string]$packet.fingerprint) { $issues.Add('Task packet drifted.') }
    if ([string]$Critique.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Critique policy drifted.') }
    foreach ($property in @($Critique.inputs.artifactHashes.PSObject.Properties)) {
        $path = Join-Path $absoluteWorkspace $property.Name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or [string]$property.Value -cne (Get-FileSha $path)) {
            $issues.Add("Critique input drifted: $($property.Name).")
        }
    }
    $expected = New-Critique ([string]$Critique.createdAtUtc)
    if ((Get-Hash @($Critique.reviewAreas)) -cne (Get-Hash @($expected.reviewAreas))) { $issues.Add('Critique review areas drifted.') }
    if ((Get-Hash @($Critique.findings)) -cne (Get-Hash @($expected.findings))) { $issues.Add('Critique findings drifted.') }
    if ((Get-Hash $Critique.summary) -cne (Get-Hash $expected.summary) -or [double]$Critique.score -ne [double]$expected.score -or [string]$Critique.verdict -cne [string]$expected.verdict) {
        $issues.Add('Critique score or verdict arithmetic is invalid.')
    }
    if ([string]$Critique.inputs.telemetryRegistryHash -cne [string]$expected.inputs.telemetryRegistryHash) { $issues.Add('Critique telemetry input drifted.') }
    if ([string]$Critique.inputs.confidenceLedgerHash -cne [string]$expected.inputs.confidenceLedgerHash) { $issues.Add('Critique confidence input drifted.') }
    if ([string]$Critique.critiqueHash -cne (Get-Hash (Get-Payload $Critique))) { $issues.Add('Critique hash is invalid.') }
    @($issues)
}

$critique = $null
$issues = @()
$savedPath = $null
if ($Action -in @('show', 'verify')) {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        $issues = @('change-critique.json is absent.')
    } else {
        try {
            $critique = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
            $issues = @(Test-Critique $critique)
        } catch { $issues = @($_.Exception.Message) }
    }
} else {
    $critique = New-Critique ($AsOfUtc.ToUniversalTime().ToString('o'))
    $issues = @(Test-Critique $critique)
    if ($Action -eq 'create' -and $issues.Count -eq 0) {
        [IO.File]::WriteAllText($receiptPath, (($critique | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        $savedPath = "$workspace/change-critique.json"
    }
}
$result = [pscustomobject][ordered]@{
    action = $Action
    valid = $issues.Count -eq 0
    critique = $critique
    issues = @($issues)
    savedPath = $savedPath
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 50
} else {
    Write-Host "Change critique: action=$Action, valid=$($result.valid)"
    if ($null -ne $critique) {
        Write-Host "Score=$($critique.score)/100, verdict=$($critique.verdict), findings=$($critique.summary.findingCount), hash=$($critique.critiqueHash)"
        foreach ($finding in @($critique.findings)) {
            Write-Host " - [$($finding.severity)] $($finding.area)/$($finding.id): $($finding.summary)"
        }
    }
    foreach ($issue in @($issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
