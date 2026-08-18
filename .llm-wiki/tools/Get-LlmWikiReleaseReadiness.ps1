[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string]$Objective,
    [object]$PacketInput,
    [object]$ApiCompatibilityInput,
    [string]$ManifestPath = '.artifacts/llm-wiki/change-manifest.json',
    [string]$AcceptancePath = '.artifacts/llm-wiki/acceptance-matrix.json',
    [string]$EvidencePath = '.artifacts/llm-wiki/evidence.json',
    [switch]$RequireManifest,
    [switch]$RequireAcceptance,
    [switch]$RequireEvidence,
    [switch]$FailOnNotReady,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
function Resolve-ArtifactPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) { return $Path }
    return Join-Path $repositoryRoot $Path
}
function Test-PathMatch([string]$Value, [object[]]$Patterns) {
    foreach ($pattern in @($Patterns)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$pattern) -and $Value -match $pattern) { return $true }
    }
    return $false
}
function Test-GovernanceGeneratedPath([string]$Value) {
    $Value -match '^\.llm-wiki/generated/' -or
        $Value -eq '.llm-wiki/reviews/source-impact-reviews.json'
}
function Get-Ids([object[]]$Items) {
    return @($Items | ForEach-Object {
        if ($null -ne $_ -and $_.PSObject.Properties['id'] -and -not [string]::IsNullOrWhiteSpace([string]$_.id)) { $_.id }
    })
}

$packetArguments = @{ BaseRef = $BaseRef; Objective = $Objective; Format = 'Json' }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $packetArguments.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $packetArguments.ChangedPath = $ChangedPath }
$packet = if ($null -ne $PacketInput) { $PacketInput } else {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiChangePacket.ps1') @packetArguments | ConvertFrom-Json
}
$dimensions = [System.Collections.Generic.List[object]]::new()
function Add-Dimension([string]$Id, [int]$Weight, [string]$Status, [string]$Summary, [object[]]$Issues = @()) {
    $dimensions.Add([pscustomobject][ordered]@{
        id = $Id
        weight = $Weight
        status = $Status
        summary = $Summary
        issues = @($Issues)
    })
}

$policyIssues = @($packet.policy.violations | ForEach-Object { "[$($_.rule)] $($_.message)" })
if ($policyIssues.Count -gt 0) {
    Add-Dimension 'policy' 20 'fail' 'Structural change policy violations exist.' $policyIssues
} else {
    Add-Dimension 'policy' 20 'pass' "$(@($packet.policy.matchedRules).Count) policy rule(s) evaluated without structural violations."
}

$health = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/architecture-health-index.json') -Raw | ConvertFrom-Json
$architectureIssues = @(
    @($health.projectDependencyViolations | ForEach-Object { "$($_.source) -> $($_.target)" }) +
    @($health.untrackedProductionProjects | ForEach-Object { "Ungoverned project: $($_.name)" }) +
    @($health.moduleCycleNodes | ForEach-Object { "Module cycle node: $_" })
)
if ($architectureIssues.Count -gt 0) {
    Add-Dimension 'architecture' 15 'fail' 'Enforced architecture drift exists.' $architectureIssues
} else {
    Add-Dimension 'architecture' 15 'pass' 'No forbidden project edges, ungoverned production projects, or module cycles.'
}

