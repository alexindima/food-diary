[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Objective,
    [string]$Module,
    [string[]]$ProposedPath,
    [ValidateRange(1, 500)][int]$Limit = 100,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$paths = [string[]]@($ProposedPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
if ($paths.Length -eq 0 -and -not [string]::IsNullOrWhiteSpace($Module)) {
    $paths = @("FoodDiary.Application/$Module", "FoodDiary.Application.$Module")
}
if ($paths.Length -eq 0) {
    $candidate = @([regex]::Matches($Objective, '\b[A-Z][A-Za-z0-9]+\b') | ForEach-Object Value | Select-Object -Last 1)
    $hint = if ($candidate.Count -gt 0) { " The intent suggests '$($candidate[0])', but fast mode will not select a boundary silently." } else { '' }
    throw "Fast graph research requires an explicit source boundary.$hint Examples: ./.llm-wiki/wiki.ps1 research -Fast -Module Recipes -Query 'Extract Recipes'; ./.llm-wiki/wiki.ps1 research -Fast -PlannedPath 'FoodDiary.Application/Recipes' -Query 'Extract Recipes'."
}
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$graphLimit = [Math]::Min(500, [Math]::Max(100, $Limit * 20))
$impact = & $manager impact -ChangedPath $paths -Limit $graphLimit -Format Json | ConvertFrom-Json
$stopwatch.Stop()
$matchedPaths = [object[]]@($impact.paths | Sort-Object -Unique)
$downstream = [object[]]@($impact.consumers | Group-Object path | ForEach-Object {
    [pscustomobject][ordered]@{
        path = $_.Name
        symbols = @($_.Group.symbol | Sort-Object -Unique)
    }
} | Sort-Object path)
$dependencies = [object[]]@($impact.references | Group-Object declarationPath | ForEach-Object {
    [pscustomobject][ordered]@{
        path = $_.Name
        symbols = @($_.Group.symbol | Sort-Object -Unique)
    }
} | Sort-Object path)
$logicalModule = if (-not [string]::IsNullOrWhiteSpace($Module)) { $Module } else {
    $firstBoundary = [string]@($paths)[0]
    $match = [regex]::Match($firstBoundary.Replace('\','/'), '^FoodDiary\.Application(?:/|\.)(?<module>[^/]+)')
    if ($match.Success) { $match.Groups['module'].Value } else { Split-Path $firstBoundary -Leaf }
}
$sourceRoot = [string]@($paths)[0]
$currentProject = if ($sourceRoot -match '^(?<project>[^/\\]+)[/\\]') { $Matches['project'] } else { $sourceRoot }
$targetProjectCandidate = if ($currentProject -eq 'FoodDiary.Application' -and -not [string]::IsNullOrWhiteSpace($logicalModule)) { "FoodDiary.Application.$logicalModule" } else { $null }
$result = [pscustomobject][ordered]@{
    mode = 'experimental-sqlite-graph'
    objective = $Objective
    requestedPaths = $paths
    boundary = [pscustomobject][ordered]@{
        logicalModule = $logicalModule
        currentProject = $currentProject
        sourceRoot = $sourceRoot
        targetProjectCandidate = $targetProjectCandidate
    }
    matchedPaths = $matchedPaths
    declarations = @($impact.declaredSymbols)
    downstreamConsumers = $downstream
    dependencies = $dependencies
    durationMs = $stopwatch.ElapsedMilliseconds
    confidence = $(if ($matchedPaths.Count -gt 0 -and ($downstream.Count + $dependencies.Count) -gt 0) { 'high' } elseif ($matchedPaths.Count -gt 0) { 'medium' } else { 'low' })
    limitations = @(
        'Static symbol/token evidence; dynamic dispatch and reflection require source verification.'
        'Use ordinary research when Git precedents, known failures, journeys, or policy guidance are required.'
    )
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; return }
Write-Host "Fast graph research: $($result.confidence) confidence, $($matchedPaths.Count) source file(s), $($downstream.Count) downstream path(s), $($dependencies.Count) dependency path(s), $($result.durationMs)ms."
Write-Host "Boundary: logicalModule=$($result.boundary.logicalModule); currentProject=$($result.boundary.currentProject); sourceRoot=$($result.boundary.sourceRoot); targetCandidate=$($result.boundary.targetProjectCandidate)"
Write-Host 'Source boundary:'
foreach ($path in @($matchedPaths | Select-Object -First $Limit)) { Write-Host " - $path" }
Write-Host 'Downstream consumers:'
foreach ($item in @($downstream | Select-Object -First $Limit)) { Write-Host " - $($item.path) [$($item.symbols -join ', ')]" }
Write-Host 'Dependencies:'
foreach ($item in @($dependencies | Select-Object -First $Limit)) { Write-Host " - $($item.path) [$($item.symbols -join ', ')]" }
Write-Host 'This is a bounded source scan. Run ordinary research for historical and governed context.'
