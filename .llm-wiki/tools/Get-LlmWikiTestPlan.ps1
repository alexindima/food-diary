[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string[]]$ProposedPath,
    [string]$Intent,
    [object]$DiffInput,
    [object]$PolicyInput,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [switch]$Compact,
    [ValidateRange(1, 30)]
    [int]$Limit = 12
)

$ErrorActionPreference = 'Stop'
$toolsRoot = $PSScriptRoot
$wikiRoot = Split-Path -Parent $toolsRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $toolsRoot 'LlmWikiQueryCache.ps1')
. (Join-Path $toolsRoot 'LlmWikiVerificationReceipts.ps1')
$verificationReceipts = @(Get-LlmWikiVerificationReceipts $repositoryRoot)
$validReceiptFingerprint = Get-LlmWikiSha256 (@(
    $verificationReceipts |
        Where-Object validForCurrentState |
        ForEach-Object { "$($_.normalizedCommand):$($_.recordedAtUtc)" }
) -join '|')
$queryCacheEntry = $null
$cacheEligible = $Format -eq 'Json' -and $null -eq $DiffInput -and $null -eq $PolicyInput
if ($cacheEligible) {
    $queryCacheEntry = Get-LlmWikiQueryCacheEntry -RepositoryRoot $repositoryRoot -Namespace 'test-plan' -Arguments @{
        BaseRef = $BaseRef; HeadRef = $HeadRef; ChangedPath = @($ChangedPath)
        ProposedPath = @($ProposedPath); Intent = $Intent; Compact = [bool]$Compact; Limit = $Limit
        VerificationReceipts = $validReceiptFingerprint
    }
    $cachedTestPlan = Read-LlmWikiQueryCache -Entry $queryCacheEntry
    if ($null -ne $cachedTestPlan) { Write-Output $cachedTestPlan; exit 0 }
}
$frontendPackage = Get-Content -LiteralPath (Join-Path $repositoryRoot 'FoodDiary.Web.Client/package.json') -Raw | ConvertFrom-Json
$frontendWorkspace = Get-Content -LiteralPath (Join-Path $repositoryRoot 'FoodDiary.Web.Client/angular.json') -Raw | ConvertFrom-Json
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
$ruleIds = @($policy.matchedRules | ForEach-Object { if ($_.PSObject.Properties['id']) { $_.id } } | Where-Object { $_ })
$scopes = @($diff.scopes)
$scenarios = [System.Collections.Generic.List[object]]::new()
$discoveredTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$directTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$plannedDirectoryTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$siblingTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$consumerTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$directConsumerProjects = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$changedTestFiles = @(
    $diff.changedPaths |
        Where-Object { $_ -match '\.cs$' -and $_ -match '(^|/)tests/' -or $_ -match '\.(spec|test)\.ts$' } |
        Sort-Object -Unique
)

foreach ($proposedDirectory in @($ProposedPath)) {
    $normalizedDirectory = ([string]$proposedDirectory).Replace('\', '/').TrimEnd('/')
    $absoluteDirectory = Join-Path $repositoryRoot $normalizedDirectory
    if (-not (Test-Path -LiteralPath $absoluteDirectory -PathType Container)) { continue }
    $directoryTests = @(
        Get-ChildItem -LiteralPath $absoluteDirectory -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '\.(?:spec|test)\.(?:ts|js)$' } |
            Select-Object -First (($Limit * 2) + 1)
    )
    if ($directoryTests.Count -gt ($Limit * 2)) { continue }
    foreach ($test in $directoryTests) {
        $relativeTest = $test.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
        $null = $plannedDirectoryTests.Add($relativeTest)
    }
}

