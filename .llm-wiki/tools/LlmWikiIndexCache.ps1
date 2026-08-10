function Get-LlmWikiFileSha256([string]$Path) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $stream = [IO.File]::OpenRead($Path)
        try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
        finally { $stream.Dispose() }
    } finally { $sha.Dispose() }
}

function Get-LlmWikiIndexInputFingerprint([string]$RepositoryRoot, [string[]]$InputPath) {
    $existingPaths = @($InputPath | Sort-Object -Unique | Where-Object {
        Test-Path -LiteralPath (Join-Path $RepositoryRoot $_) -PathType Leaf
    })
    # git hash-object performs the same content scan in one native process. This
    # avoids thousands of PowerShell FileStream/open/dispose round trips while
    # retaining content-addressed invalidation for tracked and untracked files.
    $contentHashes = @($existingPaths | & git -C $RepositoryRoot hash-object --stdin-paths)
    if ($LASTEXITCODE -eq 0 -and $contentHashes.Count -eq $existingPaths.Count) {
        $entries = for ($index = 0; $index -lt $existingPaths.Count; $index++) {
            "$($existingPaths[$index].Replace('\', '/')):$($contentHashes[$index])"
        }
    } else {
        $entries = foreach ($relativePath in $existingPaths) {
        $absolutePath = Join-Path $RepositoryRoot $relativePath
            "$($relativePath.Replace('\', '/')):$(Get-LlmWikiFileSha256 $absolutePath)"
        }
    }
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($entries -join "`n"))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Test-LlmWikiIndexCache([string]$ReceiptPath, [string]$OutputPath, [string]$InputFingerprint) {
    if (-not (Test-Path -LiteralPath $ReceiptPath -PathType Leaf) -or -not (Test-Path -LiteralPath $OutputPath -PathType Leaf)) { return $false }
    try {
        $receipt = Get-Content -LiteralPath $ReceiptPath -Raw | ConvertFrom-Json
        return [int]$receipt.schemaVersion -eq 1 -and [string]$receipt.inputFingerprint -ceq $InputFingerprint -and
            [string]$receipt.outputFingerprint -ceq (Get-LlmWikiFileSha256 $OutputPath)
    } catch { return $false }
}

function Write-LlmWikiIndexCache([string]$ReceiptPath, [string]$OutputPath, [string]$InputFingerprint) {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $ReceiptPath) -Force
    $json = ([ordered]@{ schemaVersion = 1; inputFingerprint = $InputFingerprint; outputFingerprint = Get-LlmWikiFileSha256 $OutputPath } | ConvertTo-Json) + [Environment]::NewLine
    [IO.File]::WriteAllText($ReceiptPath, $json, (New-Object Text.UTF8Encoding($false)))
}
