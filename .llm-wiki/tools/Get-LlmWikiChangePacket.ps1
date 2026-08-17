[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string]$Objective,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$wikiRoot = Split-Path -Parent $toolsRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $toolsRoot 'LlmWikiGitPaths.ps1')
$requestedBaseRef = $BaseRef
$BaseRef = Resolve-LlmWikiCommitRef -RepositoryRoot $repositoryRoot -Ref $BaseRef
$common = @{ BaseRef = $BaseRef; Format = 'Json' }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
if ($PSBoundParameters.ContainsKey('ChangedPath')) { $common.ChangedPath = $ChangedPath }

$diffArguments = @{} + $common
$diffArguments.Limit = [Math]::Min($Limit, 20)
$diff = & (Join-Path $toolsRoot 'Get-LlmWikiDiffContext.ps1') @diffArguments | ConvertFrom-Json
$policy = & (Join-Path $toolsRoot 'Test-LlmWikiChangePolicy.ps1') @common | ConvertFrom-Json
$ownership = & (Join-Path $toolsRoot 'Get-LlmWikiOwnershipImpact.ps1') @common -DiffInput $diff | ConvertFrom-Json
$testPlan = & (Join-Path $toolsRoot 'Get-LlmWikiTestPlan.ps1') @diffArguments -DiffInput $diff -PolicyInput $policy | ConvertFrom-Json
$rollout = & (Join-Path $toolsRoot 'Get-LlmWikiRolloutPlan.ps1') @common -DiffInput $diff -PolicyInput $policy | ConvertFrom-Json
$decision = & (Join-Path $toolsRoot 'Get-LlmWikiDecisionContext.ps1') @common -DiffInput $diff -PolicyInput $policy | ConvertFrom-Json
$brief = & (Join-Path $toolsRoot 'Get-LlmWikiTaskBrief.ps1') @diffArguments `
    -Intent $Objective `
    -DiffInput $diff `
    -PolicyInput $policy `
    -OwnershipInput $ownership `
    -TestPlanInput $testPlan `
    -RolloutInput $rollout `
    -DecisionInput $decision | ConvertFrom-Json
$implementationPlan = & (Join-Path $toolsRoot 'Get-LlmWikiImplementationPlan.ps1') @diffArguments `
    -Objective $Objective `
    -BriefInput $brief | ConvertFrom-Json

$head = git rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD for change packet.' }
$fingerprintInput = [ordered]@{
    gitHead = [string]$head
    baseRef = $BaseRef
    headRef = if ($PSBoundParameters.ContainsKey('HeadRef')) { $HeadRef } else { $null }
    changedPaths = @($diff.changedPaths | Sort-Object -Unique)
    objective = $Objective
}
$fingerprintJson = $fingerprintInput | ConvertTo-Json -Depth 5 -Compress
$sha256 = [System.Security.Cryptography.SHA256]::Create()
try {
    $fingerprint = ([BitConverter]::ToString($sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($fingerprintJson))) -replace '-', '').ToLowerInvariant()
} finally {
    $sha256.Dispose()
}
$packet = [pscustomobject][ordered]@{
    schemaVersion = 1
    fingerprint = $fingerprint
    inputs = $fingerprintInput
    requestedBaseRef = $requestedBaseRef
    diff = $diff
    policy = $policy
    ownership = $ownership
    testPlan = $testPlan
    rollout = $rollout
    decision = $decision
    brief = $brief
    implementationPlan = $implementationPlan
}
$json = ($packet | ConvertTo-Json -Depth 15) + [Environment]::NewLine
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $absoluteOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) { $OutputPath } else { Join-Path $repositoryRoot $OutputPath }
    $directory = Split-Path -Parent $absoluteOutputPath
    if (-not (Test-Path -LiteralPath $directory)) { New-Item -ItemType Directory -Path $directory | Out-Null }
    $utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($absoluteOutputPath, $json, $utf8WithoutBom)
}
if ($Format -eq 'Json') { Write-Output $json.TrimEnd(); exit 0 }
Write-Host "Compiled change packet: $(@($diff.changedPaths).Count) path(s), $(@($implementationPlan.phases).Count) phase(s), fingerprint $fingerprint"
Write-Host "Risk: $($brief.risk.level) ($($brief.risk.score)); scopes: $(@($diff.scopes) -join ', ')"
Write-Host "Checks: $(@($policy.requiredChecks).Count); reviews: $(@($policy.reviewObligations).Count); scenarios: $(@($testPlan.scenarios).Count)"
if ($OutputPath) { Write-Host "Saved: $OutputPath" }
