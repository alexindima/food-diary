[CmdletBinding()]
param(
    [ValidateSet('hotspots', 'test-gaps', 'debt')]
    [string]$View = 'hotspots',
    [string]$Query,
    [ValidateRange(1, 100)]
    [int]$Limit = 20,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$queryResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
    -Action query `
    -Category risks `
    -Query $Query `
    -Limit 500 `
    -SkipRefresh `
    -Format Json | ConvertFrom-Json
$graphStatus = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
    -Action status `
    -SkipRefresh `
    -Format Json | ConvertFrom-Json
$riskRecords = @($queryResult.records)

$items = switch ($View) {
    'test-gaps' { @($riskRecords | Where-Object { $_.payload.recordKind -eq 'criticalSymbol' -and [int]$_.payload.testReferenceCount -eq 0 } | ForEach-Object payload) }
    'debt' { @($riskRecords | Where-Object { $_.payload.recordKind -eq 'debtMarker' } | ForEach-Object payload) }
    default { @($riskRecords | Where-Object { $_.payload.recordKind -eq 'hotspot' } | ForEach-Object payload) }
}
$items = @($items | Select-Object -First $Limit)
if ($View -eq 'test-gaps') {
    $items = @($items | ForEach-Object {
        [pscustomobject][ordered]@{
            name = $_.name; role = $_.role; path = $_.path; line = $_.line
            coverageClassification = 'direct-test-reference-absent'
            confidence = 'medium'
            evidenceType = 'static-symbol-name-reference'
            caveat = 'No direct symbol-name reference was discovered in indexed tests; integration, dynamic, reflection-based, or differently named coverage may still exist.'
        }
    })
}
$indexFresh = [bool]$graphStatus.changeSetFresh
$conclusive = $indexFresh -and $items.Count -gt 0
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    view = $View
    query = $Query
    count = $items.Count
    conclusive = $conclusive
    abstained = -not $conclusive
    abstentionReason = $(if ($conclusive) { $null } elseif (-not $indexFresh) { 'The SQLite code graph is stale for the current workspace; run wiki graph-build before treating risk results as current.' } else { 'No matching indexed risk evidence was found; an empty static result is not proof of absence.' })
    scope = [pscustomobject][ordered]@{
        indexedRiskRecordCount = $riskRecords.Count
        returnedItemLimit = $Limit
        source = 'sqlite-code-graph'
        fingerprint = $(if ($queryResult.PSObject.Properties['fingerprint']) { $queryResult.fingerprint } else { $null })
        updatedAtUtc = $(if ($queryResult.PSObject.Properties['updatedAtUtc']) { $queryResult.updatedAtUtc } else { $null })
        fresh = $indexFresh
        indexedChangeSetFingerprint = [string]$graphStatus.changeSetFingerprint
        currentChangeSetFingerprint = [string]$graphStatus.currentChangeSetFingerprint
    }
    items = $items
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 8
    exit 0
}

Write-Host "Quality view '$View': $($items.Count) result(s)."
if (-not $conclusive) { Write-Host "Abstained: $($result.abstentionReason)" }
foreach ($item in $items) {
    if ($View -eq 'hotspots') {
        Write-Host " - $($item.path): score=$($item.structuralRiskScore), lines=$($item.nonBlankLines), decisions=$($item.decisionPoints), unreferenced=$($item.unreferencedCriticalSymbols)"
    } elseif ($View -eq 'test-gaps') {
        Write-Host " - [$($item.role)] $($item.name) ($($item.path):$($item.line)) [$($item.coverageClassification), confidence=$($item.confidence)]"
    } else {
        Write-Host " - [$($item.marker)] $($item.path):$($item.line)"
    }
}
if ($View -eq 'test-gaps' -and $Format -eq 'Text') { Write-Host 'Evidence caveat: static absence is an investigation lead, not proof that execution coverage is absent.' }
