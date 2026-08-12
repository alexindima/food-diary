[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiDependencyManifest.ps1')

function Assert-Case([string]$Name, [string]$Xml, [int]$Count, [string]$Include = '', [string]$Version = '') {
    $references = @(Get-LlmWikiPackageReferences -XmlText $Xml)
    if ($references.Count -ne $Count) { throw "$Name returned $($references.Count) references; expected $Count." }
    if ($Count -gt 0 -and ([string]$references[0].Include -cne $Include -or [string]$references[0].Version -cne $Version)) {
        throw "$Name returned an unexpected package reference."
    }
}

Assert-Case 'empty project' '<Project />' 0
Assert-Case 'property-only project' '<Project><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>' 0
Assert-Case 'project-reference-only group' '<Project><ItemGroup><ProjectReference Include="..\Other.csproj" /></ItemGroup></Project>' 0
Assert-Case 'package in later group' '<Project><ItemGroup><ProjectReference Include="..\Other.csproj" /></ItemGroup><ItemGroup><PackageReference Include="Example" Version="1.2.3" /></ItemGroup></Project>' 1 'Example' '1.2.3'
Assert-Case 'central package management' '<Project><ItemGroup><PackageReference Include="Central.Package" /></ItemGroup></Project>' 1 'Central.Package' ''

Write-Host 'LLM Wiki dependency manifest regression passed.'
