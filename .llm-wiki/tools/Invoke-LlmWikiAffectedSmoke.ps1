[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string[]]$ChangedPath,
    [switch]$Plan,
    [Alias('Group')]
    [string[]]$RequestedGroup,
    [switch]$NoCache,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$wikiRoot = Split-Path -Parent $toolsRoot
$repositoryRoot = Split-Path -Parent $wikiRoot
. (Join-Path $toolsRoot 'LlmWikiGitPaths.ps1')

if (-not $PSBoundParameters.ContainsKey('ChangedPath')) {
    $ChangedPath = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('diff', '--name-only', '--diff-filter=ACMRD', $BaseRef, '--') -FailureMessage "Unable to collect changed paths from '$BaseRef'.")
    if ($BaseRef -eq 'HEAD') {
        $ChangedPath += @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to collect untracked paths.')
    }
}
$paths = @($ChangedPath | Where-Object { $_ } | ForEach-Object { ConvertTo-LlmWikiRepositoryPath $_ } | Sort-Object -Unique)
$hasExplicitChangedPaths = $paths.Count -gt 0
$forcedGroups = @(
    if ($PSBoundParameters.ContainsKey('RequestedGroup')) {
        $RequestedGroup | Where-Object { $_ } | Sort-Object -Unique
    }
)
if ($paths.Count -eq 0) {
    if ($forcedGroups.Count -gt 0) {
        $paths = @('.llm-wiki/tools/__forced-focused-smoke__.ps1')
    } else {
        if ($Plan -and $Format -eq 'Json') {
            [pscustomobject][ordered]@{ changedPathCount = 0; groups = @() } | ConvertTo-Json -Depth 3
            exit 0
        }
        Write-Host 'Affected tools smoke: no changed paths; nothing to run.'
        exit 0
    }
}

$smokeGroups = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

