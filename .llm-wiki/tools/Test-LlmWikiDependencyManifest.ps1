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

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'dependency-diff'
try {
    $fixtureTools = Join-Path $fixture '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $fixtureTools -Force
    foreach ($name in @('Get-LlmWikiDependencyChanges.ps1', 'LlmWikiDependencyManifest.ps1', 'LlmWikiGitPaths.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $fixtureTools
    }
    function Write-FixtureProject([string]$Name, [string]$Package, [string]$Version) {
        [IO.File]::WriteAllText((Join-Path $fixture $Name), "<Project><ItemGroup><PackageReference Include=`"$Package`" Version=`"$Version`" /></ItemGroup></Project>")
    }
    Write-FixtureProject 'changed project.csproj' 'Changed' '1.0'
    Write-FixtureProject 'removed.csproj' 'Removed' '1.0'
    Write-FixtureProject 'stable.csproj' 'Stable' '1.0'
    [IO.File]::WriteAllText((Join-Path $fixture 'package-lock.json'), '{"lockfileVersion":2}')
    & git -C $fixture init --quiet
    & git -C $fixture add .
    & git -C $fixture -c user.name=WikiRegression -c user.email=wiki@example.invalid commit --quiet -m baseline
    if ($LASTEXITCODE -ne 0) { throw 'Unable to commit dependency fixture baseline.' }
    Write-FixtureProject 'changed project.csproj' 'Changed' '2.0'
    Remove-Item -LiteralPath (Join-Path $fixture 'removed.csproj')
    Write-FixtureProject 'staged.csproj' 'Staged' '1.0'
    & git -C $fixture add staged.csproj
    $unicodeName = ([char]0x0442).ToString() + ' new.csproj'
    Write-FixtureProject $unicodeName 'Untracked' '1.0'
    [IO.File]::WriteAllText((Join-Path $fixture 'package-lock.json'), '{"lockfileVersion":3}')
    $tool = Join-Path $fixtureTools 'Get-LlmWikiDependencyChanges.ps1'
    $diff = & $tool -Format Json | ConvertFrom-Json
    $inventory = & $tool -RepositoryWide -Format Json | ConvertFrom-Json
    if ($diff.changeCount -ne 5 -or $inventory.inventory.manifestCount -ne 4 -or
        ($diff.changes | ConvertTo-Json -Depth 7 -Compress) -cne ($inventory.changes | ConvertTo-Json -Depth 7 -Compress)) {
        throw 'Dependency diff lost changed, deleted, staged, untracked or lockfile evidence.'
    }
    foreach ($expected in @('Changed:version-changed', 'Removed:removed', 'Staged:added', 'Untracked:added', '(lockfile graph):lockfile-changed')) {
        if ($expected -notin @($diff.changes | ForEach-Object { "$($_.package):$($_.kind)" })) { throw "Missing dependency change: $expected" }
    }
    Write-FixtureProject 'changed project.csproj' 'Changed' '3.0'
    $edited = & $tool -Format Json | ConvertFrom-Json
    if (@($edited.changes | Where-Object package -eq 'Changed')[0].after -ne '3.0') { throw 'Dependency diff reused stale workspace text.' }
    Write-Host 'Dependency diff regression passed: complete inventory, edits, deletion, staged/untracked Unicode paths and lockfiles.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    $sandbox = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot)).TrimEnd('\', '/')
    if (-not $resolvedFixture.StartsWith($sandbox + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe dependency fixture cleanup path.' }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
