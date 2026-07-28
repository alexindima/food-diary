[CmdletBinding()]
param(
    [string]$Query,
    [ValidateSet('all', 'drift', 'allowances', 'untracked', 'cycles', 'ambiguous', 'dead-candidates', 'spec-gaps', 'test-gaps', 'debt')]
    [string]$View = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)
$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$index = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/architecture-health-index.json') -Raw | ConvertFrom-Json
$groups = [ordered]@{}
if ($View -in @('all', 'drift')) { $groups.dependencyViolations = @($index.projectDependencyViolations) }
if ($View -eq 'allowances') { $groups.unusedAllowances = @($index.unusedProjectAllowances) }
if ($View -eq 'untracked') { $groups.untrackedProjects = @($index.untrackedProductionProjects) }
if ($View -eq 'cycles') { $groups.moduleCycleNodes = @($index.moduleCycleNodes) }
if ($View -eq 'ambiguous') { $groups.ambiguousContracts = @($index.ambiguousBackendContracts) }
if ($View -eq 'dead-candidates') {
    $groups.unconsumedBackendContracts = @($index.unconsumedBackendContracts)
    $groups.selectorUnreferencedComponents = @($index.selectorUnreferencedComponents)
}
if ($View -eq 'spec-gaps') { $groups.componentsWithoutSpecs = @($index.componentsWithoutDirectSpecs) }
if ($View -eq 'test-gaps') { $groups.criticalSymbolsWithoutTests = @($index.criticalSymbolsWithoutTestReferences) }
if ($View -eq 'debt') { $groups.debtMarkers = @($index.explicitDebtMarkers) }
foreach ($key in @($groups.Keys)) {
    if (-not [string]::IsNullOrWhiteSpace($Query)) {
        $groups[$key] = @($groups[$key] | Where-Object { ($_ | ConvertTo-Json -Depth 7 -Compress) -match [regex]::Escape($Query) })
    }
    $groups[$key] = @($groups[$key] | Select-Object -First $Limit)
}
if ($Format -eq 'Json') { [pscustomobject]$groups | ConvertTo-Json -Depth 10; exit 0 }
foreach ($key in $groups.Keys) {
    Write-Host "$key ($(@($groups[$key]).Count)):"
    foreach ($item in $groups[$key]) { Write-Host " - $(($item | ConvertTo-Json -Depth 7 -Compress))" }
    Write-Host ''
}
