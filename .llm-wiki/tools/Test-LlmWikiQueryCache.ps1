$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("llm-wiki-query-cache-" + [guid]::NewGuid().ToString('N'))
try {
    $null = New-Item -ItemType Directory -Path $tempRoot -Force
    & git -C $tempRoot init --quiet
    & git -C $tempRoot config user.email 'wiki-cache@example.invalid'
    & git -C $tempRoot config user.name 'Wiki Cache Test'
    [IO.File]::WriteAllText((Join-Path $tempRoot 'source.txt'), 'one', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $tempRoot 'unrelated.txt'), 'one', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $tempRoot 'dependency.txt'), 'one', [Text.UTF8Encoding]::new($false))
    & git -C $tempRoot add source.txt unrelated.txt dependency.txt
    & git -C $tempRoot commit --quiet -m initial
    if ($LASTEXITCODE -ne 0) { throw 'Unable to create query-cache test repository.' }

    $arguments = @{ Intent = 'same request'; Paths = @('source.txt') }
    $first = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace test -Arguments $arguments -RelevantPath 'source.txt' -DependencyPath 'dependency.txt'
    if (Read-LlmWikiQueryCache -Entry $first) { throw 'A cache miss returned content.' }
    Write-LlmWikiQueryCache -Entry $first -Content '{"value":1}'
    Write-LlmWikiQueryCache -Entry $first -Content '{"value":1}'
    if ((Read-LlmWikiQueryCache -Entry $first) -cne '{"value":1}') { throw 'Recorded query cache content was not reused.' }
    [IO.File]::WriteAllText($first.path, '{broken', [Text.UTF8Encoding]::new($false))
    if (Read-LlmWikiQueryCache -Entry $first) { throw 'A corrupt query-cache entry was returned.' }
    if (Test-Path -LiteralPath $first.path) { throw 'A corrupt query-cache entry was not removed.' }
    Write-LlmWikiQueryCache -Entry $first -Content '{"value":1}'
    $alreadyRemovedPath = Join-Path (Split-Path -Parent $first.path) 'already-removed.json'
    [IO.File]::WriteAllText($alreadyRemovedPath, '{}', [Text.UTF8Encoding]::new($false))
    Remove-LlmWikiQueryCacheFileIfPresent -Path $alreadyRemovedPath
    Remove-LlmWikiQueryCacheFileIfPresent -Path $alreadyRemovedPath
    if (Test-Path -LiteralPath $alreadyRemovedPath) { throw 'Idempotent query-cache removal retained a stale entry.' }

    [IO.File]::WriteAllText((Join-Path $tempRoot 'unrelated.txt'), 'two', [Text.UTF8Encoding]::new($false))
    $unrelated = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace test -Arguments $arguments -RelevantPath 'source.txt' -DependencyPath 'dependency.txt'
    if ($unrelated.fingerprint -cne $first.fingerprint) { throw 'An unrelated workspace edit invalidated a scoped query cache entry.' }
    if ((Read-LlmWikiQueryCache -Entry $unrelated) -cne '{"value":1}') { throw 'Scoped cache did not survive an unrelated workspace edit.' }

    [IO.File]::WriteAllText((Join-Path $tempRoot 'source.txt'), 'two', [Text.UTF8Encoding]::new($false))
    $changed = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace test -Arguments $arguments -RelevantPath 'source.txt' -DependencyPath 'dependency.txt'
    if ($changed.fingerprint -ceq $first.fingerprint) { throw 'A source edit did not invalidate the query cache.' }
    if ($changed.missReason -cne 'relevant workspace paths changed') { throw "Scoped cache reported the wrong source miss reason: $($changed.missReason)" }
    Write-LlmWikiQueryCache -Entry $changed -Content '{"value":2}'
    [IO.File]::WriteAllText((Join-Path $tempRoot 'dependency.txt'), 'two', [Text.UTF8Encoding]::new($false))
    $dependencyChanged = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace test -Arguments $arguments -RelevantPath 'source.txt' -DependencyPath 'dependency.txt'
    if ($dependencyChanged.fingerprint -ceq $changed.fingerprint) { throw 'A dependent index edit did not invalidate the query cache.' }
    if ($dependencyChanged.missReason -cne 'dependent Wiki indexes changed') { throw "Scoped cache reported the wrong dependency miss reason: $($dependencyChanged.missReason)" }
    $differentArguments = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace test -Arguments @{ Intent = 'different request' }
    if ($differentArguments.fingerprint -ceq $changed.fingerprint) { throw 'Changed arguments did not invalidate the query cache.' }

    function Get-VerifiedTestWorkspace {
        $snapshot = Get-LlmWikiChangeSetSnapshot -RepositoryRoot $tempRoot
        [pscustomobject]@{
            repositoryRoot = $tempRoot
            head = $snapshot.head
            fingerprint = $snapshot.fingerprint
            changedPathCount = @($snapshot.changedPaths).Count
            fresh = $true
        }
    }
    $verifiedWorkspace = Get-VerifiedTestWorkspace
    $ordinary = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace verified-test -Arguments $arguments -DependencyPath 'dependency.txt'
    $originalSnapshotFunction = (Get-Item Function:\Get-LlmWikiChangeSetSnapshot).ScriptBlock
    try {
        function Get-LlmWikiChangeSetSnapshot { throw 'A verified request repeated its workspace scan.' }
        $verified = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace verified-test -Arguments $arguments -DependencyPath 'dependency.txt' -VerifiedWorkspace $verifiedWorkspace
        if ($verified.fingerprint -cne $ordinary.fingerprint -or $verified.workspacePathCount -ne $ordinary.workspacePathCount) { throw 'Request-local reuse changed the cache identity or workspace diagnostics.' }
        Write-LlmWikiQueryCache -Entry $verified -Content '{"value":3}'
        if ((Read-LlmWikiQueryCache -Entry $verified) -cne '{"value":3}') { throw 'Request-local workspace reuse lost a cached result.' }
    } finally {
        Set-Item Function:\Get-LlmWikiChangeSetSnapshot -Value $originalSnapshotFunction
    }
    foreach ($scenario in @('different-repository', 'scoped', 'stale', 'string-freshness', 'invalid-fingerprint', 'negative-count', 'missing-head')) {
        $candidate = $verifiedWorkspace | Select-Object *
        $candidateArguments = @{ RepositoryRoot=$tempRoot; Namespace='verified-test'; Arguments=$arguments; VerifiedWorkspace=$candidate }
        switch ($scenario) {
            'different-repository' { $candidate.repositoryRoot = [IO.Path]::GetTempPath() }
            'scoped' { $candidateArguments.RelevantPath = 'source.txt' }
            'stale' { $candidate.fresh = $false }
            'string-freshness' { $candidate.fresh = 'false' }
            'invalid-fingerprint' { $candidate.fingerprint = 'invalid' }
            'negative-count' { $candidate.changedPathCount = -1 }
            'missing-head' { $candidate.PSObject.Properties.Remove('head') }
        }
        $rejected = $false
        try { $null = Get-LlmWikiQueryCacheEntry @candidateArguments } catch { $rejected = $true }
        if (-not $rejected) { throw "Unsafe request-local workspace was accepted: $scenario" }
    }
    $sourcePath = Join-Path $tempRoot 'source.txt'
    $sourceTimestamp = [IO.File]::GetLastWriteTimeUtc($sourcePath)
    [IO.File]::WriteAllText($sourcePath, 'six', [Text.UTF8Encoding]::new($false))
    [IO.File]::SetLastWriteTimeUtc($sourcePath, $sourceTimestamp)
    $nextRequest = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace verified-test -Arguments $arguments -DependencyPath 'dependency.txt' -VerifiedWorkspace (Get-VerifiedTestWorkspace)
    if ($nextRequest.fingerprint -ceq $verified.fingerprint -or (Read-LlmWikiQueryCache -Entry $nextRequest)) { throw 'A new request reused context after a same-size/same-time source edit.' }
    [IO.File]::WriteAllText((Join-Path $tempRoot 'dependency.txt'), 'three', [Text.UTF8Encoding]::new($false))
    $nextDependency = Get-LlmWikiQueryCacheEntry -RepositoryRoot $tempRoot -Namespace verified-test -Arguments $arguments -DependencyPath 'dependency.txt' -VerifiedWorkspace (Get-VerifiedTestWorkspace)
    if ($nextDependency.dependencyFingerprint -ceq $nextRequest.dependencyFingerprint) { throw 'Workspace reuse skipped dependency validation.' }
    Write-Host 'LLM Wiki query-cache smoke passed: exact reuse, idempotent stale removal, scoped invalidation, dependency lineage, and miss diagnostics work.'
    Write-Host 'Request-local workspace reuse passed: one scan, repository/scope/staleness guards, repeated content edits and independent dependency hashing.'
} finally {
    $resolvedTempRoot = [IO.Path]::GetFullPath($tempRoot)
    $temporaryParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTempRoot.StartsWith($temporaryParent, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedTempRoot) -notmatch '^llm-wiki-query-cache-[a-f0-9]{32}$') { throw 'Refusing to remove an unexpected query-cache test directory.' }
    if (Test-Path -LiteralPath $resolvedTempRoot) { Remove-Item -LiteralPath $resolvedTempRoot -Recurse -Force }
}
