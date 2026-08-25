[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$WorkspacePath,
    [Parameter(Mandatory = $true)]
    [object]$Completion,
    [string]$RegistryRoot
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if ([string]::IsNullOrWhiteSpace($RegistryRoot) -and -not [string]::IsNullOrWhiteSpace([string]$env:LLM_WIKI_WORKSPACE_OUTCOME_ROOT)) {
    $RegistryRoot = [IO.Path]::GetFullPath([string]$env:LLM_WIKI_WORKSPACE_OUTCOME_ROOT)
} elseif ([string]::IsNullOrWhiteSpace($RegistryRoot)) {
    $gitDirectory = (Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -FailureMessage 'Unable to resolve the Git directory for workspace outcomes.').Lines[0]
    $RegistryRoot = Join-Path $gitDirectory 'llm-wiki/workspace-outcomes'
}
$null = New-Item -ItemType Directory -Path $RegistryRoot -Force
$payload = [pscustomobject][ordered]@{
    schemaVersion = 1
    workspace = $WorkspacePath.Replace('\', '/')
    objective = [string]$Completion.objective
    state = 'sealed'
    finishedAtUtc = [string]$Completion.finishedAtUtc
    gitHead = [string]$Completion.git.head
    readinessScore = [int]$Completion.readiness.score
    completionFingerprint = [string]$Completion.completionFingerprint
}
if ($payload.workspace -match '/\.smoke-') { return $payload }
$path = Join-Path $RegistryRoot "$($payload.completionFingerprint).json"
[IO.File]::WriteAllText($path, (($payload | ConvertTo-Json -Depth 6) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
$payload
