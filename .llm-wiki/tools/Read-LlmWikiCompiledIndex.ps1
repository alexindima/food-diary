[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('catalog', 'symbols', 'frontend', 'frontend-contract', 'backend-contract', 'architecture-health', 'domain-data', 'configuration', 'quality', 'runtime', 'sensitive-data', 'modules')]
    [string]$Index,
    [string]$Query,
    [switch]$CheckFreshness,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 50)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$arguments = @{ Action = 'read-index'; Index = $Index; Query = $Query; Limit = $Limit; SkipRefresh = $true; Format = 'Json' }
$projection = & $manager @arguments | ConvertFrom-Json
if (-not [bool]$projection.ready) {
    if ([string]$projection.unavailableReason -eq 'compiled-source-missing') {
        throw "Compiled index '$Index' is missing. Run ./.llm-wiki/wiki.ps1 update and retry."
    }
    $null = & $manager -Action build -BackendOnlyRefresh -Format Json
    $projection = & $manager @arguments | ConvertFrom-Json
    if (-not [bool]$projection.ready) {
        throw "SQLite compiled index '$Index' remains unavailable ($($projection.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }
}
$result = $projection.index
if ($CheckFreshness) {
    $publication = & (Join-Path $PSScriptRoot 'Write-LlmWikiIndexVerificationReceipt.ps1') -ReceiptKind Status | ConvertFrom-Json
    $result | Add-Member -NotePropertyName publication -NotePropertyValue $publication
    $result.freshness = "generation=$($publication.generation.state); verification=$($publication.verification.state); SQLite snapshot validated against its source"
}
if ($Format -eq 'Json') { $result | ConvertTo-Json -Depth 20; return }
Write-Host "Compiled index '$Index' (read-only SQLite): $($result.sourcePath)"
Write-Host "Freshness: $($result.freshness)"
if ($Index -eq 'modules') {
    foreach ($item in @($result.items)) { Write-Host " - $item" }
} else {
    foreach ($section in @($result.sections)) { Write-Host " - $($section.name): $($section.count) item(s), showing $(@($section.items).Count)" }
}
