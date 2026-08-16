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
$multiSymbolTrace = @(& $backendTraceScript `
    -Query 'UpdateMenstrualEpisode, ConfirmPeriodStart' `
    -Format Json | ConvertFrom-Json)
if ($multiSymbolTrace.Count -eq 1 -and $multiSymbolTrace[0] -is [Array]) {
    $multiSymbolTrace = @($multiSymbolTrace[0])
}
if ($multiSymbolTrace.Count -lt 2) { throw 'Multi-symbol backend trace collapsed independent symbol queries.' }
foreach ($expectedQuery in @('UpdateMenstrualEpisode', 'ConfirmPeriodStart')) {
    $match = @($multiSymbolTrace | Where-Object { @($_.match.matchedQueries) -contains $expectedQuery })
    if ($match.Count -eq 0 -or @($match[0].impact.symbols).Count -lt 2 -or @($match[0].impact.consumers).Count -eq 0) {
        throw "Multi-symbol backend trace omitted direct symbols or consumers for '$expectedQuery'."
    }
}
$noMatchQuery = 'zzqnosuchsymbol92841'
$noMatch = @(& $backendTraceScript -Query $noMatchQuery -Compact 6>&1 | ForEach-Object { $_.ToString() })
if ($LASTEXITCODE -ne 0 -or $noMatch -notcontains "No request handlers matched '$noMatchQuery'.") {
    throw 'Backend semantic no-match must return an empty successful fragment instead of failing the facade.'
}

$broadBackendQuery = 'Recipes handlers storage consumers boundaries'
$facadeOutput = @(& (Join-Path $PSScriptRoot '../wiki.ps1') trace -Fast -Module Recipes -Query $broadBackendQuery 6>&1 | ForEach-Object { $_.ToString() })
if ($LASTEXITCODE -ne 0 -or $facadeOutput -match 'PropertyNotFoundStrict|The property .path. cannot be found') {
    throw "Broad backend semantic fallback failed through the facade: $($facadeOutput -join [Environment]::NewLine)"
}
if (-not ($facadeOutput -match 'candidate \[') -or $facadeOutput -match 'FavoriteRecipeService|AdminMailInboxComponent') {
    throw 'Broad backend trace did not return ranked backend graph candidates or was captured by the optional frontend probe.'
}

$mailInboxTrace = & (Join-Path $PSScriptRoot '../wiki.ps1') trace `
    -Query 'MailInbox SMTP receive persistence readiness telemetry' `
    -Layer Backend `
    -Module MailInbox `
    -PathPrefix 'MailInbox/' `
    -Limit 8 `
    -Format Json | ConvertFrom-Json
$rankedCandidates = @($mailInboxTrace.candidates)
$exactSymbols = @($mailInboxTrace.symbols)
$firstBackendMatch = if ($rankedCandidates.Count -gt 0) { $rankedCandidates[0] } elseif ($exactSymbols.Count -gt 0) { $exactSymbols[0] } else { $null }
if ($null -eq $firstBackendMatch -or $firstBackendMatch.path -notmatch '^MailInbox/' -or $firstBackendMatch.path -match '/tests?/' -or $firstBackendMatch.path -match 'Web\.Client') {
    throw 'Backend trace filters did not rank a production MailInbox candidate first.'
}
if ($rankedCandidates.Count -gt 0 -and (-not $rankedCandidates[0].PSObject.Properties['confidence'] -or @($rankedCandidates[0].reasons).Count -eq 0)) {
    throw 'Ranked trace candidates omitted confidence or ranking explanations.'
}

$namespaceFacadeOutput = @(& (Join-Path $PSScriptRoot '../wiki.ps1') trace -Fast -Query 'FoodDiary.Presentation.Api.Features.Auth' 6>&1 | ForEach-Object { $_.ToString() })
$namespaceFacadeText = $namespaceFacadeOutput -join [Environment]::NewLine
if ($LASTEXITCODE -ne 0 -or $namespaceFacadeText -notmatch 'namespace filter:.*ControllerConventionsTests.cs' -or $namespaceFacadeText -match 'falling back to semantic trace') {
    throw "Qualified namespace trace did not stay on the graph route: $($namespaceFacadeOutput -join [Environment]::NewLine)"
}

Write-Host 'LLM Wiki trace-output smoke passed: compact text is bounded and full trace remains available.'
