[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'catalog-controllers' -IndependentRepository
function Write-ControllerFixture([string]$Path, [string]$Content) {
    $absolute = Join-Path $fixture $Path
    $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($absolute))
    [IO.File]::WriteAllText($absolute, $Content, [Text.UTF8Encoding]::new($false))
}
try {
    foreach ($name in @('Build-LlmWikiCatalog.ps1','Build-LlmWikiModulePages.ps1','LlmWikiIndexCache.ps1','LlmWikiJson.ps1','LlmWikiGitPaths.ps1','LlmWikiProcess.ps1')) {
        Write-ControllerFixture ".llm-wiki/tools/$name" ([IO.File]::ReadAllText((Join-Path $PSScriptRoot $name)))
    }
    Write-ControllerFixture 'Directory.Build.props' '<Project><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
    Write-ControllerFixture 'FoodDiary.Web.Client/angular.json' '{"projects":{}}'
    Write-ControllerFixture 'docs/architecture/module-dependencies.json' '{"modules":{"Probe":[],"Other":[]}}'
    Write-ControllerFixture 'docs/architecture/backend-modules.json' '{"inventory":{"totalModules":2,"folderModules":2,"extractedModules":0},"modules":{"Probe":{"role":"fixture","physicalIsolation":"module-root","enforcement":"fixture","ownedEntities":[],"sourceMappings":{"logicalRoot":"Modules/Probe"}},"Other":{"role":"fixture","physicalIsolation":"module-root","enforcement":"fixture","ownedEntities":[],"sourceMappings":{"logicalRoot":"Modules/Other"}}}}'
    Write-ControllerFixture '.llm-wiki/tools/LlmWikiNativeCompilation.ps1' 'function Get-LlmWikiCompiledToolType { param($RepositoryRoot,$SourceFile,$ClassName); return $null }'
    Write-ControllerFixture '.llm-wiki/tools/Get-LlmWikiContractConsumers.ps1' 'param($Contract,$SourceTextByPath,$Format); "{}"'
    Write-ControllerFixture '.llm-wiki/generated/.gitkeep' ''
    $controller = '[Route("api/probe") ] public class ProbeController { [HttpGet("item")] public void Read() {} }'.Replace('") ]','")]')
    $expected = @(
        'Modules/Probe/Presentation/Controllers/ProbeController.cs'
        'Modules/Probe/Presentation/Controllers/DifferentlyNamedController.cs'
        'Modules/Other/Presentation/Controllers/ProbeForeignController.cs'
        'moved/service/api/ProbeController.cs'
        'FoodDiary.Presentation.Api/ProbeController.cs'
    )
    Write-ControllerFixture 'Modules/Probe/Presentation/FoodDiary.Modules.Probe.Presentation.csproj' '<Project Sdk="Microsoft.NET.Sdk" />'
    Write-ControllerFixture 'Modules/Other/Presentation/FoodDiary.Modules.Other.Presentation.csproj' '<Project Sdk="Microsoft.NET.Sdk" />'
    Write-ControllerFixture 'moved/service/api/FoodDiary.MailRelay.Presentation.csproj' '<Project Sdk="Microsoft.NET.Sdk" />'
    Write-ControllerFixture 'FoodDiary.Presentation.Api/FoodDiary.Presentation.Api.csproj' '<Project Sdk="Microsoft.NET.Sdk" />'
    Write-ControllerFixture 'Modules/Probe/Domain/FoodDiary.Modules.Probe.Domain.csproj' '<Project Sdk="Microsoft.NET.Sdk" />'
    Write-ControllerFixture 'Modules/Probe/tests/FoodDiary.Modules.Probe.Presentation.Tests.csproj' '<Project><PropertyGroup><IsTestProject>true</IsTestProject></PropertyGroup></Project>'
    foreach ($path in $expected + @('Modules/Probe/Domain/NotAnHttpController.cs','Modules/Probe/tests/TestController.cs')) {
        Write-ControllerFixture $path ($controller.Replace('class ProbeController', ('class ' + [IO.Path]::GetFileNameWithoutExtension($path))))
    }
    & (Join-Path $fixture '.llm-wiki/tools/Build-LlmWikiCatalog.ps1') | Out-Null
    $catalog = [IO.File]::ReadAllText((Join-Path $fixture '.llm-wiki/generated/repository-catalog.json')) | ConvertFrom-Json
    $actual = @($catalog.http.controllers.path | Sort-Object)
    if ($actual.Count -ne $expected.Count -or @($expected | Where-Object { $_ -cnotin $actual }).Count) {
        throw "Controller inventory omitted a module or relocated presentation project: $($actual -join ', ')"
    }
    if (@($catalog.http.controllers | Where-Object { $_.endpoints[0].route -cne '/api/probe/item' }).Count) {
        throw 'Controller relocation changed literal endpoint routes.'
    }
    & (Join-Path $fixture '.llm-wiki/tools/Build-LlmWikiModulePages.ps1') | Out-Null
    $page = [IO.File]::ReadAllText((Join-Path $fixture '.llm-wiki/generated/modules/probe.md'))
    if (-not $page.Contains('DifferentlyNamedController.cs') -or $page.Contains('ProbeForeignController.cs')) {
        throw 'Module pages must follow physical controller ownership, including differently named controllers and excluding neighboring modules.'
    }
    Write-Host 'Controller inventory passed: module, relocated service and legacy presentation roots; test/domain exclusions.'
} finally {
    if (-not [IO.Path]::GetFileName($fixture).StartsWith('catalog-controllers-', [StringComparison]::Ordinal)) { throw 'Unowned controller fixture.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
