[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Test-LlmWikiApplicationModulePaths.ps1')
$readinessToolText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Raw
if (-not $readinessToolText.Contains('$reusable = -not $CompileProbe -or [bool]$cached.compileProbe.passed') -or
    -not $readinessToolText.Contains('$cachePath -and (-not $CompileProbe -or [bool]$compileProbeResult.passed)')) {
    throw 'Extraction readiness must not reuse or persist interrupted/failed compile probes.'
}
$result = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Users -Format Json | ConvertFrom-Json
if ($result.contractReadiness.aggregateBlockers -ne 0) { throw 'Current IUserContextService should have no aggregate blockers.' }
if (-not $result.moduleReadiness.ready) { throw "Users module should be extraction-ready after external aggregate and mutation consumers are removed: $($result.moduleReadiness.blockers -join '; ')" }
if (@($result.moduleReadiness.leakingContracts) -contains 'IUserDirectoryService') { throw 'Removed IUserDirectoryService must not remain in extraction readiness.' }
if (@($result.moduleReadiness.leakingContracts) -notcontains 'IUserLookupRepository') { throw 'Boundary scan missed an inherited transitive wrapper.' }
if ($result.categories.transitiveWrapper -lt 1) { throw 'Inherited aggregate wrappers must be categorized separately.' }
if (@($result.leaks | Where-Object contract -eq 'IUserDirectoryService').Count -ne 0) { throw 'Removed IUserDirectoryService must have no consumers.' }
if (@($result.moduleReadiness.blockers).Count -ne 0) { throw 'Extraction-ready module must have no blockers.' }
if ($result.contractReadiness.mutationBlockers -ne 0) { throw 'Owner-internal IUserContextService mutations must not block extraction.' }
if (-not $result.contractReadiness.aggregateReady) { throw 'Contract and module readiness were not separated.' }
$dietologist = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Dietologist -Format Json | ConvertFrom-Json
if ($dietologist.dependencyReadiness.sourceFileCount -le 0 -or
    @($dietologist.dependencyReadiness.sourceRoots) -notcontains 'Modules/Dietologist/Application') {
    throw 'Logical application readiness must scan real moved sources, never an empty donor folder.'
}
if ($dietologist.contractReadiness.mutationBlockers -ne 0) { throw 'Users-owned mutation consumers must not block unrelated module extraction.' }
if (-not $dietologist.moduleReadiness.ready) { throw "Dietologist should be extraction-ready after cross-feature dependencies are removed: $($dietologist.moduleReadiness.blockers -join '; ')" }
$probe = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Dietologist -CompileProbe -Format Json | ConvertFrom-Json
if (-not $probe.compileProbe.passed) { throw "Dietologist extraction compile probe failed: $($probe.compileProbe.diagnostics -join '; ')" }
if (-not $probe.dependencyReadiness.ready -or @($probe.dependencyReadiness.actualModules).Count -ne 0) { throw 'Dietologist dependency scan must be clean before extraction.' }

$bodyMetrics = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module BodyMetrics -Format Json | ConvertFrom-Json
if (-not $bodyMetrics.moduleReadiness.ready -or -not $bodyMetrics.dependencyReadiness.ready) {
    throw "BodyMetrics internal feature namespaces must not be treated as external module dependencies: $($bodyMetrics.moduleReadiness.blockers -join '; ')"
}
if (@($bodyMetrics.dependencyReadiness.internalFeatureNamespaces) -notcontains 'WeightEntries' -or
    @($bodyMetrics.dependencyReadiness.internalFeatureNamespaces) -notcontains 'WaistEntries') {
    throw 'BodyMetrics readiness did not discover both logical features owned by its physical source set.'
}
if (@($bodyMetrics.dependencyReadiness.actualModules) -contains 'WeightEntries' -or
    @($bodyMetrics.dependencyReadiness.actualModules) -contains 'WaistEntries') {
    throw 'BodyMetrics readiness still reports assembly-internal logical features as external dependencies.'
}

$recipesProject = Join-Path (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path 'Modules/Recipes/Application/FoodDiary.Modules.Recipes.Application.csproj'
if (Test-Path -LiteralPath $recipesProject -PathType Leaf) {
    $recipes = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Recipes -Format Json | ConvertFrom-Json
    $recipeRegistrations = @($recipes.dependencyReadiness.diRegistrations)
    if (@($recipeRegistrations | Where-Object { $_.path -like 'Modules/Recipes/*' }).Count -gt 0) {
        throw 'Module registration definitions must not be counted as external composition-root calls.'
    }
    foreach ($compositionRoot in @(
        'FoodDiary.Initializer/Program.cs'
        'FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs'
    )) {
        if ($compositionRoot -notin @($recipeRegistrations.path)) { throw "Extracted Recipes DI registration was not found in $compositionRoot." }
    }
    if (@($recipeRegistrations | Where-Object kind -eq 'module-extension-call').Count -ne 2) {
        throw "Expected exactly two extracted Recipes composition registrations, found $(@($recipeRegistrations).Count)."
    }
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'extraction-readiness'
$fixturePath = Join-Path $fixtureRoot 'DashboardLeak.cs'
try {
    $null = New-Item -ItemType Directory -Path $fixtureRoot -Force
    [IO.File]::WriteAllText($fixturePath, "using FoodDiary.Application.Dashboard.Models;`nnamespace FoodDiary.Application.Dietologist.Tests;`ninternal sealed class DashboardLeak;`n", [Text.UTF8Encoding]::new($false))
    $relativeFixture = $fixturePath.Substring($repositoryRoot.Length + 1).Replace('\', '/')
    $withLeak = & (Join-Path $PSScriptRoot 'Get-LlmWikiExtractionReadiness.ps1') -Module Dietologist -DependencyFixturePath $relativeFixture -Format Json | ConvertFrom-Json
    if (@($withLeak.dependencyReadiness.actualModules) -notcontains 'Dashboard') { throw 'Universal dependency scan missed a Dashboard namespace/type reference.' }
    if ($withLeak.moduleReadiness.ready) { throw 'A cross-feature source dependency must block physical extraction readiness.' }
} finally {
    Remove-Item -LiteralPath $fixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
}
$probeFixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'extraction-probe-donor'
$previousProbeSandbox = $env:LLM_WIKI_SMOKE_SANDBOX
try {
    $fixtureTools = Join-Path $probeFixtureRoot '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $fixtureTools -Force
    foreach ($toolName in @('Get-LlmWikiExtractionReadiness.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiApplicationModulePaths.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $toolName) -Destination (Join-Path $fixtureTools $toolName)
    }
    $encoding = [Text.UTF8Encoding]::new($false)
    [IO.File]::WriteAllText((Join-Path $fixtureTools 'Get-LlmWikiContractConsumers.ps1'), @'
param($Contract, $Format)
'{"readiness":{"aggregateConsumers":0},"consumers":[]}'
'@, $encoding)
    # Only external graph metadata is stubbed; the readiness tool, Git discovery and all builds are real.
    [IO.File]::WriteAllText((Join-Path $fixtureTools 'Manage-LlmWikiCodeGraph.ps1'), @'
param($Action, $Format)
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$hashes = @(Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
    $_.Extension -in @('.cs', '.csproj') -and -not $_.FullName.StartsWith((Join-Path $root '.artifacts') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
} | Sort-Object FullName | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
$bytes = [Text.Encoding]::UTF8.GetBytes($hashes -join '|')
$fingerprint = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
@{ fingerprint = $fingerprint } | ConvertTo-Json -Compress
'@, $encoding)
    [IO.File]::WriteAllText((Join-Path $probeFixtureRoot '.gitignore'), ".artifacts/`n", $encoding)
    [IO.File]::WriteAllText((Join-Path $probeFixtureRoot 'Directory.Build.props'), @'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
'@, $encoding)
    [IO.File]::WriteAllText((Join-Path $probeFixtureRoot 'Directory.Build.targets'), '<Project />', $encoding)
    Copy-Item -LiteralPath (Join-Path $repositoryRoot 'Directory.Packages.props') -Destination $probeFixtureRoot
    foreach ($project in @(
        'FoodDiary.Application.Abstractions/FoodDiary.Application.Abstractions.csproj'
        'Shared/FoodDiary.Mediator/FoodDiary.Mediator.csproj'
        'Owner & Contracts/Sample.Owner.csproj'
    )) {
        $absolute = Join-Path $probeFixtureRoot $project
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $absolute) -Force
        [IO.File]::WriteAllText($absolute, '<Project Sdk="Microsoft.NET.Sdk" />', $encoding)
    }
    [IO.File]::WriteAllText((Join-Path $probeFixtureRoot 'Owner & Contracts/Marker.cs'),
        'namespace Sample.Owner; public sealed class Marker { }', $encoding)
    $sourceDirectory = Join-Path $probeFixtureRoot 'FoodDiary.Application/ProbeFixture'
    $null = New-Item -ItemType Directory -Path $sourceDirectory -Force
    $probeSource = Join-Path $sourceDirectory 'UsesOwner.cs'
    [IO.File]::WriteAllText($probeSource, @'
extern alias owner;
namespace FoodDiary.Application.ProbeFixture;
public sealed class UsesOwner { public owner::Sample.Owner.Marker Value { get; } = new(); }
'@, $encoding)
    [IO.File]::WriteAllText((Join-Path $probeFixtureRoot 'FoodDiary.Application/FoodDiary.Application.csproj'), @'
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="../Owner &amp; Contracts/Sample.Owner.csproj" Condition="'$(TargetFramework)' == 'net10.0'" Aliases="owner" />
    <ProjectReference Include="../Absent/NotForThisTarget.csproj" Condition="'$(TargetFramework)' != 'net10.0'" />
  </ItemGroup>
</Project>
'@, $encoding)
    & git -C $probeFixtureRoot init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize isolated compile-probe fixture.' }
    $env:LLM_WIKI_SMOKE_SANDBOX = Join-Path $probeFixtureRoot '.artifacts/probes'
    $fixtureTool = Join-Path $fixtureTools 'Get-LlmWikiExtractionReadiness.ps1'
    $successful = & $fixtureTool -Module ProbeFixture -CompileProbe -Format Json | ConvertFrom-Json
    if (-not $successful.compileProbe.passed -or $successful.dependencyReadiness.sourceFileCount -ne 1 -or
        @($successful.dependencyReadiness.applicationProjects).Count -ne 0) {
        throw "Synthetic donor probe must compile its selected source with the conditional aliased owner reference: $($successful.compileProbe.diagnostics -join '; ')"
    }
    foreach ($retired in @('FoodDiary.Domain/FoodDiary.Domain.csproj', 'Shared/FoodDiary.Nutrition.Domain/FoodDiary.Nutrition.Domain.csproj')) {
        if (Test-Path -LiteralPath (Join-Path $probeFixtureRoot $retired)) { throw "Fixture unexpectedly contains retired project: $retired" }
    }
    $fixtureCache = Join-Path $probeFixtureRoot '.artifacts/llm-wiki/extraction-readiness-cache'
    $successfulCacheCount = @(Get-ChildItem -LiteralPath $fixtureCache -File).Count
    if ($successfulCacheCount -ne 1) { throw 'Successful fixture probe must exercise the enabled cache branch.' }
    [IO.File]::AppendAllText($probeSource, "`npublic sealed class InvalidInput { public MissingOwnerType Value { get; } = new(); }`n", $encoding)
    $failed = & $fixtureTool -Module ProbeFixture -CompileProbe -Format Json | ConvertFrom-Json
    $failedAgain = & $fixtureTool -Module ProbeFixture -CompileProbe -Format Json | ConvertFrom-Json
    foreach ($failure in @($failed, $failedAgain)) {
        if ($failure.compileProbe.passed -or $failure.compileProbe.exitCode -eq 0 -or
            ($failure.compileProbe.diagnostics -join ' ') -notmatch 'CS0246') {
            throw 'Invalid selected source must preserve native compiler failure and diagnostics.'
        }
    }
    if ($failed.compileProbe.projectPath -eq $failedAgain.compileProbe.projectPath -or
        @(Get-ChildItem -LiteralPath $fixtureCache -File).Count -ne $successfulCacheCount) {
        throw 'Failed probes must neither be cached nor reused; each retry must perform a new native compilation.'
    }
    Remove-Item -LiteralPath (Join-Path $probeFixtureRoot 'FoodDiary.Application/FoodDiary.Application.csproj')
    $withoutDonor = & $fixtureTool -Module ProbeFixture -CompileProbe -Format Json | ConvertFrom-Json
    if ($withoutDonor.compileProbe.passed -or ($withoutDonor.compileProbe.diagnostics -join ' ') -notmatch 'CS0430') {
        throw 'Missing donor references must preserve the native missing alias error.'
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Get-LlmWikiContractConsumers.ps1') -Destination $fixtureTools -Force
    foreach ($owner in @('Users', 'Meals')) {
        $ownerDirectory = Join-Path $probeFixtureRoot "Modules/$owner/Application"
        $null = New-Item -ItemType Directory -Path $ownerDirectory -Force
        [IO.File]::WriteAllText((Join-Path $ownerDirectory "FoodDiary.Application.$owner.csproj"), '<Project Sdk="Microsoft.NET.Sdk" />', $encoding)
        [IO.File]::WriteAllText((Join-Path $ownerDirectory 'Consumer.cs'), "namespace FoodDiary.Application.$owner; public sealed class Consumer(IUserContextService context) { public Task ChangeAsync() => context.UpdateUserAsync(); }", $encoding)
    }
    [IO.File]::WriteAllText((Join-Path $probeFixtureRoot 'Modules/Users/Application/IUserContextService.cs'), "public interface IUserContextService {`n Task UpdateUserAsync();`n}", $encoding)
    $externalMutation = & $fixtureTool -Module Users -Format Json | ConvertFrom-Json
    if ($externalMutation.contractReadiness.mutationBlockers -ne 1 -or $externalMutation.moduleReadiness.ready) {
        throw 'A real external Meals mutation must still block Users readiness while its owner-internal mutation remains excluded.'
    }
} finally {
    $env:LLM_WIKI_SMOKE_SANDBOX = $previousProbeSandbox
    $resolvedFixture = [IO.Path]::GetFullPath($probeFixtureRoot)
    $expectedSandbox = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot)).TrimEnd('\', '/')
    if (-not $resolvedFixture.StartsWith($expectedSandbox + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove fixture outside its smoke sandbox: $resolvedFixture"
    }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "LLM Wiki extraction readiness regression passed: $($result.moduleReadiness.aggregateLeakPaths) production leak path(s); real extracted-project and synthetic donor compilation verified."
