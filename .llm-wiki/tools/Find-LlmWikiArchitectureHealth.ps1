[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'drift', 'allowances', 'untracked', 'cycles', 'ambiguous', 'dead-candidates', 'spec-gaps', 'test-gaps', 'debt')]
    [string]$View = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
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
    $resultJson = [LlmWiki.SqliteReader.CompiledIndexReader]::QueryArchitectureHealth(
        $repositoryRoot,
        $View,
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
    $indexRaw = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/architecture-health-index.json') -Raw
    $index = $indexRaw | ConvertFrom-Json
    if ($View -in @('all', 'drift')) { $groups.dependencyViolations = @($index.projectDependencyViolations) }
    if ($View -eq 'allowances') { $groups.unusedAllowances = @($index.unusedProjectAllowances) }
    if ($View -eq 'untracked') { $groups.untrackedProjects = @($index.untrackedProductionProjects) }
    if ($View -eq 'cycles') { $groups.moduleCycleNodes = @($index.moduleCycleNodes) }
    if ($View -eq 'ambiguous') { $groups.ambiguousContracts = @($index.ambiguousBackendContracts) }
    if ($View -eq 'dead-candidates') {
        $groups.unconsumedBackendContracts = @($index.unconsumedBackendContracts)
        $groups.selectorUnreferencedComponents = @($index.selectorUnreferencedComponents)
    }
    if ($View -eq 'spec-gaps') { $groups.componentsWithoutSpecs = @($index.componentsWithoutDirectSpecs) }
    if ($View -eq 'test-gaps') { $groups.criticalSymbolsWithoutTests = @($index.criticalSymbolsWithoutTestReferences) }
    if ($View -eq 'debt') { $groups.debtMarkers = @($index.explicitDebtMarkers) }
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
        source = 'json-baseline'; reader = 'powershell-json'; readerLoadDurationMs = 0; sqlDurationMs = $null
        scannedRecords = $null; candidateRecords = $candidateRecords; returnedRecords = $returnedRecords; sourceHash = $null
        sourceBytesVerified = $sourceBytes; sourceBytesMaterialized = $sourceBytes
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
