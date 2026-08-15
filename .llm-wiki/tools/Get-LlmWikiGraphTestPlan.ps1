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
$scoped = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$graphConsumers = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$scopeTooBroad = [Collections.Generic.List[object]]::new()
foreach ($path in $scope) {
    $normalizedPath = ([string]$path).Replace('\', '/').TrimEnd('/')
    $absolutePath = Join-Path $repositoryRoot $normalizedPath
    if (& $isTestPath $normalizedPath) { [void]$scoped.Add($normalizedPath); continue }
    if (Test-Path -LiteralPath $absolutePath -PathType Container) {
        $directoryTests = @(
            Get-ChildItem -LiteralPath $absolutePath -Recurse -File |
                Where-Object { $_.Name -match '\.(?:spec|test)\.(?:ts|js)$' } |
                Select-Object -First (($Limit * 2) + 1)
        )
        if ($directoryTests.Count -gt ($Limit * 2)) {
            $scopeTooBroad.Add([pscustomobject]@{
                path = $normalizedPath
                discoveredAtLeast = $directoryTests.Count
                suggestedAction = 'Provide a feature, component, or shared-library subdirectory.'
            })
        } else {
            foreach ($test in $directoryTests) {
                $relativeTest = $test.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
                [void]$scoped.Add($relativeTest)
            }
        }
        continue
    }
    foreach ($candidate in @(
        ($normalizedPath -replace '\.ts$', '.spec.ts')
        ($normalizedPath -replace '\.cs$', 'Tests.cs')
    )) {
        if ($candidate -ne $normalizedPath -and (Test-Path -LiteralPath (Join-Path $repositoryRoot $candidate) -PathType Leaf)) { [void]$scoped.Add($candidate) }
    }
}
foreach ($consumer in @($impact.consumers)) {
    if (& $isTestPath $consumer.path) { [void]$graphConsumers.Add([string]$consumer.path) }
}
$required = @(
    @($scoped | Sort-Object)
    @($graphConsumers | Where-Object { -not $scoped.Contains($_) } | Sort-Object)
) | Select-Object -First $Limit
$recommended = @(
    $graphConsumers |
        Where-Object { $_ -notin $required } |
        Sort-Object |
        Select-Object -First $Limit
)
$result = [pscustomobject][ordered]@{
    mode = 'sqlite-graph-only'
    scope = $scope
    required = @($required)
    recommended = @($recommended)
    scopeTooBroad = @($scopeTooBroad)
    fullRegression = @('Use ordinary test-plan or the publication gate when policy, journeys, runtime wiring, or historical regressions must be included.')
    confidence = $(if ($scopeTooBroad.Count -gt 0) { 'low' } elseif ($scoped.Count -gt 0) { 'high' } elseif ($graphConsumers.Count -gt 0) { 'medium' } else { 'low' })
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 7; return }
Write-Host "Fast graph test plan: confidence=$($result.confidence), required=$(@($result.required).Count), recommended=$(@($result.recommended).Count)."
foreach ($path in $result.required) { Write-Host " REQUIRED direct: $path" }
foreach ($path in $result.recommended) { Write-Host " RECOMMENDED downstream: $path" }
Write-Host 'Graph-only mode omits policy/journey/history expansion; use ordinary test-plan for governed or runtime-sensitive work.'
