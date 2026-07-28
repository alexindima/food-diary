[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'components', 'consumers', 'api', 'translations', 'spec-gaps')]
    [string]$View = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$index = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/frontend-contract-index.json') -Raw | ConvertFrom-Json
$groups = [ordered]@{}
if ($View -in @('all', 'components')) { $groups.components = @($index.components) }
if ($View -eq 'spec-gaps') { $groups.specGaps = @($index.components | Where-Object { $null -eq $_.specPath }) }
if ($View -in @('all', 'consumers')) { $groups.consumers = @($index.consumerEdges) }
if ($View -in @('all', 'api')) { $groups.apiCalls = @($index.apiCalls) }
if ($View -in @('all', 'translations')) { $groups.translations = @($index.translationUsage) }
foreach ($key in @($groups.Keys)) {
    if (-not [string]::IsNullOrWhiteSpace($Query)) {
        $groups[$key] = @($groups[$key] | Where-Object { ($_ | ConvertTo-Json -Depth 6 -Compress) -match [regex]::Escape($Query) })
    }
    $groups[$key] = @($groups[$key] | Select-Object -First $Limit)
}
if ($Format -eq 'Json') {
    [pscustomobject]$groups | ConvertTo-Json -Depth 9
    exit 0
}
foreach ($key in $groups.Keys) {
    Write-Host "$key ($(@($groups[$key]).Count)):"
    foreach ($item in $groups[$key]) { Write-Host " - $(($item | ConvertTo-Json -Depth 6 -Compress))" }
    Write-Host ''
}
