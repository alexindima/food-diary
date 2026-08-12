[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [switch]$FailOnUnreviewed,
    [switch]$VerboseGenerated,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$reviewLedgerPath = Join-Path $wikiRoot 'reviews/source-impact-reviews.json'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')

function ConvertTo-RepositoryPath {
    param([string]$Path)

    return ConvertTo-LlmWikiRepositoryPath $Path
}

function Get-PageMetadata {
    param([System.IO.FileInfo]$Page)

    $lines = @(Get-Content -LiteralPath $Page.FullName)
    $closingDelimiter = -1
    for ($index = 1; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -eq '---') {
            $closingDelimiter = $index
            break
        }
    }

    if ($lines.Count -lt 3 -or $lines[0] -ne '---' -or $closingDelimiter -lt 0) {
        throw "$($Page.FullName) has invalid front matter. Run Test-LlmWiki.ps1 first."
    }

    $frontMatter = $lines[1..($closingDelimiter - 1)]
    $idLine = $frontMatter | Where-Object { $_ -match '^id:\s*(\S+)\s*$' } | Select-Object -First 1
    $statusLine = $frontMatter | Where-Object { $_ -match '^status:\s*(\S+)\s*$' } | Select-Object -First 1
    $generatedByLine = $frontMatter | Where-Object { $_ -match '^generated_by:\s*(\S+)\s*$' } | Select-Object -First 1
    $sourcesLineIndex = [Array]::IndexOf($frontMatter, 'sources:')
    if (-not $idLine -or -not $statusLine -or $sourcesLineIndex -lt 0) {
        throw "$($Page.FullName) has incomplete front matter. Run Test-LlmWiki.ps1 first."
    }

    $null = $idLine -match '^id:\s*(\S+)\s*$'
    $pageId = $Matches[1]
    $null = $statusLine -match '^status:\s*(\S+)\s*$'
    $status = $Matches[1]
    $generatedBy = $null
    if ($generatedByLine) {
        $null = $generatedByLine -match '^generated_by:\s*(\S+)\s*$'
        $generatedBy = $Matches[1]
    }
    $sources = [System.Collections.Generic.List[string]]::new()

    for ($index = $sourcesLineIndex + 1; $index -lt $frontMatter.Count; $index++) {
        if ($frontMatter[$index] -match '^\s+-\s+(.+?)\s*$') {
            $sources.Add((ConvertTo-RepositoryPath $Matches[1]))
            continue
        }
        if ($frontMatter[$index] -match '^\S') {
            break
        }
    }

    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pageUri = [System.Uri]::new($Page.FullName)
    $pagePath = [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pageUri).ToString())

    return [pscustomobject]@{
        Id = $pageId
        Path = $pagePath
        Status = $status
        GeneratedBy = $generatedBy
        Sources = @($sources)
    }
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
    $gitArguments = @('diff', '--name-only', '--diff-filter=ACMRD', $BaseRef)
    if (-not [string]::IsNullOrWhiteSpace($HeadRef)) {
        $gitArguments += $HeadRef
    }
    $gitArguments += '--'

    $ChangedPath = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments $gitArguments -FailureMessage "git diff failed for base '$BaseRef' and head '$HeadRef'.")
    if ([string]::IsNullOrWhiteSpace($HeadRef)) {
        $ChangedPath += @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'git ls-files failed while collecting untracked paths.')
    }
}

$changedPaths = @(
    $ChangedPath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { ConvertTo-RepositoryPath $_ } |
        Sort-Object -Unique
)
$changedPathSet = @{}
foreach ($path in $changedPaths) {
    $changedPathSet[$path] = $true
}

$pages = @(
    Get-ChildItem -LiteralPath $wikiRoot -Recurse -File -Filter '*.md' |
        Where-Object { $_.FullName -ne (Join-Path $wikiRoot 'README.md') } |
        ForEach-Object { Get-PageMetadata $_ }
)
$sourceReviews = if (Test-Path -LiteralPath $reviewLedgerPath) {
    @((Get-Content -LiteralPath $reviewLedgerPath -Raw | ConvertFrom-Json).reviews)
} else {
    @()
}

