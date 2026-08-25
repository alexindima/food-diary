[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$wikiPath = Join-Path $repositoryRoot '.llm-wiki/wiki.ps1'
$runRoot = Join-Path $repositoryRoot '.artifacts/llm-wiki/verify-runs'
$fixtureRoot = Join-Path $repositoryRoot ".artifacts/llm-wiki/concurrent-verify-$PID-$([guid]::NewGuid().ToString('N'))"
$latestProgressPath = Join-Path $repositoryRoot '.artifacts/llm-wiki/verify-progress.json'
$latestProgressBefore = if (Test-Path -LiteralPath $latestProgressPath -PathType Leaf) { Get-Content -LiteralPath $latestProgressPath -Raw } else { $null }
$previousMetricsRoot = $env:LLM_WIKI_WORKFLOW_METRICS_ROOT
$previousVerificationTelemetryPath = $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH
$previousCi = $env:CI
$shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$processes = [Collections.Generic.List[Diagnostics.Process]]::new()
$createdRunDirectories = @()
$runIdPrefix = "concurrent-test-$PID-$([guid]::NewGuid().ToString('N'))"

try {
    $null = New-Item -ItemType Directory -Path $fixtureRoot -Force
    $env:LLM_WIKI_WORKFLOW_METRICS_ROOT = Join-Path $fixtureRoot 'workflow-metrics'
    $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH = Join-Path $fixtureRoot 'verification-telemetry.json'
    $env:CI = 'true'
    for ($index = 1; $index -le 2; $index++) {
        $runId = "$runIdPrefix-$index"
        $stdoutPath = Join-Path $fixtureRoot "verify-$index.stdout.log"
        $stderrPath = Join-Path $fixtureRoot "verify-$index.stderr.log"
        $process = Start-Process `
            -FilePath $shellPath `
            -ArgumentList @('-NoLogo', '-NoProfile', '-File', "`"$wikiPath`"", 'verify', '-Stage', '"workspace policy"', '-ChangedPath', '.llm-wiki/wiki.ps1', '-VerifyRunId', $runId) `
            -WorkingDirectory $repositoryRoot `
            -WindowStyle Hidden `
            -RedirectStandardOutput $stdoutPath `
            -RedirectStandardError $stderrPath `
            -PassThru
        $processes.Add($process)
    }
    $deadline = [DateTime]::UtcNow.AddSeconds(90)
    foreach ($process in $processes) {
        $remainingMilliseconds = [Math]::Max(1, [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds)
        if (-not $process.WaitForExit($remainingMilliseconds)) {
            try { $process.Kill($true) } catch { }
            throw 'Concurrent verify regression timed out.'
        }
        if ($process.ExitCode -ne 0) {
            $output = Get-Content -LiteralPath (Join-Path $fixtureRoot "verify-$($processes.IndexOf($process) + 1).stdout.log") -Raw -ErrorAction SilentlyContinue
            $errorOutput = Get-Content -LiteralPath (Join-Path $fixtureRoot "verify-$($processes.IndexOf($process) + 1).stderr.log") -Raw -ErrorAction SilentlyContinue
            throw "Concurrent verify process failed with exit code $($process.ExitCode).`n$output`n$errorOutput"
        }
    }

    $createdRunDirectories = @(1..2 | ForEach-Object { Get-Item -LiteralPath (Join-Path $runRoot "$runIdPrefix-$_") })
    if ($createdRunDirectories.Count -ne 2) {
        throw "Concurrent verify did not create two isolated run directories; observed $($createdRunDirectories.Count)."
    }
    $progressReceipts = @($createdRunDirectories | ForEach-Object {
        $progressPath = Join-Path $_.FullName 'progress.json'
        $logPath = Join-Path $_.FullName 'logs/workspace-policy.log'
        if (-not (Test-Path -LiteralPath $progressPath -PathType Leaf) -or -not (Test-Path -LiteralPath $logPath -PathType Leaf)) {
            throw "Concurrent verify run '$($_.Name)' lost its progress receipt or stage log."
        }
        Get-Content -LiteralPath $progressPath -Raw | ConvertFrom-Json
    })
    if (@($progressReceipts.runId | Sort-Object -Unique).Count -ne 2 -or
        @($progressReceipts | Where-Object status -eq 'passed').Count -ne 2) {
        throw 'Concurrent verify receipts were overwritten or did not both pass.'
    }
    Write-Host 'LLM Wiki concurrent verify regression passed: two runs retained independent progress and stage logs.'
} finally {
    foreach ($process in $processes) { $process.Dispose() }
    if ([string]::IsNullOrWhiteSpace([string]$previousMetricsRoot)) { Remove-Item Env:LLM_WIKI_WORKFLOW_METRICS_ROOT -ErrorAction SilentlyContinue }
    else { $env:LLM_WIKI_WORKFLOW_METRICS_ROOT = $previousMetricsRoot }
    if ([string]::IsNullOrWhiteSpace([string]$previousVerificationTelemetryPath)) { Remove-Item Env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH -ErrorAction SilentlyContinue }
    else { $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH = $previousVerificationTelemetryPath }
    if ([string]::IsNullOrWhiteSpace([string]$previousCi)) { Remove-Item Env:CI -ErrorAction SilentlyContinue }
    else { $env:CI = $previousCi }
    $runPrefix = [IO.Path]::GetFullPath($runRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    foreach ($directory in $createdRunDirectories) {
        $resolved = [IO.Path]::GetFullPath($directory.FullName)
        if ($resolved.StartsWith($runPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolved -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    if ($null -eq $latestProgressBefore) { Remove-Item -LiteralPath $latestProgressPath -Force -ErrorAction SilentlyContinue }
    else { [IO.File]::WriteAllText($latestProgressPath, $latestProgressBefore, [Text.UTF8Encoding]::new($false)) }
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