$hasUnknownToolChange = $false
$wikiRelevantPathCount = 0
if ($forcedGroups.Count -gt 0 -and -not $hasExplicitChangedPaths) {
    foreach ($forcedGroup in $forcedGroups) { $null = $smokeGroups.Add($forcedGroup) }
    $wikiRelevantPathCount = 1
}
if ($smokeGroups.Count -eq 0) {
    foreach ($path in $paths) {
        if ($path -notmatch '^\.llm-wiki/') { continue }
        $wikiRelevantPathCount++
        if ($path -match '^\.llm-wiki/(tools/(Get-LlmWikiAdaptiveWorkflow|Start-LlmWikiDevelopment|Get-LlmWikiSolutionComparison|Test-LlmWikiAdaptiveWorkflow)|policies/experience-policies\.json|workflows/developer-experience\.md)') {
        $null = $smokeGroups.Add('adaptive-routing')
    } elseif ($path -match '^\.llm-wiki/(tools/(Invoke-LlmWikiAdaptiveVerification|Get-LlmWikiIntegrationScan|Test-LlmWikiIntegrationScan|Invoke-LlmWikiEvals)|evals/|workflows/(integration-scan|evals|learned-regression-evals)\.md)') {
        $null = $smokeGroups.Add('adaptive-evals')
    } elseif ($path -match '^\.llm-wiki/(policies/change-policies\.json|tools/(Get-LlmWikiChangePolicy|Test-LlmWikiChangePolicy)\.ps1)$') {
        $null = $smokeGroups.Add('change-policy')
    } elseif ($path -match '^\.llm-wiki/tools/Get-LlmWikiDesignCheckpoint\.ps1$') {
        $null = $smokeGroups.Add('adaptive-experience')
    } elseif ($path -match '^\.llm-wiki/(tools/Get-LlmWikiDependencyChanges|workflows/dependency-rollout\.md)') {
        $null = $smokeGroups.Add('dependency-analysis')
    } elseif ($path -match '^\.llm-wiki/tools/(Invoke-LlmWikiReadOnlyTool|Test-LlmWikiReadOnlyGuard)\.ps1$') {
        $null = $smokeGroups.Add('read-only-guard')
    } elseif ($path -match '^\.llm-wiki/(tools/(Invoke-LlmWikiAffectedSmoke|Invoke-LlmWikiParallelSmoke|Invoke-LlmWikiObservedStage|Start-LlmWikiVerifyWorker|Invoke-LlmWikiAdaptiveVerification|Test-LlmWikiAffectedSmokePlanning|Test-LlmWikiObservedStageReceipt|Test-LlmWikiStrictAffected|Test-LlmWikiFormattingReady)|wiki\.ps1|workflows/(adaptive-development|index-pipeline)\.md)') {
        $null = $smokeGroups.Add('facade-contract')
    } elseif ($path -match '^\.llm-wiki/tools/(Invoke-LlmWikiIndexPipeline|LlmWikiIndexCache|LlmWikiIndexTiming|Test-LlmWikiIndexTiming|Test-LlmWikiIndexCheckpoint|LlmWikiGeneratedArtifacts|Build-LlmWikiCatalog|Build-LlmWiki(?:Frontend|FrontendContract|BackendContract|Quality|ArchitectureHealth|ModulePages)Index|Build-LlmWikiModulePages|Test-LlmWikiGeneratedArtifacts|Test-LlmWikiIndexSelection|Test-LlmWikiBackendModuleModel)\.ps1$' -or $path -eq 'docs/architecture/backend-modules.json') {
        $null = $smokeGroups.Add('index-selection')
    } elseif ($path -match '^\.llm-wiki/tools/(Find-LlmWikiFrontendTrace|Find-LlmWikiTrace|Test-LlmWikiTraceOutput)\.ps1$') {
        $null = $smokeGroups.Add('trace-output')
    } elseif ($path -match '^\.llm-wiki/tools/(?:Manage-LlmWikiCodeGraph|Measure-LlmWikiCodeGraph|Test-LlmWikiCodeGraph|Get-LlmWikiGraphResearch|Get-LlmWikiGraphTestPlan)\.ps1$' -or $path -eq '.llm-wiki/tools/code-graph.mjs') {
        $null = $smokeGroups.Add('code-graph')
    } elseif ($path -match '^\.llm-wiki/tools/(Manage-LlmWikiTaskBaseline|Get-LlmWikiDiffContext|Test-LlmWikiTaskBaseline)\.ps1$') {
        $null = $smokeGroups.Add('task-baseline')
    } elseif ($path -match '^\.llm-wiki/tools/(LlmWikiGitPaths|Test-LlmWikiGitPaths)\.ps1$') {
        $null = $smokeGroups.Add('git-paths')
    } elseif ($path -match '^\.llm-wiki/tools/(Initialize-LlmWikiTaskWorkspace|Manage-LlmWikiTaskContract|Manage-LlmWikiTaskWorkspace|Manage-LlmWikiPlanConformance|Test-LlmWikiTaskScope|Test-LlmWikiTaskWorkspace|Update-LlmWikiTaskWorkspace|Manage-LlmWikiTaskJournal|Compare-LlmWikiTaskPolicy)\.ps1$') {
        $null = $smokeGroups.Add('task-scope')
    } elseif ($path -match '^\.llm-wiki/tools/(Get-LlmWikiUiContinuation|Test-LlmWikiUiContinuation|Get-LlmWikiTestPlan)\.ps1$') {
        $null = $smokeGroups.Add('ui-continuation')
    } elseif ($path -match '^\.llm-wiki/tools/(Get-LlmWikiAdaptiveWorkflow|Get-LlmWikiResearchPacket|Test-LlmWikiResearchConfidence)\.ps1$') {
        $null = $smokeGroups.Add('research-confidence')
    } elseif ($path -match '^\.llm-wiki/tools/(LlmWikiImplementationBrief|LlmWikiExtractionPlanning|LlmWikiRequirementCriteria|Get-LlmWikiImplementationPlan|Get-LlmWikiChangePacket|Manage-LlmWikiChangeManifest|Get-LlmWikiReleaseReadiness|Get-LlmWikiReviewReport|Manage-LlmWikiRequirementModel|Manage-LlmWikiAcceptanceMatrix|Manage-LlmWikiProofOfChange|Initialize-LlmWikiTaskWorkspace|Start-LlmWikiDevelopment|Test-LlmWikiImplementationPlan|Test-LlmWikiGovernedExtraction|Test-LlmWikiGovernedAuthenticationStart)\.ps1$') {
        $null = $smokeGroups.Add('implementation-plan')
    } elseif ($path -match '^\.llm-wiki/tools/(Get-LlmWikiReviewReport|Test-LlmWikiReviewReport)\.ps1$') {
        $null = $smokeGroups.Add('reporting')
    } elseif ($path -match '^\.llm-wiki/tools/(LlmWikiVerificationReceipts|Manage-LlmWikiVerificationReceipts|Test-LlmWikiVerificationReceipts)\.ps1$') {
        $null = $smokeGroups.Add('verification-receipts')
    } elseif ($path -match '^\.llm-wiki/tools/(Manage-LlmWikiVerificationCache|Test-LlmWikiVerificationCache|Get-LlmWikiVerificationStageFingerprint|Invoke-LlmWikiFullVerification)\.ps1$') {
        $null = $smokeGroups.Add('verification-cache')
    } elseif ($path -match '^\.llm-wiki/tools/(LlmWikiQueryCache|Test-LlmWikiQueryCache|Get-LlmWikiTaskBrief|Get-LlmWikiResearchPacket|Get-LlmWikiTestPlan)\.ps1$') {
        $null = $smokeGroups.Add('query-cache')
    } elseif ($path -match '^\.llm-wiki/tools/(Get-LlmWikiContractConsumers|Test-LlmWikiContractConsumers)\.ps1$') {
        $null = $smokeGroups.Add('contract-consumers')
    } elseif ($path -match '^\.llm-wiki/tools/(Get-LlmWikiExtractionReadiness|Test-LlmWikiExtractionReadiness)\.ps1$') {
        $null = $smokeGroups.Add('extraction-readiness')
    } elseif ($path -match '^\.llm-wiki/(knowledge/learning-promotions\.json|tools/(Manage-LlmWikiLearningPromotion|Manage-LlmWikiLearningExperiment|Format-LlmWikiLearningExperimentResult|Test-LlmWikiLearningExperimentFormatting|Format-LlmWikiLearningResults|Test-LlmWikiLearningResultFormatting|Test-LlmWikiLearningPromotionMigration|Manage-LlmWikiEvalPromotion|Manage-LlmWikiLearningHealth|Test-LlmWikiKnowledgeIsolation)\.ps1)$') {
        $null = $smokeGroups.Add('knowledge-isolation')
    } elseif ($path -match '^\.llm-wiki/tools/(Manage-LlmWikiMemory|Test-LlmWikiMemoryIsolation)\.ps1$') {
        $null = $smokeGroups.Add('memory')
    } elseif ($path -match '^\.llm-wiki/tools/(Find-LlmWikiContext|Manage-LlmWikiContextBundle)\.ps1$') {
        $null = $smokeGroups.Add('context-bundle')
    } elseif ($path -match '^\.llm-wiki/tools/(Manage-LlmWikiContextFeedback|Test-LlmWikiContextFeedbackMetrics)\.ps1$') {
        $null = $smokeGroups.Add('context-feedback')
    } elseif ($path -match '^\.llm-wiki/tools/(LlmWikiCollections|Test-LlmWikiCollections)\.ps1$') {
        $null = $smokeGroups.Add('strict-shapes')
    } elseif ($path -match '^\.llm-wiki/tools/(Manage-LlmWikiChangeManifest|Manage-LlmWikiAcceptanceMatrix|Test-LlmWikiTestOnlyGovernance)\.ps1$') {
        $null = $smokeGroups.Add('test-only-governance')
    } elseif ($path -match '^\.llm-wiki/tools/(LlmWikiChangePacket|Invoke-LlmWikiDeliveryWorkflow|Manage-LlmWikiPlanConformance|Manage-LlmWikiTaskWorkspace|Manage-LlmWikiTaskEvidence|Manage-LlmWikiAcceptanceMatrix|Manage-LlmWikiChangeCritique|Manage-LlmWikiConfidenceLedger|Manage-LlmWikiImpactSimulation|Manage-LlmWikiRiskCalibration|Manage-LlmWikiFailurePrediction|Manage-LlmWikiVerificationCost|Manage-LlmWikiRequirementModel|New-LlmWikiEvidenceLineage|Update-LlmWikiTaskEvidence|Add-LlmWikiSourceReview|Get-LlmWikiReleaseReadiness|Get-LlmWikiReviewReport|Test-LlmWikiEvidenceLineage|Test-LlmWikiChangePacketMetadata|Test-LlmWikiGovernedDeliveryRegression|Test-LlmWikiGovernedAuthenticationStart)\.ps1$') {
        $null = $smokeGroups.Add('governed-delivery')
    } elseif ($path -match '^\.llm-wiki/tools/') {
        $hasUnknownToolChange = $true
    }
    }
}
if ($wikiRelevantPathCount -eq 0) {
    if ($Plan -and $Format -eq 'Json') {
        [pscustomobject][ordered]@{ changedPathCount = $paths.Count; groups = @() } | ConvertTo-Json -Depth 3
        exit 0
    }
    Write-Host 'Affected tools smoke: no LLM Wiki implementation paths changed; nothing to run.'
    exit 0
}
if ($hasUnknownToolChange) { $null = $smokeGroups.Add('tool-contract') }
if ($smokeGroups.Contains('adaptive-evals')) {
    # The eval shards exercise both routing and experience cases. Avoid replaying
    # the same expensive workflow regressions as separate sibling processes.
    $null = $smokeGroups.Remove('adaptive-routing')
    $null = $smokeGroups.Remove('adaptive-experience')
}
if ($forcedGroups.Count -gt 0) {
    $requestedGroups = $forcedGroups
    $selectedGroups = @($smokeGroups | Where-Object { $_ -in $requestedGroups })
    $smokeGroups = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($selectedGroup in $selectedGroups) { $null = $smokeGroups.Add($selectedGroup) }
}