$impacts = [System.Collections.Generic.List[object]]::new()
foreach ($page in $pages) {
    $changedSources = @($page.Sources | Where-Object { $changedPathSet.ContainsKey($_) })
    if ($changedSources.Count -eq 0) {
        continue
    }

    $pageChanged = $changedPathSet.ContainsKey($page.Path)
    $reviewReceipt = @($sourceReviews | Where-Object pageId -eq $page.Id | Where-Object {
        $candidateReview = $_
        $candidateReview.pagePath -eq $page.Path -and
        $candidateReview.pageSha256 -eq (Get-ContentHash $page.Path) -and
        @($changedSources | Where-Object {
            $sourcePath = $_
            @($candidateReview.sources | Where-Object {
                $_.path -eq $sourcePath -and $_.sha256 -eq (Get-ContentHash $sourcePath)
            }).Count -eq 0
        }).Count -eq 0
    } | Select-Object -First 1)
    $reviewed = $pageChanged -or $page.Status -eq 'stale' -or -not [string]::IsNullOrWhiteSpace($page.GeneratedBy) -or $reviewReceipt.Count -gt 0
    $impacts.Add([pscustomobject]@{
        Id = $page.Id
        Path = $page.Path
        Status = $page.Status
        GeneratedBy = $page.GeneratedBy
        ChangedSources = $changedSources
        PageChanged = $pageChanged
        ReviewReceipt = if ($reviewReceipt.Count -gt 0) { $reviewReceipt[0] } else { $null }
        Reviewed = $reviewed
    })
}

if ($Format -eq 'Json') {
    [pscustomobject]@{
        impactCount = $impacts.Count
        unreviewedCount = @($impacts | Where-Object { -not $_.Reviewed }).Count
        impacts = @($impacts)
    } | ConvertTo-Json -Depth 7
    if ($FailOnUnreviewed -and @($impacts | Where-Object { -not $_.Reviewed }).Count -gt 0) {
        exit 1
    }
    exit 0
}

if ($impacts.Count -eq 0) {
    Write-Host "LLM Wiki freshness check passed: no declared sources changed."
    return
}

Write-Host "LLM Wiki source impact:"
foreach ($impact in @($impacts | Where-Object { [string]::IsNullOrWhiteSpace($_.GeneratedBy) })) {
    $reviewState = if ($impact.Reviewed) {
        if ($null -ne $impact.ReviewReceipt) { 'reviewed by receipt' } else { 'reviewed' }
    } else {
        'needs review'
    }
    Write-Host " - $($impact.Path) [id: $($impact.Id); $($impact.Status), $reviewState]"
    foreach ($source in $impact.ChangedSources) {
        Write-Host "   <- $source"
    }
}
$generatedImpacts = @($impacts | Where-Object { -not [string]::IsNullOrWhiteSpace($_.GeneratedBy) })
if ($VerboseGenerated) {
    foreach ($impact in $generatedImpacts) {
        Write-Host " - $($impact.Path) [id: $($impact.Id); $($impact.Status), generated by $($impact.GeneratedBy)]"
        foreach ($source in $impact.ChangedSources) {
            Write-Host "   <- $source"
        }
    }
} else {
    foreach ($generatorGroup in @($generatedImpacts | Group-Object GeneratedBy | Sort-Object Name)) {
        Write-Host " - $($generatorGroup.Count) generated page(s) validated by $($generatorGroup.Name)"
    }
}

$unreviewed = @($impacts | Where-Object { -not $_.Reviewed })
if ($FailOnUnreviewed -and $unreviewed.Count -gt 0) {
    Write-Host ''
    Write-Host 'Update each affected page, set it stale, or record a source review with wiki.ps1 review -Id <page-id> -Reason <reason>.'
    exit 1
}

if ($unreviewed.Count -eq 0) {
    Write-Host "LLM Wiki freshness check passed: $($impacts.Count) affected page(s) were reviewed."
} else {
    Write-Host "LLM Wiki freshness report: $($unreviewed.Count) page(s) need review."
}
