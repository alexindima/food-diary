#Requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot 'DockerReadiness.psm1'
$savedEnvironment = @{}
foreach ($name in @('CI', 'DOCKER_HOST', 'DOCKER_CONTEXT')) {
    $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
    [Environment]::SetEnvironmentVariable($name, $null)
}

function New-Reply([string]$Command, [int]$ExitCode = 0, [string]$Output = '') {
    return [pscustomobject]@{
        Command = $Command; ExitCode = $ExitCode; Output = $Output
        StandardOutput = $Output; TimedOut = $false
    }
}

function Assert-Scenario {
    param(
        [string]$Name, [object[]]$Replies, [string]$ExpectedError = '',
        [switch]$NoStart, [switch]$MissingCli, [int]$ExpectedStarts = 0
    )
    $module = Import-Module $modulePath -Force -PassThru
    & $module {
        param($Replies, $MissingCli)
        $script:replies = [Collections.Generic.Queue[object]]::new()
        foreach ($reply in $Replies) { $script:replies.Enqueue($reply) }
        $script:calls = [Collections.Generic.List[string]]::new()
        $script:missingCli = $MissingCli
        function script:Resolve-DockerExecutable {
            if ($script:missingCli) { throw 'Docker CLI is missing.' }
            return 'fixture-docker'
        }
        function script:Invoke-DockerCommand {
            param($FilePath, $Arguments, $TimeoutMilliseconds)
            if ($TimeoutMilliseconds -le 0 -or $TimeoutMilliseconds -gt 1000) { throw 'Probe exceeded the remaining deadline.' }
            $command = $Arguments[0]
            $script:calls.Add($command)
            if ($script:replies.Count -eq 0) {
                return [pscustomobject]@{ ExitCode = 1; Output = 'engine unavailable'; StandardOutput = ''; TimedOut = $false }
            }
            $reply = $script:replies.Dequeue()
            if ($reply.Command -ne $command) { throw "Expected $($reply.Command), got $command." }
            return $reply
        }
    } $Replies $MissingCli
    $failure = ''
    try { Wait-FoodDiaryDocker -TimeoutSeconds 1 -ProbeTimeoutSeconds 1 -PollIntervalMilliseconds 1 -NoStart:$NoStart }
    catch { $failure = $_.Exception.Message }
    if ($ExpectedError) {
        if ($failure -notlike "*$ExpectedError*") { throw "$Name expected '$ExpectedError', got '$failure'." }
    } elseif ($failure) { throw "$Name failed: $failure" }
    $actualStarts = & $module { @($script:calls | Where-Object { $_ -eq 'desktop' }).Count }
    $pending = & $module { $script:replies.Count }
    if ($actualStarts -ne $ExpectedStarts -or $pending -ne 0) {
        throw "${Name}: expected $ExpectedStarts startups, got $actualStarts; unconsumed replies: $pending."
    }
    Write-Host "Passed: $Name"
}

