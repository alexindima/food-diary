[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string[]]$BaselineExcludedPath,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 20)]
    [int]$Limit = 8
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitRenames.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiChangeSemantics.ps1')
$catalogPath = Join-Path $wikiRoot 'generated/repository-catalog.json'
$symbolIndexPath = Join-Path $wikiRoot 'generated/csharp-symbol-index.json'
$frontendIndexPath = Join-Path $wikiRoot 'generated/frontend-index.json'

function ConvertTo-RepositoryPath {
    param([string]$Path)
    return ConvertTo-LlmWikiRepositoryPath $Path
}

function ConvertTo-Slug {
    param([string]$Name)

    return [regex]::Replace($Name, '([a-z0-9])([A-Z])', '$1-$2').ToLowerInvariant()
}

$renames = @()
if (-not $PSBoundParameters.ContainsKey('ChangedPath')) {
    $workspaceHead = Test-LlmWikiWorkspaceHeadRef $HeadRef
    $gitArguments = @('diff', '--name-only', '--diff-filter=ACMRD', $BaseRef)
    if (-not $workspaceHead) {
        $gitArguments += $HeadRef
    }
    $gitArguments += '--'
    $ChangedPath = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments $gitArguments -FailureMessage "git diff failed for base '$BaseRef' and head '$HeadRef'.")

    if ($workspaceHead) {
        $ChangedPath += @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'git ls-files failed while collecting untracked paths.')
    }
    $renames = @(Get-LlmWikiGitRenames -RepositoryRoot $repositoryRoot -BaseRef $BaseRef -HeadRef $(if ($workspaceHead) { '' } else { $HeadRef }))
}

$allChangedPaths = @(
    $ChangedPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { ConvertTo-RepositoryPath $_ } |
        Sort-Object -Unique
)
$derivedWikiPaths = @($allChangedPaths | Where-Object { $_ -match '^\.llm-wiki/generated/' })
$reviewMetadataPaths = @($allChangedPaths | Where-Object {
    $_ -match '^\.llm-wiki/reviews/' -or $_ -match '(?i)(review-receipt|source-impact-review)'
})
$changedPaths = @($allChangedPaths |
    Where-Object { $_ -notin $derivedWikiPaths -and $_ -notin $reviewMetadataPaths } |
    Sort-Object -Unique)
$baselineExcludedPaths = @(
    $BaselineExcludedPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { ConvertTo-RepositoryPath $_ } |
        Sort-Object -Unique
)
$workspaceContextScopes = [Collections.Generic.List[string]]::new()
if (@($baselineExcludedPaths | Where-Object { $_ -match '\.cs$|\.csproj$|Directory\.(Build|Packages)\.props$' }).Count -gt 0) { $workspaceContextScopes.Add('Backend') }
if (@($baselineExcludedPaths | Where-Object { $_ -match 'Presentation|Web\.Api/.+\.cs$|Controller\.cs$|/Snapshots/' }).Count -gt 0) { $workspaceContextScopes.Add('Api') }
if (@($baselineExcludedPaths | Where-Object { $_ -match '^FoodDiary\.Web\.Client/|^FoodDiary\.Mobile/' }).Count -gt 0) { $workspaceContextScopes.Add('Frontend') }
if (@($baselineExcludedPaths | Where-Object { $_ -match 'Infrastructure/.*(Persistence|Migration)|Migrations?/|ModelSnapshot\.cs$' }).Count -gt 0) { $workspaceContextScopes.Add('Database') }
if ($changedPaths.Count -eq 0) {
    $emptyResult = [ordered]@{
        changedPaths = @()
        sourceChangedPaths = @()
        derivedWikiPaths = $derivedWikiPaths
        reviewMetadataPaths = $reviewMetadataPaths
        renames = @($renames)
        baselineExcludedPaths = $baselineExcludedPaths
        workspaceContextScopes = @($workspaceContextScopes | Sort-Object -Unique)
        scopes = @()
        modules = @()
        projects = @()
        agentGuides = @()
        wikiPages = @()
        focusedTests = @()
        generatedActions = @()
        warnings = @()
        recommendedChecks = @()
    }
    if ($Format -eq 'Json') {
        $emptyResult | ConvertTo-Json -Depth 8
    } else {
        Write-Host 'LLM Wiki diff context: no task-delta paths.'
        if ($baselineExcludedPaths.Count -gt 0) {
            Write-Host "Workspace context excluded by task baseline: $($baselineExcludedPaths.Count) path(s); scopes=$(if ($workspaceContextScopes.Count) { @($workspaceContextScopes | Sort-Object -Unique) -join ', ' } else { 'none detected' })."
            Write-Host 'Use -ChangedPath explicitly to include any of those paths in the current task; they are shown but not silently claimed from another session.'
        }
    }
    return
}

