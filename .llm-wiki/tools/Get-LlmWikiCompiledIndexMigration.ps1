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
    '.llm-wiki/generated/backend-contract-index.json',
    '.llm-wiki/generated/quality-index.json'
)
$results = foreach ($indexPath in $indexes) {
    $isCatalogOrSymbol = $indexPath -in @(
        '.llm-wiki/generated/repository-catalog.json',
        '.llm-wiki/generated/csharp-symbol-index.json'
    )
    $isBackendContract = $indexPath -eq '.llm-wiki/generated/backend-contract-index.json'
    $fileName = Split-Path -Leaf $indexPath
    $matches = @(& rg -l --hidden --fixed-strings --glob '*.ps1' --glob '*.mjs' --glob '*.md' $fileName $repositoryRoot 2>$null)
    $consumers = @($matches | ForEach-Object {
        [IO.Path]::GetRelativePath($repositoryRoot, [string]$_).Replace('\', '/')
    } | Where-Object {
        $_ -ne '.llm-wiki/tools/Get-LlmWikiCompiledIndexMigration.ps1'
    } | Sort-Object -Unique)
    [pscustomobject][ordered]@{
        path = $indexPath
        queryLayer = if ($indexPath -in @(
            '.llm-wiki/generated/backend-contract-index.json',
            '.llm-wiki/generated/quality-index.json',
            '.llm-wiki/generated/repository-catalog.json',
            '.llm-wiki/generated/csharp-symbol-index.json'
        )) {
            'migrated'
        } else {
            'pending'
        }
        defaultRoute = if ($isCatalogOrSymbol) { 'sqlite-compiled-index' } elseif ($isBackendContract) { 'sqlite-query-documents' } else { 'index-specific' }
        automaticJsonFallback = if ($isCatalogOrSymbol -or $isBackendContract) { $false } else { $null }
        retainedAs = if ($isCatalogOrSymbol -or $isBackendContract) { 'projection-source-and-explicit-parity-baseline' } else { 'compiled-source' }
        consumerCount = $consumers.Count
        removable = $consumers.Count -eq 0
        consumers = [string[]]$consumers
    }
}

$report = [pscustomobject][ordered]@{
    schemaVersion = 2
    migratedQueryLayerCount = @($results | Where-Object queryLayer -eq 'migrated').Count
    removableCount = @($results | Where-Object removable).Count
    indexes = @($results)
}
if ($Format -eq 'Json') { $report | ConvertTo-Json -Depth 6; exit 0 }
Write-Host "Compiled index migration: $($report.removableCount) safely removable index(es)."
foreach ($item in $results) {
    Write-Host " - $($item.path): query-layer=$($item.queryLayer), default=$($item.defaultRoute), automatic-json-fallback=$($item.automaticJsonFallback), consumers=$($item.consumerCount), removable=$($item.removable)"
}
