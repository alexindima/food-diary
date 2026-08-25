[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Query,
    [ValidateSet('Any', 'Api', 'Backend', 'Frontend', 'Database', 'Tests')]
    [string]$ChangeType = 'Any',
    [string]$Module,
    [string[]]$ScopePath,
    [ValidateRange(1, 20)]
    [int]$Limit = 5,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$arguments = @{
    Action = 'search'
    Query = $Query
    ChangeType = $ChangeType
    Limit = $Limit
    SkipRefresh = $true
    Format = 'Json'
}
if (-not [string]::IsNullOrWhiteSpace($Module)) { $arguments.Module = $Module }
if (@($ScopePath).Count -gt 0) { $arguments.Path = @($ScopePath) -join ';' }
$search = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') @arguments | ConvertFrom-Json
$records = @($search.records | ForEach-Object {
    [pscustomobject][ordered]@{
        rank = [int]$_.rank
        path = [string]$_.path
        score = [double]$_.score
        lexicalRank = [double]$_.lexicalRank
        layer = [string]$_.layer
        module = [string]$_.module
        role = [string]$_.role
        isTest = [bool]$_.isTest
        extension = [string]$_.extension
        reasons = @($_.reasons)
    }
})
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    authority = 'sqlite-derived'
    queryTerms = @($search.queryTerms)
    indexedDocuments = [int]$search.indexedDocuments
    durationMs = [double]$search.durationMs
    candidates = $records
}
& (Join-Path $PSScriptRoot 'Write-LlmWikiContextQueryObservation.ps1') `
    -DurationMs $result.durationMs `
    -QueryTermCount @($result.queryTerms).Count `
    -CandidateCount $records.Count `
    -TopLayer $(if ($records.Count -gt 0) { $records[0].layer } else { 'none' }) `
    -TopRole $(if ($records.Count -gt 0) { $records[0].role } else { 'none' }) `
    -Ready ([bool]$search.ready)
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 8; exit 0 }
Write-Host "Context explanation: $($records.Count) candidate(s), SQLite/scoring=$($result.durationMs)ms."
Write-Host "Query terms: $($result.queryTerms -join ', ')"
foreach ($record in $records) {
    Write-Host "[$($record.rank)] $($record.path) score=$($record.score) layer=$($record.layer) module=$($record.module) role=$($record.role) test=$($record.isTest)"
    foreach ($reason in $record.reasons) { Write-Host "    - $reason" }
}
