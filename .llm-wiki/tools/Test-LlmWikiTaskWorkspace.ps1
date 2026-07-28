[CmdletBinding()]
param(
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$workspacePolicySnapshot = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json | ConvertFrom-Json
$workspacePolicy = $workspacePolicySnapshot.policy
$latestWorkspaceSchemaVersion = [int]$workspacePolicy.workspace.latestSchemaVersion

if ([System.IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspacePath = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspacePath -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+(?:/.*)?$') {
    throw 'WorkspacePath must be inside .artifacts/llm-wiki/tasks/<task-name>.'
}
$absoluteWorkspacePath = Join-Path $repositoryRoot $normalizedWorkspacePath
$errors = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$checks = [System.Collections.Generic.List[object]]::new()

function Add-Check([string]$Id, [bool]$Passed, [string]$Message, [switch]$Warning) {
    $checks.Add([pscustomobject][ordered]@{
        id = $Id
        status = $(if ($Passed) { 'pass' } elseif ($Warning) { 'warning' } else { 'fail' })
        message = $Message
    })
    if (-not $Passed) {
        if ($Warning) { $warnings.Add($Message) } else { $errors.Add($Message) }
    }
}
function Read-JsonArtifact([string]$Name) {
    $path = Join-Path $absoluteWorkspacePath $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $errors.Add("Missing artifact: $Name")
        return $null
    }
    try {
        return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    } catch {
        $errors.Add("Invalid JSON in ${Name}: $($_.Exception.Message)")
        return $null
    }
}
function Get-Fingerprint([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 15 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}

if (-not (Test-Path -LiteralPath $absoluteWorkspacePath -PathType Container)) {
    throw "Task workspace does not exist: $normalizedWorkspacePath"
}
$expectedArtifacts = [ordered]@{
    packet = 'change-packet.json'
    taskContract = 'task-contract.json'
    manifest = 'change-manifest.json'
    acceptance = 'acceptance-matrix.json'
    evidence = 'evidence.json'
    journal = 'journal.json'
    report = 'review-report.md'
}
$descriptor = Read-JsonArtifact 'workspace.json'
$packet = Read-JsonArtifact $expectedArtifacts.packet
$taskContract = Read-JsonArtifact $expectedArtifacts.taskContract
$manifest = Read-JsonArtifact $expectedArtifacts.manifest
$acceptance = Read-JsonArtifact $expectedArtifacts.acceptance
$evidence = Read-JsonArtifact $expectedArtifacts.evidence
$journal = Read-JsonArtifact $expectedArtifacts.journal
$reportExists = Test-Path -LiteralPath (Join-Path $absoluteWorkspacePath $expectedArtifacts.report) -PathType Leaf
Add-Check 'required-artifacts' ($errors.Count -eq 0 -and $reportExists) 'Every required workspace artifact must exist and parse.'

if ($null -ne $descriptor) {
    Add-Check 'workspace-schema' ($descriptor.schemaVersion -eq $latestWorkspaceSchemaVersion) "workspace.json must use schemaVersion $latestWorkspaceSchemaVersion."
    if ($descriptor.schemaVersion -eq $latestWorkspaceSchemaVersion) {
        Add-Check 'workspace-format' ([string]$descriptor.format -ceq [string]$workspacePolicy.workspace.format) "workspace.json format must be '$($workspacePolicy.workspace.format)'."
        Add-Check 'policy-fingerprint' ([string]$descriptor.policyFingerprint -ceq [string]$workspacePolicySnapshot.fingerprint) 'workspace.json policyFingerprint must match the current workspace policy.'
        Add-Check 'policy-snapshot' ($null -ne $descriptor.policySnapshot) 'workspace.json must retain the accepted policy snapshot.'
        if ($null -ne $descriptor.policySnapshot) {
            Add-Check 'policy-snapshot-fingerprint' ((Get-Fingerprint $descriptor.policySnapshot) -ceq [string]$descriptor.policyFingerprint) 'The accepted policy snapshot must match policyFingerprint.'
        }
    }
    foreach ($artifact in $expectedArtifacts.GetEnumerator()) {
        $expectedPath = "$normalizedWorkspacePath/$($artifact.Value)"
        $actualPath = [string]$descriptor.artifacts.($artifact.Key)
        Add-Check "artifact-path-$($artifact.Key)" ($actualPath -ceq $expectedPath) "Descriptor path for '$($artifact.Key)' must be '$expectedPath'."
        if ($descriptor.schemaVersion -eq $latestWorkspaceSchemaVersion) {
            $expectedArtifactSchema = [int]$workspacePolicy.workspace.artifactSchemaVersions.($artifact.Key)
            Add-Check "artifact-schema-$($artifact.Key)" ($descriptor.artifactSchemaVersions.($artifact.Key) -eq $expectedArtifactSchema) "Descriptor schema version for '$($artifact.Key)' must be $expectedArtifactSchema."
        }
    }
}

foreach ($artifactItem in @(
    [pscustomobject]@{ name = 'packet'; value = $packet }
    [pscustomobject]@{ name = 'task contract'; value = $taskContract }
    [pscustomobject]@{ name = 'manifest'; value = $manifest }
    [pscustomobject]@{ name = 'acceptance'; value = $acceptance }
    [pscustomobject]@{ name = 'evidence'; value = $evidence }
    [pscustomobject]@{ name = 'journal'; value = $journal }
)) {
    if ($null -ne $artifactItem.value) {
        Add-Check "schema-$($artifactItem.name -replace ' ', '-')" ($artifactItem.value.schemaVersion -eq 1) "$($artifactItem.name) must use schemaVersion 1."
    }
}

if ($null -ne $descriptor -and $null -ne $taskContract -and $null -ne $manifest -and $null -ne $acceptance) {
    $objectives = @(
        [string]$descriptor.objective
        [string]$taskContract.objective
        [string]$manifest.objective
        [string]$acceptance.objective
    ) | Sort-Object -Unique
    Add-Check 'objective-consistency' ($objectives.Count -eq 1 -and -not [string]::IsNullOrWhiteSpace($objectives[0])) 'Objective must match across descriptor, task contract, manifest, and acceptance.'

    $baseRefs = @(
        [string]$taskContract.git.base
        [string]$manifest.git.base
        [string]$acceptance.git.base
        [string]$evidence.git.base
    ) | Sort-Object -Unique
    Add-Check 'git-base-consistency' ($baseRefs.Count -eq 1 -and -not [string]::IsNullOrWhiteSpace($baseRefs[0])) 'Git base must match across task contract, manifest, acceptance, and evidence.'
}

if ($null -ne $descriptor -and $null -ne $packet) {
    Add-Check 'current-packet-fingerprint' ([string]$descriptor.currentPacketFingerprint -ceq [string]$packet.fingerprint) 'Descriptor currentPacketFingerprint must match change-packet.json.'
    Add-Check 'initial-packet-fingerprint' ([string]$descriptor.initialPacketFingerprint -match '^[a-f0-9]{64}$') 'Descriptor initialPacketFingerprint must be SHA-256.'
}
if ($null -ne $descriptor -and $null -ne $acceptance) {
    Add-Check 'acceptance-origin' ([string]$acceptance.packetFingerprint -ceq [string]$descriptor.initialPacketFingerprint) 'Acceptance matrix must retain the initial packet fingerprint.'
}
if ($null -ne $manifest) {
    Add-Check 'manifest-plan-fingerprint' ((Get-Fingerprint $manifest.plan) -ceq [string]$manifest.planFingerprint) 'Manifest plan fingerprint must match its immutable plan snapshot.'
}

if ($null -ne $acceptance) {
    $scenarioIds = @($acceptance.availableEvidence.scenarios.id)
    $checkIds = @($acceptance.availableEvidence.checks.id)
    $reviewIds = @($acceptance.availableEvidence.reviews.id)
    $criterionIds = @($acceptance.criteria.id)
    Add-Check 'acceptance-criterion-ids' (@($criterionIds | Sort-Object -Unique).Count -eq $criterionIds.Count) 'Acceptance criterion IDs must be unique.'
    foreach ($criterion in @($acceptance.criteria)) {
        foreach ($id in @($criterion.mapping.scenarioIds)) {
            Add-Check "acceptance-$($criterion.id)-scenario-$id" ($id -in $scenarioIds) "Criterion $($criterion.id) references unknown scenario '$id'."
        }
        foreach ($id in @($criterion.mapping.checkIds)) {
            Add-Check "acceptance-$($criterion.id)-check-$id" ($id -in $checkIds) "Criterion $($criterion.id) references unknown check '$id'."
        }
        foreach ($id in @($criterion.mapping.reviewIds)) {
            Add-Check "acceptance-$($criterion.id)-review-$id" ($id -in $reviewIds) "Criterion $($criterion.id) references unknown review '$id'."
        }
    }
}

if ($null -ne $manifest -and $null -ne $evidence) {
    $missingChecks = @($manifest.plan.requiredChecks.id | Where-Object { $_ -notin @($evidence.checks.id) })
    $missingReviews = @($manifest.plan.reviewObligations.id | Where-Object { $_ -notin @($evidence.reviews.id) })
    Add-Check 'evidence-check-coverage' ($missingChecks.Count -eq 0) "Evidence must contain every manifest check. Missing: $($missingChecks -join ', ')"
    Add-Check 'evidence-review-coverage' ($missingReviews.Count -eq 0) "Evidence must contain every manifest review. Missing: $($missingReviews -join ', ')"
}
if ($null -ne $evidence) {
    $lineageValidation = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
    Add-Check 'evidence-lineage' ([bool]$lineageValidation.valid) "Resolved evidence must have compatible lineage. $(@($lineageValidation.issues) -join ' ')"
}

if ($null -ne $journal) {
    $journalValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskJournal.ps1') validate `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
    Add-Check 'journal-validity' ([bool]$journalValidation.valid) "Task journal must be valid. $(@($journalValidation.issues) -join ' ')"
}

$temporaryFiles = @(Get-ChildItem -LiteralPath $absoluteWorkspacePath -File -Force | Where-Object {
    $_.Name -like '.refresh-*' -or $_.Name -like '.completion-*'
})
Add-Check 'temporary-files' ($temporaryFiles.Count -eq 0) "Workspace must not contain abandoned temporary files: $(@($temporaryFiles.Name) -join ', ')"

$completionPath = Join-Path $absoluteWorkspacePath 'completion.json'
if (Test-Path -LiteralPath $completionPath -PathType Leaf) {
    $seal = & (Join-Path $PSScriptRoot 'Complete-LlmWikiTaskWorkspace.ps1') verify `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
    Add-Check 'completion-seal' ([bool]$seal.valid) "Completion seal must be valid. $(@($seal.issues) -join ' ')"
}

$policyImpact = if ($null -ne $descriptor -and [int]$descriptor.schemaVersion -eq $latestWorkspaceSchemaVersion) {
    & (Join-Path $PSScriptRoot 'Compare-LlmWikiTaskPolicy.ps1') `
        -WorkspacePath $normalizedWorkspacePath `
        -Format Json | ConvertFrom-Json
} else { $null }
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $normalizedWorkspacePath
    workspaceSchemaVersion = $(if ($null -ne $descriptor) { [int]$descriptor.schemaVersion } else { $null })
    latestWorkspaceSchemaVersion = $latestWorkspaceSchemaVersion
    migrationRequired = $null -ne $descriptor -and [int]$descriptor.schemaVersion -lt $latestWorkspaceSchemaVersion
    storedPolicyFingerprint = $(if ($null -ne $descriptor) { [string]$descriptor.policyFingerprint } else { '' })
    currentPolicyFingerprint = [string]$workspacePolicySnapshot.fingerprint
    policyDrift = $null -ne $descriptor -and
        [int]$descriptor.schemaVersion -eq $latestWorkspaceSchemaVersion -and
        [string]$descriptor.policyFingerprint -cne [string]$workspacePolicySnapshot.fingerprint
    policyImpact = $policyImpact
    valid = $errors.Count -eq 0
    checkCount = $checks.Count
    passedCount = @($checks | Where-Object status -eq 'pass').Count
    warningCount = $warnings.Count
    errorCount = $errors.Count
    errors = @($errors)
    warnings = @($warnings)
    checks = @($checks)
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
} else {
    Write-Host "Task doctor: valid=$($result.valid), checks=$($result.checkCount), errors=$($result.errorCount), warnings=$($result.warningCount)"
    foreach ($check in $checks) { Write-Host " - [$($check.status)] $($check.id): $($check.message)" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
