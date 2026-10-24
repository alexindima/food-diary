[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiJson.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiGeneratedArtifacts.ps1')
$sourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixtureParent = Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $sourceRoot -IndependentRepository
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $sourceRoot -Name 'pipeline-cache' -IndependentRepository
$shell = (Get-Process -Id $PID).Path
$cases = 0
function Write-Fixture([string]$Path, [string]$Text) {
    $absolute = Join-Path $fixture $Path
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($absolute))
    [IO.File]::WriteAllText($absolute, $Text, [Text.UTF8Encoding]::new($false))
}
function Read-ChildLog([string]$Path) {
    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    $reader = [IO.StreamReader]::new($stream)
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
function Start-Pipeline([string[]]$Argument) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $shell
    $start.WorkingDirectory = $fixture
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    Clear-LlmWikiGitRepositoryEnvironment $start
    $start.Arguments = '-NoLogo -NoProfile -File "' + (Join-Path $fixture '.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1') + '" -AffectedOnly -ChangedPath .llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1 ' + ($Argument -join ' ')
    $process = [Diagnostics.Process]::Start($start)
    $id = [guid]::NewGuid().ToString('N')
    $stdoutPath = Join-Path $fixture ".artifacts/$id.stdout"
    $stderrPath = Join-Path $fixture ".artifacts/$id.stderr"
    $stdout = [IO.FileStream]::new($stdoutPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite, 1, [IO.FileOptions]::Asynchronous)
    $stderr = [IO.FileStream]::new($stderrPath, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite, 1, [IO.FileOptions]::Asynchronous)
    return [pscustomobject]@{
        process = $process; stdout = $stdout; stderr = $stderr; stdoutPath = $stdoutPath; stderrPath = $stderrPath
        stdoutTask = $process.StandardOutput.BaseStream.CopyToAsync($stdout)
        stderrTask = $process.StandardError.BaseStream.CopyToAsync($stderr)
    }
}
function Complete-Pipeline([object]$Child, [int]$ExpectedExit = 0) {
    try {
        if (-not $Child.process.WaitForExit(60000)) { throw 'Fixture pipeline timed out.' }
        [void]$Child.stdoutTask.GetAwaiter().GetResult()
        [void]$Child.stderrTask.GetAwaiter().GetResult()
        $Child.stdout.Flush(); $Child.stderr.Flush()
        $output = (Read-ChildLog $Child.stdoutPath) + (Read-ChildLog $Child.stderrPath)
        if ($Child.process.ExitCode -ne $ExpectedExit) { throw "Pipeline exit=$($Child.process.ExitCode), expected=$ExpectedExit. $output" }
        $script:cases++
        return $output
    } finally {
        if (-not $Child.process.HasExited) { Stop-LlmWikiProcessTree -Process $Child.process }
        $Child.process.Dispose(); $Child.stdout.Dispose(); $Child.stderr.Dispose()
    }
}
function Invoke-Pipeline([string[]]$Argument = @('-ReuseUnchangedChecks'), [int]$ExpectedExit = 0) {
    Complete-Pipeline (Start-Pipeline $Argument) $ExpectedExit
}
function Get-RunCount {
    $path = Join-Path $fixture '.artifacts/runs'
    if (-not [IO.Directory]::Exists($path)) { return 0 }
    return [IO.Directory]::GetFiles($path).Length
}
function Assert-Update([bool]$Cached, [int]$RunsAdded) {
    $before = Get-RunCount
    $output = Invoke-Pipeline
    if ($output.Contains('index update cache hit:') -ne $Cached -or (Get-RunCount) -ne $before + $RunsAdded) {
        throw "Unexpected update reuse=$Cached or worker starts. $output"
    }
}
try {
    Write-Fixture '.gitignore' ".artifacts/`n"
    [void][IO.Directory]::CreateDirectory((Join-Path $fixture '.artifacts'))
    [void][IO.Directory]::CreateDirectory((Join-Path $fixture '.llm-wiki/generated'))
    foreach ($name in @('Invoke-LlmWikiIndexPipeline.ps1','LlmWikiChangeSemantics.ps1','LlmWikiProcess.ps1','LlmWikiJson.ps1','LlmWikiGeneratedArtifacts.ps1','LlmWikiIndexCache.ps1','LlmWikiGitPaths.ps1','LlmWikiIndexTiming.ps1')) {
        Write-Fixture ".llm-wiki/tools/$name" ([IO.File]::ReadAllText((Join-Path $PSScriptRoot $name)))
    }
    $stub = @'
param([switch]$Check, [switch]$ReuseUnchangedCheck)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$name = [IO.Path]::GetFileNameWithoutExtension($PSCommandPath)
$runs = Join-Path $root '.artifacts/runs'
[void][IO.Directory]::CreateDirectory($runs)
[IO.File]::WriteAllText((Join-Path $runs ("$name-" + [guid]::NewGuid().ToString('N'))), $name)
$paths = @{
    'Build-LlmWikiSymbolIndex' = 'csharp-symbol-index.json'
    'Build-LlmWikiBackendContractIndex' = 'backend-contract-index.json'
    'Build-LlmWikiQualityIndex' = 'quality-index.json'
    'Build-LlmWikiArchitectureHealthIndex' = 'architecture-health-index.json'
}
$expected = '{"summary":{"dependencyViolations":0,"untrackedProductionProjects":0,"moduleCycleNodes":0},"fixture":true}'
if ($name -eq 'Build-LlmWikiArchitectureHealthIndex' -and [IO.File]::Exists((Join-Path $root '.artifacts/drift'))) {
    $expected = $expected.Replace('"dependencyViolations":0', '"dependencyViolations":1')
}
$output = Join-Path $root ('.llm-wiki/generated/' + $paths[$name])
if ($Check) {
    if (-not [IO.File]::Exists($output) -or [IO.File]::ReadAllText($output) -cne $expected) { exit 1 }
} else {
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($output))
    [IO.File]::WriteAllText($output, $expected)
}
if ($name -eq 'Build-LlmWikiSymbolIndex' -and [IO.File]::Exists((Join-Path $root '.artifacts/mutate-source'))) {
    [IO.File]::WriteAllText((Join-Path $root 'Source.cs'), 'omega')
}
'@
    foreach ($name in @('Build-LlmWikiSymbolIndex','Build-LlmWikiBackendContractIndex','Build-LlmWikiQualityIndex','Build-LlmWikiArchitectureHealthIndex')) {
        Write-Fixture ".llm-wiki/tools/$name.ps1" $stub
    }
    Write-Fixture 'Source.cs' 'alpha'
    & git -C $fixture add -- Source.cs
    if ($LASTEXITCODE -ne 0) { throw 'Unable to stage the tracked source fixture.' }
    Assert-Update $false 4
    Assert-Update $true 0
    $generated = Join-Path $fixture '.llm-wiki/generated'
    $symbol = Join-Path $generated 'csharp-symbol-index.json'
    $originalOutput = [IO.File]::ReadAllBytes($symbol)
    $outputTime = [IO.File]::GetLastWriteTimeUtc($symbol)
    Assert-Update $true 0
    if ([IO.File]::GetLastWriteTimeUtc($symbol) -ne $outputTime) { throw 'Cached update rewrote an unchanged index.' }
    $before = Get-RunCount
    Invoke-Pipeline @() | Out-Null
    if ((Get-RunCount) -ne $before + 4) { throw 'Omitting reuse did not force all selected generators.' }

    $source = Join-Path $fixture 'Source.cs'
    $sourceTime = [IO.File]::GetLastWriteTimeUtc($source)
    Write-Fixture 'Source.cs' 'bravo'
    [IO.File]::SetLastWriteTimeUtc($source, $sourceTime)
    Assert-Update $false 4
    Assert-Update $true 0
    Write-Fixture 'Added.cs' 'alpha'
    Assert-Update $false 4
    Move-Item -LiteralPath (Join-Path $fixture 'Added.cs') -Destination (Join-Path $fixture 'Renamed.cs')
    Assert-Update $false 4
    Remove-Item -LiteralPath (Join-Path $fixture 'Renamed.cs')
    Assert-Update $false 4
    Remove-Item -LiteralPath $source
    Assert-Update $false 4
    Write-Fixture 'Source.cs' 'bravo'
    Assert-Update $false 4
    Remove-Item -LiteralPath $symbol
    Assert-Update $false 4
    [IO.File]::WriteAllText($symbol, '{"changed":true}')
    Assert-Update $false 4
    Assert-Update $true 0

    $receipt = @(Get-ChildItem -LiteralPath (Join-Path $fixture '.git/llm-wiki/index-cache') -Filter 'pipeline-*.json')[0].FullName
    $receiptObject = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    $receiptObject.schemaVersion = 1
    [IO.File]::WriteAllText($receipt, ($receiptObject | ConvertTo-Json))
    Assert-Update $false 4
    [IO.File]::WriteAllText($receipt, '{broken')
    Assert-Update $false 4
    $receiptObject = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
    $receiptObject.runtimeFingerprint = 'changed-runtime'
    [IO.File]::WriteAllText($receipt, ($receiptObject | ConvertTo-Json))
    Assert-Update $false 4

    $architectureTool = Join-Path $fixture '.llm-wiki/tools/Build-LlmWikiArchitectureHealthIndex.ps1'
    $architectureSource = [IO.File]::ReadAllBytes($architectureTool)
    [IO.File]::AppendAllText($architectureTool, "`n# exercise generated drift`n")
    Write-Fixture '.artifacts/drift' 'yes'
    Assert-Update $false 4
    Assert-Update $true 0
    $before = Get-RunCount
    $output = Invoke-Pipeline @('-Check','-ReuseUnchangedChecks') 1
    if (-not $output.Contains('Architecture drift detected in cached index') -or (Get-RunCount) -ne $before) {
        throw 'A cached generated result bypassed the architecture check contract.'
    }
    Remove-Item -LiteralPath (Join-Path $fixture '.artifacts/drift')
    [IO.File]::WriteAllBytes($architectureTool, $architectureSource)
    Assert-Update $false 4

    $lockPath = Join-Path $fixture '.git/llm-wiki/index-transactions/update.lock'
    $lease = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $child = Start-Pipeline @('-ReuseUnchangedChecks')
    try {
        $watch = [Diagnostics.Stopwatch]::StartNew()
        while (-not (Read-ChildLog $child.stdoutPath).Contains('Another LLM Wiki index update is running') -and
            -not $child.process.HasExited -and $watch.Elapsed.TotalSeconds -lt 20) { Start-Sleep -Milliseconds 100 }
        if ($child.process.HasExited -or $watch.Elapsed.TotalSeconds -ge 20) { throw 'Cached update bypassed its writer lock or never reached the lock.' }
    } catch {
        $failure = $_
        Stop-LlmWikiProcessTree -Process $child.process
        try { Complete-Pipeline $child | Out-Null } catch { }
        throw $failure
    } finally { $lease.Dispose() }
    $before = Get-RunCount
    $output = Complete-Pipeline $child
    if (-not $output.Contains('Concurrent LLM Wiki index result reused') -or (Get-RunCount) -ne $before) { throw 'Lock waiter did not reuse the completed stable result.' }

    # Use the actual snapshot/state functions to create owned crash fixtures;
    # the child still executes the complete production pipeline entrypoint.
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Invoke-LlmWikiIndexPipeline.ps1'), [ref]$null, [ref]$null)
    foreach ($definition in $ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] }, $true)) {
        . ([scriptblock]::Create($definition.Extent.Text))
    }
    $transactionStateRoot = Join-Path $fixture '.git/llm-wiki/index-transactions'
    $orphan = Join-Path $transactionStateRoot ([guid]::NewGuid().ToString('N'))
    $snapshot = New-IndexSnapshot $generated (Join-Path $orphan 'backup')
    $state = @{schemaVersion=3;status='in-progress';ownerPid=[int]::MaxValue;ownerStartTicks=1;backupManifestHash=$snapshot.manifestHash;checkpointSnapshot='';checkpointManifestHash=''}
    Write-IndexTransactionState $orphan $state
    Assert-Update $true 0
    if (Test-Path -LiteralPath $orphan) { throw 'A matching receipt bypassed valid orphan recovery.' }

    $orphan = Join-Path $transactionStateRoot ([guid]::NewGuid().ToString('N'))
    $snapshot = New-IndexSnapshot $generated (Join-Path $orphan 'backup')
    $state.backupManifestHash = $snapshot.manifestHash
    Write-IndexTransactionState $orphan $state
    [IO.File]::WriteAllText((Join-Path $snapshot.path 'content/csharp-symbol-index.json'), 'corrupt')
    $before = Get-RunCount
    Invoke-Pipeline @('-ReuseUnchangedChecks') 1 | Out-Null
    if (-not (Test-Path -LiteralPath $orphan) -or (Get-RunCount) -ne $before -or
        [Convert]::ToBase64String([IO.File]::ReadAllBytes($symbol)) -cne [Convert]::ToBase64String($originalOutput)) {
        throw 'Corrupt orphan evidence was bypassed, removed or displaced the usable tree.'
    }
    Remove-IndexTransactionDirectory $orphan $transactionStateRoot

    $receiptBefore = [IO.File]::ReadAllBytes($receipt)
    [IO.File]::AppendAllText((Join-Path $fixture '.llm-wiki/tools/Build-LlmWikiSymbolIndex.ps1'), "`n# force a cache miss`n")
    Write-Fixture '.artifacts/mutate-source' 'yes'
    $output = Invoke-Pipeline @('-ReuseUnchangedChecks') 1
    if (-not $output.Contains('inputs changed during generation') -or
        [Convert]::ToBase64String([IO.File]::ReadAllBytes($receipt)) -cne [Convert]::ToBase64String($receiptBefore) -or
        [Convert]::ToBase64String([IO.File]::ReadAllBytes($symbol)) -cne [Convert]::ToBase64String($originalOutput)) {
        throw 'Changing inputs certified a mixed snapshot or failed without preserving rollback/receipt bytes.'
    }
    Write-Fixture 'Source.cs' 'alpha'
    $output = Invoke-Pipeline @('-Check','-ReuseUnchangedChecks') 1
    if (-not $output.Contains('inputs changed during generation') -or
        [Convert]::ToBase64String([IO.File]::ReadAllBytes($receipt)) -cne [Convert]::ToBase64String($receiptBefore)) {
        throw 'Changing inputs during check published a new reusable receipt.'
    }
    Write-Host "Pipeline cache regression passed: $cases executions; unchanged reuse, forced runs, content/output/schema invalidation, lock, recovery and unstable-source rollback."
} finally {
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture)) -cne [IO.Path]::GetFullPath($fixtureParent).TrimEnd('\','/')) { throw 'Unsafe pipeline fixture cleanup.' }
    Remove-Item -LiteralPath $fixture -Recurse -Force -ErrorAction SilentlyContinue
}
