[CmdletBinding()]
param(
    [Nullable[int]]$WindowDays,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnAttention,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$dispatchRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler/dispatches'
$policy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json
$effectiveWindowDays = if ($null -ne $WindowDays) { [int]$WindowDays } else { [int]$policy.scheduler.defaultMetricsWindowDays }
if ($effectiveWindowDays -lt 1 -or $effectiveWindowDays -gt [int]$policy.scheduler.maximumReceiptRetentionDays) {
    throw "WindowDays must be between 1 and $($policy.scheduler.maximumReceiptRetentionDays)."
}
$now = $AsOfUtc.ToUniversalTime()
$cutoff = $now.AddDays(-$effectiveWindowDays)

function Convert-ToUtc([object]$Value) {
    $parsed = [DateTime]::MinValue
    if (-not [DateTime]::TryParse(
        [string]$Value,
        [System.Globalization.CultureInfo]::InvariantCulture,
        [System.Globalization.DateTimeStyles]::AssumeUniversal,
        [ref]$parsed)) {
        throw "Invalid UTC timestamp: $Value"
    }
    return $parsed.ToUniversalTime()
}
function Get-Percentile([double[]]$Values, [double]$Percentile) {
    if ($Values.Count -eq 0) { return $null }
    $ordered = @($Values | Sort-Object)
    $rank = [Math]::Max(1, [Math]::Ceiling($Percentile * $ordered.Count))
    return [Math]::Round([double]$ordered[$rank - 1], 2)
}
function Get-Rate([int]$Numerator, [int]$Denominator) {
    if ($Denominator -eq 0) { return $null }
    return [Math]::Round(($Numerator * 100.0) / $Denominator, 2)
}
function Get-FailureCategory([object]$TerminalEvent) {
    if ($null -eq $TerminalEvent -or [string]$TerminalEvent.type -ne 'failed') { return '' }
    $result = [string]$TerminalEvent.details.result
    if ($result -match '(?i)^Watchdog terminated|silent dispatch') { return 'watchdog-silence' }
    if ($result -match '(?i)^Compensated batch claim') { return 'claim-compensation' }
    if ($null -ne $TerminalEvent.details.PSObject.Properties['reconciled'] -and [bool]$TerminalEvent.details.reconciled) { return 'reconciliation' }
    if ($result -match '(?i)(packet|context)[ -]drift') { return 'dispatch-drift' }
    return 'agent-reported'
}

$dispatchList = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
$validViews = @{}
foreach ($view in @($dispatchList.dispatches | Where-Object valid)) { $validViews[[string]$view.dispatchId] = $view }
$records = [System.Collections.Generic.List[object]]::new()

foreach ($file in @(Get-ChildItem -LiteralPath $dispatchRoot -File -Filter '*.json' -ErrorAction SilentlyContinue | Sort-Object Name)) {
    if (-not $validViews.ContainsKey($file.BaseName)) { continue }
    try {
        $receipt = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        $startedAt = Convert-ToUtc $receipt.startedAtUtc
        if ($startedAt -lt $cutoff -or $startedAt -gt $now) { continue }
        $events = @($receipt.events)
        $terminalEvent = $events | Where-Object type -in @('completed', 'failed') | Select-Object -Last 1
        $terminalAt = if ($null -ne $terminalEvent) { Convert-ToUtc $terminalEvent.atUtc } else { $null }
        $durationMinutes = if ($null -ne $terminalAt) {
            [Math]::Round([Math]::Max(0, ($terminalAt - $startedAt).TotalMinutes), 2)
        } else {
            [Math]::Round([Math]::Max(0, ($now - $startedAt).TotalMinutes), 2)
        }
        $reconciled = $false
        if ($null -ne $terminalEvent -and $null -ne $terminalEvent.details.PSObject.Properties['reconciled']) {
            $reconciled = [bool]$terminalEvent.details.reconciled
        }
        $records.Add([pscustomobject][ordered]@{
            dispatchId = [string]$receipt.dispatchId
            workspace = [string]$receipt.workspace
            owner = [string]$receipt.owner
            agentId = [string]$receipt.agentId
            agentCapabilities = @($receipt.agentCapabilities)
            requiredCapabilities = @($receipt.requiredCapabilities)
            lane = $receipt.lane
            state = [string]$validViews[$file.BaseName].state
            startedAtUtc = $startedAt.ToString('o')
            terminalAtUtc = $(if ($null -ne $terminalAt) { $terminalAt.ToString('o') } else { $null })
            durationMinutes = $durationMinutes
            heartbeatCount = @($events | Where-Object type -eq 'heartbeat').Count
            eventCount = $events.Count
            reconciled = $reconciled
            failureCategory = Get-FailureCategory $terminalEvent
        })
    } catch {
        continue
    }
}

$terminal = @($records | Where-Object state -in @('completed', 'failed'))
$completed = @($terminal | Where-Object state -eq 'completed')
$failed = @($terminal | Where-Object state -eq 'failed')
$reconciled = @($terminal | Where-Object reconciled)
$withHeartbeat = @($records | Where-Object heartbeatCount -gt 0)
$terminalDurations = @($terminal | ForEach-Object { [double]$_.durationMinutes })
$averageDuration = if ($terminalDurations.Count -gt 0) {
    [Math]::Round(($terminalDurations | Measure-Object -Average).Average, 2)
} else { $null }

$owners = @($records | Group-Object owner | ForEach-Object {
    $ownerRecords = @($_.Group)
    $ownerTerminal = @($ownerRecords | Where-Object state -in @('completed', 'failed'))
    $ownerCompleted = @($ownerTerminal | Where-Object state -eq 'completed')
    $ownerDurations = @($ownerTerminal | ForEach-Object { [double]$_.durationMinutes })
    [pscustomobject][ordered]@{
        owner = $_.Name
        dispatchCount = $ownerRecords.Count
        terminalCount = $ownerTerminal.Count
        completedCount = $ownerCompleted.Count
        failedCount = @($ownerTerminal | Where-Object state -eq 'failed').Count
        successRatePercent = Get-Rate $ownerCompleted.Count $ownerTerminal.Count
        heartbeatCoveragePercent = Get-Rate @($ownerRecords | Where-Object heartbeatCount -gt 0).Count $ownerRecords.Count
        averageDurationMinutes = $(if ($ownerDurations.Count -gt 0) { [Math]::Round(($ownerDurations | Measure-Object -Average).Average, 2) } else { $null })
        failureCategories = @($ownerTerminal | Where-Object state -eq 'failed' | Group-Object failureCategory | ForEach-Object {
            [pscustomobject][ordered]@{ category = $_.Name; count = $_.Count }
        } | Sort-Object category)
    }
} | Sort-Object owner)

$capabilitySamples = @($records | ForEach-Object {
    $record = $_
    foreach ($capability in @($record.requiredCapabilities | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Select-Object -Unique)) {
        [pscustomobject]@{ owner = $record.owner; agentId = $record.agentId; capability = [string]$capability; record = $record }
    }
})
$capabilityProfiles = @($capabilitySamples | Group-Object { "$($_.owner)`n$($_.capability)" } | ForEach-Object {
    $samples = @($_.Group)
    $sampleRecords = @($samples.record)
    $sampleTerminal = @($sampleRecords | Where-Object state -in @('completed', 'failed'))
    $sampleCompleted = @($sampleTerminal | Where-Object state -eq 'completed')
    $durations = @($sampleTerminal | ForEach-Object { [double]$_.durationMinutes })
    [pscustomobject][ordered]@{
        owner = [string]$samples[0].owner
        capability = [string]$samples[0].capability
        dispatchCount = $sampleRecords.Count
        terminalCount = $sampleTerminal.Count
        completedCount = $sampleCompleted.Count
        failedCount = @($sampleTerminal | Where-Object state -eq 'failed').Count
        successRatePercent = Get-Rate $sampleCompleted.Count $sampleTerminal.Count
        heartbeatCoveragePercent = Get-Rate @($sampleRecords | Where-Object heartbeatCount -gt 0).Count $sampleRecords.Count
        averageDurationMinutes = $(if ($durations.Count -gt 0) { [Math]::Round(($durations | Measure-Object -Average).Average, 2) } else { $null })
        p95DurationMinutes = Get-Percentile $durations 0.95
        failureCategories = @($sampleTerminal | Where-Object state -eq 'failed' | Group-Object failureCategory | ForEach-Object {
            [pscustomobject][ordered]@{ category = $_.Name; count = $_.Count }
        } | Sort-Object category)
    }
} | Sort-Object owner, capability)

$agentProfiles = @($records | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.agentId) } | Group-Object agentId | ForEach-Object {
    $agentRecords = @($_.Group)
    $agentTerminal = @($agentRecords | Where-Object state -in @('completed', 'failed'))
    $agentCompleted = @($agentTerminal | Where-Object state -eq 'completed')
    [pscustomobject][ordered]@{
        agentId = $_.Name
        owner = [string]$agentRecords[-1].owner
        dispatchCount = $agentRecords.Count
        terminalCount = $agentTerminal.Count
        successRatePercent = Get-Rate $agentCompleted.Count $agentTerminal.Count
        capabilitiesObserved = @($agentRecords.requiredCapabilities | ForEach-Object { @($_) } | Select-Object -Unique | Sort-Object)
    }
} | Sort-Object owner, agentId)

