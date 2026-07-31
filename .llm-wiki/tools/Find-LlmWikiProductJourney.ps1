[CmdletBinding()]
param(
    [string]$Query,
    [string[]]$ChangedPath,
    [ValidateRange(1, 50)]
    [int]$Limit = 12,
    [switch]$FailOnNone,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$catalogPath = Join-Path $wikiRoot 'knowledge/product-journeys.json'
if (-not (Test-Path -LiteralPath $catalogPath -PathType Leaf)) {
    throw "Product journey catalog is absent: $catalogPath"
}
$catalog = Get-Content -LiteralPath $catalogPath -Raw | ConvertFrom-Json
if ([int]$catalog.schemaVersion -ne 1) { throw 'Unsupported product journey catalog schema.' }

$paths = @($ChangedPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique)
$queryText = [string]$Query
$journeyMatches = [Collections.Generic.List[object]]::new()
foreach ($journey in @($catalog.journeys)) {
    $matchedAliases = @($journey.aliases | Where-Object {
        -not [string]::IsNullOrWhiteSpace($queryText) -and $queryText.IndexOf([string]$_, [StringComparison]::OrdinalIgnoreCase) -ge 0
    })
    $matchedPaths = @($paths | Where-Object {
        $candidate = $_
        @($journey.pathPatterns | Where-Object { $candidate -match [string]$_ }).Count -gt 0
    })
    $idMatch = -not [string]::IsNullOrWhiteSpace($queryText) -and (
        $queryText.IndexOf([string]$journey.id, [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $queryText.IndexOf([string]$journey.title, [StringComparison]::OrdinalIgnoreCase) -ge 0)
    $score = ($matchedAliases.Count * 20) + ($matchedPaths.Count * 15) + $(if ($idMatch) { 40 } else { 0 })
    if ($score -gt 0) {
        $journeyMatches.Add([pscustomobject][ordered]@{
            id = [string]$journey.id
            title = [string]$journey.title
            risk = [string]$journey.risk
            score = $score
            matchedAliases = @($matchedAliases)
            matchedPaths = @($matchedPaths)
            scenarios = @($journey.scenarios)
            requiredReviewAreas = @($journey.requiredReviewAreas)
            evidenceHints = @($journey.evidenceHints)
            provenance = '.llm-wiki/knowledge/product-journeys.json'
        })
    }
}
$ordered = @($journeyMatches | Sort-Object @{ Expression = 'score'; Descending = $true }, id | Select-Object -First $Limit)
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    authority = [string]$catalog.authority
    query = $queryText
    changedPaths = $paths
    matchCount = $ordered.Count
    journeys = $ordered
    note = 'Journey matches are reviewed navigation and test-scope evidence. Confirm runtime behavior in source and tests.'
}
if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 12
} else {
    Write-Host "Product journeys: $($result.matchCount) match(es)"
    foreach ($journey in $ordered) {
        Write-Host " - $($journey.id) [$($journey.risk), score=$($journey.score)]: $($journey.title)"
        Write-Host "   Scenarios: $($journey.scenarios -join ', ')"
        Write-Host "   Reviews: $($journey.requiredReviewAreas -join ', ')"
        if ($journey.matchedPaths.Count -gt 0) { Write-Host "   Paths: $($journey.matchedPaths -join ', ')" }
    }
    Write-Host $result.note
}
if ($FailOnNone -and $ordered.Count -eq 0) { exit 1 }
