Set-StrictMode -Version Latest

function ConvertFrom-LlmWikiGitNameStatus {
    param([string[]]$Lines)

    @(
        foreach ($line in @($Lines)) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            $parts = @($line -split "`t")
            if ($parts.Count -lt 3 -or $parts[0] -notmatch '^R\d{0,3}$') { continue }
            [pscustomobject][ordered]@{
                status = [string]$parts[0]
                from = ([string]$parts[1]).Replace('\', '/')
                to = ([string]$parts[2]).Replace('\', '/')
            }
        }
    )
}

function Get-LlmWikiGitRenames {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$BaseRef,
        [string]$HeadRef
    )

    $arguments = @('-c', 'core.quotepath=false', '-C', $RepositoryRoot, 'diff', '--name-status', '--find-renames', '--diff-filter=R', $BaseRef)
    if (-not [string]::IsNullOrWhiteSpace($HeadRef) -and $HeadRef -ine 'HEAD') { $arguments += $HeadRef }
    $arguments += '--'
    # Invoke git via Diagnostics.Process with stderr captured through .NET instead of
    # PowerShell's native-command stream: under Set-StrictMode/$ErrorActionPreference =
    # 'Stop', the `&` call operator can promote even a harmless git warning (e.g. "CRLF
    # will be replaced by LF", common with core.autocrlf=true on Windows) into a
    # terminating NativeCommandError, and that promotion happens regardless of a local
    # `2>$null` redirect in some hosts (verified: reproduces from a fresh
    # `powershell.exe -File` invocation even with `2>$null` present). Routing stderr
    # through .NET's ReadToEndAsync never touches that PowerShell machinery at all.
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'git'
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    $startInfo.StandardErrorEncoding = [Text.UTF8Encoding]::new($false)
    $startInfo.Arguments = (@($arguments | ForEach-Object { '"' + ([string]$_).Replace('"', '\"') + '"' }) -join ' ')
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $stdout = ''
    $stderr = ''
    $exitCode = -1
    try {
        if (-not $process.Start()) { throw "Unable to start git rename discovery for base '$BaseRef' and head '$HeadRef'." }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = [string]$stdoutTask.GetAwaiter().GetResult()
        $stderr = [string]$stderrTask.GetAwaiter().GetResult()
        $exitCode = $process.ExitCode
    } finally { $process.Dispose() }
    if ($exitCode -ne 0) { throw "git rename discovery failed for base '$BaseRef' and head '$HeadRef'.$(if ($stderr.Trim()) { " $($stderr.Trim())" })" }
    $lines = @($stdout -split '\r?\n')
    @(ConvertFrom-LlmWikiGitNameStatus $lines)
}

function Test-LlmWikiRenameDestination {
    param(
        [Parameter(Mandatory)][string]$Path,
        [object[]]$Renames,
        [string[]]$KnownPaths
    )

    $normalized = $Path.Replace('\', '/')
    foreach ($rename in @($Renames)) {
        if ($null -eq $rename -or -not $rename.PSObject.Properties['from'] -or -not $rename.PSObject.Properties['to']) { continue }
        $from = ([string]$rename.from).Replace('\', '/')
        $to = ([string]$rename.to).Replace('\', '/')
        $fromDirectory = if ($from.Contains('/')) { $from.Substring(0, $from.LastIndexOf('/')) } else { '' }
        $toDirectory = if ($to.Contains('/')) { $to.Substring(0, $to.LastIndexOf('/')) } else { '' }
        $isExactDestination = $normalized -ceq $to
        $isInsideDestination = -not [string]::IsNullOrWhiteSpace($toDirectory) -and
            $normalized.StartsWith("$toDirectory/", [StringComparison]::Ordinal)
        if (-not $isExactDestination -and -not $isInsideDestination) { continue }
        if ($from -in @($KnownPaths) -or @($KnownPaths | Where-Object {
            $known = ([string]$_).Replace('\', '/').TrimEnd('/')
            $from -eq $known -or
                $from.StartsWith("$known/", [StringComparison]::Ordinal) -or
                (-not [string]::IsNullOrWhiteSpace($fromDirectory) -and
                    ($known.StartsWith("$fromDirectory/", [StringComparison]::Ordinal) -or $known -ceq $fromDirectory))
        }).Count -gt 0) { return $true }
    }
    return $false
}
