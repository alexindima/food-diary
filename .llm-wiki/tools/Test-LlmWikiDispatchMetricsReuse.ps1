[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki'))
$fixtureRoot = Join-Path $artifactRoot ('dispatch-metrics-reuse-' + [Guid]::NewGuid().ToString('N'))
$fixtureTools = Join-Path $fixtureRoot '.llm-wiki/tools'
function Assert-Condition([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
try {
    New-Item -ItemType Directory -Path $fixtureTools -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Get-LlmWikiDispatchMetrics.ps1') -Destination $fixtureTools
    & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | Set-Content (Join-Path $fixtureTools 'policy.json')
    'Get-Content (Join-Path $PSScriptRoot ''policy.json'') -Raw' | Set-Content (Join-Path $fixtureTools 'Get-LlmWikiWorkspacePolicy.ps1')
    @'
param($Action, $AsOfUtc, $Format)
Add-Content (Join-Path $PSScriptRoot 'reads.txt') 'read'
Get-Content (Join-Path $PSScriptRoot 'registry.json') -Raw
'@ | Set-Content (Join-Path $fixtureTools 'Manage-LlmWikiTaskDispatch.ps1')
    $registry = [ordered]@{
        totalCount = 2; invalidCount = 1; orphanedCount = 0; driftedCount = 0
        dispatches = @(
            @{ dispatchId = 'old-running'; state = 'running'; valid = $true; workspace = 'fixture' }
            @{ dispatchId = 'invalid'; state = 'invalid'; valid = $false; workspace = 'fixture' }
        )
    }
    $registryPath = Join-Path $fixtureTools 'registry.json'
    $registry | ConvertTo-Json -Depth 10 | Set-Content $registryPath
    $dispatchRoot = Join-Path $fixtureRoot '.artifacts/llm-wiki/scheduler/dispatches'
    New-Item -ItemType Directory -Path $dispatchRoot -Force | Out-Null
    '{"startedAtUtc":"2020-01-01T00:00:00Z"}' | Set-Content (Join-Path $dispatchRoot 'old-running.json')
    $tool = Join-Path $fixtureTools 'Get-LlmWikiDispatchMetrics.ps1'
    $arguments = @{ AsOfUtc = [DateTime]'2026-09-10T00:00:00Z'; Format = 'Json' }
    $plain = & $tool @arguments | ConvertFrom-Json
    $withRegistry = & $tool @arguments -IncludeDispatchRegistry | ConvertFrom-Json
    Assert-Condition ($null -eq $plain.PSObject.Properties['dispatchRegistry']) 'Default metrics schema changed.'
    Assert-Condition ($withRegistry.dispatchRegistry.dispatches.Count -eq 2) 'Registry lost an old or invalid dispatch outside metrics records.'
    Assert-Condition ($withRegistry.dispatchCount -eq 0 -and $withRegistry.attentionCount -eq 1) 'Metrics filtering or invalid-receipt attention changed.'
    $withRegistry.PSObject.Properties.Remove('dispatchRegistry')
    Assert-Condition (($plain | ConvertTo-Json -Depth 20 -Compress) -ceq ($withRegistry | ConvertTo-Json -Depth 20 -Compress)) 'Including the registry changed metric values.'
    Assert-Condition (@(Get-Content (Join-Path $fixtureTools 'reads.txt')).Count -eq 2) 'Each metrics invocation must validate the registry exactly once.'
    $registry.invalidCount = 2
    $registry | ConvertTo-Json -Depth 10 | Set-Content $registryPath
    $fresh = & $tool @arguments -IncludeDispatchRegistry | ConvertFrom-Json
    Assert-Condition ($fresh.dispatchRegistry.invalidCount -eq 2 -and $fresh.attentionCount -eq 2) 'A later invocation reused stale registry state.'
    $scheduler = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Get-LlmWikiTaskSchedule.ps1'))
    Assert-Condition ($scheduler.Contains('-IncludeDispatchRegistry') -and -not $scheduler.Contains("'Manage-LlmWikiTaskDispatch.ps1'")) 'Scheduler must consume the full validated registry without a second scan.'
    Write-Host 'Dispatch metrics reuse passed: unchanged metrics, invalid and out-of-window dispatches retained, one fresh validation per invocation.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolvedFixture.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture cleanup escaped its artifact root.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
