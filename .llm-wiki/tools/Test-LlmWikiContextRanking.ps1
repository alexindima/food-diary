[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$contextTool = Join-Path $PSScriptRoot 'Find-LlmWikiContext.ps1'
$null = & $manager build -Format Json
foreach ($moduleName in @('Billing', 'Products', 'Recipes')) {
    $apiContext = & $contextTool -Module $moduleName -ChangeType Api -Limit 8 -Format Json | ConvertFrom-Json
    $moduleTestPrefix = "Modules/$moduleName/tests/"
    if (@($apiContext.tests | Where-Object { $_.path.StartsWith($moduleTestPrefix, [StringComparison]::Ordinal) }).Count -eq 0) {
        throw "$moduleName API context lost focused module tests behind production candidates."
    }
    $productionSearch = & $manager -Action search -Query $moduleName -Module $moduleName -ChangeType Api -Limit 50 -SkipRefresh -Format Json | ConvertFrom-Json
    foreach ($candidate in $apiContext.candidates) {
        $original = @($productionSearch.records | Where-Object path -eq $candidate.path)
        if ($original.Count -ne 1 -or $candidate.score -ne $original[0].score -or
            $candidate.rank -ne $original[0].rank -or $candidate.confidence -cne $original[0].confidence) {
            throw "$moduleName test-context retrieval changed a production candidate's ranking."
        }
    }
}
$context = & $contextTool `
    -Module Recipes `
    -Query 'Recipe nutrition updater' `
    -ScopePath 'Modules/Recipes/Application' `
    -Limit 12 `
    -SkipQueryCache `
    -Format Json | ConvertFrom-Json

$topCandidate = @($context.candidates | Select-Object -First 1)
if ($topCandidate.Count -ne 1 -or $topCandidate[0].path -notmatch 'RecipeNutritionUpdater\.cs$') {
    throw 'SQLite context did not rank RecipeNutritionUpdater first.'
}
if ($context.compiledIndex.source -ne 'sqlite-search' -or -not $context.compiledIndex.fresh -or
    @($context.candidates | Where-Object path -notlike 'Modules/Recipes/Application/*').Count -gt 0 -or
    [double]$context.compiledIndex.sqlDurationMs -lt 0 -or [double]$context.compiledIndex.roundTripDurationMs -lt 0) {
    throw 'SQLite context lost scope, freshness or non-negative query and transport timings.'
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$graphStatus = & $manager status -SkipRefresh -Format Json | ConvertFrom-Json
$normalizationCases = @(
    @{ query = 'Где клиент разбирает JWT и извлекает срок истечения токена?'; path = 'FoodDiary.Web.Client/src/app/services/jwt-decoder.service.ts' },
    @{ query = 'Как клиент выполняет разбор JWT и узнаёт когда токен истекает?'; path = 'FoodDiary.Web.Client/src/app/services/jwt-decoder.service.ts' },
    @{ query = 'Где браузер перезагружается после ошибки восстановления версии приложения?'; path = 'FoodDiary.Web.Client/src/app/shared/service-worker/app-version-recovery.service.ts' },
    @{ query = 'Где браузер восстанавливает версию приложения через перезагрузку?'; path = 'FoodDiary.Web.Client/src/app/shared/service-worker/app-version-recovery.service.ts' },
    @{ query = 'Where does the client parse JWT payload expiration?'; path = 'FoodDiary.Web.Client/src/app/services/jwt-decoder.service.ts' },
    @{ query = 'Где клиент разбирает настройки версии приложения?'; path = $null }
)
foreach ($case in $normalizationCases) {
    $nodeResult = & $manager search -Query $case.query -Limit 10 -SkipRefresh -Format Json | ConvertFrom-Json
    $readerResult = [LlmWiki.SqliteReader.ContextSearchReader]::Search(
        $repositoryRoot, $case.query, 10, 'Any', '', [string[]]@(), [string]$graphStatus.currentChangeSetFingerprint
    ) | ConvertFrom-Json
    if (-not $nodeResult.ready -or -not $readerResult.ready -or
        (@($readerResult.queryTerms) -join "`0") -cne (@($nodeResult.queryTerms) -join "`0") -or
        (@($readerResult.records.path) -join "`0") -cne (@($nodeResult.records.path) -join "`0")) {
        throw "Conversational normalization changed Node/SQLite reader parity: $($case.query)"
    }
    if ($case.path -and @($readerResult.records | Where-Object { $_.path -ceq $case.path -and $_.rank -le 5 }).Count -ne 1) {
        throw "Conversational normalization lost the implementation owner: $($case.query)"
    }
    if (-not $case.path -and @($readerResult.queryTerms | Where-Object { $_ -in @('decode', 'decoder', 'payload') }).Count -gt 0) {
        throw 'Ordinary parsing incorrectly acquired JWT decoding context.'
    }
}

$serviceCases = @(
    @{ query = 'frontend service loads and marks notifications'; path = 'FoodDiary.Web.Client/src/app/shared/notifications/notification.service.ts' },
    @{ query = 'FoodDiary.Web.Client/src/app/shared/api/sdk/generated/api/notifications.service.ts'; path = 'FoodDiary.Web.Client/src/app/shared/api/sdk/generated/api/notifications.service.ts' },
    @{ query = 'FoodDiary.Web.Client/projects/fooddiary-admin/src/app/shared/api/sdk/generated/api/admin-users.service.ts'; path = 'FoodDiary.Web.Client/projects/fooddiary-admin/src/app/shared/api/sdk/generated/api/admin-users.service.ts' }
)
foreach ($case in $serviceCases) {
    $nodeResult = & $manager search -Query $case.query -ChangeType Frontend -Limit 10 -SkipRefresh -Format Json | ConvertFrom-Json
    $readerResult = [LlmWiki.SqliteReader.ContextSearchReader]::Search(
        $repositoryRoot, $case.query, 10, 'Frontend', '', [string[]]@(), [string]$graphStatus.currentChangeSetFingerprint
    ) | ConvertFrom-Json
    if (-not $nodeResult.ready -or -not $readerResult.ready -or
        (@($readerResult.records.path) -join "`0") -cne (@($nodeResult.records.path) -join "`0") -or
        @($readerResult.records)[0].path -cne $case.path) {
        throw "Application service and generated SDK ranking lost its owner or reader parity: $($case.query)"
    }
    foreach ($candidate in @($nodeResult.records) + @($readerResult.records)) {
        if ($candidate.path -like '*/api/sdk/generated/*' -and
            @($candidate.reasons | Where-Object { $_ -like 'structural role frontend-api-service-role*' }).Count -gt 0) {
            throw "Generated SDK received an application-service role boost: $($candidate.path)"
        }
    }
}

Write-Host 'LLM Wiki SQLite context ranking passed: exact identity, scope, focused tests, contextual normalization and generated SDK roles with reader parity.'