$daily = @($terminal | Group-Object { (Convert-ToUtc $_.terminalAtUtc).ToString('yyyy-MM-dd') } | ForEach-Object {
    $dayRecords = @($_.Group)
    [pscustomobject][ordered]@{
        date = $_.Name
        terminalCount = $dayRecords.Count
        completedCount = @($dayRecords | Where-Object state -eq 'completed').Count
        failedCount = @($dayRecords | Where-Object state -eq 'failed').Count
        reconciledCount = @($dayRecords | Where-Object reconciled).Count
    }
} | Sort-Object date)

$successRate = Get-Rate $completed.Count $terminal.Count
$reconciliationRate = Get-Rate $reconciled.Count $terminal.Count
$heartbeatCoverage = Get-Rate $withHeartbeat.Count $records.Count
$p95Duration = Get-Percentile $terminalDurations 0.95
$sloViolations = [System.Collections.Generic.List[object]]::new()
$sloEvaluated = $terminal.Count -ge [int]$policy.scheduler.slo.minimumTerminalSamples
if ($sloEvaluated) {
    if ($successRate -lt [double]$policy.scheduler.slo.minimumSuccessRatePercent) {
        $sloViolations.Add([pscustomobject][ordered]@{
            id = 'success-rate'
            actual = $successRate
            operator = '>='
            threshold = [double]$policy.scheduler.slo.minimumSuccessRatePercent
            remediation = 'Review failed dispatch outcomes and owner-level reliability.'
        })
    }
    if ($heartbeatCoverage -lt [double]$policy.scheduler.slo.minimumHeartbeatCoveragePercent) {
        $sloViolations.Add([pscustomobject][ordered]@{
            id = 'heartbeat-coverage'
            actual = $heartbeatCoverage
            operator = '>='
            threshold = [double]$policy.scheduler.slo.minimumHeartbeatCoveragePercent
            remediation = 'Ensure active agents emit periodic task-dispatch-heartbeat events.'
        })
    }
    if ($reconciliationRate -gt [double]$policy.scheduler.slo.maximumReconciliationRatePercent) {
        $sloViolations.Add([pscustomobject][ordered]@{
            id = 'reconciliation-rate'
            actual = $reconciliationRate
            operator = '<='
            threshold = [double]$policy.scheduler.slo.maximumReconciliationRatePercent
            remediation = 'Investigate lease expiry, packet drift, and abandoned agent runs.'
        })
    }
    if ($null -ne $p95Duration -and $p95Duration -gt [double]$policy.scheduler.slo.maximumP95DurationMinutes) {
        $sloViolations.Add([pscustomobject][ordered]@{
            id = 'p95-duration'
            actual = $p95Duration
            operator = '<='
            threshold = [double]$policy.scheduler.slo.maximumP95DurationMinutes
            remediation = 'Split oversized tasks or increase scheduler parallelism within conflict limits.'
        })
    }
}
$sloVerdict = if (-not $sloEvaluated) { 'insufficient-data' } elseif ($sloViolations.Count -gt 0) { 'degraded' } else { 'healthy' }
$operationalAttentionCount = [int]($dispatchList.orphanedCount + $dispatchList.driftedCount + $dispatchList.invalidCount)

