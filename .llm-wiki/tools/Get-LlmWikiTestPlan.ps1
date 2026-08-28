[CmdletBinding()]
param(
    [string]$BaseRef = 'HEAD',
    [string]$HeadRef,
    [string[]]$ChangedPath,
    [string[]]$ProposedPath,
    [string[]]$ExecutedCheck,
    [string]$Intent,
    [object]$DiffInput,
    [object]$PolicyInput,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text',
    [switch]$Compact,
    [switch]$NoBaseline,
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
        VerificationReceipts = $validReceiptFingerprint; ExecutedCheck = @($ExecutedCheck); NoBaseline = [bool]$NoBaseline
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
$normalizedIntent = ([string]$Intent).ToLowerInvariant()
$assessmentDimensionCount = @(
    [regex]::Matches($normalizedIntent, '\b(correctness|reliability|concurrency|architecture|privacy|security|ci|operations|operational|project|repository|cross-layer|system-wide)\b|корректност|над[её]жност|конкурент|архитектур|приватност|конфиденциальност|безопасност|уязвимост|операц|проект|репозитор') |
        ForEach-Object Value |
        Sort-Object -Unique
).Count
$explicitRepositoryWideIntent = $normalizedIntent -match '\b(entire|whole)\s+(project|repository|codebase)\b|\brepository-wide\b|всего\s+проекта|всей\s+кодовой\s+базы'
$repositoryAssessment = $normalizedIntent -match '\b(audit|assessment|evaluate|review)\b|аудит|оцен' -and
    ($assessmentDimensionCount -ge 3 -or ($explicitRepositoryWideIntent -and $assessmentDimensionCount -ge 2))
if ($effectivePaths.Count -gt 0) {
    $common.ChangedPath = $effectivePaths
} elseif ($repositoryAssessment) {
    # A repository assessment is intentionally broader than the current diff.
    # Keep an unrelated dirty worktree out of its evidence selection.
    $common.ChangedPath = @()
}

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
$declaredTypeTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$plannedDirectoryTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$siblingTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$consumerTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$behavioralIntentTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$neighborTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$repositoryAssessmentTests = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$repositoryAntipatterns = [System.Collections.Generic.List[object]]::new()
$directConsumerProjects = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$changedTestFiles = @(
    $effectivePaths |
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

foreach ($changedPath in @($effectivePaths | Where-Object {
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

$changedDeclaredTypeNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$changedTypeNames = @($effectivePaths | Where-Object { $_ -match '\.(cs|ts)$' } | ForEach-Object {
    $changedSourcePath = Join-Path $repositoryRoot $_
    if (Test-Path -LiteralPath $changedSourcePath -PathType Leaf) {
        $sourceText = [IO.File]::ReadAllText($changedSourcePath)
        $patterns = if ($_ -match '\.ts$') {
            @(
                '(?m)^(?:export\s+)?(?:default\s+)?(?:abstract\s+)?(?:class|interface|type|enum|function)\s+([A-Za-z_$][\w$]*)'
                '(?m)^(?:export\s+)?const\s+([A-Za-z_$][\w$]*)'
            )
        } else {
            @(
                '(?m)^\s*(?:public\s+|internal\s+|protected\s+|private\s+|sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|interface|record|struct|enum)\s+([A-Za-z_][\w]*)'
                '(?m)^\s*(?:public\s+|internal\s+|protected\s+|private\s+)(?:static\s+|virtual\s+|override\s+|abstract\s+|async\s+)*(?:[A-Za-z_][\w.<>?\[\],]*\s+)+([A-Za-z_][\w]*)\s*\('
            )
        }
        for ($patternIndex = 0; $patternIndex -lt $patterns.Count; $patternIndex++) {
            $pattern = $patterns[$patternIndex]
            foreach ($match in [regex]::Matches($sourceText, $pattern)) {
                if ($match.Groups[1].Value.Length -ge 5) {
                    $name = $match.Groups[1].Value
                    if ($patternIndex -eq 0) { $null = $changedDeclaredTypeNames.Add($name) }
                    $name
                }
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
        $relative = $testFile.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
        $declaredTypeMatch = @($changedDeclaredTypeNames | Where-Object {
            $content -match "\b$([regex]::Escape($_))\b"
        }).Count -gt 0
        if ($declaredTypeMatch) {
            $null = $declaredTypeTests.Add($relative)
            $null = $directTests.Add($relative)
            continue
        }
        foreach ($typeName in $changedTypeNames) {
            if ($content -match "\b$([regex]::Escape($typeName))\b") {
                $null = $directTests.Add($relative)
                break
            }
        }
    }
    $trackedCSharpFileSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $escapedConsumerNames = @($changedTypeNames | ForEach-Object { [regex]::Escape($_) })
    foreach ($consumerNamesPattern in @(Split-LlmWikiGitGrepAlternatives -Alternative $escapedConsumerNames)) {
        $consumerPattern = "(^|[^A-Za-z0-9_])($consumerNamesPattern)([^A-Za-z0-9_]|$)"
        foreach ($trackedCSharpFile in @(Invoke-LlmWikiGitPathList `
                -RepositoryRoot $repositoryRoot `
                -Arguments @('grep', '-l', '-E', $consumerPattern, '--', '*.cs') `
                -AllowedExitCode @(0, 1) `
                -FailureMessage 'Unable to search C# consumers for the focused test plan.')) {
            $null = $trackedCSharpFileSet.Add($trackedCSharpFile)
        }
    }
    $trackedCSharpFiles = @($trackedCSharpFileSet | Sort-Object)
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
if (-not [string]::IsNullOrWhiteSpace($Intent) -and $Intent -match '(?i)idempoten|duplicate|retry|replay|deduplic') {
    $behaviorAffinity = @($effectivePaths | ForEach-Object {
        [regex]::Matches(([string]$_).ToLowerInvariant(), '[a-z0-9]+') | ForEach-Object Value
    } | Where-Object { $_.Length -ge 5 -and $_ -notin @('fooddiary', 'application', 'infrastructure', 'presentation', 'services') } | Sort-Object -Unique)
    foreach ($testRoot in @('tests', 'MailRelay/tests', 'MailInbox/tests')) {
        $absoluteTestRoot = Join-Path $repositoryRoot $testRoot
        if (-not (Test-Path -LiteralPath $absoluteTestRoot -PathType Container)) { continue }
        foreach ($testFile in Get-ChildItem -LiteralPath $absoluteTestRoot -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue) {
            $relative = $testFile.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
            $content = [IO.File]::ReadAllText($testFile.FullName)
            if ($content -notmatch '(?i)idempoten|duplicate|retry|replay|deduplic') { continue }
            if ($behaviorAffinity.Count -eq 0 -or @($behaviorAffinity | Where-Object { $relative.ToLowerInvariant().Contains($_) }).Count -gt 0) {
                $null = $behavioralIntentTests.Add($relative)
            }
        }
    }
}
if ($repositoryAssessment) {
    foreach ($assessmentTest in @(
        'tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs'
        'tests/FoodDiary.ArchitectureTests/SideEffectReliabilityGuardrailTests.cs'
        'tests/FoodDiary.Web.Api.IntegrationTests/RedisIdempotencyConcurrencyIntegrationTests.cs'
        'tests/FoodDiary.Web.Api.IntegrationTests/PostgresCriticalApiFlowTests.cs'
        'tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.cs'
        'tests/FoodDiary.Application.Tests/Billing/BillingFeatureTests.WebhookCommandTests.cs'
        'tests/FoodDiary.Infrastructure.Tests/Persistence/EmailOutboxTests.cs'
        'tests/FoodDiary.Infrastructure.IntegrationTests/Integration/MigrationSafetyIntegrationTests.cs'
        'tests/FoodDiary.Infrastructure.Tests/Services/BillingGatewayTests.cs'
        'tests/FoodDiary.Web.Api.Tests/Extensions/RateLimiterOptionsSetupTests.cs'
        'tests/FoodDiary.ArchitectureTests/ContainerSupplyChainGuardrailTests.cs'
        'MailRelay/tests/FoodDiary.MailRelay.Application.Tests/MailRelayMessageProcessorTests.cs'
        'MailRelay/tests/FoodDiary.MailRelay.Presentation.Tests/MailRelayPresentationTests.cs'
        'MailInbox/tests/FoodDiary.MailInbox.IntegrationTests/NpgsqlInboundMailStoreIntegrationTests.cs'
        'FoodDiary.Web.Client/src/app/services/auth.service.spec.ts'
        'FoodDiary.Web.Client/src/app/features/dashboard/api/dashboard.service.spec.ts'
    )) {
        if (Test-Path -LiteralPath (Join-Path $repositoryRoot $assessmentTest) -PathType Leaf) {
            $null = $repositoryAssessmentTests.Add($assessmentTest)
        }
    }
}
foreach ($directTest in @($directTests)) {
    $directory = Split-Path -Parent (Join-Path $repositoryRoot $directTest)
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) { continue }
    foreach ($neighbor in Get-ChildItem -LiteralPath $directory -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -eq '.cs' -or $_.Name -match '\.(?:spec|test)\.ts$' } | Select-Object -First 4) {
        $relative = $neighbor.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
        if ($relative -cne $directTest) { $null = $neighborTests.Add($relative) }
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
Add-RankedTests @($declaredTypeTests | Sort-Object) 98 'references-changed-declared-type'
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
Add-RankedTests @($behavioralIntentTests | Sort-Object) 85 'behavioral-intent-and-scope-affinity'
if ($repositoryAssessment) {
    Add-RankedTests @(
        'tests/FoodDiary.ArchitectureTests/SideEffectReliabilityGuardrailTests.cs'
        'tests/FoodDiary.Web.Api.IntegrationTests/RedisIdempotencyConcurrencyIntegrationTests.cs'
        'FoodDiary.Web.Client/src/app/services/auth.service.spec.ts'
    ) 90 'repository-assessment-core-representative'
}
Add-RankedTests @($repositoryAssessmentTests | Sort-Object) 85 'repository-assessment-risk-lane'
Add-RankedTests @($neighborTests | Sort-Object) 70 'neighboring-test-class'
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

$fixedParentPattern = 'Join-Path\s+\$PSScriptRoot\s+[''\"](?:\.\.[\\/])+\.\.'
$fixedParentTriggered = -not [string]::IsNullOrWhiteSpace($Intent) -and $Intent -match '(?i)repository root|repo root|parent traversal|fixed.*\.\.'
if (-not $fixedParentTriggered) {
    foreach ($path in @($effectivePaths | Where-Object { $_ -match '\.ps1$' })) {
        $absolute = Join-Path $repositoryRoot $path
        if ((Test-Path -LiteralPath $absolute -PathType Leaf) -and [IO.File]::ReadAllText($absolute) -match $fixedParentPattern) {
            $fixedParentTriggered = $true
            break
        }
    }
}
if ($fixedParentTriggered) {
    foreach ($candidate in Get-ChildItem -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/tools') -File -Filter '*.ps1') {
        $lineNumber = 0
        foreach ($line in Get-Content -LiteralPath $candidate.FullName) {
            $lineNumber++
            if ($line -notmatch $fixedParentPattern) { continue }
            $repositoryAntipatterns.Add([pscustomobject]@{
                id = 'fixed-parent-repository-root'
                path = $candidate.FullName.Substring($repositoryRoot.Length + 1).Replace('\', '/')
                line = $lineNumber
                evidence = $line.Trim()
            })
        }
    }
    Add-Scenario 'repository-root-resolution-antipattern' 'Review every fixed-depth parent traversal in the affected tool family and replace brittle root discovery with a verified resolver.' 'Repository antipattern matches plus focused Wiki smoke'
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
if ($repositoryAssessment) {
    Add-Scenario 'assessment-architecture' 'Check dependency rules, module cycles, composition roots, and executable-host boundaries.' 'Architecture tests plus source validation'
    Add-Scenario 'assessment-security-privacy' 'Check authentication, authorization, abuse controls, secret handling, sensitive-data boundaries, and provider sharing.' 'Critical API tests plus privacy/security source review'
    Add-Scenario 'assessment-reliability-concurrency' 'Check retries, cancellation, idempotency, duplicate delivery, concurrency, outbox recovery, and graceful shutdown.' 'Focused integration and delivery tests'
    Add-Scenario 'assessment-contracts-data' 'Check API compatibility, persistence invariants, migrations, serialization, and realistic query behavior.' 'Contract, domain, and provider-backed tests'
    Add-Scenario 'assessment-client-ci-operations' 'Check frontend auth/data flows, builds, deterministic CI gates, startup configuration, and operational readiness.' 'Frontend verification, build, and configuration tests'
    Add-Scenario 'assessment-webhook-authenticity' 'Check provider signature validation, timestamp freshness, replay handling, malformed payloads, and duplicate delivery.' 'MailRelay and billing webhook tests plus current provider configuration review'
    Add-Scenario 'assessment-migration-safety' 'Apply migrations against the supported provider and inspect locks, defaults, backfills, rollback/roll-forward, and generated-file completeness.' 'Migration safety integration test and migration source review'
    Add-Scenario 'assessment-deployment-supply-chain' 'Validate Compose syntax, Docker build inputs, pinned dependencies, secret-free manifests, and startup/readiness ordering.' 'docker compose config plus container supply-chain guardrails'
    Add-Scenario 'assessment-dependency-inventory' 'Inventory NuGet/npm manifests and lockfiles, then run current ecosystem advisory audits before concluding vulnerability status.' 'Wiki dependency inventory plus external advisory data'
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
if ($repositoryAssessment) {
    $commands += [pscustomobject]@{
        id = 'assessment-architecture'; command = 'dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj'
        source = 'repository-assessment'; priority = 'required'; reason = 'repository-wide-architecture-lane'; commandEvidence = 'tests/FoodDiary.ArchitectureTests'
    }
    $commands += [pscustomobject]@{
        id = 'assessment-backend'; command = 'dotnet test FoodDiary.slnx'
        source = 'repository-assessment'; priority = 'recommended'; reason = 'repository-wide-backend-regression'; commandEvidence = 'FoodDiary.slnx'
    }
    $commands += [pscustomobject]@{
        id = 'assessment-frontend'; command = 'cd FoodDiary.Web.Client && npm run verify'
        source = 'repository-assessment'; priority = 'recommended'; reason = 'repository-wide-frontend-regression'; commandEvidence = 'FoodDiary.Web.Client/package.json'
    }
    $commands += [pscustomobject]@{
        id = 'assessment-migrations'; command = 'dotnet test tests/FoodDiary.Infrastructure.IntegrationTests/FoodDiary.Infrastructure.IntegrationTests.csproj'
        source = 'repository-assessment'; priority = 'required'; reason = 'repository-wide-migration-safety-lane'; commandEvidence = 'MigrationSafetyIntegrationTests.cs'
    }
    $commands += [pscustomobject]@{
        id = 'assessment-compose'; command = 'docker compose config --quiet'
        source = 'repository-assessment'; priority = 'recommended'; reason = 'repository-wide-deployment-declaration-lane'; commandEvidence = 'docker-compose.yml'
    }
    $commands += [pscustomobject]@{
        id = 'assessment-dependencies'; command = './.llm-wiki/wiki.ps1 dependencies -RepositoryWide'
        source = 'repository-assessment'; priority = 'required'; reason = 'repository-wide-dependency-inventory'; commandEvidence = 'tracked NuGet/npm manifests and lockfiles'
    }
}

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
if ($NoBaseline) {
    $commands = @($commands | Where-Object { [string]$_.command -notmatch '(?i)\bapi-compat\b' })
}
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
$executedChecksByCommand = @{}
foreach ($executedCheck in @($ExecutedCheck | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) {
    $executedChecksByCommand[(Normalize-LlmWikiVerificationCommand ([string]$executedCheck))] = $true
}
$frontendVerifyCommand = Normalize-LlmWikiVerificationCommand 'cd FoodDiary.Web.Client && npm run verify'
function Test-LlmWikiCommandCoverage([string]$CoveringCommand, [string]$RequiredCommand) {
    if ($CoveringCommand -ceq $RequiredCommand) { return $true }
    return $CoveringCommand -ceq $frontendVerifyCommand -and
        $RequiredCommand -match '^cd FoodDiary\.Web\.Client && npm run test:ci:(?:app|ui-kit|tour|admin)(?:\s|$)'
}
$commands = @($commands | ForEach-Object {
    $normalizedCommand = Normalize-LlmWikiVerificationCommand ([string]$_.command)
    $receipt = $validReceiptsByCommand[$normalizedCommand]
    $coveringExecutedCommand = @($executedChecksByCommand.Keys | Where-Object {
        Test-LlmWikiCommandCoverage $_ $normalizedCommand
    } | Select-Object -First 1)
    $coveringReceipt = @($validReceiptsByCommand.Keys | Where-Object {
        Test-LlmWikiCommandCoverage $_ $normalizedCommand
    } | ForEach-Object { $validReceiptsByCommand[$_] } | Select-Object -First 1)
    $executedInRequest = $coveringExecutedCommand.Count -gt 0
    if ($null -eq $receipt -and $coveringReceipt.Count -gt 0) { $receipt = $coveringReceipt[0] }
    $receiptEvidence = $null
    if ($executedInRequest) {
        $receiptEvidence = [pscustomobject]@{
            result = 'passed'
            source = $(if ($coveringExecutedCommand[0] -ceq $normalizedCommand) { 'executedChecks' } else { 'executedChecksCoverage' })
            coveredBy = [string]$coveringExecutedCommand[0]
            fingerprint = $null
        }
    } elseif ($null -ne $receipt) {
        $receiptEvidence = [pscustomobject]@{
            result = $receipt.result
            durationSeconds = $receipt.durationSeconds
            coverageScope = @($receipt.coverageScope)
            recordedAtUtc = $receipt.recordedAtUtc
            fingerprint = $receipt.fingerprint
        }
    }
    [pscustomobject][ordered]@{
        id = $_.id
        command = $_.command
        source = $_.source
        priority = $_.priority
        reason = $_.reason
        commandEvidence = $(if ($_.PSObject.Properties['commandEvidence']) { $_.commandEvidence } else { $null })
        status = $(if ($null -eq $receipt -and -not $executedInRequest) { 'pending' } else { 'satisfied' })
        receipt = $receiptEvidence
    }
})

$result = [pscustomobject]@{
    selectionMode = $(if ($repositoryAssessment) { 'repository-assessment' } else { 'change-focused' })
    baseline = [pscustomobject][ordered]@{
        available = -not [bool]$NoBaseline
        baseRevision = $(if ($NoBaseline) { $null } else { $BaseRef })
        headRevision = $(if ($PSBoundParameters.ContainsKey('HeadRef')) { $HeadRef } else { 'WORKTREE' })
        note = $(if ($NoBaseline) { 'Compatibility baseline was not supplied; API compatibility checks are omitted until baseRevision is provided.' } else { $null })
    }
    scopes = $scopes
    intent = $Intent
    proposedPaths = @($ProposedPath | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)
    modules = @($diff.modules | ForEach-Object { if ($_.PSObject.Properties['name']) { $_.name } } | Where-Object { $_ })
    focusedTestFiles = @($selectedFocusedTests | ForEach-Object { if ($_.PSObject.Properties['path']) { $_.path } } | Where-Object { $_ })
    focusedTestDetails = $selectedFocusedTests
    commands = @($commands)
    prerequisites = [pscustomobject][ordered]@{
        dotnetSdk = [pscustomobject]@{ required = $true; detected = [bool](Get-Command dotnet -ErrorAction SilentlyContinue); recovery = 'Install the repository-supported .NET SDK, then rerun commands without --no-restore on a cold checkout.' }
        nodeDependencies = [pscustomobject]@{ required = $repositoryAssessment; detected = [bool](Test-Path -LiteralPath (Join-Path $repositoryRoot 'FoodDiary.Web.Client/node_modules') -PathType Container); recovery = 'Run npm ci in FoodDiary.Web.Client before frontend verification.' }
        dockerCompose = [pscustomobject]@{ required = $false; detected = [bool](Get-Command docker -ErrorAction SilentlyContinue); recovery = 'Install/start Docker when Compose declaration validation is part of the assessment.' }
        providerBackedTests = [pscustomobject]@{ required = $false; detected = $null; recovery = 'Provider-backed integration tests may require Docker/testcontainers and network access; report a skipped prerequisite separately from a passing test.' }
    }
    commandGroups = [pscustomobject][ordered]@{
        required = @($commands | Where-Object { $_.priority -eq 'required' -and $_.status -eq 'pending' })
        recommended = @($commands | Where-Object { $_.priority -eq 'recommended' -and $_.status -eq 'pending' })
        fullRegression = @($commands | Where-Object { $_.priority -in @('contextual', 'full-regression') -and $_.status -eq 'pending' })
        satisfied = @($commands | Where-Object status -eq 'satisfied')
    }
    scenarios = @($scenarios)
    reviewObligations = @($policy.reviewObligations)
    repositoryAntipatterns = @($repositoryAntipatterns)
    warnings = @(
        $(if ($NoBaseline) { 'API compatibility baseline unavailable; provide baseRevision to enable compatibility checks.' })
        $(if ($repositoryAntipatterns.Count -gt 1) { "Repeated repository antipattern found in $($repositoryAntipatterns.Count) locations; review all matches, not only the changed file." })
    )
}

$resultOutput = if ($Compact) {
    [pscustomobject]@{
        compact = $true
        baseline = $result.baseline
        scopes = $result.scopes
        modules = $result.modules
        focusedTests = $result.focusedTestDetails
        commands = $result.commands
        prerequisites = $result.prerequisites
        scenarios = @($result.scenarios | Select-Object id, description)
        repositoryAntipatterns = $result.repositoryAntipatterns
        reviewObligationIds = @($result.reviewObligations | ForEach-Object { if ($_.PSObject.Properties['id']) { $_.id } } | Where-Object { $_ })
        warnings = $result.warnings
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
if (@($result.repositoryAntipatterns).Count -gt 0) {
    Write-Host ''
    Write-Host 'Related repository antipatterns:'
    foreach ($match in $result.repositoryAntipatterns) { Write-Host " - $($match.id): $($match.path):$($match.line)" }
}
