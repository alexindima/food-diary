[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixtureParent = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki/frontend-inventory-tests'))
$fixtureRoot = Join-Path $fixtureParent ([guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixtureRoot -Force

function Write-FixtureFile([string]$Path, [string]$Content) {
    $target = Join-Path $fixtureRoot $Path
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force
    [IO.File]::WriteAllText($target, $Content, [Text.UTF8Encoding]::new($false))
}

function Invoke-FixtureGenerator([string]$Name, [switch]$Check, [switch]$ReuseUnchangedCheck) {
    $global:LASTEXITCODE = 0
    $output = & (Join-Path $fixtureRoot ".llm-wiki/tools/Build-LlmWiki${Name}Index.ps1") -Check:$Check -ReuseUnchangedCheck:$ReuseUnchangedCheck 6>&1
    [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = ($output -join "`n") }
}

function Read-FixtureIndex([string]$Name) {
    [IO.File]::ReadAllText((Join-Path $fixtureRoot ".llm-wiki/generated/$Name-index.json")) | ConvertFrom-Json
}

try {
    foreach ($path in @(
        '.llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1',
        '.llm-wiki/tools/Build-LlmWikiFrontendContractIndex.ps1',
        '.llm-wiki/tools/Build-LlmWikiArchitectureHealthIndex.ps1',
        '.llm-wiki/tools/LlmWikiJson.ps1', '.llm-wiki/tools/LlmWikiGitPaths.ps1',
        '.llm-wiki/tools/LlmWikiIndexCache.ps1',
        '.llm-wiki/generated/repository-catalog.json', '.llm-wiki/generated/backend-contract-index.json',
        '.llm-wiki/generated/quality-index.json',
        'docs/architecture/module-dependencies.json', 'docs/architecture/backend-modules.json',
        'Tooling/tests/FoodDiary.ArchitectureTests/ProjectDependencyMatrixTests.cs'
    )) { Write-FixtureFile $path ([IO.File]::ReadAllText((Join-Path $repositoryRoot $path))) }
    Write-FixtureFile '.gitignore' "node_modules/`ndist*/`ncoverage/`n.angular/`ntest-results/`nplaywright-report/`nstorybook-static/`ngenerated/`n.artifacts/`n"
    Write-FixtureFile 'FoodDiary.Web.Client/assets/i18n/en/test.json' '{"TITLE":"Fixture"}'
    Write-FixtureFile 'FoodDiary.Web.Client/assets/i18n/ru/test.json' '{"TITLE":"Fixture"}'
    Write-FixtureFile 'FoodDiary.Web.Client/src/app/features/fixture/widget.ts' "@Component({ selector: 'fixture-widget', templateUrl: './widget.html' })`nexport class WidgetComponent { readonly value = input<string>(); readonly changed = output<string>(); }"
    Write-FixtureFile 'FoodDiary.Web.Client/src/app/features/fixture/widget.html' '<span>{{ "TITLE" | translate }}</span>'
    Write-FixtureFile 'FoodDiary.Web.Client/src/app/features/fixture/view.html' '<fixture-widget [value]="text"></fixture-widget>'
    Write-FixtureFile 'FoodDiary.Web.Client/src/app/features/fixture/widget.spec.ts' 'export class WidgetSpec {}'
    Write-FixtureFile 'FoodDiary.Web.Client/src/app/features/fixture/route.ts' "@Component({ selector: 'fixture-route' })`nexport class RouteComponent {}"
    Write-FixtureFile 'FoodDiary.Web.Client/src/app/features/fixture/next.ts' "@Component({ selector: 'fixture-next' })`nexport class NextComponent {}"
    Write-FixtureFile 'FoodDiary.Web.Client/src/app/app.routes.ts' "export const routes = [{ path: 'fixture', component: RouteComponent }];"
    Write-FixtureFile 'FoodDiary.Web.Client/src/app/deleted.ts' 'export class DeletedComponent {}'
    Write-FixtureFile 'FoodDiary.Web.Client/generated/kept.TS' 'export class TrackedGeneratedClass {}'
    & git -C $fixtureRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize frontend inventory fixture.' }
    & git -C $fixtureRoot -c core.autocrlf=false add .
    & git -C $fixtureRoot -c core.autocrlf=false add --force -- FoodDiary.Web.Client/generated/kept.TS
    if ($LASTEXITCODE -ne 0) { throw 'Unable to stage the tracked ignored source fixture.' }
    Remove-Item -LiteralPath (Join-Path $fixtureRoot 'FoodDiary.Web.Client/src/app/deleted.ts')
    $unicodeName = -join @([char]0x0444, [char]0x0430, [char]0x0439, [char]0x043B)
    Write-FixtureFile "FoodDiary.Web.Client/src/app/features/fixture/$unicodeName with spaces.TS" 'export class UnicodeClass {}'
    foreach ($name in @('Frontend', 'FrontendContract', 'ArchitectureHealth')) {
        $result = Invoke-FixtureGenerator $name
        if ($result.ExitCode -ne 0) { throw "Fixture generation failed: $name $($result.Output)" }
    }
    $frontend = Read-FixtureIndex 'frontend'
    if ('DeletedComponent' -in $frontend.symbols.name -or
        'TrackedGeneratedClass' -notin $frontend.symbols.name -or 'UnicodeClass' -notin $frontend.symbols.name -or
        $frontend.summary.specs -ne 1) { throw 'Frontend inventory lost tracked, new, uppercase, Unicode or spec sources, or retained a deleted source.' }
    $contracts = Read-FixtureIndex 'frontend-contract'
    if ($contracts.summary.consumerEdges -ne 1 -or $contracts.consumerEdges[0].component -ne 'WidgetComponent') {
        throw 'Frontend contract inventory changed the real template consumer.'
    }
    $health = Read-FixtureIndex 'architecture-health'
    if ('RouteComponent' -in $health.selectorUnreferencedComponents.class) { throw 'Architecture health lost the routed component.' }
    $before = @{}
    foreach ($name in @('frontend','frontend-contract','architecture-health')) {
        $before[$name] = [IO.File]::ReadAllText((Join-Path $fixtureRoot ".llm-wiki/generated/$name-index.json"))
    }
    foreach ($directory in @('node_modules','dist-extra','coverage','.angular','test-results/traces','playwright-report','storybook-static')) {
        Write-FixtureFile "FoodDiary.Web.Client/$directory/noise.ts" "@Component({ selector: 'noise' })`nexport class NoiseComponent {}"
        Write-FixtureFile "FoodDiary.Web.Client/$directory/noise.html" '<fixture-widget></fixture-widget>'
        Write-FixtureFile "FoodDiary.Web.Client/$directory/noise.routes.ts" 'export const routes = [{ component: NoiseRouteComponent }];'
    }
    foreach ($name in @('Frontend','FrontendContract','ArchitectureHealth')) {
        $cached = Invoke-FixtureGenerator $name -Check -ReuseUnchangedCheck
        if ($cached.ExitCode -ne 0 -or $cached.Output -notmatch 'cache hit') { throw "Ignored artifacts invalidated the $name source receipt." }
        $strict = Invoke-FixtureGenerator $name -Check
        if ($strict.ExitCode -ne 0) { throw "Ignored artifacts leaked into strict $name generation: $($strict.Output)" }
    }
    foreach ($name in $before.Keys) {
        if ([IO.File]::ReadAllText((Join-Path $fixtureRoot ".llm-wiki/generated/$name-index.json")) -cne $before[$name]) {
            throw "Ignored artifacts changed $name output."
        }
    }
    Write-FixtureFile 'FoodDiary.Web.Client/src/app/app.routes.ts' "export const routes = [{ path: 'fixture', component: NextComponent }];"
    $stale = Invoke-FixtureGenerator 'ArchitectureHealth' -Check -ReuseUnchangedCheck
    if ($stale.ExitCode -ne 1) { throw 'Route source edits incorrectly reused architecture-health freshness.' }
    $updated = Invoke-FixtureGenerator 'ArchitectureHealth'
    if ($updated.ExitCode -ne 0) { throw 'Unable to update the architecture-health route fixture.' }
    $health = Read-FixtureIndex 'architecture-health'
    if ('RouteComponent' -notin $health.selectorUnreferencedComponents.class -or
        'NextComponent' -in $health.selectorUnreferencedComponents.class) { throw 'Architecture health did not update route consumer evidence.' }
    Write-Host 'LLM Wiki frontend inventory passed: Git sources, ignored artifacts, exact output stability and route-cache invalidation.'
} finally {
    $resolved = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolved.StartsWith($fixtureParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to remove a frontend fixture outside its dedicated parent.'
    }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
