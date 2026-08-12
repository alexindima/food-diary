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
$confidencePolicy = $policy.scheduler.confidenceLedger
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$receiptPath = Join-Path $absoluteWorkspace 'confidence-ledger.json'
$requiredArtifacts = @('workspace.json', 'change-packet.json', 'change-manifest.json', 'acceptance-matrix.json', 'evidence.json')
foreach ($artifact in $requiredArtifacts) {
    if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace $artifact) -PathType Leaf)) { throw "Confidence input is absent: $workspace/$artifact" }
}

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Value) {
    (Get-FileHash -LiteralPath $Value -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Add-Dimension([Collections.Generic.List[object]]$List, [string]$Id, [string]$Status, [string]$Summary, [object[]]$Evidence = @()) {
    $weight = [int]$confidencePolicy.dimensions.$Id
    $multiplier = [double]$confidencePolicy.statusMultipliers.$Status
    $List.Add([pscustomobject][ordered]@{
        id = $Id; weight = $weight; status = $Status; earned = [Math]::Round($weight * $multiplier, 2)
        summary = $Summary; evidence = @($Evidence)
    })
}
function Get-Payload([object]$Ledger) {
    [pscustomobject][ordered]@{
        schemaVersion = $Ledger.schemaVersion; workspace = $Ledger.workspace; createdAtUtc = $Ledger.createdAtUtc
        packetFingerprint = $Ledger.packetFingerprint; policyFingerprint = $Ledger.policyFingerprint
        inputs = $Ledger.inputs; dimensions = @($Ledger.dimensions); hardCaps = @($Ledger.hardCaps)
        uncappedScore = $Ledger.uncappedScore; appliedCap = $Ledger.appliedCap; score = $Ledger.score
        level = $Ledger.level; verdict = $Ledger.verdict
    }
}
function Get-Level([double]$Score) {
    if ($Score -ge [int]$confidencePolicy.levels.highAt) { return 'high' }
    if ($Score -ge [int]$confidencePolicy.levels.substantialAt) { return 'substantial' }
    if ($Score -ge [int]$confidencePolicy.levels.guardedAt) { return 'guarded' }
    'low'
}
function Get-Ids([object[]]$Items) {
    @($Items | ForEach-Object { if ($null -ne $_ -and $_.PSObject.Properties['id']) { [string]$_.id } } | Where-Object { $_ })
}
function New-Ledger {
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
    } else { [pscustomobject]@{ valid = $false; registryHash = $telemetryValidation.registryHash; metrics = @(); issues = @($telemetryValidation.issues) } }
    $contextSecurityPath = Join-Path $absoluteWorkspace 'context-security.json'
    $contextSecurity = if (Test-Path -LiteralPath $contextSecurityPath -PathType Leaf) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextSecurity.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    } else { $null }

    $dimensions = [Collections.Generic.List[object]]::new()
    Add-Dimension $dimensions 'requirements' $(if ($requirements.valid) { 'pass' } else { 'fail' }) `
        $(if ($requirements.valid) { 'Acceptance criteria are structurally actionable.' } else { 'Requirement model contains blocking ambiguity.' }) `
        @(Get-Ids @($requirements.model.findings))
    Add-Dimension $dimensions 'planConformance' $(if ($conformance.valid) { 'pass' } else { 'fail' }) `
        $(if ($conformance.valid) { 'Observed changes conform to the implementation plan.' } else { 'Observed changes drift from the implementation plan.' }) `
        @(Get-Ids @($conformance.conformance.policyFindings))
    $proofStatus = if (-not $proof.applicable) { 'pass' } elseif ($proof.valid) { 'pass' } else { 'fail' }
    Add-Dimension $dimensions 'proofOfChange' $proofStatus `
        $(if (-not $proof.applicable) { 'No proof-bearing criteria are applicable.' } elseif ($proof.valid) { 'Proof-bearing criteria have valid evidence.' } else { 'Proof-of-change findings remain.' }) `
        @(Get-Ids @($proof.proof.findings))
    $unresolvedChecks = @($evidence.checks | Where-Object status -notin @('passed', 'not-applicable'))
    $unresolvedReviews = @($evidence.reviews | Where-Object status -notin @('completed', 'not-applicable'))
    $unresolvedCriteria = @($acceptance.criteria | Where-Object status -notin @('satisfied', 'not-applicable'))
    $evidenceResolved = $unresolvedChecks.Count -eq 0 -and $unresolvedReviews.Count -eq 0 -and $unresolvedCriteria.Count -eq 0
    Add-Dimension $dimensions 'evidence' $(if ($evidenceResolved) { 'pass' } else { 'fail' }) `
        $(if ($evidenceResolved) { 'Checks, reviews, and acceptance criteria are resolved.' } else { 'Evidence or acceptance remains unresolved.' }) `
        @((Get-Ids $unresolvedChecks) + (Get-Ids $unresolvedReviews) + (Get-Ids $unresolvedCriteria))
    Add-Dimension $dimensions 'impactSimulation' $(if ($impact.valid) { 'pass' } else { 'fail' }) `
        $(if ($impact.valid) { 'Observed impact stays within the forecast.' } else { 'Observed impact drifted from the forecast.' }) `
        @(Get-Ids @($impact.simulation.findings))
    $repairResolved = $repair.valid -and @($repair.unresolvedAttempts).Count -eq 0 -and @($repair.activeAttempts).Count -eq 0
    Add-Dimension $dimensions 'repairLoop' $(if ($repairResolved) { 'pass' } else { 'fail' }) `
        $(if ($repairResolved) { 'No unresolved controlled repair remains.' } else { 'Controlled repair attempts remain unresolved or invalid.' }) `
        @((Get-Ids @($repair.unresolvedAttempts)) + (Get-Ids @($repair.activeAttempts)) + @($repair.issues))
    $predictionStatus = if (-not $prediction.valid) { 'fail' } elseif ([int]$prediction.calibration.falseNegativeCount -gt 0) { 'warning' } else { 'pass' }
    Add-Dimension $dimensions 'failurePrediction' $predictionStatus `
        $(if (-not $prediction.valid) { 'Failure prediction is invalid.' } elseif ([int]$prediction.calibration.falseNegativeCount -gt 0) { 'Failure prediction contains false negatives.' } else { 'Failure prediction has no observed false negatives.' }) `
        @($prediction.calibration.outcomes | Where-Object { $_.PSObject.Properties['classification'] -and $_.classification -eq 'false-negative' -and $_.PSObject.Properties['checkId'] } | ForEach-Object { $_.checkId })
    $evidenceCheckIds = @(Get-Ids @($evidence.checks))
    $relevantTelemetry = @($telemetry.metrics | Where-Object {
        $_.PSObject.Properties['checkId'] -and [string]$_.checkId -in $evidenceCheckIds
    })
    $flaky = @($relevantTelemetry | Where-Object flaky)
    $telemetryStatus = if (-not $telemetry.valid) { 'fail' } elseif ($flaky.Count -gt 0) { 'warning' } elseif ($relevantTelemetry.Count -eq 0) { 'not-assessed' } else { 'pass' }
    Add-Dimension $dimensions 'verificationTelemetry' $telemetryStatus `
        $(if (-not $telemetry.valid) { 'Verification telemetry is invalid.' } elseif ($flaky.Count -gt 0) { 'Relevant checks exhibit flaky transitions.' } elseif ($relevantTelemetry.Count -eq 0) { 'No historical verification samples are available.' } else { 'Relevant verification history has no flaky signal.' }) `
        @($flaky | ForEach-Object { if ($_.PSObject.Properties['checkId']) { $_.checkId } })
    $contextStatus = if ($null -eq $contextSecurity) { 'not-assessed' } elseif (-not $contextSecurity.valid) { 'fail' } elseif ([int]$contextSecurity.assessment.summary.quarantineCount -gt 0) { 'warning' } else { 'pass' }
    Add-Dimension $dimensions 'contextSecurity' $contextStatus `
        $(if ($null -eq $contextSecurity) { 'No context security assessment exists.' } elseif (-not $contextSecurity.valid) { 'Context security assessment is invalid.' } elseif ([int]$contextSecurity.assessment.summary.quarantineCount -gt 0) { 'Context instructions were quarantined and require review.' } else { 'Selected context has no quarantined instruction matches.' }) `
        @($(if ($null -ne $contextSecurity) { @($contextSecurity.assessment.sources | Where-Object quarantineCount -gt 0 | Select-Object -ExpandProperty path) } else { @() }))

    $caps = [Collections.Generic.List[object]]::new()
    function Add-Cap([string]$Id, [bool]$Applies, [string]$Reason) {
        if ($Applies) { $caps.Add([pscustomobject][ordered]@{ id = $Id; maximumScore = [int]$confidencePolicy.hardCaps.$Id; reason = $Reason }) }
    }
    Add-Cap 'invalidContextSecurity' ($null -ne $contextSecurity -and -not $contextSecurity.valid) 'Invalid context trust evidence prevents normal confidence.'
    Add-Cap 'unresolvedEvidence' (-not $evidenceResolved) 'Unresolved evidence prevents majority confidence.'
    Add-Cap 'unresolvedRepair' (-not $repairResolved) 'Unresolved repair prevents majority confidence.'
    Add-Cap 'invalidRequirements' (-not $requirements.valid) 'Ambiguous requirements cap confidence.'
    Add-Cap 'invalidPlanConformance' (-not $conformance.valid) 'Plan drift caps confidence.'
    Add-Cap 'invalidProofOfChange' ($proof.applicable -and -not $proof.valid) 'Missing proof caps confidence.'
    Add-Cap 'invalidImpactSimulation' (-not $impact.valid) 'Unexpected impact caps confidence.'

    $artifactHashes = [ordered]@{}
    foreach ($artifact in $requiredArtifacts) { $artifactHashes[$artifact] = Get-FileSha (Join-Path $absoluteWorkspace $artifact) }
    foreach ($artifact in @('plan-conformance.json', 'requirement-model.json', 'proof-of-change.json', 'impact-simulation.json', 'repair-loop.json', 'failure-prediction.json', 'verification-cost.json', 'context-security.json', 'context-bundle.json')) {
        $path = Join-Path $absoluteWorkspace $artifact
        if (Test-Path -LiteralPath $path -PathType Leaf) { $artifactHashes[$artifact] = Get-FileSha $path }
    }
    $uncapped = [Math]::Round([double](($dimensions.earned | Measure-Object -Sum).Sum), 2)
    $appliedCap = if ($caps.Count -eq 0) { 100 } else { [int](($caps.maximumScore | Measure-Object -Minimum).Minimum) }
    $score = [Math]::Min($uncapped, $appliedCap)
    $failCount = @($dimensions | Where-Object status -eq 'fail').Count
    $uncertainCount = @($dimensions | Where-Object status -in @('warning', 'not-assessed')).Count
    $ledger = [pscustomobject][ordered]@{
        schemaVersion = 1; workspace = $workspace; createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        packetFingerprint = [string]$packet.fingerprint; policyFingerprint = Get-FileSha $policyPath
        inputs = [pscustomobject][ordered]@{
            artifactHashes = [pscustomobject]$artifactHashes
            telemetryRegistryHash = [string]$telemetry.registryHash
            descriptorPacketFingerprint = [string]$descriptor.currentPacketFingerprint
        }
        dimensions = @($dimensions); hardCaps = @($caps); uncappedScore = $uncapped; appliedCap = $appliedCap
        score = $score; level = Get-Level $score
        verdict = $(if ($failCount -gt 0) { 'blocked' } elseif ($uncertainCount -gt 0) { 'conditional' } else { 'trusted' })
        ledgerHash = ''
    }
    $ledger.ledgerHash = Get-Hash (Get-Payload $ledger)
    $ledger
}
function Test-Ledger([object]$Ledger) {
    $issues = [Collections.Generic.List[string]]::new()
    $packet = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-packet.json') -Raw | ConvertFrom-Json
    if ($Ledger.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Ledger.workspace -cne $workspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Ledger.packetFingerprint -cne [string]$packet.fingerprint) { $issues.Add('Task packet drifted.') }
    if ([string]$Ledger.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Confidence policy drifted.') }
    foreach ($property in @($Ledger.inputs.artifactHashes.PSObject.Properties)) {
        $path = Join-Path $absoluteWorkspace $property.Name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or [string]$property.Value -cne (Get-FileSha $path)) { $issues.Add("Confidence input drifted: $($property.Name).") }
    }
    $expectedDimensionIds = @($confidencePolicy.dimensions.PSObject.Properties.Name | Sort-Object)
    $storedDimensionIds = @($Ledger.dimensions.id | Sort-Object -Unique)
    if ($expectedDimensionIds.Count -ne $storedDimensionIds.Count -or @(Compare-Object $expectedDimensionIds $storedDimensionIds).Count -ne 0) { $issues.Add('Confidence dimension set is invalid.') }
    foreach ($dimension in @($Ledger.dimensions)) {
        $dimensionPolicy = $confidencePolicy.dimensions.PSObject.Properties[[string]$dimension.id]
        if ($null -eq $dimensionPolicy -or [int]$dimension.weight -ne [int]$dimensionPolicy.Value) { $issues.Add("Dimension weight is invalid for '$($dimension.id)'.") }
        if ([string]$dimension.status -notin @('pass', 'warning', 'not-assessed', 'fail')) { $issues.Add("Dimension status is invalid for '$($dimension.id)'.") }
    }
    foreach ($cap in @($Ledger.hardCaps)) {
        $policyCap = $confidencePolicy.hardCaps.PSObject.Properties[[string]$cap.id]
        if ($null -eq $policyCap -or [int]$cap.maximumScore -ne [int]$policyCap.Value) { $issues.Add("Confidence cap is invalid for '$($cap.id)'.") }
    }
    $expectedUncapped = [Math]::Round([double](($Ledger.dimensions | ForEach-Object {
        $statusName = [string]$_.status
        $expectedEarned = [Math]::Round([int]$_.weight * [double]$confidencePolicy.statusMultipliers.$statusName, 2)
        if ([double]$_.earned -ne $expectedEarned) { $issues.Add("Dimension arithmetic is invalid for '$($_.id)'.") }
        $expectedEarned
    } | Measure-Object -Sum).Sum), 2)
    $expectedCap = if (@($Ledger.hardCaps).Count -eq 0) { 100 } else { [int](($Ledger.hardCaps.maximumScore | Measure-Object -Minimum).Minimum) }
    $expectedScore = [Math]::Min($expectedUncapped, $expectedCap)
    if ([double]$Ledger.uncappedScore -ne $expectedUncapped -or [int]$Ledger.appliedCap -ne $expectedCap -or [double]$Ledger.score -ne $expectedScore) { $issues.Add('Confidence score arithmetic is invalid.') }
    if ([string]$Ledger.level -cne (Get-Level $expectedScore)) { $issues.Add('Confidence level is invalid.') }
    $expectedVerdict = if (@($Ledger.dimensions | Where-Object status -eq 'fail').Count -gt 0) { 'blocked' } elseif (@($Ledger.dimensions | Where-Object status -in @('warning', 'not-assessed')).Count -gt 0) { 'conditional' } else { 'trusted' }
    if ([string]$Ledger.verdict -cne $expectedVerdict) { $issues.Add('Confidence verdict is invalid.') }
    if ([string]$Ledger.ledgerHash -cne (Get-Hash (Get-Payload $Ledger))) { $issues.Add('Confidence ledger hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}

if ($Action -in @('assess', 'create')) {
    $ledger = New-Ledger
    if ($Action -eq 'create') {
        [IO.File]::WriteAllText($receiptPath, (($ledger | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $true; ledger = $ledger; issues = @(); savedPath = $(if ($Action -eq 'create') { "$workspace/confidence-ledger.json" } else { $null }) }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Confidence ledger is absent: $workspace/confidence-ledger.json" }
    $ledger = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Ledger $ledger
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; ledger = $ledger; issues = @($validation.issues) }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 40 } else {
    Write-Host "Confidence ledger: action=$Action, valid=$($result.valid), score=$($result.ledger.score), level=$($result.ledger.level), verdict=$($result.ledger.verdict)"
    foreach ($dimension in @($result.ledger.dimensions)) { Write-Host " - [$($dimension.status)] $($dimension.id): $($dimension.earned)/$($dimension.weight) - $($dimension.summary)" }
    foreach ($cap in @($result.ledger.hardCaps)) { Write-Host " - cap $($cap.id): <=$($cap.maximumScore) - $($cap.reason)" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
