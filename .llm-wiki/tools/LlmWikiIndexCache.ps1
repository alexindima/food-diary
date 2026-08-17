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
    # thousands of files through PowerShell. Explicit UTF-8 without BOM keeps
    # Unicode paths stable and avoids the native-pipeline encoding ambiguity.
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'git'
    $startInfo.WorkingDirectory = $RepositoryRoot
    $startInfo.Arguments = 'hash-object --stdin-paths'
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
    $startInfo.StandardOutputEncoding = [Text.UTF8Encoding]::new($false)
    $startInfo.StandardErrorEncoding = [Text.UTF8Encoding]::new($false)
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) { throw 'Unable to start git hash-object.' }
        $outputTask = $process.StandardOutput.ReadToEndAsync()
        $errorTask = $process.StandardError.ReadToEndAsync()
        foreach ($relativePath in $existingPaths) { $process.StandardInput.WriteLine($relativePath) }
        $process.StandardInput.Close()
        $process.WaitForExit()
        $output = $outputTask.GetAwaiter().GetResult()
        $errorText = $errorTask.GetAwaiter().GetResult().Trim()
        if ($process.ExitCode -ne 0) {
            throw "git hash-object failed with exit code $($process.ExitCode).$(if ($errorText) { " $errorText" })"
        }
    } finally {
        $process.Dispose()
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
