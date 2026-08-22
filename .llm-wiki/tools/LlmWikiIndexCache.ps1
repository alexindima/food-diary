function Get-LlmWikiFileSha256([string]$Path) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $stream = [IO.File]::OpenRead($Path)
        try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
        finally { $stream.Dispose() }
    } finally { $sha.Dispose() }
}

function Get-LlmWikiIndexInputFingerprint([string]$RepositoryRoot, [string[]]$InputPath) {
    $pathSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($input in $InputPath) {
        $relativePath = ([string]$input).TrimStart([char]0xFEFF).Replace('\', '/')
        if ([IO.File]::Exists([IO.Path]::Combine($RepositoryRoot, $relativePath))) {
            $null = $pathSet.Add($relativePath)
        }
    }
    [string[]]$existingPaths = @($pathSet)
    [Array]::Sort($existingPaths, [StringComparer]::Ordinal)

    # One Git process hashes the complete set substantially faster than opening
    # thousands of files through PowerShell. Explicit BOM-free UTF-8 keeps Unicode
    # paths stable. A live redirected pipe needs StandardInputEncoding to stay BOM-free,
    # but that ProcessStartInfo property exists only on .NET 5+ (PowerShell 7+); on
    # Windows PowerShell 5.1 the default StreamWriter encoding can also inject a stray
    # UTF-8 preamble once its internal buffer first flushes, corrupting whichever path
    # git reads at that boundary. Redirecting through temp files instead of a live pipe
    # sidesteps both problems and behaves identically on every PowerShell/.NET runtime.
    $stdinPath = [IO.Path]::GetTempFileName()
    $stdoutPath = [IO.Path]::GetTempFileName()
    $stderrPath = [IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllText($stdinPath, (($existingPaths -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
        $process = Start-Process -FilePath 'git' -ArgumentList 'hash-object', '--stdin-paths' `
            -WorkingDirectory $RepositoryRoot -RedirectStandardInput $stdinPath `
            -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath `
            -NoNewWindow -PassThru -Wait
        # Get-Content -Raw on a genuinely empty file emits no pipeline output at all on
        # Windows PowerShell 5.1 (captured as PowerShell's internal AutomationNull, which
        # still fools `-eq $null` but throws on any method call, unlike PowerShell 7+ which
        # returns ''); `-join ''` reliably collapses either case to a real empty string.
        # Get-Content without -Encoding defaults to the system codepage (not UTF-8) on
        # Windows PowerShell 5.1 for a BOM-less file; -Encoding UTF8 makes decoding
        # explicit and correct on every runtime, matching the BOM-free UTF-8 git wrote.
        $output = (Get-Content -LiteralPath $stdoutPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue) -join ''
        $errorText = ((Get-Content -LiteralPath $stderrPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue) -join '').Trim()
        if ($process.ExitCode -ne 0) {
            throw "git hash-object failed with exit code $($process.ExitCode).$(if ($errorText) { " $errorText" })"
        }
    } finally {
        Remove-Item -LiteralPath $stdinPath, $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    }

    $contentHashes = @($output -split '\r?\n' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($contentHashes.Count -ne $existingPaths.Count -or @($contentHashes | Where-Object { $_ -notmatch '^[a-f0-9]{40,64}$' }).Count -gt 0) {
        throw "git hash-object returned $($contentHashes.Count) valid hashes for $($existingPaths.Count) index inputs."
    }
    $entries = for ($index = 0; $index -lt $existingPaths.Count; $index++) {
        "$($existingPaths[$index]):$($contentHashes[$index])"
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
