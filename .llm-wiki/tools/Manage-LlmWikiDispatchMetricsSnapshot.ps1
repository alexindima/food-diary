[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('list', 'save', 'verify', 'compare', 'prune')]
    [string]$Action = 'list',
    [string]$SnapshotId,
    [Nullable[int]]$WindowDays,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [object]$MetricsInput,
    [switch]$Apply,
    [switch]$FailOnInvalid,
    [switch]$FailOnRegression,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$snapshotRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler/metrics'
$now = $AsOfUtc.ToUniversalTime()
$policySnapshot = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -WithFingerprint -Format Json | ConvertFrom-Json
$policyResult = $policySnapshot.policy
$policyFingerprint = [string]$policySnapshot.fingerprint

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-SnapshotPayload([object]$Snapshot) {
    [ordered]@{
        schemaVersion = $Snapshot.schemaVersion
        snapshotId = $Snapshot.snapshotId
        capturedAtUtc = $Snapshot.capturedAtUtc
        policyFingerprint = $Snapshot.policyFingerprint
        metricsHash = $Snapshot.metricsHash
        metrics = $Snapshot.metrics
    }
}
function Test-Snapshot([object]$Snapshot) {
    $issues = [System.Collections.Generic.List[string]]::new()
    if ($Snapshot.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Snapshot.snapshotId -notmatch '^[a-f0-9]{32}$') { $issues.Add('snapshotId is invalid.') }
    $expectedMetricsHash = Get-Hash $Snapshot.metrics
    if ([string]$Snapshot.metricsHash -cne $expectedMetricsHash) { $issues.Add('metricsHash is invalid.') }
    $expectedSnapshotHash = Get-Hash (Get-SnapshotPayload $Snapshot)
    if ([string]$Snapshot.snapshotHash -cne $expectedSnapshotHash) { $issues.Add('snapshotHash is invalid.') }
    [pscustomobject][ordered]@{
        valid = $issues.Count -eq 0
        issues = @($issues)
        expectedMetricsHash = $expectedMetricsHash
        expectedSnapshotHash = $expectedSnapshotHash
    }
}
function Get-SnapshotFiles {
    if (-not (Test-Path -LiteralPath $snapshotRoot -PathType Container)) { return @() }
    return @(Get-ChildItem -LiteralPath $snapshotRoot -File -Filter '*.json' | Sort-Object Name)
}
function Read-SnapshotById([string]$Id) {
    if ($Id -notmatch '^[a-f0-9]{32}$') { throw 'SnapshotId must be a 32-character lowercase hexadecimal identifier.' }
    $matches = @(Get-SnapshotFiles | Where-Object BaseName -like "*-$Id")
    if ($matches.Count -ne 1) { throw "Metrics snapshot does not exist or is ambiguous: $Id" }
    return Get-Content -LiteralPath $matches[0].FullName -Raw | ConvertFrom-Json
}
function Get-Metrics {
    if ($null -ne $MetricsInput) { return $MetricsInput }
    $arguments = @{ AsOfUtc = $now; Format = 'Json' }
    if ($null -ne $WindowDays) { $arguments.WindowDays = $WindowDays }
    return & (Join-Path $PSScriptRoot 'Get-LlmWikiDispatchMetrics.ps1') @arguments | ConvertFrom-Json
}
function Get-Delta([object]$Current, [object]$Baseline) {
    if ($null -eq $Current -or $null -eq $Baseline) { return $null }
    return [Math]::Round(([double]$Current - [double]$Baseline), 2)
}
function Get-PercentChange([object]$Current, [object]$Baseline) {
    if ($null -eq $Current -or $null -eq $Baseline -or [double]$Baseline -eq 0) { return $null }
    return [Math]::Round((([double]$Current - [double]$Baseline) * 100.0) / [double]$Baseline, 2)
}

