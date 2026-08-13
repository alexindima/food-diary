[CmdletBinding()]
param(
    [string[]]$ChangedPath,
    [string[]]$ProposedPath,
    [ValidateRange(1, 500)][int]$Limit = 100,
    [ValidateSet('Text','Json')][string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$scope = [string[]]@((@($ProposedPath) + @($ChangedPath)) | Where-Object { $_ } | Sort-Object -Unique)
if ($scope.Length -eq 0) { throw 'Fast graph test-plan requires -ChangedPath or -PlannedPath.' }
$impact = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') -Action impact -ChangedPath $scope -Limit ([Math]::Min(500, $Limit * 10)) -Format Json | ConvertFrom-Json
$isTestPath = { param($Path) [string]$Path -match '(^|/)(?:tests?/|[^/]+\.Tests?/)|\.(?:spec|test)\.(?:ts|js)$' }
$direct = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$downstream = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($path in @($scope + @($impact.paths))) {
    if (& $isTestPath $path) { [void]$direct.Add([string]$path); continue }
    foreach ($candidate in @(
        ([string]$path -replace '\.ts$', '.spec.ts')
        ([string]$path -replace '\.cs$', 'Tests.cs')
    )) {
        if ($candidate -ne $path -and (Test-Path -LiteralPath (Join-Path $repositoryRoot $candidate) -PathType Leaf)) { [void]$direct.Add($candidate) }
    }
}
foreach ($consumer in @($impact.consumers)) {
    if (& $isTestPath $consumer.path) { [void]$direct.Add([string]$consumer.path) }
}
foreach ($path in $direct) { [void]$downstream.Remove($path) }
$result = [pscustomobject][ordered]@{
    mode = 'sqlite-graph-only'
    scope = $scope
    required = @($direct | Sort-Object | Select-Object -First $Limit)
    recommended = @($downstream | Sort-Object | Select-Object -First $Limit)
    fullRegression = @('Use ordinary test-plan or the publication gate when policy, journeys, runtime wiring, or historical regressions must be included.')
    confidence = $(if ($direct.Count -gt 0) { 'high' } elseif ($downstream.Count -gt 0) { 'medium' } else { 'low' })
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 7; return }
Write-Host "Fast graph test plan: confidence=$($result.confidence), required=$(@($result.required).Count), recommended=$(@($result.recommended).Count)."
foreach ($path in $result.required) { Write-Host " REQUIRED direct: $path" }
foreach ($path in $result.recommended) { Write-Host " RECOMMENDED downstream: $path" }
Write-Host 'Graph-only mode omits policy/journey/history expansion; use ordinary test-plan for governed or runtime-sensitive work.'
