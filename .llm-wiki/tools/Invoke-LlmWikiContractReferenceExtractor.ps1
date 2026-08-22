[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string[]]$Path,
    [string[]]$Name,
    [object[]]$Contract,
    [switch]$BuildBackendIndex
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LlmWikiIndexCache.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
$projectPath = Join-Path $PSScriptRoot 'contract-reference-extractor/LlmWiki.ContractReferenceExtractor.csproj'
if ($BuildBackendIndex -and @($Contract).Count -eq 0) { throw 'BuildBackendIndex requires contract metadata.' }
if (-not $BuildBackendIndex -and @($Name).Count -eq 0) { throw 'Reference extraction requires at least one name.' }
$fingerprint = Get-LlmWikiIndexInputFingerprint $RepositoryRoot @(
    '.llm-wiki/tools/contract-reference-extractor/LlmWiki.ContractReferenceExtractor.csproj'
    '.llm-wiki/tools/contract-reference-extractor/Program.cs'
)
$artifactRoot = Join-Path $RepositoryRoot ".artifacts/llm-wiki/contract-reference-extractor/$fingerprint"
$readyPath = Join-Path $artifactRoot 'ready.txt'
$extractorOutputDirectory = Join-Path $artifactRoot 'bin/LlmWiki.ContractReferenceExtractor/release'
$extractorDllPath = Join-Path $extractorOutputDirectory 'LlmWiki.ContractReferenceExtractor.dll'
$extractorRuntimeConfigPath = Join-Path $extractorOutputDirectory 'LlmWiki.ContractReferenceExtractor.runtimeconfig.json'
$lockRoot = Join-Path $RepositoryRoot '.artifacts/llm-wiki/contract-reference-extractor'
$lockPath = Join-Path $lockRoot 'build.lock'
$null = New-Item -ItemType Directory -Path $lockRoot -Force

$lockStream = $null
$deadline = [DateTime]::UtcNow.AddMinutes(2)
while ($null -eq $lockStream) {
    try {
        $lockStream = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    } catch [IO.IOException] {
        if ([DateTime]::UtcNow -ge $deadline) { throw 'Timed out waiting for the contract-reference extractor build lock.' }
        Start-Sleep -Milliseconds 100
    }
}
try {
    if (-not (Test-Path -LiteralPath $readyPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $extractorDllPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $extractorRuntimeConfigPath -PathType Leaf)) {
        $buildOutput = & dotnet build $projectPath -c Release --artifacts-path $artifactRoot --nologo --verbosity quiet 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) { throw "Contract-reference extractor build failed.`n$($buildOutput.Trim())" }
        if (-not (Test-Path -LiteralPath $extractorDllPath -PathType Leaf) -or
            -not (Test-Path -LiteralPath $extractorRuntimeConfigPath -PathType Leaf)) {
            throw 'Contract-reference extractor build produced no runnable framework-dependent application.'
        }
        [IO.File]::WriteAllText($readyPath, $fingerprint + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    }
} finally {
    $lockStream.Dispose()
}

