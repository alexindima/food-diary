[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$frontendTraceScript = Join-Path $PSScriptRoot 'Find-LlmWikiFrontendTrace.ps1'
$backendTraceScript = Join-Path $PSScriptRoot 'Find-LlmWikiTrace.ps1'
$facadeText = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../wiki.ps1') -Raw
if (-not $facadeText.Contains("if (-not `$FullTrace -and `$Format -eq 'Text')") -or
    -not $facadeText.Contains('$traceArguments.Compact = $true')) {
    throw 'Wiki trace facade does not default text output to compact mode.'
}

$frontendCompact = @(& $frontendTraceScript -Query 'FdUiSelectComponent' -Compact 6>&1 | ForEach-Object { $_.ToString() })
$frontendFull = @(& $frontendTraceScript -Query 'FdUiSelectComponent' 6>&1 | ForEach-Object { $_.ToString() })
if ($frontendCompact -notcontains '  Compact trace: use -FullTrace for every match and consumer.') { throw 'Frontend compact trace omitted its expansion hint.' }
if ($frontendCompact.Count -ge $frontendFull.Count) { throw 'Frontend compact trace did not reduce output.' }

$invalidRoot = Join-Path ([IO.Path]::GetTempPath()) "llm-wiki-trace-schema-$PID"
$null = New-Item -ItemType Directory -Path $invalidRoot -Force
try {
    [IO.File]::WriteAllText((Join-Path $invalidRoot 'frontend-index.json'), '{"schemaVersion":0,"symbols":[{"name":"Old"}],"routes":[]}', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $invalidRoot 'frontend-contract-index.json'), '{"schemaVersion":0,"components":[],"apiCalls":[],"consumerEdges":[]}', [Text.UTF8Encoding]::new($false))
    $schemaError = $null
    try { & $frontendTraceScript -Query Old -IndexRoot $invalidRoot 2>&1 | Out-Null } catch { $schemaError = $_.Exception.Message }
    if ($schemaError -notmatch 'unsupported schemaVersion.*expected 1' -or $schemaError -notmatch 'update -AffectedOnly') {
        throw "Frontend trace did not provide an actionable stale-schema diagnostic: $schemaError"
    }
} finally { Remove-Item -LiteralPath $invalidRoot -Recurse -Force -ErrorAction SilentlyContinue }

$backendCompact = @(& $backendTraceScript -Query 'StartPremiumTrial' -Compact 6>&1 | ForEach-Object { $_.ToString() })
$backendFull = @(& $backendTraceScript -Query 'StartPremiumTrial' 6>&1 | ForEach-Object { $_.ToString() })
if ($backendCompact -notcontains '  Compact trace: use -FullTrace for every match and consumer.') { throw 'Backend compact trace omitted its expansion hint.' }
if ($backendCompact.Count -gt ($backendFull.Count + 1)) { throw 'Backend compact trace expanded output beyond its one-line hint.' }
$noMatchQuery = 'zzqnosuchsymbol92841'
$noMatch = @(& $backendTraceScript -Query $noMatchQuery -Compact 6>&1 | ForEach-Object { $_.ToString() })
if ($LASTEXITCODE -ne 0 -or $noMatch -notcontains "No request handlers matched '$noMatchQuery'.") {
    throw 'Backend semantic no-match must return an empty successful fragment instead of failing the facade.'
}

$broadBackendQuery = 'Recipes commands queries dependency injection repositories external module dependencies'
$facadeOutput = @(& (Join-Path $PSScriptRoot '../wiki.ps1') trace -Fast -Module Recipes -Query $broadBackendQuery 6>&1 | ForEach-Object { $_.ToString() })
if ($LASTEXITCODE -ne 0 -or $facadeOutput -match 'PropertyNotFoundStrict|The property .path. cannot be found') {
    throw "Broad backend semantic fallback failed through the facade: $($facadeOutput -join [Environment]::NewLine)"
}
if (-not ($facadeOutput -match 'falling back to semantic trace')) {
    throw 'Broad backend regression did not exercise graph miss to semantic trace fallback.'
}
if (-not ($facadeOutput -match 'classified as backend') -or -not ($facadeOutput -match 'Fast graph research') -or $facadeOutput -match 'FavoriteRecipeService') {
    throw 'Broad backend semantic fallback was incorrectly captured by the optional frontend probe.'
}

$namespaceFacadeOutput = @(& (Join-Path $PSScriptRoot '../wiki.ps1') trace -Fast -Query 'FoodDiary.Presentation.Api.Features.Auth' 6>&1 | ForEach-Object { $_.ToString() })
$namespaceFacadeText = $namespaceFacadeOutput -join [Environment]::NewLine
if ($LASTEXITCODE -ne 0 -or $namespaceFacadeText -notmatch 'namespace filter:.*ControllerConventionsTests.cs' -or $namespaceFacadeText -match 'falling back to semantic trace') {
    throw "Qualified namespace trace did not stay on the graph route: $($namespaceFacadeOutput -join [Environment]::NewLine)"
}

Write-Host 'LLM Wiki trace-output smoke passed: compact text is bounded and full trace remains available.'
