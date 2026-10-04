[CmdletBinding()]
param(
    [string]$Query,
    [string[]]$CandidatePath,
    [ValidateRange(1, 50)]
    [int]$Limit = 5,
    [ValidateSet('Sqlite')]
    [string]$CompiledIndexSource = 'Sqlite',
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$paths = @($CandidatePath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$diagnostics = $null
. (Join-Path $PSScriptRoot 'Ensure-LlmWikiSqliteProjection.ps1')
$sqlResult = Invoke-LlmWikiSqliteQuery -Arguments @{ Action = 'frontend-runtime-owner'; Query = $Query; ChangedPath = $paths; Limit = $Limit }
if (-not [bool]$sqlResult.ready) {
    throw "SQLite frontend runtime-owner projection is unavailable ($($sqlResult.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
}
$result = $sqlResult.runtimeOwner
$diagnostics = [ordered]@{
    source = [string]$sqlResult.source
    sqlDurationMs = [double]$sqlResult.durationMs
    scannedRecords = [int]$sqlResult.scannedRecords
    candidateRecords = [int]$sqlResult.candidateRecords
    returnedRecords = [int]$sqlResult.returnedRecords
    sourceHash = [string]$sqlResult.sourceHash
}
$stopwatch.Stop()
$diagnostics['roundTripDurationMs'] = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
$result | Add-Member -NotePropertyName compiledIndex -NotePropertyValue ([pscustomobject]$diagnostics) -Force
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12 } else {
    Write-Host "Frontend runtime owners: $($result.ownerCount) ($($result.confidence) confidence)"
    foreach ($owner in @($result.owners)) {
        Write-Host " - $($owner.class) [$($owner.selector)]: $($owner.templatePath)"
        foreach ($edge in @($owner.renderChain)) { Write-Host "   <- $($edge.renderedBy)" }
    }
    Write-Host $result.note
}
