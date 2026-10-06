[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiVerificationReceipts.ps1')
$command = 'dotnet test tests/Example.Tests.csproj --no-restore'
$receiptRoot = Get-LlmWikiVerificationReceiptRoot $repositoryRoot
$receiptPath = Join-Path $receiptRoot "$(Get-LlmWikiSha256 (Normalize-LlmWikiVerificationCommand $command)).json"
# The moved Users contract has many module consumers; Ai is an actual focused
# consumer in the bounded current test plan. This fixture tests receipt reuse,
# not a requirement that shared contracts belong to central Application.Tests.
$planCommand = 'dotnet test Modules/Ai/tests/FoodDiary.Modules.Ai.Application.Tests/FoodDiary.Modules.Ai.Application.Tests.csproj --no-restore'
$planReceiptPath = Join-Path $receiptRoot "$(Get-LlmWikiSha256 (Normalize-LlmWikiVerificationCommand $planCommand)).json"
$importCommand = 'dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj'
$importReceiptPath = Join-Path $receiptRoot "$(Get-LlmWikiSha256 (Normalize-LlmWikiVerificationCommand $importCommand)).json"
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$workspace = New-LlmWikiSmokeFixtureRepositoryPath -RepositoryRoot $repositoryRoot -Name 'verification-receipts'
$absoluteWorkspace = Join-Path $repositoryRoot $workspace
$fingerprintFixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'receipt-unicode'
$receiptBackups = @{}
foreach ($path in @($receiptPath, $planReceiptPath, $importReceiptPath)) {
    $receiptBackups[$path] = if ([IO.File]::Exists($path)) { ,([IO.File]::ReadAllBytes($path)) } else { $null }
}

try {
    & git -C $fingerprintFixture init --quiet
    & git -C $fingerprintFixture config core.quotepath true
    [IO.File]::WriteAllText((Join-Path $fingerprintFixture 'baseline.txt'), 'baseline')
    & git -C $fingerprintFixture add baseline.txt
    & git -C $fingerprintFixture -c user.name='Wiki Receipt Test' -c user.email='receipt@example.invalid' commit --quiet -m baseline
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize the receipt fingerprint fixture.' }
    $unicodeName = (-join @([char]0x0444, [char]0x0430, [char]0x0439, [char]0x043B)) + '.cs'
    $unicodePath = Join-Path $fingerprintFixture $unicodeName
    foreach ($tracked in @($false, $true)) {
        [IO.File]::WriteAllText($unicodePath, 'one')
        if ($tracked) {
            & git -C $fingerprintFixture add -- $unicodeName
            & git -C $fingerprintFixture -c user.name='Wiki Receipt Test' -c user.email='receipt@example.invalid' commit --quiet -m unicode
            [IO.File]::WriteAllText($unicodePath, 'two')
        }
        $timestamp = [IO.File]::GetLastWriteTimeUtc($unicodePath)
        $before = Get-LlmWikiVerificationFingerprint $fingerprintFixture
        [IO.File]::WriteAllText($unicodePath, $(if ($tracked) { 'six' } else { 'two' }))
        [IO.File]::SetLastWriteTimeUtc($unicodePath, $timestamp)
        $after = Get-LlmWikiVerificationFingerprint $fingerprintFixture
        if ($unicodeName -notin $after.paths -or $before.fingerprint -ceq $after.fingerprint) {
            throw "Verification fingerprint missed a Unicode content edit: tracked=$tracked"
        }
    }
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
        -ChangedPath 'Modules/Users/Contracts/Common/ICurrentUserAccessService.cs' `
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
        -ChangedPath 'Modules/Users/Contracts/Common/ICurrentUserAccessService.cs' | Out-Null
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
    foreach ($path in $receiptBackups.Keys) {
        if ($null -ne $receiptBackups[$path]) { [IO.File]::WriteAllBytes($path, $receiptBackups[$path]) }
        else { Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue }
    }
    $resolvedFixture = [IO.Path]::GetFullPath($fingerprintFixture)
    $fixtureParent = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot $repositoryRoot)).TrimEnd('\', '/')
    if ([IO.Path]::GetDirectoryName($resolvedFixture) -cne $fixtureParent -or
        [IO.Path]::GetFileName($resolvedFixture) -notmatch '^receipt-unicode-[a-f0-9]{32}$') { throw 'Unsafe receipt fixture cleanup path.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
    $resolvedWorkspace = [IO.Path]::GetFullPath($absoluteWorkspace)
    $tasksPrefix = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki/tasks')).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedWorkspace.StartsWith($tasksPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe receipt evidence cleanup path.' }
    Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki verification receipt tests passed.'
