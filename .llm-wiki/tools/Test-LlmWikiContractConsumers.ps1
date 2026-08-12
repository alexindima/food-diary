[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$result = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -Format Json | ConvertFrom-Json
if ($result.declarationPath -ne 'FoodDiary.Application.Users/Common/IUserContextService.cs') {
    throw "Unexpected IUserContextService declaration: $($result.declarationPath)"
}
if ($result.readiness.abstractionOwned) { throw 'IUserContextService must currently be reported as implementation-owned.' }
if ($result.readiness.aggregateConsumers -ne 0) { throw 'IUserContextService aggregate readers should have been extracted.' }
if ($result.readiness.mutationConsumers -lt 1) { throw 'Expected remaining mutation consumers in the contract report.' }
if ($result.readiness.businessConsumers -ge $result.readiness.productionConsumers) { throw 'Composition and empty reference matches must not be counted as business consumers.' }
if ($result.readiness.compositionRegistrations -lt 1) { throw 'Expected DI registration evidence to be reported separately.' }
if ($result.readiness.externalModuleConsumers -ne 0) { throw 'IUserContextService must have no external module consumers before Users extraction.' }
if ($result.readiness.internalOwnerConsumers -lt 1) { throw 'Expected owner-internal consumers to remain visible.' }
Write-Host "LLM Wiki contract consumer tests passed: $($result.readiness.productionConsumers) production consumer(s)."
