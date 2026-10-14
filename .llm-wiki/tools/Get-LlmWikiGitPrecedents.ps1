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
. (Join-Path $PSScriptRoot 'LlmWikiGitHistory.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$normalizedPaths = @($ScopePath | ForEach-Object { $_ -split '[;,]' } | ForEach-Object { $_.Trim().Replace('\', '/') } | Where-Object { $_ } | Sort-Object -Unique)
$scopeMatchers = @($normalizedPaths | ForEach-Object {
    $directory = if ([IO.Path]::HasExtension($_)) { Split-Path -Parent $_ } else { $_ }
    [pscustomobject]@{ path = $_; prefix = $directory.Replace('\', '/').TrimEnd('/') + '/' }
})
$stopWords = @('about', 'after', 'before', 'change', 'create', 'from', 'into', 'that', 'this', 'with')
$tokens = @([regex]::Matches($Objective.ToLowerInvariant(), '[\p{L}\p{Nd}]{4,}') | ForEach-Object Value | Where-Object { $_ -notin $stopWords } | Sort-Object -Unique)

$history = Get-LlmWikiRankedPrecedentHistory -RepositoryRoot $repositoryRoot -Tokens $tokens -ScopeMatchers $scopeMatchers
$ranked = @($history.Ranked | Sort-Object @{ Expression = 'score'; Descending = $true }, @{ Expression = 'date'; Descending = $true } | Select-Object -First $Limit)
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    objective = $Objective
    scopePaths = $normalizedPaths
    queryTokens = $tokens
    searchedCommitCount = $history.CommitCount
    precedents = @($ranked)
    confidence = if ($normalizedPaths.Count -gt 0 -and $ranked.Count -gt 0) { 'medium' } elseif ($ranked.Count -gt 0) { 'low' } else { 'none' }
    authority = 'Git history is precedent evidence, not current-source authority. Verify every reused pattern in current code and guides.'
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 8; exit 0 }
Write-Host "Git precedents: $(@($ranked).Count) match(es) from $($history.CommitCount) commits"
foreach ($item in $ranked) {
    Write-Host "$($item.shortHash) score=$($item.score): $($item.subject)"
    foreach ($path in $item.matchedPaths) { Write-Host "  Path: $path" }
}
Write-Host $result.authority
