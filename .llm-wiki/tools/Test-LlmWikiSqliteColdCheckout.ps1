[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
# The read-only cache is now inside this fixture checkout. A short, independent
# root leaves room for both Git and SQLite's Windows path limits, including
# a nested isolated validation checkout.
$previousExternalSandbox = $env:LLM_WIKI_SMOKE_EXTERNAL_SANDBOX
$env:LLM_WIKI_SMOKE_EXTERNAL_SANDBOX = Join-Path $repositoryRoot '.artifacts/native'
$sandboxParent = Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository
$sandboxRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'cold' -IndependentRepository
$checkoutRoot = Join-Path $sandboxRoot 'r'
$shellPath = (Get-Process -Id $PID).Path
function Assert-ColdCheckout([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
try {
    & git clone --shared --quiet --no-checkout $repositoryRoot $checkoutRoot
    if ($LASTEXITCODE -ne 0) { throw 'Unable to create the cold-checkout fixture.' }
    & git -C $checkoutRoot checkout --quiet --force HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Unable to check out the cold-checkout fixture.' }
    $checkoutWiki = Join-Path $checkoutRoot '.llm-wiki'
    # Carry current Wiki sources and their shared build inputs without copying ignored outputs.
    $fixtureInputs = @(
        '.llm-wiki', 'global.json', 'Directory.Build.props', 'Directory.Packages.props', 'FoodDiary.Analyzers',
        ':(glob).nuget/lockfiles/LlmWiki.*.packages.lock.json',
        '.nuget/lockfiles/FoodDiary.Analyzers.packages.lock.json',
        'FoodDiary.Development.Mcp/Wiki/SqliteContextSearchReader.cs',
        'FoodDiary.Development.Mcp/Protocol/WikiContextSearchResult.cs',
        'FoodDiary.Development.Mcp/Protocol/WikiContextSearchCandidate.cs'
    )
    $fixturePrefix = [IO.Path]::GetFullPath($checkoutRoot).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    foreach ($path in @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments (@('ls-files','--cached','--others','--exclude-standard','--') + $fixtureInputs))) {
        $source = Join-Path $repositoryRoot $path
        $destination = [IO.Path]::GetFullPath((Join-Path $checkoutRoot $path))
        if (-not $destination.StartsWith($fixturePrefix,[StringComparison]::OrdinalIgnoreCase)) { throw 'Cold Wiki overlay escaped its fixture.' }
        if ([IO.File]::Exists($source)) {
            [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
            [IO.File]::Copy($source,$destination,$true)
        } elseif ([IO.File]::Exists($destination)) { [IO.File]::Delete($destination) }
    }
    Assert-ColdCheckout (-not (Test-Path (Join-Path $checkoutRoot '.llm-wiki/tools/roslyn-extractor/bin'))) 'Cold fixture inherited Roslyn build outputs.'
    Assert-ColdCheckout (-not (Test-Path (Join-Path $checkoutRoot 'FoodDiary.Web.Client/node_modules'))) 'Cold fixture unexpectedly has TypeScript dependencies.'
    $databasePath = Join-Path $checkoutRoot '.artifacts/llm-wiki/code-graph/code-graph.sqlite'
    Assert-ColdCheckout (-not (Test-Path $databasePath)) 'Cold fixture unexpectedly has a SQLite projection.'
    $facade = Join-Path $checkoutWiki 'wiki.ps1'
    $backendPath = 'Modules/Billing/Application/Commands/ProcessBillingWebhook/BillingWebhookEventProcessor.cs'
    $diff = & $shellPath -NoLogo -NoProfile -File $facade diff -ChangedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout (@($diff.modules.name) -contains 'Billing') 'Cold SQLite diff lost module ownership.'
    Assert-ColdCheckout (-not (Test-Path $databasePath)) 'Read-only facade wrote a SQLite database in the source checkout.'
    $brief = & $shellPath -NoLogo -NoProfile -File $facade brief -Intent 'Review billing webhook idempotency' -ProposedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Compact -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout ($brief.analysis.mode -eq 'planned-paths' -and $brief.analysis.impactIndex.source -eq 'sqlite-task-brief-impact') 'Cold backend brief lost SQLite evidence.'
    # Damage only this fixture's disposable SQL projection while leaving its
    # source/change-set fingerprint current. Planning must repair that projection.
    # Each private checkout stores its database under the dot-prefixed .artifacts
    # directory. PowerShell must include hidden directories when recursing on Unix.
    $privateDatabases = @(Get-ChildItem -LiteralPath (Join-Path $checkoutRoot '.artifacts/llm-wiki/read-only-snapshots') -Filter code-graph.sqlite -Recurse -File -Force | ForEach-Object FullName)
    Assert-ColdCheckout ($privateDatabases.Count -gt 0) 'Backend brief did not prepare a private projection.'
    $damageScript = Join-Path $checkoutRoot '.artifacts/stale-projection.mjs'
    [IO.File]::WriteAllText($damageScript, @'
import { DatabaseSync } from 'node:sqlite';
for (const path of process.argv.slice(2)) {
  const database = new DatabaseSync(path);
  database.prepare('UPDATE metadata SET value = ? WHERE key = ?').run('fixture-stale', 'query_source:risks');
  database.close();
}
'@)
    & node $damageScript @privateDatabases
    Assert-ColdCheckout ($LASTEXITCODE -eq 0) 'Unable to prepare the stale projection fixture.'
    $recoveredBrief = & $shellPath -NoLogo -NoProfile -File $facade brief -Intent 'Review billing webhook idempotency' -ProposedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Compact -Format Json -Limit 2 | ConvertFrom-Json
    Assert-ColdCheckout ($recoveredBrief.analysis.impactIndex.source -eq 'sqlite-task-brief-impact') 'Backend planning did not recover the stale private projection.'
    $journeys = & $shellPath -NoLogo -NoProfile -File $facade journeys -Intent 'Review billing webhook idempotency' -ProposedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout (@($journeys.journeys.id) -contains 'FD-BILLING') 'Cold journeys lost billing scenarios.'
    $plan = & $shellPath -NoLogo -NoProfile -File $facade test-plan -Intent 'Review billing webhook idempotency' -ProposedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout (@($plan.scenarios).Count -gt 0) 'Cold SQLite test-plan omitted scenarios.'
    $design = & $shellPath -NoLogo -NoProfile -File $facade design -Intent 'Review billing webhook idempotency' -ProposedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout ($null -ne $design.behaviorSpecification) 'Cold backend design did not complete its assessment.'
    $topology = & $shellPath -NoLogo -NoProfile -File $facade topology -ChangedPath $backendPath -BackendOnlyRefresh -ChangeType Backend -Format Json -Limit 3 | ConvertFrom-Json
    Assert-ColdCheckout ($null -ne $topology) 'Cold backend topology did not complete.'
    foreach ($index in @('catalog','symbols','frontend','frontend-contract','backend-contract','architecture-health','domain-data','configuration','quality','runtime','sensitive-data','modules')) {
        $result = & $shellPath -NoLogo -NoProfile -File $facade $index -BackendOnlyRefresh -Format Json -Limit 2 | ConvertFrom-Json
        Assert-ColdCheckout ($result.compiledIndex.source -eq 'sqlite-standalone-index' -and $result.compiledIndex.fresh) "Cold index '$index' lost SQLite or freshness."
    }
    Assert-ColdCheckout (-not (Test-Path $databasePath)) 'Standalone read-only indexes modified the source checkout.'
    $frontendDiagnostics = [Collections.Generic.List[string]]::new()
    $frontendFailed = $false
    try {
        & $shellPath -NoLogo -NoProfile -File $facade context -Query DashboardComponent -ChangeType Frontend -Format Json -Limit 2 2>&1 | ForEach-Object { $frontendDiagnostics.Add($_.ToString()) }
        $frontendFailed = $LASTEXITCODE -ne 0
    } catch { $frontendFailed = $true; $frontendDiagnostics.Add($_.Exception.Message) }
    Assert-ColdCheckout ($frontendFailed -and ($frontendDiagnostics -join "`n") -match 'TypeScript|typescript|npm ci') `
        "Missing frontend prerequisites did not produce an explicit dependency error: $($frontendDiagnostics -join ' ')"
    $global:LASTEXITCODE = 0
    Write-Host 'SQLite cold checkout passed: backend planning and all 12 indexes, source isolation, explicit frontend dependency failure.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($sandboxRoot)
    $resolvedParent = [IO.Path]::GetFullPath($sandboxParent).TrimEnd('\','/')
    try {
        if ([IO.Path]::GetDirectoryName($resolvedFixture) -cne $resolvedParent -or
            [IO.Path]::GetFileName($resolvedFixture) -notmatch '^cold-[a-f0-9]{32}$') { throw 'Unsafe cold-checkout fixture path.' }
        if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
    } finally { $env:LLM_WIKI_SMOKE_EXTERNAL_SANDBOX = $previousExternalSandbox }
}
