[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')

$testRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'index-fingerprint'
$testRootRelative = $testRoot.Substring($repositoryRoot.Length + 1).Replace('\', '/')
$firstRelative = "$testRootRelative/данные-a.txt"
$secondRelative = "$testRootRelative/b.txt"
$firstPath = Join-Path $repositoryRoot $firstRelative
$secondPath = Join-Path $repositoryRoot $secondRelative
try {
    [IO.File]::WriteAllText($firstPath, 'alpha', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($secondPath, 'bravo', [Text.UTF8Encoding]::new($false))

    $first = Get-LlmWikiIndexInputFingerprint $repositoryRoot @($secondRelative, $firstRelative, $firstRelative, "$testRootRelative/missing.txt")
    $reordered = Get-LlmWikiIndexInputFingerprint $repositoryRoot @($firstRelative, $secondRelative)
    if ($first -cne $reordered) { throw 'Index fingerprint depends on input order, duplicates, or missing files.' }

    $empty = Get-LlmWikiIndexInputFingerprint $repositoryRoot @()
    $missingOnly = Get-LlmWikiIndexInputFingerprint $repositoryRoot @("$testRootRelative/missing.txt")
    if ($empty -cne $missingOnly) { throw 'Index fingerprint does not handle an empty existing input set consistently.' }

    $originalTimestamp = [IO.File]::GetLastWriteTimeUtc($firstPath)
    [IO.File]::WriteAllText($firstPath, 'omega', [Text.UTF8Encoding]::new($false))
    [IO.File]::SetLastWriteTimeUtc($firstPath, $originalTimestamp)
    $changed = Get-LlmWikiIndexInputFingerprint $repositoryRoot @($firstRelative, $secondRelative)
    if ($first -ceq $changed) { throw 'Index fingerprint did not detect a same-length content change with a restored timestamp.' }

    [IO.File]::WriteAllText($firstPath, "alpha`n", [Text.UTF8Encoding]::new($false))
    $lf = Get-LlmWikiIndexInputFingerprint $repositoryRoot @($firstRelative)
    [IO.File]::WriteAllText($firstPath, "alpha`r`n", [Text.UTF8Encoding]::new($false))
    $crlf = Get-LlmWikiIndexInputFingerprint $repositoryRoot @($firstRelative)
    if ($lf -ceq $crlf) { throw 'Git text filters concealed changed input bytes from the content fingerprint.' }

    $abcPath = Join-Path $testRoot 'hash-abc.bin'
    [IO.File]::WriteAllBytes($abcPath, [byte[]]@(97,98,99))
    $missingPath = Join-Path $testRoot 'missing.bin'
    $orders = @(
        @('LlmWikiIndexCache.ps1','LlmWikiQueryCache.ps1','LlmWikiVerificationReceipts.ps1'),
        @('LlmWikiIndexCache.ps1','LlmWikiVerificationReceipts.ps1','LlmWikiQueryCache.ps1'),
        @('LlmWikiQueryCache.ps1','LlmWikiIndexCache.ps1','LlmWikiVerificationReceipts.ps1'),
        @('LlmWikiQueryCache.ps1','LlmWikiVerificationReceipts.ps1','LlmWikiIndexCache.ps1'),
        @('LlmWikiVerificationReceipts.ps1','LlmWikiIndexCache.ps1','LlmWikiQueryCache.ps1'),
        @('LlmWikiVerificationReceipts.ps1','LlmWikiQueryCache.ps1','LlmWikiIndexCache.ps1')
    )
    foreach ($order in $orders) {
        & {
            foreach ($name in $order) { . (Join-Path $PSScriptRoot $name) }
            $expected = 'ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad'
            if ((Get-LlmWikiRequiredFileSha256 $abcPath) -cne $expected -or (Get-LlmWikiFileSha256 $abcPath) -cne $expected) {
                throw "Existing-file hashing changed after loading $($order -join ',')."
            }
            $requiredThrew = $false
            try { $null = Get-LlmWikiRequiredFileSha256 $missingPath } catch { $requiredThrew = $true }
            if (-not $requiredThrew -or (Get-LlmWikiFileSha256 $missingPath) -cne '<missing>') {
                throw "Required/optional missing-file contracts depend on helper load order: $($order -join ',')."
            }
        }
    }
    Write-Host 'Index fingerprint smoke passed: Unicode, content mutations and required/optional hashes across all six helper load orders.'
} finally {
    Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}
