[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('list', 'open', 'reset', 'verify', 'prune')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [string]$CircuitId,
    [string]$SourceWatchdogId,
    [string]$Reason,
    [Nullable[int]]$CooldownMinutes,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$FailOnOpen,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$schedulerRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler'
$circuitRoot = Join-Path $schedulerRoot 'circuits'
$lockPath = Join-Path $schedulerRoot '.circuit-lock'
$policy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json
$circuitPolicy = $policy.scheduler.watchdog.circuitBreaker
$now = $AsOfUtc.ToUniversalTime()
$effectiveCooldown = if ($null -ne $CooldownMinutes) { [int]$CooldownMinutes } else { [int]$circuitPolicy.defaultCooldownMinutes }
if ($effectiveCooldown -lt 1 -or $effectiveCooldown -gt [int]$circuitPolicy.maximumCooldownMinutes) {
    throw "CooldownMinutes must be between 1 and $($circuitPolicy.maximumCooldownMinutes)."
}

function Get-Hash([object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-Payload([object]$Receipt) {
    [ordered]@{
        schemaVersion = [int]$Receipt.schemaVersion
        circuitId = [string]$Receipt.circuitId
        event = [string]$Receipt.event
        workspace = [string]$Receipt.workspace
        packetFingerprint = [string]$Receipt.packetFingerprint
        occurredAtUtc = ([DateTimeOffset]$Receipt.occurredAtUtc).ToUniversalTime().ToString('o')
        openUntilUtc = $(if ([string]::IsNullOrWhiteSpace([string]$Receipt.openUntilUtc)) {
            ''
        } else {
            ([DateTimeOffset]$Receipt.openUntilUtc).ToUniversalTime().ToString('o')
        })
        reason = [string]$Receipt.reason
        policyFingerprint = [string]$Receipt.policyFingerprint
        predecessorCircuitId = [string]$Receipt.predecessorCircuitId
        sourceWatchdogId = [string]$Receipt.sourceWatchdogId
    }
}
function Test-Receipt([object]$Receipt) {
    $issues = [System.Collections.Generic.List[string]]::new()
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.circuitId -notmatch '^[a-f0-9]{32}$') { $issues.Add('circuitId is invalid.') }
    if ([string]$Receipt.event -notin @('opened', 'reset')) { $issues.Add('event must be opened or reset.') }
    if ([string]$Receipt.workspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') { $issues.Add('workspace is invalid.') }
    if ([string]$Receipt.packetFingerprint -notmatch '^[a-f0-9]{64}$') { $issues.Add('packetFingerprint is invalid.') }
    if ([string]$Receipt.circuitHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('circuitHash is invalid.') }
    if ($Receipt.event -eq 'opened' -and [string]::IsNullOrWhiteSpace([string]$Receipt.openUntilUtc)) { $issues.Add('opened circuit requires openUntilUtc.') }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}
function Get-Files {
    if (-not (Test-Path -LiteralPath $circuitRoot -PathType Container)) { return @() }
    @(Get-ChildItem -LiteralPath $circuitRoot -File -Filter '*.json' | Sort-Object Name)
}
function Read-Receipt([string]$Id) {
    if ($Id -notmatch '^[a-f0-9]{32}$') { throw 'CircuitId must be a 32-character lowercase hexadecimal identifier.' }
    $matches = @(Get-Files | Where-Object BaseName -like "*-$Id")
    if ($matches.Count -ne 1) { throw "Circuit receipt does not exist or is ambiguous: $Id" }
    Get-Content -LiteralPath $matches[0].FullName -Raw | ConvertFrom-Json
}
function Normalize-Workspace([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { throw 'WorkspacePath is required.' }
    if ([IO.Path]::IsPathRooted($Value)) { throw 'WorkspacePath must be repository-relative.' }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/]+$') { throw 'WorkspacePath must identify one task workspace.' }
    $normalized
}
function Get-CurrentFingerprint([string]$Workspace) {
    $descriptorPath = Join-Path $repositoryRoot ($Workspace + '/workspace.json')
    if (-not (Test-Path -LiteralPath $descriptorPath -PathType Leaf)) { throw "Workspace descriptor was not found: $Workspace" }
    $descriptor = Get-Content -LiteralPath $descriptorPath -Raw | ConvertFrom-Json
    $fingerprint = [string]$descriptor.currentPacketFingerprint
    if ($fingerprint -notmatch '^[a-f0-9]{64}$') { throw "Workspace packet fingerprint is invalid: $Workspace" }
    $fingerprint
}
function Get-Receipts {
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($file in Get-Files) {
        try {
            $receipt = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
            $validation = Test-Receipt $receipt
            $items.Add([pscustomobject][ordered]@{ receipt = $receipt; path = $file.FullName; valid = $validation.valid; issues = @($validation.issues) })
        } catch {
            $items.Add([pscustomobject][ordered]@{ receipt = $null; path = $file.FullName; valid = $false; issues = @($_.Exception.Message) })
        }
    }
    @($items)
}
function Get-View {
    $records = @(Get-Receipts)
    $states = [System.Collections.Generic.List[object]]::new()
    foreach ($group in @($records | Where-Object { $_.valid -and $null -ne $_.receipt } | Group-Object { [string]$_.receipt.workspace })) {
        $latestRecord = @($group.Group | Sort-Object { [DateTime]::Parse([string]$_.receipt.occurredAtUtc).ToUniversalTime() }, { [string]$_.receipt.circuitId })[-1]
        $latest = $latestRecord.receipt
        $currentFingerprint = ''
        try { $currentFingerprint = Get-CurrentFingerprint ([string]$latest.workspace) } catch {}
        $fingerprintChanged = -not [string]::IsNullOrWhiteSpace($currentFingerprint) -and $currentFingerprint -cne [string]$latest.packetFingerprint
        $cooldownExpired = $latest.event -eq 'opened' -and $now -ge [DateTime]::Parse([string]$latest.openUntilUtc).ToUniversalTime()
        $open = $latest.event -eq 'opened' -and -not $fingerprintChanged -and -not $cooldownExpired
        $states.Add([pscustomobject][ordered]@{
            workspace = [string]$latest.workspace
            state = $(if ($open) { 'open' } elseif ($latest.event -eq 'reset') { 'reset' } elseif ($fingerprintChanged) { 'auto-reset-packet-changed' } else { 'auto-reset-cooldown-expired' })
            open = $open
            circuitId = [string]$latest.circuitId
            packetFingerprint = [string]$latest.packetFingerprint
            currentPacketFingerprint = $currentFingerprint
            openedAtUtc = $(if ($latest.event -eq 'opened') { [string]$latest.occurredAtUtc } else { '' })
            openUntilUtc = [string]$latest.openUntilUtc
            reason = [string]$latest.reason
            sourceWatchdogId = [string]$latest.sourceWatchdogId
            eventCount = $group.Count
        })
    }
    [pscustomobject][ordered]@{
        schemaVersion = 1
        asOfUtc = $now.ToString('o')
        receiptCount = $records.Count
        invalidReceiptCount = @($records | Where-Object { -not $_.valid }).Count
        circuitCount = $states.Count
        openCount = @($states | Where-Object open).Count
        circuits = @($states | Sort-Object workspace)
    }
}
function Write-Receipt([object]$Receipt) {
    if (-not (Test-Path -LiteralPath $circuitRoot)) { New-Item -ItemType Directory -Path $circuitRoot | Out-Null }
    $fileName = "$($now.ToString('yyyyMMddTHHmmssfffZ'))-$($Receipt.circuitId).json"
    $temporaryPath = Join-Path $circuitRoot ('.circuit-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [IO.File]::WriteAllText($temporaryPath, (($Receipt | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination (Join-Path $circuitRoot $fileName)
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [IO.File]::Delete($temporaryPath) }
    }
    ".artifacts/llm-wiki/scheduler/circuits/$fileName"
}

$mutating = $Action -in @('open', 'reset', 'prune')
$lockStream = $null
if ($mutating) {
    if (-not (Test-Path -LiteralPath $schedulerRoot)) { New-Item -ItemType Directory -Path $schedulerRoot | Out-Null }
    if ((Test-Path -LiteralPath $lockPath -PathType Leaf) -and ($now - (Get-Item -LiteralPath $lockPath).LastWriteTimeUtc).TotalMinutes -gt 10) { [IO.File]::Delete($lockPath) }
    try { $lockStream = [IO.File]::Open($lockPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None) }
    catch { throw 'Workspace circuit registry is already being mutated; retry after it completes.' }
}

try {
    if ($Action -in @('open', 'reset')) {
        $workspace = Normalize-Workspace $WorkspacePath
        $fingerprint = Get-CurrentFingerprint $workspace
        $viewBefore = Get-View
        $current = $viewBefore.circuits | Where-Object workspace -eq $workspace | Select-Object -First 1
        if ($Action -eq 'open' -and $null -ne $current -and $current.open -and $current.packetFingerprint -ceq $fingerprint) {
            $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'open'; changed = $false; circuit = $current; path = '' }
        } else {
            if ($Action -eq 'reset' -and ($null -eq $current -or -not $current.open)) { throw "Workspace does not have an open circuit: $workspace" }
            $event = if ($Action -eq 'open') { 'opened' } else { 'reset' }
            $effectiveReason = if ([string]::IsNullOrWhiteSpace($Reason)) { $(if ($Action -eq 'open') { 'Retry budget exhausted.' } else { 'Manual reset.' }) } else { $Reason.Trim() }
            $receipt = [pscustomobject][ordered]@{
                schemaVersion = 1
                circuitId = [guid]::NewGuid().ToString('N')
                event = $event
                workspace = $workspace
                packetFingerprint = $fingerprint
                occurredAtUtc = $now.ToString('o')
                openUntilUtc = $(if ($Action -eq 'open') { $now.AddMinutes($effectiveCooldown).ToString('o') } else { '' })
                reason = $effectiveReason
                policyFingerprint = [string]$policy.fingerprint
                predecessorCircuitId = $(if ($null -ne $current) { [string]$current.circuitId } else { '' })
                sourceWatchdogId = ''
                circuitHash = ''
            }
            if (-not [string]::IsNullOrWhiteSpace($SourceWatchdogId) -and $Action -eq 'open') {
                if ($SourceWatchdogId -notmatch '^[a-f0-9]{32}$') { throw 'SourceWatchdogId must be a 32-character lowercase hexadecimal identifier.' }
                $receipt.sourceWatchdogId = $SourceWatchdogId
            }
            $receipt.circuitHash = Get-Hash (Get-Payload $receipt)
            $path = Write-Receipt $receipt
            $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = $Action; changed = $true; receipt = $receipt; circuit = (Get-View).circuits | Where-Object workspace -eq $workspace | Select-Object -First 1; path = $path }
        }
    } elseif ($Action -eq 'verify') {
        if ([string]::IsNullOrWhiteSpace($CircuitId)) { throw 'verify requires CircuitId.' }
        $receipt = Read-Receipt $CircuitId
        $validation = Test-Receipt $receipt
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'verify'; valid = $validation.valid; issues = @($validation.issues); receipt = $receipt }
    } elseif ($Action -eq 'prune') {
        $records = @(Get-Receipts | Sort-Object path -Descending)
        $protected = [System.Collections.Generic.HashSet[string]]::new()
        foreach ($record in @($records | Select-Object -First ([int]$circuitPolicy.retentionCount))) {
            if ($null -ne $record.receipt) { [void]$protected.Add([string]$record.receipt.circuitId) }
        }
        foreach ($state in @((Get-View).circuits | Where-Object open)) { [void]$protected.Add([string]$state.circuitId) }
        $receiptById = @{}
        foreach ($record in $records) {
            if ($null -ne $record.receipt) { $receiptById[[string]$record.receipt.circuitId] = $record.receipt }
        }
        $expanded = $true
        while ($expanded) {
            $expanded = $false
            foreach ($id in @($protected)) {
                if (-not $receiptById.ContainsKey($id)) { continue }
                $predecessor = [string]$receiptById[$id].predecessorCircuitId
                if (-not [string]::IsNullOrWhiteSpace($predecessor) -and $protected.Add($predecessor)) { $expanded = $true }
            }
        }
        $removed = [System.Collections.Generic.List[string]]::new()
        $kept = 0
        foreach ($record in $records) {
            $id = if ($null -ne $record.receipt) { [string]$record.receipt.circuitId } else { '' }
            if ($protected.Contains($id)) { $kept++; continue }
            [IO.File]::Delete($record.path)
            $removed.Add($record.path)
        }
        $response = [pscustomobject][ordered]@{ schemaVersion = 1; action = 'prune'; removedCount = $removed.Count; keptCount = $kept; removedPaths = @($removed) }
    } else {
        $response = Get-View
    }
} finally {
    if ($null -ne $lockStream) { $lockStream.Dispose() }
    if ($mutating -and (Test-Path -LiteralPath $lockPath)) { [IO.File]::Delete($lockPath) }
}

if ($Format -eq 'Json') { $response | ConvertTo-Json -Depth 20 }
else {
    if ($Action -eq 'list') {
        Write-Host "Workspace circuits: open=$($response.openCount), total=$($response.circuitCount), receipts=$($response.receiptCount), invalid=$($response.invalidReceiptCount)"
        foreach ($circuit in $response.circuits) { Write-Host " - [$($circuit.state)] $($circuit.workspace): $($circuit.reason)" }
    } else { $response | Format-List | Out-Host }
}
if ($FailOnOpen -and $Action -eq 'list' -and $response.openCount -gt 0) { exit 1 }
if ($Action -eq 'verify' -and -not $response.valid) { exit 1 }
