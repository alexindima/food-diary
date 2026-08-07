$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiProcess.ps1')
$shellPath = [IO.Path]::GetFullPath((Get-Process -Id $PID).Path)
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ("llm-wiki-process-" + [guid]::NewGuid().ToString('N'))
$pidPath = Join-Path $tempRoot 'grandchild.pid'
$childScript = Join-Path $tempRoot 'child.ps1'
$process = $null
try {
    $null = New-Item -ItemType Directory -Path $tempRoot -Force
    @"
`$grandchild = Start-Process -FilePath '$($shellPath.Replace("'", "''"))' -ArgumentList '-NoLogo','-NoProfile','-Command','Start-Sleep -Seconds 120' -PassThru
[IO.File]::WriteAllText('$($pidPath.Replace("'", "''"))', [string]`$grandchild.Id)
Start-Sleep -Seconds 120
"@ | Set-Content -LiteralPath $childScript -Encoding utf8
    $process = Start-Process -FilePath $shellPath -ArgumentList '-NoLogo','-NoProfile','-File',$childScript -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (-not (Test-Path -LiteralPath $pidPath) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 50 }
    if (-not (Test-Path -LiteralPath $pidPath)) { throw 'Grandchild process did not start.' }
    $grandchildId = [int](Get-Content -LiteralPath $pidPath -Raw)
    Stop-LlmWikiProcessTree -Process $process
    Start-Sleep -Milliseconds 150
    $grandchildAlive = $false
    try { $grandchildAlive = -not ([Diagnostics.Process]::GetProcessById($grandchildId)).HasExited } catch { $grandchildAlive = $false }
    if ($grandchildAlive) { throw "Grandchild process $grandchildId survived process-tree termination." }
    Write-Host 'LLM Wiki process-tree smoke passed: timed-out descendants are terminated.'
} finally {
    if ($process -and -not $process.HasExited) { Stop-LlmWikiProcessTree -Process $process }
    if (Test-Path -LiteralPath $tempRoot) { Remove-Item -LiteralPath $tempRoot -Recurse -Force }
}
