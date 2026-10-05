. (Join-Path $PSScriptRoot 'LlmWikiSourceInventory.ps1')

function Get-LlmWikiRuntimeTopologySourceFiles {
    param([Parameter(Mandatory)][string]$RepositoryRoot)

    Get-LlmWikiSourceFiles -RepositoryRoot $RepositoryRoot -Filter '*.cs' `
        -ExcludedDirectory @('tests', 'obj', 'bin', '.artifacts', '.llm-wiki', 'TestResults', 'Migrations') |
        Where-Object {
            $_.FullName -notmatch '[\\/](tests|obj|bin|\.artifacts|\.llm-wiki|TestResults|Migrations)[\\/]' -and
            $_.Name -notmatch '\.(Designer|g)\.cs$'
        }
}

function Get-LlmWikiNormalizedContentHash {
    param([AllowEmptyString()][string]$Content)

    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n")
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($normalized))) -replace '-', '').ToLowerInvariant()
    } finally { $hasher.Dispose() }
}

function Get-LlmWikiRuntimeTopologyFingerprint {
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Collections.IDictionary]$SourceHashes
    )

    # A generator may supply hashes from the same content it just parsed.
    # Query callers omit them and independently enumerate/read every source;
    # no timestamp cache can conceal an equal-size, equal-time content edit.
    if (-not $PSBoundParameters.ContainsKey('SourceHashes')) {
        $SourceHashes = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
        $paths = @(
            @(Join-Path $RepositoryRoot 'docker-compose.yml') +
            @(Get-LlmWikiRuntimeTopologySourceFiles -RepositoryRoot $RepositoryRoot | ForEach-Object FullName) |
            Where-Object { Test-Path -LiteralPath $_ -PathType Leaf }
        )
        foreach ($path in $paths) {
            $SourceHashes[$path] = Get-LlmWikiNormalizedContentHash -Content ([IO.File]::ReadAllText($path))
        }
    }

    $sourcePaths = @(
        $SourceHashes.Keys |
        Sort-Object { $_.ToLowerInvariant() } -Unique
    )
    $material = [Text.StringBuilder]::new()
    foreach ($path in $sourcePaths) {
        $hash = $SourceHashes[$path]
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
