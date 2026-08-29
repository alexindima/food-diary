[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$outputPath = Join-Path $wikiRoot 'generated/repository-catalog.json'
[xml]$rootBuildProps = Get-Content -LiteralPath (Join-Path $repositoryRoot 'Directory.Build.props') -Raw
$rootPropertyGroups = @($rootBuildProps.Project.PropertyGroup)

function ConvertTo-RepositoryPath {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($resolvedPath)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

function Get-FirstXmlValue {
    param(
        [object[]]$PropertyGroups,
        [string]$PropertyName
    )

    foreach ($propertyGroup in $PropertyGroups) {
        if ($null -eq $propertyGroup) { continue }
        $propertyEntry = $propertyGroup.PSObject.Properties[$PropertyName]
        $property = if ($null -eq $propertyEntry) { $null } else { $propertyEntry.Value }
        if ($null -ne $property -and -not [string]::IsNullOrWhiteSpace([string]$property)) {
            return [string]$property
        }
    }
    return $null
}

function Join-RouteTemplate {
    param(
        [string]$ControllerRoute,
        [string]$ActionRoute
    )

    $parts = @(
        $ControllerRoute.Trim('/')
        $ActionRoute.Trim('/')
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    if ($parts.Count -eq 0) {
        return '/'
    }
    return '/' + ($parts -join '/')
}

$projectFiles = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.csproj' |
        Where-Object {
            $_.FullName -notmatch '[\\/](obj|bin|\.artifacts|TestResults)[\\/]' -and
            $_.FullName -notmatch '[\\/]\.llm-wiki[\\/]tools[\\/]'
        } |
        Sort-Object FullName
)

$dotnetProjects = [System.Collections.Generic.List[object]]::new()
foreach ($projectFile in $projectFiles) {
    [xml]$projectXml = Get-Content -LiteralPath $projectFile.FullName -Raw
    $propertyGroups = @($projectXml.Project.PropertyGroup)
    $projectPath = ConvertTo-RepositoryPath $projectFile.FullName
    $projectDirectory = $projectFile.DirectoryName
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
    $assemblyName = Get-FirstXmlValue $propertyGroups 'AssemblyName'
    if ([string]::IsNullOrWhiteSpace($assemblyName)) {
        $assemblyName = $projectName
    }

    $targetFramework = Get-FirstXmlValue $propertyGroups 'TargetFramework'
    $targetFrameworks = Get-FirstXmlValue $propertyGroups 'TargetFrameworks'
    if ([string]::IsNullOrWhiteSpace($targetFramework) -and [string]::IsNullOrWhiteSpace($targetFrameworks)) {
        $targetFramework = Get-FirstXmlValue $rootPropertyGroups 'TargetFramework'
        $targetFrameworks = Get-FirstXmlValue $rootPropertyGroups 'TargetFrameworks'
    }
    $frameworks = @()
    if (-not [string]::IsNullOrWhiteSpace($targetFrameworks)) {
        $frameworks = @($targetFrameworks.Split(';') | Where-Object { $_ })
    } elseif (-not [string]::IsNullOrWhiteSpace($targetFramework)) {
        $frameworks = @($targetFramework)
    }

    $projectReferences = @(
        @($projectXml.Project.ItemGroup.ProjectReference) |
            Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_.Include) } |
            ForEach-Object {
                ConvertTo-RepositoryPath (Join-Path $projectDirectory ([string]$_.Include))
            } |
            Sort-Object -Unique
    )

    $packageReferences = @(
        @($projectXml.Project.ItemGroup.PackageReference) |
            Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_.Include) } |
            ForEach-Object {
                $version = if ($null -ne $_.Version) { [string]$_.Version } else { $null }
                [ordered]@{
                    name = [string]$_.Include
                    version = $version
                }
            } |
            Sort-Object { $_.name }
    )

    $isTestProjectValue = Get-FirstXmlValue $propertyGroups 'IsTestProject'
    $hasTestSdk = @($packageReferences | Where-Object { $_.name -eq 'Microsoft.NET.Test.Sdk' }).Count -gt 0
    $isTestProject = if ($isTestProjectValue -eq 'false') {
        $false
    } else {
        $isTestProjectValue -eq 'true' -or $hasTestSdk
    }
    $outputType = Get-FirstXmlValue $propertyGroups 'OutputType'
    if ([string]::IsNullOrWhiteSpace($outputType)) {
        $outputType = if ([string]$projectXml.Project.Sdk -match '\.(Web|Worker)$') { 'Exe' } else { 'Library' }
    }

    $dotnetProjects.Add([ordered]@{
        name = $projectName
        assemblyName = $assemblyName
        path = $projectPath
        sdk = [string]$projectXml.Project.Sdk
        outputType = $outputType
        targetFrameworks = $frameworks
        isTestProject = $isTestProject
        projectReferences = $projectReferences
        packageReferences = $packageReferences
    })
}

