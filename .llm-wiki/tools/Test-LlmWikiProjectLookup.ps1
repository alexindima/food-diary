[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiProjectLookup.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'project-lookup'
try {
    foreach ($path in @('Parent/Feature/Deep', 'Parent/Other', 'Parent/Nested/Code', 'Orphan/Deep')) {
        $null = New-Item -ItemType Directory -Path (Join-Path $fixture $path) -Force
    }
    foreach ($path in @('Parent/Parent.csproj', 'Parent/Nested/Nested.csproj')) {
        [IO.File]::WriteAllText((Join-Path $fixture $path), '<Project />')
    }
    $cache = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach ($case in @(
        @('Parent/Feature/Deep', 'Parent/Parent.csproj'),
        @('Parent/Other', 'Parent/Parent.csproj'),
        @('Parent/Nested/Code', 'Parent/Nested/Nested.csproj'),
        @('Orphan/Deep', ''),
        @('Orphan', '')
    )) {
        $actual = Find-LlmWikiNearestProject -RepositoryRoot $fixture -Directory (Join-Path $fixture $case[0]) -Cache $cache
        if ([string]$actual -cne $case[1]) { throw "Wrong nearest project for $($case[0]): $actual" }
    }
    if (-not $cache.ContainsKey((Join-Path $fixture 'Parent/Feature/Deep')) -or
        -not $cache.ContainsKey((Join-Path $fixture 'Orphan/Deep'))) { throw 'Positive and negative paths were not memoized.' }
    # A fresh discovery must observe project creation, removal and nesting changes.
    [IO.File]::WriteAllText((Join-Path $fixture 'Parent/Feature/New.csproj'), '<Project />')
    Remove-Item -LiteralPath (Join-Path $fixture 'Parent/Nested/Nested.csproj')
    $fresh = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    if ((Find-LlmWikiNearestProject $fixture (Join-Path $fixture 'Parent/Feature/Deep') $fresh) -cne 'Parent/Feature/New.csproj') { throw 'New lookup did not observe a nearer project.' }
    if ((Find-LlmWikiNearestProject $fixture (Join-Path $fixture 'Parent/Nested/Code') $fresh) -cne 'Parent/Parent.csproj') { throw 'New lookup did not observe project removal.' }
    if (Find-LlmWikiNearestProject $fixture (Split-Path -Parent $fixture) $fresh) { throw 'Lookup escaped repository boundary.' }
    Write-Host 'Project lookup regression passed: nearest project, shared ancestors, missing project, fresh discovery and boundary.'
} finally {
    $sandbox = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not [IO.Path]::GetFullPath($fixture).StartsWith($sandbox, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup target.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
