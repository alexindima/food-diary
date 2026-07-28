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
$wikiRoot = Split-Path -Parent $PSScriptRoot
$index = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/quality-index.json') -Raw | ConvertFrom-Json

$items = switch ($View) {
    'test-gaps' { @($index.criticalSymbols | Where-Object testReferenceCount -eq 0) }
    'debt' { @($index.debtMarkers) }
    default { @($index.hotspots) }
}
if (-not [string]::IsNullOrWhiteSpace($Query)) {
    $items = @($items | Where-Object { ($_ | ConvertTo-Json -Compress) -match [regex]::Escape($Query) })
}
$items = @($items | Select-Object -First $Limit)
if ($Format -eq 'Json') {
    [pscustomobject]@{ view = $View; count = $items.Count; items = $items } | ConvertTo-Json -Depth 8
    exit 0
}

Write-Host "Quality view '$View': $($items.Count) result(s)."
foreach ($item in $items) {
    if ($View -eq 'hotspots') {
        Write-Host " - $($item.path): score=$($item.structuralRiskScore), lines=$($item.nonBlankLines), decisions=$($item.decisionPoints), unreferenced=$($item.unreferencedCriticalSymbols)"
    } elseif ($View -eq 'test-gaps') {
        Write-Host " - [$($item.role)] $($item.name) ($($item.path):$($item.line))"
    } else {
        Write-Host " - [$($item.marker)] $($item.path):$($item.line)"
    }
}