$planResult = [pscustomobject][ordered]@{ changedPathCount = $paths.Count; groups = @($smokeGroups | Sort-Object) }
if ($Plan -and $Format -eq 'Json') { $planResult | ConvertTo-Json -Depth 3; exit 0 }
Write-Host "Affected tools smoke: $($paths.Count) changed path(s), groups=$(@($smokeGroups | Sort-Object) -join ',')."
if ($Plan) { exit 0 }

$gitDirectoryOutput = @(& git -C $repositoryRoot rev-parse --absolute-git-dir)
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the Git directory for smoke receipts.' }
$gitDirectory = [string]($gitDirectoryOutput | Select-Object -First 1)
$receiptRoot = Join-Path $gitDirectory 'llm-wiki/affected-smoke-groups'
$null = New-Item -ItemType Directory -Path $receiptRoot -Force
foreach ($group in @($smokeGroups | Sort-Object)) {
    $fingerprint = & (Join-Path $toolsRoot 'Get-LlmWikiVerificationStageFingerprint.ps1') `
        -Stage "affected smoke:$group" `
        -Arguments @{ group = $group } `
        -Format Text
    $receiptPath = Join-Path $receiptRoot "$group.json"
    if (-not $NoCache -and (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        try {
            $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
            if ([string]$receipt.fingerprint -ceq [string]$fingerprint) {
                Write-Host "Affected tools smoke group cached: $group ($($receipt.durationSeconds)s)."
                continue
            }
        } catch { }
    }
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    Write-Host "Affected tools smoke group starting: $group"
    switch ($group) {
        'adaptive-routing' {
            & (Join-Path $toolsRoot 'Test-LlmWikiAdaptiveWorkflow.ps1') -Group Routing
            if (-not $?) { exit 1 }
        }
        'adaptive-evals' {
            & (Join-Path $toolsRoot 'Invoke-LlmWikiAdaptiveVerification.ps1') -Scope Evals
            if (-not $?) { exit 1 }
        }
        'adaptive-experience' {
            & (Join-Path $toolsRoot 'Test-LlmWikiAdaptiveWorkflow.ps1') -Group Experience
            if (-not $?) { exit 1 }
        }
        'change-policy' {
            & (Join-Path $toolsRoot 'Test-LlmWikiChangePolicy.ps1')
            if (-not $?) { exit 1 }
        }
        'dependency-analysis' {
            $rootResult = & (Join-Path $toolsRoot 'Get-LlmWikiDependencyChanges.ps1') -BaseRef HEAD -Format Json | ConvertFrom-Json
            Push-Location (Join-Path $repositoryRoot 'FoodDiary.Web.Client')
            try {
                $frontendResult = & (Join-Path $toolsRoot 'Get-LlmWikiDependencyChanges.ps1') -BaseRef HEAD -Format Json | ConvertFrom-Json
            } finally { Pop-Location }
            if ($rootResult.changeCount -ne $frontendResult.changeCount -or
                (@($rootResult.changes | ConvertTo-Json -Depth 7) -join '') -cne (@($frontendResult.changes | ConvertTo-Json -Depth 7) -join '')) {
                throw 'Dependency analysis differs between repository-root and frontend working directories.'
            }
            Write-Host "Dependency analysis smoke passed: $($rootResult.changeCount) current change(s), cwd-independent."
        }
        'facade-contract' {
            & (Join-Path $toolsRoot 'Test-LlmWikiStrictAffected.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiAffectedSmokePlanning.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiObservedStageReceipt.ps1')
            if (-not $?) { exit 1 }
        }
        'read-only-guard' {
            & (Join-Path $toolsRoot 'Test-LlmWikiReadOnlyGuard.ps1')
            if (-not $?) { exit 1 }
        }
        'trace-output' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTraceOutput.ps1')
            if (-not $?) { exit 1 }
        }
        'task-baseline' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTaskBaseline.ps1')
            if (-not $?) { exit 1 }
        }
        'code-graph' {
            & (Join-Path $toolsRoot 'Test-LlmWikiCodeGraph.ps1')
            if (-not $?) { exit 1 }
        }
        'git-paths' {
            & (Join-Path $toolsRoot 'Test-LlmWikiGitPaths.ps1')
            if (-not $?) { exit 1 }
        }
        'task-scope' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTaskScope.ps1')
            if (-not $?) { exit 1 }
        }
        'index-selection' {
            & (Join-Path $toolsRoot 'Test-LlmWikiIndexCheckpoint.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiIndexSelection.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiIndexTiming.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiGeneratedArtifacts.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiBackendModuleModel.ps1')
            if (-not $?) { exit 1 }
        }
        'ui-continuation' {
            & (Join-Path $toolsRoot 'Test-LlmWikiUiContinuation.ps1')
            if (-not $?) { exit 1 }
        }
        'research-confidence' {
            & (Join-Path $toolsRoot 'Test-LlmWikiResearchConfidence.ps1')
            if (-not $?) { exit 1 }
        }
        'implementation-plan' {
            & (Join-Path $toolsRoot 'Test-LlmWikiImplementationPlan.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiGovernedExtraction.ps1')
            if (-not $?) { exit 1 }
        }
        'reporting' {
            & (Join-Path $toolsRoot 'Test-LlmWikiReviewReport.ps1')
            if (-not $?) { exit 1 }
        }
        'verification-cache' {
            & (Join-Path $toolsRoot 'Test-LlmWikiVerificationCache.ps1')
            if (-not $?) { exit 1 }
        }
        'verification-receipts' {
            & (Join-Path $toolsRoot 'Test-LlmWikiVerificationReceipts.ps1')
            if (-not $?) { exit 1 }
        }
        'query-cache' {
            & (Join-Path $toolsRoot 'Test-LlmWikiQueryCache.ps1')
            if (-not $?) { exit 1 }
        }
        'contract-consumers' {
            & (Join-Path $toolsRoot 'Test-LlmWikiContractConsumers.ps1')
            if (-not $?) { exit 1 }
        }
        'extraction-readiness' {
            & (Join-Path $toolsRoot 'Test-LlmWikiExtractionReadiness.ps1')
            if (-not $?) { exit 1 }
        }
        'knowledge-isolation' {
            & (Join-Path $toolsRoot 'Test-LlmWikiKnowledgeIsolation.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiLearningExperimentFormatting.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiLearningResultFormatting.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiLearningPromotionMigration.ps1')
            if (-not $?) { exit 1 }
        }
        'memory' {
            & (Join-Path $toolsRoot 'Test-LlmWikiMemoryIsolation.ps1')
            if (-not $?) { exit 1 }
        }
        'context-bundle' {
            & (Join-Path $toolsRoot 'Find-LlmWikiContext.ps1') -Module Users -Format Json | Out-Null
            if (-not $?) { exit 1 }
        }
        'context-feedback' {
            & (Join-Path $toolsRoot 'Test-LlmWikiContextFeedbackMetrics.ps1')
            if (-not $?) { exit 1 }
        }
        'strict-shapes' {
            & (Join-Path $toolsRoot 'Test-LlmWikiCollections.ps1')
            if (-not $?) { exit 1 }
        }
        'test-only-governance' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTestOnlyGovernance.ps1')
            if (-not $?) { exit 1 }
        }
        'governed-delivery' {
            & (Join-Path $toolsRoot 'Test-LlmWikiCollections.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiChangePacketMetadata.ps1')
            & (Join-Path $toolsRoot 'Test-LlmWikiGovernedDeliveryRegression.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiReviewReport.ps1')
            if (-not $?) { exit 1 }
        }
        'tool-contract' {
            $toolContractPaths = if ($hasExplicitChangedPaths) {
                $paths
            } else {
                @(
                    Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('diff', '--name-only', '--diff-filter=ACMRD', $BaseRef, '--', '.llm-wiki/tools/*.ps1') -FailureMessage "Unable to collect changed Wiki tools from '$BaseRef'."
                    if ($BaseRef -eq 'HEAD') {
                        Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard', '--', '.llm-wiki/tools/*.ps1') -FailureMessage 'Unable to collect untracked Wiki tools.'
                    }
                ) | Sort-Object -Unique
            }
            & (Join-Path $toolsRoot 'Test-LlmWikiChangedTools.ps1') -ChangedPath @($toolContractPaths)
            if (-not $?) { exit 1 }
        }
        'full-tools' {
            & (Join-Path $toolsRoot 'Invoke-LlmWikiReadOnlyTool.ps1') `
                -ToolPath (Join-Path $toolsRoot 'Test-LlmWikiTools.ps1')
            if (-not $?) { exit 1 }
        }
    }
    $stopwatch.Stop()
    $durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 2)
    $receipt = [ordered]@{
        schemaVersion = 1
        group = $group
        fingerprint = [string]$fingerprint
        recordedAtUtc = [DateTime]::UtcNow.ToString('o')
        durationSeconds = $durationSeconds
    }
    [IO.File]::WriteAllText($receiptPath, (($receipt | ConvertTo-Json -Depth 4) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
    Write-Host " - ${group}: ${durationSeconds}s"
}
