[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('preview', 'approve', 'apply', 'show', 'verify', 'rollback')]
    [string]$Action = 'preview',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [string]$Reason,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$applicationPolicy = $policy.scheduler.contextBundles.strategyApplication
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') {
    throw 'WorkspacePath must identify one non-hidden task workspace.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace 'workspace.json') -PathType Leaf)) {
    throw "Task workspace does not exist: $workspace"
}
$experimentPath = Join-Path $absoluteWorkspace 'context-experiment.json'
$approvalPath = Join-Path $absoluteWorkspace 'context-strategy-approval.json'
$applicationPath = Join-Path $absoluteWorkspace 'context-strategy-application.json'

function Get-Hash([object]$Value) {
    # PowerShell 5.1 can spend minutes traversing extended type metadata at very
    # high JSON depths. Five levels fully cover the canonical hash payload.
    $json = ConvertTo-Json -InputObject $Value -Depth 5 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-BytesSha([byte[]]$Value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        ([BitConverter]::ToString($sha.ComputeHash($Value)) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}
function Get-ApprovalPayload([object]$Approval) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Approval.schemaVersion
        workspace = [string]$Approval.workspace
        approvedAtUtc = ([DateTimeOffset]$Approval.approvedAtUtc).ToUniversalTime().ToString('o')
        experimentReceiptHash = [string]$Approval.experimentReceiptHash
        variantId = [string]$Approval.variantId
        itemLimit = [int]$Approval.itemLimit
        characterBudget = [int]$Approval.characterBudget
        reason = [string]$Approval.reason
        policyFingerprint = [string]$Approval.policyFingerprint
    }
}
function Get-ExperimentPayload([object]$Experiment) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Experiment.schemaVersion
        workspace = [string]$Experiment.workspace
        createdAtUtc = ([DateTimeOffset]$Experiment.createdAtUtc).ToUniversalTime().ToString('o')
        policyFingerprint = [string]$Experiment.policyFingerprint
        generatorFingerprint = [string]$Experiment.generatorFingerprint
        inputs = $Experiment.inputs
        plan = @($Experiment.plan)
        results = @($Experiment.results)
        recommendation = $Experiment.recommendation
    }
}
function Get-ApplicationPayload([object]$Application) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Application.schemaVersion
        workspace = [string]$Application.workspace
        state = [string]$Application.state
        appliedAtUtc = ([DateTimeOffset]$Application.appliedAtUtc).ToUniversalTime().ToString('o')
        policyFingerprint = [string]$Application.policyFingerprint
        generatorFingerprint = [string]$Application.generatorFingerprint
        experiment = Get-ExperimentPayload $Application.experiment
        approval = Get-ApprovalPayload $Application.approval
        # Snapshot contents can be large. Their byte hashes are canonical and are
        # validated separately, so the application hash stays bounded and fast.
        baseline = [pscustomobject][ordered]@{
            qualityScore = [double]$Application.baseline.qualityScore
            artifacts = @($Application.baseline.artifacts | ForEach-Object {
                [pscustomobject][ordered]@{ name = [string]$_.name; sha256 = [string]$_.sha256 }
            })
        }
        applied = $Application.applied
        postApply = $Application.postApply
        rollback = $(if ($null -eq $Application.rollback) {
            $null
        } else {
            [pscustomobject][ordered]@{
                rolledBackAtUtc = ([DateTimeOffset]$Application.rollback.rolledBackAtUtc).ToUniversalTime().ToString('o')
                reason = [string]$Application.rollback.reason
                restoredBundleHash = [string]$Application.rollback.restoredBundleHash
            }
        })
    }
}
function Assert-Reason([string]$Value) {
    if (-not [bool]$applicationPolicy.approvalRequiresHumanReason) { return }
    $wordCount = @([regex]::Matches(([string]$Value).Trim(), '\S+')).Count
    if ($wordCount -lt [int]$applicationPolicy.minimumApprovalReasonWords) {
        throw "Reason must contain at least $($applicationPolicy.minimumApprovalReasonWords) words."
    }
}
function Get-VerifiedExperiment {
    if (-not (Test-Path -LiteralPath $experimentPath -PathType Leaf)) { throw 'context-experiment.json is absent.' }
    $validation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextExperiment.ps1') verify `
        -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if (-not $validation.valid) { throw "Context experiment is invalid: $(@($validation.issues) -join ' ')" }
    if ([string]$validation.receipt.recommendation.verdict -eq 'no-safe-variant') { throw 'Context experiment has no safe recommended variant.' }
    $recommendedResult = $validation.receipt.results | Where-Object id -eq $validation.receipt.recommendation.variantId | Select-Object -First 1
    if ($null -eq $recommendedResult -or -not [bool]$recommendedResult.adoptionEligible) {
        throw 'Context experiment recommendation is blocked from adoption.'
    }
    $validation.receipt
}
function Test-Approval([object]$Approval, [object]$Experiment) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([int]$Approval.schemaVersion -ne 1) { $issues.Add('Approval schemaVersion must be 1.') }
    if ([string]$Approval.workspace -cne $workspace) { $issues.Add('Approval workspace does not match.') }
    if ([string]$Approval.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Strategy application policy drifted.') }
    if ([string]$Approval.experimentReceiptHash -cne [string]$Experiment.receiptHash) { $issues.Add('Approved experiment drifted.') }
    if ([string]$Approval.variantId -cne [string]$Experiment.recommendation.variantId -or
        [int]$Approval.itemLimit -ne [int]$Experiment.recommendation.itemLimit -or
        [int]$Approval.characterBudget -ne [int]$Experiment.recommendation.characterBudget) {
        $issues.Add('Approved strategy does not match the experiment recommendation.')
    }
    try { Assert-Reason ([string]$Approval.reason) } catch { $issues.Add($_.Exception.Message) }
    if ([string]$Approval.approvalHash -cne (Get-Hash (Get-ApprovalPayload $Approval))) { $issues.Add('Strategy approval hash is invalid.') }
    @($issues)
}
function Read-Snapshot([string]$Name) {
    $path = Join-Path $absoluteWorkspace $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Strategy input is absent: $workspace/$Name" }
    $bytes = [IO.File]::ReadAllBytes($path)
    [pscustomobject][ordered]@{
        name = $Name
        sha256 = Get-FileSha $path
        contentBase64 = [Convert]::ToBase64String($bytes)
    }
}
function Restore-Snapshot([object]$Snapshot) {
    $path = Join-Path $absoluteWorkspace ([string]$Snapshot.name)
    [IO.File]::WriteAllBytes($path, [Convert]::FromBase64String([string]$Snapshot.contentBase64))
    if ((Get-FileSha $path) -cne [string]$Snapshot.sha256) { throw "Unable to restore $($Snapshot.name)." }
}
function Test-Application([object]$Application) {
    $issues = [Collections.Generic.List[string]]::new()
    if ([int]$Application.schemaVersion -ne 1) { $issues.Add('Application schemaVersion must be 1.') }
    if ([string]$Application.workspace -cne $workspace) { $issues.Add('Application workspace does not match.') }
    if ([string]$Application.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Strategy application policy drifted.') }
    if ([string]$Application.generatorFingerprint -cne (Get-FileSha $PSCommandPath)) { $issues.Add('Strategy application generator changed.') }
    if ([string]$Application.applicationHash -cne (Get-Hash (Get-ApplicationPayload $Application))) { $issues.Add('Strategy application hash is invalid.') }
    if ([string]$Application.experiment.receiptHash -cne (Get-Hash ([pscustomobject][ordered]@{
        schemaVersion = $Application.experiment.schemaVersion
        workspace = $Application.experiment.workspace
        createdAtUtc = $Application.experiment.createdAtUtc
        policyFingerprint = $Application.experiment.policyFingerprint
        generatorFingerprint = $Application.experiment.generatorFingerprint
        inputs = $Application.experiment.inputs
        plan = @($Application.experiment.plan)
        results = @($Application.experiment.results)
        recommendation = $Application.experiment.recommendation
    }))) { $issues.Add('Embedded experiment receipt is invalid.') }
    $approvalIssues = @(Test-Approval $Application.approval $Application.experiment)
    foreach ($issue in $approvalIssues) { $issues.Add($issue) }
    foreach ($snapshot in @($Application.baseline.artifacts)) {
        try {
            $snapshotBytes = [Convert]::FromBase64String([string]$snapshot.contentBase64)
            if ([string]$snapshot.sha256 -cne (Get-BytesSha $snapshotBytes)) {
                $issues.Add("Baseline snapshot content is invalid: $($snapshot.name).")
            }
        } catch { $issues.Add("Baseline snapshot encoding is invalid: $($snapshot.name).") }
    }
    $expectedHashes = if ([string]$Application.state -eq 'rolled-back') {
        $Application.baseline.artifacts
    } else {
        $Application.applied.artifacts
    }
    foreach ($artifact in @($expectedHashes)) {
        $path = Join-Path $absoluteWorkspace ([string]$artifact.name)
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or (Get-FileSha $path) -cne [string]$artifact.sha256) {
            $issues.Add("Current strategy artifact drifted: $($artifact.name).")
        }
    }
    if ([string]$Application.state -notin @('applied', 'rollback-recommended', 'rolled-back')) {
        $issues.Add('Unknown strategy application state.')
    }
    if ([string]$Application.state -eq 'rollback-recommended' -and @($Application.postApply.failedGates).Count -eq 0) {
        $issues.Add('Rollback recommendation requires at least one failed post-apply gate.')
    }
    if ([string]$Application.state -eq 'rolled-back' -and [string]::IsNullOrWhiteSpace([string]$Application.rollback.reason)) {
        $issues.Add('Rolled-back strategy requires a rollback reason.')
    }
    @($issues)
}

