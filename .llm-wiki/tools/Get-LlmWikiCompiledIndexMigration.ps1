[CmdletBinding()]
param(
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$indexes = @(
    '.llm-wiki/generated/repository-catalog.json',
    '.llm-wiki/generated/csharp-symbol-index.json',
    '.llm-wiki/generated/frontend-index.json',
    '.llm-wiki/generated/backend-contract-index.json',
    '.llm-wiki/generated/frontend-contract-index.json',
    '.llm-wiki/generated/quality-index.json'
)
$results = foreach ($indexPath in $indexes) {
    $isCatalogOrSymbol = $indexPath -in @(
        '.llm-wiki/generated/repository-catalog.json',
        '.llm-wiki/generated/csharp-symbol-index.json'
    )
    $isBackendContract = $indexPath -eq '.llm-wiki/generated/backend-contract-index.json'
    $isFrontendContract = $indexPath -eq '.llm-wiki/generated/frontend-contract-index.json'
    $isFrontend = $indexPath -eq '.llm-wiki/generated/frontend-index.json'
    $fileName = Split-Path -Leaf $indexPath
    $matches = @(& rg -l --hidden --fixed-strings --glob '*.ps1' --glob '*.mjs' --glob '*.md' $fileName $repositoryRoot 2>$null)
    $consumers = @($matches | ForEach-Object {
        [IO.Path]::GetRelativePath($repositoryRoot, [string]$_).Replace('\', '/')
    } | Where-Object {
        $_ -ne '.llm-wiki/tools/Get-LlmWikiCompiledIndexMigration.ps1'
    } | Sort-Object -Unique)
    [pscustomobject][ordered]@{
        path = $indexPath
        queryLayer = if ($isFrontend) {
            'partial'
        } elseif ($indexPath -in @(
            '.llm-wiki/generated/backend-contract-index.json',
            '.llm-wiki/generated/frontend-contract-index.json',
            '.llm-wiki/generated/quality-index.json',
            '.llm-wiki/generated/repository-catalog.json',
            '.llm-wiki/generated/csharp-symbol-index.json'
        )) {
            'migrated'
        } else {
            'pending'
        }
        defaultRoute = if ($isCatalogOrSymbol) { 'sqlite-compiled-index' } elseif ($isFrontend) { 'sqlite-context-and-diff; json-task-brief' } elseif ($isBackendContract -or $isFrontendContract) { 'sqlite-query-documents' } else { 'index-specific' }
        automaticJsonFallback = if ($isCatalogOrSymbol -or $isFrontend -or $isBackendContract -or $isFrontendContract) { $false } else { $null }
        retainedAs = if ($isFrontend) { 'projection-source-explicit-parity-and-task-brief-source' } elseif ($isCatalogOrSymbol -or $isBackendContract -or $isFrontendContract) { 'projection-source-and-explicit-parity-baseline' } else { 'compiled-source' }
        consumerCount = $consumers.Count
        removable = $consumers.Count -eq 0
        consumers = [string[]]$consumers
    }
}

$report = [pscustomobject][ordered]@{
    schemaVersion = 3
    migratedQueryLayerCount = @($results | Where-Object queryLayer -eq 'migrated').Count
    partialQueryLayerCount = @($results | Where-Object queryLayer -eq 'partial').Count
    removableCount = @($results | Where-Object removable).Count
    indexes = @($results)
}
if ($Format -eq 'Json') { $report | ConvertTo-Json -Depth 6; exit 0 }
Write-Host "Compiled index migration: $($report.removableCount) safely removable index(es)."
foreach ($item in $results) {
    Write-Host " - $($item.path): query-layer=$($item.queryLayer), default=$($item.defaultRoute), automatic-json-fallback=$($item.automaticJsonFallback), consumers=$($item.consumerCount), removable=$($item.removable)"
}
