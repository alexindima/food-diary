if (-not (Get-Command Invoke-LlmWikiGitPathList -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
}

function Get-LlmWikiChangeSetSnapshot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [string[]]$RelevantPath
    )

    $head = (Invoke-LlmWikiGitCommand -RepositoryRoot $RepositoryRoot -Arguments @('rev-parse', 'HEAD') -FailureMessage 'Unable to resolve HEAD for the Wiki change-set snapshot.').Lines[0].Trim()
    $normalizedRelevantPaths = @($RelevantPath | Where-Object { $_ } | ForEach-Object { ([string]$_).Replace('\', '/').TrimEnd('/') } | Sort-Object -Unique)
    $gitPathspecs = if ($normalizedRelevantPaths.Count -gt 0) {
        @($normalizedRelevantPaths + @('.llm-wiki/tools', '.llm-wiki/policies', '.llm-wiki/wiki.ps1') | Sort-Object -Unique)
    } else {
        @()
    }
    $diffArguments = @('diff', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--') + @($gitPathspecs)
    $untrackedArguments = @('ls-files', '--others', '--exclude-standard', '--') + @($gitPathspecs)
    $workspacePaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments $diffArguments -FailureMessage 'Unable to resolve modified paths for the Wiki change-set snapshot.')
    $workspacePaths += @(Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments $untrackedArguments -FailureMessage 'Unable to resolve untracked paths for the Wiki change-set snapshot.')
    $workspacePaths = @($workspacePaths | Sort-Object -Unique)
    if ($normalizedRelevantPaths.Count -gt 0) {
        $workspacePaths = @($workspacePaths | Where-Object {
            $candidate = ([string]$_).Replace('\', '/').TrimEnd('/')
            if ($candidate.StartsWith('.llm-wiki/tools/', [StringComparison]::OrdinalIgnoreCase) -or
                $candidate.StartsWith('.llm-wiki/policies/', [StringComparison]::OrdinalIgnoreCase) -or
                $candidate -eq '.llm-wiki/wiki.ps1') { return $true }
            foreach ($relevantPath in $normalizedRelevantPaths) {
                if ($candidate -eq $relevantPath -or
                    $candidate.StartsWith("$relevantPath/", [StringComparison]::OrdinalIgnoreCase) -or
                    $relevantPath.StartsWith("$candidate/", [StringComparison]::OrdinalIgnoreCase)) { return $true }
            }
            return $false
        })
    }

    $material = [Collections.Generic.List[string]]::new()
    $material.Add("head=$head")
    foreach ($path in $workspacePaths) {
        $absolutePath = Join-Path $RepositoryRoot $path
        $contentHash = if (Test-Path -LiteralPath $absolutePath -PathType Leaf) {
            $stream = [IO.File]::OpenRead($absolutePath)
            $sha = [Security.Cryptography.SHA256]::Create()
            try { ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
            finally { $sha.Dispose(); $stream.Dispose() }
        } else { '<missing>' }
        $material.Add("$path=$contentHash")
    }
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $fingerprint = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($material -join "`n"))) -replace '-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }

    [pscustomobject]@{
        head = $head
        fingerprint = $fingerprint
        changedPaths = [string[]]$workspacePaths
        createdAtUtc = [DateTime]::UtcNow.ToString('o')
        relevantPaths = [string[]]$normalizedRelevantPaths
    }
}
