[CmdletBinding()]
param()

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

$noun = if ($nestedArtifacts.Count -eq 1) { 'directory' } else { 'directories' }
Write-Output "Nested .NET artifact cleanup removed $($nestedArtifacts.Count) $noun."
