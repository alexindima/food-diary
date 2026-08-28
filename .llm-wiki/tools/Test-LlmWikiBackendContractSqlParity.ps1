[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$queryTool = Join-Path $PSScriptRoot 'Find-LlmWikiBackendContract.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for backend-contract parity.' }
$null = & $manager -Action build -Format Json

$cases = @(
    [pscustomobject]@{ View = 'all'; Query = 'User'; Minimum = 60 }
    [pscustomobject]@{ View = 'contracts'; Query = 'Invitation'; Minimum = 1 }
    [pscustomobject]@{ View = 'consumers'; Query = 'User'; Minimum = 30 }
    [pscustomobject]@{ View = 'production'; Query = 'User'; Minimum = 30 }
    [pscustomobject]@{ View = 'tests'; Query = 'User'; Minimum = 30 }
    [pscustomobject]@{ View = 'ambiguous'; Query = ''; Minimum = 0; Expected = 0 }
    [pscustomobject]@{ View = 'unconsumed'; Query = ''; Minimum = 0; Expected = 0 }
)
$sqlDurations = [Collections.Generic.List[double]]::new()
$jsonDurations = [Collections.Generic.List[double]]::new()
$caseIndex = 0

foreach ($case in $cases) {
    $arguments = @{ View = $case.View; Query = $case.Query; Limit = 30; Format = 'Json' }
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
        throw "$($case.View)/$($case.Query): SQLite/JSON backend-contract parity failed."
    }
    $returnedCount = 0
    foreach ($property in $sqlite.PSObject.Properties) { $returnedCount += @($property.Value).Count }
    if ($returnedCount -lt [int]$case.Minimum) {
        throw "$($case.View)/$($case.Query): backend-contract parity was vacuous; expected at least $($case.Minimum) record(s), got $returnedCount."
    }
    if ($case.PSObject.Properties['Expected'] -and $returnedCount -ne [int]$case.Expected) {
        throw "$($case.View)/$($case.Query): expected exactly $($case.Expected) record(s), got $returnedCount."
    }
    $sqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
    $jsonDurations.Add($jsonStopwatch.Elapsed.TotalMilliseconds)
    $caseIndex++
}

$probe = & $manager -Action backend-contract -BackendContractView all -Query User -Limit 30 -SkipRefresh -Format Json | ConvertFrom-Json
$sourceText = [IO.File]::ReadAllText((Join-Path $repositoryRoot '.llm-wiki/generated/backend-contract-index.json')).Replace("`r`n", "`n")
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($sourceText))).ToLowerInvariant()
if (-not [bool]$probe.ready -or [string]$probe.source -ne 'sqlite-query-documents' -or
    [string]$probe.sourceHash -cne $sourceHash -or [int]$probe.returnedRecords -ge [int]$probe.scannedRecords) {
    throw 'Backend-contract SQLite projection is not current or did not reduce the transported payload.'
}

$sqlAverage = [Math]::Round(($sqlDurations | Measure-Object -Average).Average, 2)
$jsonAverage = [Math]::Round(($jsonDurations | Measure-Object -Average).Average, 2)
Write-Host "LLM Wiki backend-contract SQL parity passed: $($cases.Count)/$($cases.Count) views; SQL=${sqlAverage}ms, JSON=${jsonAverage}ms average; returned=$($probe.returnedRecords)/$($probe.scannedRecords)."
