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
. (Join-Path $PSScriptRoot 'LlmWikiRuntimeTopologyFingerprint.ps1')
$topologyPath = Join-Path $wikiRoot 'generated/runtime-topology.json'
$storedTopology = Get-Content -LiteralPath $topologyPath -Raw | ConvertFrom-Json
$currentFreshness = Get-LlmWikiRuntimeTopologyFingerprint -RepositoryRoot $repositoryRoot
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$groups = [ordered]@{}
$diagnostics = $null
$querySupplied = -not [string]::IsNullOrWhiteSpace($Query)
$behavioralQuery = $querySupplied -and $Query -match '(?i)retry|resilien|timeout|cancellation|idempoten|duplicate|deduplic|replay|outbox|concurren'
if ($CompiledIndexSource -eq 'Sqlite') {
    . (Join-Path $PSScriptRoot 'LlmWikiInProcessSqlite.ps1')
    $reader = Initialize-LlmWikiInProcessSqlite -Projection runtime
    $resultJson = [LlmWiki.SqliteReader.CompiledIndexReader]::QueryRuntime(
        $repositoryRoot,
        $Query,
        $Limit,
        [bool]($IncludeDiagnostics -or $querySupplied),
        [double]$reader.loadDurationMs)
    $result = $resultJson | ConvertFrom-Json
    foreach ($property in $result.PSObject.Properties) {
        if ($property.Name -eq '_diagnostics') { $diagnostics = $property.Value }
        else { $groups[$property.Name] = @($property.Value) }
    }
} else {
    $topologyRaw = Get-Content -LiteralPath $topologyPath -Raw
    $topology = $topologyRaw | ConvertFrom-Json
    $groups = [ordered]@{
        composeServices = @($topology.composeServices)
        hostedServices = @($topology.hostedServices)
        httpClients = @($topology.httpClients)
        webhooks = @($topology.webhooks)
        recurringJobRegistrations = @($topology.recurringJobRegistrations)
        networkPolicies = @($topology.networkPolicies)
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
        scannedRecords = @($topology.composeServices).Count + @($topology.hostedServices).Count + @($topology.httpClients).Count + @($topology.webhooks).Count + @($topology.recurringJobRegistrations).Count + @($topology.networkPolicies).Count
        candidateRecords = $candidateRecords; returnedRecords = $returnedRecords; sourceHash = $null
        sourceBytesVerified = $sourceBytes; sourceBytesMaterialized = $sourceBytes
    }
}
$returnedRecords = 0
foreach ($key in @($groups.Keys)) { $returnedRecords += @($groups[$key]).Count }
$candidateRecords = if ($null -ne $diagnostics) { [int]$diagnostics.candidateRecords } else { $returnedRecords }
$selectionStatus = if (-not $querySupplied) {
    'unfiltered'
} elseif ($returnedRecords -gt 0) {
    'matched'
} elseif ($candidateRecords -gt 0) {
    'abstained-empty-filter'
} else {
    'empty-index'
}
if ($querySupplied) {
    $groups['_selection'] = [pscustomobject][ordered]@{
        status = $selectionStatus
        query = $Query
        queryKind = $(if ($behavioralQuery) { 'behavioral-signal' } else { 'record-text' })
        candidateRecords = $candidateRecords
        returnedRecords = $returnedRecords
        recallConfidence = $(if ($selectionStatus -eq 'abstained-empty-filter') { 'low' } elseif ($selectionStatus -eq 'matched') { 'bounded' } else { 'not-rated' })
        recommendation = $(if ($selectionStatus -eq 'abstained-empty-filter' -and $behavioralQuery) { "Runtime topology contains only registered runtime surfaces and their inferred signals. Continue with research -Intent '$Query' and test-plan -Intent '$Query' to inspect code paths that are not hosted services, clients, webhooks, jobs, or network policies." } elseif ($selectionStatus -eq 'abstained-empty-filter') { 'Repeat topology without -Query, then inspect the relevant category with a narrower term.' } else { $null })
    }
}
$groups['_freshness'] = [pscustomobject][ordered]@{
    verified = [string]$storedTopology.freshness.sourceFingerprint -eq [string]$currentFreshness.sourceFingerprint
    storedSourceFingerprint = [string]$storedTopology.freshness.sourceFingerprint
    currentSourceFingerprint = [string]$currentFreshness.sourceFingerprint
    sourceFileCount = [int]$currentFreshness.sourceFileCount
}
$stopwatch.Stop()
if ($Format -eq 'Json') {
    if ($IncludeDiagnostics) {
        $diagnostics | Add-Member -NotePropertyName completeCommandDurationMs -NotePropertyValue ([Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)) -Force
        $groups['_diagnostics'] = $diagnostics
    }
    [pscustomobject]$groups | ConvertTo-Json -Depth 8
    exit 0
}
if ($IncludeDiagnostics -and $null -ne $diagnostics) {
    Write-Host "Source: $($diagnostics.source), reader=$($diagnostics.reader), returned=$($diagnostics.returnedRecords)/$($diagnostics.candidateRecords), load=$($diagnostics.readerLoadDurationMs)ms, query=$($diagnostics.sqlDurationMs)ms."
}
if ($selectionStatus -eq 'abstained-empty-filter') {
    Write-Host "Abstained: topology query '$Query' matched no records although the full index contains $candidateRecords. $($groups['_selection'].recommendation)"
}
Write-Host 'Evidence boundary: repository declarations and inferred code signals do not prove effective production exposure, IAM, grants, DNS behavior, or webhook idempotency.'
foreach ($key in @($groups.Keys | Where-Object { $_ -notmatch '^_' })) {
    Write-Host "$key ($(@($groups[$key]).Count)):"
    foreach ($item in @($groups[$key] | Select-Object -First $Limit)) {
        Write-Host " - $(($item | ConvertTo-Json -Compress))"
    }
    Write-Host ''
}
