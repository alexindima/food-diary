[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('list', 'register', 'heartbeat', 'quarantine', 'unquarantine', 'unregister', 'prune')]
    [string]$Action = 'list',
    [string]$Owner,
    [string]$AgentId,
    [string[]]$Capability,
    [Nullable[int]]$Capacity,
    [Nullable[int]]$RegistrationMinutes,
    [Nullable[int]]$QuarantineMinutes,
    [string]$Reason,
    [DateTime]$AsOfUtc = [DateTime]::UtcNow,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$policy = & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | ConvertFrom-Json
$registryPolicy = $policy.scheduler.agentRegistry
$effectiveCapacity = if ($null -ne $Capacity) { [int]$Capacity } else { [int]$registryPolicy.defaultCapacity }
$effectiveMinutes = if ($null -ne $RegistrationMinutes) { [int]$RegistrationMinutes } else { [int]$registryPolicy.defaultRegistrationMinutes }
$watchdogPolicy = $policy.scheduler.watchdog
$effectiveQuarantineMinutes = if ($null -ne $QuarantineMinutes) { [int]$QuarantineMinutes } else { [int]$watchdogPolicy.defaultQuarantineMinutes }
if ($effectiveCapacity -lt 1 -or $effectiveCapacity -gt [int]$registryPolicy.maximumCapacity) { throw "Capacity must be between 1 and $($registryPolicy.maximumCapacity)." }
if ($effectiveMinutes -lt 1 -or $effectiveMinutes -gt [int]$registryPolicy.maximumRegistrationMinutes) { throw "RegistrationMinutes must be between 1 and $($registryPolicy.maximumRegistrationMinutes)." }
if ($effectiveQuarantineMinutes -lt 1 -or $effectiveQuarantineMinutes -gt [int]$watchdogPolicy.maximumQuarantineMinutes) { throw "QuarantineMinutes must be between 1 and $($watchdogPolicy.maximumQuarantineMinutes)." }
$now = $AsOfUtc.ToUniversalTime()
$schedulerPath = Join-Path $repositoryRoot '.artifacts/llm-wiki/scheduler'
$registryPath = Join-Path $schedulerPath 'agents.json'
$lockPath = Join-Path $schedulerPath '.agent-lock'

