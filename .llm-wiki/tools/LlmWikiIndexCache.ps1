function Get-LlmWikiFileSha256([string]$Path) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $stream = [IO.File]::OpenRead($Path)
        try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
        finally { $stream.Dispose() }
    } finally { $sha.Dispose() }
}

function Get-LlmWikiIndexInputFingerprint([string]$RepositoryRoot, [string[]]$InputPath) {
    $existingPaths = @($InputPath | ForEach-Object { ([string]$_).TrimStart([char]0xFEFF).Replace('\', '/') } | Sort-Object -Unique | Where-Object {
        Test-Path -LiteralPath (Join-Path $RepositoryRoot $_) -PathType Leaf
    })
    # Managed hashing is encoding-independent and keeps Unicode/BOM path bytes
    # away from PowerShell 5 native stdin, which otherwise prepends a UTF-8 BOM.
    $entries = foreach ($relativePath in $existingPaths) {
        $absolutePath = Join-Path $RepositoryRoot $relativePath
        "$relativePath`:$(Get-LlmWikiFileSha256 $absolutePath)"
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
