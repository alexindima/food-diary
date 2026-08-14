if (-not (Get-Command Invoke-LlmWikiGitPathList -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
}

function Get-LlmWikiChangeSetSnapshot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $head = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD for the Wiki change-set snapshot.' }
    $workspacePaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments @('diff', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--') -FailureMessage 'Unable to resolve modified paths for the Wiki change-set snapshot.')
    $workspacePaths += @(Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to resolve untracked paths for the Wiki change-set snapshot.')
    $workspacePaths = @($workspacePaths | Sort-Object -Unique)

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
    }
}
