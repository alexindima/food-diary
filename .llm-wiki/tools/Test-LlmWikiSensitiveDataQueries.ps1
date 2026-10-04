[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$queryTool = Join-Path $PSScriptRoot 'Find-LlmWikiSensitiveData.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for sensitive-data parity.' }
$null = & $manager -Action build -Format Json

$cases = @(
    [pscustomobject]@{ Query = 'refresh token'; Category = 'all'; ScopePath = @(); Minimum = 30; Filtered = $true }
    [pscustomobject]@{ Query = 'photo AI'; Category = 'external'; ScopePath = @(); Minimum = 2; Filtered = $true }
    [pscustomobject]@{ Query = 'photo'; Category = 'all'; ScopePath = @('FoodDiary.Integrations/OpenAI/OpenAiImageClient.cs'); Minimum = 1; Filtered = $true }
    [pscustomobject]@{ Query = 'DietologistInvitationMessage'; Category = 'all'; ScopePath = @(); Minimum = 2; Filtered = $true }
    [pscustomobject]@{ Query = 'email identity'; Category = 'identity'; ScopePath = @(); Minimum = 1; Filtered = $true }
    [pscustomobject]@{ Query = 'weight health'; Category = 'health'; ScopePath = @(); Minimum = 1; Filtered = $true }
    [pscustomobject]@{ Query = 'amount'; Category = 'financial'; ScopePath = @(); Minimum = 1; Filtered = $true }
    [pscustomobject]@{ Query = 'image'; Category = 'privateContent'; ScopePath = @(); Minimum = 1; Filtered = $true }
    [pscustomobject]@{ Query = 'zzzxqv'; Category = 'all'; ScopePath = @(); Minimum = 0; Expected = 0; Filtered = $true }
    [pscustomobject]@{ Query = ''; Category = 'credential'; ScopePath = @(); Minimum = 30; Filtered = $false }
    [pscustomobject]@{ Query = ''; Category = 'logging'; ScopePath = @(); Minimum = 1; Filtered = $false }
    [pscustomobject]@{ Query = ''; Category = 'boundaries'; ScopePath = @(); Minimum = 30; Filtered = $false }
    [pscustomobject]@{ Query = ''; Category = 'external'; ScopePath = @(); Minimum = 2; Filtered = $false }
    [pscustomobject]@{ Query = ''; Category = 'all'; ScopePath = @(); Minimum = 0; Expected = 0; Filtered = $false }
)
$sqlDurations = [Collections.Generic.List[double]]::new()

$filteredSqlDurations = [Collections.Generic.List[double]]::new()

$unfilteredSqlDurations = [Collections.Generic.List[double]]::new()
foreach ($case in $cases) {
    $arguments = @{
        Query = $case.Query
        Category = $case.Category
        ScopePath = [string[]]@($case.ScopePath)
        NoImplicitScope = $true
        Limit = 30
        Format = 'Json'
    }
    $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $sqlite = & $queryTool @arguments | ConvertFrom-Json
    $sqlStopwatch.Stop()

    if ([int]$sqlite.count -lt [int]$case.Minimum) {
        throw "$($case.Category)/$($case.Query): sensitive-data parity was vacuous; expected at least $($case.Minimum) record(s), got $($sqlite.count)."
    }
    if ($case.PSObject.Properties['Expected'] -and [int]$sqlite.count -ne [int]$case.Expected) {
        throw "$($case.Category)/$($case.Query): expected exactly $($case.Expected) record(s), got $($sqlite.count)."
    }
    $sqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)

    if ($case.Filtered) {
        $filteredSqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)

    } else {
        $unfilteredSqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)

    }
}

$probeArguments = @{ Query = 'refresh token'; Category = 'all'; NoImplicitScope = $true; Limit = 30; IncludeDiagnostics = $true; Format = 'Json' }
$probe = & $queryTool @probeArguments | ConvertFrom-Json

$sourceText = [IO.File]::ReadAllText((Join-Path $repositoryRoot '.llm-wiki/generated/sensitive-data-index.json')).Replace("`r`n", "`n")
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($sourceText))).ToLowerInvariant()
if ([string]$probe._diagnostics.source -ne 'sqlite-sensitive-data' -or
    [string]$probe._diagnostics.sourceHash -cne $sourceHash -or
    [int]$probe._diagnostics.returnedRecords -ge [int]$probe._diagnostics.candidateRecords -or
    [int64]$probe._diagnostics.sourceBytesMaterialized -ge [int64]$probe._diagnostics.sourceBytesVerified) {
    throw 'Sensitive-data SQLite projection is stale or did not reduce its candidate payload.'
}

$sqlAverage = [Math]::Round(($sqlDurations | Measure-Object -Average).Average, 2)

$filteredSqlAverage = [Math]::Round(($filteredSqlDurations | Measure-Object -Average).Average, 2)

$unfilteredSqlAverage = [Math]::Round(($unfilteredSqlDurations | Measure-Object -Average).Average, 2)
Write-Host 'LLM Wiki SensitiveData SQLite behavior checks passed.'
