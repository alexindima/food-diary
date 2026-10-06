$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
$shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("llm-wiki-process-" + [guid]::NewGuid().ToString('N'))
$pidPath = Join-Path $tempRoot 'grandchild.pid'
$childScript = Join-Path $tempRoot 'child.ps1'
$process = $null
$grandchild = $null
$windowArgument = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) { '-WindowStyle Hidden' } else { '' }
$startOptions = if ($windowArgument) { @{ WindowStyle = 'Hidden' } } else { @{} }
try {
    $null = New-Item -ItemType Directory -Path $tempRoot -Force
    @"
`$grandchild = Start-Process -FilePath '$($shellPath.Replace("'", "''"))' -ArgumentList '-NoLogo','-NoProfile','-Command','Start-Sleep -Seconds 120' -PassThru $windowArgument
[IO.File]::WriteAllText('$($pidPath.Replace("'", "''"))', [string]`$grandchild.Id)
Start-Sleep -Seconds 120
"@ | Set-Content -LiteralPath $childScript -Encoding utf8
    $process = Start-Process -FilePath $shellPath -ArgumentList '-NoLogo','-NoProfile','-File',$childScript -PassThru @startOptions
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (-not (Test-Path -LiteralPath $pidPath) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 50 }
    if (-not (Test-Path -LiteralPath $pidPath)) { throw 'Grandchild process did not start.' }
    $grandchildId = [int](Get-Content -LiteralPath $pidPath -Raw)
    $grandchild = [Diagnostics.Process]::GetProcessById($grandchildId)
    if (Wait-LlmWikiProcessTermination -Process $grandchild -WaitMilliseconds 100) {
        throw 'Termination check accepted a live grandchild before cleanup.'
    }
    $global:LASTEXITCODE = 37
    Stop-LlmWikiProcessTree -Process $process
    if ($global:LASTEXITCODE -ne 37) { throw 'Process cleanup overwrote the caller native failure code.' }
    if (-not (Wait-LlmWikiProcessTermination -Process $grandchild)) { throw "Grandchild process $grandchildId survived process-tree termination." }
    foreach ($state in @('absent', 'success')) {
        $owned = $null
        try {
            $owned = Start-Process -FilePath $shellPath -ArgumentList '-NoLogo','-NoProfile','-Command','Start-Sleep -Seconds 120' -PassThru @startOptions
            if ($state -eq 'absent') { Remove-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue }
            else { $global:LASTEXITCODE = 0 }
            Stop-LlmWikiProcessTree -Process $owned
            $actualExitCode = Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue
            if (($state -eq 'absent' -and $null -ne $actualExitCode) -or
                ($state -eq 'success' -and ($null -eq $actualExitCode -or $actualExitCode.Value -ne 0))) {
                throw "Process cleanup changed the caller native exit state: $state"
            }
        } finally {
            if ($owned) {
                if (-not $owned.HasExited) { Stop-LlmWikiProcessTree -Process $owned }
                $owned.Dispose()
            }
        }
    }
    Write-Host 'LLM Wiki process-tree smoke passed: live descendants are rejected and terminated descendants are recognized.'
} finally {
    if ($process -and -not $process.HasExited) { Stop-LlmWikiProcessTree -Process $process }
    if ($grandchild) { $grandchild.Dispose() }
    $tempParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\', '/')
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($tempRoot)) -ne $tempParent) {
        throw 'Process fixture cleanup escaped its temporary directory.'
    }
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
