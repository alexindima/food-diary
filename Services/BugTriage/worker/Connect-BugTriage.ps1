# Open a local-only tunnel using the separately provisioned forwarding-only SSH key.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$taskPort = 15099
$taskProbe = [System.Net.Sockets.TcpClient]::new()
try {
    $taskProbe.Connect('127.0.0.1', $taskPort)
    Write-Output 'BugTriage local port is already listening; the API token verifies subsequent requests.'
    return
} catch {
    # No existing tunnel. OpenSSH must fail rather than silently reuse a busy port.
} finally {
    $taskProbe.Dispose()
}
$taskArguments = @('-N', '-T', '-o', 'BatchMode=yes', '-o', 'ExitOnForwardFailure=yes',
    '-o', 'ServerAliveInterval=30', '-o', 'ServerAliveCountMax=3',
    '-L', '127.0.0.1:15099:127.0.0.1:5099', 'fooddiary-bugtriage')
$taskProcess = Start-Process ssh -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 2
if ($taskProcess.HasExited) { throw 'BugTriage SSH tunnel failed to start.' }
Write-Output 'BugTriage SSH tunnel started on 127.0.0.1:15099.'
