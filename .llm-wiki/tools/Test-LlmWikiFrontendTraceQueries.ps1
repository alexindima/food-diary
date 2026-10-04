[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$tool = Join-Path $PSScriptRoot 'Find-LlmWikiFrontendTrace.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for frontend trace parity.' }
$null = & $manager -Action build -Format Json

$cases = @(
    @{ Query = 'AiPhotoPreviewComponent'; Limit = 1 }
    @{ Query = 'autocomplete'; Limit = 3 }
    @{ Query = 'FrontendObservabilityService'; Limit = 1 }
    @{ Query = 'frontend trace JSON SQLite'; Limit = 3 }
    @{ Query = 'FdUiSelectComponent'; Limit = 1 }
    @{ Query = 'DashboardComponent'; Limit = 1 }
    @{ Query = 'AuthService'; Limit = 1 }
    @{ Query = 'zyxwv no frontend symbol'; Limit = 3 }
    @{ Query = 'zznosuchfrontendsymbol92841'; Limit = 3 }
)
$sqlEndToEnd = [Collections.Generic.List[double]]::new()

$sqlRoute = [Collections.Generic.List[double]]::new()
$reducedCases = 0
foreach ($case in $cases) {
    $arguments = @{ Query = $case.Query; Limit = $case.Limit; Format = 'Json' }
    $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $sqlite = & $tool @arguments | ConvertFrom-Json
    $sqlStopwatch.Stop()
    if ([string]$sqlite.compiledIndex.source -cne 'sqlite-compiled-trace') {
        throw "$($case.Query): default frontend trace route did not use SQLite."
    }
    if (@($sqlite.traces).Count -gt $case.Limit -or [string]$sqlite.query -cne $case.Query) { throw 'Frontend trace lost query identity or output bounds.' }
    if ($case.Query -eq 'zznosuchfrontendsymbol92841' -and ($sqlite.matched -or @($sqlite.traces).Count)) { throw 'Unknown frontend symbol returned unrelated traces.' }
    if ($case.Query -in @('AiPhotoPreviewComponent','FrontendObservabilityService','FdUiSelectComponent','DashboardComponent','AuthService') -and
        (-not $sqlite.matched -or $sqlite.traces[0].symbol.name -cne $case.Query)) { throw "Exact frontend symbol was not ranked first: $($case.Query)" }
    if ([int]$sqlite.compiledIndex.returnedRecords -lt [int]$sqlite.compiledIndex.scannedRecords) { $reducedCases++ }
    $sqlRoute.Add([double]$sqlite.compiledIndex.roundTripDurationMs)
    $sqlEndToEnd.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
}
if ($reducedCases -ne $cases.Count) {
    throw "SQLite frontend trace payload filtering reduced records for only $reducedCases/$($cases.Count) parity cases."
}
function Get-NormalizedSourceHash([string]$Path) {
    $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
$expectedFrontendHash = Get-NormalizedSourceHash (Join-Path $repositoryRoot '.llm-wiki/generated/frontend-index.json')
$expectedContractHash = Get-NormalizedSourceHash (Join-Path $repositoryRoot '.llm-wiki/generated/frontend-contract-index.json')
$hashProbe = & $tool -Query $cases[0].Query -Limit 1 -Format Json | ConvertFrom-Json
if ([string]$hashProbe.compiledIndex.sourceHashes.frontend -cne $expectedFrontendHash -or
    [string]$hashProbe.compiledIndex.sourceHashes.frontendContract -cne $expectedContractHash) {
    throw 'SQLite frontend trace source hashes do not match the current generated indexes.'
}
$sqlRouteAverage = [Math]::Round(($sqlRoute | Measure-Object -Average).Average, 2)
$sqlEndToEndAverage = [Math]::Round(($sqlEndToEnd | Measure-Object -Average).Average, 2)
Write-Host 'LLM Wiki FrontendTrace SQLite behavior checks passed.'
