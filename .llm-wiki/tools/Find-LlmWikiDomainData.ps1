[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'types', 'invariants', 'mappings', 'indexes', 'relationships')]
    [string]$View = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$index = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/domain-data-index.json') -Raw | ConvertFrom-Json
$groups = [ordered]@{}
if ($View -in @('all', 'types')) { $groups.types = @($index.domainTypes) }
if ($View -in @('all', 'invariants')) { $groups.invariants = @($index.invariants) }
if ($View -in @('all', 'mappings')) { $groups.mappings = @($index.persistenceMappings) }
if ($View -eq 'indexes') {
    $groups.indexes = @($index.persistenceMappings | Where-Object { @($_.indexes).Count -gt 0 })
}
if ($View -eq 'relationships') {
    $groups.relationships = @($index.persistenceMappings | Where-Object { @($_.relationships).Count -gt 0 })
}
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
