function Get-LlmWikiRuntimeTopologyFingerprint {
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    $sourcePaths = @(
        @(Join-Path $RepositoryRoot 'docker-compose.yml') +
        @(Get-ChildItem -LiteralPath $RepositoryRoot -Recurse -File -Filter '*.cs' |
            Where-Object {
                $_.FullName -notmatch '[\\/](tests|obj|bin|\.artifacts|\.llm-wiki|TestResults|Migrations)[\\/]' -and
                $_.Name -notmatch '\.(Designer|g)\.cs$'
            } |
            ForEach-Object FullName) |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Sort-Object { $_.ToLowerInvariant() } -Unique
    )
    $material = [Text.StringBuilder]::new()
    foreach ($path in $sourcePaths) {
        $stream = [IO.File]::OpenRead($path)
        $fileHasher = [Security.Cryptography.SHA256]::Create()
        try {
            $hash = ([BitConverter]::ToString($fileHasher.ComputeHash($stream)) -replace '-', '').ToLowerInvariant()
        } finally {
            $fileHasher.Dispose()
            $stream.Dispose()
        }
        $relativePath = [IO.Path]::GetFullPath($path).Substring([IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/').Length + 1).Replace('\', '/')
        $null = $material.Append($relativePath).Append('=').Append($hash).Append("`n")
    }

    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        $fingerprint = ([BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($material.ToString()))) -replace '-', '').ToLowerInvariant()
    } finally {
        $hasher.Dispose()
    }

    return [pscustomobject][ordered]@{
        sourceFingerprint = $fingerprint
        sourceFileCount = $sourcePaths.Count
    }
}
