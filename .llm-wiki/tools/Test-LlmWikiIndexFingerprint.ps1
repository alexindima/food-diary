[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')

$testRootRelative = ".artifacts/llm-wiki/tests/index-fingerprint-$([Guid]::NewGuid().ToString('N'))"
$testRoot = Join-Path $repositoryRoot $testRootRelative
$firstRelative = "$testRootRelative/данные-a.txt"
$secondRelative = "$testRootRelative/b.txt"
$firstPath = Join-Path $repositoryRoot $firstRelative
$secondPath = Join-Path $repositoryRoot $secondRelative
try {
    $null = New-Item -ItemType Directory -Path $testRoot -Force
    [IO.File]::WriteAllText($firstPath, 'alpha', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($secondPath, 'bravo', [Text.UTF8Encoding]::new($false))

    $first = Get-LlmWikiIndexInputFingerprint $repositoryRoot @($secondRelative, $firstRelative, $firstRelative, "$testRootRelative/missing.txt")
    $reordered = Get-LlmWikiIndexInputFingerprint $repositoryRoot @($firstRelative, $secondRelative)
    if ($first -cne $reordered) { throw 'Index fingerprint depends on input order, duplicates, or missing files.' }

    $originalTimestamp = [IO.File]::GetLastWriteTimeUtc($firstPath)
    [IO.File]::WriteAllText($firstPath, 'omega', [Text.UTF8Encoding]::new($false))
    [IO.File]::SetLastWriteTimeUtc($firstPath, $originalTimestamp)
    $changed = Get-LlmWikiIndexInputFingerprint $repositoryRoot @($firstRelative, $secondRelative)
    if ($first -ceq $changed) { throw 'Index fingerprint did not detect a same-length content change with a restored timestamp.' }

    Write-Host 'Index fingerprint smoke passed: Unicode paths, stable ordering, missing inputs, and content changes are handled.'
} finally {
    Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}
