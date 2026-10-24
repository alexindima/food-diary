function Get-LlmWikiRequiredFileSha256([string]$Path) {
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

    if ($existingPaths.Count -eq 0) {
        $emptySha = [Security.Cryptography.SHA256]::Create()
        try {
            return ([BitConverter]::ToString($emptySha.ComputeHash([byte[]]::new(0))) -replace '-', '').ToLowerInvariant()
        } finally {
            $emptySha.Dispose()
        }
    }

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
    $process = $null
    $processStarted = $false
    try {
        # Do not append a trailing newline: Git for Linux treats the resulting
        # empty record as an empty path when --stdin-paths consumes the file.
        [IO.File]::WriteAllText($stdinPath, ($existingPaths -join "`n"), [Text.UTF8Encoding]::new($false))
        if ($PSVersionTable.PSVersion.Major -lt 6 -and [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
            # PS5's Start-Process PID proxy can lose the exit code for fast Git
            # processes; -Wait also races its job assignment. Own the native
            # process handle instead. Fixed cmd redirection retains BOM-free file
            # stdin without PS5's UTF-8 StreamWriter preamble. Paths are data in
            # child-only variables; delayed expansion is disabled.
            $start = [Diagnostics.ProcessStartInfo]::new()
            $start.FileName = Join-Path ([Environment]::GetFolderPath('System')) 'cmd.exe'
            $start.WorkingDirectory = $RepositoryRoot
            $start.UseShellExecute = $false
            $start.CreateNoWindow = $true
            $start.RedirectStandardOutput = $true
            $start.RedirectStandardError = $true
            $start.EnvironmentVariables['FD_WIKI_HASH_INPUT'] = $stdinPath
            $start.EnvironmentVariables['FD_WIKI_HASH_GIT'] = (Get-Command git.exe -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
            $start.Arguments = '/d /s /v:off /c ""%FD_WIKI_HASH_GIT%" hash-object --no-filters --stdin-paths <"%FD_WIKI_HASH_INPUT%""'
            $process = [Diagnostics.Process]::new()
            $process.StartInfo = $start
            if (-not $process.Start()) { throw 'git hash-object did not start.' }
            $processStarted = $true
            $stdoutTask = $process.StandardOutput.ReadToEndAsync()
            $stderrTask = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(30000)) { throw 'git hash-object timed out after 30 seconds.' }
            $output = [string]$stdoutTask.GetAwaiter().GetResult()
            $errorText = ([string]$stderrTask.GetAwaiter().GetResult()).Trim()
        } else {
            $process = Start-Process -FilePath 'git' -ArgumentList 'hash-object', '--no-filters', '--stdin-paths' `
                -WorkingDirectory $RepositoryRoot -RedirectStandardInput $stdinPath `
                -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath `
                -NoNewWindow -PassThru
            $processStarted = $true
            $null = $process.Handle
            if (-not $process.WaitForExit(30000)) { throw 'git hash-object timed out after 30 seconds.' }
            $process.WaitForExit()
            # Get-Content -Raw on a genuinely empty file emits no pipeline output at all on
            # Windows PowerShell 5.1 (captured as PowerShell's internal AutomationNull, which
            # still fools `-eq $null` but throws on any method call, unlike PowerShell 7+ which
            # returns ''); `-join ''` reliably collapses either case to a real empty string.
            # Get-Content without -Encoding defaults to the system codepage (not UTF-8) on
            # Windows PowerShell 5.1 for a BOM-less file; -Encoding UTF8 makes decoding
            # explicit and correct on every runtime, matching the BOM-free UTF-8 git wrote.
            $output = (Get-Content -LiteralPath $stdoutPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue) -join ''
            $errorText = ((Get-Content -LiteralPath $stderrPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue) -join '').Trim()
        }
        if ($process.ExitCode -ne 0) {
            throw "git hash-object failed with exit code $($process.ExitCode).$(if ($errorText) { " $errorText" })"
        }
    } finally {
        if ($null -ne $process) {
            try {
                if ($processStarted -and -not $process.HasExited) {
                    if ($PSVersionTable.PSVersion.Major -lt 6) {
                        & (Join-Path ([Environment]::GetFolderPath('System')) 'taskkill.exe') /PID $process.Id /T /F 2>$null | Out-Null
                    } else { $process.Kill($true) }
                    $null = $process.WaitForExit(5000)
                }
            } finally { $process.Dispose() }
        }
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
            [string]$receipt.outputFingerprint -ceq (Get-LlmWikiRequiredFileSha256 $OutputPath)
    } catch { return $false }
}

function Write-LlmWikiIndexCache([string]$ReceiptPath, [string]$OutputPath, [string]$InputFingerprint) {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $ReceiptPath) -Force
    $json = ([ordered]@{ schemaVersion = 1; inputFingerprint = $InputFingerprint; outputFingerprint = Get-LlmWikiRequiredFileSha256 $OutputPath } | ConvertTo-Json) + [Environment]::NewLine
    [IO.File]::WriteAllText($ReceiptPath, $json, (New-Object Text.UTF8Encoding($false)))
}

function Get-LlmWikiIndexEntryFingerprint([string[]]$Entry) {
    $unique = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($value in $Entry) { [void]$unique.Add($value) }
    [string[]]$ordered = @($unique)
    [Array]::Sort($ordered, [StringComparer]::Ordinal)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($ordered -join "`n"))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Get-LlmWikiIndexOutputSetFingerprint([string]$RepositoryRoot, [string[]]$OutputPath) {
    $entries = @($OutputPath | ForEach-Object {
        $relative = $_.Replace('\', '/')
        $absolute = Join-Path $RepositoryRoot $relative
        $hash = if (Test-Path -LiteralPath $absolute -PathType Leaf) { Get-LlmWikiRequiredFileSha256 $absolute } else { '<missing>' }
        "$relative`:$hash"
    })
    return Get-LlmWikiIndexEntryFingerprint $entries
}

function Test-LlmWikiIndexSetCache([string]$ReceiptPath, [string]$InputFingerprint, [string]$OutputFingerprint) {
    if (-not (Test-Path -LiteralPath $ReceiptPath -PathType Leaf)) { return $false }
    try {
        $receipt = Get-Content -LiteralPath $ReceiptPath -Raw | ConvertFrom-Json
        return [int]$receipt.schemaVersion -eq 2 -and [string]$receipt.inputFingerprint -ceq $InputFingerprint -and
            [string]$receipt.outputSetFingerprint -ceq $OutputFingerprint
    } catch { return $false }
}

function Write-LlmWikiIndexSetCache([string]$ReceiptPath, [string]$InputFingerprint, [string]$OutputFingerprint) {
    $null = New-Item -ItemType Directory -Path (Split-Path -Parent $ReceiptPath) -Force
    $json = ([ordered]@{ schemaVersion = 2; inputFingerprint = $InputFingerprint; outputSetFingerprint = $OutputFingerprint } | ConvertTo-Json) + [Environment]::NewLine
    [IO.File]::WriteAllText($ReceiptPath, $json, (New-Object Text.UTF8Encoding($false)))
}
