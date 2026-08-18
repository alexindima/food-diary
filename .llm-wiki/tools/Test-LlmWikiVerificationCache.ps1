[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $toolsRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'verification-cache'

function Assert-Cache([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }

$wikiFacadeText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') -Raw
$verificationCacheText = Get-Content -LiteralPath (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Raw
Assert-Cache ($wikiFacadeText.Contains("Manage-LlmWikiVerificationCache.ps1') @verificationCacheArguments") -and
    $wikiFacadeText.Contains("`$verificationCacheArguments.Action = 'Record'") -and
    $wikiFacadeText.Contains('Strict publication verification remains enforced by pre-push and CI.')) 'Verify-fast does not reuse and record a content-addressed local completion receipt.'
Assert-Cache ($verificationCacheText -match "rev-parse', 'HEAD'" -and $verificationCacheText -match "ls-files', '--others', '--exclude-standard'" -and
    $verificationCacheText -match "diff', '--raw'" -and $verificationCacheText -match 'PSVersionTable' -and
    $verificationCacheText -match 'ChangedPath') 'Verification cache fingerprint omits repository, metadata, environment, or scope inputs.'

try {
    & git -C $fixtureRoot init --quiet
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'source.txt'), 'baseline')
    & git -C $fixtureRoot add source.txt
    & git -C $fixtureRoot -c user.name='LLM Wiki' -c user.email='llm-wiki@example.invalid' commit --quiet -m baseline
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize verification-cache fixture.' }

    $arguments = @{ RepositoryRoot = $fixtureRoot; BaseRef = 'HEAD'; ChangedPath = @('source.txt'); Mode = 'default' }
    $miss = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @arguments
    Assert-Cache (-not $miss.hit) 'Verification cache reported a hit before a receipt existed.'
    $record = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Record @arguments
    $hit = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @arguments
    Assert-Cache ($record.hit -and $hit.hit) 'Verification cache did not reuse an identical successful state.'
    $headSha = (& git -C $fixtureRoot rev-parse HEAD).Trim()
    $shaHit = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check `
        -RepositoryRoot $fixtureRoot -BaseRef $headSha -ChangedPath 'source.txt' -Mode default
    Assert-Cache ($shaHit.hit -and $shaHit.fingerprint -ceq $hit.fingerprint) 'Equivalent HEAD and resolved SHA refs produced different cache identities.'

    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'source.txt'), 'changed')
    $contentMiss = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @arguments
    Assert-Cache (-not $contentMiss.hit) 'Verification cache ignored changed tracked content.'
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'source.txt'), 'baseline')
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'untracked.txt'), 'new')
    $untrackedMiss = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @arguments
    Assert-Cache (-not $untrackedMiss.hit) 'Verification cache ignored an untracked file.'
    Remove-Item -LiteralPath (Join-Path $fixtureRoot 'untracked.txt') -Force
    $scopeMiss = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check -RepositoryRoot $fixtureRoot -BaseRef HEAD -ChangedPath 'other.txt' -Mode default
    Assert-Cache (-not $scopeMiss.hit) 'Verification cache ignored a changed verification scope.'
    $modeMiss = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check -RepositoryRoot $fixtureRoot -BaseRef HEAD -ChangedPath 'source.txt' -Mode visual-ui
    Assert-Cache (-not $modeMiss.hit) 'Verification cache ignored a changed completion mode.'
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'view.scss'), '.card { color: red; }')
    $visualArguments = @{ RepositoryRoot = $fixtureRoot; BaseRef = 'HEAD'; ChangedPath = @('source.txt', 'view.scss'); Mode = 'visual-ui' }
    $null = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Record @visualArguments
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'view.scss'), '.card { color: blue; }')
    $styleDelta = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @visualArguments
    Assert-Cache (-not $styleDelta.hit -and $styleDelta.incrementalStyleOnly -and @($styleDelta.incrementalPaths) -contains 'view.scss') 'Visual verification did not isolate a stylesheet-only delta.'
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'source.txt'), 'changed again')
    $sourceDelta = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @visualArguments
    Assert-Cache (-not $sourceDelta.incrementalStyleOnly) 'Visual verification reused index evidence after a non-style change.'
    & git -C $fixtureRoot -c user.name='LLM Wiki' -c user.email='llm-wiki@example.invalid' commit --quiet --allow-empty -m next-head
    if ($LASTEXITCODE -ne 0) { throw 'Unable to advance verification-cache fixture HEAD.' }
    $headMiss = & (Join-Path $toolsRoot 'Manage-LlmWikiVerificationCache.ps1') -Action Check @arguments
    Assert-Cache (-not $headMiss.hit) 'Verification cache survived a changed Git HEAD.'
} finally {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}

Write-Host 'LLM Wiki verification-cache smoke passed: aliases resolve to one SHA identity; identical state reuses success; visual style deltas reuse scoped evidence; other changes invalidate it.'
