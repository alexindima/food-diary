Set-StrictMode -Version Latest

function Test-LlmWikiWorkspaceHeadRef {
    param([AllowEmptyString()][string]$HeadRef)

    return [string]::IsNullOrWhiteSpace($HeadRef) -or
        [string]::Equals($HeadRef.Trim(), 'HEAD', [StringComparison]::OrdinalIgnoreCase)
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

function Invoke-LlmWikiGitPathList {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string[]]$Arguments,
        [string]$FailureMessage = 'Git path enumeration failed.'
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
    if ($separatorIndex -ge 0) { $gitArguments.Insert($separatorIndex, '-z') } else { $gitArguments.Add('-z') }
    $startInfo.Arguments = (@($gitArguments | ForEach-Object { '"' + ([string]$_).Replace('"', '\"') + '"' }) -join ' ')
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) { throw "$FailureMessage Git did not start." }
        $stdout = $process.StandardOutput.ReadToEnd()
        $stderr = $process.StandardError.ReadToEnd()
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) {
            $detail = $stderr.Trim()
            throw "$FailureMessage Exit code $($process.ExitCode).$(if ($detail) { " $detail" })"
        }
        @(ConvertFrom-LlmWikiGitPathOutput $stdout)
    } finally { $process.Dispose() }
}