$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
$symbolIndex = if (Test-Path -LiteralPath $symbolIndexPath) {
    Get-Content -LiteralPath $symbolIndexPath -Raw | ConvertFrom-Json
} else {
    $null
}
$frontendIndex = if (Test-Path -LiteralPath $frontendIndexPath) {
    Get-Content -LiteralPath $frontendIndexPath -Raw | ConvertFrom-Json
} else {
    $null
}
$scopes = [ordered]@{
    Backend = @($changedPaths | Where-Object {
        $_ -match '\.cs$|\.csproj$|Directory\.(Build|Packages)\.props$' -or
        $_ -match '^(?:FoodDiary\.(?:Domain|Application(?:\.[^/]+)?|Infrastructure|Presentation\.Api|Web\.Api)|MailInbox/FoodDiary\.[^/]+|MailRelay/FoodDiary\.[^/]+)(?:/)?$'
    }).Count -gt 0
    Api = @($changedPaths | Where-Object {
        $_ -match 'Presentation|Web\.Api(?:/|$)|Controller\.cs$|/Snapshots/'
    }).Count -gt 0
    Frontend = @($changedPaths | Where-Object {
        $_ -match '^FoodDiary\.Web\.Client/|^FoodDiary\.Mobile/'
    }).Count -gt 0
    Database = @($changedPaths | Where-Object {
        $_ -match '(?:^|/)FoodDiary\.Infrastructure(?:/|$)|Infrastructure/.*(Persistence|Migration)|Migrations?/|ModelSnapshot\.cs$'
    }).Count -gt 0
    Tests = @($changedPaths | Where-Object { $_ -match '(^|/)tests/' -or $_ -match '\.(spec\.ts|test\.mjs)$' }).Count -gt 0
    Documentation = @($changedPaths | Where-Object { $_ -match '(^|/)(AGENTS|README)\.md$|^docs/|^\.llm-wiki/' }).Count -gt 0
    Configuration = @($changedPaths | Where-Object {
        $_ -match 'appsettings[^/]*\.json$|\.env\.example$|Options\.cs$'
    }).Count -gt 0
    Deployment = @($changedPaths | Where-Object {
        $_ -match '^\.github/workflows/deploy\.yml$|docker-compose[^/]*\.ya?ml$'
    }).Count -gt 0
    Localization = @($changedPaths | Where-Object { $_ -match '/assets/i18n/(en|ru)/.+\.json$' }).Count -gt 0
    Contracts = @($changedPaths | Where-Object {
        $_ -match '/Snapshots/|/Requests/|/Responses/|Controller\.cs$'
    }).Count -gt 0
}
$activeScopes = @($scopes.GetEnumerator() | Where-Object { $_.Value } | ForEach-Object { $_.Key })

$candidateModules = [System.Collections.Generic.List[object]]::new()
foreach ($graphModule in $catalog.applicationModules) {
    $candidateModules.Add([pscustomobject]@{
        name = $graphModule.name
        dependencies = @($graphModule.dependencies)
        origin = 'module-graph'
    })
}
foreach ($extractedModule in $catalog.extractedApplicationModules) {
    if (@($candidateModules | Where-Object { $_.name -eq $extractedModule.name }).Count -eq 0) {
        $candidateModules.Add([pscustomobject]@{
            name = $extractedModule.name
            dependencies = @()
            origin = 'extracted-project'
        })
    }
}

$moduleMatches = [System.Collections.Generic.List[object]]::new()
foreach ($module in $candidateModules) {
    $name = [string]$module.name
    $escapedName = [regex]::Escape($name)
    $matchingPaths = @(
        $changedPaths | Where-Object {
            $_ -match "(^|/)$escapedName(/|\.|[A-Z])" -or
            $_ -match "/Features/$escapedName/" -or
            ($module.origin -eq 'extracted-project' -and $_ -match "^FoodDiary\.Application\.$escapedName(?:/|$)")
        }
    )
    if ($matchingPaths.Count -eq 0) {
        continue
    }

    $consumers = @(
        $candidateModules |
            Where-Object { @($_.dependencies) -contains $name } |
            ForEach-Object { $_.name } |
            Sort-Object
    )
    $moduleMatches.Add([pscustomobject]@{
        name = $name
        origin = $module.origin
        score = $matchingPaths.Count
        changedPaths = $matchingPaths
        dependencies = @($module.dependencies)
        consumers = $consumers
        wikiPage = ".llm-wiki/generated/modules/$(ConvertTo-Slug $name).md"
    })
}
$matchedModules = @(
    $moduleMatches |
        Sort-Object @{ Expression = 'score'; Descending = $true }, name |
        Select-Object -First $Limit
)

