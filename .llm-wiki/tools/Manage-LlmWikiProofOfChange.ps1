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
$normalizedWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $normalizedWorkspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$workspaceAbsolute = Join-Path $repositoryRoot $normalizedWorkspace
$acceptancePath = Join-Path $workspaceAbsolute 'acceptance-matrix.json'
$evidencePath = Join-Path $workspaceAbsolute 'evidence.json'
$manifestPath = Join-Path $workspaceAbsolute 'change-manifest.json'
$packetPath = Join-Path $workspaceAbsolute 'change-packet.json'
$receiptPath = Join-Path $workspaceAbsolute 'proof-of-change.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
foreach ($requiredPath in @($acceptancePath, $evidencePath, $manifestPath, $packetPath, $policyPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { throw "Required proof input is absent: $requiredPath" }
}
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$proofPolicy = $policy.proofOfChange
. (Join-Path $PSScriptRoot 'LlmWikiRequirementCriteria.ps1')
$requirementPolicy = $policy.requirementModel

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    if ($null -eq $json) { $json = 'null' }
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileHashValue([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-MappedValues([object]$Mapping, [string]$Name) {
    if ($null -eq $Mapping -or $null -eq $Mapping.PSObject.Properties[$Name]) { return @() }
    @($Mapping.$Name | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = $Receipt.schemaVersion
        workspace = $Receipt.workspace
        assessedAtUtc = $Receipt.assessedAtUtc
        inputHashes = $Receipt.inputHashes
        policyFingerprint = $Receipt.policyFingerprint
        packetFingerprint = $Receipt.packetFingerprint
        applicable = $Receipt.applicable
        classification = $Receipt.classification
        findings = @($Receipt.findings)
        valid = $Receipt.valid
    }
}
function Get-Assessment {
    $acceptance = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $actualPaths = @($packet.diff.changedPaths | Sort-Object -Unique)
    $pending = @($acceptance.criteria | Where-Object status -eq 'pending')
    $applicable = $pending.Count -eq 0
    $criterionProofs = [Collections.Generic.List[object]]::new()
    $findings = [Collections.Generic.List[object]]::new()

    foreach ($criterion in @($acceptance.criteria)) {
        $changedPaths = @(Get-MappedValues $criterion.mapping 'changedPaths')
        $scenarioIds = @(Get-MappedValues $criterion.mapping 'scenarioIds')
        $checkIds = @(Get-MappedValues $criterion.mapping 'checkIds')
        $reviewIds = @(Get-MappedValues $criterion.mapping 'reviewIds')
        $testPaths = @(Get-MappedValues $criterion.mapping 'testPaths')
        $pathsOutsideDiff = @($changedPaths | Where-Object { $_ -notin $actualPaths })
        $missingTestPaths = @($testPaths | Where-Object { -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $_) -PathType Leaf) })
        $verifiedChecks = @($checkIds | Where-Object {
            $id = $_
            @($evidence.checks | Where-Object { $_.id -eq $id -and $_.status -eq 'passed' }).Count -gt 0
        })
        $verifiedReviews = @($reviewIds | Where-Object {
            $id = $_
            @($evidence.reviews | Where-Object { $_.id -eq $id -and $_.status -eq 'completed' }).Count -gt 0
        })
        $hasEvidenceNote = -not [string]::IsNullOrWhiteSpace([string]$criterion.resolution.evidenceNote)
        $hasVerifiedEvidence = $verifiedChecks.Count -gt 0 -or $verifiedReviews.Count -gt 0 -or $hasEvidenceNote
        $criterionFindings = [Collections.Generic.List[string]]::new()
        if (-not (Test-LlmWikiCriterionAtomic ([string]$criterion.text) $requirementPolicy)) { $criterionFindings.Add('criterion-compound') }
        if ($criterion.status -eq 'rejected') { $criterionFindings.Add('criterion-rejected') }
        if ($criterion.status -eq 'satisfied' -and $changedPaths.Count -lt [int]$proofPolicy.minimumChangedPathsPerSatisfiedCriterion) {
            $criterionFindings.Add('missing-change-link')
        }
        if ($criterion.status -eq 'satisfied' -and [bool]$proofPolicy.requireMappedPathsInCurrentDiff -and $pathsOutsideDiff.Count -gt 0) {
            $criterionFindings.Add('mapped-path-outside-diff')
        }
        if ($criterion.status -eq 'satisfied' -and [bool]$proofPolicy.requireMappedTestPathsToExist -and $missingTestPaths.Count -gt 0) {
            $criterionFindings.Add('mapped-test-path-missing')
        }
        if ($criterion.status -eq 'satisfied' -and [bool]$proofPolicy.requireVerifiedEvidencePerSatisfiedCriterion -and -not $hasVerifiedEvidence) {
            $criterionFindings.Add('verified-evidence-missing')
        }
        foreach ($findingId in $criterionFindings) {
            $findings.Add([pscustomobject][ordered]@{ id = $findingId; severity = 'block'; criterionId = [string]$criterion.id })
        }
        $criterionProofs.Add([pscustomobject][ordered]@{
            id = [string]$criterion.id
            text = [string]$criterion.text
            status = [string]$criterion.status
            changedPaths = $changedPaths
            pathsOutsideDiff = $pathsOutsideDiff
            scenarioIds = $scenarioIds
            checkIds = $checkIds
            verifiedCheckIds = $verifiedChecks
            reviewIds = $reviewIds
            verifiedReviewIds = $verifiedReviews
            testPaths = $testPaths
            missingTestPaths = $missingTestPaths
            evidenceNotePresent = $hasEvidenceNote
            verifiedEvidence = $hasVerifiedEvidence
            proven = $criterion.status -eq 'not-applicable' -or ($criterion.status -eq 'satisfied' -and $criterionFindings.Count -eq 0)
            findings = @($criterionFindings)
        })
    }
    $satisfied = @($criterionProofs | Where-Object status -eq 'satisfied')
    $proven = @($criterionProofs | Where-Object proven)
    [pscustomobject]@{
        acceptance = $acceptance
        packet = $packet
        applicable = $applicable
        inputHashes = [pscustomobject][ordered]@{
            acceptance = Get-FileHashValue $acceptancePath
            evidence = Get-FileHashValue $evidencePath
            manifest = Get-FileHashValue $manifestPath
            packet = Get-FileHashValue $packetPath
        }
        policyFingerprint = Get-FileHashValue $policyPath
        classification = [pscustomobject][ordered]@{
            criteria = @($criterionProofs)
            criteriaCount = @($criterionProofs).Count
            satisfiedCount = $satisfied.Count
            notApplicableCount = @($criterionProofs | Where-Object status -eq 'not-applicable').Count
            pendingCount = $pending.Count
            provenCount = $proven.Count
            proofCoveragePercent = $(if (@($criterionProofs).Count -eq 0) { 100 } else { [Math]::Round(($proven.Count * 100.0) / @($criterionProofs).Count, 2) })
        }
        findings = @($findings)
        valid = -not $applicable -or $findings.Count -eq 0
    }
}
function New-Receipt([object]$Assessment) {
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspace
        assessedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        inputHashes = $Assessment.inputHashes
        policyFingerprint = [string]$Assessment.policyFingerprint
        packetFingerprint = [string]$Assessment.packet.fingerprint
        applicable = [bool]$Assessment.applicable
        classification = $Assessment.classification
        findings = @($Assessment.findings)
        valid = [bool]$Assessment.valid
        proofHash = ''
    }
    $receipt.proofHash = Get-Hash (Get-Payload $receipt)
    $receipt
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    $current = Get-Assessment
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $normalizedWorkspace) { $issues.Add('Workspace does not match.') }
    foreach ($name in @('acceptance', 'evidence', 'manifest', 'packet')) {
        if ([string]$Receipt.inputHashes.$name -cne [string]$current.inputHashes.$name) { $issues.Add("$name input drifted.") }
    }
    if ([string]$Receipt.policyFingerprint -cne [string]$current.policyFingerprint) { $issues.Add('Workspace policy drifted.') }
    if ([string]$Receipt.packetFingerprint -cne [string]$current.packet.fingerprint) { $issues.Add('Stored change packet drifted.') }
    if ([bool]$Receipt.applicable -ne [bool]$current.applicable) { $issues.Add('Proof applicability drifted.') }
    if ((Get-Hash $Receipt.classification) -cne (Get-Hash $current.classification)) { $issues.Add('Proof classification drifted.') }
    if ((Get-Hash @($Receipt.findings)) -cne (Get-Hash @($current.findings))) { $issues.Add('Proof findings drifted.') }
    if ([bool]$Receipt.valid -ne [bool]$current.valid) { $issues.Add('Proof verdict drifted.') }
    if ([string]$Receipt.proofHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Proof hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0 -and [bool]$Receipt.valid; integrityValid = $issues.Count -eq 0; issues = @($issues) }
}