$apiScope = @($packet.diff.scopes) -contains 'Api'
if (-not $apiScope) {
    Add-Dimension 'api-compatibility' 15 'not-applicable' 'No backend HTTP API scope was inferred.'
} elseif ($PSBoundParameters.ContainsKey('ChangedPath')) {
    Add-Dimension 'api-compatibility' 15 'not-assessed' 'Synthetic path input cannot prove an OpenAPI diff; run against the real Git change set.'
} else {
    $apiArguments = @{ BaseRef = $BaseRef; Format = 'Json' }
    if ($PSBoundParameters.ContainsKey('HeadRef')) { $apiArguments.HeadRef = $HeadRef }
    $api = if ($null -ne $ApiCompatibilityInput) {
        $ApiCompatibilityInput
    } else {
        & (Join-Path $PSScriptRoot 'Test-LlmWikiApiCompatibility.ps1') @apiArguments | ConvertFrom-Json
    }
    $breakingChanges = if ($api.PSObject.Properties['breakingChanges']) {
        @($api.breakingChanges)
    } elseif ($api.PSObject.Properties['changes']) {
        @($api.changes | Where-Object severity -eq 'breaking')
    } else {
        @()
    }
    $behavioralRestrictions = if ($api.PSObject.Properties['behavioralRestrictions']) {
        @($api.behavioralRestrictions)
    } elseif ($api.PSObject.Properties['changes']) {
        @($api.changes | Where-Object severity -eq 'behavioral-restriction')
    } else {
        @()
    }
    if ($api.breakingCount -gt 0) {
        Add-Dimension 'api-compatibility' 15 'fail' "$($api.breakingCount) breaking API change(s) detected." $breakingChanges
    } elseif ($behavioralRestrictions.Count -gt 0) {
        Add-Dimension 'api-compatibility' 15 'warning' "$($behavioralRestrictions.Count) behavioral API restriction(s) require explicit review; no schema-breaking change was detected." $behavioralRestrictions
    } else {
        Add-Dimension 'api-compatibility' 15 'pass' 'No breaking API snapshot change was detected.'
    }
}

$manifestAbsolute = Resolve-ArtifactPath $ManifestPath
if (-not (Test-Path -LiteralPath $manifestAbsolute)) {
    Add-Dimension 'scope-manifest' 10 $(if ($RequireManifest) { 'fail' } else { 'not-assessed' }) 'Change manifest is absent.'
} else {
    $manifest = Get-Content -LiteralPath $manifestAbsolute -Raw | ConvertFrom-Json
    $outOfScope = @($packet.diff.changedPaths | Where-Object { -not (Test-GovernanceGeneratedPath ([string]$_)) } | Where-Object {
        -not (Test-PathMatch $_ @($manifest.scope.allowedPathPatterns)) -or
        (Test-PathMatch $_ @($manifest.scope.excludedPathPatterns))
    })
    $currentChecks = @($packet.policy.requiredChecks | ForEach-Object { "$($_.id)|$($_.command)" })
    $snapshotChecks = @($manifest.plan.requiredChecks | ForEach-Object { "$($_.id)|$($_.command)" })
    $newChecks = @($currentChecks | Where-Object { $_ -notin $snapshotChecks })
    $currentReviews = @(Get-Ids @($packet.policy.reviewObligations))
    $snapshotReviews = @(Get-Ids @($manifest.plan.reviewObligations))
    $newReviews = @($currentReviews | Where-Object { $_ -notin $snapshotReviews })
    $manifestIssues = @(
        @($outOfScope | ForEach-Object { "Out of scope: $_" }) +
        @($newChecks | ForEach-Object { "New required check: $_" }) +
        @($newReviews | ForEach-Object { "New review obligation: $_" })
    )
    if ($manifestIssues.Count -gt 0) {
        Add-Dimension 'scope-manifest' 10 'fail' 'The final change drifted from the manifest.' $manifestIssues
    } else {
        Add-Dimension 'scope-manifest' 10 'pass' 'Changed paths and obligations remain inside the manifest.'
    }
}