$projectMatches = [System.Collections.Generic.List[object]]::new()
foreach ($project in $catalog.dotnet.projects) {
    $projectDirectory = (Split-Path -Parent $project.path).Replace('\', '/')
    $matchingPaths = @(
        $changedPaths | Where-Object {
            $_ -eq $project.path -or
            (-not [string]::IsNullOrWhiteSpace($projectDirectory) -and $_.StartsWith("$projectDirectory/"))
        }
    )
    if ($matchingPaths.Count -gt 0) {
        $projectMatches.Add([pscustomobject]@{
            name = $project.name
            path = $project.path
            isTestProject = [bool]$project.isTestProject
            changedFiles = $matchingPaths.Count
        })
    }
}
$matchedProjects = @(
    $projectMatches |
        Sort-Object @{ Expression = 'changedFiles'; Descending = $true }, path |
        Select-Object -First $Limit
)

$changedPathSet = @{}
foreach ($changedPath in $changedPaths) {
    $changedPathSet[$changedPath] = $true
}

$changedSymbols = @()
if ($null -ne $symbolIndex) {
    $changedSymbols = @(
        $symbolIndex.symbols |
            Where-Object { $changedPathSet.ContainsKey([string]$_.path) } |
            Sort-Object path, line |
            Select-Object -First ($Limit * 3)
    )
}
$changedFrontendSymbols = @()
if ($null -ne $frontendIndex) {
    $changedFrontendSymbols = @(
        $frontendIndex.symbols |
            Where-Object { $changedPathSet.ContainsKey([string]$_.path) } |
            Sort-Object path, line |
            Select-Object -First ($Limit * 3)
    )
}

$guidePaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$null = $guidePaths.Add('AGENTS.md')
foreach ($changedPath in $changedPaths) {
    foreach ($guidePath in $catalog.knowledgeSources.agentGuides) {
        $guideDirectory = (Split-Path -Parent $guidePath).Replace('\', '/')
        if (-not [string]::IsNullOrWhiteSpace($guideDirectory) -and $changedPath.StartsWith("$guideDirectory/")) {
            $null = $guidePaths.Add([string]$guidePath)
        }
    }
}
$applicableGuides = @($guidePaths | Sort-Object { ($_ -split '/').Count }, { $_ })

$impactedWikiPages = [System.Collections.Generic.List[object]]::new()
$wikiPages = Get-ChildItem -LiteralPath $wikiRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.FullName -ne (Join-Path $wikiRoot 'README.md') }
foreach ($page in $wikiPages) {
    $lines = @(Get-Content -LiteralPath $page.FullName)
    $closingDelimiter = -1
    for ($index = 1; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -eq '---') {
            $closingDelimiter = $index
            break
        }
    }
    if ($closingDelimiter -lt 0) {
        continue
    }
    $frontMatter = $lines[1..($closingDelimiter - 1)]
    $sourcesIndex = [Array]::IndexOf($frontMatter, 'sources:')
    if ($sourcesIndex -lt 0) {
        continue
    }
    $sources = @()
    for ($index = $sourcesIndex + 1; $index -lt $frontMatter.Count; $index++) {
        if ($frontMatter[$index] -match '^\s+-\s+(.+?)\s*$') {
            $sources += ConvertTo-RepositoryPath $Matches[1]
            continue
        }
        if ($frontMatter[$index] -match '^\S') {
            break
        }
    }
    $changedSources = @($sources | Where-Object { $changedPathSet.ContainsKey($_) })
    if ($changedSources.Count -gt 0) {
        $absolutePagePath = $page.FullName
        $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
        $pageUri = [System.Uri]::new($absolutePagePath)
        $pagePath = [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pageUri).ToString())
        $impactedWikiPages.Add([pscustomobject]@{
            path = $pagePath
            changedSources = $changedSources
        })
    }
}
foreach ($module in $matchedModules) {
    if (-not @($impactedWikiPages | Where-Object { $_.path -eq $module.wikiPage }).Count) {
        $impactedWikiPages.Add([pscustomobject]@{
            path = $module.wikiPage
            changedSources = @($module.changedPaths)
        })
    }
}

