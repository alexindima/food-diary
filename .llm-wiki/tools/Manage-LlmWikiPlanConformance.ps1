[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('assess', 'create', 'replan', 'show', 'verify')]
    [string]$Action = 'assess',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$Reason,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$normalizedWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $normalizedWorkspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$workspaceAbsolute = Join-Path $repositoryRoot $normalizedWorkspace
$manifestPath = Join-Path $workspaceAbsolute 'change-manifest.json'
$packetPath = Join-Path $workspaceAbsolute 'change-packet.json'
$receiptPath = Join-Path $workspaceAbsolute 'plan-conformance.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
foreach ($requiredPath in @($manifestPath, $packetPath, $policyPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { throw "Required conformance input is absent: $requiredPath" }
}
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$conformancePolicy = $policy.planConformance

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
    if ($null -eq $json) { $json = '[]' }
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Test-PathMatch([string]$Value, [object[]]$Patterns) {
    foreach ($pattern in @($Patterns)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$pattern) -and $Value -match [string]$pattern) { return $true }
    }
    $false
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = $Receipt.schemaVersion
        workspace = $Receipt.workspace
        assessedAtUtc = $Receipt.assessedAtUtc
        manifestHash = $Receipt.manifestHash
        planFingerprint = $Receipt.planFingerprint
        packetFingerprint = $Receipt.packetFingerprint
        policyFingerprint = $Receipt.policyFingerprint
        diffFingerprint = $Receipt.diffFingerprint
        classification = $Receipt.classification
        policyFindings = @($Receipt.policyFindings)
        valid = $Receipt.valid
    }
}
function Get-Assessment {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $actual = @($packet.diff.changedPaths | Sort-Object -Unique)
    $planned = @($manifest.scope.plannedPaths | Sort-Object -Unique)
    $outOfScope = @($actual | Where-Object {
        -not (Test-PathMatch $_ @($manifest.scope.allowedPathPatterns)) -or
        (Test-PathMatch $_ @($manifest.scope.excludedPathPatterns))
    })
    $plannedChanged = @($actual | Where-Object { $_ -in $planned })
    $unplannedAllowed = @($actual | Where-Object { $_ -notin $planned -and $_ -notin $outOfScope })
    $missingPlanned = @($planned | Where-Object { $_ -notin $actual })
    $phaseFiles = @($manifest.plan.phases.files | Where-Object { $_ } | Sort-Object -Unique)
    $changedPhaseFiles = @($actual | Where-Object { $_ -in $phaseFiles })
    $findings = [Collections.Generic.List[object]]::new()
    if ([bool]$conformancePolicy.blockOutOfScope -and $outOfScope.Count -gt 0) {
        $findings.Add([pscustomobject][ordered]@{ id = 'out-of-scope'; severity = 'block'; count = $outOfScope.Count })
    }
    if ($unplannedAllowed.Count -gt [int]$conformancePolicy.maximumUnplannedAllowedPaths) {
        $findings.Add([pscustomobject][ordered]@{ id = 'unplanned-allowed'; severity = 'block'; count = $unplannedAllowed.Count })
    }
    if ($missingPlanned.Count -gt [int]$conformancePolicy.maximumMissingPlannedPaths) {
        $findings.Add([pscustomobject][ordered]@{ id = 'missing-planned'; severity = 'block'; count = $missingPlanned.Count })
    }
    $manifestChecks = @($manifest.plan.requiredChecks | ForEach-Object { "$($_.id)|$($_.command)" } | Sort-Object -Unique)
    $packetChecks = @($packet.brief.requiredChecks | ForEach-Object { "$($_.id)|$($_.command)" } | Sort-Object -Unique)
    $newChecks = @($packetChecks | Where-Object { $_ -notin $manifestChecks })
    $manifestReviews = @($manifest.plan.reviewObligations.id | Sort-Object -Unique)
    $packetReviews = @($packet.brief.reviewObligations.id | Sort-Object -Unique)
    $newReviews = @($packetReviews | Where-Object { $_ -notin $manifestReviews })
    if ([bool]$conformancePolicy.blockNewChecks -and $newChecks.Count -gt 0) {
        $findings.Add([pscustomobject][ordered]@{ id = 'new-required-checks'; severity = 'block'; count = $newChecks.Count })
    }
    if ([bool]$conformancePolicy.blockNewReviews -and $newReviews.Count -gt 0) {
        $findings.Add([pscustomobject][ordered]@{ id = 'new-review-obligations'; severity = 'block'; count = $newReviews.Count })
    }
    [pscustomobject]@{
        manifest = $manifest
        packet = $packet
        manifestHash = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
        policyFingerprint = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        diffFingerprint = Get-Hash $actual
        classification = [pscustomobject][ordered]@{
            actualPaths = $actual
            plannedChangedPaths = $plannedChanged
            unplannedAllowedPaths = $unplannedAllowed
            outOfScopePaths = $outOfScope
            missingPlannedPaths = $missingPlanned
            plannedPhaseFiles = $phaseFiles
            changedPhaseFiles = $changedPhaseFiles
            changedPathCount = $actual.Count
            plannedCoveragePercent = $(if ($planned.Count -eq 0) { 100 } else { [Math]::Round(($plannedChanged.Count * 100.0) / $planned.Count, 2) })
        }
        findings = @($findings)
        valid = $findings.Count -eq 0
    }
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    $current = Get-Assessment
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $normalizedWorkspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Receipt.manifestHash -cne [string]$current.manifestHash) { $issues.Add('Change manifest drifted.') }
    if ([string]$Receipt.planFingerprint -cne [string]$current.manifest.planFingerprint) { $issues.Add('Implementation plan drifted.') }
    if ([string]$Receipt.packetFingerprint -cne [string]$current.packet.fingerprint) { $issues.Add('Stored change packet drifted.') }
    if ([string]$Receipt.policyFingerprint -cne [string]$current.policyFingerprint) { $issues.Add('Workspace policy drifted.') }
    if ([string]$Receipt.diffFingerprint -cne [string]$current.diffFingerprint) { $issues.Add('Actual Git diff drifted.') }
    if ((Get-Hash $Receipt.classification) -cne (Get-Hash $current.classification)) { $issues.Add('Conformance classification drifted.') }
    if ((Get-Hash @($Receipt.policyFindings)) -cne (Get-Hash @($current.findings))) { $issues.Add('Conformance findings drifted.') }
    if ([bool]$Receipt.valid -ne [bool]$current.valid) { $issues.Add('Conformance verdict drifted.') }
    if ([string]$Receipt.conformanceHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Conformance hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0 -and [bool]$Receipt.valid; integrityValid = $issues.Count -eq 0; issues = @($issues); current = $current }
}
function New-Receipt([object]$Assessment) {
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspace
        assessedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        manifestHash = [string]$Assessment.manifestHash
        planFingerprint = [string]$Assessment.manifest.planFingerprint
        packetFingerprint = [string]$Assessment.packet.fingerprint
        policyFingerprint = [string]$Assessment.policyFingerprint
        diffFingerprint = [string]$Assessment.diffFingerprint
        classification = $Assessment.classification
        policyFindings = @($Assessment.findings)
        valid = [bool]$Assessment.valid
        conformanceHash = ''
    }
    $receipt.conformanceHash = Get-Hash (Get-Payload $receipt)
    $receipt
}

