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
$shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
function Get-Percentile([double[]]$Values, [double]$Percentile) {
    $ordered = @($Values | Sort-Object)
    if ($ordered.Count -eq 0) { return $null }
    if ($ordered.Count -eq 1) { return [Math]::Round([double]$ordered[0], 2) }
    $rank = ($ordered.Count - 1) * $Percentile
    $lower = [Math]::Floor($rank)
    $upper = [Math]::Ceiling($rank)
    if ($lower -eq $upper) { return [Math]::Round([double]$ordered[$lower], 2) }
    $weight = $rank - $lower
    return [Math]::Round(([double]$ordered[$lower] * (1 - $weight)) + ([double]$ordered[$upper] * $weight), 2)
}
function Measure-ColdInvocation([string]$Tool, [hashtable]$Arguments) {
    $nativeArguments = [Collections.Generic.List[string]]::new()
    foreach ($argument in @('-NoLogo', '-NoProfile', '-File', $Tool)) { $nativeArguments.Add($argument) }
    foreach ($entry in @($Arguments.GetEnumerator() | Sort-Object Key)) {
        if ($entry.Value -is [bool]) {
            if ([bool]$entry.Value) { $nativeArguments.Add("-$($entry.Key)") }
        } else {
            $nativeArguments.Add("-$($entry.Key)")
            $nativeArguments.Add([string]$entry.Value)
        }
    }
    $duration = Measure-Command {
        & $shellPath @nativeArguments | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "Cold standalone-index invocation failed for $Tool (exit=$LASTEXITCODE)." }
    }
    return $duration.TotalMilliseconds
}
$cases = @(
    [pscustomobject]@{
        name = 'runtime-topology'
        sourcePath = '.llm-wiki/generated/runtime-topology.json'
        tool = Join-Path $PSScriptRoot 'Find-LlmWikiRuntimeTopology.ps1'
        jsonArguments = @{ Query = 'MailRelay'; CompiledIndexSource = 'Json'; Format = 'Json' }
        sqliteArguments = @{ Query = 'MailRelay'; CompiledIndexSource = 'Sqlite'; Format = 'Json' }
        sqliteRoute = 'in-process-exact'
        category = 'runtime'
        query = 'MailRelay'
        projectionCoverageComplete = $true
        acceptColdTradeoffForUnifiedRoute = $true
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
        acceptColdTradeoffForUnifiedRoute = $false
    }
    [pscustomobject]@{
        name = 'architecture-health'
        sourcePath = '.llm-wiki/generated/architecture-health-index.json'
        tool = Join-Path $PSScriptRoot 'Find-LlmWikiArchitectureHealth.ps1'
        jsonArguments = @{ View = 'spec-gaps'; Query = 'component'; CompiledIndexSource = 'Json'; Format = 'Json' }
        sqliteArguments = @{ View = 'spec-gaps'; Query = 'component'; Format = 'Json' }
        sqliteRoute = 'in-process-exact'
        category = 'architecture-health'
        query = 'component'
        projectionCoverageComplete = $true
        acceptColdTradeoffForUnifiedRoute = $false
    }
)

$measurements = foreach ($case in $cases) {
    $coldSampleCount = [Math]::Min(3, $Iterations)
    $jsonColdDurations = [Collections.Generic.List[double]]::new()
    $sqliteColdDurations = [Collections.Generic.List[double]]::new()
    for ($coldIteration = 0; $coldIteration -lt $coldSampleCount; $coldIteration++) {
        if (($coldIteration % 2) -eq 0) {
            $jsonColdDurations.Add((Measure-ColdInvocation $case.tool $case.jsonArguments))
            $sqliteColdDurations.Add((Measure-ColdInvocation $case.tool $case.sqliteArguments))
        } else {
            $sqliteColdDurations.Add((Measure-ColdInvocation $case.tool $case.sqliteArguments))
            $jsonColdDurations.Add((Measure-ColdInvocation $case.tool $case.jsonArguments))
        }
    }
    $jsonWarmupArguments = $case.jsonArguments
    $sqliteWarmupArguments = $case.sqliteArguments
    & $case.tool @jsonWarmupArguments | Out-Null
    & $case.tool @sqliteWarmupArguments | Out-Null
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
    $jsonColdP50 = Get-Percentile @($jsonColdDurations) 0.5
    $sqliteColdP50 = Get-Percentile @($sqliteColdDurations) 0.5
    $jsonWarmP50 = Get-Percentile @($jsonDurations) 0.5
    $jsonWarmP95 = Get-Percentile @($jsonDurations) 0.95
    $sqliteWarmP50 = Get-Percentile @($sqliteDurations) 0.5
    $sqliteWarmP95 = Get-Percentile @($sqliteDurations) 0.95
    $recommendation = if ($case.sqliteRoute -eq 'in-process-exact' -and
        $sqliteColdP50 -le ($jsonColdP50 * 1.1) -and
        $sqliteWarmP50 -lt $jsonWarmP50 -and
        $sqliteWarmP95 -le ($jsonWarmP95 * 1.1)) {
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
        coldSampleCount = $coldSampleCount
        jsonColdProcessP50Ms = $jsonColdP50
        jsonColdProcessP95Ms = Get-Percentile @($jsonColdDurations) 0.95
        jsonWarmP50Ms = $jsonWarmP50
        jsonWarmP95Ms = $jsonWarmP95
        sqliteRoute = [string]$case.sqliteRoute
        sqliteColdProcessP50Ms = $sqliteColdP50
        sqliteColdProcessP95Ms = Get-Percentile @($sqliteColdDurations) 0.95
        sqliteWarmP50Ms = $sqliteWarmP50
        sqliteWarmP95Ms = $sqliteWarmP95
        coldProcessP50DeltaMs = [Math]::Round($sqliteColdP50 - $jsonColdP50, 2)
        warmP50DeltaMs = [Math]::Round($sqliteWarmP50 - $jsonWarmP50, 2)
        warmP95DeltaMs = [Math]::Round($sqliteWarmP95 - $jsonWarmP95, 2)
        projectionCoverageComplete = [bool]$case.projectionCoverageComplete
        performanceRecommendation = $recommendation
        routeDecision = $(if ([bool]$case.acceptColdTradeoffForUnifiedRoute) { 'keep-in-process-sqlite-for-unified-production-route' } elseif ($recommendation -eq 'keep-in-process-sqlite') { 'keep-in-process-sqlite' } else { $recommendation })
    }
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 4
    iterations = $Iterations
    alreadySqlite = @('quality-index', 'domain-data')
    measurements = @($measurements)
    caveat = 'Cold-process p50/p95 use fresh PowerShell processes; warm p50/p95 use an already loaded reader in the benchmark process. A SQLite default is recommended only when cold p50 stays within 10% and warm p50/p95 do not regress.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6; exit 0 }
Write-Host "Standalone index route telemetry ($Iterations alternating iteration(s)):"
foreach ($item in $measurements) {
    Write-Host " - $($item.index): cold-process JSON/SQLite p50=$($item.jsonColdProcessP50Ms)/$($item.sqliteColdProcessP50Ms)ms, warm JSON/SQLite p50=$($item.jsonWarmP50Ms)/$($item.sqliteWarmP50Ms)ms, warm p95=$($item.jsonWarmP95Ms)/$($item.sqliteWarmP95Ms)ms, route=$($item.sqliteRoute), performance=$($item.performanceRecommendation), decision=$($item.routeDecision)"
}
Write-Host "Caveat: $($result.caveat)"
