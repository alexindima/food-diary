[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$contextTool = Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$null = & $manager -Action build -Format Json

$cases = @(
    @{ Module = 'Recipes'; Query = 'Recipe nutrition updater'; ScopePath = 'FoodDiary.Application.Recipes'; ChangeType = 'Backend' }
    @{ Module = 'Users'; Query = 'password reset command handler'; ScopePath = 'FoodDiary.Application.Users'; ChangeType = 'Backend' }
    @{ Module = ''; Query = 'achievement definitions controller route'; ScopePath = 'FoodDiary.Presentation.Api'; ChangeType = 'Api' }
    @{ Module = ''; Query = 'SQLite Wiki context search'; ScopePath = '.llm-wiki/tools'; ChangeType = 'Tests' }
    @{ Module = 'Meals'; Query = 'meal projection read repository'; ScopePath = 'FoodDiary.Application.Meals'; ChangeType = 'Backend' }
)
$sections = @(
    'module'
    'agentGuides'
    'projects'
    'frontendProjects'
    'controllers'
    'symbols'
    'dependencyInjection'
)
$sqlRoundTrips = [Collections.Generic.List[double]]::new()
$jsonRoundTrips = [Collections.Generic.List[double]]::new()
$reducedCases = 0

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
    $sqlite = & $contextTool @arguments | ConvertFrom-Json
    $json = & $contextTool @arguments -CompiledIndexSource Json | ConvertFrom-Json

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
$lastSqlite = & $contextTool -Query 'SQLite Wiki context search' -ScopePath '.llm-wiki/tools' -ChangeType Tests -Format Json -Limit 12 -SkipQueryCache | ConvertFrom-Json
if ([string]$lastSqlite.compiledIndex.sourceHashes.repositoryCatalog -cne $catalogHash -or
    [string]$lastSqlite.compiledIndex.sourceHashes.csharpSymbols -cne $symbolHash) {
    throw 'SQLite compiled-index source hashes do not match the current generated JSON sources.'
}

$sqlAverage = [Math]::Round(($sqlRoundTrips | Measure-Object -Average).Average, 2)
$jsonAverage = [Math]::Round(($jsonRoundTrips | Measure-Object -Average).Average, 2)
if ($sqlAverage -gt ($jsonAverage + 250)) {
    throw "SQLite compiled-index transport regressed beyond the 250ms safety envelope: SQL=${sqlAverage}ms, JSON=${jsonAverage}ms."
}
Write-Host "LLM Wiki compiled-index SQL parity passed: $($cases.Count)/$($cases.Count) cases, $($sections.Count) sections each; SQL=${sqlAverage}ms, JSON=${jsonAverage}ms average data load; payload reduction=$reducedCases/$($cases.Count)."
