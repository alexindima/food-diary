[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for context-cache smoke.' }
. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')

$query = "context-cache-smoke-$([guid]::NewGuid().ToString('N'))"
$arguments = @{
    Module = 'Users'
    Query = $query
    ScopePath = @()
    ChangeType = 'Any'
    CompiledIndexSource = 'Json'
    Limit = 3
}
$entry = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repositoryRoot -Namespace 'context' -Arguments $arguments `
    -RelevantPath @('Modules/Users/Application') -DependencyPath @(
    '.llm-wiki/generated/repository-catalog.json'
    '.llm-wiki/generated/csharp-symbol-index.json'
    '.llm-wiki/generated/frontend-index.json'
    'docs/architecture/backend-modules.json'
)
if (Read-LlmWikiQueryCache -Entry $entry) { throw 'Unique context-cache smoke unexpectedly started with a cache hit.' }

$tool = Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1'
$firstStopwatch = [Diagnostics.Stopwatch]::StartNew()
$first = & $tool -Module Users -Query $query -CompiledIndexSource Json -Limit 3 -Format Json
$firstStopwatch.Stop()
if (-not (Test-Path -LiteralPath $entry.path -PathType Leaf)) { throw 'Context discovery did not persist its immutable query result.' }

$secondStopwatch = [Diagnostics.Stopwatch]::StartNew()
$second = & $tool -Module Users -Query $query -CompiledIndexSource Json -Limit 3 -Format Json
$secondStopwatch.Stop()
if ([string]$first -cne [string]$second) { throw 'Cached context discovery changed the JSON result.' }
if ($secondStopwatch.Elapsed.TotalMilliseconds -ge $firstStopwatch.Elapsed.TotalMilliseconds) {
    throw "Cached context discovery was not faster: cold=$([Math]::Round($firstStopwatch.Elapsed.TotalMilliseconds))ms, warm=$([Math]::Round($secondStopwatch.Elapsed.TotalMilliseconds))ms."
}
$coldSlaMilliseconds = 15000
$warmSlaMilliseconds = 2000
if ($firstStopwatch.Elapsed.TotalMilliseconds -ge $coldSlaMilliseconds -or
    $secondStopwatch.Elapsed.TotalMilliseconds -ge $warmSlaMilliseconds) {
    throw "Context discovery exceeded its SLA: cold=$([Math]::Round($firstStopwatch.Elapsed.TotalMilliseconds))ms (target <$coldSlaMilliseconds ms), warm=$([Math]::Round($secondStopwatch.Elapsed.TotalMilliseconds))ms (target <$warmSlaMilliseconds ms)."
}

Write-Host "LLM Wiki JSON context-cache smoke passed: cold=$([Math]::Round($firstStopwatch.Elapsed.TotalMilliseconds))ms (<$coldSlaMilliseconds ms), warm=$([Math]::Round($secondStopwatch.Elapsed.TotalMilliseconds))ms (<$warmSlaMilliseconds ms)."

$compactArguments = @{ Query = 'RefreshTokenCommandHandlerTests'; CompiledIndexSource = 'Sqlite'; Limit = 3; Format = 'Json'; Compact = $true }
$uncached = & $tool @compactArguments -SkipQueryCache | ConvertFrom-Json
$null = & $tool @compactArguments
$warmJson = & $tool @compactArguments
$warm = $warmJson | ConvertFrom-Json
if (-not $warm.cache.hit -or -not $warm.cache.storedTimings) { throw 'SQLite context cache did not report reuse and stored timings.' }
if ($warmJson.Length -gt $warm.output.characterBudget -or $warm.candidates.Count -gt 3) { throw 'Compact context exceeded its output budget.' }
if ($warm.candidates[0].path -notlike '*/RefreshTokenCommandHandlerTests.cs' -or $warm.confidence -ne 'high') { throw 'Compact context lost exact test identity.' }
if (($warm.candidates.path -join "`n") -cne ($uncached.candidates.path -join "`n")) { throw 'SQLite context cache changed candidate selection.' }
if (@($warm.candidates.path | Sort-Object -Unique).Count -ne $warm.candidates.Count) { throw 'Compact context repeated a candidate.' }
Write-Host 'LLM Wiki SQLite compact context-cache smoke passed: exact test identity, output budget, cache parity and explicit stored timings.'
