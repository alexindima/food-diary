[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'types', 'invariants', 'mappings', 'indexes', 'relationships')]
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
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$groups = [ordered]@{}
$diagnostics = $null
if ($CompiledIndexSource -eq 'Sqlite') {
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
} else {
    $indexRaw = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/domain-data-index.json') -Raw
    $index = $indexRaw | ConvertFrom-Json
    if ($View -in @('all', 'types')) { $groups.types = @($index.domainTypes) }
    if ($View -in @('all', 'invariants')) { $groups.invariants = @($index.invariants) }
    if ($View -in @('all', 'mappings')) { $groups.mappings = @($index.persistenceMappings) }
    if ($View -eq 'indexes') {
        $groups.indexes = @($index.persistenceMappings | Where-Object { @($_.indexes).Count -gt 0 })
    }
    if ($View -eq 'relationships') {
        $groups.relationships = @($index.persistenceMappings | Where-Object { @($_.relationships).Count -gt 0 })
    }
    $candidateRecords = 0
    foreach ($key in @($groups.Keys)) {
        $candidateRecords += @($groups[$key]).Count
        if (-not [string]::IsNullOrWhiteSpace($Query)) {
            $groups[$key] = @($groups[$key] | Where-Object { ($_ | ConvertTo-Json -Depth 7 -Compress) -match [regex]::Escape($Query) })
        }
        $groups[$key] = @($groups[$key] | Select-Object -First $Limit)
    }
    $returnedRecords = 0
    foreach ($key in @($groups.Keys)) { $returnedRecords += @($groups[$key]).Count }
    $sourceBytes = [Text.Encoding]::UTF8.GetByteCount($indexRaw)
    $diagnostics = [pscustomobject][ordered]@{
        source = 'json-baseline'
        reader = 'powershell-json'
        readerLoadDurationMs = 0
        sqlDurationMs = $null
        scannedRecords = @($index.domainTypes).Count + @($index.invariants).Count + @($index.persistenceMappings).Count
        candidateRecords = $candidateRecords
        returnedRecords = $returnedRecords
        sourceHash = $null
        sourceBytesVerified = $sourceBytes
        sourceBytesMaterialized = $sourceBytes
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
