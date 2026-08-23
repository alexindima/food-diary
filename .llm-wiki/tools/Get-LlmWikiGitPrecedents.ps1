[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [Alias('Intent')]
    [string]$Objective,
    [Alias('PlannedPath')]
    [string[]]$ScopePath,
    [ValidateRange(1, 20)]
    [int]$Limit = 5,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$normalizedPaths = @($ScopePath | ForEach-Object { $_ -split '[;,]' } | ForEach-Object { $_.Trim().Replace('\', '/') } | Where-Object { $_ } | Sort-Object -Unique)
$stopWords = @('about', 'after', 'before', 'change', 'create', 'from', 'into', 'that', 'this', 'with')
$tokens = @([regex]::Matches($Objective.ToLowerInvariant(), '[\p{L}\p{Nd}]{4,}') | ForEach-Object Value | Where-Object { $_ -notin $stopWords } | Sort-Object -Unique)

$raw = (Invoke-LlmWikiGitCommand -RepositoryRoot $repositoryRoot -Arguments @('log', '--all', '-n', '300', '--date=iso-strict', '--pretty=format:@@@%H%x09%ad%x09%s', '--name-only') -FailureMessage 'Unable to read Git history for precedent analysis.').Lines
$commits = [Collections.Generic.List[object]]::new()
$current = $null
foreach ($line in $raw) {
    if ($line.StartsWith('@@@')) {
        if ($null -ne $current) { $commits.Add([pscustomobject]$current) }
        $parts = $line.Substring(3).Split("`t", 3)
        $current = [ordered]@{ hash = $parts[0]; date = $parts[1]; subject = $parts[2]; paths = [Collections.Generic.List[string]]::new() }
    } elseif ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($line)) {
        $current.paths.Add($line.Trim().Replace('\', '/'))
    }
}
if ($null -ne $current) { $commits.Add([pscustomobject]$current) }

$ranked = @(
    foreach ($commit in $commits) {
        $subject = $commit.subject.ToLowerInvariant()
        $tokenMatches = @($tokens | Where-Object { $subject.Contains($_) }).Count
        $pathMatches = 0
        foreach ($candidate in $commit.paths) {
            foreach ($scope in $normalizedPaths) {
                $scopeDirectory = if ([IO.Path]::HasExtension($scope)) { Split-Path -Parent $scope } else { $scope }
                if ($candidate -eq $scope -or $candidate.StartsWith("$($scopeDirectory.Replace('\', '/').TrimEnd('/'))/", [StringComparison]::OrdinalIgnoreCase)) { $pathMatches++; break }
            }
        }
        $score = ($tokenMatches * 4) + ([Math]::Min($pathMatches, 5) * 3)
        if ($score -le 0) { continue }
        [pscustomobject][ordered]@{
            hash = $commit.hash
            shortHash = $commit.hash.Substring(0, 10)
            date = $commit.date
            subject = $commit.subject
            score = $score
            matchedIntentTokens = @($tokens | Where-Object { $subject.Contains($_) })
            matchedPaths = @($commit.paths | Where-Object {
                $candidate = $_
                @($normalizedPaths | Where-Object {
                    $scopeDirectory = if ([IO.Path]::HasExtension($_)) { Split-Path -Parent $_ } else { $_ }
                    $candidate -eq $_ -or $candidate.StartsWith("$($scopeDirectory.Replace('\', '/').TrimEnd('/'))/", [StringComparison]::OrdinalIgnoreCase)
                }).Count -gt 0
            } | Select-Object -First 12)
        }
    }
) | Sort-Object @{ Expression = 'score'; Descending = $true }, @{ Expression = 'date'; Descending = $true } | Select-Object -First $Limit

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    scopePaths = $normalizedPaths
    queryTokens = $tokens
    searchedCommitCount = $commits.Count
    precedents = @($ranked)
    confidence = if ($normalizedPaths.Count -gt 0 -and $ranked.Count -gt 0) { 'medium' } elseif ($ranked.Count -gt 0) { 'low' } else { 'none' }
    authority = 'Git history is precedent evidence, not current-source authority. Verify every reused pattern in current code and guides.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 8; exit 0 }
Write-Host "Git precedents: $(@($ranked).Count) match(es) from $($commits.Count) commits"
foreach ($item in $ranked) {
    Write-Host "$($item.shortHash) score=$($item.score): $($item.subject)"
    foreach ($path in $item.matchedPaths) { Write-Host "  Path: $path" }
}
Write-Host $result.authority
