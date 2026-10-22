# Runs explicit already-built backend test inputs; never builds or changes IDE settings.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]]$TestAssembly,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [ValidateSet('normal', 'profile', 'both')][string]$Mode = 'both',
    [string]$DotCoverExecutable,
    [string]$RunSettings,
    [string[]]$ExcludeAssembly = @(),
    [string[]]$ExcludeAttribute = @('System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute'),
    [string]$FfmpegPath,
    [string[]]$SourceFile = @()
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repo = (& git rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Run from the intended Git checkout.' }
$repo = [IO.Path]::GetFullPath($repo)
$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $repo '.artifacts')) + [IO.Path]::DirectorySeparatorChar
$comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
if (-not $output.StartsWith($artifactRoot, $comparison)) { throw 'OutputDirectory must be a fresh child of repository .artifacts/.' }
if (Test-Path -LiteralPath $output) { throw 'OutputDirectory already exists; choose a fresh evidence directory.' }
$tests = @($TestAssembly | ForEach-Object { (Resolve-Path -LiteralPath $_).Path } | Sort-Object -Unique)
if ($tests.Count -eq 0) { throw 'Select at least one test DLL.' }
foreach ($test in $tests) {
    if ([IO.Path]::GetExtension($test) -ne '.dll' -or $test -match '["\r\n]') { throw "Unsupported test DLL path: $test" }
}
$dotnet = (Get-Command dotnet -CommandType Application -ErrorAction Stop).Source
$dotCover = if ($Mode -ne 'normal') {
    if (-not $DotCoverExecutable) { throw 'DotCoverExecutable is required for profiling.' }
    (Get-Command $DotCoverExecutable -CommandType Application -ErrorAction Stop).Source
} else { $null }
$settings = if ($RunSettings) { (Resolve-Path -LiteralPath $RunSettings).Path } else { Join-Path $output 'serial.runsettings' }
if ($settings -match '["\r\n]') { throw 'Unsupported settings path.' }
if ($FfmpegPath) { $FfmpegPath = (Resolve-Path -LiteralPath $FfmpegPath).Path }
$sources = @($SourceFile | ForEach-Object { (Resolve-Path -LiteralPath $_).Path } | Sort-Object -Unique)
$rsp = Join-Path $output 'tests.rsp'

