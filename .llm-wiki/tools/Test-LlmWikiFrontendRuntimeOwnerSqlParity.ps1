[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$tool = Join-Path $PSScriptRoot 'Get-LlmWikiFrontendRuntimeOwner.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for frontend runtime-owner parity.' }
$null = & $manager -Action build -Format Json

$cases = @(
    @{ Query = 'Improve AI photo annotation layout on the dashboard result.'; CandidatePath = 'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result/ai-photo-preview/ai-photo-preview.html' }
    @{ Query = 'Move annotation labels outside the AI photo result on dashboard.'; CandidatePath = $null }
    @{ Query = 'Adjust hydration dashboard card layout'; CandidatePath = 'FoodDiary.Web.Client/src/app/features/dashboard/components/hydration-card/hydration-card.html' }
    @{ Query = 'Improve account settings provider controls'; CandidatePath = $null }
    @{ Query = 'Responsive product autocomplete results'; CandidatePath = $null }
    @{ Query = 'Dashboard widget header'; CandidatePath = 'FoodDiary.Web.Client/src/app/features/dashboard/components/dashboard-widget-header/dashboard-widget-header.ts' }
    @{ Query = 'Tune two existing cards'; CandidatePath = @('FoodDiary.Web.Client/src/app/features/dashboard/components/hydration-card/hydration-card.html', 'FoodDiary.Web.Client/src/app/components/shared/ai-input-bar/ai-photo-result/ai-photo-preview/ai-photo-preview.html') }
    @{ Query = 'Изменить карточку гидратации'; CandidatePath = $null }
    @{ Query = 'frontend surface with no matching owner zyxwv'; CandidatePath = $null }
    @{ Query = ''; CandidatePath = $null }
)
$functionalProperties = @('schemaVersion', 'query', 'candidatePaths', 'ownerCount', 'confidence', 'owners', 'note')
$sqlEndToEnd = [Collections.Generic.List[double]]::new()
$jsonEndToEnd = [Collections.Generic.List[double]]::new()
$sqlRoute = [Collections.Generic.List[double]]::new()
$jsonRoute = [Collections.Generic.List[double]]::new()
$reducedCases = 0
$caseIndex = 0

foreach ($case in $cases) {
    $arguments = @{ Query = $case.Query; Format = 'Json' }
    if ($null -ne $case.CandidatePath) { $arguments.CandidatePath = $case.CandidatePath }
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
    if ([string]$sqlite.compiledIndex.source -cne 'sqlite-query-documents') {
        throw "$($case.Query): default runtime-owner route did not use SQLite."
    }
    if ([string]$json.compiledIndex.source -cne 'json-baseline') {
        throw "$($case.Query): explicit JSON runtime-owner route did not report the baseline source."
    }
    $sqliteFunctional = $sqlite | Select-Object -Property $functionalProperties | ConvertTo-Json -Depth 12 -Compress
    $jsonFunctional = $json | Select-Object -Property $functionalProperties | ConvertTo-Json -Depth 12 -Compress
    if ($sqliteFunctional -cne $jsonFunctional) {
        throw "$($case.Query): SQLite/JSON frontend runtime-owner output parity failed."
    }
    if ([int]$sqlite.compiledIndex.returnedRecords -lt [int]$sqlite.compiledIndex.scannedRecords) { $reducedCases++ }
    $sqlRoute.Add([double]$sqlite.compiledIndex.roundTripDurationMs)
    $jsonRoute.Add([double]$json.compiledIndex.roundTripDurationMs)
    $sqlEndToEnd.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
    $jsonEndToEnd.Add($jsonStopwatch.Elapsed.TotalMilliseconds)
    $caseIndex++
}
if ($reducedCases -ne $cases.Count) {
    throw "SQLite runtime-owner payload filtering reduced records for only $reducedCases/$($cases.Count) parity cases."
}
function Get-NormalizedSourceHash([string]$Path) {
    $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
$expectedHash = Get-NormalizedSourceHash (Join-Path $repositoryRoot '.llm-wiki/generated/frontend-contract-index.json')
$hashProbe = & $tool -Query $cases[0].Query -CandidatePath $cases[0].CandidatePath -Format Json | ConvertFrom-Json
if ([string]$hashProbe.compiledIndex.sourceHash -cne $expectedHash) {
    throw 'SQLite frontend runtime-owner source hash does not match the current generated frontend-contract index.'
}
$sqlRouteAverage = [Math]::Round(($sqlRoute | Measure-Object -Average).Average, 2)
$jsonRouteAverage = [Math]::Round(($jsonRoute | Measure-Object -Average).Average, 2)
$sqlEndToEndAverage = [Math]::Round(($sqlEndToEnd | Measure-Object -Average).Average, 2)
$jsonEndToEndAverage = [Math]::Round(($jsonEndToEnd | Measure-Object -Average).Average, 2)
$loadEnvelope = [Math]::Max(500, $jsonEndToEndAverage)
if ($sqlEndToEndAverage -gt ($jsonEndToEndAverage + $loadEnvelope)) {
    throw "SQLite frontend runtime-owner exceeded its noise-tolerant end-to-end watchdog: SQL=${sqlEndToEndAverage}ms, JSON=${jsonEndToEndAverage}ms, envelope=${loadEnvelope}ms."
}
Write-Host "LLM Wiki frontend runtime-owner SQL parity passed: $($cases.Count)/$($cases.Count) cases; route SQL=${sqlRouteAverage}ms/JSON=${jsonRouteAverage}ms; end-to-end SQL=${sqlEndToEndAverage}ms/JSON=${jsonEndToEndAverage}ms, envelope=${loadEnvelope}ms; payload reduction=$reducedCases/$($cases.Count)."
