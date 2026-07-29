[CmdletBinding()]
param(
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [string]$WikiRoot,
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($WikiRoot)) {
    $WikiRoot = Split-Path -Parent $PSScriptRoot
}
$WikiRoot = (Resolve-Path -LiteralPath $WikiRoot).Path

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = Join-Path $WikiRoot '..'
}
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path

$diagnostics = [System.Collections.Generic.List[object]]::new()
$ids = @{}
$allowedKeys = @('id', 'kind', 'status', 'generated_by', 'sources', 'title', 'summary', 'area', 'tags', 'owners')
$allowedKinds = @('index', 'system', 'module', 'workflow')
$allowedStatuses = @('current', 'draft', 'stale')

function Get-RelativeDisplayPath {
    param([string]$Path)

    $repositoryUri = [System.Uri]::new(($RepositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($Path)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

function Add-WikiDiagnostic {
    param(
        [string]$Code,
        [string]$Path,
        [int]$Line,
        [string]$Message,
        [ValidateSet('error', 'warning')]
        [string]$Severity = 'error'
    )

    $diagnostics.Add([pscustomobject][ordered]@{
        code = $Code
        severity = $Severity
        path = $Path
        line = $Line
        message = $Message
    })
}

function Test-RepositoryRelativePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path) -or
        [System.IO.Path]::IsPathRooted($Path) -or
        $Path.Contains('\') -or
        $Path -match '(^|/)\.\.?(/|$)' -or
        $Path -match '^[a-zA-Z][a-zA-Z0-9+.-]*:') {
        return $false
    }
    return $true
}

function ConvertTo-WikiAnchor {
    param([string]$Heading)

    $slug = $Heading.ToLowerInvariant()
    $slug = [regex]::Replace($slug, '<[^>]+>', '')
    $slug = [regex]::Replace($slug, '[`*_~]', '')
    $slug = [regex]::Replace($slug, '[^\p{L}\p{Nd}\s-]', '')
    $slug = [regex]::Replace($slug.Trim(), '\s+', '-')
    return $slug
}

function Get-WikiAnchors {
    param([string[]]$Lines)

    $anchors = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $slugCounts = @{}
    $insideFence = $false
    foreach ($line in $Lines) {
        if ($line -match '^\s*```') {
            $insideFence = -not $insideFence
            continue
        }
        if ($insideFence) {
            continue
        }
        foreach ($anchorMatch in [regex]::Matches($line, '<a\s+(?:[^>]*?\s)?(?:id|name)=["'']([^"'']+)["''][^>]*>', 'IgnoreCase')) {
            $null = $anchors.Add($anchorMatch.Groups[1].Value)
        }
        if ($line -notmatch '^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$') {
            continue
        }
        $baseSlug = ConvertTo-WikiAnchor $Matches[1]
        if ([string]::IsNullOrWhiteSpace($baseSlug)) {
            continue
        }
        $slug = $baseSlug
        if ($slugCounts.ContainsKey($baseSlug)) {
            $slugCounts[$baseSlug]++
            $slug = "$baseSlug-$($slugCounts[$baseSlug])"
        } else {
            $slugCounts[$baseSlug] = 0
        }
        $null = $anchors.Add($slug)
    }
    return $anchors
}

$pages = @(Get-ChildItem -LiteralPath $WikiRoot -Recurse -File -Filter '*.md' |
    Where-Object { $_.FullName -ne (Join-Path $WikiRoot 'README.md') })

if ($pages.Count -eq 0) {
    Add-WikiDiagnostic 'WIKI001' (Get-RelativeDisplayPath $WikiRoot) 0 'No wiki pages were found.'
}

$pageLines = @{}
$pageAnchors = @{}
foreach ($page in $pages) {
    $lines = @(Get-Content -LiteralPath $page.FullName)
    $pageLines[$page.FullName] = $lines
    $pageAnchors[$page.FullName] = Get-WikiAnchors $lines
}

foreach ($page in $pages) {
    $displayPath = Get-RelativeDisplayPath $page.FullName
    $lines = $pageLines[$page.FullName]

    if ($lines.Count -lt 3 -or $lines[0] -ne '---') {
        Add-WikiDiagnostic 'WIKI002' $displayPath 1 'Missing opening front matter delimiter.'
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
        Add-WikiDiagnostic 'WIKI003' $displayPath 1 'Missing closing front matter delimiter.'
        continue
    }

    $frontMatter = @($lines[1..($closingDelimiter - 1)])
    $fields = @{}
    $fieldLines = @{}
    for ($index = 0; $index -lt $frontMatter.Count; $index++) {
        $line = $frontMatter[$index]
        if ($line -notmatch '^([A-Za-z_][A-Za-z0-9_-]*):(?:\s*(.*))?$') {
            continue
        }
        $key = $Matches[1]
        $value = $Matches[2]
        $sourceLine = $index + 2
        if ($fields.ContainsKey($key)) {
            Add-WikiDiagnostic 'WIKI005' $displayPath $sourceLine "Duplicate front matter field '$key'."
            continue
        }
        $fields[$key] = $value
        $fieldLines[$key] = $sourceLine
        if ($key -notin $allowedKeys) {
            Add-WikiDiagnostic 'WIKI006' $displayPath $sourceLine "Unknown front matter field '$key'."
        }
    }

    foreach ($requiredKey in @('id', 'kind', 'status', 'sources')) {
        if (-not $fields.ContainsKey($requiredKey)) {
            Add-WikiDiagnostic 'WIKI004' $displayPath 1 "Missing required front matter field '$requiredKey'."
        }
    }

    if ($fields.ContainsKey('id')) {
        $pageId = $fields['id'].Trim()
        if ($pageId -notmatch '^[a-z0-9]+(?:[._-][a-z0-9]+)*$') {
            Add-WikiDiagnostic 'WIKI007' $displayPath $fieldLines['id'] "Invalid id '$pageId'. Use a stable lowercase identifier."
        } elseif ($ids.ContainsKey($pageId)) {
            Add-WikiDiagnostic 'WIKI008' $displayPath $fieldLines['id'] "Duplicate id '$pageId', also used by $($ids[$pageId])."
        } else {
            $ids[$pageId] = $displayPath
        }
    }

    if ($fields.ContainsKey('kind') -and $fields['kind'].Trim() -notin $allowedKinds) {
        Add-WikiDiagnostic 'WIKI009' $displayPath $fieldLines['kind'] "Unsupported kind '$($fields['kind'].Trim())'."
    }
    if ($fields.ContainsKey('status') -and $fields['status'].Trim() -notin $allowedStatuses) {
        Add-WikiDiagnostic 'WIKI010' $displayPath $fieldLines['status'] "Unsupported status '$($fields['status'].Trim())'."
    }

    $generatedBy = if ($fields.ContainsKey('generated_by')) { $fields['generated_by'].Trim() } else { $null }
    if ($displayPath -match '^\.llm-wiki/generated/' -and [string]::IsNullOrWhiteSpace($generatedBy)) {
        Add-WikiDiagnostic 'WIKI011' $displayPath 1 'Generated pages must declare generated_by.'
    }
    if (-not [string]::IsNullOrWhiteSpace($generatedBy)) {
        if (-not (Test-RepositoryRelativePath $generatedBy) -or
            $generatedBy -notmatch '^\.llm-wiki/tools/.+\.ps1$') {
            Add-WikiDiagnostic 'WIKI012' $displayPath $fieldLines['generated_by'] "generated_by must be a normalized repository-relative PowerShell tool path: '$generatedBy'."
        } elseif (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot $generatedBy) -PathType Leaf)) {
            Add-WikiDiagnostic 'WIKI013' $displayPath $fieldLines['generated_by'] "generated_by tool does not exist: '$generatedBy'."
        }
    }

    if ($fields.ContainsKey('sources')) {
        $sources = [System.Collections.Generic.List[object]]::new()
        $sourcesIndex = $fieldLines['sources'] - 2
        for ($index = $sourcesIndex + 1; $index -lt $frontMatter.Count; $index++) {
            if ($frontMatter[$index] -match '^\s+-\s+(.+?)\s*$') {
                $sources.Add([pscustomobject]@{ value = $Matches[1]; line = $index + 2 })
                continue
            }
            if ($frontMatter[$index] -match '^\S') {
                break
            }
        }
        if ($sources.Count -eq 0) {
            Add-WikiDiagnostic 'WIKI014' $displayPath $fieldLines['sources'] 'Sources list is empty.'
        }
        foreach ($source in $sources) {
            if (-not (Test-RepositoryRelativePath $source.value)) {
                Add-WikiDiagnostic 'WIKI015' $displayPath $source.line "Source path is not normalized and repository-relative: '$($source.value)'."
            } elseif (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot $source.value))) {
                Add-WikiDiagnostic 'WIKI016' $displayPath $source.line "Source does not exist: '$($source.value)'."
            }
        }
    }

    $insideFence = $false
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        if ($line -match '^\s*```') {
            $insideFence = -not $insideFence
        }
        if (-not $insideFence) {
            foreach ($linkMatch in [regex]::Matches($line, '(?<!\!)\[[^\]]+\]\(([^)]+)\)')) {
                $target = $linkMatch.Groups[1].Value.Trim()
                if ($target -match '^(https?://|mailto:)') {
                    continue
                }
                $parts = $target -split '#', 2
                $pathPart = ($parts[0] -split '\?', 2)[0]
                $anchor = if ($parts.Count -eq 2) { [System.Uri]::UnescapeDataString($parts[1]) } else { $null }
                $targetPath = $page.FullName
                if (-not [string]::IsNullOrWhiteSpace($pathPart)) {
                    $decodedTarget = [System.Uri]::UnescapeDataString($pathPart)
                    $targetPath = [System.IO.Path]::GetFullPath((Join-Path $page.DirectoryName $decodedTarget))
                    if (-not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
                        Add-WikiDiagnostic 'WIKI017' $displayPath ($index + 1) "Local link target does not exist: '$target'."
                        continue
                    }
                }
                if (-not [string]::IsNullOrWhiteSpace($anchor) -and
                    $targetPath.EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase)) {
                    if (-not $pageAnchors.ContainsKey($targetPath)) {
                        $targetLines = @(Get-Content -LiteralPath $targetPath)
                        $pageAnchors[$targetPath] = Get-WikiAnchors $targetLines
                    }
                    if (-not $pageAnchors[$targetPath].Contains($anchor)) {
                        Add-WikiDiagnostic 'WIKI018' $displayPath ($index + 1) "Local link anchor does not exist: '$target'."
                    }
                }
            }
        }

        if ($line -match '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----' -or
            $line -match '\bgh[pousr]_[A-Za-z0-9]{20,}\b' -or
            $line -match '\bAKIA[0-9A-Z]{16}\b') {
            Add-WikiDiagnostic 'WIKI019' $displayPath ($index + 1) 'Possible credential material found in wiki content.'
        }
    }
}

$orderedDiagnostics = @($diagnostics | Sort-Object path, line, code)
$errorCount = @($orderedDiagnostics | Where-Object severity -eq 'error').Count
$warningCount = @($orderedDiagnostics | Where-Object severity -eq 'warning').Count
$result = [pscustomobject][ordered]@{
    valid = $errorCount -eq 0
    pageCount = $pages.Count
    uniqueIdCount = $ids.Count
    errorCount = $errorCount
    warningCount = $warningCount
    diagnostics = $orderedDiagnostics
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 6
} elseif ($errorCount -gt 0 -or $warningCount -gt 0) {
    Write-Host "LLM Wiki lint found $errorCount error(s) and $warningCount warning(s):"
    foreach ($diagnostic in $orderedDiagnostics) {
        $location = if ($diagnostic.line -gt 0) { "$($diagnostic.path):$($diagnostic.line)" } else { $diagnostic.path }
        Write-Host " - [$($diagnostic.code)] $location $($diagnostic.message)"
    }
} else {
    Write-Host "LLM Wiki lint passed: $($pages.Count) pages, $($ids.Count) unique ids."
}

if ($errorCount -gt 0) {
    exit 1
}
