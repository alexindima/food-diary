[CmdletBinding()]
param([string]$BaseRef = 'HEAD')

$ErrorActionPreference = 'Stop'
$toolsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../.llm-wiki/tools'))

# Preserve every non-smoke gate from wiki.ps1 verify-full. The complete focused
# catalog runs in isolated CI shards; only the aggregate CI job certifies them.
# This checkout must not publish a full-verification receipt for a partial run.
$checks = @(
    @{ Name = 'Get-LlmWikiWorkspacePolicy.ps1'; Arguments = @{ Action = 'validate'; FailOnInvalid = $true } }
    @{ Name = 'Test-LlmWikiPortable.ps1'; Arguments = @{} }
    @{ Name = 'Test-LlmWikiSessionResolution.ps1'; Arguments = @{} }
    @{ Name = 'Test-LlmWikiProcess.ps1'; Arguments = @{} }
    @{ Name = 'Invoke-LlmWikiIndexPipeline.ps1'; Arguments = @{ Check = $true; MaxConcurrency = 4 } }
    @{ Name = 'Manage-LlmWikiFailures.ps1'; Arguments = @{ Action = 'validate' } }
    @{ Name = 'Test-LlmWikiChangePolicy.ps1'; Arguments = @{ BaseRef = $BaseRef; FailOnViolation = $true } }
    @{ Name = 'Get-LlmWikiImpact.ps1'; Arguments = @{ BaseRef = $BaseRef; FailOnUnreviewed = $true } }
)
foreach ($check in $checks) {
    Write-Host "Wiki CI check: $($check.Name)"
    $global:LASTEXITCODE = 0
    $arguments = $check.Arguments
    & (Join-Path $toolsRoot $check.Name) @arguments
    if (-not $? -or $LASTEXITCODE -ne 0) { throw "Wiki CI check failed: $($check.Name)" }
}
