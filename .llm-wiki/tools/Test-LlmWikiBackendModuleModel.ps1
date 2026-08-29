[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$manifestText = Get-Content -LiteralPath (Join-Path $repositoryRoot 'docs/architecture/backend-modules.json') -Raw
$manifestLines = @($manifestText -split '\r?\n')
$modulesStart = [Array]::IndexOf($manifestLines, '  "modules": {')
if ($modulesStart -lt 0) { throw 'Backend module manifest modules section could not be found for duplicate-key validation.' }
$moduleKeys = @()
$moduleDepth = 1
for ($index = $modulesStart + 1; $index -lt $manifestLines.Count -and $moduleDepth -gt 0; $index++) {
    $line = $manifestLines[$index]
    if ($moduleDepth -eq 1 -and $line -match '^    "(?<name>[^"]+)": \{$') {
        $moduleKeys += $Matches['name']
    }
    $moduleDepth += @([regex]::Matches($line, '\{')).Count
    $moduleDepth -= @([regex]::Matches($line, '\}')).Count
}
if ($moduleDepth -ne 0) { throw 'Backend module manifest modules section is not structurally balanced.' }
$duplicateModuleKeys = @($moduleKeys | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name)
if ($duplicateModuleKeys.Count -gt 0) { throw "Backend module manifest contains duplicate module keys: $($duplicateModuleKeys -join ', ')." }
$manifest = $manifestText | ConvertFrom-Json
$catalog = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/generated/repository-catalog.json') -Raw | ConvertFrom-Json
$generatorText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Build-LlmWikiModulePages.ps1') -Raw

if ([int]$manifest.inventory.folderModules + [int]$manifest.inventory.extractedModules -ne [int]$manifest.inventory.totalModules -or
    @($manifest.modules.PSObject.Properties).Count -ne [int]$manifest.inventory.totalModules) {
    throw 'Backend module inventory must reconcile folder, extracted, and total module counts.'
}
foreach ($extractedModule in @($catalog.extractedApplicationModules)) {
    $module = [string]$extractedModule.name
    $boundary = $manifest.modules.$module
    $projectPath = [string]$extractedModule.project
    if ($null -eq $boundary -or $boundary.physicalIsolation -notin @('assembly', 'project') -or
        [string]::IsNullOrWhiteSpace($projectPath) -or
        -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $projectPath) -PathType Leaf)) {
        throw "Extracted module '$module' is not represented as an isolated source project."
    }
}
if ('Meals' -notin @($manifest.modules.Meals.sourceMappings.domainAreas) -or
    'Meals' -notin @($manifest.modules.Meals.sourceMappings.persistenceAreas)) {
    throw 'Meals does not map its Meals domain/persistence vocabulary explicitly.'
}
$fastingMappings = $manifest.modules.Fasting.sourceMappings
foreach ($requiredMapping in @('applicationProjects', 'applicationAbstractionProjects', 'contractProjects', 'domainProjects', 'infrastructureProjects', 'persistenceModelProjects')) {
    if ($null -eq $fastingMappings.PSObject.Properties[$requiredMapping] -or
        @($fastingMappings.$requiredMapping).Count -eq 0) {
        throw "Fasting does not map its '$requiredMapping' source root explicitly."
    }
}
$centralPersistenceMapping = $fastingMappings.PSObject.Properties['persistenceAreas']
$centralDomainMapping = $fastingMappings.PSObject.Properties['domainAreas']
if (($null -ne $centralPersistenceMapping -and @($centralPersistenceMapping.Value).Count -gt 0) -or
    ($null -ne $centralDomainMapping -and @($centralDomainMapping.Value).Count -gt 0)) {
    throw 'Fasting still declares central domain or persistence areas after physical extraction.'
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
if ($generatorText -match 'Get-ChildItem[^\r\n]+-Recurse' -or
    -not $generatorText.Contains('[IO.File]::ReadAllText') -or
    -not $generatorText.Contains('$sourceFilesByArea')) {
    throw 'Module-page generation must index relevant C# sources once instead of recursively rereading each module and host tree.'
}

Write-Host 'LLM Wiki backend module model regression passed: unified inventory, ownership, mappings, single-pass source indexing, evidence types, and limitations are explicit.'
