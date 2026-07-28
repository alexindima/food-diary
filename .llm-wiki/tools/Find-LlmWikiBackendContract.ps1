[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'contracts', 'consumers', 'production', 'tests', 'ambiguous', 'unconsumed')]
    [string]$View = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$index = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/backend-contract-index.json') -Raw | ConvertFrom-Json
$consumedNames = @($index.consumerEdges.contract | Sort-Object -Unique)
$groups = [ordered]@{}
if ($View -in @('all', 'contracts')) { $groups.contracts = @($index.contracts) }
if ($View -in @('all', 'consumers')) { $groups.consumers = @($index.consumerEdges) }
if ($View -eq 'production') { $groups.productionConsumers = @($index.consumerEdges | Where-Object { -not $_.isTest }) }
if ($View -eq 'tests') { $groups.testConsumers = @($index.consumerEdges | Where-Object isTest) }
if ($View -eq 'ambiguous') { $groups.ambiguousContracts = @($index.contracts | Where-Object ambiguous) }
if ($View -eq 'unconsumed') { $groups.unconsumedContracts = @($index.contracts | Where-Object { $_.name -notin $consumedNames }) }
foreach ($key in @($groups.Keys)) {
    if (-not [string]::IsNullOrWhiteSpace($Query)) {
        $groups[$key] = @($groups[$key] | Where-Object { ($_ | ConvertTo-Json -Depth 7 -Compress) -match [regex]::Escape($Query) })
    }
    $groups[$key] = @($groups[$key] | Select-Object -First $Limit)
}
if ($Format -eq 'Json') {
    [pscustomobject]$groups | ConvertTo-Json -Depth 10
    exit 0
}
foreach ($key in $groups.Keys) {
    Write-Host "$key ($(@($groups[$key]).Count)):"
    foreach ($item in $groups[$key]) { Write-Host " - $(($item | ConvertTo-Json -Depth 7 -Compress))" }
    Write-Host ''
}