function Get-InputHashes {
    $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($test in $tests) {
        foreach ($suffix in @('.dll', '.pdb', '.deps.json', '.runtimeconfig.json')) {
            $path = Join-Path (Split-Path -Parent $test) ([IO.Path]::GetFileNameWithoutExtension($test) + $suffix)
            if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing test input: $path" }
            [void]$paths.Add($path)
        }
    }
    $identities = @{}
    foreach ($folder in @($tests | ForEach-Object { Split-Path -Parent $_ } | Sort-Object -Unique)) {
        foreach ($dll in @(Get-ChildItem -LiteralPath $folder -Filter 'FoodDiary*.dll' -File | Sort-Object FullName)) {
            $hash = (Get-FileHash -LiteralPath $dll.FullName -Algorithm SHA256).Hash
            if ($identities.ContainsKey($dll.Name) -and $identities[$dll.Name] -ne $hash) {
                throw "Inconsistent FoodDiary dependency copies: $($dll.Name)"
            }
            $identities[$dll.Name] = $hash
            [void]$paths.Add($dll.FullName)
            $pdb = [IO.Path]::ChangeExtension($dll.FullName, '.pdb')
            if (-not (Test-Path -LiteralPath $pdb -PathType Leaf)) { throw "Missing FoodDiary PDB: $pdb" }
            $pdbName = [IO.Path]::GetFileName($pdb)
            $pdbHash = (Get-FileHash -LiteralPath $pdb -Algorithm SHA256).Hash
            if ($identities.ContainsKey($pdbName) -and $identities[$pdbName] -ne $pdbHash) {
                throw "Inconsistent FoodDiary symbol copies: $pdbName"
            }
            $identities[$pdbName] = $pdbHash
            [void]$paths.Add($pdb)
        }
    }
    foreach ($source in $sources) { [void]$paths.Add($source) }
    foreach ($configuration in @($settings, $rsp)) {
        if (Test-Path -LiteralPath $configuration -PathType Leaf) { [void]$paths.Add($configuration) }
    }
    $hashes = [ordered]@{}
    foreach ($path in @($paths | Sort-Object)) {
        $hashes[$path] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $hashes
}

[void](Get-InputHashes)
$initialHead = (& git -C $repo rev-parse HEAD).Trim()
$initialStatus = @(& git -C $repo status --porcelain=v1)
$sdk = (& $dotnet --version | Out-String).Trim()
if ($LASTEXITCODE -ne 0) { throw 'dotnet prerequisite check failed.' }
$collectorVersion = if ($dotCover) { (& $dotCover version | Out-String).Trim() } else { $null }
if ($dotCover -and $LASTEXITCODE -ne 0) { throw 'dotCover prerequisite check failed; inspect this tool version before using the helper.' }
[IO.Directory]::CreateDirectory($output) | Out-Null
if (-not $RunSettings) {
    [IO.File]::WriteAllText($settings, '<RunSettings><RunConfiguration><MaxCpuCount>1</MaxCpuCount></RunConfiguration></RunSettings>')
}
# VSTest response files use one argument per line; quotes become literal path characters.
[IO.File]::WriteAllLines($rsp, [string[]]$tests, [Text.UTF8Encoding]::new($false))
$initialHashes = Get-InputHashes
$execution = [ordered]@{
    repository = $repo; head = $initialHead; workingStatusBefore = $initialStatus
    platform = [Runtime.InteropServices.RuntimeInformation]::OSDescription; dotnetSdk = $sdk
    dotCoverVersion = $collectorVersion; testAssemblies = $tests; selectedSourceFiles = $sources
    scopeCompleteness = 'Caller-selected inputs; not inferred from solution or test count.'
    sourceBuildProvenance = 'Not inferred; retain the preceding build command and source evidence.'
    inputHashes = $initialHashes; inputDriftDetected = $false
    excludeAssemblies = $ExcludeAssembly; excludeAttributes = $ExcludeAttribute
    ffmpegPathOverride = $FfmpegPath
    generatedRunSettings = -not [bool]$RunSettings
    runSettingsSha256 = (Get-FileHash -LiteralPath $settings -Algorithm SHA256).Hash.ToLowerInvariant()
    runs = @(); error = $null
}
$oldFfmpeg = [Environment]::GetEnvironmentVariable('FOODDIARY_TEST_FFMPEG', 'Process')
$exitCode = 0
try {
    if ($FfmpegPath) { [Environment]::SetEnvironmentVariable('FOODDIARY_TEST_FFMPEG', $FfmpegPath, 'Process') }
    $modes = if ($Mode -eq 'both') { @('normal', 'profile') } else { @($Mode) }
    foreach ($runMode in $modes) {
        $currentHashes = Get-InputHashes
        if (($initialHashes | ConvertTo-Json -Compress) -cne ($currentHashes | ConvertTo-Json -Compress)) {
            $execution.inputDriftDetected = $true
            throw 'Frozen input inventory/content changed before execution.'
        }
        $results = Join-Path $output "$runMode-results"
        $log = Join-Path $output "$runMode.log"
        $testArguments = @('vstest', "@$rsp", "/Settings:$settings", '/Logger:trx', "/ResultsDirectory:$results")
        $executable = $dotnet
        $arguments = $testArguments
        if ($runMode -eq 'profile') {
            $executable = $dotCover
            # Arguments are passed to a native process, never evaluated by a shell.
            $targetArguments = ($testArguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
            $arguments = @('cover', '--target-executable', $dotnet, '--target-arguments', $targetArguments,
                '--target-working-directory', $repo, '--snapshot-output', (Join-Path $output 'profile.dcvr'),
                '--xml-report-output', (Join-Path $output 'profile.xml'))
            if ($ExcludeAssembly.Count) { $arguments += @('--exclude-assemblies', ($ExcludeAssembly -join ',')) }
            if ($ExcludeAttribute.Count) { $arguments += @('--exclude-attributes', ($ExcludeAttribute -join ',')) }
        }
        & $executable @arguments *> $log
        $exitCode = $LASTEXITCODE
        $execution.runs += [ordered]@{mode = $runMode; executable = $executable; arguments = $arguments
            processExitCode = $exitCode; log = $log; resultsDirectory = $results}
        $currentHashes = Get-InputHashes
        if (($initialHashes | ConvertTo-Json -Compress) -cne ($currentHashes | ConvertTo-Json -Compress)) {
            $execution.inputDriftDetected = $true
            throw 'Frozen input inventory/content changed during execution.'
        }
        if ($exitCode -ne 0) { break }
    }
    if ((& git -C $repo rev-parse HEAD).Trim() -ne $initialHead) { throw 'HEAD changed during execution.' }
} catch {
    $execution.error = $_.Exception.Message
    $exitCode = 1
} finally {
    if ($FfmpegPath) { [Environment]::SetEnvironmentVariable('FOODDIARY_TEST_FFMPEG', $oldFfmpeg, 'Process') }
    $execution.workingStatusAfter = @(& git -C $repo status --porcelain=v1)
    [IO.File]::WriteAllText((Join-Path $output 'execution.json'), ($execution | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
}
Write-Output "Execution receipts: $(Join-Path $output 'execution.json')"
Write-Output 'Validate TRX, reviewed skips and profiler health with check_run.py before accepting coverage.'
if ($execution.error) { Write-Output $execution.error }
exit $exitCode
