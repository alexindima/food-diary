#requires -Version 7.0
$ErrorActionPreference = 'Stop'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ("fd-project-format-" + [Guid]::NewGuid().ToString('N') + '.csproj')
$formatter = Join-Path $PSScriptRoot 'Format-ProjectFiles.ps1'
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
    & pwsh -NoProfile -File $formatter -Path $fixture -Check
    if ($LASTEXITCODE -ne 0) { throw 'Formatted fixture failed check.' }
    & pwsh -NoProfile -File $formatter -Path $fixture
    if ($LASTEXITCODE -ne 0 -or [IO.File]::ReadAllText($fixture) -cne $formatted) { throw 'Formatter is not idempotent.' }
    Write-Output 'Project formatter regression checks passed.'
} finally {
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture }
}
