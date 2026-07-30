[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'credential', 'identity', 'health', 'financial', 'privateContent', 'logging', 'boundaries', 'external')]
    [string]$Category = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$index = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/sensitive-data-index.json') -Raw | ConvertFrom-Json
$items = if ($Category -eq 'logging') {
    @($index.potentialLogging)
} elseif ($Category -eq 'boundaries') {
    @($index.boundaryFiles)
} elseif ($Category -eq 'external') {
    @($index.externalTransfers)
} elseif ($Category -eq 'all') {
    @($index.fields) + @($index.externalTransfers)
} else {
    @($index.fields | Where-Object category -eq $Category)
}
if (-not [string]::IsNullOrWhiteSpace($Query)) {
    $queryTokens = @(
        $Query -split '\s+' |
            Where-Object { $_.Length -ge 2 } |
            Sort-Object -Unique
    )
    $items = @(
        $items |
            ForEach-Object {
                $item = $_
                $searchText = $item | ConvertTo-Json -Compress
                $matchCount = @($queryTokens | Where-Object {
                    $searchText -match [regex]::Escape($_)
                }).Count
                if ($matchCount -gt 0) {
                    [pscustomobject]@{ item = $item; matchCount = $matchCount }
                }
            } |
            Sort-Object matchCount -Descending |
            Select-Object -ExpandProperty item
    )
}
$items = @($items | Select-Object -First $Limit)
if ($Format -eq 'Json') {
    [pscustomobject]@{ category = $Category; count = $items.Count; items = $items } | ConvertTo-Json -Depth 8
    exit 0
}
Write-Host "Sensitive data '$Category': $($items.Count) candidate(s)."
foreach ($item in $items) { Write-Host " - $(($item | ConvertTo-Json -Compress))" }
