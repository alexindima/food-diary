[CmdletBinding()]
param([switch]$Check, [switch]$ReuseUnchangedCheck)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$outputPath = Join-Path $wikiRoot 'generated/architecture-health-index.json'
$cachePath = Join-Path $repositoryRoot '.artifacts/llm-wiki/index-cache/architecture-health-index.json'
$cacheInputs = @(
    '.llm-wiki/generated/repository-catalog.json',
    '.llm-wiki/generated/backend-contract-index.json',
    '.llm-wiki/generated/frontend-contract-index.json',
    '.llm-wiki/generated/quality-index.json',
    'docs/architecture/module-dependencies.json',
    'docs/architecture/backend-modules.json',
    'tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs',
    '.llm-wiki/tools/Build-LlmWikiArchitectureHealthIndex.ps1',
    '.llm-wiki/tools/LlmWikiJson.ps1',
    '.llm-wiki/tools/LlmWikiIndexCache.ps1'
)
$inputFingerprint = Get-LlmWikiIndexInputFingerprint $repositoryRoot $cacheInputs
if ($ReuseUnchangedCheck -and (Test-LlmWikiIndexCache $cachePath $outputPath $inputFingerprint)) { Write-Host 'Architecture health index cache hit: inputs, generator, and output are unchanged.'; exit 0 }
$catalog = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/repository-catalog.json') -Raw | ConvertFrom-Json
$backendContracts = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/backend-contract-index.json') -Raw | ConvertFrom-Json
$frontendContracts = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/frontend-contract-index.json') -Raw | ConvertFrom-Json
$quality = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/quality-index.json') -Raw | ConvertFrom-Json
$moduleGraph = Get-Content -LiteralPath (Join-Path $repositoryRoot 'docs/architecture/module-dependencies.json') -Raw | ConvertFrom-Json
$boundaryManifest = Get-Content -LiteralPath (Join-Path $repositoryRoot 'docs/architecture/backend-modules.json') -Raw | ConvertFrom-Json

$projects = @($catalog.dotnet.projects)
$projectByPath = @{}
foreach ($project in $projects) { $projectByPath[$project.path] = $project.name }
$actualEdges = [System.Collections.Generic.List[object]]::new()
foreach ($project in $projects) {
    $isTestProject = [bool]$project.isTestProject -or $project.path -match '^tests/'
    foreach ($reference in @($project.projectReferences)) {
        $targetProject = $projects | Where-Object path -eq $reference | Select-Object -First 1
        $actualEdges.Add([pscustomobject]@{
            source = $project.name
            target = if ($null -ne $targetProject) { $targetProject.name } else { [System.IO.Path]::GetFileNameWithoutExtension($reference) }
            sourcePath = $project.path
            targetPath = $reference
            isTest = $isTestProject
        })
    }
}

$matrixPath = Join-Path $repositoryRoot 'tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs'
$matrixContent = [System.IO.File]::ReadAllText($matrixPath)
$productionSection = $matrixContent.Substring(
    $matrixContent.IndexOf('AllowedProductionProjectReferences', [System.StringComparison]::Ordinal),
    $matrixContent.IndexOf('AllowedTestProjectReferences', [System.StringComparison]::Ordinal) -
        $matrixContent.IndexOf('AllowedProductionProjectReferences', [System.StringComparison]::Ordinal)
)
$allowed = @{}
foreach ($match in [regex]::Matches($productionSection, '(?s)\["(?<project>[^"]+)"\]\s*=\s*\[(?<references>.*?)\],')) {
    $allowed[$match.Groups['project'].Value] = @(
        [regex]::Matches($match.Groups['references'].Value, '"(?<name>[^"]+)"') |
            ForEach-Object { $_.Groups['name'].Value }
    )
}
$violations = @(
    $actualEdges |
        Where-Object {
            -not $_.isTest -and (
                -not $allowed.ContainsKey($_.source) -or
                $_.target -notin @($allowed[$_.source])
            )
        }
)
$unusedAllowances = [System.Collections.Generic.List[object]]::new()
foreach ($source in @($allowed.Keys | Sort-Object)) {
    $actualTargets = @($actualEdges | Where-Object { -not $_.isTest -and $_.source -eq $source } | Select-Object -ExpandProperty target)
    foreach ($target in @($allowed[$source] | Sort-Object)) {
        if ($target -notin $actualTargets) {
            $unusedAllowances.Add([pscustomobject]@{ source = $source; target = $target })
        }
    }
}
$untrackedProductionProjects = @(
    $projects |
        Where-Object { -not $_.isTestProject -and $_.path -notmatch '^tests/' -and -not $allowed.ContainsKey($_.name) } |
        Select-Object name, path
)

