[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$result = & (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Contract IUserContextService -Format Json | ConvertFrom-Json
if ($result.declarationPath -ne 'Modules/Users/Application/Common/IUserContextService.cs') {
    throw "Unexpected IUserContextService declaration: $($result.declarationPath)"
}
if ($result.readiness.abstractionOwned) { throw 'IUserContextService must currently be reported as implementation-owned.' }
if ($result.readiness.aggregateConsumers -ne 0) { throw 'IUserContextService aggregate readers should have been extracted.' }
if ($result.readiness.mutationConsumers -lt 1) { throw 'Expected remaining mutation consumers in the contract report.' }
if ($result.readiness.businessConsumers -ge $result.readiness.productionConsumers) { throw 'Composition and empty reference matches must not be counted as business consumers.' }
if ($result.readiness.compositionRegistrations -lt 1) { throw 'Expected DI registration evidence to be reported separately.' }
if ($result.readiness.externalModuleConsumers -ne 0) { throw 'IUserContextService must have no external module consumers before Users extraction.' }
if ($result.readiness.internalOwnerConsumers -lt 1) { throw 'Expected owner-internal consumers to remain visible.' }
$classificationCases = @(
    @('Modules/Users/Application/Commands/UpdateUser.cs', 'Users'),
    @('Modules/Meals/Application/Commands/UpdateUser.cs', 'Meals'),
    @('Modules\Meals\Application\Commands\UpdateUser.cs', 'Meals'),
    @('FoodDiary.Application/Users/Commands/UpdateUser.cs', 'Users'),
    @('FoodDiary.Application.Users/Commands/UpdateUser.cs', 'Users'),
    @('FoodDiary.Web.Api/Program.cs', 'FoodDiary.Web.Api')
)
# Load the real classifier definition without rerunning the repository scan.
$tokens = $null; $parseErrors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1'), [ref]$tokens, [ref]$parseErrors)
$definition = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-ModuleName' }, $true)
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. ([scriptblock]::Create($definition.Extent.Text))
foreach ($case in $classificationCases) {
    if ((Get-ModuleName $case[0]) -ne $case[1]) { throw "Incorrect module classification for $($case[0])" }
}
foreach ($consumer in @($result.consumers | Where-Object access -eq 'mutation')) {
    if ($consumer.consumer -ne 'Users' -or 'UpdateUserAsync' -notin $consumer.methods) {
        throw 'Owner-internal mutations must remain visible with their actual operations.'
    }
}
Write-Host "LLM Wiki contract consumer tests passed: $($result.readiness.productionConsumers) production consumer(s)."
