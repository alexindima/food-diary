[CmdletBinding()]
param(
    [ValidateSet('hotspots', 'test-gaps', 'debt')]
    [string]$View = 'hotspots',
    [ValidateSet('Product', 'Wiki', 'All')]
    [string]$Area = 'Product',
    [string]$Query,
    [ValidateRange(1, 100)]
    [int]$Limit = 20,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$recordKind = switch ($View) { 'test-gaps' { 'criticalSymbol' } 'debt' { 'debtMarker' } default { 'hotspot' } }
$queryLimit = if ($Area -eq 'Product' -and [string]::IsNullOrWhiteSpace($Query)) { 100 } else { $Limit }
$queryArguments = @{
    Action = 'query'
    Category = 'risks'
    Query = $Query
    Limit = $queryLimit
    RecordKind = $recordKind
    SkipRefresh = $true
    Format = 'Json'
}
if ($Area -eq 'Product') { $queryArguments.ExcludePathPrefix = '.llm-wiki/' }
elseif ($Area -eq 'Wiki') { $queryArguments.PathPrefix = '.llm-wiki/' }
if ($View -eq 'test-gaps') { $queryArguments.OnlyUnreferenced = $true }
$queryResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') @queryArguments | ConvertFrom-Json
$graphStatus = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
    -Action status `
    -SkipRefresh `
    -Format Json | ConvertFrom-Json
$riskRecords = @($queryResult.records)
$riskRecords = @($riskRecords | Where-Object {
    $path = [string]$_.payload.path
    $isWiki = $path -eq '.llm-wiki/wiki.ps1' -or $path -match '^\.llm-wiki/'
    switch ($Area) {
        'Product' { -not $isWiki }
        'Wiki' { $isWiki }
        default { $true }
    }
})

$items = switch ($View) {
    'test-gaps' { @($riskRecords | Where-Object { $_.payload.recordKind -eq 'criticalSymbol' -and [int]$_.payload.testReferenceCount -eq 0 } | ForEach-Object payload) }
    'debt' { @($riskRecords | Where-Object { $_.payload.recordKind -eq 'debtMarker' } | ForEach-Object payload) }
    default { @($riskRecords | Where-Object { $_.payload.recordKind -eq 'hotspot' } | ForEach-Object payload) }
}
if ($Area -eq 'Product' -and [string]::IsNullOrWhiteSpace($Query) -and $items.Count -gt $Limit) {
    $selected = [System.Collections.Generic.List[object]]::new()
    $deferred = [System.Collections.Generic.List[object]]::new()
    $areaCounts = @{}
    foreach ($item in $items) {
        $path = [string]$item.path
        $topLevelArea = @($path -split '/')[0]
        $count = if ($areaCounts.ContainsKey($topLevelArea)) { [int]$areaCounts[$topLevelArea] } else { 0 }
        if ($count -lt 2 -and $selected.Count -lt $Limit) {
            $selected.Add($item)
            $areaCounts[$topLevelArea] = $count + 1
        } else {
            $deferred.Add($item)
        }
    }
    foreach ($item in $deferred) {
        if ($selected.Count -ge $Limit) { break }
        $selected.Add($item)
    }
    $items = @($selected)
} else {
    $items = @($items | Select-Object -First $Limit)
}
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
    area = $Area
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

Write-Host "Quality view '$View' ($Area): $($items.Count) result(s)."
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
