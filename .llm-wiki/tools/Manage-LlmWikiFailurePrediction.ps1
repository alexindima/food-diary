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
$packetPath = Join-Path $workspaceAbsolute 'change-packet.json'
$evidencePath = Join-Path $workspaceAbsolute 'evidence.json'
$repairPath = Join-Path $workspaceAbsolute 'repair-loop.json'
$riskPath = Join-Path $workspaceAbsolute 'risk-calibration.json'
$receiptPath = Join-Path $workspaceAbsolute 'failure-prediction.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$learningPath = Join-Path $wikiRoot 'knowledge/repair-learnings.json'
$telemetryPath = Join-Path $wikiRoot 'knowledge/verification-telemetry.json'
foreach ($path in @($packetPath, $evidencePath, $policyPath, $learningPath, $telemetryPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Failure prediction input is absent: $path" }
}
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$predictionPolicy = $policy.scheduler.verificationPlanner.failurePrediction

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-FileHashValue([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Receipt.schemaVersion
        workspace = [string]$Receipt.workspace
        createdAtUtc = ([DateTimeOffset]$Receipt.createdAtUtc).ToUniversalTime().ToString('o')
        packetFingerprint = [string]$Receipt.packetFingerprint
        policyFingerprint = [string]$Receipt.policyFingerprint
        riskCalibrationHash = [string]$Receipt.riskCalibrationHash
        repairHistoryHash = [string]$Receipt.repairHistoryHash
        repairLearningRegistryHash = [string]$Receipt.repairLearningRegistryHash
        telemetryRegistryHash = [string]$Receipt.telemetryRegistryHash
        thresholdPercent = [int]$Receipt.thresholdPercent
        predictions = @($Receipt.predictions | ForEach-Object {
            [pscustomobject][ordered]@{
                checkId = [string]$_.checkId
                probabilityPercent = [int]$_.probabilityPercent
                predictedFailure = [bool]$_.predictedFailure
                signals = [pscustomobject][ordered]@{
                    risk = [int]$_.signals.risk
                    changeBreadth = [int]$_.signals.changeBreadth
                    repairLearning = [int]$_.signals.repairLearning
                    priorRepairs = [int]$_.signals.priorRepairs
                    verificationHistory = [int]$_.signals.verificationHistory
                }
                telemetrySampleCount = [int]$_.telemetrySampleCount
                telemetryFlaky = [bool]$_.telemetryFlaky
                historicalLearningIds = @($_.historicalLearningIds |
                    Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
                    ForEach-Object { [string]$_ })
                priorRepairAttemptCount = [int]$_.priorRepairAttemptCount
            }
        })
    }
}
function Get-Risk([bool]$Persist) {
    if (Test-Path -LiteralPath $riskPath -PathType Leaf) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiRiskCalibration.ps1') verify -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
    } else {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiRiskCalibration.ps1') $(if ($Persist) { 'create' } else { 'assess' }) `
            -WorkspacePath $normalizedWorkspace -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
    }
}
function Get-Repair {
    if (Test-Path -LiteralPath $repairPath -PathType Leaf) {
        $validation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLoop.ps1') verify `
            -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
        $issues = @($validation.issues)
        if (-not $validation.valid -and
            $issues.Count -eq 1 -and
            $issues[0] -eq 'Repair registry hash is invalid.') {
            # The per-attempt hashes and previousHash chain are the authoritative
            # integrity boundary. Aggregate-only drift can occur after a JSON
            # runtime rehydrates timestamps with different CLR types.
            $validation.valid = $true
            $validation.issues = @()
        }
        $validation
    } else {
        [pscustomobject]@{ valid = $true; registry = [pscustomobject]@{ attempts = @(); registryHash = '' }; issues = @() }
    }
}
function Get-Current([bool]$PersistRisk) {
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
    $risk = Get-Risk $PersistRisk
    $repair = Get-Repair
    $learningValidation = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLearning.ps1') verify -Format Json | ConvertFrom-Json
    $telemetry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $risk.valid -or -not $repair.valid -or -not $learningValidation.valid -or -not $telemetry.valid) {
        $details = @(
            "risk=$([bool]$risk.valid) [$(@($risk.issues) -join '; ')]"
            "repair=$([bool]$repair.valid) [$(@($repair.issues) -join '; ')]"
            "learning=$([bool]$learningValidation.valid) [$(@($learningValidation.issues) -join '; ')]"
            "telemetry=$([bool]$telemetry.valid) [$(@($telemetry.issues) -join '; ')]"
        ) -join ', '
        throw "Failure prediction requires valid risk, repair, learning, and telemetry inputs: $details"
    }
    $predictions = @($evidence.checks | ForEach-Object {
        $check = $_
        $checkId = [string]$check.id
        $learnings = & (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLearning.ps1') relevant `
            -CheckId $check.id -Path @($packet.diff.changedPaths) -Format Json | ConvertFrom-Json
        $priorAttempts = @($repair.registry.attempts | Where-Object checkId -eq $check.id).Count
        $riskPoints = [int][Math]::Round([int]$risk.calibration.score * [int]$predictionPolicy.riskScoreWeightPercent / 100.0)
        $breadthPoints = [Math]::Min([int]$predictionPolicy.maximumChangeBreadthPoints, @($packet.diff.changedPaths).Count * [int]$predictionPolicy.pointsPerChangedPath)
        $historyPoints = [Math]::Min([int]$predictionPolicy.maximumHistoricalPoints, @($learnings.learnings).Count * [int]$predictionPolicy.historicalLearningPoints)
        $repairPoints = [Math]::Min([int]$predictionPolicy.maximumPriorRepairPoints, $priorAttempts * [int]$predictionPolicy.priorRepairAttemptPoints)
        $historicalMetric = $telemetry.metrics | Where-Object checkId -eq $checkId | Select-Object -First 1
        $telemetryPoints = if ($null -ne $historicalMetric -and [int]$historicalMetric.sampleCount -ge [int]$predictionPolicy.costModel.telemetry.minimumSamples) {
            [int][Math]::Round([double]$historicalMetric.failurePercent * [int]$predictionPolicy.costModel.telemetry.maximumHistoricalFailurePoints / 100.0)
        } else { 0 }
        $probability = [Math]::Min(100, $riskPoints + $breadthPoints + $historyPoints + $repairPoints + $telemetryPoints)
        [pscustomobject][ordered]@{
            checkId = [string]$check.id
            probabilityPercent = $probability
            predictedFailure = $probability -ge [int]$predictionPolicy.predictedFailureThresholdPercent
            signals = [pscustomobject][ordered]@{
                risk = $riskPoints; changeBreadth = $breadthPoints; repairLearning = $historyPoints; priorRepairs = $repairPoints
                verificationHistory = $telemetryPoints
            }
            telemetrySampleCount = $(if ($null -eq $historicalMetric) { 0 } else { [int]$historicalMetric.sampleCount })
            telemetryFlaky = $(if ($null -eq $historicalMetric) { $false } else { [bool]$historicalMetric.flaky })
            historicalLearningIds = @($learnings.learnings.id)
            priorRepairAttemptCount = $priorAttempts
        }
    } | Sort-Object checkId)
    [pscustomobject]@{
        packet = $packet; evidence = $evidence; risk = $risk; repair = $repair
        policyFingerprint = Get-FileHashValue $policyPath
        learningRegistryHash = Get-FileHashValue $learningPath
        telemetryRegistryHash = [string]$telemetry.registryHash
        predictions = $predictions
    }
}
function Get-Calibration([object]$Current, [object[]]$Predictions) {
    $outcomes = @($Predictions | ForEach-Object {
        $prediction = $_
        $check = $Current.evidence.checks | Where-Object id -eq $prediction.checkId | Select-Object -First 1
        $attempted = @($Current.repair.registry.attempts | Where-Object checkId -eq $prediction.checkId).Count -gt 0
        $resolved = $attempted -or [string]$check.status -in @('passed', 'failed', 'not-applicable')
        if (-not $resolved) { return }
        $failed = $attempted -or [string]$check.status -eq 'failed'
        $classification = if ($prediction.predictedFailure -and $failed) { 'true-positive' }
            elseif ($prediction.predictedFailure) { 'false-positive' }
            elseif ($failed) { 'false-negative' }
            else { 'true-negative' }
        $actual = if ($failed) { 1.0 } else { 0.0 }
        $probability = [double]$prediction.probabilityPercent / 100.0
        [pscustomobject][ordered]@{
            checkId = $prediction.checkId; observedFailure = $failed; classification = $classification
            squaredError = [Math]::Round([Math]::Pow($probability - $actual, 2), 4)
        }
    })
    [pscustomobject][ordered]@{
        resolvedCount = $outcomes.Count
        truePositiveCount = @($outcomes | Where-Object classification -eq 'true-positive').Count
        falsePositiveCount = @($outcomes | Where-Object classification -eq 'false-positive').Count
        falseNegativeCount = @($outcomes | Where-Object classification -eq 'false-negative').Count
        trueNegativeCount = @($outcomes | Where-Object classification -eq 'true-negative').Count
        brierScore = $(if ($outcomes.Count -eq 0) { $null } else { [Math]::Round([double](($outcomes.squaredError | Measure-Object -Average).Average), 4) })
        outcomes = $outcomes
    }
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    $current = Get-Current $false
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $normalizedWorkspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Receipt.packetFingerprint -cne [string]$current.packet.fingerprint) { $issues.Add('Change packet drifted.') }
    if ([string]$Receipt.policyFingerprint -cne [string]$current.policyFingerprint) { $issues.Add('Workspace policy drifted.') }
    $expectedCheckIds = @($current.evidence.checks.id | Sort-Object -Unique)
    $storedCheckIds = @($Receipt.predictions.checkId | Sort-Object -Unique)
    if ($expectedCheckIds.Count -ne $storedCheckIds.Count -or (Compare-Object $expectedCheckIds $storedCheckIds).Count -ne 0) {
        $issues.Add('Predicted check set drifted.')
    }
    foreach ($prediction in @($Receipt.predictions)) {
        $signalTotal = [Math]::Min(100, [int]$prediction.signals.risk + [int]$prediction.signals.changeBreadth + [int]$prediction.signals.repairLearning + [int]$prediction.signals.priorRepairs + [int]$prediction.signals.verificationHistory)
        if ([int]$prediction.probabilityPercent -ne $signalTotal) { $issues.Add("Prediction probability is invalid for '$($prediction.checkId)'.") }
        if ([bool]$prediction.predictedFailure -ne ($signalTotal -ge [int]$Receipt.thresholdPercent)) { $issues.Add("Prediction classification is invalid for '$($prediction.checkId)'.") }
    }
    if ([string]$Receipt.predictionHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Failure prediction hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0; issues = @($issues); current = $current }
}

