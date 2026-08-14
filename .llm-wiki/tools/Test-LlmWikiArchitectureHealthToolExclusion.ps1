[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$indexPath = Join-Path $wikiRoot 'generated/architecture-health-index.json'

$index = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json
$catalog = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/repository-catalog.json') -Raw | ConvertFrom-Json
$symbols = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/csharp-symbol-index.json') -Raw | ConvertFrom-Json
$catalogToolProjects = @(@($catalog.dotnet.projects) | Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' })
$toolSymbols = @(@($symbols.symbols) | Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' })
if ($catalogToolProjects.Count -ne 0 -or $toolSymbols.Count -ne 0) {
    throw 'Internal Wiki tool sources must not make production catalog or symbol indexes platform-dependent.'
}
$toolProjects = @(
    @($index.untrackedProductionProjects) |
        Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' }
)

if ($toolProjects.Count -ne 0) {
    throw "Internal Wiki tool projects must not be treated as production projects: $($toolProjects.path -join ', ')"
}

Write-Host 'Architecture health internal-tool exclusion regression passed.'
