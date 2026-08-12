[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$result = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Users -Format Json | ConvertFrom-Json
if ($result.contractReadiness.aggregateBlockers -ne 0) { throw 'Current IUserContextService should have no aggregate blockers.' }
if (-not $result.moduleReadiness.ready) { throw "Users module should be extraction-ready after external aggregate and mutation consumers are removed: $($result.moduleReadiness.blockers -join '; ')" }
if (@($result.moduleReadiness.leakingContracts) -contains 'IUserDirectoryService') { throw 'Removed IUserDirectoryService must not remain in extraction readiness.' }
if (@($result.moduleReadiness.leakingContracts) -notcontains 'IUserLookupRepository') { throw 'Boundary scan missed an inherited transitive wrapper.' }
if ($result.categories.transitiveWrapper -lt 1) { throw 'Inherited aggregate wrappers must be categorized separately.' }
if (@($result.leaks | Where-Object contract -eq 'IUserDirectoryService').Count -ne 0) { throw 'Removed IUserDirectoryService must have no consumers.' }
if (@($result.moduleReadiness.blockers).Count -ne 0) { throw 'Extraction-ready module must have no blockers.' }
if ($result.contractReadiness.mutationBlockers -ne 0) { throw 'Owner-internal IUserContextService mutations must not block extraction.' }
if (-not $result.contractReadiness.aggregateReady) { throw 'Contract and module readiness were not separated.' }
$dietologist = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Dietologist -Format Json | ConvertFrom-Json
if ($dietologist.contractReadiness.mutationBlockers -ne 0) { throw 'Users-owned mutation consumers must not block unrelated module extraction.' }
if (-not $dietologist.moduleReadiness.ready) { throw "Dietologist should be extraction-ready after cross-feature dependencies are removed: $($dietologist.moduleReadiness.blockers -join '; ')" }
Write-Host "LLM Wiki extraction readiness regression passed: $($result.moduleReadiness.aggregateLeakPaths) production leak path(s)."
