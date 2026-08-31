function Get-LlmWikiApplicationModuleLayout {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][ValidatePattern('^[A-Za-z][A-Za-z0-9_]*$')][string]$Module
    )

    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/')
    $manifestPath = Join-Path $root 'docs/architecture/backend-modules.json'
    $mapped = @()
    if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $entry = $manifest.modules.PSObject.Properties[$Module]
        if ($null -ne $entry -and $entry.Value.PSObject.Properties['sourceMappings'] -and
            $entry.Value.sourceMappings.PSObject.Properties['applicationProjects']) {
            $mapped = @($entry.Value.sourceMappings.applicationProjects)
        }
    }
    $projects = @(
        foreach ($candidate in @($mapped + @("Modules/$Module/Application", "FoodDiary.Application.$Module"))) {
            $normalized = ([string]$candidate).Replace('\', '/')
            if ([IO.Path]::IsPathRooted($normalized) -or $normalized -match '(^|/)\.\.(/|$)') {
                throw "Application mapping must stay inside the repository: $candidate"
            }
            $absolute = [IO.Path]::GetFullPath((Join-Path $root $normalized))
            if (-not $absolute.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Application mapping escapes the repository: $candidate"
            }
            if (Test-Path -LiteralPath $absolute -PathType Container) {
                # Abstractions and test projects nested below Application are not its implementation project.
                Get-ChildItem -LiteralPath $absolute -File -Filter '*.csproj' | ForEach-Object FullName
            } elseif ((Test-Path -LiteralPath $absolute -PathType Leaf) -and [IO.Path]::GetExtension($absolute) -eq '.csproj') {
                $absolute
            }
        }
    ) | Sort-Object -Unique
    $relativeProjects = @($projects | ForEach-Object { [IO.Path]::GetRelativePath($root, $_).Replace('\', '/') })
    $sourceRoots = @($relativeProjects | ForEach-Object { (Split-Path -Parent $_).Replace('\', '/') })
    $legacyFolder = "FoodDiary.Application/$Module"
    if (Test-Path -LiteralPath (Join-Path $root $legacyFolder) -PathType Container) { $sourceRoots += $legacyFolder }
    [pscustomobject]@{
        sourceRoots = @($sourceRoots | Sort-Object -Unique)
        projects = $relativeProjects
    }
}
