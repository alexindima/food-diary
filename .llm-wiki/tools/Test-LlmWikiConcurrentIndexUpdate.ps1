[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki'))
$testRoot = Join-Path $artifactRoot "concurrent-index-test-$([guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $testRoot -Force
$shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$pipelinePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Invoke-LlmWikiIndexPipeline.ps1'))
$gitDirectory = (& git -C $repositoryRoot rev-parse --absolute-git-dir).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitDirectory)) {
    throw 'Unable to resolve the Git directory for the concurrent index update test.'
}
$updateLockPath = Join-Path $gitDirectory 'llm-wiki/index-transactions/update.lock'
$null = New-Item -ItemType Directory -Path (Split-Path -Parent $updateLockPath) -Force
$arguments = @(
    '-NoLogo', '-NoProfile', '-File', $pipelinePath,
    '-AffectedOnly', '-ChangedPath', '.llm-wiki/tools/Build-LlmWikiDomainDataIndex.ps1',
    '-ReuseUnchangedChecks'
)
$firstOutputPath = Join-Path $testRoot 'first.out.log'
$firstErrorPath = Join-Path $testRoot 'first.err.log'
$secondOutputPath = Join-Path $testRoot 'second.out.log'
$secondErrorPath = Join-Path $testRoot 'second.err.log'
$first = $null
$second = $null
$windowParameters = @{}
if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    $windowParameters.WindowStyle = 'Hidden'
}

try {
    $first = Start-Process @windowParameters `
        -FilePath $shellPath `
        -ArgumentList $arguments `
        -WorkingDirectory $repositoryRoot `
        -RedirectStandardOutput $firstOutputPath `
        -RedirectStandardError $firstErrorPath `
        -PassThru

    $ownershipObserved = $false
    $ownershipDeadline = [DateTime]::UtcNow.AddSeconds(45)
    while ([DateTime]::UtcNow -lt $ownershipDeadline) {
        try {
            $lockProbe = [IO.File]::Open(
                $updateLockPath,
                [IO.FileMode]::OpenOrCreate,
                [IO.FileAccess]::ReadWrite,
                [IO.FileShare]::None)
            $lockProbe.Dispose()
        } catch [IO.IOException] {
            $ownershipObserved = $true
            break
        }
        if ($first.HasExited) { throw 'First concurrent refresh exited before the lock ownership was observed.' }
        Start-Sleep -Milliseconds 50
    }
    if (-not $ownershipObserved) { throw 'Timed out waiting for the first concurrent refresh to acquire the update lock.' }

    $second = Start-Process @windowParameters `
        -FilePath $shellPath `
        -ArgumentList $arguments `
        -WorkingDirectory $repositoryRoot `
        -RedirectStandardOutput $secondOutputPath `
        -RedirectStandardError $secondErrorPath `
        -PassThru

    $first.WaitForExit()
    $second.WaitForExit()
    $firstOutput = Get-Content -LiteralPath $firstOutputPath -Raw
    $secondOutput = Get-Content -LiteralPath $secondOutputPath -Raw
    $firstError = Get-Content -LiteralPath $firstErrorPath -Raw
    $secondError = Get-Content -LiteralPath $secondErrorPath -Raw
    if ($first.ExitCode -ne 0 -or $second.ExitCode -ne 0) {
        throw "Concurrent refresh failed: first=$($first.ExitCode) $firstError; second=$($second.ExitCode) $secondError"
    }
    if ($secondOutput -notmatch 'Another LLM Wiki index update is running' -or
        $secondOutput -notmatch 'Concurrent LLM Wiki index result reused') {
        throw "Second refresh did not wait for and reuse the first refresh result: $secondOutput"
    }
    if ($secondOutput -match 'Generated \.llm-wiki/generated/domain-data-index\.json') {
        throw 'Second refresh reran the generator after receiving matching concurrent evidence.'
    }

    Write-Host 'LLM Wiki concurrent index update regression passed: the waiter reused the matching completed refresh.'
} finally {
    foreach ($process in @($first, $second)) {
        if ($null -eq $process) { continue }
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            $process.WaitForExit()
        }
        $process.Dispose()
    }
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    if ($resolvedTestRoot.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTestRoot)) {
        for ($attempt = 1; $attempt -le 50; $attempt++) {
            try {
                Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
                break
            } catch {
                if ($attempt -eq 50) {
                    Write-Warning "Unable to remove concurrent refresh test artifacts after 50 attempts: $resolvedTestRoot"
                    break
                }
                Start-Sleep -Milliseconds 100
            }
        }
    }
}
