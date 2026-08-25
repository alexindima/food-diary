[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [switch]$IncludeDiagnostics
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$groups = [ordered]@{}
$diagnostics = $null
if ($CompiledIndexSource -eq 'Sqlite') {
    . (Join-Path $PSScriptRoot 'LlmWikiInProcessSqlite.ps1')
    $reader = Initialize-LlmWikiInProcessSqlite
    $resultJson = [LlmWiki.SqliteReader.CompiledIndexReader]::QueryRuntime(
        $repositoryRoot,
        $Query,
        $Limit,
        [bool]$IncludeDiagnostics,
        [double]$reader.loadDurationMs)
    if ($Format -eq 'Json') { $resultJson; exit 0 }
    $result = $resultJson | ConvertFrom-Json
    foreach ($property in $result.PSObject.Properties) {
        if ($property.Name -eq '_diagnostics') { $diagnostics = $property.Value }
        else { $groups[$property.Name] = @($property.Value) }
    }
} else {
    $topologyRaw = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/runtime-topology.json') -Raw
    $topology = $topologyRaw | ConvertFrom-Json
    $groups = [ordered]@{
        composeServices = @($topology.composeServices)
        hostedServices = @($topology.hostedServices)
        httpClients = @($topology.httpClients)
        webhooks = @($topology.webhooks)
        recurringJobRegistrations = @($topology.recurringJobRegistrations)
    }
    $candidateRecords = 0
    foreach ($key in @($groups.Keys)) {
        $candidateRecords += @($groups[$key]).Count
        if (-not [string]::IsNullOrWhiteSpace($Query)) {
            $groups[$key] = @($groups[$key] | Where-Object { ($_ | ConvertTo-Json -Compress) -match [regex]::Escape($Query) })
        }
        $groups[$key] = @($groups[$key] | Select-Object -First $Limit)
    }
    $returnedRecords = 0
    foreach ($key in @($groups.Keys)) { $returnedRecords += @($groups[$key]).Count }
    $sourceBytes = [Text.Encoding]::UTF8.GetByteCount($topologyRaw)
    $diagnostics = [pscustomobject][ordered]@{
        source = 'json-baseline'; reader = 'powershell-json'; readerLoadDurationMs = 0; sqlDurationMs = $null
        scannedRecords = @($topology.composeServices).Count + @($topology.hostedServices).Count + @($topology.httpClients).Count + @($topology.webhooks).Count + @($topology.recurringJobRegistrations).Count
        candidateRecords = $candidateRecords; returnedRecords = $returnedRecords; sourceHash = $null
        sourceBytesVerified = $sourceBytes; sourceBytesMaterialized = $sourceBytes
    }
}
$stopwatch.Stop()
if ($Format -eq 'Json') {
    if ($IncludeDiagnostics) {
        $diagnostics | Add-Member -NotePropertyName completeCommandDurationMs -NotePropertyValue ([Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2))
        $groups['_diagnostics'] = $diagnostics
    }
    [pscustomobject]$groups | ConvertTo-Json -Depth 8
    exit 0
}
if ($IncludeDiagnostics -and $null -ne $diagnostics) {
    Write-Host "Source: $($diagnostics.source), reader=$($diagnostics.reader), returned=$($diagnostics.returnedRecords)/$($diagnostics.candidateRecords), load=$($diagnostics.readerLoadDurationMs)ms, query=$($diagnostics.sqlDurationMs)ms."
}
foreach ($key in $groups.Keys) {
    Write-Host "$key ($(@($groups[$key]).Count)):"
    foreach ($item in @($groups[$key] | Select-Object -First $Limit)) {
        Write-Host " - $(($item | ConvertTo-Json -Compress))"
    }
    Write-Host ''
}
