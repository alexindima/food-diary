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
function Get-StandaloneProjectionStatus([string]$Json) {
    if ($PSVersionTable.PSVersion.Major -lt 7) { return $Json | ConvertFrom-Json }
    $document = [System.Text.Json.JsonDocument]::Parse($Json)
    try {
        $ready = $document.RootElement.GetProperty('ready').GetBoolean()
        $reason = if ($ready) { '' } else { $document.RootElement.GetProperty('unavailableReason').GetString() }
        return [pscustomobject]@{ ready=$ready; unavailableReason=$reason }
    } finally { $document.Dispose() }
}
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$arguments = @{ Action = 'read-index'; Index = $Index; Query = $Query; Limit = $Limit; SkipRefresh = $true; Format = 'Json' }
$directRead = [string]::IsNullOrWhiteSpace([string]$env:LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT)
if ($directRead) {
    . (Join-Path $PSScriptRoot 'LlmWikiStandaloneIndex.ps1')
    $repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
    $rawProjection = Invoke-LlmWikiStandaloneIndexRead -RepositoryRoot $repositoryRoot -Index $Index -Query $Query -Limit $Limit
    $projection = Get-StandaloneProjectionStatus ($rawProjection -join [Environment]::NewLine)
    if (-not [bool]$projection.ready -and [string]$projection.unavailableReason -ne 'compiled-source-missing') {
        Write-Verbose "Standalone projection unavailable ($($projection.unavailableReason)); using isolated recovery."
        & (Join-Path $PSScriptRoot 'Invoke-LlmWikiReadOnlyTool.ps1') -ToolPath $PSCommandPath `
            -ToolArguments @{ Index=$Index; Query=$Query; Limit=$Limit; Format=$Format; CheckFreshness=[bool]$CheckFreshness } `
            -PrepareCodeGraph -BackendOnlyRefresh
        return
    }
} else {
    $rawProjection = & $manager @arguments
    $projection = Get-StandaloneProjectionStatus ($rawProjection -join [Environment]::NewLine)
}
if (-not [bool]$projection.ready) {
    if ([string]$projection.unavailableReason -eq 'compiled-source-missing') {
        throw "Compiled index '$Index' is missing. Run ./.llm-wiki/wiki.ps1 update and retry."
    }
    $null = & $manager -Action build -BackendOnlyRefresh -Format Json
    $rawProjection = & $manager @arguments
    $projection = Get-StandaloneProjectionStatus ($rawProjection -join [Environment]::NewLine)
    if (-not [bool]$projection.ready) {
        throw "SQLite compiled index '$Index' remains unavailable ($($projection.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }
}
$rawIndex = if ($PSVersionTable.PSVersion.Major -lt 7) {
    (($rawProjection -join [Environment]::NewLine) | ConvertFrom-Json).index | ConvertTo-Json -Depth 20
} else {
    $document = [System.Text.Json.JsonDocument]::Parse(($rawProjection -join [Environment]::NewLine))
    try { $document.RootElement.GetProperty('index').GetRawText() }
    finally { $document.Dispose() }
}
if ($Format -eq 'Json' -and -not $CheckFreshness) { $rawIndex; return }
$result = $rawIndex | ConvertFrom-Json
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