$angularWorkspacePath = Join-Path $repositoryRoot 'FoodDiary.Web.Client/angular.json'
$angularWorkspace = Get-Content -LiteralPath $angularWorkspacePath -Raw | ConvertFrom-Json
$frontendProjects = [System.Collections.Generic.List[object]]::new()
foreach ($projectProperty in @($angularWorkspace.projects.PSObject.Properties | Sort-Object { Get-LlmWikiOrdinalSortKey $_.Name })) {
    $project = $projectProperty.Value
    $targets = @()
    if ($null -ne $project.architect) {
        $targets = @($project.architect.PSObject.Properties.Name | Sort-Object { Get-LlmWikiOrdinalSortKey $_ })
    }
    $frontendProjects.Add([ordered]@{
        name = $projectProperty.Name
        projectType = [string]$project.projectType
        root = [string]$project.root
        sourceRoot = [string]$project.sourceRoot
        targets = $targets
    })
}

$moduleGraphPath = Join-Path $repositoryRoot 'docs/architecture/module-dependencies.json'
$moduleGraph = Get-Content -LiteralPath $moduleGraphPath -Raw | ConvertFrom-Json
$applicationModules = [System.Collections.Generic.List[object]]::new()
foreach ($moduleProperty in @($moduleGraph.modules.PSObject.Properties | Sort-Object { Get-LlmWikiOrdinalSortKey $_.Name })) {
    $applicationModules.Add([ordered]@{
        name = $moduleProperty.Name
        dependencies = @($moduleProperty.Value | Sort-Object { Get-LlmWikiOrdinalSortKey $_ })
    })
}

$presentationRoots = @(
    'FoodDiary.Presentation.Api',
    'MailRelay/FoodDiary.MailRelay.Presentation',
    'MailInbox/FoodDiary.MailInbox.Presentation'
)
$controllerFiles = @(
    foreach ($presentationRoot in $presentationRoots) {
        $absoluteRoot = Join-Path $repositoryRoot $presentationRoot
        if (Test-Path -LiteralPath $absoluteRoot) {
            Get-ChildItem -LiteralPath $absoluteRoot -Recurse -File -Filter '*Controller.cs'
        }
    }
) | Sort-Object FullName

$controllers = [System.Collections.Generic.List[object]]::new()
$httpAttributePattern = '\[Http(?<verb>Get|Post|Put|Patch|Delete|Head|Options)(?:\("(?<route>[^"]*)"\))?\]'
foreach ($controllerFile in $controllerFiles) {
    $content = Get-Content -LiteralPath $controllerFile.FullName -Raw
    $classMatch = [regex]::Match($content, '\bclass\s+(?<name>\w+Controller)\b')
    $httpMatches = [regex]::Matches($content, $httpAttributePattern)
    if (-not $classMatch.Success -or $httpMatches.Count -eq 0) {
        continue
    }

    $classPrefix = $content.Substring(0, $classMatch.Index)
    $routeMatches = [regex]::Matches($classPrefix, '\[Route\("(?<route>[^"]*)"\)\]')
    $controllerRoute = if ($routeMatches.Count -gt 0) {
        $routeMatches[$routeMatches.Count - 1].Groups['route'].Value
    } else {
        ''
    }

    $endpoints = [System.Collections.Generic.List[object]]::new()
    foreach ($httpMatch in $httpMatches) {
        $actionRoute = $httpMatch.Groups['route'].Value
        $lineNumber = ($content.Substring(0, $httpMatch.Index) -split "`n").Count
        $endpoints.Add([ordered]@{
            verb = $httpMatch.Groups['verb'].Value.ToUpperInvariant()
            route = Join-RouteTemplate $controllerRoute $actionRoute
            line = $lineNumber
        })
    }

    $controllers.Add([ordered]@{
        name = $classMatch.Groups['name'].Value
        path = ConvertTo-RepositoryPath $controllerFile.FullName
        routePrefix = $controllerRoute
        endpoints = @($endpoints)
    })
}

