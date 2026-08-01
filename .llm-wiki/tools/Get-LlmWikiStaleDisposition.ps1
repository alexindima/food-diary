[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string[]]$FailedTool,
    [string[]]$WorkspaceChangedPath = @()
)

$indexArtifacts = @{
    'Build-LlmWikiCatalog.ps1' = '.llm-wiki/generated/repository-catalog.json'
    'Build-LlmWikiSymbolIndex.ps1' = '.llm-wiki/generated/csharp-symbol-index.json'
    'Build-LlmWikiFrontendIndex.ps1' = '.llm-wiki/generated/frontend-index.json'
    'Build-LlmWikiFrontendContractIndex.ps1' = '.llm-wiki/generated/frontend-contract-index.json'
    'Build-LlmWikiDomainDataIndex.ps1' = '.llm-wiki/generated/domain-data-index.json'
    'Build-LlmWikiConfigurationIndex.ps1' = '.llm-wiki/generated/configuration-index.json'
    'Build-LlmWikiRuntimeTopology.ps1' = '.llm-wiki/generated/runtime-topology.json'
    'Build-LlmWikiSensitiveDataIndex.ps1' = '.llm-wiki/generated/sensitive-data-index.json'
    'Build-LlmWikiBackendContractIndex.ps1' = '.llm-wiki/generated/backend-contract-index.json'
    'Build-LlmWikiQualityIndex.ps1' = '.llm-wiki/generated/quality-index.json'
    'Build-LlmWikiArchitectureHealthIndex.ps1' = '.llm-wiki/generated/architecture-health-index.json'
}

$normalizedWorkspacePaths = @(
    $WorkspaceChangedPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_.Replace('\', '/') } |
        Sort-Object -Unique
)
$artifacts = @($FailedTool | ForEach-Object { $indexArtifacts[$_] } | Where-Object { $_ })
$modifiedArtifacts = @($artifacts | Where-Object { $normalizedWorkspacePaths -contains $_ })
$allFailuresMapped = $artifacts.Count -eq $FailedTool.Count
$allArtifactsModified = $artifacts.Count -gt 0 -and $modifiedArtifacts.Count -eq $artifacts.Count

[pscustomobject]@{
    disposition = $(if ($allFailuresMapped -and $allArtifactsModified) { 'deferred-possibly-concurrent' } else { 'blocking-stale' })
    canDefer = $allFailuresMapped -and $allArtifactsModified
    artifacts = $artifacts
    modifiedArtifacts = $modifiedArtifacts
}
