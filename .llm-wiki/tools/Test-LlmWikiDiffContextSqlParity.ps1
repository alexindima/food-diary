[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$diffTool = Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for diff-context parity.' }
$null = & $manager -Action build -Format Json

$cases = @(
    [pscustomobject]@{ ChangedPath = @('FoodDiary.Application.Users/Commands/UpdateUser/UpdateUserCommandHandler.cs'); MinimumSymbols = 1 }
    [pscustomobject]@{ ChangedPath = @('FoodDiary.Presentation.Api/Features/Fasting/FastingController.cs'); MinimumSymbols = 1 }
    [pscustomobject]@{ ChangedPath = @(
        'FoodDiary.Application.Users/Commands/UpdateUser/UpdateUserCommandHandler.cs'
        'FoodDiary.Presentation.Api/Features/Fasting/FastingController.cs'
        'FoodDiary.Web.Api/appsettings.Production.json'
    ); MinimumSymbols = 2 }
    [pscustomobject]@{ ChangedPath = @('tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs'); MinimumSymbols = 0 }
    [pscustomobject]@{ ChangedPath = @(
        'FoodDiary.Application.Users/FoodDiary.Application.Users.csproj'
        'docs/ARCHITECTURE.md'
    ); MinimumSymbols = 0 }
    [pscustomobject]@{ ChangedPath = @('FoodDiary.Web.Client/projects/fd-tour/src/lib/fd-tour-host.ts'); MinimumSymbols = 0; MinimumFrontendSymbols = 1 }
)
$sqlRoundTrips = [Collections.Generic.List[double]]::new()
$jsonRoundTrips = [Collections.Generic.List[double]]::new()
$sqlEndToEnd = [Collections.Generic.List[double]]::new()
$jsonEndToEnd = [Collections.Generic.List[double]]::new()
$reducedCases = 0
$caseIndex = 0

function ConvertTo-FunctionalJson([object]$Value) {
    $functional = [ordered]@{}
    foreach ($property in $Value.PSObject.Properties) {
        if ($property.Name -eq 'compiledIndex') { continue }
        $functional[$property.Name] = $property.Value
    }
    return $functional | ConvertTo-Json -Depth 12 -Compress
}

foreach ($case in $cases) {
    $changedPaths = @($case.ChangedPath)
    $arguments = @{
        ChangedPath = [string[]]$changedPaths
        Format = 'Json'
        Limit = 12
    }
    if (($caseIndex % 2) -eq 0) {
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $diffTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $diffTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
    } else {
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $diffTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $diffTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
    }

    if ([string]$sqlite.compiledIndex.source -ne 'sqlite-compiled-index' -or
        [string]$sqlite.compiledIndex.selectionMode -ne 'changed-paths') {
        throw "$($changedPaths -join ', '): default diff route did not use changed-path SQLite selection."
    }
    if ((ConvertTo-FunctionalJson $sqlite) -cne (ConvertTo-FunctionalJson $json)) {
        throw "$($changedPaths -join ', '): SQLite/JSON diff-context parity failed."
    }
    if (@($sqlite.changedSymbols).Count -lt [int]$case.MinimumSymbols) {
        throw "$($changedPaths -join ', '): diff-context parity was vacuous; expected at least $($case.MinimumSymbols) changed C# symbol(s)."
    }
    $minimumFrontendSymbols = if ($case.PSObject.Properties['MinimumFrontendSymbols']) { [int]$case.MinimumFrontendSymbols } else { 0 }
    if (@($sqlite.changedFrontendSymbols).Count -lt $minimumFrontendSymbols) {
        throw "$($changedPaths -join ', '): diff-context parity was vacuous; expected at least $minimumFrontendSymbols changed frontend symbol(s)."
    }
    if ([int]$sqlite.compiledIndex.candidateRecords -lt [int]$sqlite.compiledIndex.scannedRecords) {
        $reducedCases++
    }
    $sqlRoundTrips.Add([double]$sqlite.compiledIndex.roundTripDurationMs)
    $jsonRoundTrips.Add([double]$json.compiledIndex.roundTripDurationMs)
    $sqlEndToEnd.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
    $jsonEndToEnd.Add($jsonStopwatch.Elapsed.TotalMilliseconds)
    $caseIndex++
}

if ($reducedCases -ne $cases.Count) {
    throw "SQLite changed-path selection reduced symbol candidates for only $reducedCases/$($cases.Count) diff cases."
}
function Get-NormalizedSourceHash([string]$Path) {
    $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}
$catalogHash = Get-NormalizedSourceHash (Join-Path $repositoryRoot '.llm-wiki/generated/repository-catalog.json')
$symbolHash = Get-NormalizedSourceHash (Join-Path $repositoryRoot '.llm-wiki/generated/csharp-symbol-index.json')
$frontendHash = Get-NormalizedSourceHash (Join-Path $repositoryRoot '.llm-wiki/generated/frontend-index.json')
$hashProbe = & $diffTool -ChangedPath @($cases[0].ChangedPath) -Format Json | ConvertFrom-Json
if ([string]$hashProbe.compiledIndex.sourceHashes.repositoryCatalog -cne $catalogHash -or
    [string]$hashProbe.compiledIndex.sourceHashes.csharpSymbols -cne $symbolHash -or
    [string]$hashProbe.compiledIndex.sourceHashes.frontend -cne $frontendHash) {
    throw 'Diff-context SQLite source hashes do not match the current generated JSON sources.'
}

$sqlAverage = [Math]::Round(($sqlRoundTrips | Measure-Object -Average).Average, 2)
$jsonAverage = [Math]::Round(($jsonRoundTrips | Measure-Object -Average).Average, 2)
if ($sqlAverage -gt ($jsonAverage + 250)) {
    throw "SQLite diff-context transport regressed beyond the 250ms safety envelope: SQL=${sqlAverage}ms, JSON=${jsonAverage}ms."
}
$sqlEndToEndAverage = [Math]::Round(($sqlEndToEnd | Measure-Object -Average).Average, 2)
$jsonEndToEndAverage = [Math]::Round(($jsonEndToEnd | Measure-Object -Average).Average, 2)
if ($sqlEndToEndAverage -gt ($jsonEndToEndAverage + 250)) {
    throw "SQLite diff-context route regressed beyond the 250ms end-to-end safety envelope: SQL=${sqlEndToEndAverage}ms, JSON=${jsonEndToEndAverage}ms."
}
Write-Host "LLM Wiki diff-context SQL parity passed: $($cases.Count)/$($cases.Count) cases; data load SQL=${sqlAverage}ms/JSON=${jsonAverage}ms; end-to-end SQL=${sqlEndToEndAverage}ms/JSON=${jsonEndToEndAverage}ms; candidate reduction=$reducedCases/$($cases.Count)."
