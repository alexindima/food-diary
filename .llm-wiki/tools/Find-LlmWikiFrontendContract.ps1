[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'components', 'consumers', 'api', 'translations', 'spec-gaps')]
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
$sqlResult = Invoke-LlmWikiSqliteQuery -Arguments @{ Action = 'frontend-contract'; FrontendContractView = $View; Query = $Query; Limit = $Limit }
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
