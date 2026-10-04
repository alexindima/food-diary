[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
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
