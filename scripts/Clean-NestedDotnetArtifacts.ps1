[CmdletBinding()]
param(
    [switch] $IncludeRoot,
    [string] $RootArtifactPath
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$rootArtifacts = Join-Path $repositoryRoot '.artifacts'
$pathComparison = if ([OperatingSystem]::IsWindows()) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }

if ($IncludeRoot -and -not [string]::IsNullOrWhiteSpace($RootArtifactPath)) {
    throw 'Specify either IncludeRoot or RootArtifactPath, not both.'
}

$resolvedTarget = $null
if (-not [string]::IsNullOrWhiteSpace($RootArtifactPath)) {
    $resolvedTarget = [IO.Path]::TrimEndingDirectorySeparator([IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($RootArtifactPath)) {
        $RootArtifactPath
    } else {
        Join-Path $repositoryRoot $RootArtifactPath
    })))
    $rootPrefix = $rootArtifacts + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTarget.StartsWith($rootPrefix, $pathComparison) -or
        $resolvedTarget -eq $rootArtifacts) {
        throw "Refusing to remove artifact path outside a scoped root child: $resolvedTarget"
    }

    for ($ancestor = $resolvedTarget; $ancestor -and $ancestor -ne $repositoryRoot; $ancestor = [IO.Path]::GetDirectoryName($ancestor)) {
        if ([IO.Directory]::Exists($ancestor) -and ([IO.File]::GetAttributes($ancestor) -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing to remove an artifact scope through a directory link: $ancestor"
        }
    }
}

if ($IncludeRoot -and [IO.Directory]::Exists($rootArtifacts) -and
    ([IO.File]::GetAttributes($rootArtifacts) -band [IO.FileAttributes]::ReparsePoint)) {
    throw "Refusing to remove a linked root artifact directory: $rootArtifacts"
}

# Prune shared caches and dependencies before descending. Once a project artifact
# directory is selected, its children belong to that one deletion operation.
$nestedArtifacts = [Collections.Generic.List[string]]::new()
$pending = [Collections.Generic.Stack[string]]::new()
$pending.Push($repositoryRoot)
while ($pending.Count) {
    $parent = $pending.Pop()
    foreach ($directory in Get-ChildItem -LiteralPath $parent -Directory -Force) {
        if (($directory.Attributes -band [IO.FileAttributes]::ReparsePoint) -or
            [string]::Equals($directory.FullName, $rootArtifacts, $pathComparison) -or
            $directory.Name -in @('.git', 'node_modules')) { continue }
        if ($directory.Name -eq '.artifacts') {
            $nestedArtifacts.Add($directory.FullName)
        } else {
            $pending.Push($directory.FullName)
        }
    }
}

foreach ($directory in $nestedArtifacts) {
    $path = [IO.Path]::GetFullPath($directory)
    $isInsideRepository = $path.StartsWith(
        $repositoryRoot + [IO.Path]::DirectorySeparatorChar,
        $pathComparison)

    if (!$isInsideRepository -or [IO.Path]::GetFileName($path) -ne '.artifacts') {
        throw "Refusing to remove unsafe artifact path: $path"
    }

    if ([IO.Directory]::Exists($path)) { [IO.Directory]::Delete($path, $true) }
}

if ($null -ne $resolvedTarget) {
    if ([IO.Directory]::Exists($resolvedTarget)) {
        [IO.Directory]::Delete($resolvedTarget, $true)
        Write-Output "Scoped .NET artifact directory removed: $resolvedTarget"
    }
}

if ($IncludeRoot -and [IO.Directory]::Exists($rootArtifacts)) {
    $resolvedRootArtifacts = [IO.Path]::GetFullPath($rootArtifacts)
    $expectedRootArtifacts = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts'))
    if ($resolvedRootArtifacts -ne $expectedRootArtifacts) {
        throw "Refusing to remove unsafe root artifact path: $resolvedRootArtifacts"
    }

    [IO.Directory]::Delete($resolvedRootArtifacts, $true)
    Write-Output "Root .NET artifact directory removed."
}

$noun = if ($nestedArtifacts.Count -eq 1) { 'directory' } else { 'directories' }
Write-Output "Nested .NET artifact cleanup removed $($nestedArtifacts.Count) $noun."
