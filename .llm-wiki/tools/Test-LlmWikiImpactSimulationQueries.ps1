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
foreach ($case in $cases) {
    $arguments = @{
        Action = 'simulate'
        Objective = $case.Objective
        ProposedPath = [string[]]@($case.ProposedPath)
        Format = 'Json'
    }
    # Warm shared change-packet query caches before measuring the feature-catalog source.
    $null = & $simulationTool @arguments | ConvertFrom-Json
    $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $sqlite = & $simulationTool @arguments | ConvertFrom-Json
    $sqlStopwatch.Stop()

    if ([string]$sqlite.alignment.status -ne [string]$case.ExpectedStatus) {
        throw "$($case.Objective): expected alignment '$($case.ExpectedStatus)', got '$($sqlite.alignment.status)'."
    }
    $sqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
}

$probeArguments = @{
    Action = 'simulate'
    Objective = 'Improve dashboard layout'
    ProposedPath = @('FoodDiary.Web.Client/src/app/features/dashboard/pages/_dashboard-shell.scss')
    IncludeDiagnostics = $true
    Format = 'Json'
}
$probe = & $simulationTool @probeArguments | ConvertFrom-Json

$source = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/generated/frontend-index.json') -Raw | ConvertFrom-Json
$sourceText = [IO.File]::ReadAllText((Join-Path $repositoryRoot '.llm-wiki/generated/frontend-index.json')).Replace("`r`n", "`n")
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($sourceText))).ToLowerInvariant()
$sqlDiagnostics = $probe._diagnostics.frontendFeatures

if ([string]$sqlDiagnostics.source -ne 'sqlite-compiled-index-reused' -or
    [string]$sqlDiagnostics.sourceHash -cne $sourceHash -or
    [int]$sqlDiagnostics.sourceRecords -ne @($source.features).Count -or
    [int64]$sqlDiagnostics.sourceBytesMaterialized -ge [int64]$sqlDiagnostics.sourceBytesVerified -or
    [double]$sqlDiagnostics.incrementalRoundTripDurationMs -gt 25) {
    throw 'Impact-simulation SQLite feature catalog is stale, incomplete, or was not reused from the existing change-packet round trip.'
}

$sqlAverage = [Math]::Round(($sqlDurations | Measure-Object -Average).Average, 2)
Write-Host 'LLM Wiki ImpactSimulation SQLite behavior checks passed.'
