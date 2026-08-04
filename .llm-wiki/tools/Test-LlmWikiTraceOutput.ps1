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

$backendCompact = @(& $backendTraceScript -Query 'StartPremiumTrial' -Compact 6>&1 | ForEach-Object { $_.ToString() })
$backendFull = @(& $backendTraceScript -Query 'StartPremiumTrial' 6>&1 | ForEach-Object { $_.ToString() })
if ($backendCompact -notcontains '  Compact trace: use -FullTrace for every match and consumer.') { throw 'Backend compact trace omitted its expansion hint.' }
if ($backendCompact.Count -gt ($backendFull.Count + 1)) { throw 'Backend compact trace expanded output beyond its one-line hint.' }

Write-Host 'LLM Wiki trace-output smoke passed: compact text is bounded and full trace remains available.'
