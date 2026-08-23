[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('develop', 'research', 'verify')][string]$Operation,
    [Parameter(Mandatory)][ValidateSet('passed', 'failed')][string]$Outcome,
    [Parameter(Mandatory)][double]$DurationSeconds,
    [int]$ScopePathCount = 0
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$gitDirectory = (Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -FailureMessage 'Unable to resolve the Git directory for workflow metrics.').Lines[0].Trim()
$root = Join-Path $gitDirectory 'llm-wiki/workflow-metrics'
$null = New-Item -ItemType Directory -Path $root -Force
$metric = [ordered]@{
    schemaVersion = 1
    operation = $Operation
    outcome = $Outcome
    durationSeconds = [Math]::Round($DurationSeconds, 2)
    scopePathCount = $ScopePathCount
    recordedAtUtc = [DateTime]::UtcNow.ToString('o')
}
$path = Join-Path $root "$([DateTime]::UtcNow.ToString('yyyyMMddHHmmssfffffff'))-$PID-$([guid]::NewGuid().ToString('N')).json"
[IO.File]::WriteAllText($path, (($metric | ConvertTo-Json) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
Get-ChildItem -LiteralPath $root -Filter '*.json' -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -Skip 500 | Remove-Item -Force
