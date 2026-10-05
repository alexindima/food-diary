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
    param(
        [AllowEmptyString()][string]$Content,
        [Security.Cryptography.SHA256]$Hasher
    )

    $normalized = $Content.Replace("`r`n", "`n").Replace("`r", "`n")
    $ownsHasher = $null -eq $Hasher
    if ($ownsHasher) { $Hasher = [Security.Cryptography.SHA256]::Create() }
    try {
        return [BitConverter]::ToString($Hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($normalized))).Replace('-', '').ToLowerInvariant()
    } finally { if ($ownsHasher) { $Hasher.Dispose() } }
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
        $hasher = [Security.Cryptography.SHA256]::Create()
        try {
            $paths = @(
                @(Join-Path $RepositoryRoot 'docker-compose.yml') +
                @(Get-LlmWikiRuntimeTopologySourceFiles -RepositoryRoot $RepositoryRoot | ForEach-Object FullName)
            )
            foreach ($path in $paths) {
                # Filesystem leaf checks avoid invoking a PowerShell provider for
                # every source, while retaining missing-file behavior.
                if (-not [IO.File]::Exists($path)) { continue }
                $SourceHashes[$path] = Get-LlmWikiNormalizedContentHash -Content ([IO.File]::ReadAllText($path)) -Hasher $hasher
            }
        } finally { $hasher.Dispose() }
    }

    $sourcePaths = @(
        $SourceHashes.Keys |
        Sort-Object { $_.ToLowerInvariant() } -Unique
    )
    $material = [Text.StringBuilder]::new()
    $rootPrefixLength = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/').Length + 1
    foreach ($path in $sourcePaths) {
        $hash = $SourceHashes[$path]
        $relativePath = [IO.Path]::GetFullPath($path).Substring($rootPrefixLength).Replace('\', '/')
        $null = $material.Append($relativePath).Append('=').Append($hash).Append("`n")
    }

    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        $fingerprint = [BitConverter]::ToString($hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($material.ToString()))).Replace('-', '').ToLowerInvariant()
    } finally {
        $hasher.Dispose()
    }

    return [pscustomobject][ordered]@{
        sourceFingerprint = $fingerprint
        sourceFileCount = $sourcePaths.Count
    }
}
