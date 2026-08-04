[CmdletBinding()]
param(
    [ValidateSet('Capture', 'ChangedPaths', 'Status')]
    [string]$Action,
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [ValidateSet('Object', 'Text', 'Json')]
    [string]$Format = 'Object'
)

$ErrorActionPreference = 'Stop'

function Invoke-Git([string[]]$Arguments) {
    $output = @(& git -C $RepositoryRoot @Arguments)
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed." }
    return $output
}

function ConvertTo-RepositoryPath([string]$Path) {
    return $Path.Trim().Replace('\', '/')
}

function Get-ChangedPaths([string]$BaseRef) {
    $paths = @(
        Invoke-Git @('diff', '--name-only', '--diff-filter=ACMRD', $BaseRef, '--')
        Invoke-Git @('ls-files', '--others', '--exclude-standard')
    )
    return @($paths | Where-Object { $_ } | ForEach-Object { ConvertTo-RepositoryPath $_ } | Sort-Object -Unique)
}

function Get-PathFingerprint([string]$RepositoryPath) {
    $absolutePath = Join-Path $RepositoryRoot $RepositoryPath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { return '<missing>' }
    $stream = [System.IO.File]::OpenRead($absolutePath)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha256.ComputeHash($stream)) -replace '-', '').ToLowerInvariant()
    } finally {
        $sha256.Dispose()
        $stream.Dispose()
    }
}

$gitDirectory = (Invoke-Git @('rev-parse', '--absolute-git-dir') | Select-Object -First 1)
$stateDirectory = Join-Path $gitDirectory 'llm-wiki'
$baselinePath = Join-Path $stateDirectory 'task-baseline.json'

if ($Action -eq 'Capture') {
    $head = (Invoke-Git @('rev-parse', 'HEAD') | Select-Object -First 1)
    $initialPaths = @(Get-ChangedPaths 'HEAD')
    $fingerprints = [ordered]@{}
    foreach ($path in $initialPaths) { $fingerprints[$path] = Get-PathFingerprint $path }
    $baseline = [ordered]@{
        schemaVersion = 1
        capturedAtUtc = [DateTime]::UtcNow.ToString('o')
        head = $head
        initialChangedPaths = $initialPaths
        fingerprints = $fingerprints
    }
    $null = New-Item -ItemType Directory -Path $stateDirectory -Force
    $baseline | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $baselinePath -Encoding utf8
    $result = [pscustomobject]@{ available = $true; baselinePath = $baselinePath; head = $head; initialChangedPaths = $initialPaths; changedPaths = @() }
} elseif (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) {
    $result = [pscustomobject]@{ available = $false; baselinePath = $baselinePath; head = $null; initialChangedPaths = @(); changedPaths = @() }
} else {
    $baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
    $currentPaths = @(Get-ChangedPaths ([string]$baseline.head))
    $delta = [System.Collections.Generic.List[string]]::new()
    foreach ($path in $currentPaths) {
        $baselineFingerprint = $baseline.fingerprints.PSObject.Properties[$path]
        if ($null -eq $baselineFingerprint -or [string]$baselineFingerprint.Value -cne (Get-PathFingerprint $path)) {
            $delta.Add($path)
        }
    }
    $result = [pscustomobject]@{
        available = $true
        baselinePath = $baselinePath
        head = [string]$baseline.head
        initialChangedPaths = @($baseline.initialChangedPaths)
        changedPaths = @($delta)
    }
}

switch ($Format) {
    'Json' { $result | ConvertTo-Json -Depth 5 }
    'Text' {
        if (-not $result.available) { Write-Host 'LLM Wiki task baseline: not captured.' }
        elseif ($Action -eq 'Capture') { Write-Host "LLM Wiki task baseline captured: $(@($result.initialChangedPaths).Count) pre-existing changed path(s)." }
        else { Write-Host "LLM Wiki task delta: $(@($result.changedPaths).Count) path(s) changed since develop." }
    }
    default { return $result }
}
