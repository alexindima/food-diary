function Get-LlmWikiModuleTestRoots {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $modulesRoot = [IO.Path]::Combine($RepositoryRoot, 'Modules')
    if (-not [IO.Directory]::Exists($modulesRoot)) { return }
    foreach ($module in Get-ChildItem -LiteralPath $modulesRoot -Directory | Sort-Object Name) {
        if ($module.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        $tests = [IO.DirectoryInfo]::new([IO.Path]::Combine($module.FullName, 'tests'))
        if (-not $tests.Exists) { continue }
        if ($tests.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        "Modules/$($module.Name)/tests"
    }
}
