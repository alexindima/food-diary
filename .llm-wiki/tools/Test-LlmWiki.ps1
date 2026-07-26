[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$errors = [System.Collections.Generic.List[string]]::new()
$ids = @{}

function Add-WikiError {
    param([string]$Message)

    $errors.Add($Message)
}

function Get-RelativeDisplayPath {
    param([string]$Path)

    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($Path)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

$pages = Get-ChildItem -LiteralPath $wikiRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.FullName -ne (Join-Path $wikiRoot 'README.md') }

if ($pages.Count -eq 0) {
    Add-WikiError 'No wiki pages were found.'
}

foreach ($page in $pages) {
    $displayPath = Get-RelativeDisplayPath $page.FullName
    $lines = @(Get-Content -LiteralPath $page.FullName)

    if ($lines.Count -lt 3 -or $lines[0] -ne '---') {
        Add-WikiError "${displayPath}: missing opening front matter delimiter."
        continue
    }

    $closingDelimiter = -1
    for ($index = 1; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -eq '---') {
            $closingDelimiter = $index
            break
        }
    }

    if ($closingDelimiter -lt 0) {
        Add-WikiError "${displayPath}: missing closing front matter delimiter."
        continue
    }

    $frontMatter = $lines[1..($closingDelimiter - 1)]
    $idLine = $frontMatter | Where-Object { $_ -match '^id:\s*(\S+)\s*$' } | Select-Object -First 1
    $kindLine = $frontMatter | Where-Object { $_ -match '^kind:\s*(\S+)\s*$' } | Select-Object -First 1
    $statusLine = $frontMatter | Where-Object { $_ -match '^status:\s*(\S+)\s*$' } | Select-Object -First 1
    $generatedByLine = $frontMatter | Where-Object { $_ -match '^generated_by:\s*(\S+)\s*$' } | Select-Object -First 1

    if (-not $idLine) {
        Add-WikiError "${displayPath}: missing id."
    } else {
        $null = $idLine -match '^id:\s*(\S+)\s*$'
        $pageId = $Matches[1]
        if ($ids.ContainsKey($pageId)) {
            Add-WikiError "${displayPath}: duplicate id '${pageId}' also used by $($ids[$pageId])."
        } else {
            $ids[$pageId] = $displayPath
        }
    }

    if (-not $kindLine) {
        Add-WikiError "${displayPath}: missing kind."
    } else {
        $null = $kindLine -match '^kind:\s*(\S+)\s*$'
        if ($Matches[1] -notin @('index', 'system', 'module', 'workflow')) {
            Add-WikiError "${displayPath}: unsupported kind '$($Matches[1])'."
        }
    }

    if (-not $statusLine) {
        Add-WikiError "${displayPath}: missing status."
    } else {
        $null = $statusLine -match '^status:\s*(\S+)\s*$'
        if ($Matches[1] -notin @('current', 'draft', 'stale')) {
            Add-WikiError "${displayPath}: unsupported status '$($Matches[1])'."
        }
    }

    if ($generatedByLine) {
        $null = $generatedByLine -match '^generated_by:\s*(\S+)\s*$'
        $generatorPath = Join-Path $repositoryRoot $Matches[1]
        if (-not (Test-Path -LiteralPath $generatorPath -PathType Leaf)) {
            Add-WikiError "${displayPath}: generated_by tool does not exist: $($Matches[1])"
        }
    }

    $sourcesLineIndex = [Array]::IndexOf($frontMatter, 'sources:')
    if ($sourcesLineIndex -lt 0) {
        Add-WikiError "${displayPath}: missing sources list."
    } else {
        $sources = [System.Collections.Generic.List[string]]::new()
        for ($index = $sourcesLineIndex + 1; $index -lt $frontMatter.Count; $index++) {
            if ($frontMatter[$index] -match '^\s+-\s+(.+?)\s*$') {
                $sources.Add($Matches[1])
                continue
            }
            if ($frontMatter[$index] -match '^\S') {
                break
            }
        }

        if ($sources.Count -eq 0) {
            Add-WikiError "${displayPath}: sources list is empty."
        }

        foreach ($source in $sources) {
            $sourcePath = Join-Path $repositoryRoot $source
            if (-not (Test-Path -LiteralPath $sourcePath)) {
                Add-WikiError "${displayPath}: source does not exist: ${source}"
            }
        }
    }

    $content = $lines -join "`n"
    $linkMatches = [regex]::Matches($content, '(?<!\!)\[[^\]]+\]\(([^)]+)\)')
    foreach ($linkMatch in $linkMatches) {
        $target = $linkMatch.Groups[1].Value.Trim()
        if ($target -match '^(https?://|mailto:|#)') {
            continue
        }

        $pathWithoutAnchor = ($target -split '[#?]', 2)[0]
        if ([string]::IsNullOrWhiteSpace($pathWithoutAnchor)) {
            continue
        }

        $decodedTarget = [System.Uri]::UnescapeDataString($pathWithoutAnchor)
        $resolvedTarget = Join-Path $page.DirectoryName $decodedTarget
        if (-not (Test-Path -LiteralPath $resolvedTarget)) {
            Add-WikiError "${displayPath}: local link target does not exist: ${target}"
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host "LLM Wiki verification failed with $($errors.Count) error(s):"
    foreach ($verificationError in $errors) {
        Write-Host " - $verificationError"
    }
    exit 1
}

Write-Host "LLM Wiki verification passed: $($pages.Count) pages, $($ids.Count) unique ids."
