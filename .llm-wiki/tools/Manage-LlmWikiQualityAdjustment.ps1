[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('record', 'list', 'verify', 'metrics', 'prune')]
    [string]$Action = 'list',
    [string]$AdjustmentId,
    [string]$DispatchId,
    [string]$Owner,
    [string]$AdjustmentType,
    [string]$Reason,
    [string[]]$Evidence = @(),
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [switch]$Apply,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$root = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler/quality-adjustments'
$feedbackRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler/context-feedback'
$dispatchRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler/dispatches'
$policy = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/workspace-policies.json') -Raw | ConvertFrom-Json
$adjustmentPolicy = $policy.scheduler.contextBundles.feedback.qualityAdjustments

function Get-Hash([object]$Value) {
    $json = ConvertTo-Json -InputObject $(if ($null -eq $Value) { @() } else { $Value }) -Depth 30 -Compress
    if ($null -eq $json) { $json = '[]' }
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-Payload([object]$Receipt) {
    [pscustomobject][ordered]@{
        schemaVersion = $Receipt.schemaVersion
        adjustmentId = $Receipt.adjustmentId
        dispatchId = $Receipt.dispatchId
        workspace = $Receipt.workspace
        owner = $Receipt.owner
        adjustmentType = $Receipt.adjustmentType
        delta = $Receipt.delta
        reason = $Receipt.reason
        evidence = @($Receipt.evidence)
        feedbackHash = $Receipt.feedbackHash
        dispatchHeadEventHash = $Receipt.dispatchHeadEventHash
        recordedAtUtc = ([DateTimeOffset]$Receipt.recordedAtUtc).ToUniversalTime().ToString('o')
    }
}
function Get-Delta([string]$Type) {
    [int]$adjustmentPolicy.PSObject.Properties["$($Type)Delta"].Value
}
function Get-Files {
    if (-not (Test-Path -LiteralPath $root -PathType Container)) { return @() }
    @(Get-ChildItem -LiteralPath $root -File -Filter '*.json' | Sort-Object Name)
}
function Test-Receipt([object]$Receipt) {
    $issues = [Collections.Generic.List[string]]::new()
    if ($Receipt.schemaVersion -ne 1) { $issues.Add('schemaVersion must be 1.') }
    if ([string]$Receipt.adjustmentId -notmatch '^[a-f0-9]{32}$') { $issues.Add('adjustmentId is invalid.') }
    if ([string]$Receipt.dispatchId -notmatch '^[a-f0-9]{32}$') { $issues.Add('dispatchId is invalid.') }
    if ([string]$Receipt.adjustmentType -notin @('rework', 'rollback', 'regression', 'recovery')) { $issues.Add('adjustmentType is invalid.') }
    if ([int]$Receipt.delta -ne (Get-Delta ([string]$Receipt.adjustmentType))) { $issues.Add('delta does not match policy.') }
    if ([string]$Receipt.feedbackHash -notmatch '^[a-f0-9]{64}$') { $issues.Add('feedbackHash is invalid.') }
    if ([string]$Receipt.dispatchHeadEventHash -notmatch '^[a-f0-9]{64}$') { $issues.Add('dispatchHeadEventHash is invalid.') }
    if ([string]::IsNullOrWhiteSpace([string]$Receipt.reason)) { $issues.Add('reason is required.') }
    if (@($Receipt.evidence | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count -eq 0) { $issues.Add('evidence is required.') }
    if ([string]$Receipt.adjustmentHash -cne (Get-Hash (Get-Payload $Receipt))) { $issues.Add('adjustmentHash is invalid.') }
    $feedbackPath = Join-Path $feedbackRoot "$($Receipt.dispatchId).json"
    if (Test-Path -LiteralPath $feedbackPath -PathType Leaf) {
        $feedback = Get-Content -LiteralPath $feedbackPath -Raw | ConvertFrom-Json
        if ([string]$feedback.feedbackHash -cne [string]$Receipt.feedbackHash) { $issues.Add('Linked feedback hash does not match.') }
        if ([string]$feedback.owner -cne [string]$Receipt.owner) { $issues.Add('Linked feedback owner does not match.') }
        if ([string]$feedback.workspace -cne [string]$Receipt.workspace) { $issues.Add('Linked feedback workspace does not match.') }
    }
    $dispatchPath = Join-Path $dispatchRoot "$($Receipt.dispatchId).json"
    if (Test-Path -LiteralPath $dispatchPath -PathType Leaf) {
        $dispatch = Get-Content -LiteralPath $dispatchPath -Raw | ConvertFrom-Json
        if ([string]$dispatch.owner -cne [string]$Receipt.owner) { $issues.Add('Linked dispatch owner does not match.') }
        if ([string]$dispatch.workspace -cne [string]$Receipt.workspace) { $issues.Add('Linked dispatch workspace does not match.') }
        if ([string]$dispatch.events[-1].type -notin @('completed', 'failed')) { $issues.Add('Linked dispatch is not terminal.') }
        if ([string]$dispatch.events[-1].eventHash -cne [string]$Receipt.dispatchHeadEventHash) { $issues.Add('Linked dispatch head event hash does not match.') }
    }
    [pscustomobject][ordered]@{ valid = $issues.Count -eq 0; issues = @($issues) }
}
function Get-Views {
    @((Get-Files) | ForEach-Object {
        try {
            $receipt = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
            $validation = Test-Receipt $receipt
            [pscustomobject][ordered]@{ receipt = $receipt; valid = $validation.valid; issues = @($validation.issues); path = $_.FullName }
        } catch {
            [pscustomobject][ordered]@{ receipt = $null; valid = $false; issues = @($_.Exception.Message); path = $_.FullName }
        }
    })
}
function Get-Metrics([object[]]$Views) {
    $valid = @($Views | Where-Object valid | ForEach-Object receipt)
    $profiles = @($valid | Group-Object dispatchId | ForEach-Object {
        [pscustomobject][ordered]@{
            dispatchId = $_.Name
            owner = [string]$_.Group[0].owner
            eventCount = $_.Count
            totalDelta = [int](($_.Group.delta | Measure-Object -Sum).Sum)
            types = @($_.Group.adjustmentType | Sort-Object -Unique)
            adjustmentHashes = @($_.Group.adjustmentHash | Sort-Object)
        }
    } | Sort-Object dispatchId)
    [pscustomobject][ordered]@{
        validReceiptCount = $valid.Count
        invalidReceiptCount = @($Views | Where-Object { -not $_.valid }).Count
        fingerprint = Get-Hash @($valid | Sort-Object adjustmentId | ForEach-Object { "$($_.adjustmentId)|$($_.adjustmentHash)" })
        dispatchProfiles = $profiles
    }
}

if ($Action -eq 'record') {
    if ($DispatchId -notmatch '^[a-f0-9]{32}$') { throw 'DispatchId is invalid.' }
    if ($AdjustmentType -notin @('rework', 'rollback', 'regression', 'recovery')) { throw 'AdjustmentType is required and must match policy.' }
    if ([string]::IsNullOrWhiteSpace($Reason) -or @($Evidence | Where-Object { $_ }).Count -eq 0) { throw 'record requires Reason and Evidence.' }
    $feedbackPath = Join-Path $feedbackRoot "$DispatchId.json"
    $dispatchPath = Join-Path $dispatchRoot "$DispatchId.json"
    if (-not (Test-Path -LiteralPath $feedbackPath -PathType Leaf)) { throw 'Quality adjustment requires terminal context feedback.' }
    if (-not (Test-Path -LiteralPath $dispatchPath -PathType Leaf)) { throw 'Linked dispatch receipt is absent.' }
    $feedback = Get-Content -LiteralPath $feedbackPath -Raw | ConvertFrom-Json
    $dispatch = Get-Content -LiteralPath $dispatchPath -Raw | ConvertFrom-Json
    if ([string]$feedback.owner -cne $Owner) { throw 'Owner must match terminal feedback.' }
    if (@((Get-Files) | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json } | Where-Object dispatchId -eq $DispatchId).Count -ge [int]$adjustmentPolicy.maximumEventsPerDispatch) {
        throw 'Maximum quality adjustments per dispatch was reached.'
    }
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        adjustmentId = [guid]::NewGuid().ToString('N')
        dispatchId = $DispatchId
        workspace = [string]$feedback.workspace
        owner = $Owner
        adjustmentType = $AdjustmentType
        delta = Get-Delta $AdjustmentType
        reason = $Reason
        evidence = @($Evidence | Where-Object { $_ })
        feedbackHash = [string]$feedback.feedbackHash
        dispatchHeadEventHash = [string]$dispatch.events[-1].eventHash
        recordedAtUtc = $AsOfUtc.ToUniversalTime().ToString('o')
        adjustmentHash = ''
    }
    $receipt.adjustmentHash = Get-Hash (Get-Payload $receipt)
    $validation = Test-Receipt $receipt
    if (-not $validation.valid) { throw "Quality adjustment is invalid: $(@($validation.issues) -join ' ')" }
    if (-not (Test-Path -LiteralPath $root)) { New-Item -ItemType Directory -Path $root | Out-Null }
    $path = Join-Path $root "$($receipt.adjustmentId).json"
    $temporaryPath = "$path.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temporaryPath, (($receipt | ConvertTo-Json -Depth 20) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
        [IO.File]::Move($temporaryPath, $path)
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [IO.File]::Delete($temporaryPath) }
    }
    $result = [pscustomobject][ordered]@{ action = 'record'; valid = $true; adjustment = $receipt; path = $path.Substring($repositoryRoot.Length + 1).Replace('\', '/') }
} elseif ($Action -eq 'verify') {
    if ($AdjustmentId -notmatch '^[a-f0-9]{32}$') { throw 'verify requires AdjustmentId.' }
    $path = Join-Path $root "$AdjustmentId.json"
    if (-not (Test-Path -LiteralPath $path)) { throw "Quality adjustment does not exist: $AdjustmentId" }
    $receipt = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $validation = Test-Receipt $receipt
    $result = [pscustomobject][ordered]@{ action = 'verify'; valid = $validation.valid; issues = @($validation.issues); adjustment = $receipt }
} elseif ($Action -eq 'prune') {
    $views = @(Get-Views)
    $candidates = @($views | Where-Object valid | Sort-Object { [DateTime]$_.receipt.recordedAtUtc } -Descending | Select-Object -Skip ([int]$adjustmentPolicy.retentionCount))
    if ($Apply) { foreach ($candidate in $candidates) { [IO.File]::Delete([string]$candidate.path) } }
    $result = [pscustomobject][ordered]@{ action = 'prune'; valid = @($views | Where-Object { -not $_.valid }).Count -eq 0; apply = [bool]$Apply; candidateCount = $candidates.Count; changedCount = $(if ($Apply) { $candidates.Count } else { 0 }) }
} else {
    $views = @(Get-Views)
    $metrics = Get-Metrics $views
    $result = if ($Action -eq 'metrics') {
        [pscustomobject][ordered]@{ action = 'metrics'; valid = $metrics.invalidReceiptCount -eq 0; metrics = $metrics }
    } else {
        [pscustomobject][ordered]@{ action = 'list'; valid = $metrics.invalidReceiptCount -eq 0; totalCount = $views.Count; invalidCount = $metrics.invalidReceiptCount; adjustments = @($views | ForEach-Object { $_.receipt }) }
    }
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 30 } else {
    Write-Host "Quality adjustments: action=$($result.action), valid=$($result.valid)"
    if ($result.PSObject.Properties['metrics']) { Write-Host "Receipts=$($result.metrics.validReceiptCount), dispatches=$(@($result.metrics.dispatchProfiles).Count), fingerprint=$($result.metrics.fingerprint)" }
    if ($result.PSObject.Properties['totalCount']) { Write-Host "Receipts=$($result.totalCount), invalid=$($result.invalidCount)" }
    if ($result.PSObject.Properties['issues']) { foreach ($issue in @($result.issues | Where-Object { $_ })) { Write-Host " - $issue" } }
}
if ($FailOnInvalid -and -not $result.valid) { exit 1 }
