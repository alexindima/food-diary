[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$simulationTool = Join-Path $PSScriptRoot 'Manage-LlmWikiImpactSimulation.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for impact-simulation parity.' }
$null = & $manager -Action build -Format Json

$cases = @(
    [pscustomobject]@{ Objective = 'Improve dashboard layout'; ProposedPath = @('FoodDiary.Web.Client/src/app/features/dashboard/pages/_dashboard-shell.scss'); ExpectedStatus = 'aligned' }
    [pscustomobject]@{ Objective = 'Change meal dashboard behavior'; ProposedPath = @('FoodDiary.Web.Client/src/app/features/dashboard/pages/_dashboard-shell.scss'); ExpectedStatus = 'mismatch' }
    [pscustomobject]@{ Objective = 'Improve fasting flow'; ProposedPath = @('FoodDiary.Web.Client/src/app/features/fasting/fasting.routes.ts'); ExpectedStatus = 'aligned' }
    [pscustomobject]@{ Objective = 'Add photo annotation'; ProposedPath = @('FoodDiary.Web.Client/src/app/components/shared/ai-input-bar'); ExpectedStatus = 'aligned' }
)
$sqlDurations = [Collections.Generic.List[double]]::new()
$jsonDurations = [Collections.Generic.List[double]]::new()
$caseIndex = 0

foreach ($case in $cases) {
    $arguments = @{
        Action = 'simulate'
        Objective = $case.Objective
        ProposedPath = [string[]]@($case.ProposedPath)
        Format = 'Json'
    }
    # Warm shared change-packet query caches before measuring the feature-catalog source.
    $null = & $simulationTool @arguments | ConvertFrom-Json
    if (($caseIndex % 2) -eq 0) {
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $simulationTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $simulationTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
    } else {
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $simulationTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $simulationTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
    }
    if (($sqlite | ConvertTo-Json -Depth 40 -Compress) -cne ($json | ConvertTo-Json -Depth 40 -Compress)) {
        throw "$($case.Objective): SQLite/JSON impact-simulation parity failed."
    }
    if ([string]$sqlite.alignment.status -ne [string]$case.ExpectedStatus) {
        throw "$($case.Objective): expected alignment '$($case.ExpectedStatus)', got '$($sqlite.alignment.status)'."
    }
    $sqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
    $jsonDurations.Add($jsonStopwatch.Elapsed.TotalMilliseconds)
    $caseIndex++
}

$probeArguments = @{
    Action = 'simulate'
    Objective = 'Improve dashboard layout'
    ProposedPath = @('FoodDiary.Web.Client/src/app/features/dashboard/pages/_dashboard-shell.scss')
    IncludeDiagnostics = $true
    Format = 'Json'
}
$probe = & $simulationTool @probeArguments | ConvertFrom-Json
$jsonProbe = & $simulationTool @probeArguments -CompiledIndexSource Json | ConvertFrom-Json
$source = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/generated/frontend-index.json') -Raw | ConvertFrom-Json
$sourceText = [IO.File]::ReadAllText((Join-Path $repositoryRoot '.llm-wiki/generated/frontend-index.json')).Replace("`r`n", "`n")
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($sourceText))).ToLowerInvariant()
$sqlDiagnostics = $probe._diagnostics.frontendFeatures
$jsonDiagnostics = $jsonProbe._diagnostics.frontendFeatures
if ([string]$sqlDiagnostics.source -ne 'sqlite-compiled-index-reused' -or
    [string]$sqlDiagnostics.sourceHash -cne $sourceHash -or
    [int]$sqlDiagnostics.sourceRecords -ne @($source.features).Count -or
    [int64]$sqlDiagnostics.sourceBytesMaterialized -ge [int64]$sqlDiagnostics.sourceBytesVerified -or
    [double]$sqlDiagnostics.incrementalRoundTripDurationMs -gt 25) {
    throw 'Impact-simulation SQLite feature catalog is stale, incomplete, or was not reused from the existing change-packet round trip.'
}
if ([string]$jsonDiagnostics.source -ne 'json-baseline' -or
    [int64]$jsonDiagnostics.sourceBytesMaterialized -ne [int64]$jsonDiagnostics.sourceBytesVerified -or
    [int64]$jsonDiagnostics.sourceBytesVerified -ne [int64]$sqlDiagnostics.sourceBytesVerified) {
    throw 'Impact-simulation JSON baseline diagnostics are incomplete.'
}

$sqlAverage = [Math]::Round(($sqlDurations | Measure-Object -Average).Average, 2)
$jsonAverage = [Math]::Round(($jsonDurations | Measure-Object -Average).Average, 2)
$loadEnvelope = [Math]::Max(250, [Math]::Round($jsonAverage * 0.35, 2))
if ($sqlAverage -gt ($jsonAverage + $loadEnvelope)) {
    throw "SQLite impact-simulation route exceeded its noise-tolerant end-to-end parity envelope: SQL=${sqlAverage}ms, JSON=${jsonAverage}ms, envelope=${loadEnvelope}ms."
}
Write-Host "LLM Wiki impact-simulation SQL parity passed: $($cases.Count)/$($cases.Count) cases; SQL=${sqlAverage}ms, JSON=${jsonAverage}ms end-to-end, envelope=${loadEnvelope}ms; reused catalog=$($sqlDiagnostics.sourceRecords) record(s), materialized=$($sqlDiagnostics.sourceBytesMaterialized)/$($sqlDiagnostics.sourceBytesVerified) bytes, incremental=$($sqlDiagnostics.incrementalRoundTripDurationMs)ms."
