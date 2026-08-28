[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Stage,
    [hashtable]$Arguments = @{},
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Test-RelevantPath([string]$Path) {
    $normalized = $Path.Replace('\', '/')
    if ($normalized.StartsWith('.git/') -or $normalized.StartsWith('.artifacts/')) { return $false }
    $smokeInfrastructure = '^\.llm-wiki/tools/(?:Invoke-LlmWikiAffectedSmoke|Invoke-LlmWikiParallelSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$'
    switch ($Stage) {
        'workspace policy' { return $normalized -match '(^|/)AGENTS\.md$|^\.llm-wiki/(policies/workspace|tools/Get-LlmWikiWorkspacePolicy)' }
        'page contracts' { return $normalized -match '^\.llm-wiki/(?!generated/|reviews/).+\.(md|json|ps1)$' }
        'lint regression' { return $normalized -match '^\.llm-wiki/(?!generated/|reviews/)' }
        'indexes' { return $normalized -notmatch '^\.llm-wiki/reviews/' }
        'adaptive verification' { return $normalized -match '^\.llm-wiki/(tools|policies|workflows|evals)/' }
        'failure knowledge' { return $normalized -match '^\.llm-wiki/(known-failures|tools/Manage-LlmWikiFailures)' }
        'change policy' { return $normalized -notmatch '^\.llm-wiki/reviews/source-impact-reviews\.json$' }
        'source impact' { return $normalized -match '^\.llm-wiki/' }
        'affected smoke' { return $normalized -match '^\.llm-wiki/(?:tools|policies|workflows|evals)/|^\.llm-wiki/wiki\.ps1$' }
        'affected smoke:adaptive-routing' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/(?:tools/(?:Get-LlmWikiAdaptiveWorkflow|Start-LlmWikiDevelopment|Get-LlmWikiSolutionComparison|Get-LlmWikiWorkflowMetrics|Write-LlmWikiWorkflowMetric|Test-LlmWikiAdaptiveWorkflow)\.ps1|policies/experience-policies\.json|workflows/developer-experience\.md)$' }
        'affected smoke:adaptive-experience' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/tools/(?:Get-LlmWikiDesignCheckpoint|Test-LlmWikiAdaptiveWorkflow)\.ps1$' }
        'affected smoke:adaptive-evals' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/(?:tools/(?:Invoke-LlmWikiAdaptiveVerification|Invoke-LlmWikiEvals|Get-LlmWikiIntegrationScan|Test-LlmWikiIntegrationScan|Get-LlmWikiAdaptiveWorkflow|Test-LlmWikiAdaptiveWorkflow)\.ps1|evals/|workflows/(?:integration-scan|evals|learned-regression-evals)\.md)' }
        'affected smoke:change-policy' { return $normalized -match '^\.llm-wiki/(policies/change-policies\.json|tools/(?:Test-LlmWikiChangePolicy|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1)$' }
        'affected smoke:contract-consumers' { return $normalized -match '^\.llm-wiki/tools/(?:Get-LlmWikiContractConsumers|Test-LlmWikiContractConsumers|Get-LlmWikiExtractionReadiness|Test-LlmWikiExtractionReadiness|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:code-graph' { return $normalized -match '^\.llm-wiki/tools/(?:code-graph\.mjs|(?:Manage-LlmWikiCodeGraph|Measure-LlmWikiCodeGraph|Test-LlmWikiCodeGraph|Get-LlmWikiGraphResearch|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1)$' }
        'affected smoke:standalone-index-migration' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/(?:tools/(?:Get-LlmWikiCompiledIndexMigration|Measure-LlmWikiStandaloneIndexRoutes|Test-LlmWikiStandaloneIndexRoutes)\.ps1|workflows/(?:quality-risk|runtime-impact|domain-data-review|architecture-health-review)\.md)$' }
        'affected smoke:domain-data-query' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/(?:tools/(?:(?:Build-LlmWikiInProcessSqliteReader|LlmWikiInProcessSqlite|Find-LlmWikiDomainData|Test-LlmWikiDomainDataSqlParity)\.ps1|LlmWiki\.SqliteReader/)|workflows/domain-data-review\.md)$' }
        'affected smoke:index-selection' { return $normalized -match '^\.llm-wiki/tools/(?:Invoke-LlmWikiIndexPipeline|LlmWikiIndexCache|LlmWikiIndexTiming|Test-LlmWikiIndexTiming|LlmWikiGeneratedArtifacts|Build-LlmWiki[^/]+|Test-LlmWikiGeneratedArtifacts|Test-LlmWikiIndexSelection|Test-LlmWikiIndexCheckpoint|Test-LlmWikiConcurrentIndexUpdate|Test-LlmWikiBackendModuleModel|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$|^docs/architecture/backend-modules\.json$' }
        'affected smoke:ui-continuation' { return $normalized -match '^\.llm-wiki/tools/(?:Get-LlmWikiUiContinuation|Test-LlmWikiUiContinuation|Get-LlmWikiTestPlan|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:research-confidence' { return $normalized -match '^\.llm-wiki/tools/(?:Get-LlmWikiAdaptiveWorkflow|Get-LlmWikiResearchPacket|Test-LlmWikiResearchConfidence|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:implementation-plan' { return $normalized -match '^\.llm-wiki/tools/(?:LlmWikiImplementationBrief|Get-LlmWikiImplementationPlan|Get-LlmWikiChangePacket|Manage-LlmWikiChangeManifest|Get-LlmWikiReleaseReadiness|Get-LlmWikiReviewReport|Manage-LlmWikiRequirementModel|Initialize-LlmWikiTaskWorkspace|Test-LlmWikiImplementationPlan|Test-LlmWikiGovernedAuthenticationStart|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:context-bundle' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/(?:tools/(?:Find-LlmWikiContext|Manage-LlmWikiContextBundle|Manage-LlmWikiContextSecurity|Manage-LlmWikiVerificationPlan|Measure-LlmWikiSqlContextEvaluation|LlmWikiQueryCache|Test-LlmWikiCompiledIndexSqlParity|Test-LlmWikiContextCache|Test-LlmWikiSqlContextEvaluation|Test-LlmWikiSqlContextShadow|Test-LlmWikiWorkflowRecovery)\.ps1|evals/context-search[^/]*\.json|policies/context-search-ranking\.json)$' }
        'affected smoke:verification-cache' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/tools/(?:Manage-LlmWikiVerificationCache|Manage-LlmWikiVerificationTelemetry|Manage-LlmWikiContextOutcome|Manage-LlmWikiModelRoutingOutcome|Test-LlmWikiVerificationCache|Test-LlmWikiIndexManifest|Test-LlmWikiOperationalTelemetry|Invoke-LlmWikiFullVerification|Write-LlmWikiIndexVerificationReceipt)\.ps1$|^\.llm-wiki/policies/query-indexes\.json$' }
        'affected smoke:verification-receipts' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/tools/(?:LlmWikiVerificationReceipts|Manage-LlmWikiVerificationReceipts|Import-LlmWikiEvidenceReceipts|Test-LlmWikiVerificationReceipts)\.ps1$' }
        'affected smoke:task-scope' { return $normalized -match '^\.llm-wiki/tools/(?:Initialize-LlmWikiTaskWorkspace|Manage-LlmWikiTaskContract|Manage-LlmWikiTaskWorkspace|Manage-LlmWikiPlanConformance|Test-LlmWikiTaskScope|Test-LlmWikiTaskWorkspace|Update-LlmWikiTaskWorkspace|Manage-LlmWikiTaskJournal|Compare-LlmWikiTaskPolicy|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:git-paths' { return $normalized -match '^\.llm-wiki/tools/(?:LlmWikiGitPaths|Test-LlmWikiGitPaths|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:facade-contract' { return $normalized -match '^\.llm-wiki/wiki\.ps1$' -or $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/tools/(?:Invoke-LlmWikiObservedStage|Test-LlmWikiAffectedSmokePlanning|Test-LlmWikiStrictAffected|Test-LlmWikiFormattingReady)\.ps1$' }
        'affected smoke:read-only-guard' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/tools/(?:Invoke-LlmWikiReadOnlyTool|Test-LlmWikiReadOnlyGuard)\.ps1$' }
        'affected smoke:tool-contract' { return $normalized -match $smokeInfrastructure -or $normalized -match '^\.llm-wiki/tools/Test-LlmWikiChangedTools\.ps1$' }
        'affected smoke:test-only-governance' { return $normalized -match '^\.llm-wiki/tools/(?:Manage-LlmWikiChangeManifest|Manage-LlmWikiAcceptanceMatrix|Test-LlmWikiTestOnlyGovernance|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:governed-delivery' { return $normalized -match '^\.llm-wiki/tools/(?:LlmWikiChangePacket|Invoke-LlmWikiDeliveryWorkflow|Invoke-LlmWikiDeliveryFinalization|Complete-LlmWikiTaskWorkspace|Manage-LlmWikiPlanConformance|Manage-LlmWikiTaskWorkspace|Manage-LlmWikiTaskEvidence|Manage-LlmWikiEvidence|Manage-LlmWikiAcceptanceMatrix|Manage-LlmWikiProofOfChange|Manage-LlmWikiContextSecurity|Manage-LlmWikiChangeCritique|Manage-LlmWikiConfidenceLedger|Manage-LlmWikiImpactSimulation|Manage-LlmWikiRiskCalibration|Manage-LlmWikiFailurePrediction|Manage-LlmWikiVerificationCost|Manage-LlmWikiRequirementModel|New-LlmWikiEvidenceLineage|Test-LlmWikiEvidenceLineage|Test-LlmWikiImpactSimulationSqlParity|Update-LlmWikiTaskEvidence|Add-LlmWikiSourceReview|Get-LlmWikiReleaseReadiness|Get-LlmWikiReviewReport|Test-LlmWikiChangePacketMetadata|Test-LlmWikiGovernedDeliveryRegression|Test-LlmWikiGovernedAuthenticationStart|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        { $_ -like 'affected smoke:*' } { return $normalized -match '^\.llm-wiki/(tools|policies|workflows|evals)/|^\.llm-wiki/wiki\.ps1$' }
        default { return $true }
    }
}

$material = [Text.StringBuilder]::new()
$null = $material.AppendLine("stage=$Stage")
$null = $material.AppendLine("head=$((Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', 'HEAD') -FailureMessage 'Unable to resolve HEAD for the verification stage fingerprint.').Lines[0].Trim())")
$canonicalArguments = [ordered]@{}
foreach ($key in @($Arguments.Keys | Sort-Object)) { $canonicalArguments[[string]$key] = $Arguments[$key] }
$null = $material.AppendLine("arguments=$(($canonicalArguments | ConvertTo-Json -Depth 8 -Compress))")
foreach ($line in @((Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('status', '--porcelain=v1', '--untracked-files=all') -FailureMessage 'Unable to enumerate working-tree status for the verification stage fingerprint.').Lines)) {
    $path = ([string]$line).Substring(3).Trim('"')
    if ($path -match ' -> ') { $path = ($path -split ' -> ')[-1] }
    $path = $path.Replace('\', '/')
    if (-not (Test-RelevantPath $path)) { continue }
    $null = $material.AppendLine("path=$path")
    $absolutePath = Join-Path $repositoryRoot $path
    if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
        $null = $material.AppendLine("sha=$((Get-FileHash -LiteralPath $absolutePath -Algorithm SHA256).Hash.ToLowerInvariant())")
    } else {
        $null = $material.AppendLine('state=deleted')
    }
}
$selfHash = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash.ToLowerInvariant()
$null = $material.AppendLine("fingerprinter=$selfHash")
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $fingerprint = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($material.ToString()))) -replace '-', '').ToLowerInvariant()
} finally { $sha.Dispose() }

$result = [pscustomobject][ordered]@{ stage = $Stage; fingerprint = $fingerprint }
if ($Format -eq 'Json') { $result | ConvertTo-Json -Compress } else { $fingerprint }
