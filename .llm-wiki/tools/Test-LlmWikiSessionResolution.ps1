$ErrorActionPreference = 'Stop'
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("llm-wiki-session-" + [guid]::NewGuid().ToString('N'))
$savedHints = @{}
try {
    $null = New-Item -ItemType Directory -Path $tempRoot -Force
    & git -C $tempRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unable to create temporary Git repository.' }
    $resolver = Join-Path $PSScriptRoot 'Resolve-LlmWikiSession.ps1'
    $first = & $resolver -RepositoryRoot $tempRoot -SessionId 'future-codex-api:a' -Create -Format Object
    $again = & $resolver -RepositoryRoot $tempRoot -SessionId 'future-codex-api:a' -Create -Format Object
    if ($first.id -cne $again.id) { throw 'A stable external hint did not resolve the same internal session.' }
    $registryPath = Join-Path $tempRoot '.git/llm-wiki/sessions/registry.json'
    $registryBefore = [IO.File]::ReadAllText($registryPath)
    $readOnly = & $resolver -RepositoryRoot $tempRoot -SessionId 'future-codex-api:a' -ReadOnly -Format Object
    $registryAfter = [IO.File]::ReadAllText($registryPath)
    if (-not $readOnly.readOnly -or $registryBefore -cne $registryAfter) {
        throw 'Read-only session resolution modified the session registry.'
    }
    foreach ($name in @('CODEX_THREAD_ID', 'CODEX_TASK_ID', 'CODEX_SESSION_ID')) {
        $savedHints[$name] = [Environment]::GetEnvironmentVariable($name)
        [Environment]::SetEnvironmentVariable($name, $null)
    }
    $implicit = & $resolver -RepositoryRoot $tempRoot -Format Object
    if ($implicit.id -cne $first.id) { throw 'A single active internal session was not selected without a Codex environment variable.' }
    $second = & $resolver -RepositoryRoot $tempRoot -SessionId 'future-codex-api:b' -Create -Format Object
    if ($second.id -ceq $first.id) { throw 'Distinct external hints resolved the same internal session.' }
    $threw = $false
    try { & $resolver -RepositoryRoot $tempRoot -Format Object | Out-Null } catch { $threw = $_.Exception.Message -match 'Multiple active' }
    if (-not $threw) { throw 'Ambiguous concurrent sessions were silently guessed.' }
    foreach ($name in $savedHints.Keys) { [Environment]::SetEnvironmentVariable($name, $savedHints[$name]) }
    Write-Host 'LLM Wiki session resolution smoke passed: internal UUID, read-only lookup, future external hints, single-session fallback, and ambiguity guard work.'
} finally {
    if ($savedHints) { foreach ($name in $savedHints.Keys) { [Environment]::SetEnvironmentVariable($name, $savedHints[$name]) } }
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
