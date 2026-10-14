function Find-LlmWikiNearestProject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Directory,
        [Parameter(Mandatory)][AllowEmptyCollection()][Collections.Generic.Dictionary[string,string]]$Cache
    )

    # The caller owns this cache for one discovery pass, never across commands.
    $root = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/')
    $current = [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/')
    $prefix = $root + [IO.Path]::DirectorySeparatorChar
    $visited = [Collections.Generic.List[string]]::new()
    $result = ''
    while ($current -eq $root -or $current.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        if ($Cache.TryGetValue($current, [ref]$result)) { break }
        $visited.Add($current)
        $project = $null
        if ($null -ne ('System.IO.EnumerationOptions' -as [type])) {
            $options = [IO.EnumerationOptions]::new()
            $options.AttributesToSkip = [IO.FileAttributes]::Hidden -bor [IO.FileAttributes]::System
            $options.MatchCasing = [IO.MatchCasing]::CaseInsensitive
            $options.MatchType = [IO.MatchType]::Win32
            $options.IgnoreInaccessible = $true
            if ([IO.Directory]::Exists($current)) {
                $projects = @([IO.DirectoryInfo]::new($current).EnumerateFiles('*.csproj', $options))
                $project = if ($projects.Count -gt 1) { $projects | Sort-Object Name | Select-Object -First 1 } elseif ($projects.Count -eq 1) { $projects[0] } else { $null }
            }
        } else {
            $project = Get-ChildItem -LiteralPath $current -Filter '*.csproj' -File -ErrorAction SilentlyContinue | Select-Object -First 1
        }
        if ($project) {
            $result = $project.FullName.Substring($root.Length + 1).Replace('\', '/')
            break
        }
        if ($current -eq $root) { break }
        $current = [IO.Path]::GetDirectoryName($current)
    }
    foreach ($path in $visited) { $Cache[$path] = $result }
    return $result
}
