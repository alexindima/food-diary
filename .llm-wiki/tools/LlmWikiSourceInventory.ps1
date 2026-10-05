function Get-LlmWikiSourceFiles {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Filter,
        [string[]]$ExcludedDirectory = @()
    )

    $excluded = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($name in $ExcludedDirectory) { $null = $excluded.Add($name) }
    $directories = [Collections.Generic.Stack[IO.DirectoryInfo]]::new()
    $directories.Push([IO.DirectoryInfo]::new($RepositoryRoot))
    # Match Get-ChildItem without -Force or -FollowSymlink, including its
    # inaccessible-directory handling. File-content read failures still propagate.
    $options = [IO.EnumerationOptions]::new()
    $options.AttributesToSkip = [IO.FileAttributes]::Hidden
    $options.IgnoreInaccessible = $true
    $options.MatchCasing = [IO.MatchCasing]::CaseInsensitive
    $options.MatchType = [IO.MatchType]::Win32
    while ($directories.Count -gt 0) {
        $directory = $directories.Pop()
        foreach ($file in $directory.EnumerateFiles($Filter, $options)) { $file }
        foreach ($child in $directory.EnumerateDirectories('*', $options)) {
            if ($excluded.Contains($child.Name) -or ($child.Attributes -band [IO.FileAttributes]::ReparsePoint)) { continue }
            $directories.Push($child)
        }
    }
}