$acceptanceAbsolute = Resolve-ArtifactPath $AcceptancePath
if (-not (Test-Path -LiteralPath $acceptanceAbsolute)) {
    Add-Dimension 'acceptance' 15 $(if ($RequireAcceptance) { 'fail' } else { 'not-assessed' }) 'Acceptance matrix is absent.'
} else {
    $acceptanceJson = & (Join-Path $PSScriptRoot 'Manage-LlmWikiAcceptanceMatrix.ps1') validate `
        -Path $AcceptancePath `
        -EvidencePath $EvidencePath `
        -RequireEvidence:$RequireEvidence `
        -Format Json
    $acceptance = $acceptanceJson | ConvertFrom-Json
    $acceptanceIssues = @(
        @($acceptance.unmapped | ForEach-Object { "Unmapped: $_" }) +
        @($acceptance.unresolved | ForEach-Object { "Unresolved: $_" }) +
        @($acceptance.unverified | ForEach-Object { "Unverified: $_" })
    )
    if (-not $acceptance.valid) {
        Add-Dimension 'acceptance' 15 'fail' 'Acceptance criteria are incomplete or unverified.' $acceptanceIssues
    } else {
        Add-Dimension 'acceptance' 15 'pass' "$($acceptance.satisfiedCount) criterion/criteria satisfied; $($acceptance.notApplicableCount) not applicable."
    }
}

$evidenceAbsolute = Resolve-ArtifactPath $EvidencePath
$evidence = if (Test-Path -LiteralPath $evidenceAbsolute) {
    Get-Content -LiteralPath $evidenceAbsolute -Raw | ConvertFrom-Json
} else { $null }
if ($null -eq $evidence) {
    $missingEvidenceStatus = $(if ($RequireEvidence) { 'fail' } else { 'not-assessed' })
    Add-Dimension 'verification-evidence' 10 $missingEvidenceStatus 'Verification evidence bundle is absent.'
    Add-Dimension 'review-evidence' 5 $missingEvidenceStatus 'Review evidence bundle is absent.'
} else {
    $unresolvedChecks = @($evidence.checks | Where-Object { $_.status -notin @('passed', 'passed-with-known-baseline-failures', 'not-applicable') })
    $unresolvedReviews = @($evidence.reviews | Where-Object { $_.status -notin @('completed', 'not-applicable') })
    $evidenceCheckIds = @(Get-Ids @($evidence.checks))
    $evidenceReviewIds = @(Get-Ids @($evidence.reviews))
    $missingChecks = @(Get-Ids @($packet.policy.requiredChecks) | Where-Object { $_ -notin $evidenceCheckIds })
    $missingReviews = @(Get-Ids @($packet.policy.reviewObligations) | Where-Object { $_ -notin $evidenceReviewIds })
    $lineage = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') -EvidencePath $EvidencePath -Format Json | ConvertFrom-Json
    $verificationIssues = @(
        @($unresolvedChecks | ForEach-Object { "Check $($_.id): $($_.status)" }) +
        @($missingChecks | ForEach-Object { "Missing check: $_" }) +
        @($lineage.issues | ForEach-Object { "Lineage: $_" })
    )
    $reviewIssues = @(
        @($unresolvedReviews | ForEach-Object { "Review $($_.id): $($_.status)" }) +
        @($missingReviews | ForEach-Object { "Missing review: $_" })
    )
    if ($verificationIssues.Count -gt 0) {
        Add-Dimension 'verification-evidence' 10 'fail' 'Required verification evidence is missing or unresolved.' $verificationIssues
    } else {
        Add-Dimension 'verification-evidence' 10 'pass' 'All required checks have resolved evidence.'
    }
    if ($reviewIssues.Count -gt 0) {
        Add-Dimension 'review-evidence' 5 'fail' 'Required review evidence is missing or unresolved.' $reviewIssues
    } else {
        Add-Dimension 'review-evidence' 5 'pass' 'All required reviews have resolved evidence.'
    }
}

$privacyImpactCount = @(
    @($packet.brief.privacyImpact.fields) +
    @($packet.brief.privacyImpact.boundaries) +
    @($packet.brief.privacyImpact.potentialLogging)
).Count
if ($privacyImpactCount -eq 0) {
    Add-Dimension 'privacy' 5 'not-applicable' 'No indexed sensitive-data impact was found.'
} elseif ($null -eq $evidence) {
    Add-Dimension 'privacy' 5 'not-assessed' "$privacyImpactCount privacy-sensitive impact item(s) lack evidence."
} else {
    $privacyReviews = @($evidence.reviews | Where-Object {
        $_.id -match 'privacy|security' -and $_.status -in @('completed', 'not-applicable')
    })
    if ($privacyReviews.Count -gt 0) {
        Add-Dimension 'privacy' 5 'pass' "$privacyImpactCount privacy-sensitive item(s) have resolved privacy/security review evidence."
    } else {
        Add-Dimension 'privacy' 5 'fail' 'Privacy-sensitive impact exists without a resolved privacy/security review.'
    }
}

