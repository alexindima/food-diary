[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$indexPath = Join-Path $wikiRoot 'generated/architecture-health-index.json'

$index = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json
$toolProjects = @(
    @($index.untrackedProductionProjects) |
        Where-Object { [string]$_.path -match '^\.llm-wiki/tools/' }
)

if ($toolProjects.Count -ne 0) {
    throw "Internal Wiki tool projects must not be treated as production projects: $($toolProjects.path -join ', ')"
}

Write-Host 'Architecture health internal-tool exclusion regression passed.'