$response = [pscustomobject][ordered]@{
    schemaVersion = 1
    asOfUtc = $now.ToString('o')
    windowDays = $effectiveWindowDays
    cutoffUtc = $cutoff.ToString('o')
    retainedReceiptCount = [int]$dispatchList.totalCount
    invalidReceiptCount = [int]$dispatchList.invalidCount
    dispatchCount = $records.Count
    runningCount = @($records | Where-Object state -eq 'running').Count
    orphanedCount = @($records | Where-Object state -eq 'orphaned').Count
    driftedCount = @($records | Where-Object state -in @('packet-drift', 'context-drift')).Count
    terminalCount = $terminal.Count
    completedCount = $completed.Count
    failedCount = $failed.Count
    reconciledCount = $reconciled.Count
    successRatePercent = $successRate
    reconciliationRatePercent = $reconciliationRate
    heartbeatCoveragePercent = $heartbeatCoverage
    throughputPerDay = [Math]::Round($terminal.Count / [double]$effectiveWindowDays, 3)
    durationMinutes = [pscustomobject][ordered]@{
        average = $averageDuration
        p50 = Get-Percentile $terminalDurations 0.50
        p95 = $p95Duration
        maximum = $(if ($terminalDurations.Count -gt 0) { [Math]::Round(($terminalDurations | Measure-Object -Maximum).Maximum, 2) } else { $null })
    }
    slo = [pscustomobject][ordered]@{
        verdict = $sloVerdict
        evaluated = $sloEvaluated
        minimumTerminalSamples = [int]$policy.scheduler.slo.minimumTerminalSamples
        sampleCount = $terminal.Count
        violationCount = $sloViolations.Count
        violations = @($sloViolations)
        thresholds = $policy.scheduler.slo
    }
    operationalAttentionCount = $operationalAttentionCount
    attentionCount = $operationalAttentionCount + $sloViolations.Count
    owners = $owners
    agentProfiles = $agentProfiles
    capabilityProfiles = $capabilityProfiles
    failureCategories = @($failed | Group-Object failureCategory | ForEach-Object {
        [pscustomobject][ordered]@{ category = $_.Name; count = $_.Count }
    } | Sort-Object category)
    daily = $daily
    dispatches = @($records)
}

if ($Format -eq 'Json') {
    $response | ConvertTo-Json -Depth 10
} else {
    Write-Host "Dispatch metrics: window=$effectiveWindowDays day(s), dispatches=$($response.dispatchCount), terminal=$($response.terminalCount), success=$($response.successRatePercent)%, slo=$($response.slo.verdict), attention=$($response.attentionCount)"
    Write-Host "Latency: avg=$($response.durationMinutes.average)m, p50=$($response.durationMinutes.p50)m, p95=$($response.durationMinutes.p95)m; heartbeat=$($response.heartbeatCoveragePercent)%"
    foreach ($ownerMetric in $owners) {
        Write-Host " - $($ownerMetric.owner): dispatches=$($ownerMetric.dispatchCount), terminal=$($ownerMetric.terminalCount), success=$($ownerMetric.successRatePercent)%"
    }
    foreach ($violation in $sloViolations) {
        Write-Host " ! $($violation.id): actual=$($violation.actual), expected $($violation.operator) $($violation.threshold). $($violation.remediation)"
    }
}
if ($FailOnAttention -and $response.attentionCount -gt 0) { exit 1 }
