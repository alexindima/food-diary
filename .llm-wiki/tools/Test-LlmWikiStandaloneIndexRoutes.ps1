[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$measurement = & (Join-Path $PSScriptRoot 'Measure-LlmWikiStandaloneIndexRoutes.ps1') -Iterations 2 -Format Json | ConvertFrom-Json
if ([int]$measurement.schemaVersion -ne 4 -or [int]$measurement.iterations -ne 2) {
    throw 'Standalone-index telemetry schema is invalid.'
}
if (@($measurement.measurements).Count -ne 3 -or @($measurement.alreadySqlite) -notcontains 'quality-index' -or
    @($measurement.alreadySqlite) -notcontains 'domain-data') {
    throw 'Standalone-index telemetry does not cover the expected migration routes.'
}
foreach ($item in @($measurement.measurements)) {
    if ([int64]$item.sourceBytes -le 0 -or [int]$item.coldSampleCount -le 0 -or
        [double]$item.jsonColdProcessP50Ms -le 0 -or [double]$item.sqliteColdProcessP50Ms -le 0 -or
        [double]$item.jsonWarmP50Ms -le 0 -or [double]$item.jsonWarmP95Ms -le 0 -or
        [double]$item.sqliteWarmP50Ms -le 0 -or [double]$item.sqliteWarmP95Ms -le 0 -or
        [string]::IsNullOrWhiteSpace([string]$item.sqliteRoute) -or
        [string]::IsNullOrWhiteSpace([string]$item.performanceRecommendation) -or
        [string]::IsNullOrWhiteSpace([string]$item.routeDecision)) {
        throw "Standalone-index telemetry is incomplete for $($item.index)."
    }
}
$runtime = @($measurement.measurements | Where-Object index -eq 'runtime-topology' | Select-Object -First 1)
if ($runtime.Count -ne 1 -or [string]$runtime[0].routeDecision -ne 'keep-in-process-sqlite-for-unified-production-route') {
    throw 'Runtime topology lost the explicit decision to accept its measured startup tradeoff for one production route.'
}
$architecture = @($measurement.measurements | Where-Object index -eq 'architecture-health' | Select-Object -First 1)
if ($architecture.Count -ne 1 -or -not [bool]$architecture[0].projectionCoverageComplete -or
    [string]$architecture[0].sqliteRoute -ne 'in-process-exact') {
    throw 'Architecture-health standalone projection coverage is incomplete.'
}
Write-Host 'LLM Wiki standalone-index route telemetry passed: all standalone routes use exact in-process SQLite defaults with explicit cold/warm tradeoff evidence.'
