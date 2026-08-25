[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$null = & $manager -Action build -Format Json
if (-not $? -or ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0)) {
    throw 'Unable to build the code graph for runtime/architecture SQLite parity.'
}

function Assert-JsonParity([string]$Name, [object]$JsonValue, [object]$SqliteValue) {
    $expected = $JsonValue | ConvertTo-Json -Depth 30 -Compress
    $actual = $SqliteValue | ConvertTo-Json -Depth 30 -Compress
    if ($actual -cne $expected) {
        throw "$Name SQLite output does not match the explicit JSON parity baseline."
    }
}

$runtimeTool = Join-Path $PSScriptRoot 'Find-LlmWikiRuntimeTopology.ps1'
foreach ($query in @('', 'MailRelay')) {
    $json = & $runtimeTool -Query $query -Limit 30 -CompiledIndexSource Json -Format Json | ConvertFrom-Json
    $sqlite = & $runtimeTool -Query $query -Limit 30 -CompiledIndexSource Sqlite -Format Json | ConvertFrom-Json
    Assert-JsonParity "runtime-topology query '$query'" $json $sqlite
}
$runtimeDiagnostics = & $runtimeTool -Query 'MailRelay' -CompiledIndexSource Sqlite -IncludeDiagnostics -Format Json | ConvertFrom-Json
if ([string]$runtimeDiagnostics._diagnostics.source -ne 'sqlite-runtime-in-process' -or
    [double]$runtimeDiagnostics._diagnostics.sqlDurationMs -lt 0 -or
    [int64]$runtimeDiagnostics._diagnostics.sourceBytesVerified -le 0) {
    throw 'Runtime-topology SQLite diagnostics are incomplete.'
}

$architectureTool = Join-Path $PSScriptRoot 'Find-LlmWikiArchitectureHealth.ps1'
foreach ($view in @('all', 'drift', 'allowances', 'untracked', 'cycles', 'ambiguous', 'dead-candidates', 'spec-gaps', 'test-gaps', 'debt')) {
    foreach ($query in @('', 'component')) {
        $json = & $architectureTool -View $view -Query $query -Limit 30 -CompiledIndexSource Json -Format Json | ConvertFrom-Json
        $sqlite = & $architectureTool -View $view -Query $query -Limit 30 -Format Json | ConvertFrom-Json
        Assert-JsonParity "architecture-health view '$view' query '$query'" $json $sqlite
    }
}
$architectureDiagnostics = & $architectureTool -View spec-gaps -Query component -IncludeDiagnostics -Format Json | ConvertFrom-Json
if ([string]$architectureDiagnostics._diagnostics.source -ne 'sqlite-architecture-health-in-process' -or
    [double]$architectureDiagnostics._diagnostics.sqlDurationMs -lt 0 -or
    [int64]$architectureDiagnostics._diagnostics.sourceBytesVerified -le 0) {
    throw 'Architecture-health SQLite diagnostics are incomplete.'
}

Write-Host 'LLM Wiki runtime/architecture SQLite parity passed: every standalone view matches the explicit JSON baseline.'
