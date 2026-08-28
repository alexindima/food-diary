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
)
$sqlEndToEnd = [Collections.Generic.List[double]]::new()
$jsonEndToEnd = [Collections.Generic.List[double]]::new()
$sqlRoute = [Collections.Generic.List[double]]::new()
$reducedCases = 0
$caseIndex = 0

foreach ($case in $cases) {
    $arguments = @{ Query = $case.Query; Limit = $case.Limit; Format = 'Json' }
    if (($caseIndex % 2) -eq 0) {
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $tool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $tool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
    } else {
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $tool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $tool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
    }
    if ([string]$sqlite.compiledIndex.source -cne 'sqlite-compiled-trace') {
        throw "$($case.Query): default frontend trace route did not use SQLite."
    }
    $sqliteFunctional = $sqlite | Select-Object matched, query, traces | ConvertTo-Json -Depth 12 -Compress
    $jsonFunctional = $json | Select-Object matched, query, traces | ConvertTo-Json -Depth 12 -Compress
    if ($sqliteFunctional -cne $jsonFunctional) {
        throw "$($case.Query): SQLite/JSON frontend trace output parity failed."
    }
    if ([int]$sqlite.compiledIndex.returnedRecords -lt [int]$sqlite.compiledIndex.scannedRecords) { $reducedCases++ }
    $sqlRoute.Add([double]$sqlite.compiledIndex.roundTripDurationMs)
    $sqlEndToEnd.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
    $jsonEndToEnd.Add($jsonStopwatch.Elapsed.TotalMilliseconds)
    $caseIndex++
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
$jsonEndToEndAverage = [Math]::Round(($jsonEndToEnd | Measure-Object -Average).Average, 2)
Write-Host "LLM Wiki frontend trace SQL parity passed: $($cases.Count)/$($cases.Count) cases; SQL route=${sqlRouteAverage}ms; end-to-end SQL=${sqlEndToEndAverage}ms/JSON=${jsonEndToEndAverage}ms; payload reduction=$reducedCases/$($cases.Count)."
