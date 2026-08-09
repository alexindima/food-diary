$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("llm-wiki-query-cache-" + [guid]::NewGuid().ToString('N'))
try {
    $null = New-Item -ItemType Directory -Path $tempRoot -Force
    & git -C $tempRoot init --quiet
    & git -C $tempRoot config user.email 'wiki-cache@example.invalid'
    & git -C $tempRoot config user.name 'Wiki Cache Test'
    [IO.File]::WriteAllText((Join-Path $tempRoot 'source.txt'), 'one', [Text.UTF8Encoding]::new($false))
    & git -C $tempRoot add source.txt
    & git -C $tempRoot commit --quiet -m initial
    if ($LASTEXITCODE -ne 0) { throw 'Unable to create query-cache test repository.' }

    $arguments = @{ Intent = 'same request'; Paths = @('source.txt') }
    $first = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace test -Arguments $arguments
    if (Read-LlmWikiQueryCache -Entry $first) { throw 'A cache miss returned content.' }
    Write-LlmWikiQueryCache -Entry $first -Content '{"value":1}'
    Write-LlmWikiQueryCache -Entry $first -Content '{"value":1}'
    if ((Read-LlmWikiQueryCache -Entry $first) -cne '{"value":1}') { throw 'Recorded query cache content was not reused.' }

    [IO.File]::WriteAllText((Join-Path $tempRoot 'source.txt'), 'two', [Text.UTF8Encoding]::new($false))
    $changed = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace test -Arguments $arguments
    if ($changed.fingerprint -ceq $first.fingerprint) { throw 'A source edit did not invalidate the query cache.' }
    $differentArguments = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace test -Arguments @{ Intent = 'different request' }
    if ($differentArguments.fingerprint -ceq $changed.fingerprint) { throw 'Changed arguments did not invalidate the query cache.' }
    Write-Host 'LLM Wiki query-cache smoke passed: exact reuse and source/argument invalidation work.'
} finally {
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
