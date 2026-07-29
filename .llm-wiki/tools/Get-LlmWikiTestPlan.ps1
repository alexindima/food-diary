[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string[]]$ProposedPath,
    [object]$DiffInput,
    [object]$PolicyInput,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [ValidateRange(1, 30)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$wikiRoot = Split-Path -Parent $toolsRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$common = @{ BaseRef = $BaseRef; Format = 'Json' }
if ($PSBoundParameters.ContainsKey('HeadRef')) { $common.HeadRef = $HeadRef }
$effectivePaths = @(
    @($ChangedPath) + @($ProposedPath) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique
)
if ($effectivePaths.Count -gt 0) { $common.ChangedPath = $effectivePaths }

$diffArguments = @{} + $common
$diffArguments.Limit = [Math]::Min($Limit, 20)
$diff = if ($null -ne $DiffInput) { $DiffInput } else {
    & (Join-Path $toolsRoot 'Get-LlmWikiDiffContext.ps1') @diffArguments | ConvertFrom-Json
}
$policy = if ($null -ne $PolicyInput) { $PolicyInput } else {
    & (Join-Path $toolsRoot 'Test-LlmWikiChangePolicy.ps1') @common | ConvertFrom-Json
}
$ruleIds = @($policy.matchedRules.id)
$scopes = @($diff.scopes)
$scenarios = [System.Collections.Generic.List[object]]::new()
$discoveredTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$directTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

$changedTypeNames = @(
    $diff.changedPaths |
        Where-Object { $_ -match '\.(cs|ts)$' } |
        ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) } |
        Where-Object { $_.Length -ge 5 } |
        Sort-Object -Unique
)
if ($changedTypeNames.Count -gt 0) {
    $testFiles = @()
    foreach ($testRoot in @('tests', 'MailRelay/tests', 'MailInbox/tests', 'FoodDiary.Web.Client/src')) {
        $absoluteTestRoot = Join-Path $repositoryRoot $testRoot
        if (-not (Test-Path -LiteralPath $absoluteTestRoot)) { continue }
        $testFiles += @(
            Get-ChildItem -LiteralPath $absoluteTestRoot -Recurse -File -ErrorAction SilentlyContinue |
                Where-Object {
                    $_.Extension -eq '.cs' -or
                    $_.Name -match '\.(spec|test)\.ts$'
                }
        )
    }
    foreach ($testFile in $testFiles) {
        $content = [System.IO.File]::ReadAllText($testFile.FullName)
        foreach ($typeName in $changedTypeNames) {
            if ($content -match "\b$([regex]::Escape($typeName))\b") {
                $relative = $testFile.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
                $null = $directTests.Add($relative)
                break
            }
        }
    }
}
foreach ($path in @($directTests | Sort-Object)) { $null = $discoveredTests.Add($path) }
foreach ($path in @($diff.focusedTests)) { $null = $discoveredTests.Add($path) }

function Add-Scenario {
    param([string]$Id, [string]$Description, [string]$Evidence)
    if (@($scenarios | Where-Object id -eq $Id).Count -eq 0) {
        $scenarios.Add([pscustomobject]@{ id = $Id; description = $Description; evidence = $Evidence })
    }
}

