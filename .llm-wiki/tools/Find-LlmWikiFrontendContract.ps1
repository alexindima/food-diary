[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'components', 'consumers', 'api', 'translations', 'spec-gaps')]
    [string]$View = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [switch]$IncludeDiagnostics,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$diagnostics = $null
$groups = [ordered]@{}
if ($CompiledIndexSource -eq 'Sqlite') {
    $sqlResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
        -Action frontend-contract `
        -FrontendContractView $View `
        -Query $Query `
        -Limit $Limit `
        -SkipRefresh `
        -Format Json | ConvertFrom-Json
    if (-not [bool]$sqlResult.ready) {
        throw "SQLite frontend-contract projection is unavailable ($($sqlResult.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }
    foreach ($property in $sqlResult.groups.PSObject.Properties) { $groups[$property.Name] = @($property.Value) }
    $diagnostics = [ordered]@{
        source = [string]$sqlResult.source
        sqlDurationMs = [double]$sqlResult.durationMs
        scannedRecords = [int]$sqlResult.scannedRecords
        returnedRecords = [int]$sqlResult.returnedRecords
        sourceHash = [string]$sqlResult.sourceHash
    }
} else {
    $index = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/frontend-contract-index.json') -Raw | ConvertFrom-Json
    if ($View -in @('all', 'components')) { $groups.components = @($index.components) }
    if ($View -eq 'spec-gaps') { $groups.specGaps = @($index.components | Where-Object { $null -eq $_.specPath }) }
    if ($View -in @('all', 'consumers')) { $groups.consumers = @($index.consumerEdges) }
    if ($View -in @('all', 'api')) { $groups.apiCalls = @($index.apiCalls) }
    if ($View -in @('all', 'translations')) { $groups.translations = @($index.translationUsage) }
    foreach ($key in @($groups.Keys)) {
        if (-not [string]::IsNullOrWhiteSpace($Query)) {
            $groups[$key] = @($groups[$key] | Where-Object { ($_ | ConvertTo-Json -Depth 6 -Compress) -match [regex]::Escape($Query) })
        }
        $groups[$key] = @($groups[$key] | Select-Object -First $Limit)
    }
    $diagnostics = [ordered]@{
        source = 'json-baseline'
        sqlDurationMs = $null
        scannedRecords = @($index.components).Count + @($index.consumerEdges).Count + @($index.apiCalls).Count + @($index.translationUsage).Count
        returnedRecords = [int](($groups.Values | ForEach-Object { @($_).Count } | Measure-Object -Sum).Sum)
        sourceHash = $null
    }
}
$stopwatch.Stop()
$diagnostics['roundTripDurationMs'] = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
if ($Format -eq 'Json') {
    if ($IncludeDiagnostics) { $groups['_diagnostics'] = $diagnostics }
    [pscustomobject]$groups | ConvertTo-Json -Depth 9
    exit 0
}
Write-Host "Frontend contract source: $($diagnostics.source), returned=$($diagnostics.returnedRecords)/$($diagnostics.scannedRecords), round-trip=$($diagnostics.roundTripDurationMs)ms."
foreach ($key in $groups.Keys) {
    Write-Host "$key ($(@($groups[$key]).Count)):"
    foreach ($item in $groups[$key]) { Write-Host " - $(($item | ConvertTo-Json -Depth 6 -Compress))" }
    Write-Host ''
}
