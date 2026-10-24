[CmdletBinding()]
param([Parameter(Mandatory)][string]$Command)

$ErrorActionPreference = 'Stop'
$wikiRoot = Split-Path -Parent $PSScriptRoot
$registry = Get-Content -LiteralPath (Join-Path $wikiRoot 'policies/command-registry.json') -Raw | ConvertFrom-Json
$tier = @($registry.tiers | Where-Object { @($_.commands) -contains $Command } | Select-Object -First 1)
if ($tier.Count -eq 0) { throw "Wiki command is not registered: $Command" }

$help = @{
    design = @("design -Intent '<outcome>' -PlannedPath <paths> -BehaviorSpec <object> -Criterion <outcomes> [-ReviewContent <object>] [-Format Json]", 'Design-required routes review outcome coverage, consumers, failures, sequence and scope. Bind ReviewContent to planReview.planFingerprint; unresolved or stale reviews keep ready=false.')
    'task-context-show' = @('task-context-show -WorkspacePath <task> [-Role researcher|implementer|reviewer] [-Format Json]', 'Prepares a role assignment from the same validated, redacted bounded bundle. Invalid or stale bundles produce no assignment; this does not launch an agent.')
    evals = @('evals [-Detailed] | evals -Behavioral [-EvaluationPath <paired-run-manifest.json>] [-Format Json]', 'Default evals remain deterministic routing checks. Behavioral mode lists pressure scenarios or validates paired reviewed transcripts; no supplied runs means not-run.')
    'verification-telemetry-archive' = @('verification-telemetry-archive [-Format Json]', 'Preserves the complete active history in an immutable hash-bound archive and starts a new active metrics window. Existing execution evidence is unchanged.')
    context = @("context -Query '<question or exact file>' [-Module <owner>] [-ChangeType Any|Backend|Frontend|Tests] [-Compact] [-Limit <1..50>] [-Format Text|Json]", 'Retrieves fresh ranked sources; compact JSON uses one bounded list with test evidence and a 12000-character budget.')
    start = @("start -Intent '<large task>' [-PlannedPath <path[]>] [-Criterion '<observable outcome>' ...] [-WorkspacePath <path>]", 'Creates research and scoped acceptance criteria; explicit criteria replace generic starter outcomes.')
    'evidence-check' = @("evidence-check -EvidencePath <evidence.json> -Id <required-or-supplemental-id> -Status passed|failed|not-applicable -EvidenceCommand '<actual command>' [-Reason '<evidence>'] [-Format Json]", 'Required definitions stay canonical. Output flags are allowed; --no-build requires a successful build receipt for current sources. Unknown IDs with commands become supplemental results and never satisfy required gates.')
    'evidence-run' = @("evidence-run -EvidencePath <evidence.json> -Id <id> [-EvidenceCommand '<focused test command>' -RegressionPhase before|after -TestPath <test files> -ExpectedFailure '<distinctive assertion message>']", 'Ordinary runs execute canonical required checks. Regression runs use a distinct supplemental ID: before records an expected failing assertion, after runs the same command and unchanged tests. Both logs are retained; a manual pass cannot confirm the pair. ExpectedFailure and TestPath are required before; after can reuse them.')
    'evidence-artifact' = @("evidence-artifact -EvidencePath <evidence.json> -Id <review-id> -OutputPath <existing artifact> [-EvidenceKind screenshot|browser-log|accessibility-report|video]", 'Id identifies a review obligation, not a new artifact or check. Read evidence.json reviews for valid IDs; acceptance-map uses -ReviewId for multiple reviews.')
    'task-finish' = @("task-finish -WorkspacePath <task> [-DryRun] [-Format Json]", 'DryRun previews task artifacts and tracked learning registries. Finish before the final commit/push; finalization refreshes the report before sealing.')
    'task-start' = @("task-start -Intent '<task>' -PlannedPath <paths> -Criterion '<outcome>' [-ExcludedPath <forbidden regex>]", 'ExcludedPath forbids changes. Pre-existing unrelated files are handled by the task baseline; explicit PlannedPath defines the analysis scope.')
    'coverage-plan' = @("coverage-plan -PlannedPath <exact spec or test source> [-Format Json]", 'Angular specs produce existing package coverage scripts; C# tests produce focused project commands. Separate mixed frontend/backend requests.')
    brief = @("brief -Intent '<task>' [-PlannedPath <path[]>] [-CompiledIndexSource Sqlite] [-Compact]", 'Compiles affected scope, risk, instructions, tests, and review obligations.')
    research = @("research -Intent '<task>' [-PlannedPath <path[]>] [-ResearchPurpose Auto|Assessment|Implementation] [-CompiledIndexSource Sqlite] [-Compact] [-SkipHistory]", 'Ranks current-source evidence and Git precedents; explicit planned paths constrain the read set.')
    trace = @("trace -Query '<command, handler, route, or component>' [-TraceView Auto|Backend|Frontend] [-CompiledIndexSource Sqlite]", 'Traces an exact existing flow and abstains when no matching entry point exists.')
    ownership = @("ownership [-Query '<intent>'] [-PlannedPath <path[]>] [-CompiledIndexSource Sqlite]", 'Finds scoped guides, direct owners, and downstream module impact.')
    topology = @("topology [-Query '<bounded term>'] [-Limit <1..50>] [-CompiledIndexSource Sqlite] [-Format Text|Json]", 'Lists declared services, workers, HTTP clients, webhooks, jobs, and network-policy surfaces.')
    privacy = @("privacy [-Query '<term>'] [-PrivacyCategory all|credential|identity|health|financial|privateContent|logging|boundaries|external] [-PlannedPath <path[]>] [-RepositoryWide]", 'Returns candidate sensitive-data evidence; results remain review leads, not proof of runtime flow.')
    health = @("health [-HealthView all|drift|allowances|untracked|cycles|ambiguous|dead-candidates|spec-gaps|test-gaps|debt] [-Query '<term>'] | health -QualityArea Wiki [-Format Json]", 'Reads architecture-health categories. Wiki diagnosis includes runtime, disk/temp/cache observations and timing; workflow-metrics supplies the existing median/p95 operation baseline.')
    hotspots = @("hotspots [-QualityArea Product|Wiki|All] [-Query '<term>'] [-Limit <1..50>]", 'Ranks structural review leads; the score is not a defect finding.')
    'test-gaps' = @("test-gaps [-QualityArea Product|Wiki|All] [-Query '<term>'] [-Limit <1..50>]", 'Reports static direct-reference gaps and explicitly distinguishes them from measured execution coverage.')
}

$entry = if ($help.ContainsKey($Command)) { @($help[$Command]) } else { @($Command, 'Use help -Detailed for the command catalog; inspect the command-specific error when a required parameter is omitted.') }
Write-Host "FoodDiary LLM Wiki command: $Command"
Write-Host "Stability tier: $($tier[0].id)"
Write-Host "Usage: ./.llm-wiki/wiki.ps1 $($entry[0])"
Write-Host "Purpose: $($entry[1])"
