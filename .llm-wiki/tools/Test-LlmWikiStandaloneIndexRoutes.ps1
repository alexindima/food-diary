[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$measurement = & (Join-Path $PSScriptRoot 'Measure-LlmWikiStandaloneIndexRoutes.ps1') -Iterations 2 -Format Json | ConvertFrom-Json
if ([int]$measurement.schemaVersion -ne 2 -or [int]$measurement.iterations -ne 2) {
    throw 'Standalone-index telemetry schema is invalid.'
}
if (@($measurement.measurements).Count -ne 3 -or @($measurement.alreadySqlite) -notcontains 'quality-index' -or
    @($measurement.alreadySqlite) -notcontains 'domain-data') {
    throw 'Standalone-index telemetry does not cover the expected migration routes.'
}
foreach ($item in @($measurement.measurements)) {
    if ([int64]$item.sourceBytes -le 0 -or [double]$item.jsonAverageMs -le 0 -or [double]$item.sqliteAverageMs -le 0 -or
        [string]::IsNullOrWhiteSpace([string]$item.sqliteRoute) -or
        [string]::IsNullOrWhiteSpace([string]$item.recommendation)) {
        throw "Standalone-index telemetry is incomplete for $($item.index)."
    }
}
$architecture = @($measurement.measurements | Where-Object index -eq 'architecture-health' | Select-Object -First 1)
if ($architecture.Count -ne 1 -or [bool]$architecture[0].projectionCoverageComplete -or
    [string]$architecture[0].recommendation -ne 'retain-json-projection-incomplete') {
    throw 'Architecture-health standalone projection coverage is overstated.'
}
Write-Host 'LLM Wiki standalone-index route telemetry passed: domain-data uses exact in-process SQLite; 2 remaining routes retain process-boundary shadows.'
