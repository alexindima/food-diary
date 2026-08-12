[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$result = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Users -Format Json | ConvertFrom-Json
if ($result.contractReadiness.aggregateBlockers -ne 0) { throw 'Current IUserContextService should have no aggregate blockers.' }
if ($result.moduleReadiness.ready) { throw 'Users module must remain not ready while aggregate or mutation paths remain.' }
if (@($result.moduleReadiness.leakingContracts) -notcontains 'IUserDirectoryService') { throw 'Boundary scan missed IUserDirectoryService.' }
if (@($result.moduleReadiness.leakingContracts) -notcontains 'IUserLookupRepository') { throw 'Boundary scan missed an inherited transitive wrapper.' }
if ($result.categories.transitiveWrapper -lt 1) { throw 'Inherited aggregate wrappers must be categorized separately.' }
if (@($result.leaks | Where-Object contract -eq 'IUserDirectoryService').Count -lt 1) { throw 'Boundary scan missed IUserDirectoryService consumers.' }
if (@($result.moduleReadiness.blockers).Count -lt 1) { throw 'Module readiness needs explicit blockers.' }
if (-not $result.contractReadiness.aggregateReady) { throw 'Contract and module readiness were not separated.' }
Write-Host "LLM Wiki extraction readiness regression passed: $($result.moduleReadiness.aggregateLeakPaths) production leak path(s)."
