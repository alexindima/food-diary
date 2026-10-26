[CmdletBinding()]
param(
    [string[]]$ChangedPath,
    [string[]]$ProposedPath,
    [switch]$SkipRefresh,
    [ValidateRange(1, 500)][int]$Limit = 100,
    [ValidateSet('Text','Json')][string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$scope = [string[]]@((@($ProposedPath) + @($ChangedPath)) | Where-Object { $_ } | Sort-Object -Unique)
if ($scope.Length -eq 0) { throw 'Fast graph test-plan requires -ChangedPath or -PlannedPath.' }
$directoryScopes = @($scope | ForEach-Object {
    $normalizedPath = ([string]$_).Replace('\', '/').TrimEnd('/')
    if (Test-Path -LiteralPath (Join-Path $repositoryRoot $normalizedPath) -PathType Container) { $normalizedPath }
})
# Directory consumers span modules. Keep the existing bounded candidate horizon
# independent of the response limit so alphabetically earlier foreign paths do
# not prevent the selected directory's tests from reaching the ranking stage.
$consumerLimit = if ($directoryScopes.Count -gt 0) { 500 } else { [Math]::Min(500, $Limit * 10) }
$impact = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') -Action impact -ChangedPath $scope -Limit $consumerLimit -SkipRefresh:$SkipRefresh -Format Json | ConvertFrom-Json
$isTestPath = { param($Path) [string]$Path -match '(^|/)(?:tests?/|[^/]+\.Tests?/)|\.(?:spec|test)\.(?:ts|js|mjs|cjs)$|(^|/)Test-[^/]+\.ps1$' }
$scoped = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$graphConsumers = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$owningGraphTests = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$directoryAffinity = [Collections.Generic.Dictionary[string, int]]::new([StringComparer]::OrdinalIgnoreCase)
$explicitSourcePaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($path in $scope) { [void]$explicitSourcePaths.Add(([string]$path).Replace('\', '/').TrimEnd('/')) }
$explicitSourceTypes = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($declaration in @($impact.declaredSymbols)) {
    if ($declaration.kind -ne 'method' -and $explicitSourcePaths.Contains([string]$declaration.path)) {
        [void]$explicitSourceTypes.Add([string]$declaration.name)
    }
}
$scopeTooBroad = [Collections.Generic.List[object]]::new()
foreach ($path in $scope) {
    $normalizedPath = ([string]$path).Replace('\', '/').TrimEnd('/')
    $absolutePath = Join-Path $repositoryRoot $normalizedPath
    if (& $isTestPath $normalizedPath) { [void]$scoped.Add($normalizedPath); continue }
    if (Test-Path -LiteralPath $absolutePath -PathType Container) {
        $directoryTests = @(
            Get-ChildItem -LiteralPath $absolutePath -Recurse -File |
                Where-Object { $_.Name -match '\.(?:spec|test)\.(?:ts|js|mjs|cjs)$' } |
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
        $(if ($normalizedPath -match '\.(?:js|mjs|cjs)$') { $normalizedPath -replace '\.(js|mjs|cjs)$', '.test.$1' })
        $(if ($normalizedPath -match '\.ps1$') { $normalizedPath -replace '/(?:Get|Set|Write|Build|Find|Invoke|Manage|Update|New)-([^/]+)\.ps1$', '/Test-$1.ps1' })
    )) {
        if ($candidate -and $candidate -ne $normalizedPath -and (Test-Path -LiteralPath (Join-Path $repositoryRoot $candidate) -PathType Leaf)) { [void]$scoped.Add($candidate) }
    }
}
foreach ($consumer in @($impact.consumers)) {
    if (& $isTestPath $consumer.path) {
        [void]$graphConsumers.Add([string]$consumer.path)
        if (-not $directoryAffinity.ContainsKey([string]$consumer.path)) {
            $candidateSegments = ([string]$consumer.path).Split('/')
            $closestDirectory = 0
            foreach ($directoryScope in $directoryScopes) {
                $sourceSegments = $directoryScope.Split('/')
                $shared = 0
                while ($shared -lt $sourceSegments.Length -and $shared -lt $candidateSegments.Length -and
                    [string]::Equals($sourceSegments[$shared], $candidateSegments[$shared], [StringComparison]::OrdinalIgnoreCase)) {
                    $shared++
                }
                $closestDirectory = [Math]::Max($closestDirectory, $shared)
            }
            $directoryAffinity[[string]$consumer.path] = $closestDirectory
        }
        # A graph-backed test of the selected type outranks broad downstream
        # consumers before the final response limit, including sibling projects.
        $testName = [IO.Path]::GetFileNameWithoutExtension([string]$consumer.path)
        if ($consumer.language -eq 'csharp' -and $explicitSourceTypes.Contains([string]$consumer.symbol) -and
            $testName -eq "$($consumer.symbol)Tests") {
            [void]$owningGraphTests.Add([string]$consumer.path)
        }
    }
}
$required = @(
    $(if (@($scope | Where-Object { $_.Replace('\', '/') -eq '.github/workflows/ci-tests.yml' }).Count -gt 0) { 'Tooling/tests/FoodDiary.ArchitectureTests/BuildWorkflowGuardrailTests.cs' })
    @($scoped | Sort-Object)
) | Select-Object -First $Limit
$recommended = @(
    $graphConsumers |
        Where-Object { -not $scoped.Contains($_) } |
        Sort-Object @{ Expression = { $owningGraphTests.Contains($_) }; Descending = $true }, @{ Expression = { $directoryAffinity[$_] }; Descending = $true }, @{ Expression = { $_ } } |
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