$rolloutImpact = @($packet.rollout.flags.PSObject.Properties | Where-Object { [bool]$_.Value }).Count -gt 0
if (-not $rolloutImpact) {
    Add-Dimension 'rollout' 5 'not-applicable' 'No specialized rollout flag was inferred.'
} elseif ($null -eq $evidence) {
    Add-Dimension 'rollout' 5 'not-assessed' 'Specialized rollout impact exists without an evidence bundle.'
} elseif (@($evidence.reviews | Where-Object { $_.status -notin @('completed', 'not-applicable') }).Count -eq 0) {
    Add-Dimension 'rollout' 5 'pass' 'Rollout-impacting reviews are resolved.'
} else {
    Add-Dimension 'rollout' 5 'fail' 'Rollout-impacting review obligations remain unresolved.'
}

$score = 0.0
foreach ($dimension in $dimensions) {
    if ($dimension.status -in @('pass', 'not-applicable')) { $score += $dimension.weight }
    elseif ($dimension.status -eq 'warning') { $score += ($dimension.weight * 0.5) }
}
$blocking = @($dimensions | Where-Object status -eq 'fail')
$unassessed = @($dimensions | Where-Object status -eq 'not-assessed')
$verdict = if ($blocking.Count -gt 0) { 'blocked' } elseif ($unassessed.Count -gt 0) { 'conditional' } else { 'ready' }
$engineeringIds = @('policy', 'architecture', 'api-compatibility', 'verification-evidence')
$engineeringDimensions = @($dimensions | Where-Object id -in $engineeringIds)
$governanceDimensions = @($dimensions | Where-Object id -notin $engineeringIds)
function Get-IndependentVerdict([object[]]$Items) {
    if (@($Items | Where-Object status -eq 'fail').Count -gt 0) { return 'blocked' }
    if (@($Items | Where-Object status -eq 'not-assessed').Count -gt 0) { return 'conditional' }
    return 'ready'
}
$result = [pscustomobject][ordered]@{
    verdict = $verdict
    score = [Math]::Round($score, 1)
    maximumScore = 100
    risk = $packet.brief.risk
    packetFingerprint = $packet.fingerprint
    dimensions = @($dimensions)
    blockingDimensions = @($blocking | ForEach-Object { $_.id })
    unassessedDimensions = @($unassessed | ForEach-Object { $_.id })
    engineeringReadiness = [pscustomobject][ordered]@{
        verdict = Get-IndependentVerdict $engineeringDimensions
        dimensions = @($engineeringDimensions.id)
        blockingDimensions = @($engineeringDimensions | Where-Object status -eq 'fail' | ForEach-Object id)
    }
    governanceCompleteness = [pscustomobject][ordered]@{
        verdict = Get-IndependentVerdict $governanceDimensions
        dimensions = @($governanceDimensions.id)
        blockingDimensions = @($governanceDimensions | Where-Object status -eq 'fail' | ForEach-Object id)
    }
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12 } else {
    Write-Host "Engineering readiness: $($result.engineeringReadiness.verdict); governance completeness: $($result.governanceCompleteness.verdict)"
    Write-Host "Combined release readiness: $verdict ($($result.score)/100), risk=$($result.risk.level)"
    foreach ($dimension in $dimensions) {
        Write-Host " - [$($dimension.status)] $($dimension.id) ($($dimension.weight)): $($dimension.summary)"
        foreach ($issue in @($dimension.issues)) { Write-Host "   - $issue" }
    }
}
if ($FailOnNotReady -and $verdict -ne 'ready') { exit 1 }
