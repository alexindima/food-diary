[CmdletBinding()]
param([Parameter(Mandatory)][string]$Command)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$registry = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/command-registry.json') -Raw | ConvertFrom-Json
$tier = @($registry.tiers | Where-Object { @($_.commands) -contains $Command } | Select-Object -First 1)
if ($tier.Count -eq 0) { throw "Wiki command is not registered: $Command" }

$help = @{
    start = @("start -Intent '<large task>' [-PlannedPath <path[]>] [-WorkspacePath <path>]", 'Creates governed research, acceptance, and delivery state for large work.')
    brief = @("brief -Intent '<task>' [-PlannedPath <path[]>] [-CompiledIndexSource Sqlite|Json] [-Compact]", 'Compiles affected scope, risk, instructions, tests, and review obligations.')
    research = @("research -Intent '<task>' [-PlannedPath <path[]>] [-ResearchPurpose Auto|Assessment|Implementation] [-CompiledIndexSource Sqlite|Json] [-Compact] [-SkipHistory]", 'Ranks current-source evidence and Git precedents; explicit planned paths constrain the read set.')
    trace = @("trace -Query '<command, handler, route, or component>' [-TraceView Auto|Backend|Frontend] [-CompiledIndexSource Sqlite|Json]", 'Traces an exact existing flow and abstains when no matching entry point exists.')
    ownership = @("ownership [-Query '<intent>'] [-PlannedPath <path[]>] [-CompiledIndexSource Sqlite|Json]", 'Finds scoped guides, direct owners, and downstream module impact.')
    topology = @("topology [-Query '<bounded term>'] [-Limit <1..50>] [-CompiledIndexSource Sqlite|Json] [-Format Text|Json]", 'Lists declared services, workers, HTTP clients, webhooks, jobs, and network-policy surfaces.')
    privacy = @("privacy [-Query '<term>'] [-PrivacyCategory all|credential|identity|health|financial|privateContent|logging|boundaries|external] [-PlannedPath <path[]>] [-RepositoryWide]", 'Returns candidate sensitive-data evidence; results remain review leads, not proof of runtime flow.')
    health = @("health [-HealthView all|drift|allowances|untracked|cycles|ambiguous|dead-candidates|spec-gaps|test-gaps|debt] [-Query '<term>']", 'Reads architecture-health categories; health all returns the complete category set.')
    hotspots = @("hotspots [-QualityArea Product|Wiki|All] [-Query '<term>'] [-Limit <1..50>]", 'Ranks structural review leads; the score is not a defect finding.')
    'test-gaps' = @("test-gaps [-QualityArea Product|Wiki|All] [-Query '<term>'] [-Limit <1..50>]", 'Reports static direct-reference gaps and explicitly distinguishes them from measured execution coverage.')
}

$entry = if ($help.ContainsKey($Command)) { @($help[$Command]) } else { @($Command, 'Use help -Detailed for the command catalog; inspect the command-specific error when a required parameter is omitted.') }
Write-Host "FoodDiary LLM Wiki command: $Command"
Write-Host "Stability tier: $($tier[0].id)"
Write-Host "Usage: ./.llm-wiki/wiki.ps1 $($entry[0])"
Write-Host "Purpose: $($entry[1])"
