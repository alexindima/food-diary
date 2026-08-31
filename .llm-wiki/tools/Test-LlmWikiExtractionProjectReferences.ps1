[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiExtractionPlanning.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'extraction-project-references'
try {
    $files = @{
        'docs/architecture/backend-modules.json' = '{"modules":{"Sample":{"sourceMappings":{"applicationProjects":["Modules/Sample/Application"],"contractProjects":["Modules/Sample/Contracts/Physical.Contracts.csproj"]}}}}'
        'Modules/Sample/Application/Physical.Application.csproj' = '<Project><PropertyGroup><AssemblyName>FoodDiary.Application.Sample</AssemblyName></PropertyGroup></Project>'
        'Modules/Sample/Contracts/Physical.Contracts.csproj' = '<Project />'
        'Modules/Sample/Application/Nested/Unmapped.Nested.csproj' = '<Project />'
        'Modules/Other/Unmapped.Other.csproj' = '<Project />'
        'Host/New.csproj' = '<Project><ItemGroup><ProjectReference Include="../Modules/Sample/Application/Physical.Application.csproj" /></ItemGroup></Project>'
        'Host/Windows.csproj' = '<Project><ItemGroup><ProjectReference Include="..\Modules\Sample\Application\Physical.Application.csproj" /></ItemGroup></Project>'
        'Host/Contracts.csproj' = '<Project><ItemGroup><ProjectReference Include="../Modules/Sample/Contracts/Physical.Contracts.csproj" /></ItemGroup></Project>'
        'Host/Legacy.cs' = 'using FoodDiary.Application.Sample;'
        'Host/Registration.cs' = 'services.AddSampleModule();'
        'Host/Similar.csproj' = '<Project><ProjectReference Include="Physical.Application.csproj.Extra" /><ProjectReference Include="OtherPhysical.Application.csproj" /><ProjectReference Include="PhysicalXApplication.csproj" /></Project>'
        'Host/Unmapped.csproj' = '<Project><ProjectReference Include="Unmapped.Other.csproj" /><ProjectReference Include="Unmapped.Nested.csproj" /></Project>'
    }
    foreach ($entry in $files.GetEnumerator()) {
        $path = Join-Path $fixture $entry.Key
        $null = New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force
        [IO.File]::WriteAllText($path, $entry.Value)
    }
    & git -C $fixture init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Unable to initialize extraction reference fixture.' }
    $plan = Get-LlmWikiExtractionPlan 'Extract Sample into an isolated application module' $fixture
    foreach ($required in @('Host/New.csproj', 'Host/Windows.csproj', 'Host/Contracts.csproj', 'Host/Legacy.cs', 'Host/Registration.cs')) {
        if ($required -notin $plan.paths) { throw "Mapped project discovery omitted $required." }
    }
    foreach ($excluded in @('Host/Similar.csproj', 'Host/Unmapped.csproj')) {
        if ($excluded -in $plan.paths) { throw "Mapped project discovery included unrelated $excluded." }
    }
    Remove-Item -LiteralPath (Join-Path $fixture 'docs/architecture/backend-modules.json')
    $legacyPlan = Get-LlmWikiExtractionPlan 'Extract Sample into an isolated application module' $fixture
    if ('Host/Legacy.cs' -notin $legacyPlan.paths -or 'Host/New.csproj' -in $legacyPlan.paths) {
        throw 'Missing manifest changed legacy discovery or invented a project mapping.'
    }
    Write-Output 'Extraction project reference regression passed: mapped directory/file, physical identity, legacy fallback and unrelated exclusions.'
} finally {
    $sandbox = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot)).TrimEnd('\', '/')
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if (-not $resolvedFixture.StartsWith($sandbox + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Extraction reference fixture cleanup escaped its sandbox.'
    }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
