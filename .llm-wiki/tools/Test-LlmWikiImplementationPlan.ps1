[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$brief = & (Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1') `
    -ChangedPath 'FoodDiary.Application/Authentication/Commands/LinkGoogle/LinkGoogleCommandHandler.cs' `
    -Intent 'Move Authentication identity mutations behind Users capabilities' `
    -Format Json | ConvertFrom-Json

$brief.decisionContext.relatedAdrs = @(
    [pscustomobject]@{ path = 'docs/adr/0009-executable-application-module-dependency-graph.md'; title = 'Module graph' }
    [pscustomobject]@{ title = 'Inline decision without a path' }
    'legacy decision label'
    $null
)
$pathlessEntry = [pscustomobject]@{ note = 'heterogeneous entry without a projected path' }
$brief.backendContractImpact.contracts = @($brief.backendContractImpact.contracts) + $pathlessEntry
$brief.backendContractImpact.productionConsumers = @($brief.backendContractImpact.productionConsumers) + $pathlessEntry
$brief.backendContractImpact.testConsumers = @($brief.backendContractImpact.testConsumers) + $pathlessEntry
$brief.frontendContractImpact.components = @($brief.frontendContractImpact.components) + $pathlessEntry
$brief.frontendContractImpact.downstreamConsumers = @($brief.frontendContractImpact.downstreamConsumers) + $pathlessEntry
$brief.domainDataImpact.types = @($brief.domainDataImpact.types) + $pathlessEntry
$brief.domainDataImpact.invariants = @($brief.domainDataImpact.invariants) + $pathlessEntry
$brief.domainDataImpact.mappings = @($brief.domainDataImpact.mappings) + $pathlessEntry

$plan = & (Join-Path $PSScriptRoot 'Get-LlmWikiImplementationPlan.ps1') `
    -BriefInput $brief `
    -Objective 'Move Authentication identity mutations behind Users capabilities' `
    -Format Json | ConvertFrom-Json

$expectedAdr = 'docs/adr/0009-executable-application-module-dependency-graph.md'
if (@($plan.acceptanceInputs.relatedAdrs).Count -ne 1 -or $plan.acceptanceInputs.relatedAdrs[0] -ne $expectedAdr) {
    throw 'Implementation plan did not safely retain only path-bearing ADR entries.'
}
$context = @($plan.phases | Where-Object id -eq 'context')[0]
if ($expectedAdr -notin @($context.files)) { throw 'Context phase lost a valid ADR path.' }

$brief.PSObject.Properties.Remove('decisionContext')
$brief.PSObject.Properties.Remove('rolloutPlan')
$brief.PSObject.Properties.Remove('architectureHealthImpact')
$brief.PSObject.Properties.Remove('backendContractImpact')
$brief.PSObject.Properties.Remove('frontendContractImpact')
$brief.PSObject.Properties.Remove('privacyImpact')
$brief.PSObject.Properties.Remove('domainDataImpact')
$brief.PSObject.Properties.Remove('warnings')
$abbreviatedPlan = & (Join-Path $PSScriptRoot 'Get-LlmWikiImplementationPlan.ps1') `
    -BriefInput $brief `
    -Objective 'Compile an abbreviated change packet' `
    -Format Json | ConvertFrom-Json
if (@($abbreviatedPlan.phases).Count -eq 0) { throw 'Abbreviated brief did not produce an implementation plan.' }
if (@($abbreviatedPlan.acceptanceInputs.relatedAdrs).Count -ne 0) { throw 'Missing decision context did not default to an empty ADR set.' }
if (@($abbreviatedPlan.unresolved.structuralViolations).Count -ne 0) { throw 'Abbreviated brief introduced structural violations.' }

$legacyApiPacket = [pscustomobject]@{
    policy = [pscustomobject]@{ violations = @(); matchedRules = @(); requiredChecks = @(); reviewObligations = @() }
    diff = [pscustomobject]@{ scopes = @('Api'); changedPaths = @() }
    brief = [pscustomobject]@{
        risk = [pscustomobject]@{ level = 'medium'; score = 3 }
        privacyImpact = [pscustomobject]@{ fields = @(); boundaries = @(); potentialLogging = @() }
    }
    rollout = [pscustomobject]@{ flags = [pscustomobject]@{} }
    fingerprint = 'legacy-api-result-fixture'
}
$legacyApiResult = [pscustomobject]@{
    breakingCount = 1
    additiveCount = 0
    changes = @([pscustomobject]@{ severity = 'breaking'; kind = 'removed-operation'; location = '/legacy'; description = 'fixture' })
}
$legacyReadiness = & (Join-Path $PSScriptRoot 'Get-LlmWikiReleaseReadiness.ps1') `
    -PacketInput $legacyApiPacket `
    -ApiCompatibilityInput $legacyApiResult `
    -ManifestPath '.artifacts/llm-wiki/nonexistent-legacy-manifest.json' `
    -AcceptancePath '.artifacts/llm-wiki/nonexistent-legacy-acceptance.json' `
    -EvidencePath '.artifacts/llm-wiki/nonexistent-legacy-evidence.json' `
    -Format Json | ConvertFrom-Json
$apiDimension = @($legacyReadiness.dimensions | Where-Object id -eq 'api-compatibility')[0]
if ($apiDimension.status -ne 'fail' -or @($apiDimension.issues).Count -ne 1) {
    throw 'Release readiness did not normalize a legacy API result without breakingChanges.'
}

Write-Host 'LLM Wiki implementation-plan tests passed.'
