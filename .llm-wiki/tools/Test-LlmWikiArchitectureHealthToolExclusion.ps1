[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$indexPath = Join-Path $wikiRoot 'generated/architecture-health-index.json'

$index = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json
$catalog = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/repository-catalog.json') -Raw | ConvertFrom-Json
$symbols = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/csharp-symbol-index.json') -Raw | ConvertFrom-Json
$backendContracts = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/backend-contract-index.json') -Raw | ConvertFrom-Json
$quality = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/quality-index.json') -Raw | ConvertFrom-Json
$catalogToolProjects = @(@($catalog.dotnet.projects) | Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' })
$toolSymbols = @(@($symbols.symbols) | Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' })
$toolContractDefinitions = @(@($backendContracts.contracts) | Where-Object { @($_.definitionPaths | Where-Object { [string]$_ -match '^\.llm-wiki/tools/' }).Count -gt 0 })
$toolContractConsumers = @(@($backendContracts.consumerEdges) | Where-Object { [string]$_.consumerPath -match '^\.llm-wiki/tools/' })
$toolQualityFiles = @(@($quality.files) | Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' })
if ($catalogToolProjects.Count -ne 0 -or $toolSymbols.Count -ne 0 -or $toolContractDefinitions.Count -ne 0 -or $toolContractConsumers.Count -ne 0 -or $toolQualityFiles.Count -ne 0) {
    throw 'Internal Wiki tool sources must not make production catalog, symbol, contract, or quality indexes platform-dependent.'
}
$toolProjects = @(
    @($index.untrackedProductionProjects) |
        Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' }
)

if ($toolProjects.Count -ne 0) {
    throw "Internal Wiki tool projects must not be treated as production projects: $($toolProjects.path -join ', ')"
}

Write-Host 'Architecture health internal-tool exclusion regression passed.'
