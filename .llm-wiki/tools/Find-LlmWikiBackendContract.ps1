[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'contracts', 'consumers', 'production', 'tests', 'ambiguous', 'unconsumed')]
    [string]$View = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Sqlite')]
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
. (Join-Path $PSScriptRoot 'Ensure-LlmWikiSqliteProjection.ps1')
$sqlResult = Invoke-LlmWikiSqliteQuery -Arguments @{ Action = 'backend-contract'; BackendContractView = $View; Query = $Query; Limit = $Limit }
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
