[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('list', 'acquire', 'heartbeat', 'release', 'prune')]
    [string]$Action = 'list',
    [string]$WorkspacePath,
    [string]$Owner,
    [string]$LeaseId,
    [Nullable[int]]$LeaseMinutes,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json
$effectiveLeaseMinutes = if ($null -ne $LeaseMinutes) { [int]$LeaseMinutes } else { [int]$policy.scheduler.defaultLeaseMinutes }
if ($effectiveLeaseMinutes -lt 1 -or $effectiveLeaseMinutes -gt [int]$policy.scheduler.maximumLeaseMinutes) {
    throw "LeaseMinutes must be between 1 and $($policy.scheduler.maximumLeaseMinutes)."
}
$now = $AsOfUtc.ToUniversalTime()
$schedulerPath = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler'
$registryPath = Join-Path $schedulerPath 'leases.json'
$lockPath = Join-Path $schedulerPath '.lease-lock'

function Normalize-Workspace([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value) -or [System.IO.Path]::IsPathRooted($Value)) {
        throw 'WorkspacePath must be a repository-relative task workspace.'
    }
    $normalized = $Value.Replace('\', '/').TrimEnd('/')
    if ($normalized -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') {
        throw 'WorkspacePath must identify one non-hidden workspace directly inside .artifacts/llm-wiki/tasks.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $normalized) -PathType Container)) {
        throw "Task workspace does not exist: $normalized"
    }
    return $normalized
}
function Read-Registry {
    if (-not (Test-Path -LiteralPath $registryPath -PathType Leaf)) {
        return [pscustomobject][ordered]@{ schemaVersion = 1; leases = @() }
    }
    $registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    if ($registry.schemaVersion -ne 1) { throw "Unsupported lease registry schemaVersion: $($registry.schemaVersion)" }
    return $registry
}
function Write-Registry([object]$Registry) {
    if (-not (Test-Path -LiteralPath $schedulerPath)) { New-Item -ItemType Directory -Path $schedulerPath | Out-Null }
    $temporaryPath = Join-Path $schedulerPath ('.leases-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [System.IO.File]::WriteAllText($temporaryPath, (($Registry | ConvertTo-Json -Depth 10) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination $registryPath -Force
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [System.IO.File]::Delete($temporaryPath) }
    }
}
function Get-LeaseView([object]$Lease) {
    $expires = [DateTime]::Parse([string]$Lease.expiresAtUtc).ToUniversalTime()
    [pscustomobject][ordered]@{
        leaseId = [string]$Lease.leaseId
        workspace = [string]$Lease.workspace
        owner = [string]$Lease.owner
        acquiredAtUtc = [string]$Lease.acquiredAtUtc
        heartbeatAtUtc = [string]$Lease.heartbeatAtUtc
        expiresAtUtc = [string]$Lease.expiresAtUtc
        remainingMinutes = [Math]::Round([Math]::Max(0, ($expires - $now).TotalMinutes), 2)
        active = $expires -gt $now
    }
}

