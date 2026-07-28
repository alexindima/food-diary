[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string]$Objective,
    [object]$PacketInput,
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
    $api = & (Join-Path $PSScriptRoot 'Test-LlmWikiApiCompatibility.ps1') @apiArguments | ConvertFrom-Json
    if ($api.breakingCount -gt 0) {
        Add-Dimension 'api-compatibility' 15 'fail' "$($api.breakingCount) breaking API change(s) detected." @($api.breakingChanges)
    } else {
        Add-Dimension 'api-compatibility' 15 'pass' 'No breaking API snapshot change was detected.'
    }
}

$manifestAbsolute = Resolve-ArtifactPath $ManifestPath
if (-not (Test-Path -LiteralPath $manifestAbsolute)) {
    Add-Dimension 'scope-manifest' 10 $(if ($RequireManifest) { 'fail' } else { 'not-assessed' }) 'Change manifest is absent.'
} else {
    $manifest = Get-Content -LiteralPath $manifestAbsolute -Raw | ConvertFrom-Json
    $outOfScope = @($packet.diff.changedPaths | Where-Object {
        -not (Test-PathMatch $_ @($manifest.scope.allowedPathPatterns)) -or
        (Test-PathMatch $_ @($manifest.scope.excludedPathPatterns))
    })
    $currentChecks = @($packet.policy.requiredChecks | ForEach-Object { "$($_.id)|$($_.command)" })
    $snapshotChecks = @($manifest.plan.requiredChecks | ForEach-Object { "$($_.id)|$($_.command)" })
    $newChecks = @($currentChecks | Where-Object { $_ -notin $snapshotChecks })
    $currentReviews = @($packet.policy.reviewObligations.id)
    $snapshotReviews = @($manifest.plan.reviewObligations.id)
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
    Add-Dimension 'evidence' 15 $(if ($RequireEvidence) { 'fail' } else { 'not-assessed' }) 'Evidence bundle is absent.'
} else {
    $unresolvedChecks = @($evidence.checks | Where-Object { $_.status -notin @('passed', 'not-applicable') })
    $unresolvedReviews = @($evidence.reviews | Where-Object { $_.status -notin @('completed', 'not-applicable') })
    $missingChecks = @($packet.policy.requiredChecks.id | Where-Object { $_ -notin @($evidence.checks.id) })
    $missingReviews = @($packet.policy.reviewObligations.id | Where-Object { $_ -notin @($evidence.reviews.id) })
    $evidenceIssues = @(
        @($unresolvedChecks | ForEach-Object { "Check $($_.id): $($_.status)" }) +
        @($unresolvedReviews | ForEach-Object { "Review $($_.id): $($_.status)" }) +
        @($missingChecks | ForEach-Object { "Missing check: $_" }) +
        @($missingReviews | ForEach-Object { "Missing review: $_" })
    )
    if ($evidenceIssues.Count -gt 0) {
        Add-Dimension 'evidence' 15 'fail' 'Required evidence is missing or unresolved.' $evidenceIssues
    } else {
        Add-Dimension 'evidence' 15 'pass' 'All required checks and reviews have resolved evidence.'
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
$result = [pscustomobject][ordered]@{
    verdict = $verdict
    score = [Math]::Round($score, 1)
    maximumScore = 100
    risk = $packet.brief.risk
    packetFingerprint = $packet.fingerprint
    dimensions = @($dimensions)
    blockingDimensions = @($blocking | ForEach-Object { $_.id })
    unassessedDimensions = @($unassessed | ForEach-Object { $_.id })
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12 } else {
    Write-Host "Release readiness: $verdict ($($result.score)/100), risk=$($result.risk.level)"
    foreach ($dimension in $dimensions) {
        Write-Host " - [$($dimension.status)] $($dimension.id) ($($dimension.weight)): $($dimension.summary)"
        foreach ($issue in @($dimension.issues)) { Write-Host "   - $issue" }
    }
}
if ($FailOnNotReady -and $verdict -ne 'ready') { exit 1 }
