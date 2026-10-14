[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSourceInventory.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiNativeTestDiscovery.ps1')
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'native-test-discovery' -IndependentRepository
$previousSourceRoot = $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT
$env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $fixture
try {
    $root = Join-Path $fixture 'tests'
    $null = New-Item -ItemType Directory -Path $root -Force
    $patterns = @([regex]::new('\b(?:BillingHandler|ExecuteAsync)\b', [Text.RegularExpressions.RegexOptions]::IgnoreCase))
    $declared = @([regex]::new('\bBillingHandler\b', [Text.RegularExpressions.RegexOptions]::IgnoreCase))
    $cases = @{
        'Unicode.cs' = 'BillingHandler Кириллица'
        'Member.cs' = 'ExecuteAsync'
        'Boundary.cs' = 'ExtraBillingHandler BillingHandlerSuffix ЖBillingHandlerЖ'
        'Capital.SPEC.TS' = 'billinghandler'
        'NonTest.ts' = 'BillingHandler'
        'Hidden.cs' = 'BillingHandler'
        'Removed.cs' = 'BillingHandler'
    }
    foreach ($entry in $cases.GetEnumerator()) { [IO.File]::WriteAllText((Join-Path $root $entry.Key), $entry.Value) }
    $hidden = Join-Path $root 'Hidden.cs'
    [IO.File]::SetAttributes($hidden, [IO.FileAttributes]::Hidden)
    foreach ($excluded in @('bin','obj','.artifacts','node_modules')) {
        $directory = Join-Path $root $excluded
        $null = New-Item -ItemType Directory -Path $directory -Force
        [IO.File]::WriteAllText((Join-Path $directory 'Ignored.cs'), 'BillingHandler')
    }
    $scanner = Get-LlmWikiNativeTestScanner -RepositoryRoot $fixture
    if ($null -eq $scanner) { Write-Host 'Native scanner unavailable; legacy scanner remains active.'; return }
    $scan = $scanner::Scan($fixture, [string[]]@('tests'), [regex[]]$patterns, [regex[]]$declared)
    $expected = @(Get-LlmWikiSourceFiles -RepositoryRoot $root -Filter '*' -ExcludedDirectory @('bin','obj','.artifacts','node_modules') -ExcludeSystem | Where-Object {
        ($_.Extension -eq '.cs' -or $_.Name -match '\.(spec|test)\.ts$') -and $patterns[0].IsMatch([IO.File]::ReadAllText($_.FullName))
    } | ForEach-Object { $_.FullName.Substring($fixture.Length + 1).Replace('\','/') } | Sort-Object)
    if (($expected -join '|') -cne (($scan.Direct | Sort-Object) -join '|') -or $scan.Declared.Count -ne 3 -or $scan.Contents.Count -ne 5) {
        throw 'Native scanner changed legacy boundaries, casing, Unicode, file filters or hidden/build exclusion.'
    }
    $membersOnly = $scanner::Scan($fixture, [string[]]@('tests'), [regex[]]$patterns, [regex[]]@($null))
    $empty = $scanner::Scan($fixture, [string[]]@('tests'), $null, $null)
    if (($membersOnly.Direct -join '|') -cne ($scan.Direct -join '|') -or $membersOnly.Declared.Count -ne 0 -or $empty.Direct.Count -ne 0) {
        throw 'Null/empty declaration patterns changed method-only evidence or crashed native matching.'
    }
    $changed = Join-Path $root 'Member.cs'
    $timestamp = [IO.File]::GetLastWriteTimeUtc($changed)
    [IO.File]::WriteAllText($changed, 'NothingAsync')
    [IO.File]::SetLastWriteTimeUtc($changed, $timestamp)
    [IO.File]::Delete((Join-Path $root 'Removed.cs'))
    [IO.File]::WriteAllText((Join-Path $root 'Untracked.cs'), 'ExecuteAsync')
    $fresh = $scanner::Scan($fixture, [string[]]@('tests'), [regex[]]$patterns, [regex[]]$declared)
    if ($fresh.Direct -contains 'tests/Member.cs' -or $fresh.Direct -contains 'tests/Removed.cs' -or $fresh.Direct -notcontains 'tests/Untracked.cs') {
        throw 'Scanner missed an untracked addition, deletion or same-size/same-time source edit.'
    }
    $helper = (Join-Path $PSScriptRoot 'LlmWikiNativeTestDiscovery.ps1').Replace("'", "''")
    $probe = Join-Path $fixture 'probe.ps1'
    [IO.File]::WriteAllText($probe, @"
param([string]`$Root)
`$ErrorActionPreference = 'Stop'
. '$helper'
`$scanner = Get-LlmWikiNativeTestScanner -RepositoryRoot `$Root -Verbose
`$scan = `$scanner::Scan(`$Root, [string[]]@('tests'), [regex[]]@([regex]::new('\bBillingHandler\b', 'IgnoreCase')), [regex[]]@())
if (`$scan.Direct.Count -ne 2) { throw 'Fresh-process scanner lost declaration matches.' }
"@)
    $shell = (Get-Process -Id $PID).Path
    & $shell -NoProfile -File $probe -Root $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Compiled scanner cache could not be reused in a fresh process.' }
    $dll = @(Get-ChildItem -LiteralPath (Join-Path $fixture '.artifacts/llm-wiki/native-tools') -Filter '*.dll' -File)[0]
    [IO.File]::WriteAllText($dll.FullName, 'corrupt derived assembly')
    & $shell -NoProfile -File $probe -Root $fixture
    if ($LASTEXITCODE -ne 0) { throw 'Corrupt compiled scanner was not rebuilt in a fresh process.' }
    [IO.File]::Delete($dll.FullName)
    [IO.File]::Delete("$($dll.FullName).json")
    $children = [Collections.Generic.List[Diagnostics.Process]]::new()
    try {
        foreach ($index in 1..3) {
            $start = [Diagnostics.ProcessStartInfo]::new($shell)
            $start.UseShellExecute = $false
            $start.RedirectStandardOutput = $true
            $start.RedirectStandardError = $true
            foreach ($argument in @('-NoProfile','-File',$probe,'-Root',$fixture)) { $start.ArgumentList.Add($argument) }
            $child = [Diagnostics.Process]::Start($start)
            $children.Add($child)
        }
        foreach ($child in $children) {
            $stdout = $child.StandardOutput.ReadToEndAsync()
            $stderr = $child.StandardError.ReadToEndAsync()
            if (-not $child.WaitForExit(30000)) { throw 'Concurrent native compilation fixture timed out.' }
            if ($child.ExitCode -ne 0) { throw "Concurrent scanner publication failed: $($stdout.GetAwaiter().GetResult()) $($stderr.GetAwaiter().GetResult())" }
        }
        $published = & $shell -NoProfile -File $probe -Root $fixture
        if ($LASTEXITCODE -ne 0) { throw 'Concurrent compilation left an unreadable derived assembly.' }
        if (($published -join "`n") -notmatch 'compiled assembly cache hit') { throw 'Concurrent publication did not leave a complete reusable assembly and receipt.' }
    } finally {
        foreach ($child in $children) { if (-not $child.HasExited) { $child.Kill($true); $child.WaitForExit() }; $child.Dispose() }
    }
    Write-Host 'Native test discovery passed: full legacy parity, Unicode boundaries, exclusions, fresh edits/additions/deletions, fresh-process compiled reuse and corruption recovery.'
} finally {
    $env:LLM_WIKI_READ_ONLY_SOURCE_ROOT = $previousSourceRoot
    $parent = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository)).TrimEnd('\','/')
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture)) -cne $parent -or [IO.Path]::GetFileName($fixture) -notmatch '^native-test-discovery-[a-f0-9]{32}$') { throw 'Unowned native scanner fixture cleanup.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force
}