$agentGuides = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter 'AGENTS.md' |
        Where-Object { $_.FullName -notmatch '[\\/](node_modules|obj|bin|\.artifacts)[\\/]' } |
        ForEach-Object { ConvertTo-RepositoryPath $_.FullName } |
        Sort-Object { Get-LlmWikiOrdinalSortKey $_ }
)
$documentation = @(
    Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'docs') -Recurse -File -Filter '*.md' |
        ForEach-Object { ConvertTo-RepositoryPath $_.FullName } |
        Sort-Object { Get-LlmWikiOrdinalSortKey $_ }
)
$testProjects = @(
    $dotnetProjects |
        Where-Object { $_.isTestProject } |
        ForEach-Object { $_.path }
)
$extractedApplicationModules = @(
    $dotnetProjects |
        Where-Object {
            -not $_.isTestProject -and
            ($_.name -match '^FoodDiary\.Application\.(?!(?:Abstractions|Runtime)$)(?<module>[^.]+)$' -or
             $_.name -match '^FoodDiary\.Modules\.(?<module>[^.]+)$')
        } |
        ForEach-Object {
            $null = $_.name -match '^FoodDiary\.(?:Application|Modules)\.(?<module>[^.]+)$'
            [ordered]@{
                name = $Matches['module']
                project = $_.path
            }
        } |
        Sort-Object { $_.name }
)
$extractedApplicationModuleNames = @($extractedApplicationModules | ForEach-Object { [string]$_.name })
$folderApplicationModules = @($applicationModules | Where-Object { [string]$_.name -notin $extractedApplicationModuleNames })

$catalog = [ordered]@{
    schemaVersion = 1
    generator = '.llm-wiki/tools/Build-LlmWikiCatalog.ps1'
    sources = @(
        'Directory.Build.props'
        'Directory.Packages.props'
        'FoodDiary.Web.Client/angular.json'
        'docs/architecture/module-dependencies.json'
        'docs/architecture/backend-modules.json'
        '**/*.csproj'
        '**/*Controller.cs'
        '**/AGENTS.md'
        'docs/**/*.md'
    )
    summary = [ordered]@{
        dotnetProjects = $dotnetProjects.Count
        testProjects = $testProjects.Count
        frontendProjects = $frontendProjects.Count
        applicationModules = $folderApplicationModules.Count
        extractedApplicationModules = $extractedApplicationModules.Count
        backendBusinessModules = $folderApplicationModules.Count + $extractedApplicationModules.Count
        controllers = $controllers.Count
        endpoints = @($controllers | ForEach-Object { $_.endpoints }).Count
        agentGuides = $agentGuides.Count
        documentationPages = $documentation.Count
    }
    dotnet = [ordered]@{
        projects = @($dotnetProjects)
        testProjects = $testProjects
    }
    frontend = [ordered]@{
        workspace = 'FoodDiary.Web.Client/angular.json'
        projects = @($frontendProjects)
    }
    applicationModules = @($folderApplicationModules)
    extractedApplicationModules = $extractedApplicationModules
    http = [ordered]@{
        extraction = 'Literal ASP.NET Core Http* and controller Route attributes'
        controllers = @($controllers)
    }
    knowledgeSources = [ordered]@{
        agentGuides = $agentGuides
        documentation = $documentation
    }
}

$json = $catalog | ConvertTo-Json -Depth 20
$expectedContent = $json + [Environment]::NewLine

if ($Check) {
    if (-not (Test-Path -LiteralPath $outputPath)) {
        Write-Host "LLM Wiki catalog is missing: $(ConvertTo-RepositoryPath $outputPath)"
        exit 1
    }

    if (-not (Test-LlmWikiJsonEquivalent -ActualPath $outputPath -ExpectedJson $expectedContent -Depth 20)) {
        Write-Host 'LLM Wiki catalog is stale. Regenerate it with:'
        Write-Host '  ./.llm-wiki/tools/Build-LlmWikiCatalog.ps1'
        exit 1
    }

    Write-Host "LLM Wiki catalog is current: $($catalog.summary.dotnetProjects) .NET projects, $($catalog.summary.endpoints) HTTP endpoints."
    return
}

$outputDirectory = Split-Path -Parent $outputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($outputPath, $expectedContent, $utf8WithoutBom)
Write-Host "Generated $(ConvertTo-RepositoryPath $outputPath)."
