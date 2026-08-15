[CmdletBinding()]
param(
    [string]$SessionId,
    [switch]$Create,
    [switch]$ReadOnly,
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [ValidateSet('Object', 'Json', 'Text')]
    [string]$Format = 'Object'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path $RepositoryRoot).Path
$gitDirectory = (& git -C $repositoryRoot rev-parse --absolute-git-dir).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the Git directory for the LLM Wiki session.' }
$stateDirectory = Join-Path $gitDirectory 'llm-wiki/sessions'
$registryPath = Join-Path $stateDirectory 'registry.json'

function Write-Registry([object]$Value) {
    $null = New-Item -ItemType Directory -Path $stateDirectory -Force
    [IO.File]::WriteAllText(
        $registryPath,
        (($Value | ConvertTo-Json -Depth 8) + [Environment]::NewLine),
        [Text.UTF8Encoding]::new($false)
    )
}

function Get-ExternalHint {
    if (-not [string]::IsNullOrWhiteSpace($SessionId)) { return $SessionId.Trim() }
    foreach ($name in @('CODEX_THREAD_ID', 'CODEX_TASK_ID', 'CODEX_SESSION_ID')) {
        $value = [Environment]::GetEnvironmentVariable($name)
        if (-not [string]::IsNullOrWhiteSpace($value)) { return "$name`:$($value.Trim())" }
    }
    return $null
}

$registry = try {
    if (Test-Path -LiteralPath $registryPath -PathType Leaf) {
        Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
    } else {
        [pscustomobject]@{ schemaVersion = 1; sessions = @() }
    }
} catch {
    if (-not $ReadOnly) { throw }
    Write-Warning 'Session registry is not readable; continuing in ephemeral read-only mode.'
    [pscustomobject]@{ schemaVersion = 1; sessions = @() }
}
$sessions = @($registry.sessions)
$externalHint = Get-ExternalHint
$resolved = $null

if ($externalHint) {
    $resolved = $sessions | Where-Object { [string]$_.externalHint -ceq $externalHint -and [string]$_.state -eq 'active' } | Select-Object -First 1
} else {
    $active = @($sessions | Where-Object { [string]$_.state -eq 'active' })
    if ($active.Count -eq 1) { $resolved = $active[0] }
    elseif ($active.Count -gt 1) {
        $choices = ($active | ForEach-Object { "$($_.id) ($($_.workspacePath))" }) -join ', '
        throw "Multiple active LLM Wiki sessions exist and no stable external hint is available: $choices. Pass -SessionId or an explicit -WorkspacePath."
    }
}

if ($null -eq $resolved -and $Create -and -not $ReadOnly) {
    $id = [guid]::NewGuid().ToString('N')
    $resolved = [pscustomobject][ordered]@{
        id = $id
        externalHint = $externalHint
        state = 'active'
        createdAtUtc = [DateTime]::UtcNow.ToString('o')
        lastSeenAtUtc = [DateTime]::UtcNow.ToString('o')
        workspacePath = ".artifacts/llm-wiki/tasks/session-$id"
    }
    $sessions += $resolved
} elseif ($null -ne $resolved -and -not $ReadOnly) {
    $resolved.lastSeenAtUtc = [DateTime]::UtcNow.ToString('o')
}

if ($null -ne $resolved -and -not $ReadOnly) {
    $registry = [pscustomobject][ordered]@{ schemaVersion = 1; sessions = @($sessions) }
    Write-Registry $registry
}

$result = if ($null -eq $resolved) {
    [pscustomobject]@{ available = $false; readOnly = [bool]$ReadOnly; id = 'default'; externalHint = $externalHint; workspacePath = '.artifacts/llm-wiki/tasks/current' }
} else {
    [pscustomobject]@{ available = $true; readOnly = [bool]$ReadOnly; id = [string]$resolved.id; externalHint = $externalHint; workspacePath = [string]$resolved.workspacePath }
}

switch ($Format) {
    'Json' { $result | ConvertTo-Json -Depth 4 }
    'Text' { Write-Host "LLM Wiki session: $($result.id) ($($result.workspacePath))" }
    default { return $result }
}
