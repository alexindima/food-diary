[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Query,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 30)]
    [int]$Limit = 10,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite'
)

$ErrorActionPreference = 'Stop'
$searchQuery = [regex]::Replace(
    $Query,
    '(?is)^\s*trace\s+(?:the\s+)?(?:primary\s+)?user\s+(?:scenario|journey|flow)(?:\s+end(?:\s+|-)to(?:\s+|-)end)?(?:\s+from\s+endpoint\s+or\s+event\s+through\s+command/query\s+to\s+persistence/provider)?(?:\s+for)?\s+',
    '')
$context = & (Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1') `
    -Query $searchQuery -CompiledIndexSource $CompiledIndexSource -SkipQueryCache -Limit $Limit -Format Json | ConvertFrom-Json
$result = [pscustomobject][ordered]@{
    schemaVersion = 1
    query = $Query
    normalizedIntent = $searchQuery
    matched = @($context.candidates).Count -gt 0
    traceConclusive = $false
    abstained = $true
    abstentionReason = 'The request describes a broad area rather than one executable entry point. Ranked candidates are returned without inventing an end-to-end chain.'
    selectionConfidence = $context.confidence
    ambiguous = -not [bool]$context.conclusive
    ambiguityReason = $context.ambiguityReason
    entryCandidates = @($context.candidates | Select-Object -First $Limit)
    chain = @()
    recommendedNextAction = 'Choose one candidate symbol or endpoint and rerun trace with its exact name and optional -Module/-PathPrefix.'
    compiledIndex = $context.compiledIndex
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 12; return }
Write-Host "Trace abstained: $($result.abstentionReason)"
foreach ($candidate in $result.entryCandidates) { Write-Host " - #$($candidate.rank) [$($candidate.confidence)] $($candidate.path)" }
Write-Host "Next: $($result.recommendedNextAction)"