if ($Action -in @('assess', 'create')) {
    $receipt = New-Receipt (Get-Assessment)
    if ($Action -eq 'create') {
        if (-not $receipt.applicable) { throw 'Proof of change cannot be sealed while acceptance criteria are pending.' }
        $temporaryPath = "$receiptPath.$([guid]::NewGuid().ToString('N')).tmp"
        try {
            [IO.File]::WriteAllText($temporaryPath, (($receipt | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
            if (Test-Path -LiteralPath $receiptPath) { [IO.File]::Delete($receiptPath) }
            [IO.File]::Move($temporaryPath, $receiptPath)
        } finally {
            if (Test-Path -LiteralPath $temporaryPath) { [IO.File]::Delete($temporaryPath) }
        }
    }
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $receipt.valid; applicable = $receipt.applicable; proof = $receipt; savedPath = $(if ($Action -eq 'create') { "$normalizedWorkspace/proof-of-change.json" } else { $null }) }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Proof-of-change receipt is absent: $normalizedWorkspace/proof-of-change.json" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Receipt $receipt
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; applicable = $receipt.applicable; integrityValid = $validation.integrityValid; issues = @($validation.issues); proof = $receipt }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 40 } else {
    Write-Host "Proof of change: action=$($result.action), applicable=$($result.applicable), valid=$($result.valid), coverage=$($result.proof.classification.proofCoveragePercent)%"
    foreach ($finding in @($result.proof.findings)) { Write-Host " - [$($finding.severity)] $($finding.criterionId): $($finding.id)" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
