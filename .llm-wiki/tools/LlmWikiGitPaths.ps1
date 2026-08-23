Set-StrictMode -Version Latest

function Test-LlmWikiWorkspaceHeadRef {
    param([AllowEmptyString()][string]$HeadRef)

    return [string]::IsNullOrWhiteSpace($HeadRef) -or
        [string]::Equals($HeadRef.Trim(), 'HEAD', [StringComparison]::OrdinalIgnoreCase)
}

function Resolve-LlmWikiCommitRef {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Ref
    )

    $resolved = @(& git -C $RepositoryRoot rev-parse --verify "$Ref^{commit}" 2>$null)
    if ($LASTEXITCODE -ne 0 -or $resolved.Count -ne 1 -or [string]$resolved[0] -notmatch '^[a-f0-9]{40}$') {
        throw "Unable to resolve Git ref '$Ref' to a commit."
    }
    return ([string]$resolved[0]).ToLowerInvariant()
}

function ConvertTo-LlmWikiRepositoryPath {
    param([AllowEmptyString()][string]$Path)

    $normalized = ([string]$Path).TrimStart([char]0xFEFF).Replace('\', '/')
    while ($normalized.StartsWith('./')) { $normalized = $normalized.Substring(2) }
    return $normalized
}

function ConvertFrom-LlmWikiGitPathOutput {
    param([AllowEmptyString()][string]$Output)

    if ([string]::IsNullOrEmpty($Output)) { return @() }
    @($Output.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object {
        ConvertTo-LlmWikiRepositoryPath ([string]$_)
    } | Where-Object { -not [string]::IsNullOrEmpty($_) } | Sort-Object -Unique)
}

function Split-LlmWikiGitGrepAlternatives {
    param(
        [Parameter(Mandatory)][string[]]$Alternative,
        [ValidateRange(256, 16000)][int]$MaxPatternLength = 6000
    )

    $groups = [Collections.Generic.List[string]]::new()
    $current = [Text.StringBuilder]::new()
    foreach ($value in @($Alternative | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $separatorLength = if ($current.Length -gt 0) { 1 } else { 0 }
        if ($current.Length -gt 0 -and $current.Length + $separatorLength + $value.Length -gt $MaxPatternLength) {
            $groups.Add($current.ToString())
            $null = $current.Clear()
        }
        if ($current.Length -gt 0) { $null = $current.Append('|') }
        $null = $current.Append($value)
    }
    if ($current.Length -gt 0) { $groups.Add($current.ToString()) }
    @($groups)
}

function Invoke-LlmWikiGitPathList {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$FailureMessage = 'Git path enumeration failed.',
        [int[]]$AllowedExitCode = @(0)
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'git'
    $startInfo.WorkingDirectory = $RepositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    $startInfo.StandardErrorEncoding = [Text.UTF8Encoding]::new($false)
    $gitArguments = [Collections.Generic.List[string]]::new()
    foreach ($argument in @($Arguments)) { $gitArguments.Add($argument) }
    $separatorIndex = $gitArguments.IndexOf('--')
    if ($gitArguments.Count -gt 0 -and $gitArguments[0] -eq 'grep') {
        $gitArguments.Insert(1, '-z')
    } elseif ($separatorIndex -ge 0) {
        $gitArguments.Insert($separatorIndex, '-z')
    } else {
        $gitArguments.Add('-z')
    }
    $startInfo.Arguments = (@($gitArguments | ForEach-Object { '"' + ([string]$_).Replace('"', '\"') + '"' }) -join ' ')
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) { throw "$FailureMessage Git did not start." }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = [string]$stdoutTask.GetAwaiter().GetResult()
        $stderr = [string]$stderrTask.GetAwaiter().GetResult()
        if ($process.ExitCode -notin $AllowedExitCode) {
            $detail = $stderr.Trim()
            throw "$FailureMessage Exit code $($process.ExitCode).$(if ($detail) { " $detail" })"
        }
        @(ConvertFrom-LlmWikiGitPathOutput $stdout)
    } finally { $process.Dispose() }
}

function Invoke-LlmWikiGitCommand {
    <#
    Runs git via Diagnostics.Process with stdout/stderr captured through .NET instead of
    PowerShell's native-command stream. Under Set-StrictMode/$ErrorActionPreference =
    'Stop' (as wiki.ps1 sets globally), the `&` call operator can promote even a harmless
    git warning (e.g. "CRLF will be replaced by LF", common with core.autocrlf=true on
    Windows) into a terminating NativeCommandError -- and that promotion has been observed
    to survive a local `2>$null` redirect in some hosts (reproduces from a fresh
    `powershell.exe -File` invocation even with `2>$null` present on the `&` call).
    Routing both streams through .NET's ReadToEndAsync never touches that machinery.
    Prefer Invoke-LlmWikiGitPathList for simple path-list output; use this for any other
    git subcommand (diff --name-status, log, rev-list, ...). Returns a single object with
    ExitCode/StandardOutput/StandardError/Lines (StandardOutput split on newlines, blank
    trailing entries removed).
    #>
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$FailureMessage = 'Git command failed.',
        [int[]]$AllowedExitCode = @(0)
    )

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'git'
    $startInfo.WorkingDirectory = $RepositoryRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    $startInfo.StandardErrorEncoding = [Text.UTF8Encoding]::new($false)
    $startInfo.Arguments = (@($Arguments | ForEach-Object { '"' + ([string]$_).Replace('"', '\"') + '"' }) -join ' ')
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) { throw "$FailureMessage Git did not start." }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = [string]$stdoutTask.GetAwaiter().GetResult()
        $stderr = [string]$stderrTask.GetAwaiter().GetResult()
        if ($process.ExitCode -notin $AllowedExitCode) {
            $detail = $stderr.Trim()
            throw "$FailureMessage Exit code $($process.ExitCode).$(if ($detail) { " $detail" })"
        }
        [pscustomobject][ordered]@{
            ExitCode = $process.ExitCode
            StandardOutput = $stdout
            StandardError = $stderr
            Lines = @($stdout -split '\r?\n' | Where-Object { $_ })
        }
    } finally { $process.Dispose() }
}
