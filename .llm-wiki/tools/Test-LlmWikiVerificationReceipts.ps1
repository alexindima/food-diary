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
} finally {
    Remove-Item -LiteralPath $receiptPath -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $planReceiptPath -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki verification receipt tests passed.'
