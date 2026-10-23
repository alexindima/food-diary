[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$wikiRoot = Split-Path -Parent $toolsRoot
$repositoryRoot = Split-Path -Parent $wikiRoot
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$workspace = New-LlmWikiSmokeFixtureRepositoryPath -RepositoryRoot $repositoryRoot -Name 'governed-delivery'
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$productPath = 'FoodDiary.Application/Fasting/Commands/FastingCommand.cs'
$generatedPath = '.llm-wiki/generated/quality-index.json'
$finishFixtureRoot = $null

function Assert-Regression([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

if (Test-Path -LiteralPath $absoluteWorkspace) { Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force }
$null = New-Item -ItemType Directory -Path $absoluteWorkspace -Force
try {
    $packet = & (Join-Path $toolsRoot 'Get-LlmWikiChangePacket.ps1') `
        -ChangedPath @($productPath, $generatedPath) `
        -Objective 'Verify governed delivery state synchronization.' `
        -Format Json | ConvertFrom-Json
    [IO.File]::WriteAllText(
        (Join-Path $absoluteWorkspace 'change-packet.json'),
        (($packet | ConvertTo-Json -Depth 20) + [Environment]::NewLine),
        [Text.UTF8Encoding]::new($false))

    & (Join-Path $toolsRoot 'Manage-LlmWikiTaskContract.ps1') init `
        -Path "$workspace/task-contract.json" `
        -Objective 'Verify governed delivery state synchronization.' `
        -AllowedPath '^FoodDiary\.Application/' | Out-Null
    & (Join-Path $toolsRoot 'Manage-LlmWikiTaskJournal.ps1') init `
        -WorkspacePath $workspace | Out-Null
    & (Join-Path $toolsRoot 'Manage-LlmWikiChangeManifest.ps1') init `
        -Path "$workspace/change-manifest.json" `
        -Objective 'Verify governed delivery state synchronization.' `
        -ChangedPath $productPath `
        -AllowedPath '^tests/' | Out-Null

    $before = & (Join-Path $toolsRoot 'Manage-LlmWikiPlanConformance.ps1') assess `
        -WorkspacePath $workspace `
        -Format Json | ConvertFrom-Json
    Assert-Regression (-not $before.valid) 'A stale manifest boundary should fail before replan.'
    Assert-Regression (@($before.conformance.classification.outOfScopePaths) -contains $productPath) 'The production path was not identified as stale-manifest drift.'
    Assert-Regression (@($before.conformance.classification.outOfScopePaths) -notcontains $generatedPath) 'Generated Wiki output incorrectly became product scope drift.'
    Assert-Regression (
        @($before.conformance.classification.governanceGeneratedPaths) -contains $generatedPath -or
        @($packet.diff.changedPaths) -notcontains $generatedPath
    ) 'Generated Wiki output was neither isolated as governance provenance nor suppressed as bookkeeping.'
    $derivedPolicy = & (Join-Path $toolsRoot 'Test-LlmWikiChangePolicy.ps1') `
        -ChangedPath @($generatedPath, '.llm-wiki/reviews/source-impact-reviews.json', '.llm-wiki/knowledge/verification-telemetry.json') `
        -Format Json | ConvertFrom-Json
    Assert-Regression (
        @($derivedPolicy.productPaths).Count -eq 0 -and
        @($derivedPolicy.derivedWikiPaths).Count -eq 2 -and
        @($derivedPolicy.operationalArtifacts).Count -eq 1 -and
        @($derivedPolicy.requiredChecks).Count -eq 0
    ) 'Derived Wiki and operational artifacts changed the product verification requirements.'

    $manifestHashBeforeFailedReplan = (Get-FileHash -LiteralPath (Join-Path $absoluteWorkspace 'change-manifest.json') -Algorithm SHA256).Hash
    Remove-Item -LiteralPath (Join-Path $absoluteWorkspace 'journal.json') -Force
    $failedReplanRejected = $false
    try {
        & (Join-Path $toolsRoot 'Manage-LlmWikiPlanConformance.ps1') replan `
            -WorkspacePath $workspace `
            -Reason 'Exercise transactional rollback.' | Out-Null
    } catch {
        $failedReplanRejected = $_.Exception.Message -match 'journal does not exist'
    }
    Assert-Regression $failedReplanRejected 'Replan did not surface the journal write failure.'
    Assert-Regression ((Get-FileHash -LiteralPath (Join-Path $absoluteWorkspace 'change-manifest.json') -Algorithm SHA256).Hash -eq $manifestHashBeforeFailedReplan) 'Failed replan left a partially updated manifest.'
    & (Join-Path $toolsRoot 'Manage-LlmWikiTaskJournal.ps1') init -WorkspacePath $workspace | Out-Null

    $replanText = & (Join-Path $toolsRoot 'Manage-LlmWikiPlanConformance.ps1') replan `
        -WorkspacePath $workspace `
        -Reason 'Synchronize the manifest with the declared task contract.' `
        -Format Text 6>&1
    Assert-Regression (($replanText -join "`n") -match 'Plan conformance: action=replan') 'Text replan failed while rendering a result without validation issues.'
    $after = & (Join-Path $toolsRoot 'Manage-LlmWikiPlanConformance.ps1') assess `
        -WorkspacePath $workspace `
        -Format Json | ConvertFrom-Json
    $nonVerificationFindings = @($after.conformance.policyFindings | Where-Object id -ne 'new-required-checks')
    Assert-Regression ($nonVerificationFindings.Count -eq 0) "Replan did not synchronize the manifest to the task-contract boundary: $($after.conformance.policyFindings | ConvertTo-Json -Compress)"
    Assert-Regression (@($after.conformance.classification.outOfScopePaths).Count -eq 0) 'Replan retained false out-of-scope paths.'
    $manifest = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-manifest.json') -Raw | ConvertFrom-Json
    Assert-Regression (@($manifest.scope.allowedPathPatterns) -contains '^FoodDiary\.Application/') 'Replan did not take allowed paths from task-contract.json.'

    $manualEvidencePath = "$workspace/evidence.json"
    & (Join-Path $toolsRoot 'Manage-LlmWikiEvidence.ps1') init `
        -Path $manualEvidencePath `
        -ChangedPath 'docs/backend/BACKEND_MIGRATION_SAFETY.md' | Out-Null
    $manualEvidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'evidence.json') -Raw | ConvertFrom-Json
    Assert-Regression (@($manualEvidence.checks).Count -eq 0) 'The supplemental lineage fixture must have no policy-required checks.'
    & (Join-Path $toolsRoot 'Manage-LlmWikiEvidence.ps1') check `
        -Path $manualEvidencePath -Id 'manual-regression' -Status passed `
        -Command 'Write-Output fixture' -Reason 'Manual attestation fixture for supplemental lineage validation.' | Out-Null
    $manualLineage = & (Join-Path $toolsRoot 'Test-LlmWikiEvidenceLineage.ps1') `
        -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Regression ($manualLineage.valid -and $manualLineage.reusableCount -eq 1) 'Supplemental lineage failed when the policy required no checks.'
    $manualEvidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'evidence.json') -Raw | ConvertFrom-Json
    $manualEvidence.supplementalChecks[0].lineage.compatibilityFingerprint = ('0' * 64)
    [IO.File]::WriteAllText(
        (Join-Path $absoluteWorkspace 'evidence.json'),
        (($manualEvidence | ConvertTo-Json -Depth 20) + [Environment]::NewLine),
        [Text.UTF8Encoding]::new($false))
    $tamperedManualLineage = & (Join-Path $toolsRoot 'Test-LlmWikiEvidenceLineage.ps1') `
        -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Regression (-not $tamperedManualLineage.valid) 'Supplemental lineage accepted a tampered fingerprint without policy-required checks.'

    # Exercise the real finish script against isolated assessment boundaries. The
    # projection uses fresh Git-visible fixture paths; unrelated readiness tools
    # are stubs. Stop at final status so no fixture can write learning records.
    $finishFixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'task-finish-scope' -IndependentRepository
    $finishTools = Join-Path $finishFixtureRoot '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $finishTools -Force
    [IO.File]::Copy((Join-Path $toolsRoot 'Complete-LlmWikiTaskWorkspace.ps1'), (Join-Path $finishTools 'Complete-LlmWikiTaskWorkspace.ps1'))
    [IO.File]::WriteAllText((Join-Path $finishFixtureRoot '.gitignore'), ".artifacts/`nassessment-calls.json`n", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $finishTools 'Get-LlmWikiWorkspacePolicy.ps1'), @'
[CmdletBinding()]
param([Parameter(Position = 0)][string]$Action, [switch]$WithFingerprint, [string]$Format)
@{ fingerprint = ('1' * 64) } | ConvertTo-Json
'@, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $finishTools 'LlmWikiAssessmentRunner.ps1'), @'
function Invoke-LlmWikiAssessment {
    param([string]$ToolName, [hashtable]$Arguments)
    $fixtureRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    if ($ToolName -ne 'Manage-LlmWikiTaskWorkspace.ps1') {
        $expectedTools = @('Manage-LlmWikiPlanConformance.ps1', 'Manage-LlmWikiRequirementModel.ps1',
            'Manage-LlmWikiImpactSimulation.ps1', 'Manage-LlmWikiProofOfChange.ps1',
            'Manage-LlmWikiFailurePrediction.ps1', 'Manage-LlmWikiVerificationCost.ps1',
            'Manage-LlmWikiConfidenceLedger.ps1', 'Manage-LlmWikiChangeCritique.ps1')
        if ($ToolName -notin $expectedTools) { throw "Unexpected fixture assessment: $ToolName" }
        return [pscustomobject]@{ valid = $true; ledger = @{ verdict = 'ready' }; critique = @{ verdict = 'accept' } }
    }
    $paths = if ($Arguments.ContainsKey('ChangedPath')) { @($Arguments.ChangedPath) } else {
        @(& git -C $fixtureRoot diff --name-only --) + @(& git -C $fixtureRoot ls-files --others --exclude-standard)
    }
    $paths = @($paths | Where-Object { $_ } | Sort-Object -Unique)
    $logPath = Join-Path $fixtureRoot 'assessment-calls.json'
    $calls = @(Get-Content -LiteralPath $logPath -Raw | ConvertFrom-Json)
    $calls += [pscustomobject]@{ action = $Arguments.Action; paths = $paths; explicit = $Arguments.ContainsKey('ChangedPath') }
    [IO.File]::WriteAllText($logPath, (ConvertTo-Json -InputObject @($calls) -Depth 6), [Text.UTF8Encoding]::new($false))
    $workspaceRoot = Join-Path $fixtureRoot $Arguments.WorkspacePath
    $outOfScope = @($paths | Where-Object { $_ -notin @('owned-a.txt', 'owned-b.txt') })
    if ($outOfScope.Count -gt 0 -or (Test-Path -LiteralPath (Join-Path $workspaceRoot 'block-readiness'))) {
        return [pscustomobject]@{ verdict = 'blocked'; score = 0; nextActions = @('Resolve scope or readiness.'); objective = 'scope fixture' }
    }
    if ($Arguments.Action -eq 'refresh') {
        [IO.File]::WriteAllText((Join-Path $workspaceRoot 'change-packet.json'),
            (ConvertTo-Json -InputObject @{ diff = @{ changedPaths = $paths } } -Depth 5), [Text.UTF8Encoding]::new($false))
        return [pscustomobject]@{ valid = $true }
    }
    if (@($calls | Where-Object action -eq 'status').Count -eq 2) { throw 'Fixture reached final scoped readiness.' }
    return [pscustomobject]@{ verdict = 'ready'; score = 100; nextActions = @(); objective = 'scope fixture' }
}
'@, [Text.UTF8Encoding]::new($false))
    & git -C $finishFixtureRoot add -- .llm-wiki .gitignore
    if ($LASTEXITCODE -ne 0) { throw 'Unable to prepare isolated finish fixture index.' }
    foreach ($name in @('owned-a.txt', 'owned-b.txt', 'foreign.txt')) {
        [IO.File]::WriteAllText((Join-Path $finishFixtureRoot $name), $name, [Text.UTF8Encoding]::new($false))
    }
    $finishWorkspace = '.artifacts/llm-wiki/tasks/finish-scope'
    $finishWorkspaceRoot = Join-Path $finishFixtureRoot $finishWorkspace
    $null = New-Item -ItemType Directory -Path $finishWorkspaceRoot -Force
    foreach ($name in @('workspace.json', 'change-packet.json', 'task-contract.json', 'change-manifest.json',
        'acceptance-matrix.json', 'evidence.json', 'journal.json', 'review-report.md')) {
        [IO.File]::WriteAllText((Join-Path $finishWorkspaceRoot $name), '{}', [Text.UTF8Encoding]::new($false))
    }
    $finishScript = Join-Path $finishTools 'Complete-LlmWikiTaskWorkspace.ps1'
    $finishLog = Join-Path $finishFixtureRoot 'assessment-calls.json'
    foreach ($explicit in @($false, $true)) {
        [IO.File]::WriteAllText($finishLog, '[]', [Text.UTF8Encoding]::new($false))
        $arguments = @{ WorkspacePath = $finishWorkspace; DryRun = $true; Format = 'Json' }
        if ($explicit) { $arguments.ChangedPath = @('owned-a.txt', 'foreign.txt') }
        $rejected = $false
        $finishError = ''
        try { & $finishScript @arguments | Out-Null } catch {
            $finishError = $_.Exception.Message
            $rejected = $finishError -match 'is not ready'
        }
        Assert-Regression $rejected "Default or explicitly foreign scope did not block task finish (explicit=$explicit): $finishError"
        $calls = @(Get-Content -LiteralPath $finishLog -Raw | ConvertFrom-Json)
        Assert-Regression (@($calls[0].paths) -contains 'foreign.txt') 'Finish silently removed a foreign path from its requested scope.'
        Assert-Regression (-not (Test-Path -LiteralPath (Join-Path $finishWorkspaceRoot 'completion.json'))) 'Rejected finish created a seal.'
    }
    [IO.File]::WriteAllText($finishLog, '[]', [Text.UTF8Encoding]::new($false))
    $ownedPaths = @('owned-a.txt', 'owned-b.txt')
    $preview = & $finishScript -WorkspacePath $finishWorkspace -ChangedPath $ownedPaths -DryRun -Format Json | ConvertFrom-Json
    Assert-Regression ($preview.valid -and $preview.dryRun) 'Explicit owned scope failed finish preview.'
    [IO.File]::WriteAllText((Join-Path $finishWorkspaceRoot 'block-readiness'), '', [Text.UTF8Encoding]::new($false))
    $notReadyRejected = $false
    try { & $finishScript -WorkspacePath $finishWorkspace -ChangedPath $ownedPaths -DryRun | Out-Null }
    catch { $notReadyRejected = $_.Exception.Message -match 'is not ready' }
    Assert-Regression $notReadyRejected 'Explicit scope bypassed an unresolved readiness gate.'
    [IO.File]::Delete((Join-Path $finishWorkspaceRoot 'block-readiness'))
    [IO.File]::WriteAllText($finishLog, '[]', [Text.UTF8Encoding]::new($false))
    $finalStatusReached = $false
    try { & $finishScript -WorkspacePath $finishWorkspace -ChangedPath $ownedPaths -Format Json | Out-Null }
    catch { $finalStatusReached = $_.Exception.Message -eq 'Fixture reached final scoped readiness.' }
    Assert-Regression $finalStatusReached 'Finish did not preserve owned scope through its fresh final status.'
    $calls = @(Get-Content -LiteralPath $finishLog -Raw | ConvertFrom-Json)
    Assert-Regression (($calls.action -join ',') -eq 'status,refresh,status') 'Finish skipped a fresh workspace assessment.'
    foreach ($call in $calls) {
        Assert-Regression ($call.explicit -and (@($call.paths) -join ',') -eq ($ownedPaths -join ',')) 'A finish assessment lost the explicit owned path array.'
    }
    $refreshedPacket = Get-Content -LiteralPath (Join-Path $finishWorkspaceRoot 'change-packet.json') -Raw | ConvertFrom-Json
    Assert-Regression ((@($refreshedPacket.diff.changedPaths) -join ',') -eq ($ownedPaths -join ',')) 'Fresh finish refresh acquired foreign paths.'
    Assert-Regression (-not (Test-Path -LiteralPath (Join-Path $finishWorkspaceRoot 'completion.json'))) 'The final-status fixture boundary allowed a seal.'
    # Run the standalone mapping fixture natively so an exit failure cannot be hidden.
    $mappingRegressionHost = (Get-Process -Id $PID).Path
    & $mappingRegressionHost -NoProfile -File (Join-Path $toolsRoot 'Test-LlmWikiTaskEvidenceMappingRetention.ps1')
    if ($LASTEXITCODE -ne 0) { throw "Mapping retention regression failed with native exit code $LASTEXITCODE." }
    Write-Host 'LLM Wiki governed-delivery regression passed: generated scope isolation, replan, lineage and explicit finish scope work.'
} finally {
    if ($finishFixtureRoot -and (Test-Path -LiteralPath $finishFixtureRoot)) {
        $finishParent = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository)).TrimEnd('\', '/')
        $finishTarget = [IO.Path]::GetFullPath($finishFixtureRoot)
        if (-not $finishTarget.StartsWith($finishParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Finish fixture cleanup escaped its owned parent.' }
        Remove-Item -LiteralPath $finishTarget -Recurse -Force
    }
    $taskParent = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks')).TrimEnd('\', '/')
    if (-not ([IO.Path]::GetFullPath($absoluteWorkspace)).StartsWith($taskParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Governed fixture cleanup escaped its task parent.' }
    if (Test-Path -LiteralPath $absoluteWorkspace) { Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force }
}
