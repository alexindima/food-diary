[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('record', 'list', 'metrics', 'verify')]
    [string]$Action = 'metrics',
    [string]$WorkspacePath,
    [string]$CheckId,
    [ValidateSet('passed', 'failed')]
    [string]$Status,
    [double]$DurationSeconds,
    [string]$Command,
    [string]$RegistryPath,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$configuredRegistryPath = if (-not [string]::IsNullOrWhiteSpace($RegistryPath)) {
    $RegistryPath
} elseif (-not [string]::IsNullOrWhiteSpace($env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH)) {
    $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH
} else {
    $gitDirectory = @((Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -FailureMessage 'Unable to resolve the Git directory for verification telemetry.').Lines)
    if ($gitDirectory.Count -ne 1) { throw 'Unable to resolve the Git directory for verification telemetry.' }
    Join-Path ([string]$gitDirectory[0]) 'llm-wiki/verification-telemetry.json'
}
$registryPath = if ([IO.Path]::IsPathRooted($configuredRegistryPath)) { $configuredRegistryPath } else { Join-Path $repositoryRoot $configuredRegistryPath }
$policyPath = Join-Path $wikiRoot 'policies/workspace-policies.json'
$telemetryPolicy = (Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json).scheduler.verificationPlanner.failurePrediction.costModel.telemetry

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $Value -Depth 30 -Compress
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-EventPayload([object]$Event) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Event.schemaVersion; sequence = [int]$Event.sequence; id = [string]$Event.id
        recordedAtUtc = ([DateTimeOffset]$Event.recordedAtUtc).ToUniversalTime().ToString('o'); workspace = [string]$Event.workspace
        packetFingerprint = [string]$Event.packetFingerprint; checkId = [string]$Event.checkId
        status = [string]$Event.status; durationSeconds = [double]$Event.durationSeconds
        commandHash = [string]$Event.commandHash; policyFingerprint = [string]$Event.policyFingerprint
        previousHash = [string]$Event.previousHash
    }
}
function Get-RegistryPayload([object]$Registry) {
    [pscustomobject][ordered]@{
        schemaVersion = [int]$Registry.schemaVersion
        events = @($Registry.events | ForEach-Object {
            $payload = Get-EventPayload $_
            $payload | Add-Member -NotePropertyName eventHash -NotePropertyValue ([string]$_.eventHash)
            $payload
        })
    }
}
function Read-Registry {
    if (-not (Test-Path -LiteralPath $registryPath -PathType Leaf)) {
        $empty = [pscustomobject][ordered]@{ schemaVersion = 1; events = @(); registryHash = '' }
        Write-Registry $empty
    }
    Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
}
function Write-Registry([object]$Registry) {
    $Registry.registryHash = Get-Hash (Get-RegistryPayload $Registry)
    $parent = Split-Path -Parent $registryPath
    $null = New-Item -ItemType Directory -Path $parent -Force
    $temporaryPath = Join-Path $parent ('.verification-telemetry-' + [guid]::NewGuid().ToString('N') + '.tmp')
    try {
        [IO.File]::WriteAllText($temporaryPath, (($Registry | ConvertTo-Json -Depth 30) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination $registryPath -Force
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
    }
}
function Enter-RegistryLock {
    $parent = Split-Path -Parent $registryPath
    $null = New-Item -ItemType Directory -Path $parent -Force
    $lockPath = "$registryPath.lock"
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    while ($true) {
        try {
            return [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
        } catch [IO.IOException] {
            if ([DateTime]::UtcNow -ge $deadline) { throw "Timed out waiting for verification telemetry registry lock: $lockPath" }
            Start-Sleep -Milliseconds 25
        }
    }
}
function Test-Registry([object]$Registry) {
    $issues = [Collections.Generic.List[string]]::new()
    if ($Registry.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    $previousHash = ''
    $sequence = 1
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($event in @($Registry.events)) {
        if ([int]$event.sequence -ne $sequence) { $issues.Add("Telemetry sequence is invalid at $($event.id).") }
        if (-not $ids.Add([string]$event.id)) { $issues.Add("Duplicate telemetry id: $($event.id)") }
        if ([string]$event.previousHash -cne $previousHash) { $issues.Add("Telemetry hash chain is invalid at $($event.id).") }
        if ([string]$event.eventHash -cne (Get-Hash (Get-EventPayload $event))) { $issues.Add("Telemetry event hash is invalid at $($event.id).") }
        if ([string]$event.status -notin @('passed', 'failed')) { $issues.Add("Telemetry status is invalid at $($event.id).") }
        if ([double]$event.durationSeconds -lt 0 -or [double]$event.durationSeconds -gt 604800) { $issues.Add("Telemetry duration is invalid at $($event.id).") }
        if ([string]::IsNullOrWhiteSpace([string]$event.checkId)) { $issues.Add("Telemetry checkId is empty at $($event.id).") }
        $previousHash = [string]$event.eventHash
        $sequence++
    }
    if (@($Registry.events).Count -gt [int]$telemetryPolicy.retentionCount) { $issues.Add('Telemetry registry exceeds retentionCount.') }
    if ([string]$Registry.registryHash -cne (Get-Hash (Get-RegistryPayload $Registry))) { $issues.Add('Verification telemetry registry hash is invalid.') }
    [pscustomobject]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}
function Get-Median([double[]]$Values) {
    $sorted = @($Values | Sort-Object)
    if ($sorted.Count -eq 0) { return $null }
    $middle = [Math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2 -eq 1) { return [Math]::Round([double]$sorted[$middle], 2) }
    [Math]::Round(([double]$sorted[$middle - 1] + [double]$sorted[$middle]) / 2.0, 2)
}
function Get-Metrics([object[]]$Events) {
    @($Events | Group-Object checkId | ForEach-Object {
        $items = @($_.Group | Sort-Object sequence)
        $transitions = 0
        for ($index = 1; $index -lt $items.Count; $index++) {
            if ([string]$items[$index].status -cne [string]$items[$index - 1].status) { $transitions++ }
        }
        $transitionPercent = if ($items.Count -lt 2) { 0 } else { [Math]::Round($transitions * 100.0 / ($items.Count - 1), 2) }
        $failures = @($items | Where-Object status -eq 'failed').Count
        [pscustomobject][ordered]@{
            checkId = $_.Name; sampleCount = $items.Count; passedCount = $items.Count - $failures
            failedCount = $failures; failurePercent = [Math]::Round($failures * 100.0 / $items.Count, 2)
            medianDurationSeconds = Get-Median @($items.durationSeconds)
            transitionCount = $transitions; transitionPercent = $transitionPercent
            flaky = $items.Count -ge [int]$telemetryPolicy.minimumSamples -and $failures -gt 0 -and
                $failures -lt $items.Count -and $transitionPercent -ge [double]$telemetryPolicy.flakyTransitionPercent
            latestStatus = [string]$items[-1].status; latestAtUtc = [string]$items[-1].recordedAtUtc
        }
    } | Sort-Object checkId)
}

$registryLock = if ($Action -eq 'record') { Enter-RegistryLock } else { $null }
try {
$registry = Read-Registry
$validation = Test-Registry $registry
if ($Action -eq 'verify') {
    $result = [pscustomobject][ordered]@{ action = 'verify'; valid = $validation.valid; totalCount = @($registry.events).Count; registryHash = $registry.registryHash; issues = @($validation.issues) }
} elseif (-not $validation.valid) {
    throw "Verification telemetry registry is invalid: $(@($validation.issues) -join ' ')"
} elseif ($Action -eq 'record') {
    if ([string]::IsNullOrWhiteSpace($CheckId) -or
        [string]::IsNullOrWhiteSpace($Status) -or $DurationSeconds -lt 0) { throw 'record requires CheckId, Status, and non-negative DurationSeconds; WorkspacePath defaults to @wiki.' }
    if (@($registry.events).Count -ge [int]$telemetryPolicy.retentionCount) {
        throw 'Verification telemetry retention limit is reached; archive history before recording more events.'
    }
    $workspace = if ([string]::IsNullOrWhiteSpace($WorkspacePath) -or $WorkspacePath -eq '@wiki') { '@wiki' } else { $WorkspacePath.Replace('\', '/').TrimEnd('/') }
    if ($workspace -ne '@wiki' -and ([IO.Path]::IsPathRooted($WorkspacePath) -or $workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$')) { throw 'WorkspacePath must identify one task workspace or use @wiki for a repository-level check.' }
    $packetFingerprint = ''
    if ($workspace -ne '@wiki') {
        $packetPath = Join-Path (Join-Path $repositoryRoot $workspace) 'change-packet.json'
        if (-not (Test-Path -LiteralPath $packetPath -PathType Leaf)) { throw "Change packet is absent: $workspace/change-packet.json" }
        $packetFingerprint = [string](Get-Content -LiteralPath $packetPath -Raw | ConvertFrom-Json).fingerprint
    }
    $previousHash = if (@($registry.events).Count -eq 0) { '' } else { [string]$registry.events[-1].eventHash }
    $event = [pscustomobject][ordered]@{
        schemaVersion = 1; sequence = @($registry.events).Count + 1; id = [guid]::NewGuid().ToString('N')
        recordedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o'); workspace = $workspace
        packetFingerprint = $packetFingerprint; checkId = $CheckId; status = $Status
        durationSeconds = [Math]::Round($DurationSeconds, 2)
        commandHash = $(if ([string]::IsNullOrWhiteSpace($Command)) { '' } else { Get-Hash $Command })
        policyFingerprint = (Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        previousHash = $previousHash; eventHash = ''
    }
    $event.eventHash = Get-Hash (Get-EventPayload $event)
    $registry.events = @($registry.events) + @($event)
    Write-Registry $registry
    $result = [pscustomobject][ordered]@{ action = 'record'; valid = $true; event = $event; registryHash = $registry.registryHash; issues = @() }
} else {
    $events = @($registry.events | Where-Object { [string]::IsNullOrWhiteSpace($CheckId) -or $_.checkId -eq $CheckId })
    if ($Action -eq 'metrics') {
        $metrics = @(Get-Metrics $events)
        $failureCount = @($events | Where-Object status -eq 'failed').Count
        $result = [pscustomobject][ordered]@{
            action = 'metrics'; valid = $true; totalCount = $events.Count; flakyCount = @($metrics | Where-Object flaky).Count
            passedCount = $events.Count - $failureCount; failedCount = $failureCount
            successRatePercent = $(if ($events.Count -eq 0) { $null } else { [Math]::Round(100.0 * ($events.Count - $failureCount) / $events.Count, 2) })
            health = $(if ($events.Count -eq 0) { 'insufficient-data' } elseif ($failureCount -gt 0) { 'attention' } else { 'healthy' })
            registryHash = $registry.registryHash; metrics = $metrics; issues = @()
        }
    } else {
        $result = [pscustomobject][ordered]@{ action = 'list'; valid = $true; totalCount = $events.Count; registryHash = $registry.registryHash; events = $events; issues = @() }
    }
}
} finally {
    if ($registryLock) { $registryLock.Dispose() }
}

$result | Add-Member -NotePropertyName registryPath -NotePropertyValue $registryPath
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    if ($Action -eq 'record') {
        Write-Host "Verification telemetry recorded: $($result.event.checkId)=$($result.event.status), duration=$($result.event.durationSeconds)s"
    } else {
        Write-Host "Verification telemetry: action=$Action, valid=$($result.valid), total=$($result.totalCount)"
    }
    if ($Action -eq 'metrics') {
        foreach ($metric in @($result.metrics)) { Write-Host " - $($metric.checkId): samples=$($metric.sampleCount), failures=$($metric.failurePercent)%, median=$($metric.medianDurationSeconds)s, flaky=$($metric.flaky)" }
    }
    foreach ($issue in @($result.issues)) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
