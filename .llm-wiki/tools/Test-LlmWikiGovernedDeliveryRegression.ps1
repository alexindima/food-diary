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

    Write-Host 'LLM Wiki governed-delivery regression passed: generated scope isolation and contract-synchronized replan work.'
} finally {
    if (Test-Path -LiteralPath $absoluteWorkspace) { Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force }
}
