[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$sourceRoot = [string]@(& git -C $PSScriptRoot rev-parse --show-toplevel)[0]
if ($LASTEXITCODE -ne 0 -or -not $sourceRoot) { throw 'Unable to resolve migration fixture source.' }
$fixtureParent = Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $sourceRoot -IndependentRepository
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $sourceRoot -Name 'migration-removal' -IndependentRepository
try {
    $tools = Join-Path $fixture '.llm-wiki/tools'
    $generated = Join-Path $fixture '.llm-wiki/generated'
    [void][IO.Directory]::CreateDirectory($tools)
    [void][IO.Directory]::CreateDirectory($generated)
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Get-LlmWikiCompiledIndexMigration.ps1') -Destination $tools
    [IO.File]::WriteAllText((Join-Path $generated 'configuration-index.json'), '{}')
    [IO.File]::WriteAllText((Join-Path $fixture 'ProjectionWriter.cs'), 'File.ReadAllText(".llm-wiki/generated/configuration-index.json");')
    $report = & (Join-Path $tools 'Get-LlmWikiCompiledIndexMigration.ps1') -Format Json | ConvertFrom-Json
    $item = @($report.indexes | Where-Object path -eq '.llm-wiki/generated/configuration-index.json')[0]
    if ($item.consumerCount -ne 0) { throw 'Migration fixture unexpectedly included the C# writer in its limited text scan.' }
    if ($item.removable -or $item.retainedAs -ne 'projection-source-and-git-review-artifact') {
        throw 'Zero limited text references incorrectly authorized deletion of a retained projection input.'
    }
} finally {
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture)) -cne [IO.Path]::GetFullPath($fixtureParent).TrimEnd('\','/')) { throw 'Unsafe migration fixture cleanup.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
$measurement = & (Join-Path $PSScriptRoot 'Measure-LlmWikiStandaloneIndexRoutes.ps1') -Iterations 2 -Format Json | ConvertFrom-Json
if ($measurement.schemaVersion -ne 5 -or $measurement.iterations -ne 2 -or @($measurement.measurements).Count -ne 3) { throw 'Invalid SQLite route telemetry schema.' }
foreach ($name in @('runtime-topology','domain-data','architecture-health')) {
    $matches = @($measurement.measurements | Where-Object index -eq $name)
    if ($matches.Count -ne 1) { throw "Missing route telemetry: $name" }
    $item = $matches[0]
    if ($item.sourceBytes -le 0 -or $item.coldSampleCount -le 0 -or $item.sqliteColdProcessP50Ms -le 0 -or
        $item.sqliteColdProcessP95Ms -lt $item.sqliteColdProcessP50Ms -or $item.sqliteWarmP50Ms -le 0 -or
        $item.sqliteWarmP95Ms -lt $item.sqliteWarmP50Ms -or $item.sqliteRoute -ne 'in-process-exact' -or
        -not $item.projectionCoverageComplete -or $item.routeDecision -ne 'sqlite-only') { throw "Incomplete SQLite telemetry: $name" }
}
Write-Host 'Standalone SQLite telemetry passed: all routes, positive cold/warm p50/p95, complete projections.'
