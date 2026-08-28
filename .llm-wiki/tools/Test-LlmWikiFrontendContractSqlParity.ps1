[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$queryTool = Join-Path $PSScriptRoot 'Find-LlmWikiFrontendContract.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for frontend-contract parity.' }
$null = & $manager -Action build -Format Json

$cases = @(
    [pscustomobject]@{ View = 'all'; Query = ''; Limit = 30; Minimum = 120 }
    [pscustomobject]@{ View = 'components'; Query = 'Autocomplete'; Limit = 30; Minimum = 1 }
    [pscustomobject]@{ View = 'consumers'; Query = 'fd-ui-autocomplete'; Limit = 30; Minimum = 1 }
    [pscustomobject]@{ View = 'api'; Query = 'linkGoogle'; Limit = 30; Minimum = 1 }
    [pscustomobject]@{ View = 'translations'; Query = 'profile'; Limit = 30; Minimum = 1 }
    [pscustomobject]@{ View = 'spec-gaps'; Query = ''; Limit = 30; Minimum = 30 }
    [pscustomobject]@{ View = 'components'; Query = 'AiPhotoPreview'; Limit = 10; Minimum = 1 }
    [pscustomobject]@{ View = 'api'; Query = 'google/link'; Limit = 10; Minimum = 1 }
)
$sqlDurations = [Collections.Generic.List[double]]::new()
$jsonDurations = [Collections.Generic.List[double]]::new()
$caseIndex = 0

foreach ($case in $cases) {
    $arguments = @{ View = $case.View; Query = $case.Query; Limit = $case.Limit; Format = 'Json' }
    if (($caseIndex % 2) -eq 0) {
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $queryTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $queryTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
    } else {
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $queryTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $queryTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
    }

    if (($sqlite | ConvertTo-Json -Depth 12 -Compress) -cne ($json | ConvertTo-Json -Depth 12 -Compress)) {
        throw "$($case.View)/$($case.Query): SQLite/JSON frontend-contract parity failed."
    }
    $returnedCount = 0
    foreach ($property in $sqlite.PSObject.Properties) { $returnedCount += @($property.Value).Count }
    if ($returnedCount -lt [int]$case.Minimum) {
        throw "$($case.View)/$($case.Query): frontend-contract parity was vacuous; expected at least $($case.Minimum) record(s), got $returnedCount."
    }
    $sqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
    $jsonDurations.Add($jsonStopwatch.Elapsed.TotalMilliseconds)
    $caseIndex++
}

$probe = & $manager -Action frontend-contract -FrontendContractView all -Limit 30 -SkipRefresh -Format Json | ConvertFrom-Json
$sourceText = [IO.File]::ReadAllText((Join-Path $repositoryRoot '.llm-wiki/generated/frontend-contract-index.json')).Replace("`r`n", "`n")
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($sourceText))).ToLowerInvariant()
if (-not [bool]$probe.ready -or [string]$probe.source -ne 'sqlite-query-documents' -or
    [string]$probe.sourceHash -cne $sourceHash -or [int]$probe.returnedRecords -ge [int]$probe.scannedRecords) {
    throw 'Frontend-contract SQLite projection is not current or did not reduce the transported payload.'
}

$sqlAverage = [Math]::Round(($sqlDurations | Measure-Object -Average).Average, 2)
$jsonAverage = [Math]::Round(($jsonDurations | Measure-Object -Average).Average, 2)
Write-Host "LLM Wiki frontend-contract SQL parity passed: $($cases.Count)/$($cases.Count) cases; SQL=${sqlAverage}ms, JSON=${jsonAverage}ms average; returned=$($probe.returnedRecords)/$($probe.scannedRecords)."
