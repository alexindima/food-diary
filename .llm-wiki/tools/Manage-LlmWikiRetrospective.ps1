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
$retrospectivePolicy = $policy.scheduler.retrospective
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$receiptPath = Join-Path $absoluteWorkspace 'retrospective.json'
$completionPath = Join-Path $absoluteWorkspace 'completion.json'
if (-not (Test-Path -LiteralPath $completionPath -PathType Leaf)) {
    throw 'Retrospective requires a sealed completion.json.'
}

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 50 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileSha([string]$Value) {
    (Get-FileHash -LiteralPath $Value -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-Payload([object]$Retrospective) {
    [pscustomobject][ordered]@{
        schemaVersion = $Retrospective.schemaVersion
        workspace = $Retrospective.workspace
        createdAtUtc = $Retrospective.createdAtUtc
        completionFingerprint = $Retrospective.completionFingerprint
        packetFingerprint = $Retrospective.packetFingerprint
        policyFingerprint = $Retrospective.policyFingerprint
        inputs = $Retrospective.inputs
        outcome = $Retrospective.outcome
        learningCandidates = @($Retrospective.learningCandidates)
        summary = $Retrospective.summary
    }
}
function Add-Candidate(
    [Collections.Generic.List[object]]$Candidates,
    [string]$Id,
    [string]$Type,
    [string]$Statement,
    [string]$Rationale,
    [int]$Score,
    [object[]]$Evidence,
    [string[]]$Tags,
    [object]$Data = $null
) {
    if ($Candidates.Count -ge [int]$retrospectivePolicy.maximumLearningCandidates) { return }
    $Candidates.Add([pscustomobject][ordered]@{
        id = $Id
        type = $Type
        statement = $Statement
        rationale = $Rationale
        score = $Score
        eligible = $Score -ge [int]$retrospectivePolicy.minimumLearningScore
        evidence = @($Evidence | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
        suggestedTags = @($Tags | Sort-Object -Unique)
        data = $Data
    })
}
function New-Retrospective([string]$CreatedAtUtc, [object[]]$TelemetrySnapshot = $null) {
    $completion = Get-Content -LiteralPath $completionPath -Raw | ConvertFrom-Json
    $packet = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-packet.json') -Raw | ConvertFrom-Json
    $confidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'confidence-ledger.json') -Raw | ConvertFrom-Json
    $critique = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-critique.json') -Raw | ConvertFrom-Json
    $impact = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'impact-simulation.json') -Raw | ConvertFrom-Json
    $prediction = & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') assess -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $cost = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationCost.ps1') assess -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $repair = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLoop.ps1') show -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    $evidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'evidence.json') -Raw | ConvertFrom-Json
    $riskPath = Join-Path $absoluteWorkspace 'risk-calibration.json'
    $risk = if (Test-Path -LiteralPath $riskPath -PathType Leaf) {
        Get-Content -LiteralPath $riskPath -Raw | ConvertFrom-Json
    } else { [pscustomobject]@{ score = 0; level = 'low'; calibrationHash = '' } }
    $contextPath = Join-Path $absoluteWorkspace 'context-security.json'
    $context = if (Test-Path -LiteralPath $contextPath -PathType Leaf) {
        Get-Content -LiteralPath $contextPath -Raw | ConvertFrom-Json
    } else { $null }
    if ($null -eq $TelemetrySnapshot) {
        $telemetry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -Format Json | ConvertFrom-Json
        $TelemetrySnapshot = @($telemetry.metrics | Where-Object checkId -in @($evidence.checks.id) | Sort-Object checkId)
    }

    $failedRepairs = @($repair.registry.attempts | Where-Object state -eq 'failed')
    $completedRepairs = @($repair.registry.attempts | Where-Object state -eq 'completed')
    $falseNegatives = @($prediction.calibration.outcomes | Where-Object classification -eq 'false-negative')
    $falsePositives = @($prediction.calibration.outcomes | Where-Object classification -eq 'false-positive')
    $flakyChecks = @($TelemetrySnapshot | Where-Object flaky)
    $impactFindings = [Collections.Generic.List[object]]::new()
    foreach ($property in @($impact.simulation.comparison.unexpected.PSObject.Properties | Sort-Object Name)) {
        $unexpectedValues = @($property.Value | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
        if ($unexpectedValues.Count -gt 0) {
            $impactFindings.Add([pscustomobject][ordered]@{
                id = "unexpected-$($property.Name)"
                count = $unexpectedValues.Count
                values = @($unexpectedValues | Sort-Object -Unique)
            })
        }
    }
    $costOutcomes = @($cost.calibration.outcomes)
    $costVariance = @($costOutcomes | Where-Object {
        $null -ne $_.forecastErrorPercent -and [Math]::Abs([double]$_.forecastErrorPercent) -ge [double]$retrospectivePolicy.costVarianceWarningPercent
    })
    $quarantinedSources = if ($null -eq $context) { @() } else {
        @($context.sources | Where-Object quarantineCount -gt 0)
    }

    $candidates = [Collections.Generic.List[object]]::new()
    foreach ($finding in $impactFindings) {
        Add-Candidate $candidates "impact-$($finding.id)" 'impact-drift' `
            "Forecast future changes to include impact pattern '$($finding.id)'." `
            "The completed task observed $($finding.count) unexpected impact(s), so its planning prior was incomplete." `
            ([int]$retrospectivePolicy.candidateScores.impactDrift) @($finding.id, "count=$($finding.count)", @($finding.values)) @('architecture', 'impact', 'planning')
    }
    foreach ($item in $falseNegatives) {
        Add-Candidate $candidates "prediction-$($item.checkId)" 'failure-prediction' `
            "Raise predicted failure risk for check '$($item.checkId)' under a similar change profile." `
            'The check failed although the stored prediction was below the configured failure threshold.' `
            ([int]$retrospectivePolicy.candidateScores.falseNegative) @($item.checkId, "probability=$($item.probabilityPercent)") @('verification', 'prediction', 'failure')
    }
    foreach ($attempt in $failedRepairs) {
        Add-Candidate $candidates "repair-$($attempt.id)" 'repair-learning' `
            "Avoid repeating repair fingerprint '$($attempt.attemptFingerprint)' for '$($attempt.checkId)'." `
            "The hypothesis '$($attempt.hypothesis)' ended in a failed repair attempt." `
            ([int]$retrospectivePolicy.candidateScores.failedRepair) @($attempt.id, $attempt.resolution) @('repair', 'failure', [string]$attempt.category)
    }
    foreach ($metric in $flakyChecks) {
        Add-Candidate $candidates "flaky-$($metric.checkId)" 'flaky-verification' `
            "Treat '$($metric.checkId)' as flaky until stable evidence supersedes this history." `
            "Outcome transitions reached $($metric.transitionPercent)% across $($metric.sampleCount) samples." `
            ([int]$retrospectivePolicy.candidateScores.flakyVerification) @($metric.checkId, "transitions=$($metric.transitionPercent)%") @('verification', 'flaky') `
            ([pscustomobject][ordered]@{
                checkId = [string]$metric.checkId
                recommendedSeconds = [double]$metric.medianDurationSeconds
                sampleCount = [int]$metric.sampleCount
                transitionPercent = [double]$metric.transitionPercent
            })
    }
    foreach ($item in $costVariance) {
        $direction = if ([double]$item.forecastErrorPercent -lt 0) { 'underestimated' } else { 'overestimated' }
        Add-Candidate $candidates "cost-$($item.checkId)" 'cost-calibration' `
            "Adjust the expected cost of '$($item.checkId)'; the previous forecast $direction actual effort." `
            "Expected $($item.expectedTotalSeconds)s versus $($item.actualTotalSeconds)s, error $($item.forecastErrorPercent)%." `
            ([int]$retrospectivePolicy.candidateScores.costVariance) @($item.checkId, "error=$($item.forecastErrorPercent)%") @('verification', 'cost', 'calibration') `
            ([pscustomobject][ordered]@{
                checkId = [string]$item.checkId
                recommendedSeconds = [double]$item.actualTotalSeconds
                expectedSeconds = [double]$item.expectedTotalSeconds
                errorPercent = [double]$item.forecastErrorPercent
            })
    }
    foreach ($source in $quarantinedSources) {
        $sourceId = (Get-Hash ([string]$source.path)).Substring(0, 12)
        Add-Candidate $candidates "context-$sourceId" 'context-security' `
            "Keep '$($source.path)' non-authoritative until its quarantined instruction patterns are reviewed." `
            "The completed task quarantined $($source.quarantineCount) instruction-like match(es) from this source." `
            ([int]$retrospectivePolicy.candidateScores.contextQuarantine) @($source.path, "quarantine=$($source.quarantineCount)") @('context', 'security', 'prompt-injection')
    }

    $quality = if ($critique.verdict -eq 'approve' -and [double]$confidence.score -ge 90 -and $failedRepairs.Count -eq 0) {
        'excellent'
    } elseif ($critique.verdict -in @('approve', 'approve-with-notes') -and [double]$confidence.score -ge 70) {
        'good'
    } elseif ($completion.readiness.verdict -eq 'ready') { 'mixed' } else { 'poor' }
    $outcome = [pscustomobject][ordered]@{
        completionVerdict = [string]$completion.readiness.verdict
        readinessScore = [double]$completion.readiness.score
        confidenceScore = [double]$confidence.score
        confidenceVerdict = [string]$confidence.verdict
        critiqueScore = [double]$critique.score
        critiqueVerdict = [string]$critique.verdict
        quality = $quality
        risk = [pscustomobject][ordered]@{ level = [string]$risk.level; score = [int]$risk.score }
        impactDriftCount = $impactFindings.Count
        prediction = [pscustomobject][ordered]@{
            resolvedCount = [int]$prediction.calibration.resolvedCount
            falseNegativeCount = $falseNegatives.Count
            falsePositiveCount = $falsePositives.Count
            brierScore = $prediction.calibration.brierScore
        }
        verificationCost = [pscustomobject][ordered]@{
            resolvedCount = [int]$cost.calibration.resolvedCount
            meanAbsoluteErrorSeconds = $cost.calibration.meanAbsoluteErrorSeconds
            materialVarianceCount = $costVariance.Count
        }
        repair = [pscustomobject][ordered]@{
            totalAttempts = @($repair.registry.attempts).Count
            failedAttempts = $failedRepairs.Count
            completedAttempts = $completedRepairs.Count
        }
        flakyCheckCount = $flakyChecks.Count
        quarantinedContextSourceCount = $quarantinedSources.Count
    }
    $artifactHashes = [ordered]@{}
    foreach ($artifact in @(
        'completion.json', 'workspace.json', 'change-packet.json', 'change-manifest.json', 'acceptance-matrix.json',
        'evidence.json', 'risk-calibration.json', 'failure-prediction.json', 'verification-cost.json',
        'impact-simulation.json', 'repair-loop.json', 'context-security.json', 'confidence-ledger.json', 'change-critique.json'
    )) {
        $path = Join-Path $absoluteWorkspace $artifact
        if (Test-Path -LiteralPath $path -PathType Leaf) { $artifactHashes[$artifact] = Get-FileSha $path }
    }
    $orderedCandidates = @($candidates | Sort-Object @{ Expression = 'score'; Descending = $true }, type, id)
    $retrospective = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $workspace
        createdAtUtc = $CreatedAtUtc
        completionFingerprint = [string]$completion.completionFingerprint
        packetFingerprint = [string]$packet.fingerprint
        policyFingerprint = Get-FileSha $policyPath
        inputs = [pscustomobject][ordered]@{
            artifactHashes = [pscustomobject]$artifactHashes
            telemetrySnapshot = @($TelemetrySnapshot)
            telemetrySnapshotHash = Get-Hash @($TelemetrySnapshot)
        }
        outcome = $outcome
        learningCandidates = $orderedCandidates
        summary = [pscustomobject][ordered]@{
            candidateCount = $orderedCandidates.Count
            eligibleCandidateCount = @($orderedCandidates | Where-Object eligible).Count
            strongestCandidateScore = $(if ($orderedCandidates.Count -eq 0) { 0 } else { [int]$orderedCandidates[0].score })
        }
        retrospectiveHash = ''
    }
    $retrospective.retrospectiveHash = Get-Hash (Get-Payload $retrospective)
    $retrospective
}
function Test-Retrospective([object]$Retrospective) {
    $issues = [Collections.Generic.List[string]]::new()
    if ($Retrospective.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Retrospective.workspace -cne $workspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Retrospective.policyFingerprint -cne (Get-FileSha $policyPath)) { $issues.Add('Retrospective policy drifted.') }
    foreach ($property in @($Retrospective.inputs.artifactHashes.PSObject.Properties)) {
        $path = Join-Path $absoluteWorkspace $property.Name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf) -or [string]$property.Value -cne (Get-FileSha $path)) {
            $issues.Add("Retrospective input drifted: $($property.Name).")
        }
    }
    if ([string]$Retrospective.inputs.telemetrySnapshotHash -cne (Get-Hash @($Retrospective.inputs.telemetrySnapshot))) {
        $issues.Add('Retrospective telemetry snapshot hash is invalid.')
    }
    $expected = New-Retrospective ([string]$Retrospective.createdAtUtc) @($Retrospective.inputs.telemetrySnapshot)
    if ((Get-Hash $Retrospective.outcome) -cne (Get-Hash $expected.outcome)) { $issues.Add('Retrospective outcome drifted.') }
    if ((Get-Hash @($Retrospective.learningCandidates)) -cne (Get-Hash @($expected.learningCandidates))) { $issues.Add('Retrospective learning candidates drifted.') }
    if ((Get-Hash $Retrospective.summary) -cne (Get-Hash $expected.summary)) { $issues.Add('Retrospective summary drifted.') }
    if ([string]$Retrospective.retrospectiveHash -cne (Get-Hash (Get-Payload $Retrospective))) { $issues.Add('Retrospective hash is invalid.') }
    @($issues)
}

