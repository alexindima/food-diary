[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'reader-fingerprint' -IndependentRepository
try {
    $inputs = @('Directory.Build.props','Directory.Packages.props',
        '.llm-wiki/tools/Build-LlmWikiInProcessSqliteReader.ps1','.llm-wiki/tools/LlmWikiJson.ps1',
        '.llm-wiki/tools/LlmWiki.SqliteReader/LlmWiki.SqliteReader.csproj',
        '.llm-wiki/tools/LlmWiki.SqliteReader/DomainDataReader.cs','.llm-wiki/tools/LlmWiki.SqliteReader/CompiledIndexReader.cs',
        '.llm-wiki/tools/LlmWiki.SqliteReader/StandaloneIndexReader.cs','.llm-wiki/tools/LlmWiki.SqliteReader/ContextSearchReader.cs',
        'FoodDiary.Development.Mcp/Wiki/SqliteContextSearchReader.cs',
        'FoodDiary.Development.Mcp/Protocol/WikiContextSearchResult.cs','FoodDiary.Development.Mcp/Protocol/WikiContextSearchCandidate.cs')
    foreach ($path in $inputs) {
        $target = Join-Path $fixture $path
        $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))
        Copy-Item -LiteralPath (Join-Path $repositoryRoot $path) -Destination $target
    }
    $builder = Join-Path $fixture '.llm-wiki/tools/Build-LlmWikiInProcessSqliteReader.ps1'
    $props = Join-Path $fixture 'Directory.Build.props'
    $text = [IO.File]::ReadAllText($props).Replace("`r`n", "`n")
    [IO.File]::WriteAllText($props, $text)
    $lf = & $builder -CheckOnly -Format Json | ConvertFrom-Json
    [IO.File]::WriteAllText($props, $text.Replace("`n", "`r`n"))
    $crlf = & $builder -CheckOnly -Format Json | ConvertFrom-Json
    if ($lf.fingerprint -cne $crlf.fingerprint) { throw 'Equivalent XML newline conversion invalidated the reader assembly fingerprint.' }
    $source = Join-Path $fixture '.llm-wiki/tools/LlmWiki.SqliteReader/DomainDataReader.cs'
    [IO.File]::AppendAllText($source, "`n// New source generation.")
    $changed = & $builder -CheckOnly -Format Json | ConvertFrom-Json
    if ($changed.fingerprint -ceq $lf.fingerprint) { throw 'Changed C# input reused the old reader assembly fingerprint.' }
    Write-Host 'Reader fingerprint passed: portable XML newlines and exact C# source invalidation.'
} finally {
    if (-not [IO.Path]::GetFileName($fixture).StartsWith('reader-fingerprint-', [StringComparison]::Ordinal)) { throw 'Unowned reader fingerprint fixture.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
