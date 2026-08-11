[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$catalogPath = Join-Path $wikiRoot 'generated/repository-catalog.json'
$boundaryManifestPath = Join-Path $repositoryRoot 'docs/architecture/backend-modules.json'
$outputRoot = Join-Path $wikiRoot 'generated/modules'
$generatorPath = '.llm-wiki/tools/Build-LlmWikiModulePages.ps1'

if (-not (Test-Path -LiteralPath $catalogPath)) {
    throw 'Repository catalog is missing. Run Build-LlmWikiCatalog.ps1 first.'
}
if (-not (Test-Path -LiteralPath $boundaryManifestPath)) {
    throw 'Backend module boundary manifest is missing: docs/architecture/backend-modules.json.'
}

function ConvertTo-RepositoryPath {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($resolvedPath)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

function ConvertTo-Slug {
    param([string]$Name)

    $slug = [regex]::Replace($Name, '([a-z0-9])([A-Z])', '$1-$2')
    return $slug.ToLowerInvariant()
}

function Get-RepositoryFilePaths {
    $paths = @(& git -C $repositoryRoot ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to enumerate repository files.'
    }
    return @($paths | Where-Object { $_ } | Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique)
}

function New-FrontMatter {
    param(
        [string]$Id,
        [string[]]$Sources
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add('---')
    $lines.Add("id: $Id")
    $lines.Add('kind: module')
    $lines.Add('status: current')
    $lines.Add("generated_by: $generatorPath")
    $lines.Add('sources:')
    foreach ($source in $Sources) {
        $lines.Add("  - $source")
    }
    $lines.Add('---')
    return ,$lines
}

$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$boundaryManifest = Get-Content -LiteralPath $boundaryManifestPath -Raw | ConvertFrom-Json
$allModules = [System.Collections.Generic.List[object]]::new()
foreach ($graphModule in $catalog.applicationModules) {
    $allModules.Add([pscustomobject]@{
        name = $graphModule.name
        dependencies = @($graphModule.dependencies)
        origin = 'module-graph'
        project = $null
        boundary = $boundaryManifest.modules.($graphModule.name)
    })
}
foreach ($extractedModule in $catalog.extractedApplicationModules) {
    if (@($allModules | Where-Object { $_.name -eq $extractedModule.name }).Count -eq 0) {
        $allModules.Add([pscustomobject]@{
            name = $extractedModule.name
            dependencies = @()
            origin = 'extracted-project'
            project = $extractedModule.project
            boundary = $boundaryManifest.modules.($extractedModule.name)
        })
    }
}
$moduleNames = @($allModules | ForEach-Object { $_.name })
if ($moduleNames.Count -ne [int]$boundaryManifest.inventory.totalModules) {
    throw "Backend module inventory mismatch: catalog=$($moduleNames.Count), manifest=$($boundaryManifest.inventory.totalModules)."
}
$missingBoundaryModules = @($moduleNames | Where-Object { $null -eq $boundaryManifest.modules.$_ })
if ($missingBoundaryModules.Count -gt 0) {
    throw "Backend module boundary metadata is missing for: $($missingBoundaryModules -join ', ')."
}
$repositoryFilePaths = @(Get-RepositoryFilePaths)
$directoryPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($filePath in $repositoryFilePaths) {
    $segments = @($filePath.Replace('\', '/').Split('/'))
    for ($length = 1; $length -lt $segments.Count; $length++) {
        [void]$directoryPaths.Add(($segments[0..($length - 1)] -join '/'))
    }
}
$allDirectories = @(
    $directoryPaths |
        Where-Object {
            "/$_/" -notmatch '/(\.git|\.github|\.llm-wiki|docs|node_modules|obj|bin|dist[^/]*|coverage|\.artifacts|TestResults)/'
        } |
        Where-Object { Test-Path -LiteralPath (Join-Path $repositoryRoot $_) } |
        ForEach-Object { Get-Item -LiteralPath (Join-Path $repositoryRoot $_) -Force }
)
$allTestFiles = @(
    $repositoryFilePaths |
        Where-Object {
            "/$_" -match '/tests/' -and
            $_ -match '\.cs$' -and
            "/$_/" -notmatch '/(obj|bin|\.artifacts|TestResults)/'
        } |
        Where-Object { Test-Path -LiteralPath (Join-Path $repositoryRoot $_) } |
        ForEach-Object { Get-Item -LiteralPath (Join-Path $repositoryRoot $_) -Force }
)

function Get-BoundaryMappingValues {
    param([object]$Boundary, [string]$Name, [string[]]$Default)

    $property = $Boundary.sourceMappings.PSObject.Properties[$Name]
    if ($null -ne $property -and @($property.Value).Count -gt 0) {
        return @($property.Value)
    }
    return @($Default)
}

function Get-SourceAreaPaths {
    param([string]$ModuleName, [object]$Boundary)

    $candidatePaths = [Collections.Generic.List[string]]::new()
    foreach ($projectName in @(Get-BoundaryMappingValues $Boundary 'applicationProjects' @("FoodDiary.Application/$ModuleName"))) {
        $candidatePaths.Add([string]$projectName)
    }
    foreach ($area in @(Get-BoundaryMappingValues $Boundary 'abstractionAreas' @($ModuleName))) {
        $candidatePaths.Add("FoodDiary.Application.Abstractions/$area")
    }
    foreach ($area in @(Get-BoundaryMappingValues $Boundary 'domainAreas' @($ModuleName))) {
        $candidatePaths.Add("FoodDiary.Domain/Entities/$area")
    }
    foreach ($area in @(Get-BoundaryMappingValues $Boundary 'persistenceAreas' @($ModuleName))) {
        $candidatePaths.Add("FoodDiary.Infrastructure/Persistence/$area")
        $candidatePaths.Add("FoodDiary.Infrastructure/Persistence/Configurations/$area")
    }
    foreach ($area in @(Get-BoundaryMappingValues $Boundary 'adapterAreas' @())) {
        $candidatePaths.Add([string]$area)
    }
    $candidatePaths.Add("FoodDiary.Presentation.Api/Features/$ModuleName")

    return @($candidatePaths | Sort-Object { Get-LlmWikiOrdinalSortKey $_ } -Unique | Where-Object {
        Test-Path -LiteralPath (Join-Path $repositoryRoot $_)
    })
}

function Get-ReferencedContractAreas {
    param([string[]]$SourceAreas, [string]$OwningModule)

    $areas = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($sourceArea in $SourceAreas) {
        $absoluteArea = Join-Path $repositoryRoot $sourceArea
        if (-not (Test-Path -LiteralPath $absoluteArea -PathType Container)) { continue }
        foreach ($file in @(Get-ChildItem -LiteralPath $absoluteArea -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue)) {
            $content = Get-Content -LiteralPath $file.FullName -Raw
            foreach ($match in [regex]::Matches($content, 'FoodDiary\.Application\.Abstractions\.(?<area>[A-Za-z0-9_]+)')) {
                $area = $match.Groups['area'].Value
                if ($area -notin @('Common', $OwningModule)) { [void]$areas.Add($area) }
            }
        }
    }
    return @($areas | Sort-Object { Get-LlmWikiOrdinalSortKey $_ })
}

function Get-HostConsumers {
    param([string]$ModuleName, [string[]]$ContractAreas, [string]$ExtractedProject)

    $consumers = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    if (-not [string]::IsNullOrWhiteSpace($ExtractedProject)) {
        foreach ($project in @($catalog.dotnet.projects)) {
            if (-not [bool]$project.isTestProject -and @($project.projectReferences) -contains $ExtractedProject) {
                [void]$consumers.Add([string]$project.name)
            }
        }
    }
    $searchAreas = @($ModuleName) + @($ContractAreas)
    foreach ($hostArea in @('FoodDiary.Presentation.Api', 'FoodDiary.JobManager', 'FoodDiary.Initializer', 'FoodDiary.Web.Api', 'FoodDiary.Integrations')) {
        $hostRoot = Join-Path $repositoryRoot $hostArea
        if (-not (Test-Path -LiteralPath $hostRoot)) { continue }
        $matched = $false
        foreach ($file in @(Get-ChildItem -LiteralPath $hostRoot -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue)) {
            $content = Get-Content -LiteralPath $file.FullName -Raw
            if (@($searchAreas | Where-Object { $content -match "FoodDiary\.Application(?:\.Abstractions)?\.$([regex]::Escape($_))(?:\.|;)" }).Count -gt 0) {
                $matched = $true
                break
            }
        }
        if ($matched) { [void]$consumers.Add($hostArea) }
    }
    return @($consumers | Sort-Object { Get-LlmWikiOrdinalSortKey $_ })
}

$generatedFiles = [ordered]@{}
$usersContextReadiness = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -Format Json | ConvertFrom-Json
$usersProfileReadiness = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserProfileReadService -Format Json | ConvertFrom-Json
$indexLines = New-FrontMatter 'generated.application-modules' @(
    $generatorPath
    '.llm-wiki/generated/repository-catalog.json'
    'docs/architecture/module-dependencies.json'
    'docs/architecture/backend-modules.json'
)
$indexLines.Add('')
$indexLines.Add('# Application Modules')
$indexLines.Add('')
$indexLines.Add('This index unifies 39 folder modules and 2 extracted application modules.')
$indexLines.Add('Business-module edges, abstraction contracts, adapter consumers, and runtime composition')
$indexLines.Add('are reported separately; `none observed` never means proven isolation.')
$indexLines.Add('')
$indexLines.Add('| Module | Role | Business deps | Contract deps | App consumers | Host consumers | Enforcement |')
$indexLines.Add('| --- | --- | ---: | ---: | ---: | ---: | --- |')

foreach ($module in @($allModules | Sort-Object { Get-LlmWikiOrdinalSortKey $_.name })) {
    $moduleName = [string]$module.name
    $slug = ConvertTo-Slug $moduleName
    $relativeOutputPath = ".llm-wiki/generated/modules/$slug.md"
    $boundary = $module.boundary
    $dependencies = @($module.dependencies | Sort-Object { Get-LlmWikiOrdinalSortKey $_ })
    $consumers = @(
        $allModules |
            Where-Object { @($_.dependencies) -contains $moduleName } |
            ForEach-Object { $_.name } |
            Sort-Object { Get-LlmWikiOrdinalSortKey $_ }
    )
    $controllers = @(
        $catalog.http.controllers |
            Where-Object {
                $_.path -match "/Features/$([regex]::Escape($moduleName))/" -or
                $_.name -like "$moduleName*Controller"
            } |
            Sort-Object { Get-LlmWikiOrdinalSortKey $_.path }
    )
    $sourceDirectories = @(Get-SourceAreaPaths $moduleName $boundary)
    $applicationSourceAreas = @($sourceDirectories | Where-Object { $_ -match '^FoodDiary\.Application(?:\.|/)' })
    $contractAreas = @(Get-BoundaryMappingValues $boundary 'abstractionAreas' @($moduleName))
    $contractDependencies = @(Get-ReferencedContractAreas $applicationSourceAreas $moduleName)
    $hostConsumers = @(Get-HostConsumers $moduleName $contractAreas ([string]$module.project))
    $publicContractFiles = @(
        foreach ($area in $contractAreas) {
            $root = Join-Path $repositoryRoot "FoodDiary.Application.Abstractions/$area"
            if (Test-Path -LiteralPath $root) {
                Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue |
                    Where-Object { (Get-Content -LiteralPath $_.FullName -Raw) -match '\bpublic\s+(?:(?:sealed|abstract|partial|readonly|static)\s+)*(?:interface|record|class|struct|enum)\b' }
            }
        }
    )
    $publicContractTypes = @(
        foreach ($contractFile in $publicContractFiles) {
            $content = Get-Content -LiteralPath $contractFile.FullName -Raw
            foreach ($match in [regex]::Matches($content, '\bpublic\s+(?:(?:sealed|abstract|partial|readonly|static)\s+)*(?<kind>interface|record(?:\s+(?:class|struct))?|class|struct|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')) {
                [pscustomobject]@{
                    kind = $match.Groups['kind'].Value
                    name = $match.Groups['name'].Value
                    repositoryShaped = $match.Groups['name'].Value -match 'Repository|Store'
                    projectionShaped = $match.Groups['name'].Value -match '(?:Dto|Model|ReadModel|Projection|Response|Request)$'
                    domainEntityReference = $content -match '\bFoodDiary\.Domain\.Entities\.' -or $content -match '\bResult<User>\b'
                }
            }
        }
    )
    $tests = @(
        $allTestFiles |
            Where-Object {
                (ConvertTo-RepositoryPath $_.FullName) -match "/$([regex]::Escape($moduleName))(/|[^/]*Tests?\.cs$)"
            } |
            ForEach-Object { ConvertTo-RepositoryPath $_.FullName } |
            Sort-Object { Get-LlmWikiOrdinalSortKey $_ } |
            Select-Object -First 30
    )

    $lines = New-FrontMatter "generated.module.$slug" @(
        $generatorPath
        '.llm-wiki/generated/repository-catalog.json'
        'docs/architecture/module-dependencies.json'
        'docs/architecture/backend-modules.json'
    )
    $lines.Add('')
    $lines.Add("# $moduleName")
    $lines.Add('')
    $lines.Add('## Graph')
    $lines.Add('')
    $lines.Add("- Origin: $($module.origin)")
    if (-not [string]::IsNullOrWhiteSpace($module.project)) {
        $lines.Add(('- Extracted project: `{0}`' -f $module.project))
    }
    $lines.Add("- Business-module dependencies: $(if ($dependencies.Count) { $dependencies -join ', ' } else { 'none observed' })")
    $lines.Add("- Abstraction-contract dependencies: $(if ($contractDependencies.Count) { $contractDependencies -join ', ' } else { 'none observed' })")
    $lines.Add("- Business-module consumers: $(if ($consumers.Count) { $consumers -join ', ' } else { 'none observed' })")
    $lines.Add("- Host/adapter consumers: $(if ($hostConsumers.Count) { $hostConsumers -join ', ' } else { 'none observed' })")
    $lines.Add('- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.')
    $lines.Add('')
    $lines.Add('## Source Areas')
    $lines.Add('')
    if ($sourceDirectories.Count -eq 0) {
        $lines.Add('- No exact module-named source directory was found.')
    } else {
        foreach ($sourceDirectory in $sourceDirectories) {
            $lines.Add(('- `{0}`' -f $sourceDirectory))
        }
    }
    $lines.Add('')
    $lines.Add('## HTTP Surface')
    $lines.Add('')
    if ($controllers.Count -eq 0) {
        $lines.Add('No literal attribute-routed controller was associated with this module.')
    } else {
        foreach ($controller in $controllers) {
            $lines.Add("### $($controller.name)")
            $lines.Add('')
            $lines.Add(('Source: `{0}`' -f $controller.path))
            $lines.Add('')
            foreach ($endpoint in $controller.endpoints) {
                $lines.Add(('- `{0} {1}`' -f $endpoint.verb, $endpoint.route))
            }
            $lines.Add('')
        }
    }
    $lines.Add('## Boundary Health')
    $lines.Add('')
    $lines.Add("- Role: $($boundary.role)")
    $lines.Add("- Physical isolation: $($boundary.physicalIsolation)")
    $lines.Add("- Architecture guardrails: $($boundary.enforcement)")
    $lines.Add("- Declared owned entities: $(if (@($boundary.ownedEntities).Count) { @($boundary.ownedEntities) -join ', ' } else { 'not yet enumerated' })")
    $lines.Add("- Public contract files: $($publicContractFiles.Count)")
    $lines.Add("- Observed external consumer groups: $($consumers.Count + $hostConsumers.Count)")
    $lines.Add("- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page")
    $lines.Add('')
    $lines.Add('## Public Surface')
    $lines.Add('')
    $lines.Add("- Public contract types: $($publicContractTypes.Count)")
    $lines.Add("- Interfaces: $(@($publicContractTypes | Where-Object kind -eq 'interface').Count)")
    $lines.Add("- DTO/read-model/projection types: $(@($publicContractTypes | Where-Object projectionShaped).Count)")
    $lines.Add("- Enums: $(@($publicContractTypes | Where-Object kind -eq 'enum').Count)")
    $lines.Add("- Exported repository-shaped contracts: $(@($publicContractTypes | Where-Object repositoryShaped).Count)")
    $lines.Add("- Contracts referencing domain entities: $(@($publicContractTypes | Where-Object domainEntityReference).Count)")
    if ($publicContractTypes.Count -eq 0) {
        $lines.Add('- No public declaration was found in the mapped abstraction areas.')
    } else {
        foreach ($contractType in @($publicContractTypes | Sort-Object kind, name | Select-Object -First 30)) {
            $lines.Add(('- `{0} {1}`' -f $contractType.kind, $contractType.name))
        }
        if ($publicContractTypes.Count -gt 30) { $lines.Add("- ... $($publicContractTypes.Count - 30) more type(s)") }
    }
    $lines.Add('')
    if ($moduleName -eq 'Users') {
        $legacyConsumerModules = @($usersContextReadiness.consumers.consumer | Sort-Object -Unique)
        $profileConsumerModules = @($usersProfileReadiness.consumers.consumer | Sort-Object -Unique)
        $lines.Add('## Extraction Readiness')
        $lines.Add('')
        $lines.Add("- Abstraction-owned profile-read consumers: $($usersProfileReadiness.readiness.productionConsumers) across $($profileConsumerModules.Count) group(s)")
        $lines.Add("- Implementation-owned `IUserContextService` consumers: $($usersContextReadiness.readiness.productionConsumers) across $($legacyConsumerModules.Count) group(s)")
        $lines.Add("- Consumers receiving the `User` aggregate: $($usersContextReadiness.readiness.aggregateConsumers)")
        $lines.Add("- Consumers with aggregate mutation access: $($usersContextReadiness.readiness.mutationConsumers)")
        $lines.Add("- Composition registrations: $($usersContextReadiness.readiness.compositionRegistrations)")
        $lines.Add("- Remaining blocker classes: $(@($usersContextReadiness.readiness.blockers).Count)")
        $lines.Add("- Extraction readiness: $(if (@($usersContextReadiness.readiness.blockers).Count) { 'partial; migrate legacy aggregate/mutation consumers' } else { 'ready by observed contract evidence' })")
        $lines.Add('')
        $lines.Add('| Consumer | Contract | Owning assembly | Methods/data | Access | Extraction |')
        $lines.Add('| --- | --- | --- | --- | --- | --- |')
        foreach ($consumerGroup in @($usersContextReadiness.consumers | Group-Object consumer | Sort-Object Name)) {
            $entries = @($consumerGroup.Group)
            $methods = @($entries.methods | Where-Object { $_ } | Sort-Object -Unique)
            $data = @($entries.returnedData | Where-Object { $_ } | Sort-Object -Unique)
            $access = @($entries.access | Sort-Object -Unique)
            $safe = @($entries | Where-Object { -not $_.extractionSafe }).Count -eq 0
            $lines.Add("| $($consumerGroup.Name) | IUserContextService | $($usersContextReadiness.owningAssembly) | $(if ($methods.Count) { $methods -join ', ' } else { 'constructor/registration only' }) => $(if ($data.Count) { $data -join ', ' } else { 'inherited or unresolved' }) | $($access -join ', ') | $(if ($safe) { 'safe' } else { 'migration-required' }) |")
        }
        $lines.Add('')
    }
    $lines.Add('## Focused Tests')
    $lines.Add('')
    $lines.Add('Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.')
    $lines.Add('')
    if ($tests.Count -eq 0) {
        $lines.Add('No test file with an exact module path/name match was found.')
    } else {
        foreach ($test in $tests) {
            $testKind = if ($test -match 'ArchitectureTests') { 'architecture-boundary' } elseif ($test -match 'IntegrationTests') { 'integration' } elseif ($test -match 'Presentation') { 'presentation' } else { 'behavioral-or-text-match' }
            $lines.Add(('- [{0}] `{1}`' -f $testKind, $test))
        }
    }
    $lines.Add('')
    $lines.Add('## Working Rule')
    $lines.Add('')
    $lines.Add('Use this page for discovery only. Read the nearest scoped `AGENTS.md` and')
    $lines.Add('verify behavior in source code, tests, and API contract snapshots before')
    $lines.Add('changing the module.')

    $generatedFiles[$relativeOutputPath] = ($lines -join [Environment]::NewLine) + [Environment]::NewLine
    $indexLines.Add("| [$moduleName]($slug.md) | $($boundary.role) | $($dependencies.Count) | $($contractDependencies.Count) | $($consumers.Count) | $($hostConsumers.Count) | $($boundary.enforcement) |")
}

$indexRelativePath = '.llm-wiki/generated/modules/index.md'
$generatedFiles[$indexRelativePath] = ($indexLines -join [Environment]::NewLine) + [Environment]::NewLine

if ($Check) {
    $errors = [System.Collections.Generic.List[string]]::new()
    foreach ($entry in $generatedFiles.GetEnumerator()) {
        $absolutePath = Join-Path $repositoryRoot $entry.Key
        if (-not (Test-Path -LiteralPath $absolutePath)) {
            $errors.Add("Missing generated module page: $($entry.Key)")
            continue
        }
        if (-not (Test-LlmWikiTextEquivalent -ActualPath $absolutePath -ExpectedText $entry.Value)) {
            $errors.Add("Stale generated module page: $($entry.Key)")
        }
    }

    if (Test-Path -LiteralPath $outputRoot) {
        $actualGeneratedPaths = @(
            Get-ChildItem -LiteralPath $outputRoot -File -Filter '*.md' |
                ForEach-Object { ConvertTo-RepositoryPath $_.FullName }
        )
        foreach ($actualPath in $actualGeneratedPaths) {
            if (-not $generatedFiles.Contains($actualPath)) {
                $errors.Add("Unexpected generated module page: $actualPath")
            }
        }
    }

    if ($errors.Count -gt 0) {
        Write-Host "Generated application-module pages are stale ($($errors.Count) error(s)):"
        foreach ($errorMessage in $errors) {
            Write-Host " - $errorMessage"
        }
        Write-Host 'Regenerate with ./.llm-wiki/tools/Build-LlmWikiModulePages.ps1'
        exit 1
    }

    Write-Host "Generated application-module pages are current: $($moduleNames.Count) modules."
    return
}

if (-not (Test-Path -LiteralPath $outputRoot)) {
    New-Item -ItemType Directory -Path $outputRoot | Out-Null
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
foreach ($entry in $generatedFiles.GetEnumerator()) {
    $absolutePath = Join-Path $repositoryRoot $entry.Key
    [System.IO.File]::WriteAllText($absolutePath, $entry.Value, $utf8WithoutBom)
}

Get-ChildItem -LiteralPath $outputRoot -File -Filter '*.md' |
    Where-Object { -not $generatedFiles.Contains((ConvertTo-RepositoryPath $_.FullName)) } |
    Remove-Item -Force

Write-Host "Generated $($moduleNames.Count) application-module pages and index."
