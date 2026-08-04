[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
function Assert-IntegrationScan([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

$featureWorkflow = [pscustomobject]@{ profile = 'feature' }
$featureResearch = [pscustomobject]@{
    discovery = [pscustomobject]@{
        groundedPaths = @('FoodDiary.Presentation.Api/Features/Dashboard/DashboardEndpoints.cs')
        dependencyInjection = @([pscustomobject]@{ path = 'FoodDiary.Web.Api/Program.cs'; service = 'DashboardService' })
    }
    boundaries = [pscustomobject]@{ runtime = @([pscustomobject]@{ path = 'FoodDiary.Integrations/DashboardClient.cs'; type = 'http-client' }) }
}
$featureBrief = [pscustomobject]@{
    change = [pscustomobject]@{ scopes = @('Api', 'Backend'); directModules = @('Presentation.Api'); downstreamModules = @('Web.Api') }
    runtimeImpact = [pscustomobject]@{ hostedServices = @(); httpClients = @([pscustomobject]@{ registrationPath = 'FoodDiary.Web.Api/Program.cs' }); webhooks = @(); recurringJobs = @(); composeServices = @() }
    privacyImpact = [pscustomobject]@{ externalTransfers = @(); potentialLogging = @() }
    backendContractImpact = [pscustomobject]@{
        contracts = @([pscustomobject]@{ name = 'DashboardResponse' })
        productionConsumers = @([pscustomobject]@{ consumerPath = 'FoodDiary.Web.Api/Dashboard.cs'; contract = 'DashboardResponse' })
    }
    frontendContractImpact = [pscustomobject]@{ downstreamConsumers = @(); changedConsumers = @(); apiCalls = @() }
    focusedTests = @('tests/FoodDiary.Web.Api.IntegrationTests/DashboardTests.cs')
    testScenarios = @([pscustomobject]@{ id = 'api-compatible'; description = 'Existing clients remain compatible.' })
    requiredChecks = @([pscustomobject]@{ id = 'api-tests'; command = 'dotnet test tests/FoodDiary.Web.Api.IntegrationTests' })
}
$featureScan = & (Join-Path $PSScriptRoot 'Get-LlmWikiIntegrationScan.ps1') `
    -Objective 'Extend the dashboard API response.' `
    -WorkflowInput $featureWorkflow `
    -ResearchInput $featureResearch `
    -BriefInput $featureBrief `
    -Format Json | ConvertFrom-Json
Assert-IntegrationScan $featureScan.recommended 'Cross-layer API work did not recommend an integration scan.'
Assert-IntegrationScan (@($featureScan.inboundConsumers).Count -eq 1) 'Integration scan omitted an indexed inbound consumer.'
Assert-IntegrationScan (@($featureScan.outboundDependencies | Where-Object kind -eq 'dependency-injection').Count -eq 1) 'Integration scan omitted a DI dependency.'
Assert-IntegrationScan (@($featureScan.externalBoundaries).Count -gt 0) 'Integration scan omitted an external runtime boundary.'
Assert-IntegrationScan (@($featureScan.verification.focusedTests).Count -eq 1) 'Integration scan omitted focused verification.'

$tinyWorkflow = [pscustomobject]@{ profile = 'tiny' }
$tinyResearch = [pscustomobject]@{
    discovery = [pscustomobject]@{ groundedPaths = @('FoodDiary.Web.Client/src/app/card.scss'); dependencyInjection = @() }
    boundaries = [pscustomobject]@{ runtime = @() }
}
$tinyBrief = [pscustomobject]@{
    change = [pscustomobject]@{ scopes = @('Frontend'); directModules = @('Web.Client'); downstreamModules = @() }
    runtimeImpact = [pscustomobject]@{ hostedServices = @(); httpClients = @(); webhooks = @(); recurringJobs = @(); composeServices = @() }
    privacyImpact = [pscustomobject]@{ externalTransfers = @(); potentialLogging = @() }
    backendContractImpact = [pscustomobject]@{ contracts = @(); productionConsumers = @() }
    frontendContractImpact = [pscustomobject]@{ downstreamConsumers = @(); changedConsumers = @(); apiCalls = @() }
    focusedTests = @('FoodDiary.Web.Client/src/app/card.spec.ts')
    testScenarios = @()
    requiredChecks = @()
}
$tinyScan = & (Join-Path $PSScriptRoot 'Get-LlmWikiIntegrationScan.ps1') `
    -Objective 'Adjust local card spacing.' `
    -WorkflowInput $tinyWorkflow `
    -ResearchInput $tinyResearch `
    -BriefInput $tinyBrief `
    -Format Json | ConvertFrom-Json
Assert-IntegrationScan (-not $tinyScan.recommended) 'Tiny frontend work gained integration-scan ceremony.'
Assert-IntegrationScan ($tinyScan.nextAction -match 'No integration scan is recommended') 'Tiny scan did not return the focused-workflow guidance.'

Write-Host 'Integration scan smoke passed: cross-layer evidence is composed and tiny work remains unchanged.'
