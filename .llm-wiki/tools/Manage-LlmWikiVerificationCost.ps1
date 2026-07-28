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
$workspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') {
    throw 'WorkspacePath must identify one workspace directly inside .artifacts/llm-wiki/tasks.'
}
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$packetPath = Join-Path $absoluteWorkspace 'change-packet.json'
$evidencePath = Join-Path $absoluteWorkspace 'evidence.json'
$repairPath = Join-Path $absoluteWorkspace 'repair-loop.json'
$predictionPath = Join-Path $absoluteWorkspace 'failure-prediction.json'
$receiptPath = Join-Path $absoluteWorkspace 'verification-cost.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
foreach ($path in @($packetPath, $evidencePath, $policyPath)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Verification cost input is absent: $path" }
}
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$costPolicy = $policy.scheduler.verificationPlanner.failurePrediction.costModel

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 40 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-Category([string]$Id) {
    if ($Id -match '(?i)compile|build') { return 'compile' }
    if ($Id -match '(?i)format|whitespace') { return 'format' }
    if ($Id -match '(?i)lint|eslint') { return 'lint' }
    if ($Id -match '(?i)contract|compatib|snapshot') { return 'contract' }
    if ($Id -match '(?i)architecture|dependency') { return 'architecture' }
    if ($Id -match '(?i)docker|database|migration|network|infrastructure') { return 'infrastructure' }
    if ($Id -match '(?i)test|spec|verify') { return 'test' }
    'unknown'
}
function Get-VerificationSeconds([string]$Id) {
    foreach ($rule in @($costPolicy.verificationSecondsByPattern)) {
        if ($Id -match [string]$rule.pattern) { return [int]$rule.seconds }
    }
    [int]$costPolicy.defaultVerificationSeconds
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Receipt.schemaVersion
        workspace = [string]$Receipt.workspace
        createdAtUtc = ([DateTimeOffset]$Receipt.createdAtUtc).ToUniversalTime().ToString('o')
        packetFingerprint = [string]$Receipt.packetFingerprint
        policyFingerprint = [string]$Receipt.policyFingerprint
        predictionHash = [string]$Receipt.predictionHash
        telemetryRegistryHash = [string]$Receipt.telemetryRegistryHash
        learningRegistryFingerprint = [string]$Receipt.learningRegistryFingerprint
        experimentRegistryFingerprint = [string]$Receipt.experimentRegistryFingerprint
        appliedLearningSnapshot = @($Receipt.appliedLearningSnapshot)
        estimates = @($Receipt.estimates); totals = $Receipt.totals
    }
}
function Get-Prediction([bool]$Persist) {
    if (Test-Path -LiteralPath $predictionPath -PathType Leaf) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    } else {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiFailurePrediction.ps1') $(if ($Persist) { 'create' } else { 'assess' }) `
            -WorkspacePath $workspace -AsOfUtc $AsOfUtc -Format Json | ConvertFrom-Json
    }
}
function Get-Forecast([bool]$PersistPrediction) {
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $prediction = Get-Prediction $PersistPrediction
    if (-not $prediction.valid) { throw "Failure prediction is invalid: $(@($prediction.issues) -join ' ')" }
    $telemetry = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $telemetry.valid) { throw "Verification telemetry is invalid: $(@($telemetry.issues) -join ' ')" }
    $learning = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningPromotion.ps1') list -Format Json | ConvertFrom-Json
    if (-not $learning.valid) { throw "Learning-promotion registry is invalid: $(@($learning.issues) -join ' ')" }
    $appliedCalibrations = @($learning.candidates | Where-Object {
        $_.decision -eq 'approved' -and
        $_.materialization -eq 'applied' -and
        $_.application.target -eq 'verification-calibration'
    })
    $experiments = & (Join-Path $PSScriptRoot 'Manage-LlmWikiLearningExperiment.ps1') active -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    if (-not $experiments.valid) { throw "Learning-experiment registry is invalid: $(@($experiments.issues) -join ' ')" }
    $canaryCalibrations = @($experiments.experiments | Where-Object {
        $_.canary.application.target -eq 'verification-calibration'
    } | ForEach-Object {
        [pscustomobject][ordered]@{
            id = [string]$_.candidateId
            application = $_.canary.application
            decidedAtUtc = $null
            source = 'canary'
            experimentEventHash = [string]$_.canaryEventHash
        }
    })
    $effectiveCalibrations = @($appliedCalibrations) + @($canaryCalibrations)
    $estimates = @($prediction.prediction.predictions | ForEach-Object {
        $checkId = [string]$_.checkId
        $category = Get-Category ([string]$_.checkId)
        $policySeconds = Get-VerificationSeconds ([string]$_.checkId)
        $historical = $telemetry.metrics | Where-Object checkId -eq $checkId | Select-Object -First 1
        $useHistory = $null -ne $historical -and [int]$historical.sampleCount -ge [int]$costPolicy.telemetry.minimumSamples
        $verificationSeconds = if ($useHistory) {
            [int][Math]::Round(
                [double]$policySeconds * (100 - [int]$costPolicy.telemetry.historicalBlendPercent) / 100.0 +
                [double]$historical.medianDurationSeconds * [int]$costPolicy.telemetry.historicalBlendPercent / 100.0)
        } else { $policySeconds }
        $learningOverrides = @($effectiveCalibrations | Where-Object { $checkId -in @($_.application.subjectIds) })
        if ($learningOverrides.Count -gt 0) {
            $verificationSeconds = [int][Math]::Round([double](($learningOverrides.application.recommendedSeconds | Measure-Object -Average).Average))
        }
        $verificationSeconds = [Math]::Max(1, $verificationSeconds)
        $repairSeconds = [int]$costPolicy.repairSecondsByCategory.$category
        $expectedFailureSeconds = [Math]::Round($repairSeconds * [int]$_.probabilityPercent / 100.0, 2)
        $density = [Math]::Round($expectedFailureSeconds / $verificationSeconds, 4)
        $boost = [Math]::Min([int]$costPolicy.maximumPriorityBoost, [int][Math]::Floor($density * [double]$costPolicy.valueDensityBoostMultiplier))
        [pscustomobject][ordered]@{
            checkId = $_.checkId; category = $category; failureProbabilityPercent = [int]$_.probabilityPercent
            verificationSeconds = $verificationSeconds; repairSeconds = $repairSeconds
            verificationCostSource = $(if ($learningOverrides.Count -gt 0) { 'approved-learning' } elseif ($useHistory) { 'blended-history' } else { 'policy' })
            learningCandidateIds = @($learningOverrides.id | Sort-Object -Unique)
            telemetrySampleCount = $(if ($null -eq $historical) { 0 } else { [int]$historical.sampleCount })
            telemetryMedianDurationSeconds = $(if ($null -eq $historical) { $null } else { [double]$historical.medianDurationSeconds })
            telemetryFlaky = $(if ($null -eq $historical) { $false } else { [bool]$historical.flaky })
            expectedFailureSeconds = $expectedFailureSeconds
            expectedTotalSeconds = [Math]::Round($verificationSeconds + $expectedFailureSeconds, 2)
            valueDensity = $density; priorityBoost = $boost
        }
    } | Sort-Object checkId)
    [pscustomobject]@{
        packet = $packet
        prediction = $prediction
        telemetry = $telemetry
        learning = $learning
        experiments = $experiments
        appliedLearningSnapshot = @($effectiveCalibrations | ForEach-Object {
            [pscustomobject][ordered]@{
                id = [string]$_.id
                application = $_.application
                decidedAtUtc = $_.decidedAtUtc
                source = $(if ($null -ne $_.source) { [string]$_.source } else { 'applied' })
                experimentEventHash = $(if ($null -ne $_.experimentEventHash) { [string]$_.experimentEventHash } else { '' })
            }
        })
        estimates = $estimates
    }
}
function Get-Calibration([object[]]$Estimates) {
    $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
    $repairs = if (Test-Path -LiteralPath $repairPath -PathType Leaf) {
        (& (Join-Path $PSScriptRoot 'Manage-LlmWikiRepairLoop.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json).registry.attempts
    } else { @() }
    $outcomes = @($estimates | ForEach-Object {
        $estimate = $_
        $check = $evidence.checks | Where-Object id -eq $estimate.checkId | Select-Object -First 1
        $attempts = @($repairs | Where-Object checkId -eq $estimate.checkId)
        if ([string]$check.status -notin @('passed', 'failed', 'not-applicable') -and $attempts.Count -eq 0) { return }
        $verificationActual = [double]$check.durationSeconds
        $repairActual = [double](($attempts | ForEach-Object {
            if ([string]::IsNullOrWhiteSpace([string]$_.finishedAtUtc)) { 0 } else {
                $finished = [DateTime]$_.finishedAtUtc
                $started = [DateTime]$_.startedAtUtc
                ($finished - $started).TotalSeconds
            }
        } | Measure-Object -Sum).Sum)
        $actual = [Math]::Max(0, [Math]::Round($verificationActual + $repairActual, 2))
        $error = if ($actual -le 0) { $null } else { [Math]::Round(([double]$estimate.expectedTotalSeconds - $actual) / $actual * 100, 2) }
        [pscustomobject][ordered]@{
            checkId = $estimate.checkId; expectedTotalSeconds = $estimate.expectedTotalSeconds
            actualTotalSeconds = $actual; forecastErrorPercent = $error
        }
    })
    [pscustomobject][ordered]@{
        resolvedCount = $outcomes.Count
        meanAbsoluteErrorSeconds = $(if ($outcomes.Count -eq 0) { $null } else {
            [Math]::Round([double](($outcomes | ForEach-Object { [Math]::Abs($_.expectedTotalSeconds - $_.actualTotalSeconds) } | Measure-Object -Average).Average), 2)
        })
        outcomes = $outcomes
    }
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $workspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Receipt.packetFingerprint -cne [string]$packet.fingerprint) { $issues.Add('Change packet drifted.') }
    if ([string]$Receipt.policyFingerprint -cne (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()) { $issues.Add('Workspace policy drifted.') }
    $allowedCategories = @('compile', 'test', 'format', 'lint', 'contract', 'architecture', 'infrastructure', 'unknown')
    foreach ($estimate in @($Receipt.estimates)) {
        if ([string]::IsNullOrWhiteSpace([string]$estimate.checkId)) { $issues.Add('Cost estimate checkId must be non-empty.'); continue }
        if ([string]$estimate.category -notin $allowedCategories) { $issues.Add("Cost category is invalid for '$($estimate.checkId)'.") }
        if ([int]$estimate.failureProbabilityPercent -lt 0 -or [int]$estimate.failureProbabilityPercent -gt 100) { $issues.Add("Failure probability is invalid for '$($estimate.checkId)'.") }
        if ([int]$estimate.verificationSeconds -le 0 -or [int]$estimate.repairSeconds -le 0) {
            $issues.Add("Cost duration is invalid for '$($estimate.checkId)'.")
            continue
        }
        $expectedFailure = [Math]::Round([int]$estimate.repairSeconds * [int]$estimate.failureProbabilityPercent / 100.0, 2)
        $expectedTotal = [Math]::Round([int]$estimate.verificationSeconds + $expectedFailure, 2)
        $density = [Math]::Round($expectedFailure / [int]$estimate.verificationSeconds, 4)
        $boost = [Math]::Min([int]$costPolicy.maximumPriorityBoost, [int][Math]::Floor($density * [double]$costPolicy.valueDensityBoostMultiplier))
        if ([double]$estimate.expectedFailureSeconds -ne $expectedFailure -or [double]$estimate.expectedTotalSeconds -ne $expectedTotal -or [double]$estimate.valueDensity -ne $density) {
            $issues.Add("Cost arithmetic is invalid for '$($estimate.checkId)'.")
        }
        if ([int]$estimate.priorityBoost -ne $boost) { $issues.Add("Cost priority boost is invalid for '$($estimate.checkId)'.") }
        $snapshotOverrides = @($Receipt.appliedLearningSnapshot | Where-Object { $estimate.checkId -in @($_.application.subjectIds) })
        if ($snapshotOverrides.Count -gt 0) {
            $expectedSeconds = [int][Math]::Round([double](($snapshotOverrides.application.recommendedSeconds | Measure-Object -Average).Average))
            if ([string]$estimate.verificationCostSource -ne 'approved-learning' -or [int]$estimate.verificationSeconds -ne [Math]::Max(1, $expectedSeconds)) {
                $issues.Add("Applied learning calibration is invalid for '$($estimate.checkId)'.")
            }
            if ((Get-Hash @($estimate.learningCandidateIds)) -cne (Get-Hash @($snapshotOverrides.id | Sort-Object -Unique))) {
                $issues.Add("Applied learning provenance is invalid for '$($estimate.checkId)'.")
            }
        } elseif ([string]$estimate.verificationCostSource -eq 'approved-learning') {
            $issues.Add("Applied learning source has no snapshot for '$($estimate.checkId)'.")
        }
    }
    $expectedTotals = [pscustomobject][ordered]@{
        verificationSeconds = [double](($Receipt.estimates.verificationSeconds | Measure-Object -Sum).Sum)
        expectedFailureSeconds = [double](($Receipt.estimates.expectedFailureSeconds | Measure-Object -Sum).Sum)
        expectedTotalSeconds = [double](($Receipt.estimates.expectedTotalSeconds | Measure-Object -Sum).Sum)
    }
    if ((Get-Hash $Receipt.totals) -cne (Get-Hash $expectedTotals)) { $issues.Add('Cost totals are invalid.') }
    if ([string]$Receipt.costHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Verification cost hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}

if ($Action -eq 'create') {
    $forecast = Get-Forecast $true
    $totals = [pscustomobject][ordered]@{
        verificationSeconds = [double](($forecast.estimates.verificationSeconds | Measure-Object -Sum).Sum)
        expectedFailureSeconds = [double](($forecast.estimates.expectedFailureSeconds | Measure-Object -Sum).Sum)
        expectedTotalSeconds = [double](($forecast.estimates.expectedTotalSeconds | Measure-Object -Sum).Sum)
    }
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1; workspace = $workspace; createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        packetFingerprint = [string]$forecast.packet.fingerprint
        policyFingerprint = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        predictionHash = [string]$forecast.prediction.prediction.predictionHash
        telemetryRegistryHash = [string]$forecast.telemetry.registryHash
        learningRegistryFingerprint = [string]$forecast.learning.registryFingerprint
        experimentRegistryFingerprint = [string]$forecast.experiments.registryFingerprint
        appliedLearningSnapshot = @($forecast.appliedLearningSnapshot)
        estimates = @($forecast.estimates); totals = $totals; costHash = ''
    }
    $receipt.costHash = Get-Hash (Get-Payload $receipt)
    [IO.File]::WriteAllText($receiptPath, (($receipt | ConvertTo-Json -Depth 40) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    $result = [pscustomobject][ordered]@{ action = 'create'; valid = $true; forecast = $receipt; calibration = Get-Calibration $receipt.estimates; issues = @() }
} elseif ($Action -eq 'assess' -and -not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
    $forecast = Get-Forecast $false
    $totals = [pscustomobject][ordered]@{
        verificationSeconds = [double](($forecast.estimates.verificationSeconds | Measure-Object -Sum).Sum)
        expectedFailureSeconds = [double](($forecast.estimates.expectedFailureSeconds | Measure-Object -Sum).Sum)
        expectedTotalSeconds = [double](($forecast.estimates.expectedTotalSeconds | Measure-Object -Sum).Sum)
    }
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1; workspace = $workspace; createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        packetFingerprint = [string]$forecast.packet.fingerprint
        policyFingerprint = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        predictionHash = [string]$forecast.prediction.prediction.predictionHash
        telemetryRegistryHash = [string]$forecast.telemetry.registryHash
        learningRegistryFingerprint = [string]$forecast.learning.registryFingerprint
        experimentRegistryFingerprint = [string]$forecast.experiments.registryFingerprint
        appliedLearningSnapshot = @($forecast.appliedLearningSnapshot)
        estimates = @($forecast.estimates); totals = $totals; costHash = ''
    }
    $receipt.costHash = Get-Hash (Get-Payload $receipt)
    $result = [pscustomobject][ordered]@{ action = 'assess'; valid = $true; forecast = $receipt; calibration = Get-Calibration $receipt.estimates; issues = @() }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Verification cost forecast is absent: $workspace/verification-cost.json" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Receipt $receipt
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; forecast = $receipt; calibration = Get-Calibration $receipt.estimates; issues = @($validation.issues) }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 40 } else {
    Write-Host "Verification cost: action=$Action, valid=$($result.valid), expected=$($result.forecast.totals.expectedTotalSeconds)s"
    foreach ($estimate in @($result.forecast.estimates)) { Write-Host " - $($estimate.checkId): expected=$($estimate.expectedTotalSeconds)s, density=$($estimate.valueDensity)" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
