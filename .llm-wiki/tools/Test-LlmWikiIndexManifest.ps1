[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$manifestPath = Join-Path $repositoryRoot '.llm-wiki/policies/query-indexes.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$paths = @($manifest.paths | ForEach-Object { ([string]$_).Replace('\', '/') })
if ([int]$manifest.schemaVersion -ne 1 -or $paths.Count -ne @($paths | Sort-Object -Unique).Count) {
    throw 'Wiki query-index manifest schema or path uniqueness is invalid.'
}
foreach ($requiredPath in @(
    '.llm-wiki/generated/domain-data-index.json'
    '.llm-wiki/generated/frontend-contract-index.json'
    '.llm-wiki/generated/frontend-index.json'
    '.llm-wiki/generated/runtime-topology.json'
    '.llm-wiki/generated/sensitive-data-index.json'
)) {
    if ($requiredPath -notin $paths) { throw "Wiki query-index manifest omits a query dependency: $requiredPath" }
}
foreach ($path in $paths) {
    $absolutePath = Join-Path $repositoryRoot $path
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { throw "Manifest index is missing: $path" }
    try { $null = Get-Content -LiteralPath $absolutePath -Raw | ConvertFrom-Json }
    catch { throw "Manifest index is not valid JSON: $path" }
}
$receiptText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Write-LlmWikiIndexVerificationReceipt.ps1') -Raw
$mcpManifestText = Get-Content -LiteralPath (Join-Path $repositoryRoot 'FoodDiary.Development.Mcp/Diagnostics/WikiIndexManifest.cs') -Raw
if (-not $receiptText.Contains('.llm-wiki/policies/query-indexes.json') -or
    -not $mcpManifestText.Contains('.llm-wiki/policies/query-indexes.json')) {
    throw 'PowerShell receipt and MCP status must consume the shared query-index manifest.'
}
Write-Host "LLM Wiki index-manifest regression passed: $($paths.Count) query dependencies share one freshness contract."
