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
    '.llm-wiki/generated/quality-index.json',
    '.llm-wiki/generated/runtime-topology.json',
    '.llm-wiki/generated/sensitive-data-index.json',
    '.llm-wiki/generated/domain-data-index.json',
    '.llm-wiki/generated/architecture-health-index.json'
)
$results = foreach ($indexPath in $indexes) {
    $isCatalogOrSymbol = $indexPath -in @(
        '.llm-wiki/generated/repository-catalog.json',
        '.llm-wiki/generated/csharp-symbol-index.json'
    )
    $isBackendContract = $indexPath -eq '.llm-wiki/generated/backend-contract-index.json'
    $isFrontendContract = $indexPath -eq '.llm-wiki/generated/frontend-contract-index.json'
    $isFrontend = $indexPath -eq '.llm-wiki/generated/frontend-index.json'
    $isSensitiveData = $indexPath -eq '.llm-wiki/generated/sensitive-data-index.json'
    $isTaskBriefImpact = $indexPath -in @(
        '.llm-wiki/generated/backend-contract-index.json',
        '.llm-wiki/generated/frontend-contract-index.json',
        '.llm-wiki/generated/quality-index.json',
        '.llm-wiki/generated/runtime-topology.json',
        '.llm-wiki/generated/sensitive-data-index.json',
        '.llm-wiki/generated/domain-data-index.json',
        '.llm-wiki/generated/architecture-health-index.json'
    )
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
            '.llm-wiki/generated/frontend-contract-index.json',
            '.llm-wiki/generated/frontend-index.json',
            '.llm-wiki/generated/quality-index.json',
            '.llm-wiki/generated/runtime-topology.json',
            '.llm-wiki/generated/sensitive-data-index.json',
            '.llm-wiki/generated/domain-data-index.json',
            '.llm-wiki/generated/architecture-health-index.json',
            '.llm-wiki/generated/repository-catalog.json',
            '.llm-wiki/generated/csharp-symbol-index.json'
        )) {
            'migrated'
        } else {
            'pending'
        }
        defaultRoute = if ($isCatalogOrSymbol) { 'sqlite-compiled-index' } elseif ($isFrontend) { 'sqlite-context-diff-task-brief-trace-and-impact-simulation' } elseif ($isBackendContract -or $isFrontendContract) { 'sqlite-query-documents-and-task-brief-impact' } elseif ($isSensitiveData) { 'sqlite-sensitive-data-and-task-brief-impact' } elseif ($isTaskBriefImpact) { 'sqlite-task-brief-impact; json-standalone-query' } else { 'index-specific' }
        automaticJsonFallback = if ($isCatalogOrSymbol -or $isFrontend -or $isTaskBriefImpact) { $false } else { $null }
        retainedAs = if ($isFrontend -or $isCatalogOrSymbol -or $isBackendContract -or $isFrontendContract -or $isSensitiveData) { 'projection-source-and-explicit-parity-baseline' } elseif ($isTaskBriefImpact) { 'projection-source-explicit-parity-and-standalone-query-source' } else { 'compiled-source' }
        consumerCount = $consumers.Count
        removable = $consumers.Count -eq 0
        consumers = [string[]]$consumers
    }
}

$report = [pscustomobject][ordered]@{
    schemaVersion = 6
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
