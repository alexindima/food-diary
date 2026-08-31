function Get-LlmWikiModuleTestRoots {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $modulesRoot = Join-Path $RepositoryRoot 'Modules'
    if (-not (Test-Path -LiteralPath $modulesRoot -PathType Container)) { return }
    foreach ($module in Get-ChildItem -LiteralPath $modulesRoot -Directory | Sort-Object Name) {
        if ($module.Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        $tests = Join-Path $module.FullName 'tests'
        if (-not (Test-Path -LiteralPath $tests -PathType Container)) { continue }
        if ((Get-Item -LiteralPath $tests).Attributes -band [IO.FileAttributes]::ReparsePoint) { continue }
        "Modules/$($module.Name)/tests"
    }
}
