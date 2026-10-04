[CmdletBinding()]
param(
    [string]$Module,
    [string]$Query,
    [Alias('PlannedPath', 'ProposedPath')]
    [string[]]$ScopePath,
    [ValidateSet('Any', 'Api', 'Backend', 'Frontend', 'Database', 'Tests')]
    [string]$ChangeType = 'Any',
    [ValidateSet('Sqlite')]
    [string]$CompiledIndexSource = 'Sqlite',
    [switch]$SkipQueryCache,
    [switch]$Compact,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiApplicationModulePaths.ps1')

if ([string]::IsNullOrWhiteSpace($Module) -and [string]::IsNullOrWhiteSpace($Query)) {
    throw 'Provide -Module, -Query, or both.'
}
if ($Compact -and $Format -ne 'Json') {
    throw '-Compact requires -Format Json.'
}

$queryCacheEntry = $null


$scopePaths = @(
    $ScopePath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_ -split '[;,]' } |
        ForEach-Object { $_.Trim().Replace('\', '/') } |
        Where-Object { $_.Length -gt 0 } |
        Sort-Object -Unique
)
$searchText = (@($Module, $Query) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' '
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
function Get-ContextRemovableIndex([object[]]$Records, [string[]]$Scopes) {
    for ($index = $Records.Count - 1; $index -ge 0; $index--) {
        $remaining = @($Records | Where-Object { $_.path -ne $Records[$index].path })
        $protected = @($Scopes | Where-Object {
            $scope = $_
            (Test-ContextScopeMatch ([string]$Records[$index].path) $scope) -and
                @($remaining | Where-Object { Test-ContextScopeMatch ([string]$_.path) $scope }).Count -eq 0
        }).Count -gt 0
        if (-not $protected) { return $index }
    }
    return -1
}
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
        throw "SQLite context projection could not be prepared. $($_.Exception.Message) Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }
    $graphStatus = & $graphManager -Action status -SkipRefresh -Format Json | ConvertFrom-Json
}
$indexFresh = [bool]$graphStatus.changeSetFresh
# Validate freshness before cache reuse. A cached answer never authorizes a
# stale projection, and ranking/format changes invalidate the same key.
if ($indexFresh -and $Format -eq 'Json' -and -not $SkipQueryCache) {
    $queryCacheEntry = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repositoryRoot -Namespace 'context-sqlite' -Arguments @{
        Module = $Module; Query = $Query; ScopePath = $scopePaths; ChangeType = $ChangeType
        Limit = $Limit; Compact = [bool]$Compact; Fingerprint = [string]$graphStatus.changeSetFingerprint
    } -VerifiedWorkspace $graphStatus.currentWorkspace -DependencyPath @(
        '.artifacts/llm-wiki/code-graph/code-graph.fingerprint',
        '.llm-wiki/policies/context-search-ranking.json',
        '.llm-wiki/tools/code-graph.mjs', '.llm-wiki/tools/code-graph-path-layout.mjs',
        '.llm-wiki/tools/Find-LlmWikiContext.ps1'
        'FoodDiary.Development.Mcp/Wiki/SqliteContextSearchReader.cs'
        '.llm-wiki/tools/LlmWiki.SqliteReader/ContextSearchReader.cs'
    )
    $cachedContext = Read-LlmWikiQueryCache -Entry $queryCacheEntry
    if ($null -ne $cachedContext) {
        $cached = $cachedContext | ConvertFrom-Json
        $cached | Add-Member -NotePropertyName cache -NotePropertyValue @{ hit = $true; storedTimings = $true } -Force
        $cached | ConvertTo-Json -Depth 12
        return
    }
}
$moduleImplementationRoots = if ($Module -match '^[A-Za-z][A-Za-z0-9_]*$') {
    @((Get-LlmWikiApplicationModuleLayout -RepositoryRoot $repositoryRoot -Module $Module).sourceRoots)
} else { @() }
$searchLimit = [Math]::Min(100, [Math]::Max(50, $Limit * 4))
. (Join-Path $PSScriptRoot 'LlmWikiInProcessSqlite.ps1')
$null = Initialize-LlmWikiInProcessSqlite
$sqlResult = [LlmWiki.SqliteReader.ContextSearchReader]::Search(
    $repositoryRoot, $searchText, $searchLimit, $ChangeType, $Module,
    [string[]]$scopePaths, [string]$graphStatus.currentChangeSetFingerprint
) | ConvertFrom-Json
if (-not [bool]$sqlResult.ready) {
    throw 'SQLite search index is unavailable. Run ./.llm-wiki/wiki.ps1 graph-build and retry.'
}
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
$selectionScopes += @($moduleImplementationRoots)
$visibleRecords = @(Select-ContextRecordsWithScopeCoverage $records $selectionScopes $Limit)
$testRecords = @($records | Where-Object { [bool]$_.isTest } | Select-Object -First $Limit)
# API/production ranking can fill the bounded candidate window before any
# tests appear. Retrieve test context independently without reranking code.
if ($testRecords.Count -eq 0 -and $records.Count -gt 0 -and $ChangeType -ne 'Tests') {
    $testSearch = [LlmWiki.SqliteReader.ContextSearchReader]::Search(
        $repositoryRoot, $searchText, $searchLimit, 'Tests', $Module,
        [string[]]$scopePaths, [string]$graphStatus.currentChangeSetFingerprint
    ) | ConvertFrom-Json
    if (-not [bool]$testSearch.ready) {
        throw 'SQLite test context index is unavailable. Run ./.llm-wiki/wiki.ps1 graph-build and retry.'
    }
    $testRecords = @($testSearch.records | Where-Object { [bool]$_.isTest } | Select-Object -First $Limit)
}
$compiledIndexStopwatch.Stop()
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
if ($Compact) {
    # One bounded list, with test evidence included without duplicating the
    # production list into every legacy-shaped category.
    $compactRecords = [Collections.Generic.List[object]]::new()
    foreach ($record in $visibleRecords) { $compactRecords.Add($record) }
    if ($Limit -gt 1 -and $testRecords.Count -gt 0 -and @($compactRecords | Where-Object isTest).Count -eq 0) {
        $replacementIndex = Get-ContextRemovableIndex @($compactRecords) $scopePaths
        if ($compactRecords.Count -lt $Limit) { $compactRecords.Add($testRecords[0]) }
        elseif ($replacementIndex -ge 0) { $compactRecords[$replacementIndex] = $testRecords[0] }
    }
    $context = [ordered]@{
        query = $context.query; confidence = $confidence; conclusive = $conclusive
        abstained = $context.abstained; ambiguityReason = $context.ambiguityReason
        candidates = @($compactRecords | ForEach-Object {
            [ordered]@{ path = $_.path; rank = $_.rank; kind = $_.role; module = $_.module; layer = $_.layer
                confidence = $_.confidence; reasons = @($_.reasons | Select-Object -First 2) }
        })
        compiledIndex = $context.compiledIndex
        output = [ordered]@{ compact = $true; characterBudget = 12000; omittedCandidates = [Math]::Max(0, $records.Count - $compactRecords.Count)
            missingScopes = @($scopePaths | Where-Object {
                $scope = $_
                @($compactRecords | Where-Object { Test-ContextScopeMatch ([string]$_.path) $scope }).Count -eq 0
            })
            details = 'Use context-explain or omit -Compact for full ranking diagnostics. Read source before editing.' }
    }
    # Reserve room for cache-hit metadata added on subsequent requests.
    while (($context | ConvertTo-Json -Depth 12).Length -gt 11800 -and $context.candidates.Count -gt 1) {
        $removableIndex = Get-ContextRemovableIndex $context.candidates $scopePaths
        if ($removableIndex -lt 0) { throw 'Compact context cannot preserve requested scope coverage within its character budget. Narrow the scopes or omit -Compact.' }
        $removePath = $context.candidates[$removableIndex].path
        $context.candidates = @($context.candidates | Where-Object { $_.path -ne $removePath })
        $context.output.omittedCandidates++
    }
}
$contextJson = $context | ConvertTo-Json -Depth 12
if ($Compact -and $contextJson.Length -gt 11800) {
    throw 'Compact context exceeds its character budget. Narrow the query or scope, or omit -Compact.'
}
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
