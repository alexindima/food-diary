function Get-LlmWikiSha256 {
    param([Parameter(Mandatory)][string]$Value)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Value))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Get-LlmWikiFileSha256 {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '<missing>' }
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose(); $stream.Dispose() }
}

function Get-LlmWikiQueryCacheEntry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$RepositoryRoot,
        [Parameter(Mandatory)][string]$Namespace,
        [Parameter(Mandatory)][hashtable]$Arguments
    )

    $head = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve HEAD for the Wiki query cache.' }
    $workspacePaths = @(& git -C $RepositoryRoot diff --name-only --diff-filter=ACMRD HEAD --)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve modified paths for the Wiki query cache.' }
    $workspacePaths += @(& git -C $RepositoryRoot ls-files --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve untracked paths for the Wiki query cache.' }
    $workspacePaths = @($workspacePaths | Where-Object { $_ } | ForEach-Object { ([string]$_).Replace('\', '/') } | Sort-Object -Unique)
    $argumentJson = [ordered]@{}
    foreach ($key in @($Arguments.Keys | Sort-Object)) {
        $value = $Arguments[$key]
        $argumentJson[$key] = if ($value -is [Management.Automation.SwitchParameter]) { [bool]$value } else { $value }
    }
    $material = [Collections.Generic.List[string]]::new()
    $material.Add('schema=1')
    $material.Add("namespace=$Namespace")
    $material.Add("head=$head")
    $material.Add("pwsh=$($PSVersionTable.PSVersion)")
    $material.Add(($argumentJson | ConvertTo-Json -Depth 8 -Compress))
    foreach ($path in $workspacePaths) {
        $material.Add("$path=$(Get-LlmWikiFileSha256 (Join-Path $RepositoryRoot $path))")
    }
    $fingerprint = Get-LlmWikiSha256 ($material -join "`n")
    $gitDirectory = (& git -C $RepositoryRoot rev-parse --absolute-git-dir).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve Git directory for the Wiki query cache.' }
    $cacheDirectory = Join-Path $gitDirectory "llm-wiki/query-cache/$Namespace"
    return [pscustomobject]@{
        fingerprint = $fingerprint
        path = Join-Path $cacheDirectory "$fingerprint.json"
        workspacePathCount = $workspacePaths.Count
    }
}

function Read-LlmWikiQueryCache {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Entry)
    if (-not (Test-Path -LiteralPath $Entry.path -PathType Leaf)) { return $null }
    return [IO.File]::ReadAllText($Entry.path, [Text.Encoding]::UTF8)
}

function Write-LlmWikiQueryCache {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object]$Entry,
        [Parameter(Mandatory)][string]$Content,
        [ValidateRange(5, 500)][int]$Retain = 100
    )
    $directory = Split-Path -Parent $Entry.path
    $null = New-Item -ItemType Directory -Path $directory -Force
    $temporaryPath = "$($Entry.path).$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temporaryPath, $Content, [Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryPath -Destination $Entry.path -Force
    } finally {
        Remove-Item -LiteralPath $temporaryPath -Force -ErrorAction SilentlyContinue
    }
    $staleEntries = @(Get-ChildItem -LiteralPath $directory -Filter '*.json' -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -Skip $Retain)
    foreach ($staleEntry in $staleEntries) {
        Remove-Item -LiteralPath $staleEntry.FullName -Force -ErrorAction SilentlyContinue
    }
}
