[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for context-cache smoke.' }
. (Join-Path $PSScriptRoot 'LlmWikiQueryCache.ps1')

$query = "context-cache-smoke-$([guid]::NewGuid().ToString('N'))"
$tool = Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1'
$firstStopwatch = [Diagnostics.Stopwatch]::StartNew()
$first = & $tool -Module Users -Query $query -Limit 3 -Format Json | ConvertFrom-Json
$firstStopwatch.Stop()
$secondStopwatch = [Diagnostics.Stopwatch]::StartNew()
$second = & $tool -Module Users -Query $query -Limit 3 -Format Json | ConvertFrom-Json
$secondStopwatch.Stop()
if (-not $second.cache.hit -or -not $second.cache.storedTimings) { throw 'SQLite query cache did not reuse its immutable result.' }
foreach ($property in @('candidates','tests','confidence','conclusive','ambiguityReason')) {
    if (($first.$property | ConvertTo-Json -Depth 12 -Compress) -cne ($second.$property | ConvertTo-Json -Depth 12 -Compress)) { throw "Cached SQLite context changed $property." }
}
$coldSlaMilliseconds = 15000
$warmSlaMilliseconds = 2000
if ($firstStopwatch.Elapsed.TotalMilliseconds -ge $coldSlaMilliseconds -or
    $secondStopwatch.Elapsed.TotalMilliseconds -ge $warmSlaMilliseconds -or
    $secondStopwatch.Elapsed.TotalMilliseconds -ge $firstStopwatch.Elapsed.TotalMilliseconds) {
    throw "SQLite context-cache SLA failed: cold=$($firstStopwatch.Elapsed.TotalMilliseconds)ms (<$coldSlaMilliseconds), warm=$($secondStopwatch.Elapsed.TotalMilliseconds)ms (<$warmSlaMilliseconds)."
}
Write-Host "SQLite context-cache passed: cold=$([Math]::Round($firstStopwatch.Elapsed.TotalMilliseconds))ms, warm=$([Math]::Round($secondStopwatch.Elapsed.TotalMilliseconds))ms."

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

$scoped = & $tool -Query RefreshTokenCommandHandler -ScopePath @('Modules/Identity', 'Modules/Meals') -Limit 2 -Compact -Format Json -SkipQueryCache | ConvertFrom-Json
foreach ($scope in @('Modules/Identity', 'Modules/Meals')) {
    if (@($scoped.candidates | Where-Object { $_.path.StartsWith("$scope/") }).Count -eq 0) { throw "Compact context lost requested scope $scope." }
}
if (@($scoped.output.missingScopes).Count -ne 0) { throw 'Compact context reports missing scopes despite complete coverage.' }
$insufficient = & $tool -Query RefreshTokenCommandHandler -ScopePath @('Modules/Identity', 'Modules/Meals') -Limit 1 -Compact -Format Json -SkipQueryCache | ConvertFrom-Json
if (@($insufficient.output.missingScopes).Count -eq 0) { throw 'Compact context hid scope loss when the candidate budget was insufficient.' }
