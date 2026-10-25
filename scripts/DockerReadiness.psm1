#Requires -Version 7.0
Set-StrictMode -Version Latest

function Resolve-DockerExecutable {
    $command = Get-Command docker -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $command) { throw 'Docker CLI is missing. Install Docker Desktop and make docker available on PATH.' }
    return $command.Source
}

function Invoke-DockerCommand {
    param([string]$FilePath, [string[]]$Arguments, [int]$TimeoutMilliseconds)

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FilePath
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $startInfo.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $commandTimer = [Diagnostics.Stopwatch]::StartNew()
    try {
        [void]$process.Start()
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $timedOut = -not $process.WaitForExit($TimeoutMilliseconds)
        if ($timedOut) {
            # Desktop startup may spawn the application; preserve that process.
            $killTree = $Arguments.Count -eq 0 -or $Arguments[0] -ne 'desktop'
            try { $process.Kill($killTree) } catch { if (-not $process.HasExited) { throw } }
            [void]$process.WaitForExit(1000)
        }
        $drainBudget = [Math]::Max(1, $TimeoutMilliseconds - [int]$commandTimer.ElapsedMilliseconds)
        [void][Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdout, $stderr), $drainBudget)
        $standardOutput = if ($stdout.IsCompletedSuccessfully) { $stdout.Result.Trim() } else { '' }
        $output = @(
            $standardOutput
            if ($stderr.IsCompletedSuccessfully) { $stderr.Result.Trim() }
        ) -join [Environment]::NewLine
        return [pscustomobject]@{
            ExitCode = if ($timedOut) { -1 } else { $process.ExitCode }
            Output = $output.Trim()
            StandardOutput = $standardOutput
            TimedOut = $timedOut
        }
    } finally { $process.Dispose() }
}

function Test-DesktopEndpoint {
    param([string]$Endpoint)

    # Never start a local Desktop for an SSH/TCP or custom-socket target.
    return $Endpoint -match '^npipe:/+\./pipe/docker(?:_engine|DesktopLinuxEngine)$' -or
        $Endpoint -match '^unix:///.*/\.docker/desktop/docker\.sock$'
}

function Wait-FoodDiaryDocker {
    [CmdletBinding()]
    param(
        [ValidateRange(1, 600)][int]$TimeoutSeconds = 180,
        [ValidateRange(1, 30)][int]$ProbeTimeoutSeconds = 5,
        [ValidateRange(1, 5000)][int]$PollIntervalMilliseconds = 1000,
        [switch]$NoStart
    )

    $dockerPath = Resolve-DockerExecutable
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $deadline = $TimeoutSeconds * 1000
    $startupConsidered = $false
    $startupMessage = ''
    $lastProblem = 'Docker Engine did not answer.'
    $ciRun = -not [string]::IsNullOrWhiteSpace($env:CI) -and $env:CI -notin @('false', '0')

    function Invoke-Probe([string[]]$DockerArguments) {
        $remaining = $deadline - [int]$timer.ElapsedMilliseconds
        if ($remaining -le 0) {
            return [pscustomobject]@{ ExitCode = -1; Output = 'Readiness deadline reached.'; StandardOutput = ''; TimedOut = $true }
        }
        return Invoke-DockerCommand -FilePath $dockerPath -Arguments $DockerArguments `
            -TimeoutMilliseconds ([Math]::Min($remaining, $ProbeTimeoutSeconds * 1000))
    }

    Write-Host "Checking Docker Engine (timeout: $TimeoutSeconds seconds)..."
    while ($timer.ElapsedMilliseconds -lt $deadline) {
        $probe = Invoke-Probe @('info', '--format', '{{.OSType}}')
        if ($probe.ExitCode -eq 0 -and $probe.StandardOutput -eq 'linux') {
            Write-Host 'Docker Engine is ready (Linux containers).'
            return
        }
        if ($probe.ExitCode -eq 0 -and $probe.StandardOutput -eq 'windows') {
            throw 'FoodDiary integration tests require Linux containers. Switch Docker Desktop to Linux containers and retry.'
        }
        $lastProblem = if ($probe.TimedOut) { 'Docker Engine probe timed out.' }
            elseif ($probe.Output) { $probe.Output }
            else { 'Docker Engine returned no Linux readiness response.' }

        if (-not $startupConsidered) {
            $startupConsidered = $true
            if ($NoStart -or $ciRun) {
                $startupMessage = 'Automatic Desktop startup is disabled (CI or -NoStart).'
            } else {
                # DOCKER_CONTEXT overrides DOCKER_HOST in the Docker CLI.
                if (-not [string]::IsNullOrWhiteSpace($env:DOCKER_HOST) -and
                    [string]::IsNullOrWhiteSpace($env:DOCKER_CONTEXT)) {
                    $endpoint = $env:DOCKER_HOST
                } else {
                    $context = Invoke-Probe @('context', 'inspect', '--format', '{{.Endpoints.docker.Host}}')
                    $endpoint = if ($context.ExitCode -eq 0) { $context.StandardOutput } else { '' }
                }
                if (Test-DesktopEndpoint $endpoint) {
                    Write-Host 'Requesting Docker Desktop startup...'
                    $start = Invoke-Probe @('desktop', 'start', '--detach')
                    $startupMessage = if ($start.ExitCode -eq 0) { 'Docker Desktop startup requested.' }
                        else { 'Docker Desktop startup command failed or timed out. Check Docker Desktop and its CLI plugin.' }
                } else {
                    $startupMessage = 'The selected Docker endpoint is not a local Docker Desktop endpoint; start that engine separately.'
                }
            }
        }
        $remaining = $deadline - [int]$timer.ElapsedMilliseconds
        if ($remaining -gt 0) { Start-Sleep -Milliseconds ([Math]::Min($remaining, $PollIntervalMilliseconds)) }
    }
    throw "Docker Engine was not ready within $TimeoutSeconds seconds. $startupMessage Last probe: $lastProblem"
}

Export-ModuleMember -Function Wait-FoodDiaryDocker
