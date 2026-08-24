[CmdletBinding()]
param(
    [switch] $IncludeRoot,
    [string] $RootArtifactPath
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$rootArtifacts = Join-Path $repositoryRoot '.artifacts'
$nestedArtifacts = @(
    Get-ChildItem -LiteralPath $repositoryRoot -Directory -Filter '.artifacts' -Recurse -Force |
        Where-Object { $_.FullName -ne $rootArtifacts }
)

foreach ($directory in $nestedArtifacts) {
    $path = [IO.Path]::GetFullPath($directory.FullName)
    $isInsideRepository = $path.StartsWith(
        $repositoryRoot + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)

    if (!$isInsideRepository -or [IO.Path]::GetFileName($path) -ne '.artifacts') {
        throw "Refusing to remove unsafe artifact path: $path"
    }

    [IO.Directory]::Delete($path, $true)
}

if ($IncludeRoot -and -not [string]::IsNullOrWhiteSpace($RootArtifactPath)) {
    throw 'Specify either IncludeRoot or RootArtifactPath, not both.'
}

if (-not [string]::IsNullOrWhiteSpace($RootArtifactPath)) {
    $resolvedTarget = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($RootArtifactPath)) {
        $RootArtifactPath
    } else {
        Join-Path $repositoryRoot $RootArtifactPath
    }))
    $rootPrefix = $rootArtifacts + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTarget.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        $resolvedTarget -eq $rootArtifacts) {
        throw "Refusing to remove artifact path outside a scoped root child: $resolvedTarget"
    }

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
