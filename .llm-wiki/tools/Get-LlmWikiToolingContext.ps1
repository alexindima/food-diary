[CmdletBinding()]
param(
    [Parameter(Mandatory)][Alias('Intent')][string]$Query,
    [ValidateRange(1, 50)][int]$Limit = 12,
    [ValidateSet('Text', 'Json')][string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = Split-Path -Parent $wikiRoot
$ignored = @('wiki', 'llm', 'tooling', 'change', 'improve', 'harden', 'simplify', 'with', 'from', 'that', 'this', 'and')
$tokens = @(
    [regex]::Matches($Query.ToLowerInvariant(), '[a-z0-9][a-z0-9-]{2,}') |
        ForEach-Object Value |
        Where-Object { $_ -notin $ignored } |
        Sort-Object -Unique
)

$candidatePaths = @(
    '.llm-wiki/wiki.ps1'
    '.llm-wiki/README.md'
    '.llm-wiki/policies/command-registry.json'
    '.llm-wiki/policies/affected-smoke-catalog.psd1'
    Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter '*.ps1' | ForEach-Object { ".llm-wiki/tools/$($_.Name)" }
    Get-ChildItem -LiteralPath (Join-Path $wikiRoot 'workflows') -File -Filter '*.md' | ForEach-Object { ".llm-wiki/workflows/$($_.Name)" }
) | Sort-Object -Unique

$ranked = @(
    foreach ($path in $candidatePaths) {
        $absolutePath = Join-Path $repositoryRoot $path
        if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { continue }
        $name = [IO.Path]::GetFileNameWithoutExtension($path).ToLowerInvariant()
        $content = [IO.File]::ReadAllText($absolutePath).ToLowerInvariant()
        $matched = @($tokens | Where-Object { $name.Contains($_) -or $content.Contains($_) })
        $nameMatches = @($tokens | Where-Object { $name.Contains($_) })
        $score = ($nameMatches.Count * 8) + ($matched.Count * 2)
        if ($score -le 0) { continue }
        [pscustomobject][ordered]@{
            path = $path
            score = $score
            matchedTerms = $matched
        }
    }
)
$items = @($ranked | Sort-Object @{ Expression = 'score'; Descending = $true }, path | Select-Object -First $Limit)
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    query = $Query
    tokens = $tokens
    items = $items
    groundedPaths = @($items.path)
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6; exit 0 }
Write-Host "Wiki tooling context: $($items.Count) path(s)"
foreach ($item in $items) { Write-Host " - $($item.path) (score=$($item.score); $($item.matchedTerms -join ', '))" }
