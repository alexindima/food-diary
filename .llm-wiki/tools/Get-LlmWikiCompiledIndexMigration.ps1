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
            '.llm-wiki/generated/quality-index.json'
        )) { 'migrated' } else { 'pending' }
        consumerCount = $consumers.Count
        removable = $consumers.Count -eq 0
        consumers = [string[]]$consumers
    }
}

$report = [pscustomobject][ordered]@{
    schemaVersion = 1
    removableCount = @($results | Where-Object removable).Count
    indexes = @($results)
}
if ($Format -eq 'Json') { $report | ConvertTo-Json -Depth 6; exit 0 }
Write-Host "Compiled index migration: $($report.removableCount) safely removable index(es)."
foreach ($item in $results) {
    Write-Host " - $($item.path): query-layer=$($item.queryLayer), consumers=$($item.consumerCount), removable=$($item.removable)"
}
