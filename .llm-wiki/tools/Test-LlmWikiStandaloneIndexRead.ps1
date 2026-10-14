[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiStandaloneIndex.ps1')
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$parent = Join-Path $repositoryRoot '.artifacts/llm-wiki/standalone-read-fixtures'
$fixture = Join-Path $parent ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixture -Force
$fixtureTool = Join-Path $PSScriptRoot 'code-graph-standalone-fixture.mjs'
$previousCulture = [Globalization.CultureInfo]::CurrentCulture
try {
    $null = Initialize-LlmWikiInProcessSqlite
    & node $fixtureTool prepare $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Standalone reader fixture preparation failed.' }
    $expected = Get-Content -LiteralPath (Join-Path $fixture 'expected.json') -Raw | ConvertFrom-Json
    $results = [Collections.Generic.List[string]]::new()
    [Globalization.CultureInfo]::CurrentCulture = [Globalization.CultureInfo]::GetCultureInfo('tr-TR')
    foreach ($case in $expected.cases) {
        $json = Invoke-LlmWikiStandaloneIndexRead -RepositoryRoot $fixture -Index $case.index -Query $case.query -Limit $case.limit
        $result = $json | ConvertFrom-Json
        if (-not $result.ready) { throw "Standalone query failed: $($case.index) / $($case.query): $($result.unavailableReason)" }
        $results.Add($json)
    }
    ConvertTo-Json -InputObject @($results) -Depth 5 | Set-Content -LiteralPath (Join-Path $fixture 'actual.json') -Encoding utf8
    & node $fixtureTool compare $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Standalone cross-runtime result parity failed.' }
    $source = Join-Path $fixture '.llm-wiki/generated/csharp-symbol-index.json'
    $originalBytes = [IO.File]::ReadAllBytes($source)
    $timestamp = [IO.File]::GetLastWriteTimeUtc($source)
    $changed = [Text.Encoding]::UTF8.GetString($originalBytes).Replace('Alpha_', 'Omega_')
    [IO.File]::WriteAllText($source, $changed, [Text.UTF8Encoding]::new($false))
    [IO.File]::SetLastWriteTimeUtc($source, $timestamp)
    $stale = [LlmWiki.SqliteReader.StandaloneIndexReader]::Query($fixture, 'symbols', '', 2) | ConvertFrom-Json
    if ($stale.ready -or $stale.unavailableReason -ne 'standalone-index-projection-stale') { throw 'Same-size/same-time source edit escaped hash validation.' }
    [IO.File]::WriteAllBytes($source, $originalBytes)
    Remove-Item -LiteralPath $source
    $missing = [LlmWiki.SqliteReader.StandaloneIndexReader]::Query($fixture, 'symbols', '', 2) | ConvertFrom-Json
    if ($missing.unavailableReason -ne 'compiled-source-missing') { throw 'Missing source was not detected.' }
    [IO.File]::WriteAllBytes($source, $originalBytes)
    & node $fixtureTool schema-stale $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Schema fixture mutation failed.' }
    $schema = [LlmWiki.SqliteReader.StandaloneIndexReader]::Query($fixture, 'symbols', '', 2) | ConvertFrom-Json
    if ($schema.unavailableReason -ne 'standalone-index-schema-stale') { throw 'Stale schema was not detected.' }
    & node $fixtureTool refresh $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Schema fixture refresh failed.' }
    $database = Join-Path $fixture '.artifacts/llm-wiki/code-graph/code-graph.sqlite'
    $beforeHash = (Get-FileHash -LiteralPath $database).Hash
    $null = [LlmWiki.SqliteReader.StandaloneIndexReader]::Query($fixture, 'symbols', '', 2)
    & node (Join-Path $PSScriptRoot 'read-standalone-index.mjs') $fixture '--index=modules' '--query=Тип' '--limit=2' | Out-Null
    if ($LASTEXITCODE -ne 0 -or (Get-FileHash -LiteralPath $database).Hash -cne $beforeHash) { throw 'Read-only queries modified the SQLite database.' }
    Move-Item -LiteralPath $database -Destination "$database.saved"
    $missingDatabase = [LlmWiki.SqliteReader.StandaloneIndexReader]::Query($fixture, 'symbols', '', 2) | ConvertFrom-Json
    if ($missingDatabase.unavailableReason -ne 'standalone-index-projection-missing' -or (Test-Path -LiteralPath $database)) { throw 'Read-only query created a missing database.' }
    $originalInitializer = (Get-Item Function:\Initialize-LlmWikiInProcessSqlite).ScriptBlock
    $script:coldReaderInitializationAttempts = 0
    Set-Item Function:\Initialize-LlmWikiInProcessSqlite -Value {
        $script:coldReaderInitializationAttempts++
        throw 'A cache-missing read must not initialize the existing .NET reader.'
    }
    try {
        $cold = Invoke-LlmWikiStandaloneIndexRead -RepositoryRoot $fixture -Index symbols -Query Alpha_ -Limit 2 | ConvertFrom-Json
        if ($script:coldReaderInitializationAttempts -ne 0) { throw 'Cold read unnecessarily initialized the .NET reader.' }
    } finally { Set-Item Function:\Initialize-LlmWikiInProcessSqlite -Value $originalInitializer }
    if (-not $cold.ready -or (Test-Path -LiteralPath $database) -or -not (Test-Path -LiteralPath $cold.cacheDatabasePath)) { throw 'Cold read did not prepare an isolated index-only cache.' }
    $warm = Invoke-LlmWikiStandaloneIndexRead -RepositoryRoot $fixture -Index symbols -Query Alpha_ -Limit 2 | ConvertFrom-Json
    if (-not $warm.ready -or $warm.index.compiledIndex.sourceHash -cne $cold.index.compiledIndex.sourceHash) { throw 'Prepared standalone cache was not reusable in process.' }
    $results.Clear()
    foreach ($case in $expected.cases) {
        $json = Invoke-LlmWikiStandaloneIndexRead -RepositoryRoot $fixture -Index $case.index -Query $case.query -Limit $case.limit
        $result = $json | ConvertFrom-Json
        if (-not $result.ready) { throw "Private cache query failed: $($case.index) / $($case.query)" }
        # This internal cache hint is outside the published index response.
        $result.PSObject.Properties.Remove('cacheDatabasePath')
        $results.Add(($result | ConvertTo-Json -Depth 100 -Compress))
    }
    ConvertTo-Json -InputObject @($results) -Depth 5 | Set-Content -LiteralPath (Join-Path $fixture 'actual.json') -Encoding utf8
    & node $fixtureTool compare $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Cache-missing/private cache cross-runtime result parity failed.' }
    Move-Item -LiteralPath "$database.saved" -Destination $database
    $longRoot = Join-Path $fixture ('long-source-root-' * 10)
    $longGenerated = Join-Path $longRoot '.llm-wiki/generated'
    $null = New-Item -ItemType Directory -Path $longGenerated -Force
    [IO.File]::WriteAllText((Join-Path $longGenerated 'csharp-symbol-index.json'), '{"entries":[{"name":"LongPath"}]}', [Text.UTF8Encoding]::new($false))
    $longCache = Invoke-LlmWikiStandaloneIndexRead -RepositoryRoot $longRoot -Index symbols -Query LongPath -Limit 2 | ConvertFrom-Json
    if (-not $longCache.ready -or $longCache.cacheDatabasePath.Length -le 260) { throw 'Long-path cache preparation failed.' }
    $longRead = [LlmWiki.SqliteReader.StandaloneIndexReader]::Query($longRoot, 'symbols', 'LongPath', 2, $longCache.cacheDatabasePath) | ConvertFrom-Json
    if (-not $longRead.ready -or $longRead.index.sections[0].count -ne 1) { throw 'In-process long-path SQLite read failed.' }
    Write-Host 'Standalone read-only freshness, schema, Unicode, culture, limits and cross-runtime parity passed.'
} finally {
    [Globalization.CultureInfo]::CurrentCulture = $previousCulture
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    $parentPrefix = [IO.Path]::GetFullPath($parent).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedFixture.StartsWith($parentPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture escaped its owned parent.' }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