$remainingModules = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($property in $moduleGraph.modules.PSObject.Properties) { $null = $remainingModules.Add($property.Name) }
$indegree = @{}
foreach ($name in $remainingModules) { $indegree[$name] = 0 }
foreach ($property in $moduleGraph.modules.PSObject.Properties) {
    foreach ($dependency in @($property.Value)) {
        if ($indegree.ContainsKey($dependency)) { $indegree[$dependency]++ }
    }
}
$queue = [System.Collections.Generic.Queue[string]]::new()
foreach ($name in @($remainingModules | Sort-Object)) { if ($indegree[$name] -eq 0) { $queue.Enqueue($name) } }
while ($queue.Count -gt 0) {
    $name = $queue.Dequeue()
    $null = $remainingModules.Remove($name)
    foreach ($dependency in @($moduleGraph.modules.$name)) {
        if (-not $indegree.ContainsKey($dependency)) { continue }
        $indegree[$dependency]--
        if ($indegree[$dependency] -eq 0) { $queue.Enqueue($dependency) }
    }
}
$moduleCycleNodes = @($remainingModules | Sort-Object)
$moduleHotspots = @(
    foreach ($property in @($moduleGraph.modules.PSObject.Properties)) {
        $moduleName = $property.Name
        $fanOut = @($property.Value).Count
        $fanIn = @($moduleGraph.modules.PSObject.Properties | Where-Object { @($_.Value) -contains $moduleName }).Count
        $level = if ($fanIn -ge 10 -or $fanOut -ge 8) { 'review-candidate' } else { 'informational' }
        [pscustomobject]@{
            module = $moduleName
            fanIn = $fanIn
            fanOut = $fanOut
            level = $level
            role = [string]$boundaryManifest.modules.$moduleName.role
        }
    }
)

$referencedFrontendComponents = @($frontendContracts.consumerEdges.component | Sort-Object -Unique)
$selectorUnreferenced = @(
    $frontendContracts.components |
        Where-Object { $_.class -notin $referencedFrontendComponents } |
        Select-Object class, selector, feature, path, templatePath, specPath
)
$result = [ordered]@{
    schemaVersion = 1
    summary = [ordered]@{
        productionProjectEdges = @($actualEdges | Where-Object { -not $_.isTest }).Count
        dependencyViolations = $violations.Count
        unusedAllowances = $unusedAllowances.Count
        untrackedProductionProjects = $untrackedProductionProjects.Count
        moduleCycleNodes = $moduleCycleNodes.Count
        backendBusinessModules = [int]$boundaryManifest.inventory.totalModules
        moduleHotspotReviewCandidates = @($moduleHotspots | Where-Object level -eq 'review-candidate').Count
        ambiguousBackendContracts = @($backendContracts.contracts | Where-Object ambiguous).Count
        unconsumedBackendContracts = @($backendContracts.contracts | Where-Object { $_.name -notin @($backendContracts.consumerEdges.contract) }).Count
        selectorUnreferencedComponents = $selectorUnreferenced.Count
        componentsWithoutDirectSpecs = @($frontendContracts.components | Where-Object { $null -eq $_.specPath }).Count
        criticalSymbolsWithoutTestReferences = @($quality.criticalSymbols | Where-Object testReferenceCount -eq 0).Count
        explicitDebtMarkers = @($quality.debtMarkers).Count
    }
    projectDependencyViolations = $violations
    unusedProjectAllowances = @($unusedAllowances)
    untrackedProductionProjects = $untrackedProductionProjects
    moduleCycleNodes = $moduleCycleNodes
    moduleHotspots = @($moduleHotspots | Sort-Object @{ Expression = { $_.fanIn + $_.fanOut }; Descending = $true }, module)
    ambiguousBackendContracts = @($backendContracts.contracts | Where-Object ambiguous)
    unconsumedBackendContracts = @($backendContracts.contracts | Where-Object { $_.name -notin @($backendContracts.consumerEdges.contract) })
    selectorUnreferencedComponents = $selectorUnreferenced
    componentsWithoutDirectSpecs = @($frontendContracts.components | Where-Object { $null -eq $_.specPath })
    criticalSymbolsWithoutTestReferences = @($quality.criticalSymbols | Where-Object testReferenceCount -eq 0)
    explicitDebtMarkers = @($quality.debtMarkers)
}
$jsonText = ($result | ConvertTo-Json -Depth 10) + [Environment]::NewLine
if ($Check) {
    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $jsonText -Depth 10)) {
        Write-Host 'Architecture health index is stale. Run ./.llm-wiki/wiki.ps1 update.'
        exit 1
    }
    if ($violations.Count -gt 0 -or $untrackedProductionProjects.Count -gt 0 -or $moduleCycleNodes.Count -gt 0) {
        Write-Host "Architecture drift detected: $($violations.Count) dependency violations, $($untrackedProductionProjects.Count) untracked projects, $($moduleCycleNodes.Count) cycle nodes."
        exit 1
    }
    Write-LlmWikiIndexCache $cachePath $outputPath $inputFingerprint
    $productionEdgeCount = @($actualEdges | Where-Object { -not $_.isTest }).Count
    $testEdgeCount = @($actualEdges | Where-Object isTest).Count
    Write-Host "Architecture health index is current: $productionEdgeCount production project edges, $testEdgeCount test project edges, no enforced drift."
    exit 0
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $jsonText, $utf8WithoutBom)
Write-LlmWikiIndexCache $cachePath $outputPath $inputFingerprint
Write-Host 'Generated .llm-wiki/generated/architecture-health-index.json.'
