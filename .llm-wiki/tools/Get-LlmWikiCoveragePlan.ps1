[CmdletBinding()]
param(
    [Alias('PlannedPath')][string[]]$ProposedPath,
    [string]$Query,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$paths = @($ProposedPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { ([string]$_).Replace('\', '/') } | Sort-Object -Unique)
if ($paths.Count -eq 0) { throw "coverage-plan requires -PlannedPath for the test or production source under investigation." }

function Find-NearestProject([string]$RelativePath) {
    $candidate = Join-Path $repositoryRoot $RelativePath
    $directory = if (Test-Path -LiteralPath $candidate -PathType Leaf) { Split-Path -Parent $candidate } else { $candidate }
    while ($directory -and $directory.StartsWith($repositoryRoot, [StringComparison]::OrdinalIgnoreCase)) {
        $project = Get-ChildItem -LiteralPath $directory -Filter '*.csproj' -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($project) { return $project.FullName }
        $parent = Split-Path -Parent $directory
        if ($parent -eq $directory) { break }
        $directory = $parent
    }
    return $null
}

$testSource = @($paths | Where-Object { $_ -match '(^|/)tests/|Tests?\.cs$' } | Select-Object -First 1)
$testProject = if ($testSource.Count -gt 0) { Find-NearestProject $testSource[0] } else { $null }
if (-not $testProject) {
    throw 'coverage-plan could not infer a test project. Include the exact test file in -PlannedPath.'
}
$testProjectRelative = $testProject.Substring($repositoryRoot.Length).TrimStart('\', '/').Replace('\', '/')
$sourceText = if ($testSource.Count -gt 0 -and (Test-Path -LiteralPath (Join-Path $repositoryRoot $testSource[0]) -PathType Leaf)) { [IO.File]::ReadAllText((Join-Path $repositoryRoot $testSource[0])) } else { '' }
$namespace = if ($sourceText -match '(?m)^namespace\s+([^;\s]+)') { $Matches[1] } else { '' }
$class = if ($sourceText -match '(?m)\bclass\s+([A-Za-z_][A-Za-z0-9_]*)') { $Matches[1] } else { '' }
$fullyQualifiedFilter = @($namespace, $class | Where-Object { $_ }) -join '.'
$testFilter = if ($fullyQualifiedFilter) { " --filter `"FullyQualifiedName~$fullyQualifiedFilter`"" } else { '' }
$resultRoot = '.artifacts/coverage-investigation'
$targetArguments = "test `"$testProjectRelative`" --no-restore$testFilter"
$isIntegration = $testProjectRelative -match '(?i)IntegrationTests'
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    scope = [pscustomobject][ordered]@{ paths = $paths; query = $Query; testProject = $testProjectRelative; testFilter = $fullyQualifiedFilter; integration = $isIntegration }
    commands = [pscustomobject][ordered]@{
        focusedTest = "dotnet test `"$testProjectRelative`" --no-restore$testFilter"
        xplatCoverage = "dotnet test `"$testProjectRelative`" --no-restore$testFilter --settings coverage.runsettings --collect:`"XPlat Code Coverage`" --results-directory `"$resultRoot/xplat`""
        dotCover = "dotCover cover --target-executable=dotnet --target-arguments='$targetArguments' --target-working-directory='$repositoryRoot' --output='$resultRoot/dotcover.dcvr' --report-type=DetailedXML --exclude-assemblies='*.Tests;*.IntegrationTests'"
    }
    notes = @(
        'Run from the repository root; target-working-directory is explicit so dotCover resolves project and settings paths consistently.'
        'Build or restore first when --no-restore is not valid for the current checkout.'
        $(if ($isIntegration) { 'This is an integration-test project: start its required infrastructure and preserve its normal environment/configuration.' } else { 'The test filter is derived from the supplied test type; remove it only when wider project coverage is intentional.' })
        'Coverage proves execution, not assertion quality; inspect the uncovered production lines and mutation/branch behavior separately.'
    )
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 8; return }
Write-Host "Coverage plan: $testProjectRelative$(if ($fullyQualifiedFilter) { " [$fullyQualifiedFilter]" } else { '' })"
Write-Host "Focused: $($result.commands.focusedTest)"
Write-Host "XPlat:   $($result.commands.xplatCoverage)"
Write-Host "dotCover: $($result.commands.dotCover)"
foreach ($note in $result.notes) { Write-Host " - $note" }
