#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ("fd-project-format-" + [Guid]::NewGuid().ToString('N') + '.csproj')
$formatter = Join-Path $PSScriptRoot 'Format-ProjectFiles.ps1'
$repositoryFixture = Join-Path ([IO.Path]::GetTempPath()) ("fd-project-format-repo-" + [Guid]::NewGuid().ToString('N'))
try {
    $source = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <PackageReference Include="Zebra"><PrivateAssets>all</PrivateAssets></PackageReference>
    <PackageReference Include="Alpha" />
    <!-- Keep this comment with the conditional entry. -->
    <PackageReference Include="Conditional" Condition="'$(Enabled)' == 'true'" />
    <PackageReference Update="Existing" Version="1.2.3" />
    <PackageReference Include="$(DynamicPackage)" />
    <PackageReference Include="Duplicate" Version="2" />
    <PackageReference Include="Duplicate" Version="1" />
  </ItemGroup>
  <ItemGroup><ProjectReference Include="other\Example.csproj"><Aliases>custom</Aliases></ProjectReference></ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../Application/Zebra.csproj" />
    <ProjectReference Include="Model/Alpha.csproj" />
    <ProjectReference Include="../../Users/Zebra.csproj" />
    <ProjectReference Include="../../../Shared/Zebra.csproj" />
    <ProjectReference Include="../../../FoodDiary.Infrastructure/Alpha.csproj" />
    <ProjectReference Include="../../../Aardvark/Zulu.csproj" />
    <ProjectReference Include="../../../Zeta/Alpha.csproj" />
  </ItemGroup>
</Project>
'@
    [IO.File]::WriteAllText($fixture, $source)
    & pwsh -NoProfile -File $formatter -Path $fixture -Check
    if ($LASTEXITCODE -ne 1 -or [IO.File]::ReadAllText($fixture) -cne $source) {
        throw 'Check must report drift without modifying the file.'
    }
    & pwsh -NoProfile -File $formatter -Path $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Formatting failed.' }
    $formatted = [IO.File]::ReadAllText($fixture)
    [xml]$xml = $formatted
    $packages = @($xml.SelectNodes('//PackageReference'))
    if (($packages | ForEach-Object { $_.GetAttribute('Include') }) -join ',' -cne 'Alpha,Zebra,Conditional,,$(DynamicPackage),Duplicate,Duplicate') {
        throw 'Incorrect order or crossed a sorting barrier.'
    }
    if ($packages[1].PrivateAssets -cne 'all' -or $packages[5].Version -cne '2' -or $packages[6].Version -cne '1' -or
        $packages[2].GetAttribute('Condition') -cne "'`$(Enabled)' == 'true'" -or
        $xml.SelectSingleNode('//ProjectReference').GetAttribute('Include') -cne 'other/Example.csproj' -or
        $xml.SelectSingleNode('//ProjectReference/Aliases').InnerText -cne 'custom' -or
        -not $formatted.Contains('<!-- Keep this comment with the conditional entry. -->')) {
        throw 'Reference metadata, conditions, comments or normalized paths changed unexpectedly.'
    }
    $projects = @($xml.SelectNodes('//ItemGroup[last()]/ProjectReference'))
    $expected = 'Model/Alpha.csproj,../Application/Zebra.csproj,../../Users/Zebra.csproj,../../../FoodDiary.Infrastructure/Alpha.csproj,../../../Zeta/Alpha.csproj,../../../Shared/Zebra.csproj,../../../Aardvark/Zulu.csproj'
    if (($projects | ForEach-Object { $_.GetAttribute('Include') }) -join ',' -cne $expected) {
        throw 'Project references must sort by parent depth, file name, then full Include.'
    }
    & pwsh -NoProfile -File $formatter -Path $fixture -Check
    if ($LASTEXITCODE -ne 0) { throw 'Formatted fixture failed check.' }
    & pwsh -NoProfile -File $formatter -Path $fixture
    if ($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText($fixture) -cne $formatted) { throw 'Formatter is not idempotent.' }
    $fixtureScripts = Join-Path $repositoryFixture 'scripts'
    [void][IO.Directory]::CreateDirectory($fixtureScripts)
    $fixtureFormatter = Join-Path $fixtureScripts 'Format-ProjectFiles.ps1'
    Copy-Item -LiteralPath $formatter -Destination $fixtureFormatter
    & git -C $repositoryFixture init --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Cannot initialize discovery fixture.' }
    $deletedProject = Join-Path $repositoryFixture 'Old.csproj'
    $newProject = Join-Path $repositoryFixture 'New.csproj'
    [IO.File]::WriteAllText($deletedProject, $source)
    & git -C $repositoryFixture add -- Old.csproj
    if ($LASTEXITCODE -ne 0) { throw 'Cannot track discovery fixture.' }
    Remove-Item -LiteralPath $deletedProject
    [IO.File]::WriteAllText($newProject, $source)
    [IO.File]::WriteAllText((Join-Path $repositoryFixture '.gitignore'), "ignored.csproj`n")
    [IO.File]::WriteAllText((Join-Path $repositoryFixture 'ignored.csproj'), 'intentionally not XML')
    & pwsh -NoProfile -File $fixtureFormatter -Check
    if ($LASTEXITCODE -ne 1 -or [IO.File]::ReadAllText($newProject) -cne $source) {
        throw 'Discovery check must find untracked projects without modifying them.'
    }
    & pwsh -NoProfile -File $fixtureFormatter
    if ($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText($newProject) -cne $formatted) {
        throw 'Discovery must skip deleted and ignored paths and format the new project.'
    }
    Write-Output 'Project formatter regression checks passed.'
} finally {
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture }
    if (Test-Path -LiteralPath $repositoryFixture) {
        $resolvedFixture = [IO.Path]::GetFullPath($repositoryFixture)
        $fixturePrefix = Join-Path ([IO.Path]::GetFullPath([IO.Path]::GetTempPath())) 'fd-project-format-repo-'
        if (-not $resolvedFixture.StartsWith($fixturePrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Refusing to remove a directory outside the formatter fixture scope.'
        }
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
