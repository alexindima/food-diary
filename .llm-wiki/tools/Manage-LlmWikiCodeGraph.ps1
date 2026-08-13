[CmdletBinding()]
param(
    [ValidateSet('build', 'status', 'symbol', 'consumers', 'trace', 'impact')]
    [string]$Action = 'status',
    [string]$Query,
    [string[]]$ChangedPath,
    [ValidateRange(1, 500)]
    [int]$Limit = 50,
    [switch]$Force,
    [switch]$SkipRefresh,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'code-graph.mjs'
if ($Action -notin @('build', 'status') -and -not $SkipRefresh) {
    $refreshOutput = & node $scriptPath build
    if ($LASTEXITCODE -ne 0) { throw "Code graph incremental refresh failed with exit code $LASTEXITCODE." }
}
$arguments = @($scriptPath, $Action, "--limit=$Limit")
if (-not [string]::IsNullOrWhiteSpace($Query)) { $arguments += "--query=$Query" }
$normalizedChangedPaths = [string[]]@($ChangedPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
if ($normalizedChangedPaths.Length -gt 0) { $arguments += "--path=$($normalizedChangedPaths -join ';')" }
if ($Force) { $arguments += '--force=true' }
$json = & node @arguments
if ($LASTEXITCODE -ne 0) { throw "Code graph action '$Action' failed with exit code $LASTEXITCODE." }
$result = $json | ConvertFrom-Json
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; return }

switch ($Action) {
    'build' { Write-Host "Code graph: $($result.files) files, $($result.symbols) symbols; scanned=$($result.scanned), updated=$($result.updated), unchanged=$($result.unchanged), removed=$($result.removed), $($result.durationMs)ms." }
    'status' { Write-Host "Code graph: $($result.files) files, $($result.symbols) symbols, $($result.tokens) file-token edges; parser=$($result.parserVersion)." }
    'symbol' { foreach ($item in @($result.symbols)) { Write-Host "$($item.name) [$($item.kind)] $($item.path):$($item.line)" } }
    'consumers' {
        Write-Host "Code graph consumers for '$Query': $(@($result.consumers).Count)"
        foreach ($item in @($result.consumers)) { Write-Host " - $($item.path) [$($item.symbol)]" }
    }
    'trace' {
        Write-Host "Code graph trace for '$Query': $(@($result.symbols).Count) symbol(s), $(@($result.consumers).Count) consumer(s)."
        foreach ($item in @($result.symbols)) { Write-Host " - symbol: $($item.path):$($item.line) [$($item.name)]" }
        foreach ($item in @($result.consumers)) { Write-Host " - consumer: $($item.path)" }
    }
    'impact' {
        Write-Host "Code graph impact: $(@($result.declaredSymbols).Count) declaration(s), $(@($result.consumers).Count) consumer(s), $(@($result.references).Count) referenced declaration(s)."
        foreach ($item in @($result.consumers)) { Write-Host " - downstream: $($item.path) [$($item.symbol)]" }
        foreach ($item in @($result.references)) { Write-Host " - dependency: $($item.declarationPath) [$($item.symbol)]" }
    }
}
