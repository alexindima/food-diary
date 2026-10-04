[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 30)]
    [int]$Limit = 10,
    [switch]$Compact,
    [string]$IndexRoot,
    [ValidateSet('Sqlite')]
    [string]$CompiledIndexSource = 'Sqlite'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($IndexRoot)) { $IndexRoot = Join-Path $wikiRoot 'generated' }
if ($PSBoundParameters.ContainsKey('IndexRoot')) {
    throw 'Custom JSON query indexes are unsupported. Build a repository SQLite projection for query fixtures.'
}
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
. (Join-Path $PSScriptRoot 'Ensure-LlmWikiSqliteProjection.ps1')
$sqlResult = Invoke-LlmWikiSqliteQuery -Arguments @{ Action = 'frontend-trace'; Query = $Query; Limit = $Limit }
$stopwatch.Stop()
if (-not [bool]$sqlResult.ready) {
    throw "SQLite frontend trace projection is unavailable ($($sqlResult.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
}
$result = $sqlResult.trace
$result | Add-Member -NotePropertyName compiledIndex -NotePropertyValue ([pscustomobject][ordered]@{
    source = [string]$sqlResult.source
    sqlDurationMs = [double]$sqlResult.durationMs
    roundTripDurationMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
    scannedRecords = [int]$sqlResult.scannedRecords
    candidateRecords = [int]$sqlResult.candidateRecords
    returnedRecords = [int]$sqlResult.returnedRecords
    sourceHashes = $sqlResult.sourceHashes
}) -Force
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
    exit 0
}
if (-not [bool]$result.matched) {
    Write-Host "No frontend symbols matched '$Query'."
    exit 1
}
$displayTraces = if ($Compact) { @($result.traces | Select-Object -First 1) } else { @($result.traces) }
foreach ($trace in $displayTraces) {
    Write-Host "$($trace.symbol.name) [$($trace.symbol.role)]"
    Write-Host "  Source: $($trace.symbol.path):$($trace.symbol.line)"
    foreach ($route in @($trace.routes | Select-Object -First $(if ($Compact) { 3 } else { 1000 }))) { Write-Host "  Route: /$($route.path) ($($route.source):$($route.line))" }
    foreach ($consumer in @($trace.upstreamConsumers | Select-Object -First $(if ($Compact) { 5 } else { 1000 }))) { Write-Host "  Upstream: $($consumer.name) ($($consumer.path):$($consumer.line))" }
    foreach ($consumer in @($trace.selectorConsumers | Select-Object -First $(if ($Compact) { 5 } else { 1000 }))) { Write-Host "  Template consumer: $($consumer.consumerPath)" }
    foreach ($apiCall in @($trace.apiCalls | Select-Object -First $(if ($Compact) { 3 } else { 1000 }))) { Write-Host "  API: $($apiCall.method) $($apiCall.resolvedUrlExpression) ($($apiCall.path):$($apiCall.line))" }
    foreach ($test in @($trace.tests | Select-Object -First $(if ($Compact) { 5 } else { 1000 }))) { Write-Host "  Test: $test" }
    if ($Compact) { Write-Host '  Compact trace: use -FullTrace for every match and consumer.' }
    Write-Host ''
}
exit 0