$retrospective = $null
$issues = @()
$savedPath = $null
if ($Action -in @('show', 'verify')) {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        $issues = @('retrospective.json is absent.')
    } else {
        try {
            $retrospective = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
            $issues = @(Test-Retrospective $retrospective)
        } catch { $issues = @($_.Exception.Message) }
    }
} else {
    $retrospective = New-Retrospective ($AsOfUtc.ToUniversalTime().ToString('o'))
    $issues = @(Test-Retrospective $retrospective)
    if ($Action -eq 'create' -and $issues.Count -eq 0) {
        [IO.File]::WriteAllText($receiptPath, (($retrospective | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        $savedPath = "$workspace/retrospective.json"
    }
}
$result = [pscustomobject][ordered]@{
    action = $Action
    valid = $issues.Count -eq 0
    retrospective = $retrospective
    issues = @($issues)
    savedPath = $savedPath
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 50
} else {
    Write-Host "Task retrospective: action=$Action, valid=$($result.valid)"
    if ($null -ne $retrospective) {
        Write-Host "Quality=$($retrospective.outcome.quality), candidates=$($retrospective.summary.candidateCount), eligible=$($retrospective.summary.eligibleCandidateCount), hash=$($retrospective.retrospectiveHash)"
        foreach ($candidate in @($retrospective.learningCandidates)) {
            Write-Host " - [$($candidate.score)] $($candidate.type)/$($candidate.id): $($candidate.statement)"
        }
    }
    foreach ($issue in @($issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