$focusedTests = [System.Collections.Generic.List[string]]::new()
foreach ($module in $matchedModules) {
    $escapedName = [regex]::Escape($module.name)
    $candidateTests = @(
        Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.cs' |
            Where-Object {
                $_.FullName -match '[\\/]tests[\\/]' -and
                $_.FullName -notmatch '[\\/](obj|bin|\.artifacts|TestResults)[\\/]'
            } |
            ForEach-Object {
                $absolutePath = $_.FullName
                $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
                $fileUri = [System.Uri]::new($absolutePath)
                [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($fileUri).ToString())
            } |
            Where-Object { $_ -match "/$escapedName(/|[^/]*Tests?\.cs$)" } |
            Sort-Object |
            Select-Object -First 8
    )
    foreach ($candidateTest in $candidateTests) {
        if (-not $focusedTests.Contains($candidateTest)) {
            $focusedTests.Add($candidateTest)
        }
    }
}

$generatedActions = [System.Collections.Generic.List[string]]::new()
if (@($changedPaths | Where-Object {
    $_ -match '\.csproj$|Controller\.cs$|(^|/)AGENTS\.md$|^docs/.+\.md$|FoodDiary\.Web\.Client/angular\.json$|docs/architecture/module-dependencies\.json$'
}).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiCatalog.ps1')
}
if (@($changedPaths | Where-Object {
    $_ -match '\.cs$' -and $_ -notmatch '(^|/)tests/|/Migrations?/'
}).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1')
}
if (@($changedPaths | Where-Object {
    $_ -match '^FoodDiary\.Web\.Client/.+\.(ts|json)$' -or
    $_ -eq 'FoodDiary.Web.Client/angular.json'
}).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1')
}
if ($scopes.Configuration) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiConfigurationIndex.ps1')
}
if (@($changedPaths | Where-Object {
    $_ -match '\.(cs|ts)$' -and $_ -notmatch '(^|/)tests/|/Migrations?/|\.(spec|test)\.ts$'
}).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiQualityIndex.ps1')
}
if (@($changedPaths | Where-Object {
    $_ -eq 'docker-compose.yml' -or
    ($_ -match '\.cs$' -and $_ -match 'Client|Gateway|Transport|HostedService|Job|Consumer|Publisher|Webhook|DependencyInjection')
}).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiRuntimeTopology.ps1')
}
if (@($changedPaths | Where-Object {
    $_ -match '\.cs$' -and $_ -notmatch '(^|/)tests/|/Migrations?/'
}).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiSensitiveDataIndex.ps1')
}
if ($matchedModules.Count -gt 0 -or
    $changedPathSet.ContainsKey('docs/architecture/module-dependencies.json') -or
    @($changedPaths | Where-Object { $_ -match 'Controller\.cs$' }).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiModulePages.ps1')
}

$warnings = [System.Collections.Generic.List[string]]::new()
if ($scopes.Contracts) {
    $warnings.Add('Swagger-visible route, payload, or status changes may require API contract snapshot updates.')
}
if ($scopes.Localization) {
    $warnings.Add('Keep English and Russian locale files aligned and verify Cyrillic rendering.')
}
if ($scopes.Database) {
    $warnings.Add('Commit migration and Designer files together; format generated migration code.')
}
if ($scopes.Configuration) {
    $warnings.Add('Synchronize option validation, appsettings templates, environment examples, deployment values, and secret handling.')
}
if ($scopes.Deployment) {
    $warnings.Add('Record deployment ordering, mixed-version compatibility, post-deploy verification, and rollback or roll-forward strategy.')
}
if ($matchedModules.Count -gt 1) {
    $warnings.Add('Multiple business modules are affected; verify ownership and cross-module mutation boundaries.')
}

