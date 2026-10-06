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
    $options = $null
    if ($null -ne ('System.IO.EnumerationOptions' -as [type])) {
        $options = [IO.EnumerationOptions]::new()
        $options.AttributesToSkip = [IO.FileAttributes]::Hidden
        $options.IgnoreInaccessible = $true
        $options.MatchCasing = [IO.MatchCasing]::CaseInsensitive
        $options.MatchType = [IO.MatchType]::Win32
    }
    while ($directories.Count -gt 0) {
        $directory = $directories.Pop()
        if ($null -ne $options) {
            $files = $directory.EnumerateFiles($Filter, $options)
            $children = $directory.EnumerateDirectories('*', $options)
        } else {
            # Windows PowerShell runs on .NET Framework, which lacks enumeration
            # options. Walk one directory at a time so excluded and linked trees
            # are still rejected before descent. -Force exposes system entries;
            # the explicit hidden filter matches the modern runtime's contract.
            try {
                $files = @(Get-ChildItem -LiteralPath $directory.FullName -File -Force -Filter $Filter -ErrorAction Stop |
                    Where-Object { -not ($_.Attributes -band [IO.FileAttributes]::Hidden) })
                $children = @(Get-ChildItem -LiteralPath $directory.FullName -Directory -Force -ErrorAction Stop |
                    Where-Object { -not ($_.Attributes -band [IO.FileAttributes]::Hidden) })
            } catch {
                if ($_.CategoryInfo.Category -eq [Management.Automation.ErrorCategory]::PermissionDenied) { continue }
                throw
            }
        }
        foreach ($file in $files) { $file }
        foreach ($child in $children) {
            if ($excluded.Contains($child.Name) -or ($child.Attributes -band [IO.FileAttributes]::ReparsePoint)) { continue }
            $directories.Push($child)
        }
    }
}
