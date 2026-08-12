[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('assess', 'create', 'show', 'verify')]
    [string]$Action = 'show',
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
$receiptPath = Join-Path $workspaceAbsolute 'risk-calibration.json'
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
if (-not (Test-Path -LiteralPath $packetPath -PathType Leaf)) { throw "Change packet is absent: $normalizedWorkspace/change-packet.json" }
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
$riskPolicy = $policy.scheduler.verificationPlanner.riskCalibration

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Receipt.schemaVersion
        workspace = [string]$Receipt.workspace
        createdAtUtc = ([DateTimeOffset]$Receipt.createdAtUtc).ToUniversalTime().ToString('o')
        packetFingerprint = [string]$Receipt.packetFingerprint
        policyFingerprint = [string]$Receipt.policyFingerprint
        qualityAdjustmentFingerprint = [string]$Receipt.qualityAdjustmentFingerprint
        score = [int]$Receipt.score
        level = [string]$Receipt.level
        signals = @($Receipt.signals | ForEach-Object {
            [pscustomobject][ordered]@{
                id = [string]$_.id
                points = [int]$_.points
                evidence = [string]$_.evidence
            }
        })
        controls = [pscustomobject][ordered]@{
            forceIncludePassed = [bool]$Receipt.controls.forceIncludePassed
            requireSequentialExecution = [bool]$Receipt.controls.requireSequentialExecution
            failFast = [bool]$Receipt.controls.failFast
        }
    }
}
function Get-Level([int]$Score) {
    if ($Score -ge [int]$riskPolicy.criticalThreshold) { return 'critical' }
    if ($Score -ge [int]$riskPolicy.highThreshold) { return 'high' }
    if ($Score -ge [int]$riskPolicy.mediumThreshold) { return 'medium' }
    'low'
}
function Test-LevelAtLeast([string]$Actual, [string]$Threshold) {
    $levels = @('low', 'medium', 'high', 'critical')
    [Array]::IndexOf($levels, $Actual) -ge [Array]::IndexOf($levels, $Threshold)
}
function Get-Current([Nullable[int]]$HistoricalPoints) {
    $packet = Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json
    $adjustments = & (Join-Path $PSScriptRoot 'Manage-LlmWikiQualityAdjustment.ps1') metrics -Format Json | ConvertFrom-Json
    $signals = [Collections.Generic.List[object]]::new()
    $packetPoints = [Math]::Min(40, [int]$packet.brief.risk.score * [int]$riskPolicy.packetRiskPointWeight)
    if ($packetPoints -gt 0) { $signals.Add([pscustomobject][ordered]@{ id = 'packet-risk'; points = $packetPoints; evidence = "brief=$($packet.brief.risk.score)" }) }
    $pathPoints = [Math]::Min([int]$riskPolicy.maximumChangedPathPoints, @($packet.diff.changedPaths).Count * [int]$riskPolicy.changedPathPointWeight)
    if ($pathPoints -gt 0) { $signals.Add([pscustomobject][ordered]@{ id = 'change-breadth'; points = $pathPoints; evidence = "paths=$(@($packet.diff.changedPaths).Count)" }) }
    $scopePoints = [Math]::Max(0, @($packet.diff.scopes).Count - 1) * [int]$riskPolicy.additionalScopePointWeight
    if ($scopePoints -gt 0) { $signals.Add([pscustomobject][ordered]@{ id = 'cross-scope'; points = $scopePoints; evidence = "scopes=$(@($packet.diff.scopes) -join ',')" }) }
    if ('Database' -in @($packet.diff.scopes)) { $signals.Add([pscustomobject][ordered]@{ id = 'database'; points = [int]$riskPolicy.databaseScopePoints; evidence = 'Database scope' }) }
    if ('Api' -in @($packet.diff.scopes)) { $signals.Add([pscustomobject][ordered]@{ id = 'api'; points = [int]$riskPolicy.apiScopePoints; evidence = 'Api scope' }) }
    if ('security-review' -in @($packet.policy.reviewObligations.id)) { $signals.Add([pscustomobject][ordered]@{ id = 'security-review'; points = [int]$riskPolicy.securityReviewPoints; evidence = 'security-review obligation' }) }
    $negativeEventValues = @($adjustments.metrics.dispatchProfiles | Where-Object totalDelta -lt 0 | ForEach-Object { [int]$_.eventCount })
    $negativeEvents = if ($negativeEventValues.Count -gt 0) { [int](($negativeEventValues | Measure-Object -Sum).Sum) } else { 0 }
    $historyPoints = if ($null -ne $HistoricalPoints) {
        [int]$HistoricalPoints
    } else {
        [Math]::Min([int]$riskPolicy.maximumHistoricalPoints, $negativeEvents * [int]$riskPolicy.negativeQualityAdjustmentPoints)
    }
    if ($historyPoints -gt 0) { $signals.Add([pscustomobject][ordered]@{ id = 'historical-rework-pressure'; points = $historyPoints; evidence = "snapshot-points=$historyPoints" }) }
    $signalPoints = @($signals | ForEach-Object { [int]$_.points })
    $signalTotal = if ($signalPoints.Count -gt 0) { [int](($signalPoints | Measure-Object -Sum).Sum) } else { 0 }
    $score = [Math]::Min(100, $signalTotal)
    $level = Get-Level $score
    [pscustomobject]@{
        packet = $packet
        adjustmentMetrics = $adjustments
        policyFingerprint = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        score = $score
        level = $level
        signals = @($signals | Sort-Object id)
        controls = [pscustomobject][ordered]@{
            forceIncludePassed = Test-LevelAtLeast $level ([string]$riskPolicy.forceIncludePassedAt)
            requireSequentialExecution = Test-LevelAtLeast $level ([string]$riskPolicy.requireSequentialExecutionAt)
            failFast = $true
        }
    }
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    $storedHistoricalSignal = @($Receipt.signals | Where-Object id -eq 'historical-rework-pressure' | Select-Object -First 1)
    $storedHistoricalPoints = if ($storedHistoricalSignal.Count -eq 1) { [int]$storedHistoricalSignal[0].points } else { 0 }
    $current = Get-Current $storedHistoricalPoints
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.workspace -cne $normalizedWorkspace) { $issues.Add('Workspace does not match.') }
    if ([string]$Receipt.packetFingerprint -cne [string]$current.packet.fingerprint) { $issues.Add('Change packet drifted.') }
    if ([string]$Receipt.policyFingerprint -cne [string]$current.policyFingerprint) { $issues.Add('Workspace policy drifted.') }
    if ([int]$Receipt.score -ne [int]$current.score -or [string]$Receipt.level -cne [string]$current.level) { $issues.Add('Risk score drifted.') }
    if ((Get-Hash @($Receipt.signals)) -cne (Get-Hash @($current.signals))) { $issues.Add('Risk signals drifted.') }
    if ((Get-Hash $Receipt.controls) -cne (Get-Hash $current.controls)) { $issues.Add('Risk controls drifted.') }
    if ([string]$Receipt.calibrationHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('Calibration hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0; issues = @($issues); current = $current }
}

if ($Action -in @('assess', 'create')) {
    $current = Get-Current $null
    if (-not $current.adjustmentMetrics.valid) { throw 'Risk calibration requires valid quality-adjustment history.' }
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        workspace = $normalizedWorkspace
        createdAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        packetFingerprint = [string]$current.packet.fingerprint
        policyFingerprint = [string]$current.policyFingerprint
        qualityAdjustmentFingerprint = [string]$current.adjustmentMetrics.metrics.fingerprint
        score = [int]$current.score
        level = [string]$current.level
        signals = @($current.signals)
        controls = $current.controls
        calibrationHash = ''
    }
    $receipt.calibrationHash = Get-Hash (Get-Payload $receipt)
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
    $result = [pscustomobject][ordered]@{
        action = $Action; valid = $true; calibration = $receipt
        savedPath = $(if ($Action -eq 'create') { "$normalizedWorkspace/risk-calibration.json" } else { $null })
    }
} else {
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) { throw "Risk calibration is absent: $normalizedWorkspace/risk-calibration.json" }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $validation = Test-Receipt $receipt
    $result = [pscustomobject][ordered]@{ action = $Action; valid = $validation.valid; issues = @($validation.issues); calibration = $receipt }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Risk calibration: action=$($result.action), valid=$($result.valid), level=$($result.calibration.level), score=$($result.calibration.score), hash=$($result.calibration.calibrationHash)"
    foreach ($signal in @($result.calibration.signals)) { Write-Host " - $($signal.id): +$($signal.points) ($($signal.evidence))" }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
