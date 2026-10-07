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
    $writer = Join-Path $fixtureTools 'Write-LlmWikiIndexVerificationReceipt.ps1'
    # Scoped success neither reads full-gate inputs nor changes publication state.
    # At this point the fixture deliberately has no manifest, index or Git HEAD.
    $partialMessage = & $writer -ReceiptKind Verification 6>&1 | Out-String
    if ($partialMessage -notmatch 'full verification status is unchanged' -or
        (Test-Path -LiteralPath (Join-Path $fixture '.git/llm-wiki'))) { throw 'Scoped verification touched full publication evidence.' }
    [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/policies/query-indexes.json'), '{"schemaVersion":1,"paths":[".llm-wiki/generated/index.json"]}')
    [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/generated/index.json'), '{}')
    [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'), 'public class Original {}')
    [IO.File]::WriteAllBytes((Join-Path $fixture 'empty.bin'), [byte[]]@())
    $sourceFixturePaths = @('source.cs', 'empty.bin')
    # Windows PowerShell 5.1 loads BOM-free scripts through the ANSI code page.
    $unicodePathSegment = [string][char]0x043F + [char]0x0443 + [char]0x0442 + [char]0x044C
    foreach ($index in 1..64) {
        $path = 'source ' + ($unicodePathSegment * 12) + " $index.cs"
        $sourceFixturePaths += $path
        [IO.File]::WriteAllText((Join-Path $fixture $path), "// $unicodePathSegment $index", [Text.UTF8Encoding]::new($false))
    }
    & git -C $fixture add .
    & git -C $fixture -c user.name='Wiki Tests' -c user.email='wiki@example.invalid' commit --quiet -m baseline
    $expectedPaths = @(@(
        '.llm-wiki/policies/query-indexes.json'
        '.llm-wiki/tools/Write-LlmWikiIndexVerificationReceipt.ps1'
        '.llm-wiki/tools/LlmWikiGitPaths.ps1'
    ) + $sourceFixturePaths | Sort-Object -Unique)
    if ([Text.Encoding]::UTF8.GetByteCount($expectedPaths -join "`n") -le 4096) { throw 'Source hashing fixture must cross a native stdin buffer boundary.' }
    # Independent argv hashing must match the production redirected-file batch.
    $expectedHashes = @(& git -C $fixture hash-object -- $expectedPaths)
    if ($LASTEXITCODE -ne 0 -or $expectedHashes.Count -ne $expectedPaths.Count) { throw 'Native source hash reference failed.' }
    $expectedEntries = for ($index = 0; $index -lt $expectedPaths.Count; $index++) { "$($expectedPaths[$index]):$($expectedHashes[$index])" }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $expectedFingerprint = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($expectedEntries -join "`n") + "`n"))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
    $writer = Join-Path $fixtureTools 'Write-LlmWikiIndexVerificationReceipt.ps1'
    & $writer
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.sourceFingerprint -cne $expectedFingerprint) { throw 'Publication source hashes differ from the complete native argv reference.' }
    if ($status.verification.state -ne 'unverified') { throw 'A partial verify issued a full verification receipt.' }
    & $writer -ReceiptKind Generation
    & $writer
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.generation.state -ne 'verified' -or $status.verification.state -ne 'unverified') { throw 'A scoped verify claimed the full gate after generation.' }
    & $writer -CompletedFullVerification
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.generation.state -ne 'verified' -or $status.verification.state -ne 'verified') { throw 'Publication states did not recognize matching content.' }
    $untrackedPath = Join-Path $fixture "$unicodePathSegment untracked.cs"
    [IO.File]::WriteAllText($untrackedPath, '// untracked', [Text.UTF8Encoding]::new($false))
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.generation.state -ne 'stale' -or $status.verification.state -ne 'stale') { throw 'Publication fingerprints ignored an untracked Unicode source.' }
    Remove-Item -LiteralPath $untrackedPath -Force
    Remove-Item -LiteralPath (Join-Path $fixture 'empty.bin') -Force
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.generation.state -ne 'stale' -or $status.verification.state -ne 'stale') { throw 'Publication fingerprints ignored a deleted source.' }
    [IO.File]::WriteAllBytes((Join-Path $fixture 'empty.bin'), [byte[]]@())
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.sourceFingerprint -cne $expectedFingerprint -or $status.generation.state -ne 'verified' -or $status.verification.state -ne 'verified') { throw 'Restoring identical source contents did not recover matching publication states.' }
    $sourceTimestamp = [IO.File]::GetLastWriteTimeUtc((Join-Path $fixture 'source.cs'))
    [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'), 'public class Modified {}')
    [IO.File]::SetLastWriteTimeUtc((Join-Path $fixture 'source.cs'), $sourceTimestamp)
    & $writer -CompletedFullVerification
    $status = & $writer -ReceiptKind Status | ConvertFrom-Json
    if ($status.generation.state -ne 'stale' -or $status.verification.state -ne 'stale') { throw 'A partial verify hid stale generation.' }
} finally {
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host 'Wiki publication receipt regression passed: native Unicode/empty-file hashes, untracked/deleted sources, preserved-timestamp edits and distinct publication states.'