$resultObject = $null
$issues = @()
$savedPath = $null
if ($Action -eq 'preview') {
    $experiment = Get-VerifiedExperiment
    $resultObject = [pscustomobject][ordered]@{
        workspace = $workspace
        experimentReceiptHash = [string]$experiment.receiptHash
        recommendation = $experiment.recommendation
        expected = $experiment.results | Where-Object id -eq $experiment.recommendation.variantId | Select-Object -First 1
        requiresApproval = [bool]$applicationPolicy.approvalRequiresHumanReason
        approvalPresent = Test-Path -LiteralPath $approvalPath -PathType Leaf
    }
} elseif ($Action -eq 'approve') {
    Assert-Reason $Reason
    $experiment = Get-VerifiedExperiment
    $approval = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $workspace
        approvedAtUtc = [DateTime]::UtcNow.ToString('o')
        experimentReceiptHash = [string]$experiment.receiptHash
        variantId = [string]$experiment.recommendation.variantId
        itemLimit = [int]$experiment.recommendation.itemLimit
        characterBudget = [int]$experiment.recommendation.characterBudget
        reason = $Reason.Trim()
        policyFingerprint = Get-FileSha $policyPath
        approvalHash = ''
    }
    $approval.approvalHash = Get-Hash (Get-ApprovalPayload $approval)
    $issues = @(Test-Approval $approval $experiment)
    if ($issues.Count -eq 0) {
        [IO.File]::WriteAllText($approvalPath, (($approval | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        $savedPath = "$workspace/context-strategy-approval.json"
    }
    $resultObject = $approval
} elseif ($Action -eq 'apply') {
    if (Test-Path -LiteralPath $applicationPath -PathType Leaf) { throw 'A strategy application already exists; verify or rollback it first.' }
    $experiment = Get-VerifiedExperiment
    if (-not (Test-Path -LiteralPath $approvalPath -PathType Leaf)) { throw 'Strategy approval is absent.' }
    $approval = Get-Content -LiteralPath $approvalPath -Raw | ConvertFrom-Json
    $approvalIssues = @(Test-Approval $approval $experiment)
    if ($approvalIssues.Count -gt 0) { throw "Strategy approval is invalid: $($approvalIssues -join ' ')" }
    $baselineSnapshots = @(
        Read-Snapshot 'context-security.json'
        Read-Snapshot 'context-bundle.json'
        Read-Snapshot 'context-budget.json'
    )
    Write-Verbose 'Captured baseline context artifacts.'
    $expected = $experiment.results | Where-Object id -eq $experiment.recommendation.variantId | Select-Object -First 1
    $baselineResult = $experiment.results | Where-Object id -eq 'baseline' | Select-Object -First 1
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBundle.ps1') create `
        -WorkspacePath $workspace `
        -Limit ([int]$approval.itemLimit) `
        -CharacterBudget ([int]$approval.characterBudget) `
        -Format Json | Out-Null
    Write-Verbose 'Created the approved context bundle.'
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBudget.ps1') create `
        -WorkspacePath $workspace `
        -Format Json | Out-Null
    Write-Verbose 'Created the approved context budget receipt.'
    $postBenchmark = & (Join-Path $PSScriptRoot 'Manage-LlmWikiContextBenchmark.ps1') compare `
        -SourceWorkspacePath $workspace `
        -WorkspacePath $workspace `
        -Format Json | ConvertFrom-Json
    Write-Verbose 'Completed the post-apply context benchmark.'
    $currentBundle = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'context-bundle.json') -Raw | ConvertFrom-Json
    $currentBudget = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'context-budget.json') -Raw | ConvertFrom-Json
    $actualQuality = [double]$postBenchmark.receipt.candidate.qualityScore
    $failedGates = [Collections.Generic.List[string]]::new()
    if ([Math]::Abs($actualQuality - [double]$expected.qualityScore) -gt [double]$applicationPolicy.maximumQualityDeviationPoints) {
        $failedGates.Add('quality-reproduction')
    }
    if ([int]$currentBundle.security.findingCount -gt ([int]$baselineResult.securityFindingCount + [int]$applicationPolicy.maximumSecurityFindingIncrease)) {
        $failedGates.Add('security-findings')
    }
    if ([int]$currentBundle.security.quarantineMatchCount -gt ([int]$baselineResult.quarantineMatchCount + [int]$applicationPolicy.maximumQuarantineMatchIncrease)) {
        $failedGates.Add('quarantine-matches')
    }
    $appliedArtifacts = @(
        [pscustomobject][ordered]@{ name = 'context-security.json'; sha256 = Get-FileSha (Join-Path $absoluteWorkspace 'context-security.json') }
        [pscustomobject][ordered]@{ name = 'context-bundle.json'; sha256 = Get-FileSha (Join-Path $absoluteWorkspace 'context-bundle.json') }
        [pscustomobject][ordered]@{ name = 'context-budget.json'; sha256 = Get-FileSha (Join-Path $absoluteWorkspace 'context-budget.json') }
    )
    $application = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $workspace
        state = $(if ($failedGates.Count -gt 0) { 'rollback-recommended' } else { 'applied' })
        appliedAtUtc = [DateTime]::UtcNow.ToString('o')
        policyFingerprint = Get-FileSha $policyPath
        generatorFingerprint = Get-FileSha $PSCommandPath
        experiment = $experiment
        approval = $approval
        baseline = [pscustomobject][ordered]@{ qualityScore = [double]$baselineResult.qualityScore; artifacts = $baselineSnapshots }
        applied = [pscustomobject][ordered]@{
            variantId = [string]$approval.variantId
            itemLimit = [int]$approval.itemLimit
            characterBudget = [int]$approval.characterBudget
            qualityScore = $actualQuality
            bundleHash = [string]$currentBundle.bundleHash
            budgetReceiptHash = [string]$currentBudget.receiptHash
            artifacts = $appliedArtifacts
        }
        postApply = [pscustomobject][ordered]@{
            expectedQualityScore = [double]$expected.qualityScore
            actualQualityScore = $actualQuality
            qualityDeviation = [Math]::Round($actualQuality - [double]$expected.qualityScore, 2)
            securityFindingCount = [int]$currentBundle.security.findingCount
            quarantineMatchCount = [int]$currentBundle.security.quarantineMatchCount
            failedGates = @($failedGates)
        }
        rollback = $null
        applicationHash = ''
    }
    Write-Verbose 'Built the context strategy application receipt.'
    $application.applicationHash = Get-Hash (Get-ApplicationPayload $application)
    Write-Verbose 'Hashed the context strategy application receipt.'
    $issues = @(Test-Application $application)
    Write-Verbose 'Validated the context strategy application receipt.'
    if ($issues.Count -eq 0) {
        [IO.File]::WriteAllText($applicationPath, (($application | ConvertTo-Json -Depth 5) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        [IO.File]::Delete($experimentPath)
        [IO.File]::Delete($approvalPath)
        $savedPath = "$workspace/context-strategy-application.json"
    }
    $resultObject = $application
} elseif ($Action -in @('show', 'verify', 'rollback')) {
    if (-not (Test-Path -LiteralPath $applicationPath -PathType Leaf)) {
        $issues = @('context-strategy-application.json is absent.')
    } else {
        try {
            $application = Get-Content -LiteralPath $applicationPath -Raw | ConvertFrom-Json
            $issues = @(Test-Application $application)
            if ($Action -eq 'rollback' -and $issues.Count -eq 0) {
                if ([string]$application.state -eq 'rolled-back') { throw 'Strategy application is already rolled back.' }
                Assert-Reason $Reason
                foreach ($snapshot in @($application.baseline.artifacts)) { Restore-Snapshot $snapshot }
                $application.state = 'rolled-back'
                $application.rollback = [pscustomobject][ordered]@{
                    rolledBackAtUtc = [DateTime]::UtcNow.ToString('o')
                    reason = $Reason.Trim()
                    restoredBundleHash = [string](Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'context-bundle.json') -Raw | ConvertFrom-Json).bundleHash
                }
                $application.applicationHash = Get-Hash (Get-ApplicationPayload $application)
                $issues = @(Test-Application $application)
                if ($issues.Count -eq 0) {
                    [IO.File]::WriteAllText($applicationPath, (($application | ConvertTo-Json -Depth 5) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
                    $savedPath = "$workspace/context-strategy-application.json"
                }
            }
            $resultObject = $application
        } catch { $issues = @($_.Exception.Message) }
    }
}
$valid = $issues.Count -eq 0 -and $null -ne $resultObject
$result = [pscustomobject][ordered]@{ action = $Action; valid = $valid; strategy = $resultObject; issues = @($issues); savedPath = $savedPath }
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 5
} else {
    Write-Host "Context strategy: action=$Action, valid=$valid"
    if ($null -ne $resultObject.recommendation) { Write-Host "Variant=$($resultObject.recommendation.variantId), items=$($resultObject.recommendation.itemLimit), characters=$($resultObject.recommendation.characterBudget)" }
    if ($null -ne $resultObject.state) { Write-Host "State=$($resultObject.state), variant=$($resultObject.applied.variantId), quality=$($resultObject.applied.qualityScore), failedGates=$(@($resultObject.postApply.failedGates).Count), hash=$($resultObject.applicationHash)" }
    foreach ($issue in @($issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $valid) { exit 1 }
