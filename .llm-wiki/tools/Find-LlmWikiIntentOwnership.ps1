[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$search = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
    -Action search -Query $Query -Limit ([Math]::Min(50, [Math]::Max(20, $Limit * 3))) -SkipRefresh -Format Json | ConvertFrom-Json
$records = @($search.records)
$ranking = $search.rankingSummary
$confidence = if ($null -eq $ranking) { 'low' } else { [string]$ranking.confidence }
$ambiguous = if ($null -eq $ranking) { $true } else { [bool]$ranking.ambiguous }
$conclusive = $records.Count -gt 0 -and $confidence -in @('high', 'medium') -and -not $ambiguous
$selected = if ($conclusive) {
    @($records | Where-Object { [string]$_.confidence -in @('high', 'medium') } | Select-Object -First $Limit)
} else { @() }

$owners = [Collections.Generic.List[object]]::new()
foreach ($record in $selected) {
    $path = ([string]$record.path).Replace('\', '/')
    $segments = $path -split '/'
    $guide = 'AGENTS.md'
    for ($index = $segments.Count - 1; $index -ge 1; $index--) {
        $candidate = (($segments[0..($index - 1)] -join '/') + '/AGENTS.md')
        if (Test-Path -LiteralPath (Join-Path $repositoryRoot $candidate) -PathType Leaf) { $guide = $candidate; break }
    }
    $owners.Add([pscustomobject][ordered]@{
        path = $path
        guide = $guide
        module = [string]$record.module
        score = [double]$record.score
        confidence = [string]$record.confidence
        reasons = @($record.reasons)
    })
}

$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    query = $Query
    confidence = $confidence
    conclusive = $conclusive
    abstained = -not $conclusive
    abstentionReason = $(if ($records.Count -eq 0) { 'no-indexed-candidates' } elseif ($ambiguous) { [string]$ranking.ambiguityReason } elseif (-not $conclusive) { 'low-confidence' } else { $null })
    directModules = @($owners.module | Where-Object { $_ } | Sort-Object -Unique)
    transitivelyImpactedModules = @()
    downstreamModules = @()
    ownershipGuides = @($owners)
    candidates = @($records | Select-Object -First $Limit)
    index = [pscustomobject][ordered]@{ fingerprint = $search.fingerprint; updatedAtUtc = $search.updatedAtUtc; durationMs = $search.durationMs }
}

if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; return }
Write-Host "Intent ownership: confidence=$confidence; conclusive=$conclusive; candidates=$($records.Count)."
if (-not $conclusive) { Write-Host "Abstained: $($result.abstentionReason). Narrow the intent or provide -ChangedPath."; return }
foreach ($owner in $owners) { Write-Host " - $($owner.guide): $($owner.path) [$($owner.confidence)]" }
