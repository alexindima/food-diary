[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
$repositoryRoot = [string]@(& git -C $PSScriptRoot rev-parse --show-toplevel)[0]
if ($LASTEXITCODE -ne 0 -or -not $repositoryRoot) { throw 'Unable to resolve fixture source repository.' }
$fixtureParent = Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'symbol-index-cache' -IndependentRepository
$shellPath = (Get-Process -Id $PID).Path
$cases = 0
function Write-Fixture([string]$Path, [string]$Text) {
    $absolute = Join-Path $fixture $Path
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($absolute))
    [IO.File]::WriteAllText($absolute, $Text, [Text.UTF8Encoding]::new($false))
}
function Invoke-Generator([string[]]$Argument, [int]$ExpectedExit = 0, [string]$Culture = '') {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $shellPath
    $start.WorkingDirectory = $fixture
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $cultureArgument = if ($Culture) { " -Culture $Culture" } else { '' }
    $start.Arguments = '-NoLogo -NoProfile -File "' + (Join-Path $fixture '.artifacts/run.ps1') + '"' + $cultureArgument + ' ' + ($Argument -join ' ')
    Clear-LlmWikiGitRepositoryEnvironment $start
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(30000)) {
            Stop-LlmWikiProcessTree -Process $process
            throw 'Symbol-index fixture timed out.'
        }
        $output = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne $ExpectedExit) { throw "Generator exit=$($process.ExitCode), expected=$ExpectedExit. $output" }
        return $output
    } finally { $process.Dispose() }
}
function Assert-Cache([bool]$Expected, [string[]]$Argument, [int]$Exit = 0, [string]$Culture = '') {
    $output = Invoke-Generator $Argument $Exit $Culture
    if ($output.Contains('symbol index cache hit:') -ne $Expected) { throw "Unexpected cache reuse ($Expected). $output" }
    $script:cases++
}
try {
    Write-Fixture '.gitignore' ".artifacts/`n"
    foreach ($tool in @('Build-LlmWikiSymbolIndex.ps1', 'LlmWikiJson.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiIndexCache.ps1')) {
        Write-Fixture ".llm-wiki/tools/$tool" ([IO.File]::ReadAllText((Join-Path $PSScriptRoot $tool)))
    }
    Write-Fixture '.artifacts/run.ps1' @'
param([string]$Culture = '', [switch]$Check, [switch]$ReuseUnchangedCheck)
if ($Culture) { [Globalization.CultureInfo]::CurrentCulture = [Globalization.CultureInfo]::GetCultureInfo($Culture) }
& (Join-Path $PSScriptRoot '../.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1') -Check:$Check -ReuseUnchangedCheck:$ReuseUnchangedCheck
if (-not $?) { exit 1 }
exit 0
'@
    # ASCII declarations and fixed newlines give independent line/DI oracles;
    # the Cyrillic path also exercises Git's native path decoding on PS5.
    $sourceRelative = 'Modules/Users/данные.cs'
    $sourceText = "// header`npublic interface IAlphaService { }`npublic class AlphaService { }`n"
    Write-Fixture $sourceRelative $sourceText
    Write-Fixture 'Program.cs' "// setup`nservices.AddSingleton<IAlphaService, AlphaService>();`n"
    & git -C $fixture add -- .
    if ($LASTEXITCODE -ne 0) { throw 'Unable to stage fixture inputs.' }
    $reuseCheck = @('-Check', '-ReuseUnchangedCheck')
    $reuseUpdate = @('-ReuseUnchangedCheck')
    $outputPath = Join-Path $fixture '.llm-wiki/generated/csharp-symbol-index.json'
    $receiptPath = Join-Path $fixture '.artifacts/llm-wiki/index-cache/csharp-symbol-index.json'
    Assert-Cache $false $reuseCheck 1
    Assert-Cache $false @()
    $index = Get-Content -LiteralPath $outputPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($index.summary.sourceFiles -ne 2 -or $index.summary.symbols -ne 2 -or
        $index.summary.interfaceImplementationLinks -ne 1 -or $index.summary.dependencyInjectionRegistrations -ne 1 -or
        $index.symbols[0].line -ne 2 -or $index.symbols[1].line -ne 3 -or
        $index.symbols[1].role -ne 'Service' -or $index.dependencyInjectionRegistrations[0].line -ne 2) {
        throw 'Symbol/role/line/interface/DI extraction differs from the independently specified fixture.'
    }
    Assert-Cache $true $reuseCheck
    Assert-Cache $true $reuseUpdate
    Assert-Cache $false @('-Check')
    $golden = Get-LlmWikiFileSha256 $outputPath
    $timestamp = [IO.File]::GetLastWriteTimeUtc($outputPath)
    Assert-Cache $false @()
    if ((Get-LlmWikiFileSha256 $outputPath) -cne $golden -or [IO.File]::GetLastWriteTimeUtc($outputPath) -ne $timestamp) {
        throw 'Forced generation changed an identical output or differs from cached generation.'
    }
    $sourcePath = Join-Path $fixture $sourceRelative
    $sourceTimestamp = [IO.File]::GetLastWriteTimeUtc($sourcePath)
    Write-Fixture $sourceRelative ($sourceText.Replace('Alpha', 'Bravo'))
    [IO.File]::SetLastWriteTimeUtc($sourcePath, $sourceTimestamp)
    Assert-Cache $false $reuseCheck 1
    Assert-Cache $false $reuseUpdate
    Assert-Cache $true $reuseCheck

    $renamedRelative = 'Modules/Users/переименовано.cs'
    Move-Item -LiteralPath $sourcePath -Destination (Join-Path $fixture $renamedRelative)
    Assert-Cache $false $reuseCheck 1
    Assert-Cache $false $reuseUpdate
    Assert-Cache $true $reuseCheck
    $renamed = Get-Content -LiteralPath $outputPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if (@($renamed.symbols | Where-Object path -ne $renamedRelative).Count -ne 0) { throw 'Rename retained the old source path.' }

    Write-Fixture 'Added.CS' 'public class AddedQuery { }'
    Assert-Cache $false $reuseCheck 1
    Assert-Cache $false $reuseUpdate
    Remove-Item -LiteralPath (Join-Path $fixture 'Added.CS')
    Assert-Cache $false $reuseCheck 1
    Assert-Cache $false $reuseUpdate
    Write-Fixture 'tests/Unconsumed.cs' 'public class TestOnlyQuery { }'
    Write-Fixture 'Generated.g.cs' 'public class GeneratedQuery { }'
    Assert-Cache $true $reuseCheck

    $savedOutput = [IO.File]::ReadAllBytes($outputPath)
    Remove-Item -LiteralPath $outputPath
    Assert-Cache $false $reuseCheck 1
    [IO.File]::WriteAllBytes($outputPath, $savedOutput)
    Assert-Cache $true $reuseCheck
    Write-Fixture '.llm-wiki/generated/csharp-symbol-index.json' '{"broken":true}'
    Assert-Cache $false $reuseCheck 1
    Assert-Cache $false $reuseUpdate
    Assert-Cache $true $reuseCheck
    Write-Fixture '.artifacts/llm-wiki/index-cache/csharp-symbol-index.json' '{broken'
    Assert-Cache $false $reuseCheck
    Assert-Cache $true $reuseCheck
    $customJson = (Get-Content -LiteralPath $outputPath -Raw -Encoding UTF8 | ConvertFrom-Json) | ConvertTo-Json -Depth 12 -Compress
    Write-Fixture '.llm-wiki/generated/csharp-symbol-index.json' $customJson
    Assert-Cache $false $reuseCheck
    Assert-Cache $false $reuseCheck
    Assert-Cache $false $reuseUpdate
    Assert-Cache $true $reuseCheck
    foreach ($tool in @('Build-LlmWikiSymbolIndex.ps1', 'LlmWikiJson.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiIndexCache.ps1')) {
        [IO.File]::AppendAllText((Join-Path $fixture ".llm-wiki/tools/$tool"), "`n# changed dependency`n")
        Assert-Cache $false $reuseCheck
        Assert-Cache $true $reuseCheck
    }
    # The same fixture crosses runtime culture boundaries without touching inputs.
    Assert-Cache $false $reuseCheck 0 'tr-TR'
    Assert-Cache $true $reuseCheck 0 'tr-TR'
    Assert-Cache $false $reuseCheck
    Assert-Cache $true $reuseCheck
    # Schedule a real source addition after extraction but before the stability
    # check. Only the fixture's serializer is wrapped; the production check and
    # receipt publication paths execute unchanged.
    $jsonHelperPath = Join-Path $fixture '.llm-wiki/tools/LlmWikiJson.ps1'
    $jsonHelper = [IO.File]::ReadAllText($jsonHelperPath)
    $beforeFailureReceipt = Get-LlmWikiFileSha256 $receiptPath
    $beforeFailureOutput = Get-LlmWikiFileSha256 $outputPath
    [IO.File]::AppendAllText($jsonHelperPath, @'

$script:originalCanonicalJson = ${function:ConvertTo-LlmWikiCanonicalJson}
function ConvertTo-LlmWikiCanonicalJson {
    param([object]$Value, [int]$Depth = 30)
    $result = & $script:originalCanonicalJson -Value $Value -Depth $Depth
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot '../../DuringExtraction.cs'), 'public class DuringExtractionQuery { }')
    return $result
}
'@)
    $failureOutput = Invoke-Generator $reuseCheck 1
    if (-not $failureOutput.Contains('inputs changed during extraction') -or
        (Get-LlmWikiFileSha256 $receiptPath) -cne $beforeFailureReceipt -or
        (Get-LlmWikiFileSha256 $outputPath) -cne $beforeFailureOutput) {
        throw 'Changed inputs published a receipt/output or failed for an unrelated reason.'
    }
    $cases++
    Write-Fixture '.llm-wiki/tools/LlmWikiJson.ps1' $jsonHelper
    Remove-Item -LiteralPath (Join-Path $fixture 'DuringExtraction.cs')
    Assert-Cache $true $reuseCheck
    Remove-Item -LiteralPath (Join-Path $fixture $renamedRelative)
    Assert-Cache $false $reuseCheck 1
    Assert-Cache $false $reuseUpdate
    Assert-Cache $true $reuseCheck
    Write-Host "Symbol-index cache smoke passed: $cases cases; extraction oracle, forced parity, content/set/runtime/output invalidation."
} finally {
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture)) -cne [IO.Path]::GetFullPath($fixtureParent).TrimEnd('\', '/')) {
        throw 'Refusing cleanup outside the owned fixture parent.'
    }
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
