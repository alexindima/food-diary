[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('fast', 'integration-1', 'integration-2', 'integration-3', 'mcp')]
    [string]$Group,
    [string]$PlanPath = (Join-Path $PSScriptRoot 'backend-test-groups.json'),
    [switch]$PlanOnly
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$plan = Get-Content -LiteralPath $PlanPath -Raw | ConvertFrom-Json
if ($plan.schemaVersion -ne 1) { throw 'Unsupported backend test plan schema.' }

# An explicit partition makes new or moved test projects a reviewable CI change.
# No name/category filters: every test in every registered project runs once.
[xml]$solution = Get-Content -LiteralPath (Join-Path $repositoryRoot 'FoodDiary.slnx') -Raw
$expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($node in $solution.SelectNodes('//Project')) {
    $path = [string]$node.Path
    [xml]$project = Get-Content -LiteralPath (Join-Path $repositoryRoot $path) -Raw
    if ($project.SelectSingleNode('//PackageReference[@Include="Microsoft.NET.Test.Sdk"]')) {
        [void]$expected.Add($path)
    }
}
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$groupNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$requiredGroups = @('fast', 'integration-1', 'integration-2', 'integration-3', 'mcp')
foreach ($entry in $plan.groups) {
    if ($entry.name -cnotin $requiredGroups -or -not $groupNames.Add($entry.name)) {
        throw "Unknown or duplicate test group: $($entry.name)"
    }
    if (@($entry.projects).Count -eq 0) { throw "Empty test group: $($entry.name)" }
    foreach ($path in $entry.projects) {
        if (-not $expected.Contains($path)) { throw "Unknown test project: $path" }
        if (-not $seen.Add($path)) { throw "Duplicate test project: $path" }
    }
}
if ($groupNames.Count -ne $requiredGroups.Count) { throw 'A required backend test group is missing.' }
if (-not $seen.SetEquals($expected)) {
    throw "Unassigned test projects: $(($expected | Where-Object { -not $seen.Contains($_) }) -join ', ')"
}
$selected = @(($plan.groups | Where-Object name -CEQ $Group).projects)
if ($PlanOnly) {
    [pscustomobject]@{ Group = $Group; Projects = $selected; TotalProjects = $seen.Count }
    return
}

$outputRoot = Join-Path $repositoryRoot ".artifacts/backend-ci/$Group"
$logsRoot = Join-Path $outputRoot 'logs'
$resultsRoot = Join-Path $outputRoot 'results'
[void](New-Item -ItemType Directory -Force -Path $logsRoot, $resultsRoot)
$results = [Collections.Generic.List[object]]::new()
$failure = $null
$activeLog = ''
$stage = 'prepare'

function Invoke-DotnetStep([string[]]$Arguments, [string]$LogPath) {
    Write-Host "dotnet $($Arguments -join ' ')"
    & dotnet @Arguments 2>&1 | Tee-Object -FilePath $LogPath | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE; see $LogPath" }
}

