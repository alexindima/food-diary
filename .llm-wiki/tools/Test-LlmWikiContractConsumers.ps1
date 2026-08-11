[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$result = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -Format Json | ConvertFrom-Json
if ($result.declarationPath -ne 'FoodDiary.Application/Users/Common/IUserContextService.cs') {
    throw "Unexpected IUserContextService declaration: $($result.declarationPath)"
}
if ($result.readiness.abstractionOwned) { throw 'IUserContextService must currently be reported as implementation-owned.' }
if ($result.readiness.aggregateConsumers -lt 1) { throw 'Expected aggregate-reading IUserContextService consumers.' }
if (@($result.consumers | Where-Object { $_.consumer -eq 'Ai' }).Count -ne 1) { throw 'Expected the Ai consumer in the contract report.' }
Write-Host "LLM Wiki contract consumer tests passed: $($result.readiness.productionConsumers) production consumer(s)."