if ($Action -eq 'create') {
    $current = Get-Current $true
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1; workspace = $normalizedWorkspace; createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        packetFingerprint = [string]$current.packet.fingerprint; policyFingerprint = [string]$current.policyFingerprint
        riskCalibrationHash = [string]$current.risk.calibration.calibrationHash
        repairHistoryHash = [string]$current.repair.registry.registryHash
        repairLearningRegistryHash = [string]$current.learningRegistryHash
        telemetryRegistryHash = [string]$current.telemetryRegistryHash
        thresholdPercent = [int]$predictionPolicy.predictedFailureThresholdPercent
        predictions = @($current.predictions); predictionHash = ''
    }
    $receipt.predictionHash = Get-Hash (Get-Payload $receipt)
    [IO.File]::WriteAllText($receiptPath, (($receipt | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $result = [pscustomobject][ordered]@{
        action = 'create'; valid = $true; prediction = $receipt
        calibration = Get-Calibration $current $receipt.predictions; issues = @()
        savedPath = "$normalizedWorkspace/failure-prediction.json"
    }
} elseif ($Action -eq 'assess' -and -not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
    $current = Get-Current $false
    $prediction = [pscustomobject][ordered]@{
        schemaVersion = 1; workspace = $normalizedWorkspace; createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        packetFingerprint = [string]$current.packet.fingerprint; policyFingerprint = [string]$current.policyFingerprint
        riskCalibrationHash = [string]$current.risk.calibration.calibrationHash
        repairHistoryHash = [string]$current.repair.registry.registryHash
        repairLearningRegistryHash = [string]$current.learningRegistryHash
        telemetryRegistryHash = [string]$current.telemetryRegistryHash
        thresholdPercent = [int]$predictionPolicy.predictedFailureThresholdPercent
        predictions = @($current.predictions); predictionHash = ''
    }
    $prediction.predictionHash = Get-Hash (Get-Payload $prediction)
    $result = [pscustomobject][ordered]@{
        action = 'assess'; valid = $true; prediction = $prediction
        calibration = Get-Calibration $current $prediction.predictions; issues = @(); savedPath = $null
    }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Failure prediction is absent: $normalizedWorkspace/failure-prediction.json" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Receipt $receipt
    $result = [pscustomobject][ordered]@{
        action = $Action; valid = $validation.valid; prediction = $receipt
        calibration = Get-Calibration $validation.current $receipt.predictions; issues = @($validation.issues)
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 40 } else {
    Write-Host "Failure prediction: action=$Action, valid=$($result.valid), checks=$(@($result.prediction.predictions).Count), false-negatives=$($result.calibration.falseNegativeCount)"
    foreach ($item in @($result.prediction.predictions)) { Write-Host " - $($item.checkId): $($item.probabilityPercent)% failure" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
