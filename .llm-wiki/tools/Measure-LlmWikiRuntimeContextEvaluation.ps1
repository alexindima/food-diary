[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$CorpusPath,
    [switch]$SkipBuild,
    [ValidateSet('Text', 'Json')][string]$Format = 'Json'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$artifactRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/runtime-parity'
if (-not $SkipBuild) {
    & dotnet build (Join-Path $repositoryRoot 'FoodDiary.Development.Mcp/FoodDiary.Development.Mcp.csproj') --artifacts-path $artifactRoot --nologo --verbosity quiet | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Unable to build the .NET context-search runtime evaluator.' }
}
$assembly = Join-Path $artifactRoot 'bin/FoodDiary.Development.Mcp/debug/FoodDiary.Development.Mcp.dll'
if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) { throw 'The .NET context-search runtime evaluator has not been built.' }
$resolvedCorpusPath = (Resolve-Path -LiteralPath $CorpusPath).Path
$originalRoot = $env:FOODDIARY_REPOSITORY_ROOT
try {
    $env:FOODDIARY_REPOSITORY_ROOT = $repositoryRoot
    $json = & dotnet $assembly --evaluate-context-search $resolvedCorpusPath
    if ($LASTEXITCODE -ne 0) { throw "The .NET context-search runtime evaluation failed with exit code $LASTEXITCODE." }
} finally {
    $env:FOODDIARY_REPOSITORY_ROOT = $originalRoot
}
if ($Format -eq 'Json') { $json -join [Environment]::NewLine; return }
$evaluation = $json | ConvertFrom-Json
Write-Host ".NET context evaluation: $($evaluation.caseCount) cases; top1=$($evaluation.metrics.top1Count); top10=$($evaluation.metrics.top10Count)."
