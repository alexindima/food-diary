[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('init', 'show', 'validate')]
    [string]$Action = 'show',
    [string]$Path = '.artifacts/llm-wiki/task-contract.json',
    [string]$Objective,
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath,
    [string[]]$AllowedPath = @(),
    [string[]]$ExcludedPath = @(),
    [switch]$FailOnOutOfScope
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$absolutePath = if ([System.IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path $repositoryRoot $Path }

function Write-Contract {
    param($Contract)
    $directory = Split-Path -Parent $absolutePath
    if (-not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText(
        $absolutePath,
        (($Contract | ConvertTo-Json -Depth 7) + [Environment]::NewLine),
        $utf8WithoutBom)
}

function Read-Contract {
    if (-not (Test-Path -LiteralPath $absolutePath)) {
        throw "Task contract does not exist: $Path"
    }
    return Get-Content -LiteralPath $absolutePath -Raw | ConvertFrom-Json
}

function Test-PathMatch {
    param([string]$Value, [string[]]$Patterns)
    $Value = $Value.Replace('\', '/')
    foreach ($pattern in @($Patterns)) {
        if ([string]::IsNullOrWhiteSpace($pattern)) { continue }
        if ($Value -match $pattern) { return $true }
    }
    return $false
}

switch ($Action) {
    'init' {
        if ([string]::IsNullOrWhiteSpace($Objective)) { throw 'init requires -Objective.' }
        if ($AllowedPath.Count -eq 0) { throw 'init requires at least one -AllowedPath regex.' }
        foreach ($pattern in @($AllowedPath + $ExcludedPath)) {
            try { $null = [regex]::new($pattern) } catch { throw "Invalid path regex: $pattern" }
        }
        $head = git rev-parse HEAD
        if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD.' }
        $resolvedBase = git rev-parse --verify "$BaseRef^{commit}"
        if ($LASTEXITCODE -ne 0) { throw "Unable to resolve BaseRef '$BaseRef'." }
        $contract = [ordered]@{
            schemaVersion = 1
            objective = $Objective
            git = [ordered]@{ base = ([string]$resolvedBase).Trim(); requestedBase = $BaseRef; headAtStart = ([string]$head).Trim() }
            scope = [ordered]@{
                allowedPathPatterns = @($AllowedPath)
                excludedPathPatterns = @($ExcludedPath)
            }
        }
        Write-Contract $contract
        Write-Host "Initialized task contract: $Path"
    }
    'validate' {
        $contract = Read-Contract
        $diffArguments = @{ BaseRef = $contract.git.base; Format = 'Json' }
        if ($PSBoundParameters.ContainsKey('ChangedPath')) { $diffArguments.ChangedPath = $ChangedPath }
        $diff = & (Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1') @diffArguments | ConvertFrom-Json
        $outOfScope = @(
            $diff.changedPaths | Where-Object {
                -not (Test-PathMatch -Value $_ -Patterns @($contract.scope.allowedPathPatterns)) -or
                (Test-PathMatch -Value $_ -Patterns @($contract.scope.excludedPathPatterns))
            }
        )
        Write-Host "Task contract: $(@($diff.changedPaths).Count) changed path(s), $($outOfScope.Count) out of scope."
        foreach ($changedPath in $outOfScope) { Write-Host " - $changedPath" }
        if ($FailOnOutOfScope -and $outOfScope.Count -gt 0) { exit 1 }
    }
    default {
        $contract = Read-Contract
        Write-Host "Objective: $($contract.objective)"
        Write-Host "Base: $($contract.git.base)"
        Write-Host "Allowed: $(@($contract.scope.allowedPathPatterns) -join ', ')"
        Write-Host "Excluded: $(@($contract.scope.excludedPathPatterns) -join ', ')"
    }
}
