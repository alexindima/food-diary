[CmdletBinding()]
param(
    [string]$Query,
    [Alias('PlannedPath', 'ProposedPath', 'ChangedPath')]
    [string[]]$ScopePath,
    [ValidateSet('all', 'credential', 'identity', 'health', 'financial', 'privateContent', 'logging', 'boundaries', 'external')]
    [string]$Category = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$index = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/sensitive-data-index.json') -Raw | ConvertFrom-Json
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$scopePaths = @(
    $ScopePath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_ -split '[;,]' } |
        ForEach-Object { $_.Trim().Replace('\', '/') } |
        Where-Object { $_.Length -gt 0 } |
        Sort-Object -Unique
)
$scopeMode = if ($scopePaths.Count -gt 0) { 'explicit' } else { 'none' }
if ($scopePaths.Count -eq 0 -and [string]::IsNullOrWhiteSpace($Query) -and $Category -eq 'all') {
    $gitPaths = @(& git -C $repositoryRoot diff --name-only HEAD --)
    $gitPaths += @(& git -C $repositoryRoot ls-files --others --exclude-standard)
    $scopePaths = @(
        $gitPaths |
            ForEach-Object { $_.Replace('\', '/') } |
            Where-Object { $_ -notmatch '^\.llm-wiki/' } |
            Sort-Object -Unique
    )
    if ($scopePaths.Count -gt 0) { $scopeMode = 'git-diff' }
}
$items = if ($Category -eq 'logging') {
    @($index.potentialLogging)
} elseif ($Category -eq 'boundaries') {
    @($index.boundaryFiles)
} elseif ($Category -eq 'external') {
    @($index.externalTransfers)
} elseif ($Category -eq 'all') {
    @($index.fields) + @($index.externalTransfers)
} else {
    @($index.fields | Where-Object category -eq $Category)
}
$searchInput = (@($Query) + @($scopePaths)) -join ' '
if (-not [string]::IsNullOrWhiteSpace($searchInput)) {
    $aliases = @{ photo = 'image'; picture = 'image'; ai = 'openai'; credential = 'token' }
    $queryTokens = @(
        [regex]::Matches($searchInput.ToLowerInvariant(), '[\p{L}\p{Nd}]+') |
            ForEach-Object {
                $token = $_.Value
                if ($aliases.ContainsKey($token)) { @($token, $aliases[$token]) } else { $token }
            } |
            Where-Object { $_.Length -ge 3 -and $_ -notin @('fooddiary', 'client', 'shared', 'features', 'components', 'app', 'src') } |
            Sort-Object -Unique
    )
    $items = @(
        $items |
            ForEach-Object {
                $item = $_
                $searchText = $item | ConvertTo-Json -Compress
                $matchCount = @($queryTokens | Where-Object {
                    $searchText -match [regex]::Escape($_)
                }).Count
                $itemPath = [string]$item.path
                $scopeMatch = @($scopePaths | Where-Object {
                    $scopePath = $_
                    $scopeDirectory = if ([IO.Path]::HasExtension($scopePath)) { Split-Path -Parent $scopePath } else { $scopePath }
                    $itemPath -eq $scopePath -or $itemPath.StartsWith("$($scopeDirectory.Replace('\', '/').TrimEnd('/'))/")
                }).Count -gt 0
                $score = $matchCount + $(if ($scopeMatch) { 20 } else { 0 })
                $minimumMatches = if ($scopePaths.Count -gt 0 -and -not $scopeMatch) { 2 } else { 1 }
                if ($scopeMatch -or $matchCount -ge $minimumMatches) {
                    [pscustomobject]@{ item = $item; score = $score; scopeMatch = $scopeMatch }
                }
            } |
            Sort-Object @{ Expression = 'score'; Descending = $true }, @{ Expression = { $_.item.path } } |
            Select-Object -ExpandProperty item
    )
}
$guidance = @()
if ($scopeMode -eq 'none' -and [string]::IsNullOrWhiteSpace($Query) -and $Category -eq 'all') {
    $items = @()
    $guidance = @(
        "Provide -Query, choose -PrivacyCategory, or scope the review with -PlannedPath @('path/one','path/two')."
        "When a non-wiki git diff exists, the default privacy command scopes itself to that diff."
    )
}
$items = @($items | Select-Object -First $Limit)
if ($Format -eq 'Json') {
    [pscustomobject]@{
        category = $Category
        count = $items.Count
        scope = [pscustomobject]@{ mode = $scopeMode; paths = $scopePaths }
        guidance = $guidance
        summary = $index.summary
        items = $items
    } | ConvertTo-Json -Depth 8
    exit 0
}
Write-Host "Sensitive data '$Category': $($items.Count) candidate(s), scope=$scopeMode."
foreach ($message in $guidance) { Write-Host " - $message" }
foreach ($item in $items) { Write-Host " - $(($item | ConvertTo-Json -Compress))" }
