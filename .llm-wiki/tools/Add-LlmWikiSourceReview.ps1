[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Id,
    [Parameter(Mandatory)]
    [string]$Reason,
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$ledgerPath = Join-Path $wikiRoot 'reviews/source-impact-reviews.json'

function ConvertTo-RepositoryPath([string]$Path) {
    return ConvertTo-LlmWikiRepositoryPath $Path
}

function Get-ContentHash([string]$RepositoryPath) {
    $absolutePath = Join-Path $repositoryRoot $RepositoryPath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { return '<missing>' }
    $normalizedContent = [System.IO.File]::ReadAllText($absolutePath).Replace("`r`n", "`n").Replace("`r", "`n")
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $hash = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($normalizedContent))
        return ([System.BitConverter]::ToString($hash) -replace '-', '').ToLowerInvariant()
    } finally {
        $sha256.Dispose()
    }
}

if (-not $PSBoundParameters.ContainsKey('ChangedPath')) {
    $workspaceHead = Test-LlmWikiWorkspaceHeadRef $HeadRef
    $gitArguments = @('diff', '--name-only', '--diff-filter=ACMRD', $BaseRef)
    if (-not $workspaceHead) { $gitArguments += $HeadRef }
    $gitArguments += '--'
    $ChangedPath = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments $gitArguments -FailureMessage "git diff failed for base '$BaseRef'.")
    if ($workspaceHead) {
        $ChangedPath += @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'git ls-files failed while collecting untracked paths.')
    }
}
$changedPathSet = @{}
foreach ($path in @($ChangedPath | Where-Object { $_ } | ForEach-Object { ConvertTo-RepositoryPath $_ })) {
    $changedPathSet[$path] = $true
}

$page = $null
$pageSources = @()
foreach ($candidate in Get-ChildItem -LiteralPath $wikiRoot -Recurse -File -Filter '*.md') {
    $lines = @(Get-Content -LiteralPath $candidate.FullName)
    if (@($lines | Where-Object { $_ -eq "id: $Id" }).Count -eq 0) { continue }
    $page = $candidate
    $sourcesIndex = [Array]::IndexOf($lines, 'sources:')
    if ($sourcesIndex -lt 0) { throw "Wiki page '$Id' has no sources list." }
    $sourceList = [System.Collections.Generic.List[string]]::new()
    for ($index = $sourcesIndex + 1; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match '^\s+-\s+(.+?)\s*$') {
            $sourceList.Add((ConvertTo-RepositoryPath $Matches[1]))
            continue
        }
        if ($lines[$index] -match '^\S') { break }
    }
    $pageSources = @($sourceList)
    break
}
if ($null -eq $page) { throw "Wiki page id not found: $Id" }

$changedSources = @($pageSources | Where-Object { $changedPathSet.ContainsKey($_) })
if ($changedSources.Count -eq 0) {
    throw "Wiki page '$Id' has no declared source changed in the selected diff."
}
$pagePath = $page.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
$review = [pscustomobject][ordered]@{
    pageId = $Id
    pagePath = $pagePath
    pageSha256 = Get-ContentHash $pagePath
    sources = @($changedSources | Sort-Object | ForEach-Object {
        [pscustomobject][ordered]@{ path = $_; sha256 = Get-ContentHash $_ }
    })
    reason = $Reason
    baseRef = $BaseRef
    gitHead = (Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('rev-parse', 'HEAD') -FailureMessage 'Unable to resolve HEAD for the source review.').Lines[0].Trim()
    reviewedAtUtc = [DateTime]::UtcNow.ToString('o')
}

$ledger = Get-Content -LiteralPath $ledgerPath -Raw | ConvertFrom-Json
$existingReview = @($ledger.reviews | Where-Object pageId -eq $Id | Select-Object -First 1)
if ($existingReview.Count -eq 1) {
    $signatureProperties = @('pageId', 'pagePath', 'pageSha256', 'sources', 'reason', 'baseRef', 'gitHead')
    $existingSignature = $existingReview[0] | Select-Object $signatureProperties | ConvertTo-Json -Depth 8 -Compress
    $newSignature = $review | Select-Object $signatureProperties | ConvertTo-Json -Depth 8 -Compress
    if ($existingSignature -eq $newSignature) {
        Write-Host "Source-impact review for '$Id' is already current; ledger unchanged."
        exit 0
    }
}
$reviews = @($ledger.reviews | Where-Object pageId -ne $Id) + $review
[pscustomobject][ordered]@{
    schemaVersion = 1
    reviews = @($reviews | Sort-Object pageId)
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ledgerPath -Encoding utf8
Write-Host "Recorded source-impact review for '$Id': $($changedSources.Count) changed source(s)."
