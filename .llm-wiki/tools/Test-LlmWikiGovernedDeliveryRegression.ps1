[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$wikiRoot = Split-Path -Parent $toolsRoot
$repositoryRoot = Split-Path -Parent $wikiRoot
$workspace = '.artifacts/llm-wiki/tasks/governed-delivery-regression'
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
    Assert-Regression (@($before.conformance.classification.governanceGeneratedPaths) -contains $generatedPath) 'Generated Wiki output was not reported separately.'

    $after = & (Join-Path $toolsRoot 'Manage-LlmWikiPlanConformance.ps1') replan `
        -WorkspacePath $workspace `
        -Reason 'Synchronize the manifest with the declared task contract.' `
        -Format Json | ConvertFrom-Json
    Assert-Regression $after.valid "Replan did not synchronize the manifest to the task-contract boundary: $($after.conformance.policyFindings | ConvertTo-Json -Compress)"
    Assert-Regression (@($after.conformance.classification.outOfScopePaths).Count -eq 0) 'Replan retained false out-of-scope paths.'
    $manifest = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'change-manifest.json') -Raw | ConvertFrom-Json
    Assert-Regression (@($manifest.scope.allowedPathPatterns) -contains '^FoodDiary\.Application/') 'Replan did not take allowed paths from task-contract.json.'

    Write-Host 'LLM Wiki governed-delivery regression passed: generated scope isolation and contract-synchronized replan work.'
} finally {
    if (Test-Path -LiteralPath $absoluteWorkspace) { Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force }
}
