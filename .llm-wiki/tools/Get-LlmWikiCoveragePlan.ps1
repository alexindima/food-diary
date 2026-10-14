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

$frontendPaths = @($paths | Where-Object { $_ -match '^FoodDiary\.Web\.Client/.+\.(?:spec\.)?ts$' })
if ($frontendPaths.Count -gt 0) {
    if ($frontendPaths.Count -ne $paths.Count) { throw 'Request separate coverage plans for frontend and backend paths so both execution boundaries remain explicit.' }
    $package = Get-Content -LiteralPath (Join-Path $repositoryRoot 'FoodDiary.Web.Client/package.json') -Raw | ConvertFrom-Json
    $workspace = Get-Content -LiteralPath (Join-Path $repositoryRoot 'FoodDiary.Web.Client/angular.json') -Raw | ConvertFrom-Json
    $projects = @($workspace.projects.PSObject.Properties | Sort-Object { ([string]$_.Value.sourceRoot).Length } -Descending)
    $selected = foreach ($path in $frontendPaths) {
        $relative = $path.Substring('FoodDiary.Web.Client/'.Length)
        $project = @($projects | Where-Object { $relative.StartsWith(([string]$_.Value.sourceRoot).TrimEnd('/') + '/', [StringComparison]::Ordinal) } | Select-Object -First 1)
        if ($project.Count -ne 1 -or $project[0].Value.architect.test.builder -ne '@angular/build:unit-test') { throw "No supported Angular test project owns '$path'." }
        if (-not [IO.File]::Exists((Join-Path $repositoryRoot $path))) { throw "Coverage source does not exist: $path" }
        $script = @($package.scripts.PSObject.Properties | Where-Object { $_.Name -like 'test:coverage:*' -and [string]$_.Value -match ('^ng test ' + [regex]::Escape($project[0].Name) + '(?:\s|$)') -and [string]$_.Value -match '--configuration=coverage(?:\s|$)' } | Select-Object -First 1)
        if ($script.Count -ne 1) { throw "No package.json coverage script exists for Angular project '$($project[0].Name)'." }
        if (-not $package.scripts.PSObject.Properties[$script[0].Name.Replace('test:coverage:', 'test:ci:')]) { throw "No focused test script exists for '$($project[0].Name)'." }
        [pscustomobject]@{ path = $path; relative = $relative; project = $project[0].Name; script = $script[0].Name }
    }
    $plans = @(foreach ($group in @($selected | Group-Object project)) {
        $includes = @($group.Group | Where-Object relative -match '\.spec\.ts$' | ForEach-Object { '--include=' + $_.relative }) -join ' '
        $arguments = if ($includes) { " -- $includes" } else { '' }
        [pscustomobject][ordered]@{
            scope = [ordered]@{ paths = @($group.Group.path); testProject = $group.Name; integration = $false; framework = 'angular' }
            commands = [ordered]@{ focusedTest = "cd FoodDiary.Web.Client && npm run $($group.Group[0].script.Replace('test:coverage:', 'test:ci:'))$arguments"; coverage = "cd FoodDiary.Web.Client && npm run $($group.Group[0].script)$arguments" }
            commandEvidence = "FoodDiary.Web.Client/package.json:$($group.Group[0].script); angular.json:$($group.Name)=@angular/build:unit-test"
        }
    })
    $result = [ordered]@{
        schemaVersion = 1; scope = [ordered]@{ paths = $paths; query = $Query; framework = 'angular' }; plans = $plans
        commands = $(if ($plans.Count -eq 1) { $plans[0].commands } else { $null })
        notes = @('Run each plan from the repository root. Spec paths constrain execution with Angular --include; production-only paths retain project coverage.', 'Coverage proves execution, not assertion quality. Backend coverage is a separate request.')
    }
    if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 10; return }
    foreach ($plan in $plans) { Write-Host "Coverage plan: $($plan.scope.testProject) [Angular]"; Write-Host "Focused: $($plan.commands.focusedTest)"; Write-Host "Coverage: $($plan.commands.coverage)" }
    return
}

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