Push-Location $repositoryRoot
try {
    # Build only the selected projects and their transitive dependencies, once.
    # Keep all outputs at repository level, away from other local groups/servers.
    $groupSolution = Join-Path $outputRoot 'tests.slnx'
    $xml = [xml]'<Solution />'
    $buildProjects = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    function Add-BuildProject([string]$Path) {
        $fullPath = [IO.Path]::GetFullPath($Path)
        if (-not $buildProjects.Add($fullPath)) { return }
        [xml]$definition = Get-Content -LiteralPath $fullPath -Raw
        foreach ($reference in $definition.SelectNodes('//ProjectReference')) {
            $referencePath = ([string]$reference.Include).Replace('\', '/')
            Add-BuildProject (Join-Path (Split-Path $fullPath) $referencePath)
        }
    }
    foreach ($path in $selected) { Add-BuildProject (Join-Path $repositoryRoot $path) }
    # Directory.Build.props adds this reference to all projects. Include the
    # complete closure in the solution so MSBuild maps dependencies to Release,
    # rather than falling back to Debug for projects outside its configuration.
    Add-BuildProject (Join-Path $repositoryRoot 'FoodDiary.Analyzers/FoodDiary.Analyzers.csproj')
    foreach ($path in ($buildProjects | Sort-Object)) {
        $node = $xml.CreateElement('Project')
        $node.SetAttribute('Path', $path)
        [void]$xml.DocumentElement.AppendChild($node)
    }
    $xml.Save($groupSolution)
    $buildRoot = Join-Path $outputRoot 'build'
    $stage = 'restore'
    $activeLog = Join-Path $logsRoot "$Group-restore.log"
    Invoke-DotnetStep @('restore', $groupSolution, '--locked-mode', '--artifacts-path', $buildRoot) $activeLog
    $stage = 'build'
    $activeLog = Join-Path $logsRoot "$Group-build.log"
    Invoke-DotnetStep @('build', $groupSolution, '-c', 'Release', '--no-restore', '--artifacts-path', $buildRoot) $activeLog

    foreach ($path in $selected) {
        $name = [IO.Path]::GetFileNameWithoutExtension($path)
        $stage = $name
        $activeLog = Join-Path $logsRoot "$Group-$name.log"
        $trxName = "$Group-$name-$([Guid]::NewGuid().ToString('N')).trx"
        $timer = [Diagnostics.Stopwatch]::StartNew()
        $testFailure = $null
        try {
            Invoke-DotnetStep @(
                'test', $path, '-c', 'Release', '--no-build', '--no-restore',
                '--artifacts-path', $buildRoot, '--logger', "trx;LogFileName=$trxName",
                '--results-directory', $resultsRoot
            ) $activeLog
        } catch { $testFailure = $_ }
        $timer.Stop()
        $trxPath = Join-Path $resultsRoot $trxName
        if (-not (Test-Path -LiteralPath $trxPath)) { throw "No TRX result for $path. $testFailure" }
        [xml]$trx = Get-Content -LiteralPath $trxPath -Raw
        $counters = $trx.TestRun.ResultSummary.Counters
        $results.Add([pscustomobject]@{
            Project = $path; Seconds = [math]::Round($timer.Elapsed.TotalSeconds, 2)
            Total = [int]$counters.total; Passed = [int]$counters.passed
            Failed = [int]$counters.failed; Skipped = [int]$counters.notExecuted
        })
        if ($testFailure) { throw $testFailure }
        if ([int]$counters.total -eq 0 -or [int]$counters.executed -eq 0) {
            throw "No tests executed for $path."
        }
        if ([int]$counters.failed -ne 0 -or $trx.TestRun.ResultSummary.outcome -ne 'Completed') {
            throw "Unsuccessful test results for $path."
        }
    }
} catch {
    $failure = $_
    @(
        'FAILED_AREA=dotnet'
        "FAILED_TARGET=Backend $Group / $stage"
        "FAILED_LOG_FILE_NAME=$([IO.Path]::GetFileName($activeLog))"
    ) | Set-Content -LiteralPath (Join-Path $logsRoot "$Group-failure-meta.env")
} finally {
    Pop-Location
    ConvertTo-Json -InputObject @($results.ToArray()) -Depth 5 |
        Set-Content -LiteralPath (Join-Path $logsRoot "$Group-durations.json")
    $summary = @(
        "## Backend $Group"
        ''
        "Completed $($results.Count) of $($selected.Count) projects."
        ''
        '| Project | Seconds | Passed | Failed | Skipped |'
        '| --- | ---: | ---: | ---: | ---: |'
    )
    $summary += @($results | ForEach-Object {
        "| $($_.Project) | $($_.Seconds) | $($_.Passed) | $($_.Failed) | $($_.Skipped) |"
    })
    if ($failure) { $summary += "`nFailed at: $stage. See $([IO.Path]::GetFileName($activeLog))." }
    $summary | Set-Content -LiteralPath (Join-Path $logsRoot "$Group-summary.md")
    if ($env:GITHUB_STEP_SUMMARY) { $summary | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY }
}
if ($failure) { throw $failure }