$recommendedChecks = [System.Collections.Generic.List[string]]::new()
if ($scopes.Backend) {
    $recommendedChecks.Add('dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj')
}
foreach ($project in $matchedProjects | Where-Object { $_.isTestProject }) {
    $recommendedChecks.Add("dotnet test $($project.path)")
}
if ($scopes.Api) {
    $recommendedChecks.Add('dotnet test tests/FoodDiary.Presentation.Api.Tests/FoodDiary.Presentation.Api.Tests.csproj')
    $recommendedChecks.Add('dotnet test tests/FoodDiary.Web.Api.IntegrationTests/FoodDiary.Web.Api.IntegrationTests.csproj')
}
if ($scopes.Frontend) {
    $recommendedChecks.Add('cd FoodDiary.Web.Client && npm run verify')
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiFrontendContractIndex.ps1')
}
if ($scopes.Localization) {
    $recommendedChecks.Add('cd FoodDiary.Web.Client && npm run check:i18n')
}
if ($scopes.Database) {
    $recommendedChecks.Add('dotnet test tests/FoodDiary.Infrastructure.IntegrationTests/FoodDiary.Infrastructure.IntegrationTests.csproj')
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiDomainDataIndex.ps1')
}
if ($scopes.Backend -and @($changedPaths | Where-Object { $_ -match '(^|/)(FoodDiary\.Domain|FoodDiary\.MailInbox\.Domain|FoodDiary\.MailRelay\.Domain)/' }).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiDomainDataIndex.ps1')
}
if ($scopes.Backend -and @($changedPaths | Where-Object {
    $_ -match '\.cs$' -and (
        $_ -match 'Application\.Abstractions/' -or
        $_ -match '/(Requests|Responses|Commands|Queries|Events)/' -or
        $_ -match '\.(Client|Domain\.Primitives)/'
    )
}).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiBackendContractIndex.ps1')
}
if (@($changedPaths | Where-Object { $_ -match '/Persistence/Configurations/.+Configuration\.cs$|DbContext\.cs$' }).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiDomainDataIndex.ps1')
}
if (@($changedPaths | Where-Object { $_ -match '\.csproj$|^docs/architecture/module-dependencies\.json$' }).Count -gt 0) {
    $generatedActions.Add('./.llm-wiki/tools/Build-LlmWikiArchitectureHealthIndex.ps1')
}
$uniqueChecks = @($recommendedChecks | Sort-Object -Unique)

$result = [ordered]@{
    changedPaths = $changedPaths
    sourceChangedPaths = $changedPaths
    derivedWikiPaths = $derivedWikiPaths
    reviewMetadataPaths = $reviewMetadataPaths
    renames = @($renames)
    baselineExcludedPaths = $baselineExcludedPaths
    workspaceContextScopes = @($workspaceContextScopes | Sort-Object -Unique)
    scopes = $activeScopes
    modules = @($matchedModules)
    projects = @($matchedProjects)
    changedSymbols = $changedSymbols
    changedFrontendSymbols = $changedFrontendSymbols
    agentGuides = $applicableGuides
    wikiPages = @($impactedWikiPages | Sort-Object path)
    focusedTests = @($focusedTests | Select-Object -First ($Limit * 2))
    generatedActions = @($generatedActions)
    warnings = @($warnings)
    recommendedChecks = $uniqueChecks
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 10
    return
}

Write-Host "LLM Wiki diff context: $($changedPaths.Count) changed path(s)"
Write-Host "Scopes: $(if ($activeScopes.Count) { $activeScopes -join ', ' } else { 'none detected' })"
if ($baselineExcludedPaths.Count -gt 0) {
    Write-Host "Workspace context excluded by task baseline: $($baselineExcludedPaths.Count) path(s); scopes=$(if ($workspaceContextScopes.Count) { @($workspaceContextScopes | Sort-Object -Unique) -join ', ' } else { 'none detected' })."
    Write-Host 'Use -ChangedPath explicitly to include any of those paths in the current task; they are shown but not silently claimed from another session.'
}

function Write-Section {
    param(
        [string]$Title,
        [object[]]$Items,
        [scriptblock]$Formatter
    )
    if ($Items.Count -eq 0) {
        return
    }
    Write-Host ''
    Write-Host "${Title}:"
    foreach ($item in $Items) {
        Write-Host " - $(& $Formatter $item)"
    }
}

Write-Section 'Modules' @($result.modules) {
    param($item)
    $dependencies = if ($item.dependencies.Count) { $item.dependencies -join ', ' } else { 'none' }
    "$($item.name) ($($item.changedPaths.Count) changed path(s); depends on: $dependencies)"
}
Write-Section 'Projects' @($result.projects) { param($item) "$($item.path) ($($item.changedFiles) files)" }
Write-Section 'Changed C# symbols' @($result.changedSymbols) {
    param($item)
    "$($item.name) [$($item.role)] - $($item.path):$($item.line)"
}
Write-Section 'Changed frontend symbols' @($result.changedFrontendSymbols) {
    param($item)
    "$($item.name) [$($item.role)] - $($item.path):$($item.line)"
}
Write-Section 'Applicable guides' @($result.agentGuides) { param($item) $item }
Write-Section 'Wiki pages to review' @($result.wikiPages) { param($item) $item.path }
Write-Section 'Focused tests' @($result.focusedTests) { param($item) $item }
Write-Section 'Generated artifacts to refresh' @($result.generatedActions) { param($item) $item }
Write-Section 'Warnings' @($result.warnings) { param($item) $item }
Write-Section 'Recommended checks' @($result.recommendedChecks) { param($item) $item }