if ($Action -eq 'save') {
    $metrics = Get-Metrics
    $snapshotIdValue = [guid]::NewGuid().ToString('N')
    $snapshot = [pscustomobject][ordered]@{
        schemaVersion = 1
        snapshotId = $snapshotIdValue
        capturedAtUtc = $now.ToString('o')
        policyFingerprint = [string]$policyFingerprint
        metricsHash = Get-Hash $metrics
        metrics = $metrics
        snapshotHash = ''
    }
    $snapshot.snapshotHash = Get-Hash (Get-SnapshotPayload $snapshot)
    if (-not (Test-Path -LiteralPath $snapshotRoot)) { New-Item -ItemType Directory -Path $snapshotRoot | Out-Null }
    $fileName = "$($now.ToString('yyyyMMddTHHmmssfffZ'))-$snapshotIdValue.json"
    $path = Join-Path $snapshotRoot $fileName
    $temporaryPath = Join-Path $snapshotRoot ('.metrics-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [System.IO.File]::WriteAllText($temporaryPath, (($snapshot | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination $path
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [System.IO.File]::Delete($temporaryPath) }
    }
    $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'save'; valid = $true; snapshot = $snapshot; path = ".artifacts/llm-wiki/scheduler/metrics/$fileName" }
} elseif ($Action -eq 'verify') {
    if ([string]::IsNullOrWhiteSpace($SnapshotId)) { throw 'verify requires SnapshotId.' }
    $snapshot = Read-SnapshotById $SnapshotId
    $validation = Test-Snapshot $snapshot
    $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'verify'; valid = $validation.valid; snapshotId = $SnapshotId; issues = @($validation.issues); snapshot = $snapshot }
} elseif ($Action -eq 'compare') {
    $files = Get-SnapshotFiles
    $baseline = if (-not [string]::IsNullOrWhiteSpace($SnapshotId)) {
        Read-SnapshotById $SnapshotId
    } else {
        $latestValid = $null
        foreach ($candidateFile in @($files | Sort-Object Name -Descending)) {
            try {
                $candidateSnapshot = Get-Content -LiteralPath $candidateFile.FullName -Raw | ConvertFrom-Json
                if ((Test-Snapshot $candidateSnapshot).valid) {
                    $latestValid = $candidateSnapshot
                    break
                }
            } catch {
                continue
            }
        }
        $latestValid
    }
    $current = Get-Metrics
    $violations = [System.Collections.Generic.List[object]]::new()
    $baselineValidation = if ($null -ne $baseline) { Test-Snapshot $baseline } else { $null }
    $minimumSamples = [int]$policyResult.scheduler.metricsSnapshots.regression.minimumTerminalSamples
    $eligible = $null -ne $baseline -and $baselineValidation.valid -and
        [int]$baseline.metrics.terminalCount -ge $minimumSamples -and [int]$current.terminalCount -ge $minimumSamples
    $deltas = [pscustomobject][ordered]@{
        successRatePoints = Get-Delta $current.successRatePercent $baseline.metrics.successRatePercent
        heartbeatCoveragePoints = Get-Delta $current.heartbeatCoveragePercent $baseline.metrics.heartbeatCoveragePercent
        reconciliationRatePoints = Get-Delta $current.reconciliationRatePercent $baseline.metrics.reconciliationRatePercent
        p95DurationPercent = Get-PercentChange $current.durationMinutes.p95 $baseline.metrics.durationMinutes.p95
        throughputPercent = Get-PercentChange $current.throughputPerDay $baseline.metrics.throughputPerDay
    }
    if ($eligible) {
        $rules = $policyResult.scheduler.metricsSnapshots.regression
        if ($null -ne $deltas.successRatePoints -and $deltas.successRatePoints -lt -[double]$rules.maximumSuccessRateDropPoints) { $violations.Add([pscustomobject]@{ id = 'success-rate-regression'; delta = $deltas.successRatePoints; limit = -[double]$rules.maximumSuccessRateDropPoints }) }
        if ($null -ne $deltas.heartbeatCoveragePoints -and $deltas.heartbeatCoveragePoints -lt -[double]$rules.maximumHeartbeatCoverageDropPoints) { $violations.Add([pscustomobject]@{ id = 'heartbeat-coverage-regression'; delta = $deltas.heartbeatCoveragePoints; limit = -[double]$rules.maximumHeartbeatCoverageDropPoints }) }
        if ($null -ne $deltas.reconciliationRatePoints -and $deltas.reconciliationRatePoints -gt [double]$rules.maximumReconciliationRateIncreasePoints) { $violations.Add([pscustomobject]@{ id = 'reconciliation-rate-regression'; delta = $deltas.reconciliationRatePoints; limit = [double]$rules.maximumReconciliationRateIncreasePoints }) }
        if ($null -ne $deltas.p95DurationPercent -and $deltas.p95DurationPercent -gt [double]$rules.maximumP95DurationIncreasePercent) { $violations.Add([pscustomobject]@{ id = 'p95-duration-regression'; delta = $deltas.p95DurationPercent; limit = [double]$rules.maximumP95DurationIncreasePercent }) }
        if ($null -ne $deltas.throughputPercent -and $deltas.throughputPercent -lt -[double]$rules.maximumThroughputDropPercent) { $violations.Add([pscustomobject]@{ id = 'throughput-regression'; delta = $deltas.throughputPercent; limit = -[double]$rules.maximumThroughputDropPercent }) }
    }
    $verdict = if ($null -eq $baseline) { 'no-baseline' } elseif (-not $baselineValidation.valid) { 'invalid-baseline' } elseif (-not $eligible) { 'insufficient-data' } elseif ($violations.Count -gt 0) { 'regressed' } else { 'stable' }
    $response = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = 'compare'
        asOfUtc = $now.ToString('o')
        verdict = $verdict
        eligible = $eligible
        baselineSnapshotId = $(if ($null -ne $baseline) { [string]$baseline.snapshotId } else { '' })
        policyFingerprintChanged = $(if ($null -ne $baseline) { [string]$baseline.policyFingerprint -cne [string]$policyFingerprint } else { $false })
        deltas = $deltas
        violationCount = $violations.Count
        violations = @($violations)
        currentMetrics = $current
    }
} elseif ($Action -eq 'prune') {
    $files = Get-SnapshotFiles
    $retentionCount = [int]$policyResult.scheduler.metricsSnapshots.retentionCount
    $candidates = @($files | Sort-Object Name -Descending | Select-Object -Skip $retentionCount)
    if ($Apply) { foreach ($file in $candidates) { [System.IO.File]::Delete($file.FullName) } }
    $response = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = 'prune'
        apply = [bool]$Apply
        retentionCount = $retentionCount
        candidateCount = $candidates.Count
        changedCount = $(if ($Apply) { $candidates.Count } else { 0 })
        candidates = @($candidates | ForEach-Object BaseName)
    }
} else {
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($file in Get-SnapshotFiles) {
        try {
            $snapshot = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
            $validation = Test-Snapshot $snapshot
            $items.Add([pscustomobject][ordered]@{
                snapshotId = [string]$snapshot.snapshotId
                capturedAtUtc = [string]$snapshot.capturedAtUtc
                valid = $validation.valid
                metricsHash = [string]$snapshot.metricsHash
                sloVerdict = [string]$snapshot.metrics.slo.verdict
                terminalCount = [int]$snapshot.metrics.terminalCount
                issues = @($validation.issues)
            })
        } catch {
            $items.Add([pscustomobject][ordered]@{ snapshotId = $file.BaseName; valid = $false; issues = @($_.Exception.Message) })
        }
    }
    $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'list'; totalCount = $items.Count; invalidCount = @($items | Where-Object { -not $_.valid }).Count; snapshots = @($items) }
}

if ($Format -eq 'Json') {
    $response | ConvertTo-Json -Depth 20
} else {
    Write-Host "Dispatch metrics snapshots: action=$Action"
    if ($Action -eq 'compare') { Write-Host "Verdict: $($response.verdict); violations=$($response.violationCount)" }
    elseif ($Action -eq 'list') { Write-Host "Snapshots: total=$($response.totalCount), invalid=$($response.invalidCount)" }
    elseif ($Action -eq 'prune') { Write-Host "Prune: candidates=$($response.candidateCount), changed=$($response.changedCount)" }
    else { Write-Host "Snapshot: $($response.snapshot.snapshotId), valid=$($response.valid)" }
}
$invalid = ($Action -eq 'verify' -and -not $response.valid) -or ($Action -eq 'list' -and $response.invalidCount -gt 0) -or ($Action -eq 'compare' -and $response.verdict -eq 'invalid-baseline')
if ($FailOnInvalid -and $invalid) { exit 1 }
if ($FailOnRegression -and $Action -eq 'compare' -and $response.verdict -eq 'regressed') { exit 1 }
