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

. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'index-publication-receipts'
try {
    & git -C $fixture init --quiet
    $fixtureTools = Join-Path $fixture '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $fixtureTools, (Join-Path $fixture '.llm-wiki/policies'), (Join-Path $fixture '.llm-wiki/generated') -Force
    foreach ($name in @('Write-LlmWikiIndexVerificationReceipt.ps1', 'LlmWikiGitPaths.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $fixtureTools
    }
    [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/policies/query-indexes.json'), '{"schemaVersion":1,"paths":[".llm-wiki/generated/index.json"]}')
    [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/generated/index.json'), '{}')
    [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'), 'public class Original {}')
    & git -C $fixture add .
    & git -C $fixture -c user.name='Wiki Tests' -c user.email='wiki@example.invalid' commit --quiet -m baseline
    $writer = Join-Path $fixtureTools 'Write-LlmWikiIndexVerificationReceipt.ps1'
    & $writer
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.verification.state -ne 'unverified') { throw 'A partial verify issued a full verification receipt.' }
    & $writer -ReceiptKind Generation
    & $writer
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.generation.state -ne 'verified' -or $status.verification.state -ne 'unverified') { throw 'A scoped verify claimed the full gate after generation.' }
    & $writer -CompletedFullVerification
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.generation.state -ne 'verified' -or $status.verification.state -ne 'verified') { throw 'Publication states did not recognize matching content.' }
    [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'), 'public class Modified {}')
    & $writer -CompletedFullVerification
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.generation.state -ne 'stale' -or $status.verification.state -ne 'stale') { throw 'A partial verify hid stale generation.' }
} finally {
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host 'Wiki publication receipt regression passed: partial, generated, verified and stale states remain distinct.'
