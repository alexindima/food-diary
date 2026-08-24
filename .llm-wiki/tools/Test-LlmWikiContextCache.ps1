[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')

$query = "context-cache-smoke-$([guid]::NewGuid().ToString('N'))"
$arguments = @{
    Module = 'Users'
    Query = $query
    ScopePath = @()
    ChangeType = 'Any'
    CompiledIndexSource = 'Sqlite'
    Limit = 3
}
$entry = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repositoryRoot -Namespace 'context' -Arguments $arguments `
    -RelevantPath @('FoodDiary.Application/Users', 'FoodDiary.Application.Users') -DependencyPath @(
    '.llm-wiki/generated/frontend-index.json'
    '.artifacts/llm-wiki/code-graph/code-graph.fingerprint'
)
if (Read-LlmWikiQueryCache -Entry $entry) { throw 'Unique context-cache smoke unexpectedly started with a cache hit.' }

$tool = Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1'
$firstStopwatch = [Diagnostics.Stopwatch]::StartNew()
$first = & $tool -Module Users -Query $query -Limit 3 -Format Json
$firstStopwatch.Stop()
if (-not (Test-Path -LiteralPath $entry.path -PathType Leaf)) { throw 'Context discovery did not persist its immutable query result.' }

$secondStopwatch = [Diagnostics.Stopwatch]::StartNew()
$second = & $tool -Module Users -Query $query -Limit 3 -Format Json
$secondStopwatch.Stop()
if ([string]$first -cne [string]$second) { throw 'Cached context discovery changed the JSON result.' }
if ($secondStopwatch.Elapsed.TotalMilliseconds -ge $firstStopwatch.Elapsed.TotalMilliseconds) {
    throw "Cached context discovery was not faster: cold=$([Math]::Round($firstStopwatch.Elapsed.TotalMilliseconds))ms, warm=$([Math]::Round($secondStopwatch.Elapsed.TotalMilliseconds))ms."
}
$coldSlaMilliseconds = 10000
$warmSlaMilliseconds = 2000
if ($firstStopwatch.Elapsed.TotalMilliseconds -ge $coldSlaMilliseconds -or
    $secondStopwatch.Elapsed.TotalMilliseconds -ge $warmSlaMilliseconds) {
    throw "Context discovery exceeded its SLA: cold=$([Math]::Round($firstStopwatch.Elapsed.TotalMilliseconds))ms (target <$coldSlaMilliseconds ms), warm=$([Math]::Round($secondStopwatch.Elapsed.TotalMilliseconds))ms (target <$warmSlaMilliseconds ms)."
}

Write-Host "LLM Wiki context-cache smoke passed: cold=$([Math]::Round($firstStopwatch.Elapsed.TotalMilliseconds))ms (<$coldSlaMilliseconds ms), warm=$([Math]::Round($secondStopwatch.Elapsed.TotalMilliseconds))ms (<$warmSlaMilliseconds ms)."