if ('Backend' -in $scopes) {
    Add-Scenario 'backend-happy-path' 'Exercise the primary successful use-case path and assert durable result/state.' 'Focused unit or integration test'
    Add-Scenario 'backend-validation' 'Cover invalid, missing, and boundary inputs without performing side effects.' 'Validator/handler test'
    Add-Scenario 'backend-cancellation' 'Confirm asynchronous work propagates CancellationToken where the path performs I/O.' 'Focused test or code review'
}
if ('Api' -in $scopes) {
    Add-Scenario 'api-auth-scope' 'Verify anonymous, forbidden, and cross-user/resource access behavior as applicable.' 'Presentation/integration test'
    Add-Scenario 'api-contract' 'Verify route, request/response schema, status codes, and stable error shape.' 'Contract snapshots and API integration test'
}
if ('Database' -in $scopes -or 'performance-data-access' -in $ruleIds) {
    Add-Scenario 'persistence-query-shape' 'Exercise realistic cardinality, ordering, pagination, tracking, and provider-specific query behavior.' 'PostgreSQL integration test'
    Add-Scenario 'persistence-concurrency' 'Check duplicate, retry, idempotency, and concurrent mutation behavior when applicable.' 'Integration test'
}
if ('ef-migration' -in $ruleIds) {
    Add-Scenario 'migration-forward' 'Apply the migration from the preceding schema and verify expected objects/data.' 'Migration integration test'
    Add-Scenario 'migration-operational-safety' 'Review locks, backfill cost, null/default transition, and rollback/roll-forward strategy.' 'Recorded review evidence'
}
if ('Frontend' -in $scopes) {
    Add-Scenario 'frontend-state-matrix' 'Cover loading, success, empty, validation, permission, and error states that changed.' 'Unit/component tests'
    Add-Scenario 'frontend-interaction' 'Exercise the target user interaction and assert the rendered state transition.' 'Browser or Playwright evidence'
}
if ('frontend-component-contract' -in $ruleIds) {
    Add-Scenario 'frontend-component-contract' 'Verify selector, required/optional inputs, output payloads, defaults, and consuming templates remain compatible.' 'Component tests and consumer review'
    Add-Scenario 'frontend-accessibility' 'Verify accessible name, semantics, keyboard path, focus behavior, disabled state, and error announcement.' 'Component/browser accessibility evidence'
}
if ('shared-ui-consumer-contract' -in $ruleIds) {
    Add-Scenario 'shared-ui-consumers' 'Inspect every indexed selector consumer and verify changed required inputs, defaults, output payloads, styling hooks, and projected content.' 'Consumer graph review and representative app/admin tests'
    Add-Scenario 'shared-ui-cross-surface' 'Render representative main-app, admin, and UI-kit consumers at relevant viewport and theme combinations.' 'Browser screenshots or visual regression evidence'
}
if ('Localization' -in $scopes) {
    Add-Scenario 'localization-pair' 'Verify English/Russian key parity, interpolation, pluralization, and Cyrillic rendering.' 'i18n check and rendered evidence'
}
if ('Configuration' -in $scopes) {
    Add-Scenario 'configuration-contract' 'Verify binding, validation, safe defaults, missing/invalid values, synchronized templates, and secret redaction.' 'Options/startup tests and environment review'
}
if ('Deployment' -in $scopes -or 'deployment-rollout' -in $ruleIds) {
    Add-Scenario 'deployment-compatibility' 'Verify deployment ordering, mixed-version compatibility, readiness, post-deploy smoke checks, and data-safe rollback or roll-forward.' 'Staging or operational evidence'
}
if ('security-sensitive' -in $ruleIds) {
    Add-Scenario 'security-abuse' 'Exercise replay, enumeration, authorization scope, sensitive logging, and resource-abuse cases as applicable.' 'Focused security tests/review'
}
if ('observability-critical-flow' -in $ruleIds) {
    Add-Scenario 'observability-outcomes' 'Verify success, expected failure, unexpected failure, duration, and stable low-cardinality dimensions.' 'Telemetry test or recorded inspection'
}
if ('runtime-resilience' -in $ruleIds) {
    Add-Scenario 'runtime-resilience' 'Exercise timeout/cancellation, retry exhaustion, duplicate/replayed/out-of-order delivery, partial failure, graceful shutdown, and recovery as applicable.' 'Focused unit/integration and operational evidence'
}
if ('privacy-data-lifecycle' -in $ruleIds) {
    Add-Scenario 'privacy-lifecycle' 'Verify authorization, minimization, redaction, provider sharing, export, retention/deletion, and absence from logs/telemetry/cache where not required.' 'Focused tests and recorded privacy review'
}
if ('domain-invariant' -in $ruleIds) {
    Add-Scenario 'domain-invariant-boundaries' 'Exercise valid boundary values plus below/above, null/empty, non-finite, and illegal state transitions as applicable.' 'Focused domain unit tests'
    Add-Scenario 'domain-invariant-preservation' 'Verify every construction and mutation path preserves the aggregate/value-object invariant.' 'Domain tests and call-site review'
}
if ('persistence-model-contract' -in $ruleIds) {
    Add-Scenario 'persistence-model-contract' 'Verify table/column mapping, nullability, keys, uniqueness, relationships, delete behavior, and value conversion against the domain model.' 'Provider-backed integration test and migration review'
    Add-Scenario 'persistence-index-shape' 'Validate expected lookup/order predicates are supported by indexes without redundant or unsafe uniqueness changes.' 'Query-plan or schema review'
}
if ('backend-public-contract' -in $ruleIds) {
    Add-Scenario 'backend-contract-consumers' 'Inspect indexed production and test consumers; verify constructor/member/nullability/generic changes compile and preserve behavior.' 'Consumer graph review plus focused tests'
    Add-Scenario 'backend-contract-serialization' 'Verify JSON/message serialization names, requiredness, defaults, enum values, backward/forward compatibility, and unknown-field behavior where the contract crosses a process boundary.' 'Contract/integration tests'
    Add-Scenario 'backend-contract-rollout' 'Check mixed-version producer/consumer compatibility and deployment order for HTTP, message, and client-package contracts.' 'Compatibility and rollout review'
}
if ('architecture-drift' -in $ruleIds) {
    Add-Scenario 'architecture-dependency-drift' 'Verify every project reference is explicitly allowed, new production projects are governed, and module dependencies remain acyclic.' 'Architecture health index and architecture tests'
    Add-Scenario 'architecture-dependency-necessity' 'Confirm new references are necessary, point in the intended layer direction, and do not bypass client or abstraction boundaries.' 'Dependency and ADR review'
}

