[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$null = & $manager -Action build -Format Json
$runtimeTool = Join-Path $PSScriptRoot 'Find-LlmWikiRuntimeTopology.ps1'
foreach ($query in @('', 'MailRelay', 'zznosuchruntime92841')) {
    $result = & $runtimeTool -Query $query -Limit 30 -IncludeDiagnostics -Format Json | ConvertFrom-Json
    foreach ($group in @('composeServices','hostedServices','httpClients','webhooks','recurringJobRegistrations','networkPolicies')) {
        if (-not $result.PSObject.Properties[$group] -or @($result.$group).Count -gt 30) { throw "Missing or unbounded runtime group: $group" }
    }
    if (-not $result._freshness.verified -or $result._diagnostics.source -ne 'sqlite-runtime-in-process' -or
        $result._diagnostics.sqlDurationMs -lt 0 -or $result._diagnostics.sourceBytesVerified -le 0) {
        throw 'Runtime query lost exact freshness or SQLite diagnostics.'
    }
    if ($query -eq 'MailRelay' -and $result._selection.status -ne 'matched') { throw 'Runtime query lost MailRelay evidence.' }
    if ($query -like 'zznosuch*' -and $result._selection.returnedRecords -ne 0) { throw 'Unknown runtime query returned unrelated evidence.' }
}
$retry = & $runtimeTool -Query retry -Format Json | ConvertFrom-Json
if ($retry._selection.status -ne 'matched' -or $retry._selection.queryKind -ne 'behavioral-signal') { throw 'Runtime query lost retry behavior signals.' }
$idempotency = & $runtimeTool -Query idempotency -Format Json | ConvertFrom-Json
if ($idempotency._selection.status -eq 'abstained-empty-filter' -and $idempotency._selection.recommendation -notmatch 'research.*test-plan') {
    throw 'Unsupported behavior query omitted its source/test recovery recommendation.'
}
$views = [ordered]@{
    all = @('dependencyViolations','unusedAllowances','untrackedProjects','moduleCycleNodes','ambiguousContracts','unconsumedBackendContracts','selectorUnreferencedComponents','componentsWithoutSpecs','criticalSymbolsWithoutTests','debtMarkers')
    drift = @('dependencyViolations'); allowances = @('unusedAllowances'); untracked = @('untrackedProjects')
    cycles = @('moduleCycleNodes'); ambiguous = @('ambiguousContracts')
    'dead-candidates' = @('unconsumedBackendContracts','selectorUnreferencedComponents')
    'spec-gaps' = @('componentsWithoutSpecs'); 'test-gaps' = @('criticalSymbolsWithoutTests'); debt = @('debtMarkers')
}
foreach ($view in $views.Keys) {
    foreach ($query in @('', 'component', 'zznosucharchitecture92841')) {
        $result = & (Join-Path $PSScriptRoot 'Find-LlmWikiArchitectureHealth.ps1') -View $view -Query $query -Limit 30 -IncludeDiagnostics -Format Json | ConvertFrom-Json
        $groups = @($result.PSObject.Properties | Where-Object Name -ne '_diagnostics')
        if (($groups.Name -join ',') -cne ($views[$view] -join ',')) { throw "Architecture view '$view' returned incorrect groups." }
        foreach ($group in $groups) {
            if (@($group.Value).Count -gt 30 -or ($query -like 'zznosuch*' -and @($group.Value).Count -ne 0)) { throw "Incorrect bounds or no-match behavior in architecture view '$view'." }
        }
        if ($result._diagnostics.source -ne 'sqlite-architecture-health-in-process' -or
            $result._diagnostics.sqlDurationMs -lt 0 -or $result._diagnostics.sourceBytesVerified -le 0) { throw 'Architecture SQLite diagnostics are incomplete.' }
        if ($view -eq 'spec-gaps' -and -not $query -and @($result.componentsWithoutSpecs).Count -eq 0) { throw 'Architecture query lost component spec-gap evidence.' }
    }
}
Write-Host 'LLM Wiki runtime/architecture SQLite behavior passed: all views, bounded groups, freshness, behavior signals and abstention.'