function Read-Registry {
    if (-not (Test-Path -LiteralPath $registryPath -PathType Leaf)) { return [pscustomobject][ordered]@{ schemaVersion = 1; agents = @() } }
    $registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    if ($registry.schemaVersion -ne 1) { throw "Unsupported agent registry schemaVersion: $($registry.schemaVersion)" }
    return $registry
}
function Write-Registry([object]$Registry) {
    if (-not (Test-Path -LiteralPath $schedulerPath)) { New-Item -ItemType Directory -Path $schedulerPath | Out-Null }
    $temporaryPath = Join-Path $schedulerPath ('.agents-' + [guid]::NewGuid().ToString('N') + '.json')
    try {
        [System.IO.File]::WriteAllText($temporaryPath, (($Registry | ConvertTo-Json -Depth 10) + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination $registryPath -Force
    } finally {
        if (Test-Path -LiteralPath $temporaryPath) { [System.IO.File]::Delete($temporaryPath) }
    }
}
function Get-AgentView([object]$Agent, [object[]]$ActiveLeases) {
    $expires = [DateTime]::Parse([string]$Agent.expiresAtUtc).ToUniversalTime()
    $quarantineUntil = if ($null -ne $Agent.PSObject.Properties['quarantineUntilUtc'] -and -not [string]::IsNullOrWhiteSpace([string]$Agent.quarantineUntilUtc)) { [DateTime]::Parse([string]$Agent.quarantineUntilUtc).ToUniversalTime() } else { [DateTime]::MinValue }
    $quarantined = $quarantineUntil -gt $now
    $registered = $expires -gt $now
    $activeLeaseCount = @($ActiveLeases | Where-Object owner -eq $Agent.owner).Count
    [pscustomobject][ordered]@{
        agentId = [string]$Agent.agentId
        owner = [string]$Agent.owner
        capabilities = @($Agent.capabilities)
        capacity = [int]$Agent.capacity
        activeLeaseCount = $activeLeaseCount
        availableCapacity = $(if ($quarantined) { 0 } else { [Math]::Max(0, [int]$Agent.capacity - $activeLeaseCount) })
        registeredAtUtc = [string]$Agent.registeredAtUtc
        heartbeatAtUtc = [string]$Agent.heartbeatAtUtc
        expiresAtUtc = [string]$Agent.expiresAtUtc
        remainingMinutes = [Math]::Round([Math]::Max(0, ($expires - $now).TotalMinutes), 2)
        registered = $registered
        active = $registered -and -not $quarantined
        quarantined = $quarantined
        quarantineUntilUtc = $(if ($quarantined) { $quarantineUntil.ToString('o') } else { '' })
        quarantineReason = $(if ($quarantined) { [string]$Agent.quarantineReason } else { '' })
    }
}

$mutating = $Action -ne 'list'
$lockStream = $null
if ($mutating) {
    if (-not (Test-Path -LiteralPath $schedulerPath)) { New-Item -ItemType Directory -Path $schedulerPath | Out-Null }
    if (Test-Path -LiteralPath $lockPath -PathType Leaf) {
        if (([DateTime]::UtcNow - [System.IO.File]::GetLastWriteTimeUtc($lockPath)).TotalMinutes -gt 5) { [System.IO.File]::Delete($lockPath) }
    }
    try {
        $lockStream = [System.IO.File]::Open($lockPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
    } catch {
        throw 'Agent registry is busy; retry after the current mutation completes.'
    }
}
try {
    $registry = Read-Registry
    $allAgents = @($registry.agents)
    $activeAgents = @($allAgents | Where-Object { [DateTime]::Parse([string]$_.expiresAtUtc).ToUniversalTime() -gt $now })
    $expiredAgents = @($allAgents | Where-Object { [DateTime]::Parse([string]$_.expiresAtUtc).ToUniversalTime() -le $now })
    $selectedAgent = $null
    $changed = $false
    if ($Action -eq 'register') {
        if ([string]::IsNullOrWhiteSpace($Owner) -or $Owner.Length -gt 200) { throw 'Owner is required and must not exceed 200 characters.' }
        if ($null -ne ($activeAgents | Where-Object owner -eq $Owner | Select-Object -First 1)) { throw "Owner '$Owner' already has an active agent registration." }
        $normalizedCapabilities = @($Capability | ForEach-Object { ([string]$_).Trim().ToLowerInvariant() } | Where-Object { $_ } | Select-Object -Unique | Sort-Object)
        if ($normalizedCapabilities.Count -eq 0) { throw 'At least one Capability is required.' }
        $unknown = @($normalizedCapabilities | Where-Object { $_ -notin @($registryPolicy.allowedCapabilities) })
        if ($unknown.Count -gt 0) { throw "Unknown agent capability: $($unknown -join ', ')." }
        $selectedAgent = [pscustomobject][ordered]@{
            agentId = [guid]::NewGuid().ToString('N')
            owner = $Owner
            capabilities = $normalizedCapabilities
            capacity = $effectiveCapacity
            registeredAtUtc = $now.ToString('o')
            heartbeatAtUtc = $now.ToString('o')
            expiresAtUtc = $now.AddMinutes($effectiveMinutes).ToString('o')
        }
        $activeAgents += $selectedAgent
        $changed = $true
    } elseif ($Action -eq 'heartbeat') {
        if ([string]::IsNullOrWhiteSpace($AgentId)) { throw 'heartbeat requires AgentId.' }
        $selectedAgent = $activeAgents | Where-Object agentId -eq $AgentId | Select-Object -First 1
        if ($null -eq $selectedAgent) { throw 'Active agent registration was not found; register again.' }
        if (-not [string]::IsNullOrWhiteSpace($Owner) -and [string]$selectedAgent.owner -cne $Owner) { throw 'Agent owner does not match.' }
        $selectedAgent.heartbeatAtUtc = $now.ToString('o')
        $selectedAgent.expiresAtUtc = $now.AddMinutes($effectiveMinutes).ToString('o')
        if ($null -ne $Capacity) { $selectedAgent.capacity = $effectiveCapacity }
        $changed = $true
    } elseif ($Action -eq 'quarantine') {
        if ([string]::IsNullOrWhiteSpace($AgentId)) { throw 'quarantine requires AgentId.' }
        if ([string]::IsNullOrWhiteSpace($Reason) -or $Reason.Length -gt 1000) { throw 'quarantine requires Reason with at most 1000 characters.' }
        $selectedAgent = $activeAgents | Where-Object agentId -eq $AgentId | Select-Object -First 1
        if ($null -eq $selectedAgent) { throw 'Active agent registration was not found.' }
        if (-not [string]::IsNullOrWhiteSpace($Owner) -and [string]$selectedAgent.owner -cne $Owner) { throw 'Agent owner does not match.' }
        $selectedAgent | Add-Member -NotePropertyName quarantineUntilUtc -NotePropertyValue $now.AddMinutes($effectiveQuarantineMinutes).ToString('o') -Force
        $selectedAgent | Add-Member -NotePropertyName quarantineReason -NotePropertyValue $Reason -Force
        $changed = $true
    } elseif ($Action -eq 'unquarantine') {
        if ([string]::IsNullOrWhiteSpace($AgentId)) { throw 'unquarantine requires AgentId.' }
        $selectedAgent = $activeAgents | Where-Object agentId -eq $AgentId | Select-Object -First 1
        if ($null -eq $selectedAgent) { throw 'Active agent registration was not found.' }
        if (-not [string]::IsNullOrWhiteSpace($Owner) -and [string]$selectedAgent.owner -cne $Owner) { throw 'Agent owner does not match.' }
        $selectedAgent | Add-Member -NotePropertyName quarantineUntilUtc -NotePropertyValue '' -Force
        $selectedAgent | Add-Member -NotePropertyName quarantineReason -NotePropertyValue '' -Force
        $changed = $true
    } elseif ($Action -eq 'unregister') {
        if ([string]::IsNullOrWhiteSpace($AgentId)) { throw 'unregister requires AgentId.' }
        $selectedAgent = $activeAgents | Where-Object agentId -eq $AgentId | Select-Object -First 1
        if ($null -eq $selectedAgent) { throw 'Active agent registration was not found.' }
        if (-not [string]::IsNullOrWhiteSpace($Owner) -and [string]$selectedAgent.owner -cne $Owner) { throw 'Agent owner does not match.' }
        $activeAgents = @($activeAgents | Where-Object agentId -ne $AgentId)
        $changed = $true
    } elseif ($Action -eq 'prune' -and $expiredAgents.Count -gt 0) {
        $changed = $true
    }
    if ($changed) {
        Write-Registry ([pscustomobject][ordered]@{ schemaVersion = 1; updatedAtUtc = $now.ToString('o'); agents = @($activeAgents) })
    }
    $leases = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskLease.ps1') list -AsOfUtc $now -Format Json | ConvertFrom-Json
    $views = @($activeAgents | ForEach-Object { Get-AgentView $_ @($leases.leases | Where-Object active) } | Sort-Object owner)
    $result = [pscustomobject][ordered]@{
        schemaVersion = 1
        action = $Action
        asOfUtc = $now.ToString('o')
        changed = $changed
        activeCount = @($views | Where-Object active).Count
        registeredCount = $views.Count
        quarantinedCount = @($views | Where-Object quarantined).Count
        expiredCount = $expiredAgents.Count
        totalCapacity = [int]($($views | Where-Object active | ForEach-Object { $_.capacity } | Measure-Object -Sum).Sum)
        availableCapacity = [int]($($views | Where-Object active | ForEach-Object { $_.availableCapacity } | Measure-Object -Sum).Sum)
        agent = $(if ($null -ne $selectedAgent) { Get-AgentView $selectedAgent @($leases.leases | Where-Object active) } else { $null })
        agents = $views
    }
} finally {
    if ($null -ne $lockStream) { $lockStream.Dispose() }
    if ($mutating -and (Test-Path -LiteralPath $lockPath)) { [System.IO.File]::Delete($lockPath) }
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
} else {
    Write-Host "AI agents: action=$Action, active=$($result.activeCount), quarantined=$($result.quarantinedCount), expired=$($result.expiredCount), available=$($result.availableCapacity)/$($result.totalCapacity)"
    foreach ($agent in $result.agents) { Write-Host " - $($agent.owner): $(@($agent.capabilities) -join ', '), capacity=$($agent.availableCapacity)/$($agent.capacity), quarantined=$($agent.quarantined)" }
}
