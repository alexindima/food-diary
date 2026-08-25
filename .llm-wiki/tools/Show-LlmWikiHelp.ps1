[CmdletBinding()]
param(
    [ValidateSet('core', 'governed', 'experimental')]
    [string]$Tier = 'core'
)

$ErrorActionPreference = 'Stop'
$registryPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'policies/command-registry.json'
$registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json
$selectedTier = @($registry.tiers | Where-Object id -eq $Tier)
if ($selectedTier.Count -ne 1) {
    throw "Command registry must contain exactly one '$Tier' tier."
}

Write-Host 'FoodDiary LLM Wiki'
Write-Host ''
Write-Host "Core workflow ($($selectedTier[0].description))"
foreach ($entry in @($selectedTier[0].helpEntries)) {
    Write-Host "  ./.llm-wiki/wiki.ps1 $entry"
}
Write-Host ''
Write-Host 'Command stability tiers: core, governed, experimental.'
Write-Host 'Administrative and compatibility commands:'
Write-Host '  ./.llm-wiki/wiki.ps1 help -Detailed'
