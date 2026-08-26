[CmdletBinding()]
param(
    [string]$CorpusPath,
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($CorpusPath)) {
    $CorpusPath = Join-Path $repositoryRoot '.llm-wiki/evals/development-context-bundles.json'
}
$resolvedCorpusPath = (Resolve-Path -LiteralPath $CorpusPath).Path
$projectPath = Join-Path $repositoryRoot 'FoodDiary.Development.Mcp/FoodDiary.Development.Mcp.csproj'
$artifactsPath = Join-Path $repositoryRoot '.artifacts/llm-wiki/development-context-evaluation'
$assemblyPath = Join-Path $artifactsPath 'bin/FoodDiary.Development.Mcp/debug/FoodDiary.Development.Mcp.dll'

if (-not $SkipBuild) {
    & dotnet build $projectPath --artifacts-path $artifactsPath --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Development MCP build failed with exit code $LASTEXITCODE." }
}
if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    throw "Development MCP evaluation assembly was not found at $assemblyPath."
}

$json = & dotnet $assemblyPath --evaluate-development-context-bundles $resolvedCorpusPath
if ($LASTEXITCODE -ne 0) { throw "Development-context evaluation failed with exit code $LASTEXITCODE." }
$evaluation = $json | ConvertFrom-Json
if (-not $evaluation.passed) {
    throw "Development-context evaluation missed its thresholds: sqlite=$($evaluation.metrics.sqlitePrimaryRate), scope=$($evaluation.metrics.scopeRecallRate), complete=$($evaluation.metrics.completeBundleRate), checks=$($evaluation.metrics.focusedChecksRate), contextReady=$($evaluation.metrics.contextBundleReadyRate), unplanned=$($evaluation.metrics.unplannedQueryRate), warmP95=$($evaluation.metrics.warmP95DurationMilliseconds)ms."
}
Write-Host "LLM Wiki development-context evaluation passed: cases=$($evaluation.caseCount), sqlite=$($evaluation.metrics.sqlitePrimaryRate), scope=$($evaluation.metrics.scopeRecallRate), complete=$($evaluation.metrics.completeBundleRate), checks=$($evaluation.metrics.focusedChecksRate), contextReady=$($evaluation.metrics.contextBundleReadyRate), unplanned=$($evaluation.metrics.unplannedQueryRate), averageScope=$($evaluation.metrics.averageExpandedScopePaths), warmP95=$($evaluation.metrics.warmP95DurationMilliseconds)ms, cold=$($evaluation.metrics.coldStartDurationMilliseconds)ms, maxCompact=$($evaluation.metrics.maximumCompactCharacters) chars."
