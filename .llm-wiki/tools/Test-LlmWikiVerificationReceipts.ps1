[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiVerificationReceipts.ps1')
$command = 'dotnet test tests/Example.Tests.csproj --no-restore'
$receiptRoot = Get-LlmWikiVerificationReceiptRoot $repositoryRoot
$receiptPath = Join-Path $receiptRoot "$(Get-LlmWikiSha256 (Normalize-LlmWikiVerificationCommand $command)).json"
$planCommand = 'dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --no-restore'
$planReceiptPath = Join-Path $receiptRoot "$(Get-LlmWikiSha256 (Normalize-LlmWikiVerificationCommand $planCommand)).json"
$importCommand = 'dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj'
$importReceiptPath = Join-Path $receiptRoot "$(Get-LlmWikiSha256 (Normalize-LlmWikiVerificationCommand $importCommand)).json"
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$workspace = New-LlmWikiSmokeFixtureRepositoryPath -RepositoryRoot $repositoryRoot -Name 'verification-receipts'
$absoluteWorkspace = Join-Path $repositoryRoot $workspace

try {
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationReceipts.ps1') Record `
        -RepositoryRoot $repositoryRoot `
        -Command $command `
        -DurationSeconds 12.5 `
        -CoverageScope @('example', 'contract') `
        -Format Json | Out-Null
    $receipt = @(Get-LlmWikiVerificationReceipts $repositoryRoot | Where-Object normalizedCommand -eq (Normalize-LlmWikiVerificationCommand $command))[0]
    if ($null -eq $receipt) { throw 'Recorded verification receipt was not found.' }
    if (-not $receipt.validForCurrentState) { throw 'Fresh verification receipt was not valid for the current state.' }
    if ([double]$receipt.durationSeconds -ne 12.5) { throw 'Verification duration was not preserved.' }
    if ((@($receipt.coverageScope) -join '|') -cne 'contract|example') { throw 'Verification coverage scope was not normalized.' }

    & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationReceipts.ps1') Record `
        -RepositoryRoot $repositoryRoot `
        -Command $planCommand `
        -DurationSeconds 21 `
        -CoverageScope 'application-contract' `
        -Format Json | Out-Null
    $plan = & (Join-Path $PSScriptRoot 'Get-LlmWikiTestPlan.ps1') `
        -ChangedPath 'FoodDiary.Application.Abstractions/Users/Common/ICurrentUserAccessService.cs' `
        -Format Json | ConvertFrom-Json
    $normalizedPlanCommand = Normalize-LlmWikiVerificationCommand $planCommand
    $applicationCheck = @($plan.commands | Where-Object {
        (Normalize-LlmWikiVerificationCommand ([string]$_.command)) -ceq $normalizedPlanCommand
    } | Select-Object -First 1)
    if ($applicationCheck.Count -eq 0) { throw 'Test plan omitted the focused application verification command.' }
    $applicationCheck = $applicationCheck[0]
    if ($applicationCheck.status -ne 'satisfied' -or [double]$applicationCheck.receipt.durationSeconds -ne 21) {
        throw 'Test plan did not reuse matching verification evidence.'
    }
    if (@($plan.commands | Where-Object id -eq 'composition-confidence').Count -ne 1) {
        throw 'Test plan did not group a broad consumer set into composition confidence.'
    }
    if (@($plan.commands | Where-Object id -eq 'compile-direct-consumer').Count -ne 0) {
        throw 'Test plan emitted noisy per-project builds for a broad consumer set.'
    }

    & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationReceipts.ps1') Record `
        -RepositoryRoot $repositoryRoot `
        -Command $importCommand `
        -DurationSeconds 8 `
        -CoverageScope 'architecture' `
        -Format Json | Out-Null

    $null = New-Item -ItemType Directory -Path $absoluteWorkspace -Force
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') init `
        -Path "$workspace/evidence.json" `
        -ChangedPath 'FoodDiary.Application.Abstractions/Users/Common/ICurrentUserAccessService.cs' | Out-Null
    $import = & (Join-Path $PSScriptRoot 'Import-LlmWikiEvidenceReceipts.ps1') `
        -WorkspacePath $workspace `
        -Format Json | ConvertFrom-Json
    if ($import.importedCount -lt 1 -or 'architecture-tests' -notin @($import.importedCheckIds)) {
        throw 'Explicit evidence receipt import did not restore the matching current check.'
    }
    $importedEvidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'evidence.json') -Raw | ConvertFrom-Json
    $importedCheck = $importedEvidence.checks | Where-Object id -eq 'architecture-tests' | Select-Object -First 1
    if ($importedCheck.status -ne 'passed' -or $null -eq $importedCheck.lineage) {
        throw 'Imported evidence did not retain a current lineage attestation.'
    }
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') check `
        -Path "$workspace/evidence.json" `
        -Id architecture-tests `
        -Status passed-with-known-baseline-failures `
        -Command $importCommand `
        -Reason 'Two named architecture failures were reproduced at the pinned baseline and are unrelated to this change.' | Out-Null
    $baselineEvidence = Get-Content -LiteralPath (Join-Path $absoluteWorkspace 'evidence.json') -Raw | ConvertFrom-Json
    $baselineCheck = $baselineEvidence.checks | Where-Object id -eq 'architecture-tests' | Select-Object -First 1
    $baselineLineage = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') `
        -EvidencePath "$workspace/evidence.json" `
        -Format Json | ConvertFrom-Json
    if ($baselineCheck.status -ne 'passed-with-known-baseline-failures' -or -not $baselineLineage.valid) {
        throw 'Known-baseline-failure evidence was not treated as resolved, explicit, lineage-valid evidence.'
    }
} finally {
    Remove-Item -LiteralPath $receiptPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $planReceiptPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $importReceiptPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki verification receipt tests passed.'