$mutating = $Action -in @('acquire', 'heartbeat', 'release', 'prune')
$lockStream = $null
if ($mutating) {
    if (-not (Test-Path -LiteralPath $schedulerPath)) { New-Item -ItemType Directory -Path $schedulerPath | Out-Null }
    if (Test-Path -LiteralPath $lockPath -PathType Leaf) {
        $lockAge = ([DateTime]::UtcNow - (Get-Item -LiteralPath $lockPath).LastWriteTimeUtc).TotalMinutes
        if ($lockAge -gt 5) { [System.IO.File]::Delete($lockPath) }
    }
    try {
        $lockStream = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    } catch {
        throw 'Task lease registry is busy; retry after the current scheduler mutation completes.'
    }
}
try {
    $registry = Read-Registry
    $allLeases = @($registry.leases)
    $activeLeases = @($allLeases | Where-Object { [DateTime]::Parse([string]$_.expiresAtUtc).ToUniversalTime() -gt $now })
    $expiredLeases = @($allLeases | Where-Object { [DateTime]::Parse([string]$_.expiresAtUtc).ToUniversalTime() -le $now })
    $changed = $false
    $selectedLease = $null
    if ($Action -eq 'acquire') {
        $normalizedWorkspace = Normalize-Workspace $WorkspacePath
        if ([string]::IsNullOrWhiteSpace($Owner) -or $Owner.Length -gt 200) { throw 'Owner is required and must not exceed 200 characters.' }
        $existing = $activeLeases | Where-Object workspace -eq $normalizedWorkspace | Select-Object -First 1
        if ($null -ne $existing) { throw "Task workspace is already leased by '$($existing.owner)' until $($existing.expiresAtUtc)." }
        $selectedLease = [pscustomobject][ordered]@{
            leaseId = [guid]::NewGuid().ToString('N')
            workspace = $normalizedWorkspace
            owner = $Owner
            acquiredAtUtc = $now.ToString('o')
            heartbeatAtUtc = $now.ToString('o')
            expiresAtUtc = $now.AddMinutes($effectiveLeaseMinutes).ToString('o')
        }
        $activeLeases += $selectedLease
        $changed = $true
    } elseif ($Action -eq 'heartbeat') {
        if ([string]::IsNullOrWhiteSpace($LeaseId)) { throw 'heartbeat requires LeaseId.' }
        $selectedLease = $activeLeases | Where-Object leaseId -eq $LeaseId | Select-Object -First 1
        if ($null -eq $selectedLease) { throw 'Active lease was not found; reacquire the task.' }
        if (-not [string]::IsNullOrWhiteSpace($Owner) -and [string]$selectedLease.owner -cne $Owner) { throw 'Lease owner does not match.' }
        $selectedLease.heartbeatAtUtc = $now.ToString('o')
        $selectedLease.expiresAtUtc = $now.AddMinutes($effectiveLeaseMinutes).ToString('o')
        $changed = $true
    } elseif ($Action -eq 'release') {
        if ([string]::IsNullOrWhiteSpace($LeaseId)) { throw 'release requires LeaseId.' }
        $selectedLease = $activeLeases | Where-Object leaseId -eq $LeaseId | Select-Object -First 1
        if ($null -eq $selectedLease) { throw 'Active lease was not found or already expired.' }
        if (-not [string]::IsNullOrWhiteSpace($Owner) -and [string]$selectedLease.owner -cne $Owner) { throw 'Lease owner does not match.' }
        $activeLeases = @($activeLeases | Where-Object leaseId -ne $LeaseId)
        $changed = $true
    } elseif ($Action -eq 'prune' -and $expiredLeases.Count -gt 0) {
        $changed = $true
    }
    if ($changed) {
        $registry = [pscustomobject][ordered]@{ schemaVersion = 1; updatedAtUtc = $now.ToString('o'); leases = @($activeLeases) }
        Write-Registry $registry
    }
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = $Action
        asOfUtc = $now.ToString('o')
        changed = $changed
        activeCount = $activeLeases.Count
        expiredCount = $expiredLeases.Count
        lease = $(if ($null -ne $selectedLease) { Get-LeaseView $selectedLease } else { $null })
        leases = @($activeLeases | ForEach-Object { Get-LeaseView $_ } | Sort-Object workspace)
    }
} finally {
    if ($null -ne $lockStream) { $lockStream.Dispose() }
    if ($mutating -and (Test-Path -LiteralPath $lockPath)) { [System.IO.File]::Delete($lockPath) }
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 8
} else {
    Write-Host "Task leases: action=$Action, active=$($result.activeCount), expired=$($result.expiredCount), changed=$($result.changed)"
    foreach ($lease in $result.leases) { Write-Host " - $($lease.workspace): $($lease.owner), expires $($lease.expiresAtUtc)" }
}
