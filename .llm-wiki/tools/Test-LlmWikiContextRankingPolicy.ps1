[CmdletBinding()]
param(
    [ValidateRange(1, 1000)]
    [int]$MaximumNormalizationRules = 400,
    [ValidateRange(1, 1000)]
    [int]$MaximumRankingRules = 400,
    [ValidateRange(0, 200)]
    [int]$MaximumRuleGrowth = 60,
    [switch]$FailOnInvalid,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$policyPath = Join-Path $repositoryRoot '.llm-wiki/policies/context-search-ranking.json'
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json
function Get-Counts($Policy) {
    [pscustomobject][ordered]@{
        normalization = @($Policy.queryTermExpansions.PSObject.Properties).Count + @($Policy.queryPrefixExpansions.PSObject.Properties).Count
        ranking = @($Policy.pathBoosts).Count + @($Policy.identityBoosts).Count + @($Policy.structuralRoleBoosts).Count
    }
}
$counts = Get-Counts $policy
$baselineText = (& git -C $repositoryRoot show HEAD:.llm-wiki/policies/context-search-ranking.json) -join "`n"
$baseline = $baselineText | ConvertFrom-Json
$baselineCounts = Get-Counts $baseline
$ids = @(@($policy.pathBoosts).id + @($policy.identityBoosts).id + @($policy.structuralRoleBoosts).id | Where-Object { $_ })
$duplicates = @($ids | Group-Object | Where-Object Count -gt 1 | Select-Object -ExpandProperty Name)
$issues = [Collections.Generic.List[string]]::new()
if ($counts.normalization -gt $MaximumNormalizationRules) { $issues.Add("normalization=$($counts.normalization)>$MaximumNormalizationRules") }
if ($counts.ranking -gt $MaximumRankingRules) { $issues.Add("ranking=$($counts.ranking)>$MaximumRankingRules") }
if (($counts.normalization + $counts.ranking) - ($baselineCounts.normalization + $baselineCounts.ranking) -gt $MaximumRuleGrowth) { $issues.Add('policy growth exceeded the per-change budget') }
if ($duplicates.Count -gt 0) { $issues.Add("duplicate ids: $($duplicates -join ', ')") }
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    valid = $issues.Count -eq 0
    counts = $counts
    baselineCounts = $baselineCounts
    growth = ($counts.normalization + $counts.ranking) - ($baselineCounts.normalization + $baselineCounts.ranking)
    duplicateIds = $duplicates
    issues = @($issues)
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 6 } else {
    Write-Host "Context ranking policy: valid=$($result.valid), normalization=$($counts.normalization)/$MaximumNormalizationRules, ranking=$($counts.ranking)/$MaximumRankingRules, growth=$($result.growth)/$MaximumRuleGrowth."
    foreach ($issue in $issues) { Write-Host " - $issue" }
}
if ($FailOnInvalid -and -not $result.valid) { throw "Context ranking policy governance failed: $($issues -join '; ')." }
