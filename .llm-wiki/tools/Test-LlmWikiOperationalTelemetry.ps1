[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$workspaceName = "telemetry-regression-$([guid]::NewGuid().ToString('N'))"
$workspacePath = ".artifacts/llm-wiki/tasks/$workspaceName"
$absoluteWorkspace = Join-Path $repositoryRoot $workspacePath
$registryPath = Join-Path $repositoryRoot ".artifacts/llm-wiki/$workspaceName.json"
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
    if (-not $empty.valid -or $empty.totalCount -ne 0 -or -not (Test-Path -LiteralPath $registryPath -PathType Leaf)) {
        throw 'Operational telemetry did not initialize an empty local registry.'
    }
    foreach ($sample in @(
        [pscustomobject]@{ status = 'failed'; duration = 10 }
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
    if (-not $metrics.valid -or $metrics.totalCount -ne 3 -or $metrics.metrics[0].medianDurationSeconds -ne 20) {
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
