[CmdletBinding()]
param([ValidateRange(2,20)][int]$Iterations = 5,[ValidateSet('Text','Json')][string]$Format = 'Text')
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$null = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') -Action build -Format Json
$shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
function Assert-MeasurementResponse([string]$Json, [hashtable]$Case) {
    try { $response = $Json | ConvertFrom-Json } catch { throw "Invalid measurement response for $($Case.name): expected JSON." }
    $diagnostics = if ($null -ne $response -and $response.PSObject.Properties['_diagnostics']) { $response._diagnostics } else { $null }
    $hasDiagnostics = $null -ne $diagnostics -and @('reader', 'source', 'sourceHash' | Where-Object { -not $diagnostics.PSObject.Properties[$_] }).Count -eq 0
    $fresh = $Case.name -ne 'runtime-topology' -or ($null -ne $response -and
        $response.PSObject.Properties['_freshness'] -and $null -ne $response._freshness -and
        $response._freshness.PSObject.Properties['verified'] -and $response._freshness.verified)
    $sourcePath = Join-Path $repositoryRoot ('.llm-wiki/generated/' + $Case.source)
    $text = [IO.File]::ReadAllText($sourcePath).Replace("`r`n", "`n")
    $sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text))).ToLowerInvariant()
    if ($null -eq $response -or ($response.PSObject.Properties['ready'] -and -not $response.ready) -or
        -not $hasDiagnostics -or $diagnostics.reader -cne 'microsoft-data-sqlite' -or
        $diagnostics.source -cne $Case.diagnosticSource -or $diagnostics.sourceHash -cne $sourceHash -or
        -not $fresh) {
        throw "Unverified measurement response for $($Case.name): expected the current, complete in-process SQLite projection."
    }
}
function Get-Percentile([double[]]$Values,[double]$Percentile) {
    $ordered = @($Values | Sort-Object)
    $rank = ($ordered.Count-1)*$Percentile
    $lower = [int][Math]::Floor($rank)
    $upper = [int][Math]::Ceiling($rank)
    return [Math]::Round($ordered[$lower]*(1-($rank-$lower))+$ordered[$upper]*($rank-$lower),2)
}
$cases = @(
    @{name='runtime-topology';source='runtime-topology.json';diagnosticSource='sqlite-runtime-in-process';tool='Find-LlmWikiRuntimeTopology.ps1';arguments=@{Query='MailRelay';Format='Json';IncludeDiagnostics=$true}}
    @{name='domain-data';source='domain-data-index.json';diagnosticSource='sqlite-domain-data-in-process';tool='Find-LlmWikiDomainData.ps1';arguments=@{View='invariants';Query='weight';Format='Json';IncludeDiagnostics=$true}}
    @{name='architecture-health';source='architecture-health-index.json';diagnosticSource='sqlite-architecture-health-in-process';tool='Find-LlmWikiArchitectureHealth.ps1';arguments=@{View='spec-gaps';Query='component';Format='Json';IncludeDiagnostics=$true}}
)
$measurements = @(foreach ($case in $cases) {
    $tool = Join-Path $PSScriptRoot $case.tool
    $arguments = $case.arguments
    $coldDurations = [Collections.Generic.List[double]]::new()
    $warmDurations = [Collections.Generic.List[double]]::new()
    $nativeArguments = @('-NoLogo','-NoProfile','-File',$tool)
    foreach ($entry in $arguments.GetEnumerator()) {
        $nativeArguments += "-$($entry.Key)"
        if ($entry.Value -isnot [bool]) { $nativeArguments += [string]$entry.Value }
    }
    $coldCount = [Math]::Min(3,$Iterations)
    for ($round=0;$round -lt $coldCount;$round++) {
        $clock = [Diagnostics.Stopwatch]::StartNew()
        $response = & $shellPath @nativeArguments
        if ($LASTEXITCODE) { throw "Cold SQLite invocation failed: $($case.name)" }
        $clock.Stop()
        Assert-MeasurementResponse ($response -join "`n") $case
        $coldDurations.Add($clock.Elapsed.TotalMilliseconds)
    }
    $response = & $tool @arguments
    Assert-MeasurementResponse ($response -join "`n") $case
    for ($round=0;$round -lt $Iterations;$round++) {
        $clock = [Diagnostics.Stopwatch]::StartNew()
        $response = & $tool @arguments
        if (-not $?) { throw "Warm SQLite invocation failed: $($case.name)" }
        $clock.Stop()
        Assert-MeasurementResponse ($response -join "`n") $case
        $warmDurations.Add($clock.Elapsed.TotalMilliseconds)
    }
    [pscustomobject][ordered]@{
        index=$case.name;sourceBytes=(Get-Item (Join-Path $repositoryRoot ('.llm-wiki/generated/'+$case.source))).Length
        coldSampleCount=$coldCount;sqliteRoute='in-process-exact';projectionCoverageComplete=$true
        sqliteColdProcessP50Ms=Get-Percentile @($coldDurations) 0.5
        sqliteColdProcessP95Ms=Get-Percentile @($coldDurations) 0.95
        sqliteWarmP50Ms=Get-Percentile @($warmDurations) 0.5
        sqliteWarmP95Ms=Get-Percentile @($warmDurations) 0.95
        routeDecision='sqlite-only'
    }
})
$result = [pscustomobject][ordered]@{schemaVersion=5;iterations=$Iterations;alreadySqlite=@('quality-index','domain-data');measurements=$measurements;caveat='Cold samples use fresh PowerShell processes; warm samples reuse the loaded SQLite reader. Machine load affects observed latency.'}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6; return }
foreach ($item in $measurements) { Write-Host "$($item.index): cold p50/p95=$($item.sqliteColdProcessP50Ms)/$($item.sqliteColdProcessP95Ms)ms, warm p50/p95=$($item.sqliteWarmP50Ms)/$($item.sqliteWarmP95Ms)ms." }
