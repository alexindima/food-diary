[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$diffTool = Join-Path $PSScriptRoot 'Get-LlmWikiDiffContext.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for diff-context parity.' }
$null = & $manager -Action build -Format Json

# The entire current backend uses extracted modules; diff navigation must keep
# declared business edges and module-owned tests even without legacy roots.
$layoutInput = [pscustomobject]@{
    ready = $true; source = 'sqlite-compiled-index'; selectionMode = 'context'
    catalog = [pscustomobject]@{
        applicationModules = @()
        extractedApplicationModules = @([pscustomobject]@{ name = 'Admin'; project = 'Modules/Admin/Application/FoodDiary.Modules.Admin.Application.csproj' })
        dotnet = [pscustomobject]@{ projects = @() }
        knowledgeSources = [pscustomobject]@{ agentGuides = @('AGENTS.md') }
    }
    symbols = @(); frontendSymbols = @(); sourceHashes = [pscustomobject]@{}
    scannedRecords = 0; returnedRecords = 0; durationMs = 0
}
$layoutDiff = & $diffTool -ChangedPath 'Modules/Admin/Application/Queries' -CompiledIndexInput $layoutInput -Format Json -Limit 12 | ConvertFrom-Json
$moduleGraph = Get-Content -LiteralPath (Join-Path $repositoryRoot 'docs/architecture/module-dependencies.json') -Raw | ConvertFrom-Json
$layoutFailures = [Collections.Generic.List[string]]::new()
if ((@($layoutDiff.modules[0].dependencies | Sort-Object) -join '|') -cne
    (@($moduleGraph.modules.Admin | Sort-Object) -join '|')) {
    $layoutFailures.Add('Extracted module diff navigation lost its declared business dependencies.')
}
if (@($layoutDiff.focusedTests | Where-Object { $_ -match '^Modules/Admin/tests/.+\.cs$' }).Count -eq 0) {
    $layoutFailures.Add('Diff navigation missed current module-owned focused test sources.')
}
if ($layoutFailures.Count -gt 0) { throw ($layoutFailures -join ' ') }

$cases = @(
    [pscustomobject]@{ ChangedPath = @('Modules/Users/Application/Commands/UpdateUser/UpdateUserCommandHandler.cs'); MinimumSymbols = 1 }
    [pscustomobject]@{ ChangedPath = @('Modules/Fasting/Presentation/Controllers/FastingController.cs'); MinimumSymbols = 1 }
    [pscustomobject]@{ ChangedPath = @(
        'Modules/Users/Application/Commands/UpdateUser/UpdateUserCommandHandler.cs'
        'Modules/Fasting/Presentation/Controllers/FastingController.cs'
        'FoodDiary.Web.Api/appsettings.Production.json'
    ); MinimumSymbols = 2 }
    [pscustomobject]@{ ChangedPath = @('Tooling/tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs'); MinimumSymbols = 0 }
    [pscustomobject]@{ ChangedPath = @(
        'Modules/Users/Application/FoodDiary.Modules.Users.Application.csproj'
        'docs/ARCHITECTURE.md'
    ); MinimumSymbols = 0 }
    [pscustomobject]@{ ChangedPath = @('FoodDiary.Web.Client/projects/fd-tour/src/lib/fd-tour-host.ts'); MinimumSymbols = 0; MinimumFrontendSymbols = 1 }
)
$sqlRoundTrips = [Collections.Generic.List[double]]::new()

$sqlEndToEnd = [Collections.Generic.List[double]]::new()

$reducedCases = 0
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
    $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $sqlite = & $diffTool @arguments | ConvertFrom-Json
    $sqlStopwatch.Stop()

    if ([string]$sqlite.compiledIndex.source -ne 'sqlite-compiled-index' -or
        [string]$sqlite.compiledIndex.selectionMode -ne 'changed-paths') {
        throw "$($changedPaths -join ', '): default diff route did not use changed-path SQLite selection."
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

    $sqlEndToEnd.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
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

function Get-Median([Collections.Generic.List[double]]$Durations) {
    $ordered = @($Durations | Sort-Object)
    $middle = [int]($ordered.Count / 2)
    if (($ordered.Count % 2) -eq 0) { return ($ordered[$middle - 1] + $ordered[$middle]) / 2 }
    return $ordered[$middle]
}
$sqlMedian = [Math]::Round((Get-Median $sqlRoundTrips), 2)

$sqlEndToEndMedian = [Math]::Round((Get-Median $sqlEndToEnd), 2)
Write-Host 'LLM Wiki DiffContext SQLite behavior checks passed.'
