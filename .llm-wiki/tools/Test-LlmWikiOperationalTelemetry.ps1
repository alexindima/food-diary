[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiTaskAuditHelpers.ps1')
$script:auditTime = [DateTime]::UtcNow
if ((Get-AgeDays $script:auditTime) -ne 0 -or $null -ne (Read-Json (Join-Path $repositoryRoot '.artifacts/llm-wiki/missing-audit-helper.json'))) {
    throw 'LlmWikiTaskAuditHelpers did not preserve safe age and JSON behavior.'
}
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$workspaceName = "telemetry-regression-$([guid]::NewGuid().ToString('N'))"
$workspacePath = New-LlmWikiSmokeFixtureRepositoryPath -RepositoryRoot $repositoryRoot -Name $workspaceName
$absoluteWorkspace = Join-Path $repositoryRoot $workspacePath
$registryPath = Join-Path (Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot) "$workspaceName.json"
$previousRegistryPath = $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH
$statusBefore = @(git -C $repositoryRoot status --short)

try {
    $null = New-Item -ItemType Directory -Path $absoluteWorkspace -Force
    [IO.File]::WriteAllText(
        (Join-Path $absoluteWorkspace 'change-packet.json'),
        "{`"fingerprint`":`"$('a' * 64)`"}`n",
        [Text.UTF8Encoding]::new($false))
    $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH = $registryPath

    $empty = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -Format Json | ConvertFrom-Json
    if (-not $empty.valid -or $empty.totalCount -ne 0 -or $empty.health -ne 'insufficient-data' -or -not (Test-Path -LiteralPath $registryPath -PathType Leaf)) {
        throw 'Operational telemetry did not initialize an empty local registry.'
    }
    foreach ($sample in @(
        [pscustomobject]@{ status = 'failed'; duration = 10 }
        [pscustomobject]@{ status = 'action-required'; duration = 15 }
        [pscustomobject]@{ status = 'passed'; duration = 20 }
        [pscustomobject]@{ status = 'failed'; duration = 30 }
    )) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') record `
            -WorkspacePath $workspacePath `
            -CheckId 'telemetry-regression' `
            -Status $sample.status `
            -DurationSeconds $sample.duration `
            -Command 'dotnet test tests/Example.Tests/Example.Tests.csproj' `
            -Format Json | Out-Null
    }
    $metrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -CheckId 'telemetry-regression' -Format Json | ConvertFrom-Json
    if (-not $metrics.valid -or $metrics.totalCount -ne 4 -or $metrics.health -ne 'attention' -or
        $metrics.passedCount -ne 1 -or $metrics.failedCount -ne 2 -or $metrics.actionRequiredCount -ne 1 -or
        $metrics.successRatePercent -ne 33.33 -or $metrics.metrics[0].failurePercent -ne 66.67 -or
        $metrics.metrics[0].actionRequiredCount -ne 1 -or $metrics.metrics[0].medianDurationSeconds -ne 17.5 -or
        -not $metrics.metrics[0].flaky -or $metrics.metrics[0].fingerprintCohortCount -ne 1) {
        throw 'Operational telemetry did not preserve valid local metrics.'
    }

    $snapshotCheck = 'repository-snapshot-regression'
    foreach ($sample in @(
        [pscustomobject]@{ status = 'failed'; fingerprint = 'b' * 64 }
        [pscustomobject]@{ status = 'passed'; fingerprint = 'b' * 64 }
        [pscustomobject]@{ status = 'failed'; fingerprint = 'c' * 64 }
    )) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') record `
            -WorkspacePath '@wiki' `
            -CheckId $snapshotCheck `
            -Status $sample.status `
            -DurationSeconds 1 `
            -Command 'repository verification stage' `
            -InputFingerprint $sample.fingerprint `
            -Format Json | Out-Null
    }
    $snapshotMetrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -CheckId $snapshotCheck -Format Json | ConvertFrom-Json
    if ($snapshotMetrics.metrics[0].flaky -or $snapshotMetrics.metrics[0].fingerprintCohortCount -ne 2 -or
        $snapshotMetrics.metrics[0].comparableSampleCount -ne 3) {
        throw 'Operational telemetry mixed outcomes from different repository input snapshots into one flaky cohort.'
    }

    $legacyCheck = 'legacy-unfingerprinted-regression'
    foreach ($status in @('failed', 'passed', 'failed')) {
        & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') record `
            -WorkspacePath '@wiki' `
            -CheckId $legacyCheck `
            -Status $status `
            -DurationSeconds 1 `
            -Command 'legacy repository verification stage' `
            -Format Json | Out-Null
    }
    $legacyMetrics = & (Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationTelemetry.ps1') metrics -CheckId $legacyCheck -Format Json | ConvertFrom-Json
    if ($legacyMetrics.metrics[0].flaky -or $legacyMetrics.metrics[0].fingerprintCohortCount -ne 0 -or
        $legacyMetrics.metrics[0].legacyUnfingerprintedSampleCount -ne 3) {
        throw 'Operational telemetry treated legacy outcomes from unknown input snapshots as comparable flakiness evidence.'
    }
    $statusAfter = @(git -C $repositoryRoot status --short)
    if ((@($statusBefore) -join "`n") -cne (@($statusAfter) -join "`n")) {
        throw 'Recording verification telemetry changed repository Git status.'
    }
    Write-Host 'LLM Wiki operational telemetry regression passed: local metrics are snapshot-aware, integrity-protected, and Git-clean.'
} finally {
    if ([string]::IsNullOrWhiteSpace($previousRegistryPath)) {
        Remove-Item Env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH -ErrorAction SilentlyContinue
    } else {
        $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH = $previousRegistryPath
    }
    if (Test-Path -LiteralPath $absoluteWorkspace) { Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force }
    if (Test-Path -LiteralPath $registryPath) { Remove-Item -LiteralPath $registryPath -Force }
}
