[CmdletBinding()]
param([ValidateRange(2,20)][int]$Iterations = 5,[ValidateSet('Text','Json')][string]$Format = 'Text')
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$null = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') -Action build -Format Json
$shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
function Get-Percentile([double[]]$Values,[double]$Percentile) {
    $ordered = @($Values | Sort-Object)
    $rank = ($ordered.Count-1)*$Percentile
    $lower = [int][Math]::Floor($rank)
    $upper = [int][Math]::Ceiling($rank)
    return [Math]::Round($ordered[$lower]*(1-($rank-$lower))+$ordered[$upper]*($rank-$lower),2)
}
$cases = @(
    @{name='runtime-topology';source='runtime-topology.json';tool='Find-LlmWikiRuntimeTopology.ps1';arguments=@{Query='MailRelay';Format='Json'}}
    @{name='domain-data';source='domain-data-index.json';tool='Find-LlmWikiDomainData.ps1';arguments=@{View='invariants';Query='weight';Format='Json'}}
    @{name='architecture-health';source='architecture-health-index.json';tool='Find-LlmWikiArchitectureHealth.ps1';arguments=@{View='spec-gaps';Query='component';Format='Json'}}
)
$measurements = @(foreach ($case in $cases) {
    $tool = Join-Path $PSScriptRoot $case.tool
    $arguments = $case.arguments
    $coldDurations = [Collections.Generic.List[double]]::new()
    $warmDurations = [Collections.Generic.List[double]]::new()
    $nativeArguments = @('-NoLogo','-NoProfile','-File',$tool)
    foreach ($entry in $arguments.GetEnumerator()) { $nativeArguments += @("-$($entry.Key)",[string]$entry.Value) }
    $coldCount = [Math]::Min(3,$Iterations)
    for ($round=0;$round -lt $coldCount;$round++) {
        $clock = [Diagnostics.Stopwatch]::StartNew()
        & $shellPath @nativeArguments | Out-Null
        if ($LASTEXITCODE) { throw "Cold SQLite invocation failed: $($case.name)" }
        $clock.Stop(); $coldDurations.Add($clock.Elapsed.TotalMilliseconds)
    }
    & $tool @arguments | Out-Null
    for ($round=0;$round -lt $Iterations;$round++) {
        $clock = [Diagnostics.Stopwatch]::StartNew()
        & $tool @arguments | Out-Null
        if (-not $?) { throw "Warm SQLite invocation failed: $($case.name)" }
        $clock.Stop(); $warmDurations.Add($clock.Elapsed.TotalMilliseconds)
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
