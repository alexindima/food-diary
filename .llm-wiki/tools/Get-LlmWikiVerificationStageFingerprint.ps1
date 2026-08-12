[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Stage,
    [hashtable]$Arguments = @{},
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Test-RelevantPath([string]$Path) {
    $normalized = $Path.Replace('\', '/')
    if ($normalized.StartsWith('.git/') -or $normalized.StartsWith('.artifacts/')) { return $false }
    switch ($Stage) {
        'workspace policy' { return $normalized -match '(^|/)AGENTS\.md$|^\.llm-wiki/(policies/workspace|tools/Get-LlmWikiWorkspacePolicy)' }
        'page contracts' { return $normalized -match '^\.llm-wiki/(?!generated/|reviews/).+\.(md|json|ps1)$' }
        'lint regression' { return $normalized -match '^\.llm-wiki/(?!generated/|reviews/)' }
        'indexes' { return $normalized -notmatch '^\.llm-wiki/reviews/' }
        'adaptive verification' { return $normalized -match '^\.llm-wiki/(tools|policies|workflows|evals)/' }
        'failure knowledge' { return $normalized -match '^\.llm-wiki/(known-failures|tools/Manage-LlmWikiFailures)' }
        'change policy' { return $normalized -notmatch '^\.llm-wiki/reviews/source-impact-reviews\.json$' }
        'source impact' { return $normalized -match '^\.llm-wiki/' }
        'affected smoke:change-policy' { return $normalized -match '^\.llm-wiki/(policies/change-policies\.json|tools/(?:Test-LlmWikiChangePolicy|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1)$' }
        'affected smoke:contract-consumers' { return $normalized -match '^\.llm-wiki/tools/(?:Get-LlmWikiContractConsumers|Test-LlmWikiContractConsumers|Get-LlmWikiExtractionReadiness|Test-LlmWikiExtractionReadiness|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:index-selection' { return $normalized -match '^\.llm-wiki/tools/(?:Invoke-LlmWikiIndexPipeline|LlmWikiIndexCache|LlmWikiIndexTiming|Test-LlmWikiIndexTiming|LlmWikiGeneratedArtifacts|Build-LlmWiki[^/]+|Test-LlmWikiGeneratedArtifacts|Test-LlmWikiIndexSelection|Test-LlmWikiBackendModuleModel|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$|^docs/architecture/backend-modules\.json$' }
        'affected smoke:ui-continuation' { return $normalized -match '^\.llm-wiki/tools/(?:Get-LlmWikiUiContinuation|Test-LlmWikiUiContinuation|Get-LlmWikiTestPlan|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:research-confidence' { return $normalized -match '^\.llm-wiki/tools/(?:Get-LlmWikiAdaptiveWorkflow|Get-LlmWikiResearchPacket|Test-LlmWikiResearchConfidence|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:implementation-plan' { return $normalized -match '^\.llm-wiki/tools/(?:Get-LlmWikiImplementationPlan|Get-LlmWikiChangePacket|Initialize-LlmWikiTaskWorkspace|Test-LlmWikiImplementationPlan|Test-LlmWikiGovernedAuthenticationStart|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:verification-cache' { return $normalized -match '^\.llm-wiki/tools/(?:Manage-LlmWikiVerificationCache|Test-LlmWikiVerificationCache|LlmWikiVerificationReceipts|Manage-LlmWikiVerificationReceipts|Test-LlmWikiVerificationReceipts|Get-LlmWikiVerificationStageFingerprint|Invoke-LlmWikiFullVerification|Invoke-LlmWikiAffectedSmoke)\.ps1$' }
        'affected smoke:task-scope' { return $normalized -match '^\.llm-wiki/tools/(?:Initialize-LlmWikiTaskWorkspace|Manage-LlmWikiTaskContract|Manage-LlmWikiTaskWorkspace|Manage-LlmWikiPlanConformance|Test-LlmWikiTaskScope|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:git-paths' { return $normalized -match '^\.llm-wiki/tools/(?:LlmWikiGitPaths|Test-LlmWikiGitPaths|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:facade-contract' { return $normalized -match '^\.llm-wiki/wiki\.ps1$|^\.llm-wiki/tools/(?:Invoke-LlmWikiAffectedSmoke|Invoke-LlmWikiObservedStage|Invoke-LlmWikiAdaptiveVerification|Invoke-LlmWikiReadOnlyTool|Test-LlmWikiReadOnlyGuard|Test-LlmWikiStrictAffected|Test-LlmWikiFormattingReady|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:test-only-governance' { return $normalized -match '^\.llm-wiki/tools/(?:Manage-LlmWikiChangeManifest|Manage-LlmWikiAcceptanceMatrix|Test-LlmWikiTestOnlyGovernance|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        'affected smoke:governed-delivery' { return $normalized -match '^\.llm-wiki/tools/(?:Invoke-LlmWikiDeliveryWorkflow|Manage-LlmWikiPlanConformance|Manage-LlmWikiTaskWorkspace|Manage-LlmWikiTaskEvidence|Manage-LlmWikiChangeCritique|Manage-LlmWikiRequirementModel|New-LlmWikiEvidenceLineage|Update-LlmWikiTaskEvidence|Add-LlmWikiSourceReview|Get-LlmWikiReleaseReadiness|Get-LlmWikiReviewReport|Test-LlmWikiGovernedDeliveryRegression|Invoke-LlmWikiAffectedSmoke|Get-LlmWikiVerificationStageFingerprint)\.ps1$' }
        { $_ -like 'affected smoke:*' } { return $normalized -match '^\.llm-wiki/(tools|policies|workflows|evals)/|^\.llm-wiki/wiki\.ps1$' }
        default { return $true }
    }
}

$material = [Text.StringBuilder]::new()
$null = $material.AppendLine("stage=$Stage")
$null = $material.AppendLine("head=$((& git -C $repositoryRoot rev-parse HEAD).Trim())")
$canonicalArguments = [ordered]@{}
foreach ($key in @($Arguments.Keys | Sort-Object)) { $canonicalArguments[[string]$key] = $Arguments[$key] }
$null = $material.AppendLine("arguments=$(($canonicalArguments | ConvertTo-Json -Depth 8 -Compress))")
foreach ($line in @(& git -C $repositoryRoot status --porcelain=v1 --untracked-files=all)) {
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
