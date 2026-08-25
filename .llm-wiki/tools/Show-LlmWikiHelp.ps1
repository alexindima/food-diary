[CmdletBinding()]
param(
    [ValidateSet('core', 'governed', 'experimental')]
    [string]$Tier = 'core',
    [switch]$Detailed
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
if ($Detailed) {
    Write-Host 'Detailed command catalog:'
    foreach ($registryTier in @($registry.tiers)) {
        Write-Host ''
        Write-Host "$($registryTier.id): $($registryTier.description)"
        foreach ($command in @($registryTier.commands)) {
            Write-Host "  ./.llm-wiki/wiki.ps1 $command"
        }
    }
    exit 0
}
Write-Host "Core workflow ($($selectedTier[0].description))"
foreach ($entry in @($selectedTier[0].helpEntries)) {
    Write-Host "  ./.llm-wiki/wiki.ps1 $entry"
}
Write-Host ''
Write-Host 'Command stability tiers: core, governed, experimental.'
Write-Host 'Administrative and compatibility commands:'
Write-Host '  ./.llm-wiki/wiki.ps1 help -Detailed'
