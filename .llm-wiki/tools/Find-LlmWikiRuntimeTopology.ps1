[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 100)]
    [int]$Limit = 30
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$topology = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/runtime-topology.json') -Raw | ConvertFrom-Json
$groups = [ordered]@{
    composeServices = @($topology.composeServices)
    hostedServices = @($topology.hostedServices)
    httpClients = @($topology.httpClients)
    webhooks = @($topology.webhooks)
    recurringJobRegistrations = @($topology.recurringJobRegistrations)
}
if (-not [string]::IsNullOrWhiteSpace($Query)) {
    foreach ($key in @($groups.Keys)) {
        $groups[$key] = @(
            $groups[$key] |
                Where-Object { ($_ | ConvertTo-Json -Compress) -match [regex]::Escape($Query) } |
                Select-Object -First $Limit
        )
    }
}
if ($Format -eq 'Json') {
    [pscustomobject]$groups | ConvertTo-Json -Depth 8
    exit 0
}
foreach ($key in $groups.Keys) {
    Write-Host "$key ($(@($groups[$key]).Count)):"
    foreach ($item in @($groups[$key] | Select-Object -First $Limit)) {
        Write-Host " - $(($item | ConvertTo-Json -Compress))"
    }
    Write-Host ''
}
