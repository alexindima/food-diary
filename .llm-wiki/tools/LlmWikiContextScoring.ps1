function ConvertTo-RepositoryPath {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $repositoryUri = [System.Uri]::new(($repositoryRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar))
    $pathUri = [System.Uri]::new($resolvedPath)
    return [System.Uri]::UnescapeDataString($repositoryUri.MakeRelativeUri($pathUri).ToString())
}

function Get-SearchScore {
    param(
        [string]$Text,
        [string[]]$Tokens,
        [int]$TokenWeight = 4,
        [int]$ExactWeight = 12
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return 0
    }

    $normalizedText = $Text.ToLowerInvariant()
    if ($searchNeedsCamelCaseExpansion) {
        $normalizedText = $Text -creplace '([a-z0-9])([A-Z])', '$1 $2'
        $normalizedText = $normalizedText.ToLowerInvariant()
    }
    $score = 0
    foreach ($token in $Tokens) {
        $tokenMatched = $token.Length -ge 4 -and $normalizedText.Contains($token)
        if (-not $tokenMatched -and $token.Length -lt 4) {
            $searchStart = 0
            while ($searchStart -lt $normalizedText.Length) {
                $matchIndex = $normalizedText.IndexOf($token, $searchStart, [StringComparison]::Ordinal)
                if ($matchIndex -lt 0) { break }
                $matchEnd = $matchIndex + $token.Length
                $leftBoundary = $matchIndex -eq 0 -or -not [char]::IsLetterOrDigit($normalizedText[$matchIndex - 1])
                $rightBoundary = $matchEnd -eq $normalizedText.Length -or -not [char]::IsLetterOrDigit($normalizedText[$matchEnd])
                if ($leftBoundary -and $rightBoundary) {
                    $tokenMatched = $true
                    break
                }
                $searchStart = $matchIndex + 1
            }
        }
        if ($tokenMatched) {
            $score += $TokenWeight
        }
    }

    foreach ($phrase in $searchPhrases) {
        if ($normalizedText.Contains($phrase)) {
            $score += $ExactWeight
        }
    }
    return $score
}

function Get-ScopeAffinity {
    param([string]$Path)

    if ($scopePaths.Count -eq 0 -or [string]::IsNullOrWhiteSpace($Path)) { return 0 }
    $normalizedPath = $Path.Replace('\', '/')
    foreach ($scopePath in $scopePaths) {
        $scopeDirectory = if ([IO.Path]::HasExtension($scopePath)) { Split-Path -Parent $scopePath } else { $scopePath }
        $scopeDirectory = $scopeDirectory.Replace('\', '/').TrimEnd('/')
        if ($normalizedPath -eq $scopePath -or
            $normalizedPath.StartsWith("$scopeDirectory/", [StringComparison]::OrdinalIgnoreCase) -or
            $scopePath.StartsWith("$($normalizedPath.TrimEnd('/'))/", [StringComparison]::OrdinalIgnoreCase)) {
            return 40
        }
        $featureMatch = [regex]::Match($scopePath, '/features/(?<feature>[^/]+)/')
        if ($featureMatch.Success -and $normalizedPath -match "/features/$([regex]::Escape($featureMatch.Groups['feature'].Value))/") {
            return 24
        }
    }
    return -8
}

function Select-ScoredItems {
    param(
        [object[]]$Items,
        [int]$Maximum = $Limit
    )

    return @(
        $Items |
            Where-Object { $_.score -gt 0 } |
            Sort-Object @{ Expression = 'score'; Descending = $true }, @{ Expression = 'path'; Descending = $false } |
            Select-Object -First $Maximum
    )
}
