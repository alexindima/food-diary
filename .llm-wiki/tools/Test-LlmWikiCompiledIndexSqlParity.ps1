[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$contextTool = Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for compiled-index parity.' }
$null = & $manager -Action build -Format Json

$cases = @(
    @{ Module = 'Recipes'; Query = 'Recipe nutrition updater'; ScopePath = 'FoodDiary.Application.Recipes'; ChangeType = 'Backend'; ExpectedPath = '^FoodDiary\.Application\.Recipes/' }
    @{ Module = 'Users'; Query = 'password reset command handler'; ScopePath = 'FoodDiary.Application.Users'; ChangeType = 'Backend'; ExpectedPath = '^FoodDiary\.Application\.Users/' }
    @{ Module = ''; Query = 'achievement definitions controller route'; ScopePath = 'FoodDiary.Presentation.Api'; ChangeType = 'Api'; ExpectedPath = '^FoodDiary\.Presentation\.Api/' }
    @{ Module = ''; Query = 'SQLite Wiki context search'; ScopePath = '.llm-wiki/tools'; ChangeType = 'Tests'; ExpectedPath = '^\.llm-wiki/tools/' }
    @{ Module = 'Meals'; Query = 'meal projection read repository'; ScopePath = 'FoodDiary.Application.Meals'; ChangeType = 'Backend'; ExpectedPath = '^FoodDiary\.Application\.Meals/' }
    @{ Module = ''; Query = 'autocomplete product search component'; ScopePath = 'FoodDiary.Web.Client/src/app/features/products'; ChangeType = 'Frontend'; ExpectedPath = '^FoodDiary\.Web\.Client/src/app/features/products/' }
    @{ Module = ''; Query = 'responsive dashboard component layout'; ScopePath = 'FoodDiary.Web.Client/src/app/features/dashboard'; ChangeType = 'Frontend'; ExpectedPath = '^FoodDiary\.Web\.Client/src/app/features/dashboard/' }
    @{ Module = ''; Query = 'translation locale'; ScopePath = 'FoodDiary.Web.Client/assets/i18n'; ChangeType = 'Frontend'; ExpectedPath = '^FoodDiary\.Web\.Client/(?:scripts/check-i18n\.mjs|src/app/shared/i18n/)'; ExpectAbstention = $true }
)
$endToEndDurations = [Collections.Generic.List[double]]::new()

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
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $sqlite = & $contextTool @arguments | ConvertFrom-Json
    $stopwatch.Stop()

    if ([string]$sqlite.compiledIndex.source -ne 'sqlite-search') {
        throw "$($case.Query): default context route did not use the SQLite search projection."
    }
    if (-not [bool]$sqlite.compiledIndex.fresh -or [string]$sqlite.compiledIndex.indexedChangeSetFingerprint -cne [string]$sqlite.compiledIndex.currentChangeSetFingerprint) {
        throw "$($case.Query): SQLite context route reported stale or mismatched workspace state after an explicit graph build."
    }
    if (@($sqlite.candidates).Count -eq 0 -or -not (@($sqlite.candidates.path) -match $case.ExpectedPath)) {
        throw "$($case.Query): SQLite context route did not return a candidate inside the requested scope."
    }
    if ($case.ContainsKey('ExpectAbstention') -and [bool]$case.ExpectAbstention -and -not [bool]$sqlite.abstained) {
        throw "$($case.Query): SQLite context route claimed conclusive coverage for a scope whose locale JSON files are not directly indexed."
    }
    if ([int]$sqlite.compiledIndex.returnedRecords -ge [int]$sqlite.compiledIndex.indexedDocuments) {
        throw "$($case.Query): SQLite search did not reduce the indexed corpus to a bounded result set."
    }
    $endToEndDurations.Add($stopwatch.Elapsed.TotalMilliseconds)
}

$average = [Math]::Round(($endToEndDurations | Measure-Object -Average).Average, 2)
if ($average -gt 5000) {
    throw "SQLite context retrieval exceeded the 5s average safety envelope: ${average}ms."
}
Write-Host "LLM Wiki SQLite context retrieval passed: $($cases.Count)/$($cases.Count) scoped cases, fresh fingerprints, bounded payloads, average=${average}ms."