if ($Action -eq 'replan') {
    if ([string]::IsNullOrWhiteSpace($Reason)) { throw 'replan requires Reason.' }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeManifest.ps1') init `
        -Path "$normalizedWorkspace/change-manifest.json" `
        -Objective ([string]$manifest.objective) `
        -BaseRef ([string]$manifest.git.base) `
        -ChangedPath @($packet.diff.changedPaths) `
        -AllowedPath @($manifest.scope.allowedPathPatterns) `
        -ExcludedPath @($manifest.scope.excludedPathPatterns) | Out-Null
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') add `
        -WorkspacePath $normalizedWorkspace `
        -JournalType decision `
        -Text 'Rebaselined the implementation plan against the current Git diff.' `
        -Rationale $Reason | Out-Null
    if (Test-Path -LiteralPath $receiptPath) { [IO.File]::Delete($receiptPath) }
    $receipt = New-Receipt (Get-Assessment)
    $result = [pscustomobject][ordered]@{ action = 'replan'; valid = $receipt.valid; conformance = $receipt; savedPath = $null; reason = $Reason }
} elseif ($Action -in @('assess', 'create')) {
    $receipt = New-Receipt (Get-Assessment)
    if ($Action -eq 'create') {
        $temporaryPath = "$receiptPath.$([guid]::NewGuid().ToString('N')).tmp"
        try {
            [IO.File]::WriteAllText($temporaryPath, (($receipt | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
            if (Test-Path -LiteralPath $receiptPath) { [IO.File]::Delete($receiptPath) }
            [IO.File]::Move($temporaryPath, $receiptPath)
        } finally {
            if (Test-Path -LiteralPath $temporaryPath) { [IO.File]::Delete($temporaryPath) }
        }
    }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $receipt.valid; conformance = $receipt; savedPath = $(if ($Action -eq 'create') { "$normalizedWorkspace/plan-conformance.json" } else { $null }) }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Plan conformance receipt is absent: $normalizedWorkspace/plan-conformance.json" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Receipt $receipt
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; integrityValid = $validation.integrityValid; issues = @($validation.issues); conformance = $receipt }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Plan conformance: action=$($result.action), valid=$($result.valid), changed=$($result.conformance.classification.changedPathCount), unplanned=$(@($result.conformance.classification.unplannedAllowedPaths).Count), out-of-scope=$(@($result.conformance.classification.outOfScopePaths).Count), missing=$(@($result.conformance.classification.missingPlannedPaths).Count)"
    foreach ($finding in @($result.conformance.policyFindings)) { Write-Host " - [$($finding.severity)] $($finding.id): $($finding.count)" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
