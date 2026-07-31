[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('pause', 'resume')]
    [string]$Action = 'resume',
    [string]$WorkspacePath = '.artifacts/llm-wiki/tasks/current',
    [object]$StatusInput,
    [object]$DoctorInput,
    [string[]]$HandoffMarkdown,
    [ValidateRange(1, 100)]
    [int]$Limit = 20,
    [switch]$Overwrite,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
if ([IO.Path]::IsPathRooted($WorkspacePath)) { throw 'WorkspacePath must be repository-relative.' }
$normalizedWorkspace = $WorkspacePath.Replace('\', '/').TrimEnd('/')
if ($normalizedWorkspace -notmatch '^\.artifacts/llm-wiki/tasks/[^/.][^/]*$') { throw 'WorkspacePath must identify one task workspace.' }
$absoluteWorkspace = Join-Path $repositoryRoot $normalizedWorkspace
if (-not (Test-Path -LiteralPath (Join-Path $absoluteWorkspace 'workspace.json') -PathType Leaf)) { throw "Task workspace does not exist: $normalizedWorkspace" }
$sessionPath = Join-Path $absoluteWorkspace 'adaptive-session.json'
$handoffPath = Join-Path $absoluteWorkspace 'adaptive-handoff.md'

function Write-Json([string]$Path, [object]$Value) {
    $json = $Value | ConvertTo-Json -Depth 20
    [IO.File]::WriteAllText($Path, ($json + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}

if ($Action -eq 'pause') {
    if ((Test-Path -LiteralPath $sessionPath -PathType Leaf) -and -not $Overwrite) {
        throw "A paused session already exists. Resume it or pass -Overwrite: $normalizedWorkspace"
    }
    $status = if ($null -ne $StatusInput) { $StatusInput } else {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') status -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
    }
    $descriptor = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'workspace.json') -Raw | ConvertFrom-Json
    $head = (& git -C $repositoryRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve Git HEAD while pausing the task.' }
    if ($null -ne $HandoffMarkdown -and $HandoffMarkdown.Count -gt 0) {
        [IO.File]::WriteAllText($handoffPath, (($HandoffMarkdown -join [Environment]::NewLine) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    } else {
        & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskHandoff.ps1') `
            -WorkspacePath $normalizedWorkspace `
            -StatusInput $status `
            -Limit $Limit `
            -OutputPath $handoffPath | Out-Null
    }
    $receipt = [pscustomobject][ordered]@{
        schemaVersion = 1
        state = 'paused'
        workspace = $normalizedWorkspace
        objective = $descriptor.objective
        pausedAtUtc = [DateTime]::UtcNow.ToString('o')
        pausedHead = $head
        packetFingerprint = $status.currentPacketFingerprint
        refreshRequired = [bool]$status.refreshRequired
        blockingReasons = @($status.blockingReasons)
        nextActions = @($status.nextActions)
        handoffPath = "$normalizedWorkspace/adaptive-handoff.md"
    }
    Write-Json $sessionPath $receipt
    $result = [pscustomobject][ordered]@{
        action = 'pause'
        valid = $true
        workspace = $normalizedWorkspace
        session = $receipt
        resumeCommand = "./.llm-wiki/wiki.ps1 resume -WorkspacePath $normalizedWorkspace"
    }
} else {
    if (-not (Test-Path -LiteralPath $sessionPath -PathType Leaf)) { throw "No paused adaptive session exists: $normalizedWorkspace" }
    $receipt = Get-Content -LiteralPath $sessionPath -Raw | ConvertFrom-Json
    if ([string]$receipt.state -ne 'paused') { throw 'Adaptive session receipt is not paused.' }
    $doctor = if ($null -ne $DoctorInput) { $DoctorInput } else {
        & (Join-Path $PSScriptRoot 'Test-LlmWikiTaskWorkspace.ps1') -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
    }
    $status = if ($null -ne $StatusInput) { $StatusInput } else {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') status -WorkspacePath $normalizedWorkspace -Format Json | ConvertFrom-Json
    }
    $currentHead = (& git -C $repositoryRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve Git HEAD while resuming the task.' }
    $repositoryDriftPaths = @()
    if ($currentHead -ne [string]$receipt.pausedHead) {
        & git -C $repositoryRoot cat-file -e "$($receipt.pausedHead)^{commit}" 2>$null
        if ($LASTEXITCODE -eq 0) {
            $repositoryDriftPaths = @(& git -C $repositoryRoot diff --name-only "$($receipt.pausedHead)..$currentHead" | Where-Object { $_ } | Select-Object -First $Limit)
        }
    }
    $packetDrift = [string]$receipt.packetFingerprint -ne [string]$status.currentPacketFingerprint
    $canContinue = [bool]$doctor.valid -and -not $packetDrift -and -not [bool]$status.refreshRequired
    $recommended = if (-not [bool]$doctor.valid) {
        "Repair workspace integrity before continuing: ./.llm-wiki/wiki.ps1 task-doctor -WorkspacePath $normalizedWorkspace -FailOnInvalid"
    } elseif ($packetDrift -or [bool]$status.refreshRequired) {
        "Refresh and review derived context before editing: ./.llm-wiki/wiki.ps1 task-refresh -WorkspacePath $normalizedWorkspace"
    } elseif (@($status.nextActions).Count -gt 0) {
        [string]$status.nextActions[0]
    } else {
        'Read the handoff and continue the first incomplete implementation or verification step.'
    }
    $result = [pscustomobject][ordered]@{
        action = 'resume'
        valid = [bool]$doctor.valid
        canContinueWithoutRefresh = $canContinue
        workspace = $normalizedWorkspace
        objective = $receipt.objective
        continuity = [pscustomobject][ordered]@{
            pausedHead = $receipt.pausedHead
            currentHead = $currentHead
            headChanged = $currentHead -ne [string]$receipt.pausedHead
            repositoryDriftPaths = $repositoryDriftPaths
            pausedPacketFingerprint = $receipt.packetFingerprint
            currentPacketFingerprint = $status.currentPacketFingerprint
            packetDrift = $packetDrift
            refreshRequired = [bool]$status.refreshRequired
        }
        handoffPath = $receipt.handoffPath
        blockingReasons = @($status.blockingReasons)
        nextActions = @($status.nextActions)
        recommendedAction = $recommended
    }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; exit 0 }
if ($Action -eq 'pause') {
    Write-Host "Adaptive task paused: $normalizedWorkspace"
    Write-Host "Handoff: $($result.session.handoffPath)"
    Write-Host "Resume: $($result.resumeCommand)"
} else {
    Write-Host "Adaptive task resume audit: $normalizedWorkspace"
    Write-Host "Workspace valid: $($result.valid)"
    Write-Host "Continue without refresh: $($result.canContinueWithoutRefresh)"
    Write-Host "HEAD changed: $($result.continuity.headChanged)"
    Write-Host "Packet drift: $($result.continuity.packetDrift)"
    foreach ($path in $result.continuity.repositoryDriftPaths) { Write-Host "  Drift path: $path" }
    Write-Host "Next: $($result.recommendedAction)"
}
