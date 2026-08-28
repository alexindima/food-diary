[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'contracts', 'consumers', 'production', 'tests', 'ambiguous', 'unconsumed')]
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
    . (Join-Path $PSScriptRoot 'Ensure-LlmWikiSqliteProjection.ps1')
    Ensure-LlmWikiSqliteProjection -Category contracts
    $sqlResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
        -Action backend-contract `
        -BackendContractView $View `
        -Query $Query `
        -Limit $Limit `
        -SkipRefresh `
        -Format Json | ConvertFrom-Json
    if (-not [bool]$sqlResult.ready) {
        throw "SQLite backend-contract projection is unavailable ($($sqlResult.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
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
    $index = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/backend-contract-index.json') -Raw | ConvertFrom-Json
    $consumedNames = @($index.consumerEdges.contract | Sort-Object -Unique)
    if ($View -in @('all', 'contracts')) { $groups.contracts = @($index.contracts) }
    if ($View -in @('all', 'consumers')) { $groups.consumers = @($index.consumerEdges) }
    if ($View -eq 'production') { $groups.productionConsumers = @($index.consumerEdges | Where-Object { -not $_.isTest }) }
    if ($View -eq 'tests') { $groups.testConsumers = @($index.consumerEdges | Where-Object isTest) }
    if ($View -eq 'ambiguous') { $groups.ambiguousContracts = @($index.contracts | Where-Object ambiguous) }
    if ($View -eq 'unconsumed') { $groups.unconsumedContracts = @($index.contracts | Where-Object { $_.name -notin $consumedNames }) }
    foreach ($key in @($groups.Keys)) {
        if (-not [string]::IsNullOrWhiteSpace($Query)) {
            $groups[$key] = @($groups[$key] | Where-Object { ($_ | ConvertTo-Json -Depth 7 -Compress) -match [regex]::Escape($Query) })
        }
        $groups[$key] = @($groups[$key] | Select-Object -First $Limit)
    }
    $diagnostics = [ordered]@{
        source = 'json-baseline'
        sqlDurationMs = $null
        scannedRecords = @($index.contracts).Count + @($index.consumerEdges).Count
        returnedRecords = [int](($groups.Values | ForEach-Object { @($_).Count } | Measure-Object -Sum).Sum)
        sourceHash = $null
    }
}
$stopwatch.Stop()
$diagnostics['roundTripDurationMs'] = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
if ($Format -eq 'Json') {
    if ($IncludeDiagnostics) { $groups['_diagnostics'] = $diagnostics }
    [pscustomobject]$groups | ConvertTo-Json -Depth 10
    exit 0
}
Write-Host "Backend contract source: $($diagnostics.source), returned=$($diagnostics.returnedRecords)/$($diagnostics.scannedRecords), round-trip=$($diagnostics.roundTripDurationMs)ms."
foreach ($key in $groups.Keys) {
    Write-Host "$key ($(@($groups[$key]).Count)):"
    foreach ($item in $groups[$key]) { Write-Host " - $(($item | ConvertTo-Json -Depth 7 -Compress))" }
    Write-Host ''
}
