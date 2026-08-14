[CmdletBinding()]
param(
    [ValidateSet('build', 'status', 'symbol', 'consumers', 'trace', 'impact', 'relations', 'coverage', 'fingerprint', 'query')]
    [string]$Action = 'status',
    [string]$Query,
    [ValidateSet('modules', 'contracts', 'risks', 'tests')]
    [string]$Category = 'modules',
    [string[]]$ChangedPath,
    [string[]]$RelationKind,
    [string]$Module,
    [string]$PathPrefix,
    [ValidateSet('Any', 'HostedService', 'Service', 'Handler', 'Controller', 'Repository', 'Component')]
    [string]$SymbolKind = 'Any',
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
if ($Action -eq 'query') { $arguments += "--category=$Category" }
if (-not [string]::IsNullOrWhiteSpace($Module)) { $arguments += "--module=$Module" }
if (-not [string]::IsNullOrWhiteSpace($PathPrefix)) { $arguments += "--path-prefix=$PathPrefix" }
if ($SymbolKind -ne 'Any') { $arguments += "--symbol-kind=$SymbolKind" }
$normalizedChangedPaths = [string[]]@($ChangedPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
if ($normalizedChangedPaths.Length -gt 0) { $arguments += "--path=$($normalizedChangedPaths -join ';')" }
$normalizedRelationKinds = [string[]]@($RelationKind | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
if ($normalizedRelationKinds.Length -gt 0) { $arguments += "--kind=$($normalizedRelationKinds -join ';')" }
if ($Force) { $arguments += '--force=true' }
$json = & node @arguments
if ($LASTEXITCODE -ne 0) { throw "Code graph action '$Action' failed with exit code $LASTEXITCODE." }
$result = $json | ConvertFrom-Json
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; return }

switch ($Action) {
    'build' { Write-Host "Code graph: $($result.files) files, $($result.symbols) symbols; scanned=$($result.scanned), updated=$($result.updated), unchanged=$($result.unchanged), removed=$($result.removed), $($result.durationMs)ms." }
    'status' { Write-Host "Code graph: $($result.files) files, $($result.symbols) symbols, $($result.tokens) file-token edges, $($result.typedEdges) typed edges; parser=$($result.parserVersion)." }
    'symbol' { foreach ($item in @($result.symbols)) { Write-Host "$($item.name) [$($item.kind)] $($item.path):$($item.line)" } }
    'consumers' {
        Write-Host "Code graph consumers for '$Query': $(@($result.consumers).Count)"
        foreach ($item in @($result.consumers)) { Write-Host " - $($item.path) [$($item.symbol)]" }
    }
    'trace' {
        Write-Host "Code graph trace for '$Query': $(@($result.symbols).Count) symbol(s), $(@($result.consumers).Count) consumer(s)."
        foreach ($item in @($result.candidates)) { Write-Host " - candidate [$($item.confidence), score=$($item.score)]: $($item.path):$($item.line) [$($item.name)] because $(@($item.reasons) -join '; ')" }
        foreach ($item in @($result.symbols)) { Write-Host " - symbol: $($item.path):$($item.line) [$($item.name)]" }
        foreach ($item in @($result.consumers)) {
            $relation = if ($item.PSObject.Properties['relationKind'] -and -not [string]::IsNullOrWhiteSpace([string]$item.relationKind)) { [string]$item.relationKind } else { [string]$item.source }
            Write-Host " - consumer: $($item.path) [$relation]"
        }
        foreach ($item in @($result.namespaceFilters)) {
            $status = if ([int]$item.matchedDeclarations -eq 0) { 'EMPTY' } else { "$($item.matchedDeclarations) declaration(s)" }
            Write-Host " - namespace filter: $($item.path):$($item.line) -> $($item.namespace) [$status]"
        }
    }
    'impact' {
        Write-Host "Code graph impact: $(@($result.declaredSymbols).Count) declaration(s), $(@($result.consumers).Count) consumer(s), $(@($result.references).Count) referenced declaration(s)."
        foreach ($item in @($result.consumers)) { Write-Host " - downstream: $($item.path) [$($item.symbol)]" }
        foreach ($item in @($result.references)) { Write-Host " - dependency: $($item.declarationPath) [$($item.symbol)]" }
    }
    'relations' {
        Write-Host "Code graph relations: $(@($result.relations).Count) edge(s)."
        foreach ($item in @($result.relations)) { Write-Host " - $($item.kind): $($item.path):$($item.line) -> $($item.target) [$($item.confidence)]" }
    }
    'coverage' {
        Write-Host "Code graph coverage: $($result.files) files, $($result.symbols) symbols, $($result.typedEdges) typed edges; parser=$($result.parserVersion)."
        foreach ($item in @($result.relationKinds)) { Write-Host " - $($item.kind): $($item.count) edge(s) across $($item.files) file(s)" }
        foreach ($item in @($result.legacySymbolCoverage)) { Write-Host " - shadow $($item.index): $($item.covered)/$($item.total) covered, missing=$($item.missing)" }
    }
    'fingerprint' { Write-Host "Code graph fingerprint: $($result.fingerprint) ($($result.fileCount) file(s))." }
    'query' {
        Write-Host "Code graph query [$Category] '$Query': $(@($result.records).Count) record(s)."
        foreach ($item in @($result.records)) { Write-Host " - $($item.path) [$($item.recordKey)]" }
    }
}
