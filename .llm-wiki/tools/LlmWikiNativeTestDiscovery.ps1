. (Join-Path $PSScriptRoot 'LlmWikiNativeCompilation.ps1')
function Get-LlmWikiNativeTestScanner {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)
    Get-LlmWikiCompiledToolType -RepositoryRoot $RepositoryRoot -SourceFile 'LlmWikiTestSourceScanner.cs' -ClassName Scanner
}
