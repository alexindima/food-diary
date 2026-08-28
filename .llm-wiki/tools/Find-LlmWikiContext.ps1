[CmdletBinding()]
param(
    [string]$Module,
    [string]$Query,
    [Alias('PlannedPath', 'ProposedPath')]
    [string[]]$ScopePath,
    [ValidateSet('Any', 'Api', 'Backend', 'Frontend', 'Database', 'Tests')]
    [string]$ChangeType = 'Any',
    [switch]$SqlShadow,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [switch]$SkipQueryCache,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$catalogPath = Join-Path $wikiRoot 'generated/repository-catalog.json'
$symbolIndexPath = Join-Path $wikiRoot 'generated/csharp-symbol-index.json'
$frontendIndexPath = Join-Path $wikiRoot 'generated/frontend-index.json'
. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiContextScoring.ps1')

if ([string]::IsNullOrWhiteSpace($Module) -and [string]::IsNullOrWhiteSpace($Query)) {
    throw 'Provide -Module, -Query, or both.'
}
if ($SqlShadow) {
    # Shadow mode intentionally keeps the legacy JSON projection authoritative
    # while comparing it with the read-only SQLite search result.
    $CompiledIndexSource = 'Json'
}
if ($CompiledIndexSource -eq 'Json' -and -not (Test-Path -LiteralPath $catalogPath)) {
    throw 'Repository catalog is missing. Run Build-LlmWikiCatalog.ps1 first.'
}

$queryCacheEntry = $null
if ($Format -eq 'Json' -and $CompiledIndexSource -eq 'Json' -and -not $SqlShadow -and -not $SkipQueryCache) {
    $cacheRelevantPaths = @(
        @($ScopePath) + $(if (-not [string]::IsNullOrWhiteSpace($Module)) {
            @("FoodDiary.Application/$Module", "FoodDiary.Application.$Module")
        }) | Where-Object { $_ } | Sort-Object -Unique
    )
    $compiledIndexDependencies = if ($CompiledIndexSource -eq 'Json') {
        @(
            '.llm-wiki/generated/repository-catalog.json'
            '.llm-wiki/generated/csharp-symbol-index.json'
            '.llm-wiki/generated/frontend-index.json'
        )
    } else {
        @('.artifacts/llm-wiki/code-graph/code-graph.fingerprint')
    }
    $queryCacheEntry = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repositoryRoot -Namespace 'context' -Arguments @{
        Module = $Module
        Query = $Query
        ScopePath = @($ScopePath)
        ChangeType = $ChangeType
        CompiledIndexSource = $CompiledIndexSource
        Limit = $Limit
    } -RelevantPath $cacheRelevantPaths -DependencyPath $compiledIndexDependencies
    $cachedContext = Read-LlmWikiQueryCache -Entry $queryCacheEntry
    if ($null -ne $cachedContext) {
        Write-Output $cachedContext
        return
    }
}

$scopePaths = @(
    $ScopePath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_ -split '[;,]' } |
        ForEach-Object { $_.Trim().Replace('\', '/') } |
        Where-Object { $_.Length -gt 0 } |
        Sort-Object -Unique
)
$searchText = (@($Module, $Query) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' '
$tokens = @(
    [regex]::Matches($searchText.ToLowerInvariant(), '[\p{L}\p{N}]+') |
        ForEach-Object { $_.Value } |
        Where-Object { $_.Length -ge 2 } |
        Sort-Object -Unique
)
$searchPhrases = @($Module, $Query) |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    ForEach-Object { $_.ToLowerInvariant() }
$searchNeedsCamelCaseExpansion = @($tokens | Where-Object { $_.Length -lt 4 }).Count -gt 0 -or
    @($searchPhrases | Where-Object { $_ -match '\s' }).Count -gt 0
$frontendOnlyScope = $ChangeType -eq 'Frontend' -and
    ($scopePaths.Count -eq 0 -or @($scopePaths | Where-Object { $_ -notmatch '^FoodDiary\.Web\.Client/' }).Count -eq 0)
$compiledIndexStopwatch = [Diagnostics.Stopwatch]::StartNew()
$compiledIndexDiagnostics = $null
function Test-ContextScopeMatch([string]$Path, [string]$ScopePath) {
    $normalizedPath = $Path.Replace('\', '/').TrimEnd('/').ToLowerInvariant()
    $normalizedScope = $ScopePath.Replace('\', '/').TrimEnd('/').ToLowerInvariant()
    return $normalizedPath -eq $normalizedScope -or
        $normalizedPath.StartsWith("$normalizedScope/", [StringComparison]::Ordinal) -or
        $normalizedScope.StartsWith("$normalizedPath/", [StringComparison]::Ordinal)
}
function Select-ContextRecordsWithScopeCoverage([object[]]$Records, [string[]]$Scopes, [int]$Maximum) {
    $selected = [Collections.Generic.List[object]]::new()
    foreach ($record in @($Records | Select-Object -First $Maximum)) { $selected.Add($record) }
    if ($selected.Count -eq 0 -or @($Scopes).Count -eq 0 -or $Maximum -lt @($Scopes).Count) { return @($selected) }
    foreach ($scope in @($Scopes)) {
        if (@($selected | Where-Object { Test-ContextScopeMatch ([string]$_.path) $scope }).Count -gt 0) { continue }
        $scopedCandidate = $Records | Where-Object { Test-ContextScopeMatch ([string]$_.path) $scope } | Select-Object -First 1
        if ($null -eq $scopedCandidate) { continue }
        $replacementIndex = -1
        for ($index = $selected.Count - 1; $index -ge 0; $index--) {
            $current = $selected[$index]
            $isOnlyRepresentative = $false
            foreach ($candidateScope in @($Scopes)) {
                if ((Test-ContextScopeMatch ([string]$current.path) $candidateScope) -and
                    @($selected | Where-Object { Test-ContextScopeMatch ([string]$_.path) $candidateScope }).Count -eq 1) {
                    $isOnlyRepresentative = $true
                    break
                }
            }
            if (-not $isOnlyRepresentative) { $replacementIndex = $index; break }
        }
        if ($replacementIndex -ge 0) {
            $selected[$replacementIndex] = $scopedCandidate
        }
    }
    return @($selected | Sort-Object rank, path)
}
if ($CompiledIndexSource -eq 'Sqlite') {
    $graphManager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
    $graphStatus = & $graphManager `
        -Action status `
        -SkipRefresh `
        -Format Json | ConvertFrom-Json
    $requiresTypeScriptProjection = $ChangeType -in @('Any', 'Frontend')
    $typescriptProjectionComplete = $null -ne $graphStatus.typescriptProjectionComplete -and [bool]$graphStatus.typescriptProjectionComplete
    if (-not [bool]$graphStatus.changeSetFresh -or
        [int]$graphStatus.searchDocuments -eq 0 -or
        ($requiresTypeScriptProjection -and -not $typescriptProjectionComplete)) {
        $backendOnlyRefresh = $ChangeType -in @('Api', 'Backend', 'Database', 'Tests')
        try {
            & $graphManager -Action build -BackendOnlyRefresh:$backendOnlyRefresh -Format Json | Out-Null
        } catch {
            throw "SQLite context projection could not be prepared. $($_.Exception.Message) Use explicit -CompiledIndexSource Json only when a read-only baseline is acceptable."
        }
        $graphStatus = & $graphManager -Action status -SkipRefresh -Format Json | ConvertFrom-Json
    }
    $indexFresh = [bool]$graphStatus.changeSetFresh
    $searchLimit = [Math]::Min(100, [Math]::Max(50, $Limit * 4))
    $sqlResult = & $graphManager `
        -Action search `
        -Query $searchText `
        -Module $Module `
        -ChangedPath $scopePaths `
        -ChangeType $ChangeType `
        -Limit $searchLimit `
        -SkipRefresh `
        -Format Json | ConvertFrom-Json
    if (-not [bool]$sqlResult.ready) {
        throw 'SQLite search index is unavailable. Run ./.llm-wiki/wiki.ps1 graph-build and retry.'
    }
    $compiledIndexStopwatch.Stop()
    $records = @($sqlResult.records)
    $top = @($records | Select-Object -First 1)
    $ranking = $sqlResult.rankingSummary
    $confidence = if ($null -eq $ranking) { 'low' } else { [string]$ranking.confidence }
    $ambiguous = if ($null -eq $ranking) { $true } else { [bool]$ranking.ambiguous }
    $conclusive = $indexFresh -and $records.Count -gt 0 -and $confidence -in @('high', 'medium') -and -not $ambiguous
    $toItem = { [pscustomobject][ordered]@{
        path = [string]$_.path
        score = [double]$_.score
        rank = [int]$_.rank
        confidence = [string]$_.confidence
        reasons = @($_.reasons)
    } }
    $selectionScopes = @($scopePaths)
    if (-not [string]::IsNullOrWhiteSpace($Module)) {
        $moduleImplementationScope = "FoodDiary.Application.$Module"
        if (Test-Path -LiteralPath (Join-Path $repositoryRoot $moduleImplementationScope) -PathType Container) {
            $selectionScopes += $moduleImplementationScope
        }
    }
    $visibleRecords = @(Select-ContextRecordsWithScopeCoverage $records $selectionScopes $Limit)
    $testRecords = @($records | Where-Object { [bool]$_.isTest } | Select-Object -First $Limit)
    $wikiRecords = @($records | Where-Object { $_.path -match '^(\.llm-wiki/|docs/).+\.md$' } | Select-Object -First $Limit)
    $guideRecords = @($records | Where-Object { $_.path -match '(^|/)AGENTS\.md$' } | Select-Object -First $Limit)
    $implementationRecordPool = @($records | Where-Object {
        -not [bool]$_.isTest -and $_.path -notmatch '^(\.llm-wiki/|docs/)' -and $_.path -notmatch '(^|/)AGENTS\.md$'
    })
    $implementationRecords = @(Select-ContextRecordsWithScopeCoverage $implementationRecordPool $selectionScopes $Limit)
    $frontendRecordPool = @($implementationRecordPool | Where-Object { $_.path -match '^FoodDiary\.Web\.Client/' })
    $frontendRecords = @(Select-ContextRecordsWithScopeCoverage $frontendRecordPool $scopePaths $Limit)
    $symbolRecords = @($implementationRecords | Where-Object { $_.recordType -eq 'code' -and $_.role -ne 'other' })
    $modulePagePath = $null
    $explicitModulePage = if (-not [string]::IsNullOrWhiteSpace($Module)) {
        $moduleSlug = [regex]::Replace($Module, '([a-z0-9])([A-Z])', '$1-$2').ToLowerInvariant()
        $modulePagePath = ".llm-wiki/generated/modules/$moduleSlug.md"
        if (Test-Path -LiteralPath (Join-Path $repositoryRoot $modulePagePath) -PathType Leaf) {
            [pscustomobject][ordered]@{
                path = $modulePagePath
                score = 1000
                rank = 0
                confidence = 'high'
                reasons = @("explicit module $Module")
            }
        }
    }
    $context = [ordered]@{
        query = [ordered]@{ module = $Module; text = $Query; changeType = $ChangeType; scopePaths = $scopePaths }
        module = $(if (-not [string]::IsNullOrWhiteSpace($Module)) {
            [pscustomobject][ordered]@{ name = $Module; dependencies = @(); consumers = @(); origin = 'explicit-module' }
        } elseif ($top.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace([string]$top[0].module)) {
            [pscustomobject][ordered]@{ name = [string]$top[0].module; dependencies = @(); consumers = @(); origin = 'sqlite-search' }
        } else { $null })
        confidence = $confidence
        conclusive = $conclusive
        abstained = -not $conclusive
        ambiguityReason = $(if (-not $indexFresh) { 'stale-index' } elseif ($records.Count -eq 0) { 'no-indexed-candidates' } elseif ($ambiguous) { [string]$ranking.ambiguityReason } elseif (-not $conclusive) { 'low-confidence' } else { $null })
        candidates = @($visibleRecords)
        wikiPages = @($explicitModulePage) + @($wikiRecords | Where-Object path -ne $modulePagePath | ForEach-Object $toItem)
        agentGuides = @($guideRecords | ForEach-Object $toItem)
        projects = @()
        frontendProjects = @()
        frontendFeatures = @()
        frontendSymbols = @($frontendRecords | ForEach-Object $toItem)
        frontendRoutes = @()
        implementationFiles = @($implementationRecords | ForEach-Object $toItem)
        localization = @()
        controllers = @($implementationRecords | Where-Object role -eq 'controller' | ForEach-Object $toItem)
        symbols = @($symbolRecords | ForEach-Object $toItem)
        dependencyInjection = @()
        tests = @($testRecords | ForEach-Object $toItem)
        recommendedChecks = @(
            $(if ($ChangeType -eq 'Frontend') { 'cd FoodDiary.Web.Client && npm run verify' } else { 'Run focused tests for the highest-ranked current-source candidates.' })
            'Verify inferred paths in current code before editing.'
        )
        compiledIndex = [ordered]@{
            source = 'sqlite-search'
            fingerprint = $sqlResult.fingerprint
            updatedAtUtc = $sqlResult.updatedAtUtc
            indexedDocuments = [int]$sqlResult.indexedDocuments
            returnedRecords = $records.Count
            sqlDurationMs = [double]$sqlResult.durationMs
            roundTripDurationMs = [Math]::Round($compiledIndexStopwatch.Elapsed.TotalMilliseconds, 2)
            fresh = $indexFresh
            indexedChangeSetFingerprint = [string]$graphStatus.changeSetFingerprint
            currentChangeSetFingerprint = [string]$graphStatus.currentChangeSetFingerprint
        }
    }
    $contextJson = $context | ConvertTo-Json -Depth 12
    if ($Format -eq 'Json') {
        if ($null -ne $queryCacheEntry) { Write-LlmWikiQueryCache -Entry $queryCacheEntry -Content $contextJson }
        Write-Output $contextJson
        return
    }
    Write-Host "LLM Wiki context: '$searchText' [$ChangeType]"
    Write-Host "Confidence: $confidence; conclusive=$conclusive; candidates=$($records.Count); round-trip=$($context.compiledIndex.roundTripDurationMs)ms."
    if (-not $conclusive) { Write-Host "Abstained: $($context.ambiguityReason). Inspect candidates or narrow the query." }
    foreach ($record in @($records | Select-Object -First $Limit)) { Write-Host " - #$($record.rank) [$($record.confidence)] $($record.path) score=$($record.score)" }
    return

    $compiledResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
        -Action compiled-context `
        -Query $Query `
        -Module $Module `
        -ChangedPath $scopePaths `
        -SkipRefresh `
        -Format Json | ConvertFrom-Json
    if (-not [bool]$compiledResult.ready) {
        throw "SQLite compiled-index projection is unavailable ($($compiledResult.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }
    $catalog = $compiledResult.catalog
    $symbolIndex = [pscustomobject]@{
        symbols = @($compiledResult.symbols)
        dependencyInjectionRegistrations = @($compiledResult.dependencyInjectionRegistrations)
    }
    $frontendIndex = [pscustomobject]@{
        features = @($compiledResult.frontendFeatures)
        symbols = @($compiledResult.frontendSymbols)
        routes = @($compiledResult.frontendRoutes)
        localization = @($compiledResult.frontendLocalization)
    }
    $compiledIndexDiagnostics = [ordered]@{
        source = [string]$compiledResult.source
        sqlDurationMs = [double]$compiledResult.durationMs
        scannedRecords = [int]$compiledResult.scannedRecords
        returnedRecords = [int]$compiledResult.returnedRecords
        sourceHashes = $compiledResult.sourceHashes
    }
} else {
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
    $jsonRecordCount = $(if ($null -eq $symbolIndex) { 0 } else { @($symbolIndex.symbols).Count + @($symbolIndex.dependencyInjectionRegistrations).Count }) +
        $(if ($null -eq $frontendIndex) { 0 } else { @($frontendIndex.features).Count + @($frontendIndex.symbols).Count + @($frontendIndex.routes).Count + @($frontendIndex.localization).Count })
    $compiledIndexDiagnostics = [ordered]@{
        source = 'json-baseline'
        sqlDurationMs = $null
        scannedRecords = $jsonRecordCount
        returnedRecords = $jsonRecordCount
        sourceHashes = $null
    }
}
$compiledIndexStopwatch.Stop()
$compiledIndexDiagnostics['roundTripDurationMs'] = [Math]::Round($compiledIndexStopwatch.Elapsed.TotalMilliseconds, 2)
$matchedModule = $null
if (-not [string]::IsNullOrWhiteSpace($Module)) {
    $matchedModule = @(
        $catalog.applicationModules |
            Where-Object { $_.name -ieq $Module } |
            Select-Object -First 1
    )
    if ($matchedModule.Count -eq 0) {
        $matchedModule = @(
            $catalog.applicationModules |
                Where-Object { $_.name -like "*$Module*" } |
                Select-Object -First 1
        )
    }
    if ($matchedModule.Count -gt 0) {
        $matchedModule = $matchedModule[0]
    } else {
        $extractedMatch = @(
            $catalog.extractedApplicationModules |
                Where-Object { $_.name -ieq $Module -or $_.name -like "*$Module*" } |
                Select-Object -First 1
        )
        if ($extractedMatch.Count -gt 0) {
            $matchedModule = [pscustomobject]@{
                name = $extractedMatch[0].name
                dependencies = @()
                origin = 'extracted-project'
                project = $extractedMatch[0].project
            }
        } else {
            $matchedModule = $null
        }
    }
}

$moduleContext = $null
if ($null -ne $matchedModule) {
    $consumers = @(
        $catalog.applicationModules |
            Where-Object { @($_.dependencies) -contains $matchedModule.name } |
            ForEach-Object { $_.name } |
            Sort-Object
    )
    $moduleContext = [ordered]@{
        name = $matchedModule.name
        origin = if ($matchedModule.PSObject.Properties['origin'] -and -not [string]::IsNullOrWhiteSpace([string]$matchedModule.origin)) { [string]$matchedModule.origin } else { 'module-graph' }
        project = if ($matchedModule.PSObject.Properties['project']) { $matchedModule.project } else { $null }
        dependencies = @(if ($matchedModule.PSObject.Properties['dependencies']) { @($matchedModule.dependencies) })
        consumers = $consumers
    }
}

$wikiCandidates = [System.Collections.Generic.List[object]]::new()
$wikiPages = Get-ChildItem -LiteralPath $wikiRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.FullName -ne (Join-Path $wikiRoot 'README.md') }
foreach ($page in $wikiPages) {
    $content = Get-Content -LiteralPath $page.FullName -Raw
    $path = ConvertTo-RepositoryPath $page.FullName
    $score = (Get-SearchScore $path $tokens 8 16) + (Get-SearchScore $content $tokens 2 6)
    if ($path -eq '.llm-wiki/index.md') {
        $score += 1
    }
    $wikiCandidates.Add([pscustomobject]@{ path = $path; score = $score })
}
$wikiResults = Select-ScoredItems $wikiCandidates 6

$projectCandidates = [System.Collections.Generic.List[object]]::new()
$changeTypeContextProjects = @{
    Api = @(
        'FoodDiary.Presentation.Api'
        'FoodDiary.Web.Api'
        'FoodDiary.Presentation.Api.Tests'
        'FoodDiary.Web.Api.IntegrationTests'
    )
    Backend = @(
        'FoodDiary.Application'
        'FoodDiary.Application.Abstractions'
        'FoodDiary.Domain'
        'FoodDiary.Infrastructure'
        'FoodDiary.ArchitectureTests'
    )
    Frontend = @()
    Database = @(
        'FoodDiary.Infrastructure'
        'FoodDiary.Initializer'
        'FoodDiary.Infrastructure.Tests'
        'FoodDiary.Infrastructure.IntegrationTests'
    )
    Tests = @('FoodDiary.ArchitectureTests')
    Any = @()
}
foreach ($project in $catalog.dotnet.projects) {
    $searchable = "$($project.name) $($project.path)"
    $score = Get-SearchScore $searchable $tokens 10 20
    if ($frontendOnlyScope) { $score = 0 } else { $score += Get-ScopeAffinity $project.path }
    if (@($changeTypeContextProjects[$ChangeType]) -contains $project.name) {
        $score += 3
    }
    $projectCandidates.Add([pscustomobject]@{
        name = $project.name
        path = $project.path
        isTestProject = [bool]$project.isTestProject
        score = $score
    })
}
$projectResults = Select-ScoredItems $projectCandidates

$frontendProjectCandidates = [System.Collections.Generic.List[object]]::new()
foreach ($frontendProject in $catalog.frontend.projects) {
    $searchable = "$($frontendProject.name) $($frontendProject.root) $($frontendProject.sourceRoot)"
    $score = Get-SearchScore $searchable $tokens 10 20
    $score += Get-ScopeAffinity "$($frontendProject.sourceRoot)"
    if ($ChangeType -eq 'Frontend') {
        $score += 3
    }
    $frontendProjectCandidates.Add([pscustomobject]@{
        name = $frontendProject.name
        projectType = $frontendProject.projectType
        root = $frontendProject.root
        sourceRoot = $frontendProject.sourceRoot
        score = $score
    })
}
$frontendProjectResults = Select-ScoredItems $frontendProjectCandidates

$frontendFeatureResults = @()
$frontendSymbolResults = @()
$frontendRouteResults = @()
$implementationFileResults = @()
$localizationResults = @()
if ($null -ne $frontendIndex) {
    $frontendFeatureCandidates = [System.Collections.Generic.List[object]]::new()
    foreach ($feature in $frontendIndex.features) {
        $score = Get-SearchScore "$($feature.area) $($feature.name) $($feature.root)" $tokens 10 20
        $score += Get-ScopeAffinity $feature.root
        if ($score -gt 0) {
            $frontendFeatureCandidates.Add([pscustomobject]@{
                area = $feature.area
                name = $feature.name
                root = $feature.root
                routes = @($feature.routes)
                tests = @($feature.tests)
                score = $score
            })
        }
    }
    $frontendFeatureResults = Select-ScoredItems $frontendFeatureCandidates

    $frontendSymbolCandidates = [System.Collections.Generic.List[object]]::new()
    foreach ($frontendSymbol in $frontendIndex.symbols) {
        $score = Get-SearchScore "$($frontendSymbol.name) $($frontendSymbol.role) $($frontendSymbol.selector) $($frontendSymbol.path)" $tokens 8 18
        $score += Get-ScopeAffinity $frontendSymbol.path
        if ($score -gt 0) {
            $frontendSymbolCandidates.Add([pscustomobject]@{
                name = $frontendSymbol.name
                role = $frontendSymbol.role
                selector = $frontendSymbol.selector
                path = $frontendSymbol.path
                line = $frontendSymbol.line
                score = $score
            })
        }
    }
    $frontendSymbolResults = Select-ScoredItems $frontendSymbolCandidates

    $frontendRouteCandidates = [System.Collections.Generic.List[object]]::new()
    foreach ($frontendRoute in $frontendIndex.routes) {
        $score = Get-SearchScore "$($frontendRoute.path) $($frontendRoute.source)" $tokens 10 20
        $score += Get-ScopeAffinity $frontendRoute.source
        if ($score -gt 0) {
            $frontendRouteCandidates.Add([pscustomobject]@{
                route = $frontendRoute.path
                path = $frontendRoute.source
                line = $frontendRoute.line
                score = $score
            })
        }
    }
    $frontendRouteResults = Select-ScoredItems $frontendRouteCandidates

    $localizationCandidates = [System.Collections.Generic.List[object]]::new()
    foreach ($localeFile in $frontendIndex.localization) {
        $score = Get-SearchScore $localeFile.name $tokens 10 20
        if (@($tokens | Where-Object { $_ -in @('i18n', 'locale', 'localization', 'translation') }).Count -gt 0) {
            $score += 8
        }
        if ($score -gt 0) {
            $localizationCandidates.Add([pscustomobject]@{
                name = $localeFile.name
                englishProperties = $localeFile.englishProperties
                russianProperties = $localeFile.russianProperties
                countsMatch = $localeFile.countsMatch
                score = $score
            })
        }
    }
    $localizationResults = Select-ScoredItems $localizationCandidates

    $implementationFileCandidates = [System.Collections.Generic.List[object]]::new()
    $trackedFrontendFiles = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--', 'FoodDiary.Web.Client/**/*.ts', 'FoodDiary.Web.Client/**/*.html', 'FoodDiary.Web.Client/**/*.scss') -FailureMessage 'Unable to enumerate tracked frontend implementation files.')
    foreach ($implementationPath in $trackedFrontendFiles) {
        $scopeAffinity = Get-ScopeAffinity $implementationPath
        if ($scopePaths.Count -gt 0 -and $scopeAffinity -lt 0) {
            continue
        }
        $absoluteImplementationPath = Join-Path $repositoryRoot $implementationPath
        $pathScore = Get-SearchScore $implementationPath $tokens 14 30
        $contentScore = 0
        if (Test-Path -LiteralPath $absoluteImplementationPath -PathType Leaf) {
            $content = Get-Content -LiteralPath $absoluteImplementationPath -Raw
            $contentScore = [Math]::Min((Get-SearchScore $content $tokens 2 8), 16)
        }
        $score = $pathScore + $contentScore + $scopeAffinity
        if ($score -gt 0) {
            $implementationFileCandidates.Add([pscustomobject]@{
                path = $implementationPath
                score = $score
                match = if ($pathScore -gt 0 -and $contentScore -gt 0) {
                    'path-and-content'
                } elseif ($pathScore -gt 0) {
                    'path'
                } else {
                    'content'
                }
                provenance = 'tracked-source'
            })
        }
    }
    $implementationFileResults = Select-ScoredItems $implementationFileCandidates
}

$controllerCandidates = [System.Collections.Generic.List[object]]::new()
foreach ($controller in $catalog.http.controllers) {
    $endpointRoutes = @($controller.endpoints | ForEach-Object { "$($_.verb) $($_.route)" })
    $searchable = "$($controller.name) $($controller.path) $($controller.routePrefix) $($endpointRoutes -join ' ')"
    $score = Get-SearchScore $searchable $tokens 10 20
    if ($frontendOnlyScope) { $score = 0 } else { $score += Get-ScopeAffinity $controller.path }
    if ($score -gt 0 -and $ChangeType -eq 'Api') {
        $score += 2
    }
    if ($score -gt 0) {
        $controllerCandidates.Add([pscustomobject]@{
            name = $controller.name
            path = $controller.path
            routePrefix = $controller.routePrefix
            endpoints = @($controller.endpoints)
            score = $score
        })
    }
}
$controllerResults = Select-ScoredItems $controllerCandidates

$symbolResults = @()
$registrationResults = @()
if ($null -ne $symbolIndex) {
    $symbolCandidates = [System.Collections.Generic.List[object]]::new()
    foreach ($symbol in $symbolIndex.symbols) {
        $searchable = "$($symbol.name) $($symbol.role) $($symbol.path)"
        $score = Get-SearchScore $searchable $tokens 8 18
        if ($frontendOnlyScope) { $score = 0 } else { $score += Get-ScopeAffinity $symbol.path }
        if ($null -ne $matchedModule) {
            $modulePathPattern = "/$([regex]::Escape([string]$matchedModule.name))/"
            if ([string]$symbol.path -match $modulePathPattern) {
                $score += 12
            }
            if ($matchedModule.PSObject.Properties['project'] -and -not [string]::IsNullOrWhiteSpace([string]$matchedModule.project)) {
                $moduleProjectDirectory = (Split-Path -Parent $matchedModule.project).Replace('\', '/')
                if ([string]$symbol.path -like "$moduleProjectDirectory/*") {
                    $score += 12
                }
            }
        }
        if ($score -gt 0) {
            $symbolCandidates.Add([pscustomobject]@{
                name = $symbol.name
                kind = $symbol.kind
                role = $symbol.role
                path = $symbol.path
                line = $symbol.line
                score = $score
            })
        }
    }
    $symbolResults = Select-ScoredItems $symbolCandidates

    $registrationCandidates = [System.Collections.Generic.List[object]]::new()
    foreach ($registration in $symbolIndex.dependencyInjectionRegistrations) {
        $searchable = "$($registration.service) $($registration.implementation) $($registration.path)"
        $score = Get-SearchScore $searchable $tokens 8 18
        if ($frontendOnlyScope) { $score = 0 } else { $score += Get-ScopeAffinity $registration.path }
        if ($null -ne $matchedModule -and
            [string]$registration.path -match "/$([regex]::Escape([string]$matchedModule.name))(/|\.)") {
            $score += 12
        }
        if ($score -gt 0) {
            $registrationCandidates.Add([pscustomobject]@{
                service = $registration.service
                implementation = $registration.implementation
                lifetime = $registration.lifetime
                path = $registration.path
                line = $registration.line
                score = $score
            })
        }
    }
    $registrationResults = Select-ScoredItems $registrationCandidates
}

$preferredGuidePaths = @{'AGENTS.md' = $true}
foreach ($projectResult in $projectResults) {
    $projectDirectory = Split-Path -Parent $projectResult.path
    while (-not [string]::IsNullOrWhiteSpace($projectDirectory)) {
        $candidateGuide = ($projectDirectory.TrimEnd('/', '\') + '/AGENTS.md').Replace('\', '/')
        if (@($catalog.knowledgeSources.agentGuides) -contains $candidateGuide) {
            $preferredGuidePaths[$candidateGuide] = $true
            break
        }
        $parentDirectory = Split-Path -Parent $projectDirectory
        if ($parentDirectory -eq $projectDirectory) {
            break
        }
        $projectDirectory = $parentDirectory
    }
}
foreach ($frontendProjectResult in $frontendProjectResults) {
    $frontendRoot = 'FoodDiary.Web.Client'
    if (-not [string]::IsNullOrWhiteSpace($frontendProjectResult.root)) {
        $frontendRoot += '/' + $frontendProjectResult.root.Trim('/')
    }
    $frontendGuide = $frontendRoot + '/AGENTS.md'
    if (@($catalog.knowledgeSources.agentGuides) -contains $frontendGuide) {
        $preferredGuidePaths[$frontendGuide] = $true
    } elseif (@($catalog.knowledgeSources.agentGuides) -contains 'FoodDiary.Web.Client/AGENTS.md') {
        $preferredGuidePaths['FoodDiary.Web.Client/AGENTS.md'] = $true
    }
}

$guideCandidates = [System.Collections.Generic.List[object]]::new()
foreach ($guidePath in $catalog.knowledgeSources.agentGuides) {
    $absolutePath = Join-Path $repositoryRoot $guidePath
    $content = Get-Content -LiteralPath $absolutePath -Raw
    $pathScore = Get-SearchScore $guidePath $tokens 10 20
    $contentScore = Get-SearchScore $content $tokens 1 4
    $score = $pathScore
    if ($pathScore -gt 0 -or $preferredGuidePaths.ContainsKey($guidePath)) {
        $score += $contentScore
    }
    if ($preferredGuidePaths.ContainsKey($guidePath)) {
        $score += 6
    }
    $guideCandidates.Add([pscustomobject]@{ path = $guidePath; score = $score })
}
$guideResults = Select-ScoredItems $guideCandidates 8

$testCandidates = [System.Collections.Generic.List[object]]::new()
$testRoots = if ($ChangeType -eq 'Frontend') {
    @()
} else {
    @(
        Join-Path $repositoryRoot 'tests'
        Join-Path $repositoryRoot 'MailRelay/tests'
        Join-Path $repositoryRoot 'MailInbox/tests'
    )
}
$existingTestRoots = @($testRoots | Where-Object { Test-Path -LiteralPath $_ -PathType Container })
$testSearchStopwords = @(
    'add', 'assess', 'assessment', 'audit', 'benchmark', 'change', 'changes', 'current',
    'fooddiary', 'implement', 'implementation', 'project', 'reliability', 'repository',
    'scope', 'scoped', 'service', 'test', 'tests'
)
$testSearchTokens = @(
    @($tokens) + @($scopePaths | ForEach-Object { [regex]::Matches($_, '[\p{L}\p{N}]+') | ForEach-Object { $_.Value.ToLowerInvariant() } }) |
        Where-Object { $_.Length -ge 3 -and $_ -notin $testSearchStopwords } |
        Sort-Object -Unique
)
$testFiles = @()
if ($existingTestRoots.Count -gt 0 -and $testSearchTokens.Count -gt 0) {
    $candidatePaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $trackedTestPaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--', 'tests/**/*.cs', 'MailRelay/tests/**/*.cs', 'MailInbox/tests/**/*.cs') -FailureMessage 'Unable to enumerate tracked backend test files.')
    foreach ($trackedTestPath in $trackedTestPaths) {
        $normalizedTestPath = $trackedTestPath.Replace('\', '/')
        if (@($testSearchTokens | Where-Object { $normalizedTestPath.IndexOf($_, [StringComparison]::OrdinalIgnoreCase) -ge 0 }).Count -gt 0) {
            $null = $candidatePaths.Add($normalizedTestPath)
        }
    }
    $ripgrep = Get-Command rg -ErrorAction SilentlyContinue
    if ($null -ne $ripgrep) {
        $testPattern = @($testSearchTokens | ForEach-Object { [regex]::Escape($_) }) -join '|'
        $contentMatches = @(& $ripgrep.Source --files-with-matches --ignore-case --glob '*.cs' --glob '!**/obj/**' --glob '!**/bin/**' -- $testPattern @existingTestRoots)
        if ($LASTEXITCODE -notin @(0, 1)) { throw 'Unable to search focused backend test candidates.' }
        foreach ($contentMatch in $contentMatches) {
            $null = $candidatePaths.Add((ConvertTo-RepositoryPath $contentMatch))
        }
    } else {
        foreach ($trackedTestPath in $trackedTestPaths) { $null = $candidatePaths.Add($trackedTestPath.Replace('\', '/')) }
    }
    $testFiles = @($candidatePaths | ForEach-Object { Get-Item -LiteralPath (Join-Path $repositoryRoot $_) -ErrorAction SilentlyContinue })
} elseif ($existingTestRoots.Count -gt 0) {
    $testFiles = @(
        foreach ($testRoot in $existingTestRoots) {
            Get-ChildItem -LiteralPath $testRoot -Recurse -File -Filter '*.cs' |
                Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' }
        }
    )
}
foreach ($testFile in $testFiles) {
    $path = ConvertTo-RepositoryPath $testFile.FullName
    $pathScore = Get-SearchScore $path $tokens 12 24
    $content = Get-Content -LiteralPath $testFile.FullName -Raw
    $contentScore = Get-SearchScore $content $tokens 1 3
    $score = $pathScore + [Math]::Min($contentScore, 8)
    $testCandidates.Add([pscustomobject]@{ path = $path; score = $score })
}
if ($ChangeType -eq 'Frontend' -and $null -ne $frontendIndex) {
    $frontendTestPaths = @(
        $frontendIndex.features |
            ForEach-Object { $_.tests } |
            Sort-Object -Unique
    )
    foreach ($frontendTestPath in $frontendTestPaths) {
        $score = Get-SearchScore $frontendTestPath $tokens 12 24
        $score += Get-ScopeAffinity $frontendTestPath
        if ($score -gt 0) {
            $testCandidates.Add([pscustomobject]@{ path = $frontendTestPath; score = $score })
        }
    }
}
$testResults = Select-ScoredItems $testCandidates

$sqlShadowDiagnostics = $null
if ($SqlShadow) {
    $legacyCandidates = @(
        @($frontendSymbolResults) + @($frontendRouteResults) + @($implementationFileResults) +
        @($controllerResults) + @($symbolResults) +
        @($registrationResults) + @($testResults)
    )
    $rankedLegacyPaths = [Collections.Generic.List[string]]::new()
    $seenLegacyPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($candidate in @($legacyCandidates | Sort-Object @{ Expression = { [double]$_.score }; Descending = $true })) {
        $candidatePath = @('path', 'root', 'sourceRoot') |
            ForEach-Object {
                $property = $candidate.PSObject.Properties[$_]
                if ($null -ne $property) { $property.Value }
            } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Select-Object -First 1
        if ($null -ne $candidatePath -and $seenLegacyPaths.Add(([string]$candidatePath).Replace('\', '/'))) {
            $rankedLegacyPaths.Add(([string]$candidatePath).Replace('\', '/'))
        }
        if ($rankedLegacyPaths.Count -ge $Limit) { break }
    }
    $shadowStopwatch = [Diagnostics.Stopwatch]::StartNew()
    try {
        $shadowArguments = @{
            Action = 'search'
            Query = $searchText
            ChangedPath = $scopePaths
            Module = $Module
            ChangeType = $ChangeType
            Limit = $Limit
            SkipRefresh = $true
            Format = 'Json'
        }
        $sqlResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') @shadowArguments | ConvertFrom-Json
        $sqlPaths = @($sqlResult.records.path | Where-Object { $_ } | ForEach-Object { ([string]$_).Replace('\', '/') } | Select-Object -Unique)
        $overlap = @($sqlPaths | Where-Object { $_ -in $rankedLegacyPaths })
        $legacyDenominator = [Math]::Max(1, [Math]::Min($Limit, $rankedLegacyPaths.Count))
        $sqlDenominator = [Math]::Max(1, [Math]::Min($Limit, $sqlPaths.Count))
        $sqlShadowDiagnostics = [ordered]@{
            authoritative = [string]$compiledIndexDiagnostics.source
            ready = [bool]$sqlResult.ready
            indexedDocuments = [int]$sqlResult.indexedDocuments
            fingerprint = $sqlResult.fingerprint
            queryTerms = @($sqlResult.queryTerms)
            legacyCandidateCount = $rankedLegacyPaths.Count
            sqlCandidateCount = $sqlPaths.Count
            overlapCount = $overlap.Count
            legacyRecallAtLimit = [Math]::Round($overlap.Count / $legacyDenominator, 4)
            sqlPrecisionAtLimit = [Math]::Round($overlap.Count / $sqlDenominator, 4)
            sqlQueryDurationMs = [double]$sqlResult.durationMs
            topCandidates = @($sqlResult.records)
        }
    } catch {
        $sqlShadowDiagnostics = [ordered]@{
            authoritative = 'json'
            ready = $false
            error = $_.Exception.Message
        }
    } finally {
        $shadowStopwatch.Stop()
        $sqlShadowDiagnostics.roundTripDurationMs = [Math]::Round($shadowStopwatch.Elapsed.TotalMilliseconds, 2)
    }
}

$recommendedChecks = switch ($ChangeType) {
    'Api' {
        @(
            'dotnet test tests/FoodDiary.Presentation.Api.Tests/FoodDiary.Presentation.Api.Tests.csproj'
            'dotnet test tests/FoodDiary.Web.Api.IntegrationTests/FoodDiary.Web.Api.IntegrationTests.csproj'
            'Update relevant API contract snapshots when the Swagger-visible surface changes.'
        )
    }
    'Backend' {
        @(
            'dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj'
            'Run the focused application/domain/infrastructure test project for the changed area.'
        )
    }
    'Frontend' {
        @(
            'cd FoodDiary.Web.Client && npm run verify'
            'Run npm run check:i18n when UI text changes.'
        )
    }
    'Database' {
        @(
            'dotnet test tests/FoodDiary.Infrastructure.IntegrationTests/FoodDiary.Infrastructure.IntegrationTests.csproj'
            'Run migration whitespace formatting and commit both migration files.'
        )
    }
    'Tests' {
        @('Run the focused test project and the architecture guardrails when boundaries are involved.')
    }
    default {
        @('Run the focused project checks plus architecture guardrails for cross-project changes.')
    }
}

$jsonCandidatePool = @(
    @($frontendSymbolResults) +
    @($frontendRouteResults) +
    @($implementationFileResults) +
    @($controllerResults) +
    @($symbolResults) +
    @($registrationResults) +
    @($testResults)
)
$jsonCandidates = @(
    $jsonCandidatePool |
        Where-Object { $_.PSObject.Properties['path'] -and -not [string]::IsNullOrWhiteSpace([string]$_.path) } |
        Group-Object path |
        ForEach-Object { $_.Group | Sort-Object @{ Expression = { [double]$_.score }; Descending = $true } | Select-Object -First 1 } |
        Sort-Object @{ Expression = { [double]$_.score }; Descending = $true }, path |
        Select-Object -First $Limit |
        ForEach-Object -Begin { $rank = 0 } -Process {
            $rank++
            $candidatePath = ([string]$_.path).Replace('\', '/')
            $candidateModule = if ($candidatePath -match '^FoodDiary\.Application\.([^/]+)/') { $Matches[1] }
                elseif ($candidatePath -match '^FoodDiary\.Web\.Client/') { 'Frontend' }
                elseif ($candidatePath -match '^tests/') { 'Tests' }
                else { $null }
            $candidateScore = [double]$_.score
            [pscustomobject][ordered]@{
                path = $candidatePath
                score = $candidateScore
                rank = $rank
                confidence = $(if ($candidateScore -ge 20) { 'high' } elseif ($candidateScore -ge 8) { 'medium' } else { 'low' })
                module = $candidateModule
                reasons = @('legacy-json-ranking')
            }
        }
)
$jsonTopConfidence = if ($jsonCandidates.Count -eq 0) { 'low' } else { [string]$jsonCandidates[0].confidence }
$jsonConclusive = $jsonCandidates.Count -gt 0 -and $jsonTopConfidence -in @('high', 'medium')

$context = [ordered]@{
    query = [ordered]@{
        module = $Module
        text = $Query
        changeType = $ChangeType
        scopePaths = $scopePaths
    }
    module = $moduleContext
    confidence = $jsonTopConfidence
    conclusive = $jsonConclusive
    abstained = -not $jsonConclusive
    ambiguityReason = $(if ($jsonCandidates.Count -eq 0) { 'no-indexed-candidates' } elseif (-not $jsonConclusive) { 'low-confidence' } else { $null })
    candidates = $jsonCandidates
    wikiPages = @($wikiResults | ForEach-Object { [ordered]@{ path = $_.path; score = $_.score } })
    agentGuides = @($guideResults | ForEach-Object { [ordered]@{ path = $_.path; score = $_.score } })
    projects = @($projectResults | ForEach-Object {
        [ordered]@{ name = $_.name; path = $_.path; isTestProject = $_.isTestProject; score = $_.score }
    })
    frontendProjects = @($frontendProjectResults | ForEach-Object {
        [ordered]@{
            name = $_.name
            projectType = $_.projectType
            root = $_.root
            sourceRoot = $_.sourceRoot
            score = $_.score
        }
    })
    frontendFeatures = @($frontendFeatureResults)
    frontendSymbols = @($frontendSymbolResults)
    frontendRoutes = @($frontendRouteResults)
    implementationFiles = @($implementationFileResults)
    localization = @($localizationResults)
    controllers = @($controllerResults | ForEach-Object {
        [ordered]@{
            name = $_.name
            path = $_.path
            routePrefix = $_.routePrefix
            endpoints = $_.endpoints
            score = $_.score
        }
    })
    symbols = @($symbolResults | ForEach-Object {
        [ordered]@{
            name = $_.name
            kind = $_.kind
            role = $_.role
            path = $_.path
            line = $_.line
            score = $_.score
        }
    })
    dependencyInjection = @($registrationResults | ForEach-Object {
        [ordered]@{
            service = $_.service
            implementation = $_.implementation
            lifetime = $_.lifetime
            path = $_.path
            line = $_.line
            score = $_.score
        }
    })
    tests = @($testResults | ForEach-Object { [ordered]@{ path = $_.path; score = $_.score } })
    recommendedChecks = $recommendedChecks
    compiledIndex = $compiledIndexDiagnostics
}
if ($null -ne $sqlShadowDiagnostics) { $context['sqlShadow'] = $sqlShadowDiagnostics }

if ($Format -eq 'Json') {
    $contextJson = $context | ConvertTo-Json -Depth 12
    if ($null -ne $queryCacheEntry) { Write-LlmWikiQueryCache -Entry $queryCacheEntry -Content $contextJson }
    Write-Output $contextJson
    return
}

Write-Host "LLM Wiki context: '$searchText' [$ChangeType]"
if ($null -ne $sqlShadowDiagnostics) {
    Write-Host "SQL shadow: ready=$($sqlShadowDiagnostics.ready), overlap=$($sqlShadowDiagnostics.overlapCount)/$($sqlShadowDiagnostics.legacyCandidateCount), SQL=$($sqlShadowDiagnostics.sqlQueryDurationMs)ms, round-trip=$($sqlShadowDiagnostics.roundTripDurationMs)ms; authority=$($sqlShadowDiagnostics.authoritative)."
}
Write-Host "Compiled indexes: source=$($compiledIndexDiagnostics.source), records=$($compiledIndexDiagnostics.returnedRecords)/$($compiledIndexDiagnostics.scannedRecords), SQL=$($compiledIndexDiagnostics.sqlDurationMs)ms, round-trip=$($compiledIndexDiagnostics.roundTripDurationMs)ms."
if ($null -ne $moduleContext) {
    Write-Host "Module: $($moduleContext.name)"
    Write-Host "  depends on: $(if ($moduleContext.dependencies.Count) { $moduleContext.dependencies -join ', ' } else { 'none' })"
    Write-Host "  consumed by: $(if ($moduleContext.consumers.Count) { $moduleContext.consumers -join ', ' } else { 'none' })"
}

function Write-ContextSection {
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

Write-ContextSection 'Wiki pages' @($context.wikiPages) { param($item) $item.path }
Write-ContextSection 'Applicable guides' @($context.agentGuides) { param($item) $item.path }
Write-ContextSection 'Projects' @($context.projects) { param($item) $item.path }
Write-ContextSection 'Frontend projects' @($context.frontendProjects) {
    param($item)
    "$($item.name) ($($item.projectType), root: $(if ($item.root) { $item.root } else { '.' }))"
}
Write-ContextSection 'Frontend features' @($context.frontendFeatures) {
    param($item)
    "$($item.area)/$($item.name) - $($item.root)"
}
Write-ContextSection 'Frontend symbols' @($context.frontendSymbols) {
    param($item)
    "$($item.name) [$($item.role)] - $($item.path):$($item.line)"
}
Write-ContextSection 'Frontend routes' @($context.frontendRoutes) {
    param($item)
    "'$($item.route)' - $($item.path):$($item.line)"
}
Write-ContextSection 'Implementation files' @($context.implementationFiles) {
    param($item)
    "$($item.path) [match: $($item.match), score: $($item.score)]"
}
Write-ContextSection 'Localization files' @($context.localization) {
    param($item)
    "$($item.name): en=$($item.englishProperties), ru=$($item.russianProperties), countsMatch=$($item.countsMatch)"
}
Write-ContextSection 'Controllers' @($context.controllers) {
    param($item)
    "$($item.path) ($(@($item.endpoints).Count) endpoints)"
}
Write-ContextSection 'C# symbols' @($context.symbols) {
    param($item)
    "$($item.name) [$($item.role)] - $($item.path):$($item.line)"
}
Write-ContextSection 'DI registrations' @($context.dependencyInjection) {
    param($item)
    "$($item.service) -> $($item.implementation) [$($item.lifetime)] - $($item.path):$($item.line)"
}
Write-ContextSection 'Tests' @($context.tests) { param($item) $item.path }
Write-ContextSection 'Recommended checks' @($context.recommendedChecks) { param($item) $item }
