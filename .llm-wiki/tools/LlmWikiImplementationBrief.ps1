function Add-LlmWikiBriefDefault([object]$Target, [string]$Name, [object]$DefaultValue) {
    if (-not $Target.PSObject.Properties[$Name] -or $null -eq $Target.$Name) {
        $Target | Add-Member -NotePropertyName $Name -NotePropertyValue $DefaultValue -Force
    }
}

function Normalize-LlmWikiImplementationBrief([object]$Brief) {
    if ($null -eq $Brief) { $Brief = [pscustomobject]@{} }
    Add-LlmWikiBriefDefault $Brief 'risk' ([pscustomobject]@{ level = 'low'; score = 0; reasons = @() })
    Add-LlmWikiBriefDefault $Brief 'change' ([pscustomobject]@{ paths = @(); scopes = @(); directModules = @(); downstreamModules = @() })
    Add-LlmWikiBriefDefault $Brief 'instructions' @()
    Add-LlmWikiBriefDefault $Brief 'contextPages' @()
    Add-LlmWikiBriefDefault $Brief 'decisionContext' ([pscustomobject]@{ relatedAdrs = @(); reviewRequired = $false; guidance = $null })
    Add-LlmWikiBriefDefault $Brief 'rolloutPlan' ([pscustomobject]@{ preDeploy = @(); deploy = @(); postDeploy = @(); rollback = @() })
    Add-LlmWikiBriefDefault $Brief 'structuralViolations' @()
    Add-LlmWikiBriefDefault $Brief 'architectureHealthImpact' ([pscustomobject]@{ dependencyViolations = @(); untrackedProductionProjects = @(); moduleCycleNodes = @() })
    Add-LlmWikiBriefDefault $Brief 'backendContractImpact' ([pscustomobject]@{ contracts = @(); productionConsumers = @(); testConsumers = @() })
    Add-LlmWikiBriefDefault $Brief 'frontendContractImpact' ([pscustomobject]@{ components = @(); downstreamConsumers = @() })
    Add-LlmWikiBriefDefault $Brief 'privacyImpact' ([pscustomobject]@{ fields = @() })
    Add-LlmWikiBriefDefault $Brief 'domainDataImpact' ([pscustomobject]@{ types = @(); invariants = @(); mappings = @() })
    foreach ($arrayProperty in @('focusedTests', 'testScenarios', 'requiredChecks', 'reviewObligations', 'generatedActions')) {
        Add-LlmWikiBriefDefault $Brief $arrayProperty @()
    }
    Add-LlmWikiBriefDefault $Brief 'warnings' @()

    foreach ($property in @('reasons')) { Add-LlmWikiBriefDefault $Brief.risk $property @() }
    foreach ($property in @('paths', 'scopes', 'directModules', 'downstreamModules')) { Add-LlmWikiBriefDefault $Brief.change $property @() }
    foreach ($property in @('relatedAdrs')) { Add-LlmWikiBriefDefault $Brief.decisionContext $property @() }
    Add-LlmWikiBriefDefault $Brief.decisionContext 'reviewRequired' $false
    Add-LlmWikiBriefDefault $Brief.decisionContext 'guidance' $null
    foreach ($property in @('preDeploy', 'deploy', 'postDeploy', 'rollback')) { Add-LlmWikiBriefDefault $Brief.rolloutPlan $property @() }
    foreach ($property in @('dependencyViolations', 'untrackedProductionProjects', 'moduleCycleNodes')) { Add-LlmWikiBriefDefault $Brief.architectureHealthImpact $property @() }
    foreach ($property in @('contracts', 'productionConsumers', 'testConsumers')) { Add-LlmWikiBriefDefault $Brief.backendContractImpact $property @() }
    foreach ($property in @('components', 'downstreamConsumers')) { Add-LlmWikiBriefDefault $Brief.frontendContractImpact $property @() }
    Add-LlmWikiBriefDefault $Brief.privacyImpact 'fields' @()
    foreach ($property in @('types', 'invariants', 'mappings')) { Add-LlmWikiBriefDefault $Brief.domainDataImpact $property @() }
    return $Brief
}
