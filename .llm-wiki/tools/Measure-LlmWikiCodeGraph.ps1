[CmdletBinding()]
param(
    [string]$OutputPath = '.artifacts/llm-wiki/code-graph-benchmark/after/timings.json'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$results = [Collections.Generic.List[object]]::new()
function Measure-Graph([string]$Name, [scriptblock]$Operation) {
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $result = & $Operation
    $stopwatch.Stop()
    $results.Add([pscustomobject][ordered]@{
        name = $Name
        milliseconds = $stopwatch.ElapsedMilliseconds
        resultCount = if ($null -eq $result) { 0 } elseif ($result -is [array]) { @($result).Count } else { 1 }
    })
    return $result
}

$null = Measure-Graph 'incremental-build' { & $manager build -Format Json | ConvertFrom-Json }
$null = Measure-Graph 'symbol-recipe-nutrition' { & $manager symbol -Query RecipeNutritionUpdater -Format Json | ConvertFrom-Json }
$null = Measure-Graph 'consumers-recipe-overview' { & $manager consumers -Query IRecipeOverviewReadService -Limit 100 -Format Json | ConvertFrom-Json }
$null = Measure-Graph 'trace-recipe-nutrition' { & $manager trace -Query RecipeNutritionUpdater -Limit 100 -Format Json | ConvertFrom-Json }
$null = Measure-Graph 'impact-recipes-module' { & $manager impact -ChangedPath 'FoodDiary.Application/Recipes' -Limit 500 -Format Json | ConvertFrom-Json }
$null = Measure-Graph 'research-recipes' {
    & (Join-Path $PSScriptRoot 'Get-LlmWikiGraphResearch.ps1') `
        -Objective 'Extract Recipes into an isolated application module while preserving consumers and composition dependencies' `
        -ProposedPath 'FoodDiary.Application/Recipes' `
        -Limit 100 `
        -Format Json | ConvertFrom-Json
}
$absoluteOutputPath = if ([IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $repositoryRoot $OutputPath }
$directory = Split-Path -Parent $absoluteOutputPath
if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
[IO.File]::WriteAllText($absoluteOutputPath, (($results | ConvertTo-Json -Depth 5) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
$results | Format-Table -AutoSize
