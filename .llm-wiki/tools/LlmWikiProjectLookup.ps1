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
        $project = Get-ChildItem -LiteralPath $current -Filter '*.csproj' -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($project) {
            $result = $project.FullName.Substring($root.Length + 1).Replace('\', '/')
            break
        }
        if ($current -eq $root) { break }
        $current = Split-Path -Parent $current
    }
    foreach ($path in $visited) { $Cache[$path] = $result }
    return $result
}
