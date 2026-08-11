[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$manifest = Get-Content -LiteralPath (Join-Path $repositoryRoot 'docs/architecture/backend-modules.json') -Raw | ConvertFrom-Json
$generatorText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Build-LlmWikiModulePages.ps1') -Raw

if ([int]$manifest.inventory.folderModules -ne 39 -or
    [int]$manifest.inventory.extractedModules -ne 2 -or
    [int]$manifest.inventory.totalModules -ne 41 -or
    @($manifest.modules.PSObject.Properties).Count -ne 41) {
    throw 'Backend module inventory must explicitly explain 39 folder + 2 extracted = 41 modules.'
}
foreach ($module in @('Billing', 'Marketing')) {
    $boundary = $manifest.modules.$module
    if ($boundary.physicalIsolation -ne 'assembly' -or @($boundary.sourceMappings.applicationProjects).Count -ne 1) {
        throw "Extracted module '$module' is not represented as an assembly and source project."
    }
}
if ('Meals' -notin @($manifest.modules.Consumptions.sourceMappings.domainAreas) -or
    'Meals' -notin @($manifest.modules.Consumptions.sourceMappings.persistenceAreas)) {
    throw 'Consumptions does not map its Meals domain/persistence vocabulary explicitly.'
}
$owners = @{}
foreach ($property in @($manifest.modules.PSObject.Properties)) {
    foreach ($entity in @($property.Value.ownedEntities)) {
        if ($owners.ContainsKey($entity)) { throw "Owned entity '$entity' has multiple owners: $($owners[$entity]), $($property.Name)." }
        $owners[$entity] = $property.Name
    }
}
foreach ($contract in @('Business-module dependencies:', 'Abstraction-contract dependencies:', 'Host/adapter consumers:', '## Boundary Health', '## Public Surface', 'Exported repository-shaped contracts:', 'none observed', 'discovery evidence, not proof')) {
    if (-not $generatorText.Contains($contract)) { throw "Generated module pages omit '$contract'." }
}

Write-Host 'LLM Wiki backend module model regression passed: unified inventory, ownership, mappings, evidence types, and limitations are explicit.'
