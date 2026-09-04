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
    if ($null -eq $boundary -or $boundary.physicalIsolation -notin @('assembly', 'project', 'logical-module', 'module-root') -or
        [string]::IsNullOrWhiteSpace($projectPath) -or
        -not (Test-Path -LiteralPath (Join-Path $repositoryRoot $projectPath) -PathType Leaf)) {
        throw "Extracted module '$module' is not represented as an isolated source project."
    }
}
$expectedExtractedProjects = @{
    Fasting = 'Modules/Fasting/Application/FoodDiary.Modules.Fasting.Application.csproj'
    Hydration = 'Modules/Hydration/Application/FoodDiary.Modules.Hydration.Application.csproj'
    WeeklyGoals = 'Modules/WeeklyGoals/Application/FoodDiary.Modules.WeeklyGoals.Application.csproj'
}
foreach ($expectedExtractedProject in $expectedExtractedProjects.GetEnumerator()) {
    $catalogModule = @($catalog.extractedApplicationModules | Where-Object name -eq $expectedExtractedProject.Key)
    if ($catalogModule.Count -ne 1 -or [string]$catalogModule[0].project -ne $expectedExtractedProject.Value) {
        throw "Extracted Application project '$($expectedExtractedProject.Key)' is not discovered at '$($expectedExtractedProject.Value)'."
    }
}
if ('Modules/Meals/Domain' -notin @($manifest.modules.Meals.sourceMappings.domainProjects) -or
    'Modules/Meals/Infrastructure/Model' -notin @($manifest.modules.Meals.sourceMappings.persistenceModelProjects)) {
    throw 'Meals does not map its extracted domain and persistence-model projects explicitly.'
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

# Exercise the generator's path resolver without running the generator or writing fixtures.
$generatorAst = [Management.Automation.Language.Parser]::ParseInput($generatorText, [ref]$null, [ref]$null)
$resolver = $generatorAst.Find({ param($node)
    $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Resolve-AbstractionAreaPath'
}, $true)
if ($null -eq $resolver) { throw 'Module-page abstraction path resolver is missing.' }
. ([scriptblock]::Create($resolver.Extent.Text))
foreach ($case in @(
    @{ area = 'Users'; expected = 'Modules/Users/Application/Abstractions' }
    @{ area = 'Authentication/Abstractions'; expected = 'Authentication/Abstractions' }
    @{ area = 'Modules/BodyMetrics/Application/Abstractions'; expected = 'Modules/BodyMetrics/Application/Abstractions' }
    @{ area = '.\Modules\Billing\Application\Abstractions\'; expected = 'Modules/Billing/Application/Abstractions' }
    @{ area = 'FoodDiary.Application.Abstractions/Users'; expected = 'FoodDiary.Application.Abstractions/Users' }
)) {
    $actual = Resolve-AbstractionAreaPath $case.area
    if ($actual -ne $case.expected) { throw "Abstraction area '$($case.area)' resolved to '$actual' instead of '$($case.expected)'." }
}

# Independently count public source files at explicit manifest paths. This catches
# accidental legacy prefixes in either source discovery or public-surface discovery.
foreach ($property in @($manifest.modules.PSObject.Properties)) {
    $mapping = $property.Value.sourceMappings
    $abstractionAreasProperty = $mapping.PSObject.Properties['abstractionAreas']
    if ($null -eq $abstractionAreasProperty) { continue }
    $explicitAreas = @($abstractionAreasProperty.Value | Where-Object { [string]$_ -like 'Modules/*' })
    if ($explicitAreas.Count -eq 0) { continue }
    $contractProjectsProperty = $mapping.PSObject.Properties['contractProjects']
    $contractProjects = @(if ($null -ne $contractProjectsProperty) { $contractProjectsProperty.Value })
    $publicPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($area in @($explicitAreas) + @($contractProjects)) {
        if ([string]::IsNullOrWhiteSpace([string]$area)) { continue }
        foreach ($file in @(Get-ChildItem -LiteralPath (Join-Path $repositoryRoot $area) -Recurse -File -Filter '*.cs')) {
            if ($file.FullName -match '[\\/](?:obj|bin|\.artifacts)[\\/]') { continue }
            if ([IO.File]::ReadAllText($file.FullName) -match '\bpublic\s+(?:(?:sealed|abstract|partial|readonly|static)\s+)*(?:interface|record|class|struct|enum)\b') {
                [void]$publicPaths.Add($file.FullName)
            }
        }
    }
    $slug = [regex]::Replace($property.Name, '([a-z0-9])([A-Z])', '$1-$2').ToLowerInvariant()
    $page = Get-Content -LiteralPath (Join-Path $repositoryRoot ".llm-wiki/generated/modules/$slug.md") -Raw
    if ($page -notmatch '(?m)^- Public contract files: (\d+)' -or [int]$Matches[1] -ne $publicPaths.Count) {
        throw "Module '$($property.Name)' must report $($publicPaths.Count) public contract files from its explicit source paths."
    }
}

Write-Host 'LLM Wiki backend module model regression passed: unified inventory, ownership, legacy and module-relative mappings, public surfaces, single-pass source indexing, evidence types, and limitations are explicit.'
