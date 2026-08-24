[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$contextTool = Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for compiled-index parity.' }
$null = & $manager -Action build -Format Json

$cases = @(
    @{ Module = 'Recipes'; Query = 'Recipe nutrition updater'; ScopePath = 'FoodDiary.Application.Recipes'; ChangeType = 'Backend' }
    @{ Module = 'Users'; Query = 'password reset command handler'; ScopePath = 'FoodDiary.Application.Users'; ChangeType = 'Backend' }
    @{ Module = ''; Query = 'achievement definitions controller route'; ScopePath = 'FoodDiary.Presentation.Api'; ChangeType = 'Api' }
    @{ Module = ''; Query = 'SQLite Wiki context search'; ScopePath = '.llm-wiki/tools'; ChangeType = 'Tests' }
    @{ Module = 'Meals'; Query = 'meal projection read repository'; ScopePath = 'FoodDiary.Application.Meals'; ChangeType = 'Backend' }
    @{ Module = ''; Query = 'autocomplete product search component'; ScopePath = 'FoodDiary.Web.Client/src/app/features/products'; ChangeType = 'Frontend' }
    @{ Module = ''; Query = 'responsive dashboard component layout'; ScopePath = 'FoodDiary.Web.Client/src/app/features/dashboard'; ChangeType = 'Frontend' }
    @{ Module = ''; Query = 'translation locale'; ScopePath = 'FoodDiary.Web.Client/assets/i18n'; ChangeType = 'Frontend' }
)
$sections = @(
    'module'
    'agentGuides'
    'projects'
    'frontendProjects'
    'controllers'
    'symbols'
    'dependencyInjection'
    'frontendFeatures'
    'frontendSymbols'
    'frontendRoutes'
    'implementationFiles'
    'localization'
    'tests'
)
$sqlRoundTrips = [Collections.Generic.List[double]]::new()
$jsonRoundTrips = [Collections.Generic.List[double]]::new()
$sqlEndToEnd = [Collections.Generic.List[double]]::new()
$jsonEndToEnd = [Collections.Generic.List[double]]::new()
$reducedCases = 0
$caseIndex = 0

foreach ($case in $cases) {
    $arguments = @{
        Query = $case.Query
        ScopePath = $case.ScopePath
        ChangeType = $case.ChangeType
        Format = 'Json'
        Limit = 12
        SkipQueryCache = $true
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$case.Module)) { $arguments.Module = $case.Module }
    if (($caseIndex % 2) -eq 0) {
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $contextTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $contextTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
    } else {
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $contextTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $contextTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
    }

    if ([string]$sqlite.compiledIndex.source -ne 'sqlite-compiled-index') {
        throw "$($case.Query): default context route did not use the SQLite compiled-index projection."
    }
    foreach ($section in $sections) {
        $sqliteJson = $sqlite.$section | ConvertTo-Json -Depth 12 -Compress
        $baselineJson = $json.$section | ConvertTo-Json -Depth 12 -Compress
        if ($sqliteJson -cne $baselineJson) {
            throw "$($case.Query): SQLite/JSON parity failed for section '$section'."
        }
    }
    if ([int]$sqlite.compiledIndex.returnedRecords -lt [int]$sqlite.compiledIndex.scannedRecords) {
        $reducedCases++
    }
    $sqlRoundTrips.Add([double]$sqlite.compiledIndex.roundTripDurationMs)
    $jsonRoundTrips.Add([double]$json.compiledIndex.roundTripDurationMs)
    $sqlEndToEnd.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
    $jsonEndToEnd.Add($jsonStopwatch.Elapsed.TotalMilliseconds)
    $caseIndex++
}

if ($reducedCases -ne $cases.Count) {
    throw "SQLite compiled-index candidate filtering reduced payloads for only $reducedCases/$($cases.Count) parity cases."
}
function Get-NormalizedSourceHash([string]$Path) {
    $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
$catalogHash = Get-NormalizedSourceHash (Join-Path $repositoryRoot '.llm-wiki/generated/repository-catalog.json')
$symbolHash = Get-NormalizedSourceHash (Join-Path $repositoryRoot '.llm-wiki/generated/csharp-symbol-index.json')
$frontendHash = Get-NormalizedSourceHash (Join-Path $repositoryRoot '.llm-wiki/generated/frontend-index.json')
$lastSqlite = & $contextTool -Query 'SQLite Wiki context search' -ScopePath '.llm-wiki/tools' -ChangeType Tests -Format Json -Limit 12 -SkipQueryCache | ConvertFrom-Json
if ([string]$lastSqlite.compiledIndex.sourceHashes.repositoryCatalog -cne $catalogHash -or
    [string]$lastSqlite.compiledIndex.sourceHashes.csharpSymbols -cne $symbolHash -or
    [string]$lastSqlite.compiledIndex.sourceHashes.frontend -cne $frontendHash) {
    throw 'SQLite compiled-index source hashes do not match the current generated JSON sources.'
}

$sqlAverage = [Math]::Round(($sqlRoundTrips | Measure-Object -Average).Average, 2)
$jsonAverage = [Math]::Round(($jsonRoundTrips | Measure-Object -Average).Average, 2)
if ($sqlAverage -gt ($jsonAverage + 250)) {
    throw "SQLite compiled-index transport regressed beyond the 250ms safety envelope: SQL=${sqlAverage}ms, JSON=${jsonAverage}ms."
}
$sqlEndToEndAverage = [Math]::Round(($sqlEndToEnd | Measure-Object -Average).Average, 2)
$jsonEndToEndAverage = [Math]::Round(($jsonEndToEnd | Measure-Object -Average).Average, 2)
if ($sqlEndToEndAverage -gt ($jsonEndToEndAverage + 250)) {
    throw "SQLite compiled-index context regressed beyond the 250ms end-to-end safety envelope: SQL=${sqlEndToEndAverage}ms, JSON=${jsonEndToEndAverage}ms."
}
Write-Host "LLM Wiki compiled-index SQL parity passed: $($cases.Count)/$($cases.Count) cases, $($sections.Count) sections each; data load SQL=${sqlAverage}ms/JSON=${jsonAverage}ms; end-to-end SQL=${sqlEndToEndAverage}ms/JSON=${jsonEndToEndAverage}ms; payload reduction=$reducedCases/$($cases.Count)."