$payload = if ($BuildBackendIndex) {
    [ordered]@{ paths = @($Path); contracts = @($Contract) } | ConvertTo-Json -Depth 8 -Compress
} else {
    [ordered]@{ paths = @($Path); names = @($Name) } | ConvertTo-Json -Compress
}
# A live redirected pipe needs an explicit BOM-free StandardInputEncoding to stay
# stable; that ProcessStartInfo property exists only on .NET 5+ (PowerShell 7+), and on
# Windows PowerShell 5.1 (.NET Framework) even a single raw BaseStream write can still pick
# up a stray UTF-8 preamble from the default StandardInput StreamWriter, corrupting the
# payload the extractor reads. ArgumentList is likewise .NET Core 2+ only. Redirecting
# stdin/stdout/stderr through temp files via a shell wrapper (which also captures the exit
# code to a file, since Start-Process -PassThru does not reliably report ExitCode without
# -Wait, and -Wait cannot be combined with our own timeout) sidesteps all of the above and
# behaves identically on every PowerShell/.NET runtime and OS this repository targets.
$stdinPath = [IO.Path]::GetTempFileName()
$stdoutPath = [IO.Path]::GetTempFileName()
$stderrPath = [IO.Path]::GetTempFileName()
$exitCodePath = [IO.Path]::GetTempFileName()
try {
    [IO.File]::WriteAllText($stdinPath, $payload, [Text.UTF8Encoding]::new($false))
    $extractorMode = $(if ($BuildBackendIndex) { '--backend-index' } else { '--stdin' })
    $runningOnWindows = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
    if ($runningOnWindows) {
        $inner = "dotnet `"$extractorDllPath`" $extractorMode < `"$stdinPath`" > `"$stdoutPath`" 2> `"$stderrPath`" & echo %ERRORLEVEL% > `"$exitCodePath`""
        $process = Start-Process -FilePath 'cmd.exe' -ArgumentList @('/d', '/s', '/c', "`"$inner`"") `
            -WorkingDirectory $RepositoryRoot -NoNewWindow -PassThru
    } else {
        $inner = "dotnet '$extractorDllPath' $extractorMode < '$stdinPath' > '$stdoutPath' 2> '$stderrPath'; echo `$? > '$exitCodePath'"
        $process = Start-Process -FilePath '/bin/sh' -ArgumentList @('-c', $inner) `
            -WorkingDirectory $RepositoryRoot -PassThru
    }
    $deadline = [DateTime]::UtcNow.AddMilliseconds(120000)
    while (-not $process.HasExited -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 25 }
    if (-not $process.HasExited) {
        Stop-LlmWikiProcessTree -Process $process
        throw 'Contract-reference extractor timed out after 120 seconds.'
    }
    # Get-Content -Raw on a genuinely empty file emits no pipeline output at all on
    # Windows PowerShell 5.1 (captured as PowerShell's internal AutomationNull, which
    # still fools `-eq $null` but throws on any method call, unlike PowerShell 7+ which
    # returns ''); `-join ''` reliably collapses either case to a real empty string.
    # Get-Content without -Encoding defaults to the system codepage (not UTF-8) on
    # Windows PowerShell 5.1 for a BOM-less file, which corrupts non-ASCII contract/type
    # names in the extractor's output; -Encoding UTF8 makes decoding explicit and correct
    # on every runtime, matching the BOM-free UTF-8 the process actually wrote.
    $output = (Get-Content -LiteralPath $stdoutPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue) -join ''
    $errorText = ((Get-Content -LiteralPath $stderrPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue) -join '').Trim()
    $exitCodeText = ((Get-Content -LiteralPath $exitCodePath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue) -join '').Trim()
    if (-not $exitCodeText -or $exitCodeText -notmatch '^-?\d+$') {
        throw "Contract-reference extractor did not report an exit code.$(if ($errorText) { " $errorText" })"
    }
    $extractorExitCode = [int]$exitCodeText
    if ($extractorExitCode -ne 0) {
        throw "Contract-reference extractor failed with exit code $extractorExitCode.$(if ($errorText) { " $errorText" })"
    }
} finally {
    Remove-Item -LiteralPath $stdinPath, $stdoutPath, $stderrPath, $exitCodePath -Force -ErrorAction SilentlyContinue
}

if ($BuildBackendIndex) {
    if ($errorText -notmatch 'LLM_WIKI_METRICS contracts=(?<contracts>\d+);consumerEdges=(?<edges>\d+)') {
        throw 'Contract-reference extractor omitted backend-index metrics.'
    }
    [pscustomobject]@{ indexJson = $output; contracts = [int]$Matches['contracts']; consumerEdges = [int]$Matches['edges'] }
} else {
    # ConvertFrom-Json writes its parsed array as a single non-enumerated object on
    # Windows PowerShell 5.1 when returned across a function/script boundary (unlike
    # PowerShell 7+, and unlike typing the same pipeline at an interactive prompt); piping
    # through ForEach-Object forces real per-item enumeration on every runtime.
    try { @($output | ConvertFrom-Json | ForEach-Object { $_ }) }
    catch { throw "Contract-reference extractor returned invalid JSON: $($_.Exception.Message)" }
}