try {
    Assert-Scenario 'already ready' @((New-Reply info 0 linux))
    Assert-Scenario 'missing CLI' @() -MissingCli -ExpectedError 'Docker CLI is missing'
    Assert-Scenario 'Windows containers' @((New-Reply info 0 windows)) -ExpectedError 'require Linux containers'
    Assert-Scenario 'Desktop starts before Engine is ready' @(
        (New-Reply info 1 unavailable),
        (New-Reply context 0 'npipe:////./pipe/dockerDesktopLinuxEngine'),
        (New-Reply desktop),
        (New-Reply info 1 starting),
        (New-Reply info 0 linux)
    ) -ExpectedStarts 1
    Assert-Scenario 'Desktop startup failure can still recover' @(
        (New-Reply info 1 unavailable),
        (New-Reply context 0 'unix:///home/test/.docker/desktop/docker.sock'),
        (New-Reply desktop 1 'already starting'),
        (New-Reply info 0 linux)
    ) -ExpectedStarts 1
    Assert-Scenario 'Desktop failure reaches deadline' @(
        (New-Reply info 1 unavailable),
        (New-Reply context 0 'npipe:////./pipe/docker_engine'),
        (New-Reply desktop 1 failed)
    ) -ExpectedStarts 1 -ExpectedError 'startup command failed or timed out'
    Assert-Scenario 'NoStart waits for externally started Engine' @(
        (New-Reply info 1 unavailable), (New-Reply info 0 linux)
    ) -NoStart
    Assert-Scenario 'unavailable Engine reaches deadline' @((New-Reply info 1 unavailable)) `
        -NoStart -ExpectedError 'not ready within 1 seconds'
    Assert-Scenario 'remote context never starts local Desktop' @(
        (New-Reply info 1 unavailable), (New-Reply context 0 'ssh://example.invalid')
    ) -ExpectedError 'start that engine separately'
    Assert-Scenario 'native Linux daemon is not treated as Desktop' @(
        (New-Reply info 1 unavailable), (New-Reply context 0 'unix:///var/run/docker.sock')
    ) -ExpectedError 'start that engine separately'
    $env:DOCKER_HOST = 'tcp://example.invalid:2376'
    Assert-Scenario 'explicit remote host is preserved' @((New-Reply info 1 unavailable)) `
        -ExpectedError 'start that engine separately'
    $env:DOCKER_CONTEXT = 'desktop-linux'
    Assert-Scenario 'context takes precedence over host' @(
        (New-Reply info 1 unavailable),
        (New-Reply context 0 'npipe:////./pipe/dockerDesktopLinuxEngine'),
        (New-Reply desktop), (New-Reply info 0 linux)
    ) -ExpectedStarts 1
    $env:CI = 'true'
    Assert-Scenario 'CI never starts Desktop' @((New-Reply info 1 unavailable)) `
        -ExpectedError 'Automatic Desktop startup is disabled'

    # Exercise the real process boundary: preserve stdout/stderr and exit status,
    # and terminate a hung child rather than trusting an unbounded Docker CLI.
    $module = Import-Module $modulePath -Force -PassThru
    & $module {
        param($PowerShellPath)
        $result = Invoke-DockerCommand $PowerShellPath @(
            '-NoLogo', '-NoProfile', '-Command', '[Console]::Out.Write("linux"); [Console]::Error.Write("warning"); exit 7'
        ) 10000
        if ($result.ExitCode -ne 7 -or $result.StandardOutput -ne 'linux' -or $result.Output -notlike '*warning*') {
            throw 'Native command lost its exit code or redirected output.'
        }
        $clock = [Diagnostics.Stopwatch]::StartNew()
        $result = Invoke-DockerCommand $PowerShellPath @(
            '-NoLogo', '-NoProfile', '-Command', '[Threading.Thread]::Sleep(30000)'
        ) 300
        if (-not $result.TimedOut -or $result.ExitCode -eq 0 -or $clock.Elapsed.TotalSeconds -gt 5) {
            throw 'Hung native command did not terminate within its bounded timeout.'
        }
    } (Get-Process -Id $PID).Path
    Write-Host 'Passed: real native process output, failure and timeout'

    # Run the actual group runner in a tiny repository fixture. Docker and dotnet
    # are replaced at their boundaries; no test here starts the user's engine.
    $fixtureParent = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../.artifacts/docker-readiness-tests'))
    $fixtureRoot = Join-Path $fixtureParent ([guid]::NewGuid().ToString('N'))
    $fixtureScripts = Join-Path $fixtureRoot 'scripts'
    $fixtureCi = Join-Path $fixtureScripts 'ci'
    [void][IO.Directory]::CreateDirectory($fixtureCi)
    $dockerMarker = Join-Path $fixtureRoot 'docker-called'
    $dotnetMarker = Join-Path $fixtureRoot 'dotnet-called'
    $rejectMarker = Join-Path $fixtureRoot 'reject-docker'
    function dotnet {
        [IO.File]::WriteAllText($dotnetMarker, 'dotnet invoked')
        throw 'Fixture reached dotnet.'
    }
    try {
        $fixtureRunner = Join-Path $fixtureCi 'Invoke-BackendTests.ps1'
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ci/Invoke-BackendTests.ps1') -Destination $fixtureRunner
        [IO.File]::WriteAllText((Join-Path $fixtureScripts 'Ensure-Docker.ps1'), @'
param([int]$TimeoutSeconds, [switch]$NoStart)
$root = Join-Path $PSScriptRoot '..'
[IO.File]::WriteAllText((Join-Path $root 'docker-called'), "$TimeoutSeconds/$NoStart")
if (Test-Path -LiteralPath (Join-Path $root 'reject-docker')) { throw 'Fixture Docker unavailable.' }
'@)
        $groups = @('fast', 'integration-1', 'integration-2', 'integration-3', 'mcp')
        $plan = @{ schemaVersion = 1; groups = @() }
        $projects = @()
        foreach ($group in $groups) {
            $project = "$group.Tests.csproj"
            $projects += "<Project Path=`"$project`" />"
            [IO.File]::WriteAllText((Join-Path $fixtureRoot $project),
                '<Project><ItemGroup><PackageReference Include="Microsoft.NET.Test.Sdk" /></ItemGroup></Project>')
            $plan.groups += @{ name = $group; projects = @($project) }
        }
        [IO.File]::WriteAllText((Join-Path $fixtureRoot 'FoodDiary.slnx'), "<Solution>$($projects -join '')</Solution>")
        [void][IO.Directory]::CreateDirectory((Join-Path $fixtureRoot 'FoodDiary.Analyzers'))
        [IO.File]::WriteAllText((Join-Path $fixtureRoot 'FoodDiary.Analyzers/FoodDiary.Analyzers.csproj'), '<Project />')
        $plan | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $fixtureCi 'backend-test-groups.json')

        [IO.File]::WriteAllText($rejectMarker, 'reject')
        $failure = ''
        try { & $fixtureRunner -Group integration-1 *> $null } catch { $failure = $_.Exception.Message }
        if ((Test-Path -LiteralPath $dotnetMarker) -or $failure -notlike '*Fixture Docker unavailable*') {
            throw "Docker readiness did not block dotnet before restore: $failure"
        }
        $failureMetadata = Get-Content -LiteralPath (Join-Path $fixtureRoot '.artifacts/backend-ci/integration-1/logs/integration-1-failure-meta.env') -Raw
        if ($failureMetadata -notlike '*FAILED_AREA=docker*') { throw 'Docker failure was mislabeled as a dotnet failure.' }
        $dockerLog = Get-Content -LiteralPath (Join-Path $fixtureRoot '.artifacts/backend-ci/integration-1/logs/integration-1-docker.log') -Raw
        if ($dockerLog -notlike '*Fixture Docker unavailable*') { throw 'Docker failure diagnostic was not retained in its log.' }
        Remove-Item -LiteralPath $rejectMarker, $dockerMarker
        foreach ($group in $groups) {
            & $fixtureRunner -Group $group -PlanOnly | Out-Null
        }
        if (Test-Path -LiteralPath $dockerMarker) { throw 'PlanOnly invoked Docker.' }

        foreach ($group in $groups) {
            $failure = ''
            try { & $fixtureRunner -Group $group -DockerTimeoutSeconds 7 -NoDockerStart *> $null }
            catch { $failure = $_.Exception.Message }
            if ($failure -notlike '*Fixture reached dotnet*') { throw "Unexpected runner failure for ${group}: $failure" }
            $requiresDocker = $group -like 'integration-*'
            if ((Test-Path -LiteralPath $dockerMarker) -ne $requiresDocker) { throw "Wrong Docker dependency for $group." }
            if ($requiresDocker) {
                if ((Get-Content -LiteralPath $dockerMarker -Raw) -ne '7/True') { throw 'Runner lost Docker readiness controls.' }
                Remove-Item -LiteralPath $dockerMarker
            }
            Remove-Item -LiteralPath $dotnetMarker
        }
        Write-Host 'Passed: runner preflight order, failure diagnostics, integration groups, fast/MCP bypass and PlanOnly'
    } finally {
        $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
        if (-not $resolvedFixture.StartsWith($fixtureParent + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
            throw "Refusing unsafe fixture cleanup: $resolvedFixture"
        }
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
} finally {
    Remove-Module DockerReadiness -ErrorAction SilentlyContinue
    foreach ($entry in $savedEnvironment.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable($entry.Key, $entry.Value)
    }
}
