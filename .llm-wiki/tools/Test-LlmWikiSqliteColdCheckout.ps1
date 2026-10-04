[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$sandboxRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'sqlite-cold-checkout'
$checkoutRoot = Join-Path $sandboxRoot 'repository'
function Assert-ColdCheckout([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
try {
    & git clone --shared --quiet --no-checkout $repositoryRoot $checkoutRoot
    if ($LASTEXITCODE -ne 0) { throw 'Unable to create the cold-checkout fixture.' }
    & git -C $checkoutRoot checkout --quiet --force HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unable to check out the cold-checkout fixture.' }
    $checkoutWiki = Join-Path $checkoutRoot '.llm-wiki'
    Get-ChildItem -LiteralPath (Join-Path $repositoryRoot '.llm-wiki') -Force | Copy-Item -Destination $checkoutWiki -Recurse -Force
    Assert-ColdCheckout (-not (Test-Path (Join-Path $checkoutRoot 'FoodDiary.Web.Client/node_modules'))) 'Cold fixture unexpectedly has TypeScript dependencies.'
    $databasePath = Join-Path $checkoutRoot '.artifacts/llm-wiki/code-graph/code-graph.sqlite'
    Assert-ColdCheckout (-not (Test-Path $databasePath)) 'Cold fixture unexpectedly has a SQLite projection.'
    $facade = Join-Path $checkoutWiki 'wiki.ps1'
    $backendPath = 'Modules/Billing/Application/Commands/ProcessBillingWebhook/BillingWebhookEventProcessor.cs'
    $diff = & $facade diff -ChangedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout (@($diff.modules.name) -contains 'Billing') 'Cold SQLite diff lost module ownership.'
    Assert-ColdCheckout (-not (Test-Path $databasePath)) 'Read-only facade wrote a SQLite database in the source checkout.'
    $brief = & $facade brief -Intent 'Review billing webhook idempotency' -ProposedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Compact -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout ($brief.analysis.mode -eq 'planned-paths' -and $brief.analysis.impactIndex.source -eq 'sqlite-task-brief-impact') 'Cold backend brief lost SQLite evidence.'
    $journeys = & $facade journeys -Intent 'Review billing webhook idempotency' -ProposedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout (@($journeys.journeys.id) -contains 'FD-BILLING') 'Cold journeys lost billing scenarios.'
    $plan = & $facade test-plan -Intent 'Review billing webhook idempotency' -ProposedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout (@($plan.scenarios).Count -gt 0) 'Cold SQLite test-plan omitted scenarios.'
    foreach ($index in @('catalog','symbols','frontend','frontend-contract','backend-contract','architecture-health','domain-data','configuration','quality','runtime','sensitive-data','modules')) {
        $result = & $facade $index -BackendOnlyRefresh -Format Json -Limit 2 | ConvertFrom-Json
        Assert-ColdCheckout ($result.compiledIndex.source -eq 'sqlite-standalone-index' -and $result.compiledIndex.fresh) "Cold index '$index' lost SQLite or freshness."
    }
    Assert-ColdCheckout (-not (Test-Path $databasePath)) 'Standalone read-only indexes modified the source checkout.'
    $frontendDiagnostics = [Collections.Generic.List[string]]::new()
    $frontendFailed = $false
    try {
        & $facade context -Query DashboardComponent -ChangeType Frontend -Format Json -Limit 2 2>&1 | ForEach-Object { $frontendDiagnostics.Add($_.ToString()) }
        $frontendFailed = $LASTEXITCODE -ne 0
    } catch { $frontendFailed = $true; $frontendDiagnostics.Add($_.Exception.Message) }
    Assert-ColdCheckout ($frontendFailed -and ($frontendDiagnostics -join "`n") -match 'TypeScript|typescript|npm ci') `
        "Missing frontend prerequisites did not produce an explicit dependency error: $($frontendDiagnostics -join ' ')"
    $global:LASTEXITCODE = 0
    Write-Host 'SQLite cold checkout passed: backend planning and all 12 indexes, source isolation, explicit frontend dependency failure.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($sandboxRoot)
    $artifactPrefix = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts')).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedFixture.StartsWith($artifactPrefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe cold-checkout fixture path.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
