[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'types', 'invariants', 'mappings', 'indexes', 'relationships')]
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
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$groups = [ordered]@{}
$diagnostics = $null
. (Join-Path $PSScriptRoot 'LlmWikiInProcessSqlite.ps1')
$reader = Initialize-LlmWikiInProcessSqlite -Projection domain
$resultJson = [LlmWiki.SqliteReader.DomainDataReader]::Query(
    $repositoryRoot,
    $View,
    $Query,
    $Limit,
    [bool]$IncludeDiagnostics,
    [double]$reader.loadDurationMs)
if ($Format -eq 'Json') {
    $resultJson
    exit 0
}
$result = $resultJson | ConvertFrom-Json
foreach ($property in $result.PSObject.Properties) {
    if ($property.Name -eq '_diagnostics') {
        $diagnostics = $property.Value
    } else {
        $groups[$property.Name] = @($property.Value)
    }
}
$stopwatch.Stop()
if ($Format -eq 'Json') {
    if ($IncludeDiagnostics) {
        $diagnostics | Add-Member -NotePropertyName completeCommandDurationMs -NotePropertyValue ([Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2))
        $groups['_diagnostics'] = $diagnostics
    }
    [pscustomobject]$groups | ConvertTo-Json -Depth 10
    exit 0
}
if ($IncludeDiagnostics -and $null -ne $diagnostics) {
    Write-Host "Source: $($diagnostics.source), reader=$($diagnostics.reader), returned=$($diagnostics.returnedRecords)/$($diagnostics.candidateRecords), load=$($diagnostics.readerLoadDurationMs)ms, query=$($diagnostics.sqlDurationMs)ms."
}
foreach ($key in $groups.Keys) {
    Write-Host "$key ($(@($groups[$key]).Count)):"
    foreach ($item in $groups[$key]) { Write-Host " - $(($item | ConvertTo-Json -Depth 7 -Compress))" }
    Write-Host ''
}