$commands = @(@(
    @($policy.requiredChecks | ForEach-Object { [pscustomobject]@{
        id = $_.id
        command = $_.command
        source = 'policy'
    } }) +
    @($diff.recommendedChecks | ForEach-Object { [pscustomobject]@{
        id = 'recommended'
        command = $_
        source = 'context'
    } })
) | Sort-Object command -Unique)

$frontendFocusedTests = @(
    $discoveredTests |
        Where-Object { $_ -match '^FoodDiary\.Web\.Client/.+\.spec\.ts$' } |
        Sort-Object |
        Select-Object -First 5
)
foreach ($testPath in $frontendFocusedTests) {
    $workspacePath = $testPath.Substring('FoodDiary.Web.Client/'.Length)
    $script = if ($workspacePath -match '^projects/fooddiary-admin/') {
        'test:ci:admin'
    } elseif ($workspacePath -match '^projects/fd-ui-kit/') {
        'test:ci:ui-kit'
    } elseif ($workspacePath -match '^projects/fd-tour/') {
        'test:ci:tour'
    } else {
        'test:ci:app'
    }
    $commands += [pscustomobject]@{
        id = 'focused-frontend'
        command = "cd FoodDiary.Web.Client && npm run $script -- --include=$workspacePath"
        source = 'focused-test'
    }
}
$commands = @($commands | Sort-Object command -Unique)

$result = [pscustomobject]@{
    scopes = $scopes
    proposedPaths = @($ProposedPath)
    modules = @($diff.modules.name)
    focusedTestFiles = @(
        @($directTests | Sort-Object) +
        @($diff.focusedTests | Where-Object { $_ -notin $directTests } | Sort-Object) |
            Select-Object -First $Limit
    )
    commands = @($commands)
    scenarios = @($scenarios)
    reviewObligations = @($policy.reviewObligations)
}

if ($Format -eq 'Json') {
    $result | ConvertTo-Json -Depth 8
    exit 0
}

Write-Host "Test plan: $(@($result.focusedTestFiles).Count) focused file(s), $(@($result.commands).Count) command(s), $(@($result.scenarios).Count) scenario(s)."
Write-Host ''
Write-Host 'Focused test files:'
foreach ($path in $result.focusedTestFiles) { Write-Host " - $path" }
Write-Host ''
Write-Host 'Commands:'
foreach ($entry in $result.commands) { Write-Host " - [$($entry.source)] $($entry.command)" }
Write-Host ''
Write-Host 'Scenarios:'
foreach ($scenario in $result.scenarios) { Write-Host " - $($scenario.id): $($scenario.description) Evidence: $($scenario.evidence)." }
