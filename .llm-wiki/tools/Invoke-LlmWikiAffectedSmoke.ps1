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
$catalogPath = Join-Path $wikiRoot 'policies/affected-smoke-catalog.psd1'
$smokeCatalog = Import-PowerShellDataFile -LiteralPath $catalogPath
if ([int]$smokeCatalog.SchemaVersion -ne 1) { throw "Unsupported affected-smoke catalog schema: $($smokeCatalog.SchemaVersion)." }
$catalogGroups = @($smokeCatalog.Groups)

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
        $matchedGroup = @($catalogGroups | Where-Object {
            @($_.Patterns | Where-Object { $path -match $_ }).Count -gt 0
        } | Select-Object -First 1)
        if ($matchedGroup.Count -gt 0) {
            $null = $smokeGroups.Add([string]$matchedGroup[0].Id)
            if ($matchedGroup[0].ContainsKey('Fallback') -and [bool]$matchedGroup[0].Fallback) { $hasUnknownToolChange = $true }
        }
        foreach ($additional in @($smokeCatalog.AdditionalMatches | Where-Object { $path -match $_.Pattern })) {
            foreach ($additionalGroup in @($additional.Groups)) { $null = $smokeGroups.Add([string]$additionalGroup) }
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
foreach ($suppression in @($smokeCatalog.Suppressions)) {
    if (-not $smokeGroups.Contains([string]$suppression.When)) { continue }
    foreach ($removedGroup in @($suppression.Remove)) { $null = $smokeGroups.Remove([string]$removedGroup) }
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

$gitDirectory = (Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -FailureMessage 'Unable to resolve the Git directory for smoke receipts.').Lines[0]
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
            & (Join-Path $toolsRoot 'Test-LlmWikiWorkflowMetrics.ps1')
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
            & (Join-Path $toolsRoot 'Test-LlmWikiFacadeCommandCatalog.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiStrictAffected.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiAffectedSmokePlanning.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiObservedStageReceipt.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiConcurrentVerify.ps1')
            if (-not $?) { exit 1 }
        }
        'read-only-guard' {
            & (Join-Path $toolsRoot 'Test-LlmWikiReadOnlyGuard.ps1')
            if (-not $?) { exit 1 }
        }
        'trace-output' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTraceOutput.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiFrontendTraceSqlParity.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiQualityRisk.ps1')
            if (-not $?) { exit 1 }
        }
        'task-baseline' {
            & (Join-Path $toolsRoot 'Test-LlmWikiDiffContextSqlParity.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiTaskBaseline.ps1')
            if (-not $?) { exit 1 }
        }
        'code-graph' {
            & (Join-Path $toolsRoot 'Test-LlmWikiRoslynExtractor.ps1')
            if ($LASTEXITCODE -ne 0) { throw "Roslyn extractor smoke failed with exit code $LASTEXITCODE." }
            & (Join-Path $toolsRoot 'Test-LlmWikiTypeScriptExtractor.ps1')
            if ($LASTEXITCODE -ne 0) { throw "TypeScript extractor smoke failed with exit code $LASTEXITCODE." }
            & (Join-Path $toolsRoot 'Test-LlmWikiCodeGraph.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiTraceOutput.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiFrontendTraceSqlParity.ps1')
            if (-not $?) { exit 1 }
        }
        'api-compatibility' {
            & (Join-Path $toolsRoot 'Test-LlmWikiApiCompatibilityRegression.ps1')
            if (-not $?) { exit 1 }
        }
        'backend-contract-query' {
            & (Join-Path $toolsRoot 'Test-LlmWikiBackendContractSqlParity.ps1')
            if (-not $?) { exit 1 }
        }
        'frontend-contract-query' {
            & (Join-Path $toolsRoot 'Test-LlmWikiFrontendContractSqlParity.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiFrontendRuntimeOwnerSqlParity.ps1')
            if (-not $?) { exit 1 }
        }
        'sensitive-data-query' {
            & (Join-Path $toolsRoot 'Test-LlmWikiSensitiveDataSqlParity.ps1')
            if (-not $?) { exit 1 }
        }
        'domain-data-query' {
            & (Join-Path $toolsRoot 'Test-LlmWikiDomainDataSqlParity.ps1')
            if (-not $?) { exit 1 }
        }
        'standalone-index-migration' {
            & (Join-Path $toolsRoot 'Test-LlmWikiStandaloneIndexRoutes.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiRuntimeArchitectureSqlParity.ps1')
            if (-not $?) { exit 1 }
        }
        'git-paths' {
            & (Join-Path $toolsRoot 'Test-LlmWikiGitPaths.ps1')
            if (-not $?) { exit 1 }
        }
        'task-scope' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTaskScope.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiInstructionExperimentStrictMode.ps1')
            if (-not $?) { exit 1 }
        }
        'index-selection' {
            & (Join-Path $toolsRoot 'Test-LlmWikiIndexFingerprint.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiContractReferenceExtractor.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiIndexCheckpoint.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiConcurrentIndexUpdate.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiIndexSelection.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiIndexTiming.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiGeneratedArtifacts.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiBackendModuleModel.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiArchitectureHealthToolExclusion.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiQualitySelfCoverage.ps1')
            if (-not $?) { exit 1 }
        }
        'ui-continuation' {
            & (Join-Path $toolsRoot 'Test-LlmWikiUiContinuation.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiTestPlanPrecision.ps1')
            if (-not $?) { exit 1 }
        }
        'research-confidence' {
            & (Join-Path $toolsRoot 'Test-LlmWikiResearchConfidence.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiCoveragePlan.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiFailuresSearch.ps1')
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
            & (Join-Path $toolsRoot 'Test-LlmWikiIndexManifest.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiVerificationCache.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiOperationalTelemetry.ps1')
            if (-not $?) { exit 1 }
        }
        'verification-receipts' {
            & (Join-Path $toolsRoot 'Test-LlmWikiVerificationReceipts.ps1')
            if (-not $?) { exit 1 }
        }
        'query-cache' {
            & (Join-Path $toolsRoot 'Test-LlmWikiTaskBriefSqlParity.ps1')
            if (-not $?) { exit 1 }
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
            & (Join-Path $toolsRoot 'Test-LlmWikiInstructionExperimentStrictMode.ps1')
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
            & (Join-Path $toolsRoot 'Test-LlmWikiCompiledIndexSqlParity.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiSqlContextShadow.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiSqlContextEvaluation.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiContextCache.ps1')
            if (-not $?) { exit 1 }
            & (Join-Path $toolsRoot 'Test-LlmWikiWorkflowRecovery.ps1')
            if (-not $?) { exit 1 }
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
            & (Join-Path $toolsRoot 'Test-LlmWikiImpactSimulationSqlParity.ps1')
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
                -ToolPath (Join-Path $toolsRoot 'Test-LlmWikiTools.ps1') `
                -ToolArguments @{ Profile = 'Full' }
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
