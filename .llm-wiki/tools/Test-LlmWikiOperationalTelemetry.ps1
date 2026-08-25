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
        $metrics.failedCount -ne 2 -or $metrics.actionRequiredCount -ne 1 -or
        $metrics.metrics[0].actionRequiredCount -ne 1 -or $metrics.metrics[0].medianDurationSeconds -ne 17.5) {
        throw 'Operational telemetry did not preserve valid local metrics.'
    }
    $statusAfter = @(git -C $repositoryRoot status --short)
    if ((@($statusBefore) -join "`n") -cne (@($statusAfter) -join "`n")) {
        throw 'Recording verification telemetry changed repository Git status.'
    }
    Write-Host 'LLM Wiki operational telemetry regression passed: local metrics are integrity-protected and Git-clean.'
} finally {
    if ([string]::IsNullOrWhiteSpace($previousRegistryPath)) {
        Remove-Item Env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH -ErrorAction SilentlyContinue
    } else {
        $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH = $previousRegistryPath
    }
    if (Test-Path -LiteralPath $absoluteWorkspace) { Remove-Item -LiteralPath $absoluteWorkspace -Recurse -Force }
    if (Test-Path -LiteralPath $registryPath) { Remove-Item -LiteralPath $registryPath -Force }
}
