[CmdletBinding()]
param(
    [string]$OutputPath = '.artifacts/llm-wiki/code-graph-benchmark/after/timings.json',
    [string]$ScopePath = 'Modules/Recipes/Application',
    [string]$NutritionPath = 'Modules/Recipes/Application/Services/RecipeNutritionUpdater.cs'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
foreach ($path in @($ScopePath, $NutritionPath)) {
    if (-not (Test-Path -LiteralPath (Join-Path $repositoryRoot $path))) { throw "Benchmark source scope does not exist: $path" }
}
$results = [Collections.Generic.List[object]]::new()
function Measure-Graph([string]$Name, [scriptblock]$Operation, [scriptblock]$CountRecords) {
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $result = & $Operation
    $stopwatch.Stop()
    $resultCount = [int](& $CountRecords $result)
    if ($resultCount -le 0) { throw "Benchmark scenario '$Name' returned no source records; timing an empty result is invalid." }
    $results.Add([pscustomobject][ordered]@{
        name = $Name
        milliseconds = $stopwatch.ElapsedMilliseconds
        resultCount = $resultCount
    })
    return $result
}

$null = Measure-Graph 'incremental-build' { & $manager build -Format Json | ConvertFrom-Json } { param($Result) $Result.files }
$null = Measure-Graph 'symbol-recipe-nutrition' { & $manager symbol -Query RecipeNutritionUpdater -Format Json | ConvertFrom-Json } { param($Result) @($Result.symbols).Count }
$null = Measure-Graph 'consumers-recipe-overview' { & $manager consumers -Query IRecipeOverviewReadService -Limit 100 -Format Json | ConvertFrom-Json } { param($Result) @($Result.consumers).Count }
$null = Measure-Graph 'trace-recipe-nutrition' { & $manager trace -Query RecipeNutritionUpdater -Limit 100 -Format Json | ConvertFrom-Json } { param($Result) @($Result.symbols).Count }
$null = Measure-Graph 'impact-recipes-module' { & $manager impact -ChangedPath $ScopePath -Limit 500 -Format Json | ConvertFrom-Json } { param($Result) @($Result.declaredSymbols).Count }
$null = Measure-Graph 'research-recipes' {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphResearch.ps1') `
        -Objective 'Investigate Recipes application consumers and composition dependencies' `
        -ProposedPath $ScopePath `
        -Limit 100 `
        -Format Json | ConvertFrom-Json
} { param($Result) @($Result.matchedPaths).Count }
$null = Measure-Graph 'test-plan-recipes' {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphTestPlan.ps1') `
        -ProposedPath $NutritionPath -Limit 100 -Format Json | ConvertFrom-Json
} { param($Result) @($Result.required).Count + @($Result.recommended).Count }
$null = Measure-Graph 'typed-relations-recipes' {
    & $manager relations -ChangedPath $ScopePath `
        -RelationKind mediator-handler,di-service -Limit 500 -Format Json | ConvertFrom-Json
} { param($Result) @($Result.relations).Count }
$null = Measure-Graph 'coverage-shadow' { & $manager coverage -Format Json | ConvertFrom-Json } { param($Result) $Result.files }
$absoluteOutputPath = if ([IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $repositoryRoot $OutputPath }
$directory = Split-Path -Parent $absoluteOutputPath
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
[IO.File]::WriteAllText($absoluteOutputPath, (($results | ConvertTo-Json -Depth 5) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
$results | Format-Table -AutoSize
