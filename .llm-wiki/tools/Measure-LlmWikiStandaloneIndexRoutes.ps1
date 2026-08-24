[CmdletBinding()]
param(
    [ValidateRange(2, 20)]
    [int]$Iterations = 5,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$graphScript = Join-Path $PSScriptRoot 'code-graph.mjs'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$null = & $manager -Action build -Format Json
$cases = @(
    [pscustomobject]@{
        name = 'runtime-topology'
        sourcePath = '.llm-wiki/generated/runtime-topology.json'
        tool = Join-Path $PSScriptRoot 'Find-LlmWikiRuntimeTopology.ps1'
        jsonArguments = @{ Query = 'MailRelay'; Format = 'Json' }
        sqliteArguments = $null
        sqliteRoute = 'node-process-shadow'
        category = 'runtime'
        query = 'MailRelay'
        projectionCoverageComplete = $true
    }
    [pscustomobject]@{
        name = 'domain-data'
        sourcePath = '.llm-wiki/generated/domain-data-index.json'
        tool = Join-Path $PSScriptRoot 'Find-LlmWikiDomainData.ps1'
        jsonArguments = @{ View = 'invariants'; Query = 'weight'; CompiledIndexSource = 'Json'; Format = 'Json' }
        sqliteArguments = @{ View = 'invariants'; Query = 'weight'; Format = 'Json' }
        sqliteRoute = 'in-process-exact'
        category = 'domain'
        query = 'weight'
        projectionCoverageComplete = $true
    }
    [pscustomobject]@{
        name = 'architecture-health'
        sourcePath = '.llm-wiki/generated/architecture-health-index.json'
        tool = Join-Path $PSScriptRoot 'Find-LlmWikiArchitectureHealth.ps1'
        jsonArguments = @{ View = 'spec-gaps'; Query = 'component'; Format = 'Json' }
        sqliteArguments = $null
        sqliteRoute = 'node-process-shadow'
        category = 'architecture-health'
        query = 'component'
        projectionCoverageComplete = $false
    }
)

$measurements = foreach ($case in $cases) {
    $jsonDurations = [Collections.Generic.List[double]]::new()
    $sqliteDurations = [Collections.Generic.List[double]]::new()
    for ($iteration = 0; $iteration -lt $Iterations; $iteration++) {
        $jsonArguments = $case.jsonArguments
        $measureJson = {
            & $case.tool @jsonArguments | Out-Null
            if (-not $?) { throw "JSON baseline failed for $($case.name)." }
        }
        $measureSqlite = {
            if ($case.sqliteRoute -eq 'in-process-exact') {
                $sqliteArguments = $case.sqliteArguments
                & $case.tool @sqliteArguments | Out-Null
                if (-not $?) { throw "In-process SQLite query failed for $($case.name)." }
            } else {
                & node $graphScript query "--category=$($case.category)" "--query=$($case.query)" '--limit=30' | Out-Null
                if ($LASTEXITCODE -ne 0) { throw "SQLite shadow query failed for $($case.name)." }
            }
        }
        if (($iteration % 2) -eq 0) {
            $jsonDurations.Add((Measure-Command $measureJson).TotalMilliseconds)
            $sqliteDurations.Add((Measure-Command $measureSqlite).TotalMilliseconds)
        } else {
            $sqliteDurations.Add((Measure-Command $measureSqlite).TotalMilliseconds)
            $jsonDurations.Add((Measure-Command $measureJson).TotalMilliseconds)
        }
    }
    $jsonAverage = [Math]::Round(($jsonDurations | Measure-Object -Average).Average, 2)
    $sqliteAverage = [Math]::Round(($sqliteDurations | Measure-Object -Average).Average, 2)
    $recommendation = if ($case.sqliteRoute -eq 'in-process-exact' -and $sqliteAverage -lt $jsonAverage) {
        'keep-in-process-sqlite'
    } elseif ($case.sqliteRoute -eq 'in-process-exact') {
        'investigate-in-process-regression'
    } elseif (-not $case.projectionCoverageComplete) {
        'retain-json-projection-incomplete'
    } elseif ($sqliteAverage -ge $jsonAverage) {
        'retain-json-process-boundary-regression'
    } else {
        'candidate-for-exact-parity'
    }
    [pscustomobject][ordered]@{
        index = $case.name
        sourceBytes = (Get-Item -LiteralPath (Join-Path $repositoryRoot $case.sourcePath)).Length
        jsonAverageMs = $jsonAverage
        sqliteRoute = [string]$case.sqliteRoute
        sqliteAverageMs = $sqliteAverage
        sqliteDeltaMs = [Math]::Round($sqliteAverage - $jsonAverage, 2)
        projectionCoverageComplete = [bool]$case.projectionCoverageComplete
        recommendation = $recommendation
    }
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 2
    iterations = $Iterations
    alreadySqlite = @('quality-index', 'domain-data')
    measurements = @($measurements)
    caveat = 'Domain-data measures its exact in-process default. Runtime and architecture-health remain process-boundary shadows and may switch only after dedicated exact-parity tests pass.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6; exit 0 }
Write-Host "Standalone index route telemetry ($Iterations alternating iteration(s)):"
foreach ($item in $measurements) {
    Write-Host " - $($item.index): JSON=$($item.jsonAverageMs)ms, SQLite=$($item.sqliteAverageMs)ms, route=$($item.sqliteRoute), delta=$($item.sqliteDeltaMs)ms, recommendation=$($item.recommendation)"
}
Write-Host "Caveat: $($result.caveat)"