foreach ($changedPath in @($diff.changedPaths | Where-Object {
    $_ -match '^FoodDiary\.Web\.Client/.+\.(ts|html|scss)$' -and
    $_ -notmatch '\.(spec|test)\.ts$'
})) {
    [string]$siblingTestPath = $changedPath -replace '\.(ts|html|scss)$', '.spec.ts'
    $absoluteSiblingTestPath = [System.IO.Path]::Combine($repositoryRoot, $siblingTestPath.Replace('/', '\'))
    if (Test-Path -LiteralPath $absoluteSiblingTestPath) {
        $null = $siblingTests.Add($siblingTestPath)
    }
}

$frontendContractPath = Join-Path $wikiRoot 'generated/frontend-contract-index.json'
if (Test-Path -LiteralPath $frontendContractPath) {
    $frontendContracts = Get-Content -LiteralPath $frontendContractPath -Raw | ConvertFrom-Json
    $changedComponents = @(
        $frontendContracts.components |
            Where-Object { $_.path -in @($diff.changedPaths) -or $_.templatePath -in @($diff.changedPaths) }
    )
    foreach ($component in $changedComponents) {
        foreach ($consumer in @($frontendContracts.consumerEdges | Where-Object component -eq $component.class)) {
            $consumerSpec = $consumer.consumerPath -replace '\.html$', '.spec.ts'
            if (Test-Path -LiteralPath (Join-Path $repositoryRoot $consumerSpec)) {
                $null = $consumerTests.Add($consumerSpec)
            }
        }
    }
}

$changedTypeNames = @($diff.changedPaths | Where-Object { $_ -match '\.(cs|ts)$' } | ForEach-Object {
    $changedSourcePath = Join-Path $repositoryRoot $_
    if (Test-Path -LiteralPath $changedSourcePath -PathType Leaf) {
        $sourceText = [IO.File]::ReadAllText($changedSourcePath)
        $patterns = if ($_ -match '\.ts$') {
            @(
                '(?m)^(?:export\s+)?(?:default\s+)?(?:abstract\s+)?(?:class|interface|type|enum|function)\s+([A-Za-z_$][\w$]*)'
                '(?m)^(?:export\s+)?const\s+([A-Za-z_$][\w$]*)'
            )
        } else {
            @('(?m)^\s*(?:public\s+|internal\s+|protected\s+|private\s+|sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|interface|record|struct|enum)\s+([A-Za-z_][\w]*)')
        }
        foreach ($pattern in $patterns) {
            foreach ($match in [regex]::Matches($sourceText, $pattern)) {
                if ($match.Groups[1].Value.Length -ge 5) { $match.Groups[1].Value }
            }
        }
    }
} | Sort-Object -Unique)
if ($changedTypeNames.Count -gt 0) {
    $testFiles = @()
    $testRoots = [Collections.Generic.List[string]]::new()
    $testRoots.Add('tests')
    $testRoots.Add('FoodDiary.Web.Client/src')
    if (@($effectivePaths | Where-Object { $_ -match '^MailRelay/' }).Count -gt 0) { $testRoots.Add('MailRelay/tests') }
    if (@($effectivePaths | Where-Object { $_ -match '^MailInbox/' }).Count -gt 0) { $testRoots.Add('MailInbox/tests') }
    foreach ($testRoot in $testRoots) {
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
    $consumerNamesPattern = @($changedTypeNames | ForEach-Object { [regex]::Escape($_) }) -join '|'
    $consumerPattern = "(^|[^A-Za-z0-9_])($consumerNamesPattern)([^A-Za-z0-9_]|$)"
    $trackedCSharpFiles = @(& git -C $repositoryRoot grep -l -E $consumerPattern -- '*.cs')
    if ($LASTEXITCODE -notin @(0, 1)) { throw 'Unable to search C# consumers for the focused test plan.' }
    foreach ($relativeSource in $trackedCSharpFiles) {
        $normalizedSource = $relativeSource.Replace('\', '/')
        if ($normalizedSource -in @($diff.changedPaths) -or $normalizedSource -match '(?i)(^|/)(tests?|__tests__)/|\.Tests?/') { continue }
        $absoluteSource = Join-Path $repositoryRoot $relativeSource
        $directory = Split-Path -Parent $absoluteSource
        while ($directory.StartsWith($repositoryRoot, [StringComparison]::OrdinalIgnoreCase)) {
            $project = Get-ChildItem -LiteralPath $directory -Filter '*.csproj' -File -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($project) {
                $null = $directConsumerProjects.Add($project.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/'))
                break
            }
            if ($directory -eq $repositoryRoot) { break }
            $directory = Split-Path -Parent $directory
        }
    }
}
foreach ($path in @($directTests | Sort-Object)) { $null = $discoveredTests.Add($path) }
foreach ($path in @($diff.focusedTests)) { $null = $discoveredTests.Add($path) }

$rankedFocusedTests = [System.Collections.Generic.List[object]]::new()
$rankedSeen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
function Add-RankedTests {
    param([string[]]$Paths, [int]$Rank, [string]$Reason)
    foreach ($path in @($Paths)) {
        if ([string]::IsNullOrWhiteSpace($path) -or -not $rankedSeen.Add($path)) { continue }
        $rankedFocusedTests.Add([pscustomobject]@{
            path = $path
            rank = $Rank
            priority = if ($Rank -ge 90) { 'required' } elseif ($Rank -ge 70) { 'recommended' } else { 'contextual' }
            reason = $Reason
        })
    }
}
Add-RankedTests $changedTestFiles 100 'changed-test'
Add-RankedTests @($plannedDirectoryTests | Sort-Object) 95 'planned-directory-spec'
Add-RankedTests @($siblingTests | Sort-Object) 90 'direct-sibling-spec'
Add-RankedTests @($consumerTests | Sort-Object) 80 'direct-component-consumer'
$changedFrontendFeatureRoots = @($effectivePaths | Where-Object { $_ -match '^FoodDiary\.Web\.Client/(?:src/app|projects/[^/]+/src/app)/features/[^/]+' } | ForEach-Object {
    [regex]::Match(([string]$_).Replace('\', '/'), '^FoodDiary\.Web\.Client/(?:src/app|projects/[^/]+/src/app)/features/[^/]+').Value
} | Sort-Object -Unique)
$affineDirectTests = @($directTests | Where-Object {
    $testPath = ([string]$_).Replace('\', '/')
    $testPath -notmatch '^FoodDiary\.Web\.Client/' -or
        $changedFrontendFeatureRoots.Count -eq 0 -or
        @($changedFrontendFeatureRoots | Where-Object { $testPath.StartsWith("$_/", [StringComparison]::OrdinalIgnoreCase) }).Count -gt 0
} | Sort-Object)
Add-RankedTests $affineDirectTests 90 'references-changed-symbol-in-feature-boundary'
$scopeAffinityTokens = @($effectivePaths | ForEach-Object {
    [regex]::Matches(([string]$_).ToLowerInvariant(), '[a-z0-9]+') | ForEach-Object Value
} | Where-Object { $_.Length -ge 5 -and $_ -notin @('fooddiary', 'application', 'client', 'features', 'tests', 'integration') } | Sort-Object -Unique)
$affineDownstreamTests = @($diff.focusedTests | Where-Object {
    $testPath = if ($_ -is [string]) { [string]$_ } elseif ($_.PSObject.Properties['path']) { [string]$_.path } else { '' }
    $normalizedTestPath = $testPath.ToLowerInvariant()
    $scopeAffinityTokens.Count -eq 0 -or @($scopeAffinityTokens | Where-Object { $normalizedTestPath.Contains($_) }).Count -gt 0
} | ForEach-Object { if ($_ -is [string]) { [string]$_ } else { [string]$_.path } })
Add-RankedTests $affineDownstreamTests 40 'downstream-context-with-path-affinity'
$selectedFocusedTests = @($rankedFocusedTests | Select-Object -First $Limit)

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
        priority = 'required'
        reason = "triggered-policy:$($_.sourceRule)"
    } }) +
    @($diff.recommendedChecks | ForEach-Object { [pscustomobject]@{
        id = "recommended-$((Get-LlmWikiSha256 ([string]$_)).Substring(0, 10))"
        command = $_
        source = 'context'
        priority = 'full-regression'
        reason = 'broad-change-context'
    } })
) | Sort-Object command -Unique)

$frontendFocusedTests = @(
    $selectedFocusedTests | ForEach-Object { if ($_.PSObject.Properties['path']) { $_.path } } |
        Where-Object { $_ -match '^FoodDiary\.Web\.Client/.+\.spec\.ts$' } |
        Select-Object -First 5
)
foreach ($frontendProjectGroup in @($frontendFocusedTests | Group-Object {
    if ($_ -match '^FoodDiary\.Web\.Client/projects/fooddiary-admin/') { 'admin' }
    elseif ($_ -match '^FoodDiary\.Web\.Client/projects/fd-ui-kit/') { 'ui-kit' }
    elseif ($_ -match '^FoodDiary\.Web\.Client/projects/fd-tour/') { 'tour' }
    else { 'app' }
})) {
    $testPaths = @($frontendProjectGroup.Group)
    $testPath = $testPaths[0]
    $workspacePath = $testPath.Substring('FoodDiary.Web.Client/'.Length)
    $scriptAndProject = if ($workspacePath -match '^projects/fooddiary-admin/') {
        @('test:ci:admin', 'fooddiary-admin')
    } elseif ($workspacePath -match '^projects/fd-ui-kit/') {
        @('test:ci:ui-kit', 'fd-ui-kit')
    } elseif ($workspacePath -match '^projects/fd-tour/') {
        @('test:ci:tour', 'fd-tour')
    } else {
        @('test:ci:app', 'food-diary-web-client')
    }
    $script = $scriptAndProject[0]
    $project = $scriptAndProject[1]
    $scriptExists = $null -ne $frontendPackage.scripts.PSObject.Properties[$script]
    if (-not $scriptExists) { continue }
    $builder = $frontendWorkspace.projects.PSObject.Properties[$project].Value.architect.test.builder
    $supportsInclude = $builder -eq '@angular/build:unit-test'
    $includeArguments = @($testPaths | ForEach-Object { "--include=$($_.Substring('FoodDiary.Web.Client/'.Length))" }) -join ' '
    $commands += [pscustomobject]@{
        id = 'focused-frontend'
        command = if ($supportsInclude) {
            "cd FoodDiary.Web.Client && npm run $script -- $includeArguments"
        } else {
            "cd FoodDiary.Web.Client && npm run $script"
        }
        source = 'focused-test'
        priority = [string](($selectedFocusedTests | Where-Object { $_.path -in $testPaths } | Sort-Object @{ Expression = { switch ($_.priority) { 'required' { 0 } 'recommended' { 1 } default { 2 } } } } | Select-Object -First 1).priority)
        reason = "grouped-focused-tests:$($testPaths.Count)"
        commandEvidence = if ($supportsInclude) {
            "package.json:$script; angular.json:$project=$builder"
        } else {
            "package.json:$script; focused include unsupported by angular.json builder '$builder'"
        }
    }
}
$backendFocusedProjects = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($focusedTest in @($selectedFocusedTests | Where-Object { $_.path -match '\.cs$' })) {
    $directory = Split-Path -Parent (Join-Path $repositoryRoot ([string]$focusedTest.path))
    while ($directory.StartsWith($repositoryRoot, [StringComparison]::OrdinalIgnoreCase)) {
        $project = Get-ChildItem -LiteralPath $directory -Filter '*.csproj' -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($project) {
            $projectPath = $project.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
            if (-not $backendFocusedProjects.ContainsKey($projectPath)) { $backendFocusedProjects[$projectPath] = $focusedTest }
            break
        }
        if ($directory -eq $repositoryRoot) { break }
        $directory = Split-Path -Parent $directory
    }
}
foreach ($focusedProject in $backendFocusedProjects.GetEnumerator()) {
    $commands += [pscustomobject]@{
        id = "focused-backend-$([IO.Path]::GetFileNameWithoutExtension($focusedProject.Key).ToLowerInvariant())"
        command = "dotnet test $($focusedProject.Key) --no-restore"
        source = 'focused-test'
        priority = [string]$focusedProject.Value.priority
        reason = 'grouped-focused-test-project'
        commandEvidence = [string]$focusedProject.Value.path
    }
}
if ($directConsumerProjects.Count -gt 3) {
    $commands += [pscustomobject]@{
        id = 'composition-confidence'
        command = 'dotnet build FoodDiary.slnx --no-restore'
        source = 'consumer-graph'
        priority = 'recommended'
        reason = 'grouped-transitive-composition-confidence'
        commandEvidence = "$($directConsumerProjects.Count) production projects reference changed symbols; grouped instead of listing each project build"
    }
} else {
    foreach ($consumerProject in @($directConsumerProjects | Sort-Object)) {
        $commands += [pscustomobject]@{
            id = 'compile-direct-consumer'
            command = "dotnet build $consumerProject --no-restore"
            source = 'consumer-graph'
            priority = 'required'
            reason = 'production-project-references-changed-symbol'
            commandEvidence = $consumerProject
        }
    }
}
$contractBoundaryChange = @($effectivePaths | Where-Object {
    $_ -match '^FoodDiary\.Application(?:\.Abstractions)?/.+\.cs$' -and
    $_ -match '(?:Common|Contracts|Models|Repositories)/|I[A-Z][A-Za-z0-9]+\.cs$'
}).Count -gt 0
$presentationBoundaryChange = @($effectivePaths | Where-Object { $_ -match '^FoodDiary\.Presentation\.Api/.+\.cs$' }).Count -gt 0
if ($contractBoundaryChange) {
    $commands += [pscustomobject]@{
        id = 'application-contract-tests'; command = 'dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --no-restore'
        source = 'contract-boundary'; priority = 'required'; reason = 'application-contract-moved-or-reshaped'; commandEvidence = 'Application/Application.Abstractions contract path'
    }
    $commands += [pscustomobject]@{
        id = 'architecture-contract-tests'; command = 'dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj --no-restore'
        source = 'contract-boundary'; priority = 'required'; reason = 'assembly-and-namespace-boundary-change'; commandEvidence = 'Application/Application.Abstractions contract path'
    }
    Add-Scenario 'old-implementation-namespace-absent' 'Verify consumers no longer import the implementation namespace for contracts moved to an abstraction assembly.' 'Architecture test plus repository search for the former namespace'
    Add-Scenario 'contract-return-type-boundary' 'Verify public abstraction contracts do not expose implementation-owned types or mutable domain aggregates unintentionally.' 'Contract consumer report and architecture test'
}
if ($presentationBoundaryChange -or ($contractBoundaryChange -and @($effectivePaths | Where-Object { $_ -match 'Presentation|Http|Controller|Mappings' }).Count)) {
    $commands += [pscustomobject]@{
        id = 'presentation-contract-tests'; command = 'dotnet test tests/FoodDiary.Presentation.Api.Tests/FoodDiary.Presentation.Api.Tests.csproj --no-restore'
        source = 'contract-boundary'; priority = 'required'; reason = 'presentation-namespace-or-contract-consumer-change'; commandEvidence = 'Presentation API path or consumer'
    }
}
$commands = @($commands | Group-Object { ([string]$_.command) -replace '\s+--no-restore\s*$', '' } | ForEach-Object {
    @($_.Group | Sort-Object @{ Expression = { switch ($_.priority) { 'required' { 0 } 'recommended' { 1 } 'contextual' { 2 } default { 3 } } } }, command | Select-Object -First 1)
} | Sort-Object command)
$commandIdCounts = @{}
$commands = @($commands | ForEach-Object {
    $baseId = [string]$_.id
    if (-not $commandIdCounts.ContainsKey($baseId)) { $commandIdCounts[$baseId] = 0 }
    $commandIdCounts[$baseId]++
    if ($commandIdCounts[$baseId] -gt 1) { $_.id = "$baseId-$($commandIdCounts[$baseId])" }
    $_
})
$validReceiptsByCommand = @{}
foreach ($receipt in @($verificationReceipts | Where-Object validForCurrentState)) {
    if (-not $validReceiptsByCommand.ContainsKey([string]$receipt.normalizedCommand)) {
        $validReceiptsByCommand[[string]$receipt.normalizedCommand] = $receipt
    }
}
$commands = @($commands | ForEach-Object {
    $receipt = $validReceiptsByCommand[(Normalize-LlmWikiVerificationCommand ([string]$_.command))]
    [pscustomobject][ordered]@{
        id = $_.id
        command = $_.command
        source = $_.source
        priority = $_.priority
        reason = $_.reason
        commandEvidence = $(if ($_.PSObject.Properties['commandEvidence']) { $_.commandEvidence } else { $null })
        status = $(if ($null -eq $receipt) { 'pending' } else { 'satisfied' })
        receipt = $(if ($null -eq $receipt) { $null } else { [pscustomobject]@{
            result = $receipt.result
            durationSeconds = $receipt.durationSeconds
            coverageScope = @($receipt.coverageScope)
            recordedAtUtc = $receipt.recordedAtUtc
            fingerprint = $receipt.fingerprint
        } })
    }
})

$result = [pscustomobject]@{
    scopes = $scopes
    intent = $Intent
    proposedPaths = @($ProposedPath)
    modules = @($diff.modules | ForEach-Object { if ($_.PSObject.Properties['name']) { $_.name } } | Where-Object { $_ })
    focusedTestFiles = @($selectedFocusedTests | ForEach-Object { if ($_.PSObject.Properties['path']) { $_.path } } | Where-Object { $_ })
    focusedTestDetails = $selectedFocusedTests
    commands = @($commands)
    commandGroups = [pscustomobject][ordered]@{
        required = @($commands | Where-Object { $_.priority -eq 'required' -and $_.status -eq 'pending' })
        recommended = @($commands | Where-Object { $_.priority -eq 'recommended' -and $_.status -eq 'pending' })
        fullRegression = @($commands | Where-Object { $_.priority -in @('contextual', 'full-regression') -and $_.status -eq 'pending' })
        satisfied = @($commands | Where-Object status -eq 'satisfied')
    }
    scenarios = @($scenarios)
    reviewObligations = @($policy.reviewObligations)
}

$resultOutput = if ($Compact) {
    [pscustomobject]@{
        compact = $true
        scopes = $result.scopes
        modules = $result.modules
        focusedTests = $result.focusedTestDetails
        commands = $result.commands
        scenarios = @($result.scenarios | Select-Object id, description)
        reviewObligationIds = @($result.reviewObligations | ForEach-Object { if ($_.PSObject.Properties['id']) { $_.id } } | Where-Object { $_ })
    }
} else {
    $result
}

if ($Format -eq 'Json') {
    $resultJson = $resultOutput | ConvertTo-Json -Depth 8
    if ($queryCacheEntry) { Write-LlmWikiQueryCache -Entry $queryCacheEntry -Content $resultJson }
    Write-Output $resultJson
    exit 0
}

Write-Host "Test plan: $(@($result.focusedTestFiles).Count) focused file(s), $(@($result.commands).Count) command(s), $(@($result.scenarios).Count) scenario(s)."
Write-Host ''
Write-Host 'Focused test files:'
foreach ($path in $result.focusedTestFiles) { Write-Host " - $path" }
Write-Host ''
Write-Host 'Commands by obligation:'
foreach ($priority in @('required', 'recommended', 'full-regression', 'contextual')) {
    $entries = @($result.commands | Where-Object priority -eq $priority)
    if ($entries.Count -eq 0) { continue }
    Write-Host " $priority`:"
    foreach ($entry in $entries) {
        $status = if ($entry.status -eq 'satisfied') { "; satisfied $($entry.receipt.durationSeconds)s" } else { '' }
        Write-Host "  - [$($entry.source); $($entry.reason)$status] $($entry.command)"
    }
}
Write-Host ''
Write-Host 'Scenarios:'
foreach ($scenario in $result.scenarios) { Write-Host " - $($scenario.id): $($scenario.description) Evidence: $($scenario.evidence)." }
