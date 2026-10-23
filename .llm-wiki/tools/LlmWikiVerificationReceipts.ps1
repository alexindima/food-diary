. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')

function Get-LlmWikiSha256([string]$Value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Value))) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Get-LlmWikiFileSha256([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '<missing>' }
    $stream = [IO.File]::OpenRead($Path)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($stream)) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose(); $stream.Dispose() }
}

function Normalize-LlmWikiVerificationCommand([string]$Command) {
    return (($Command.Trim() -replace '\s+', ' ') -replace '\s+--no-restore\s*$', '').Trim()
}

function Get-LlmWikiVerificationFingerprint([string]$RepositoryRoot) {
    $head = (Invoke-LlmWikiGitCommand -RepositoryRoot $RepositoryRoot -Arguments @('rev-parse', 'HEAD') -FailureMessage 'Unable to resolve HEAD for verification receipts.').Lines[0]
    $paths = @(
        Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments @('diff', '--no-renames', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--') -FailureMessage 'Unable to enumerate changed paths for verification receipts.'
        Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to enumerate untracked paths for verification receipts.'
    ) | Where-Object {
        $_ -and $_ -notmatch '^(?:\.artifacts/|\.llm-wiki/reviews/|\.llm-wiki/generated/)'
    } | ForEach-Object { $_.Replace('\', '/') } | Sort-Object -Unique
    $entries = @($paths | ForEach-Object {
        "$($_):$(Get-LlmWikiFileSha256 (Join-Path $RepositoryRoot $_))"
    })
    [pscustomobject]@{
        head = $head
        paths = $paths
        fingerprint = Get-LlmWikiSha256 ((@('schema=1', "head=$head") + $entries) -join "`n")
    }
}

function Get-LlmWikiVerificationReceiptRoot([string]$RepositoryRoot) {
    $gitDirectory = (Invoke-LlmWikiGitCommand -RepositoryRoot $RepositoryRoot -Arguments @('rev-parse', '--absolute-git-dir') -FailureMessage 'Unable to resolve the Git directory for verification receipts.').Lines[0]
    return Join-Path $gitDirectory 'llm-wiki/verification-receipts'
}

function Test-LlmWikiExecutionSeal([object]$Entry, [string]$ExpectedFingerprint, [string]$RepositoryRoot) {
    try {
        $seal = $Entry.lineage.execution.verification
        $recordPath = [string]$seal.recordPath
        if ($seal.fingerprint -cne $ExpectedFingerprint -or $recordPath -notmatch '^\.artifacts/llm-wiki/' -or $recordPath -match '(?:^|/)\.\.(?:/|$)') { return $false }
        $absoluteRecord = Join-Path $RepositoryRoot $recordPath
        if ((Get-LlmWikiFileSha256 $absoluteRecord) -cne [string]$seal.recordSha256) { return $false }
        $record = Get-Content -LiteralPath $absoluteRecord -Raw | ConvertFrom-Json
        $actualLogPath = if ($Entry.lineage.PSObject.Properties['reuse']) { [string]$Entry.lineage.reuse.sourceLogPath } else { [string]$Entry.lineage.artifact.path }
        return $null -ne $record.exitCode -and [int]$record.exitCode -eq 0 -and
            [string]$record.command -ceq [string]$Entry.lineage.execution.command -and
            [IO.Path]::GetFullPath([string]$record.checkout) -ceq [IO.Path]::GetFullPath($RepositoryRoot) -and
            [IO.Path]::GetFullPath([string]$record.logPath) -ceq [IO.Path]::GetFullPath((Join-Path $RepositoryRoot $actualLogPath)) -and
            [string]$record.logSha256 -ceq [string]$Entry.lineage.artifact.sha256 -and
            [double]$record.durationSeconds -eq [double]$Entry.lineage.execution.durationSeconds -and
            [string]$record.fingerprintBefore -ceq $ExpectedFingerprint -and [string]$record.fingerprintAfter -ceq $ExpectedFingerprint
    } catch { return $false }
}

function Test-LlmWikiVerificationExecutionProof([object]$Receipt, [string]$RepositoryRoot) {
    try {
        $proof = $Receipt.executionProof
        $sourcePath = [string]$proof.sourceEvidencePath
        if ($sourcePath -notmatch '^\.artifacts/llm-wiki/(?:tasks/[^/.][^/]*/)?[^/]+\.json$' -or $sourcePath -match '(?:^|/)\.\.(?:/|$)') { return $false }
        $source = Get-Content -LiteralPath (Join-Path $RepositoryRoot $sourcePath) -Raw | ConvertFrom-Json
        $entry = @($source.checks | Where-Object id -eq $proof.checkId | Select-Object -First 1)
        if ($entry.Count -ne 1 -or $entry[0].status -ne 'passed' -or $entry[0].lineage.kind -ne 'executed-check' -or $null -eq $entry[0].lineage.execution.exitCode -or [int]$entry[0].lineage.execution.exitCode -ne 0) { return $false }
        if ((Normalize-LlmWikiVerificationCommand $entry[0].command) -cne [string]$Receipt.normalizedCommand) { return $false }
        if ((Get-LlmWikiSha256 ($entry[0].lineage | ConvertTo-Json -Depth 15 -Compress)) -cne [string]$proof.lineageSha256) { return $false }
        if (-not (Test-LlmWikiExecutionSeal $entry[0] ([string]$Receipt.fingerprint) $RepositoryRoot)) { return $false }
        $artifact = $entry[0].lineage.artifact
        $artifactPath = [string]$artifact.path
        if (-not $artifactPath.StartsWith('.artifacts/llm-wiki/', [StringComparison]::Ordinal) -or $artifactPath -match '(?:^|/)\.\.(?:/|$)') { return $false }
        return (Get-LlmWikiFileSha256 (Join-Path $RepositoryRoot $artifactPath)) -ceq [string]$artifact.sha256
    } catch { return $false }
}

function Get-LlmWikiVerificationReceipts([string]$RepositoryRoot) {
    $root = Get-LlmWikiVerificationReceiptRoot $RepositoryRoot
    if (-not (Test-Path -LiteralPath $root -PathType Container)) { return @() }
    $receiptFiles = @(Get-ChildItem -LiteralPath $root -Filter '*.json' -File -ErrorAction SilentlyContinue)
    if ($receiptFiles.Count -eq 0) { return @() }
    $state = Get-LlmWikiVerificationFingerprint $RepositoryRoot
    return @($receiptFiles | ForEach-Object {
        try {
            $receipt = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
            $receipt | Add-Member -NotePropertyName validForCurrentState -NotePropertyValue (
                [int]$receipt.schemaVersion -in @(1, 2) -and
                [string]$receipt.result -eq 'passed' -and
                [string]$receipt.fingerprint -ceq [string]$state.fingerprint -and
                ([int]$receipt.schemaVersion -eq 1 -or (Test-LlmWikiVerificationExecutionProof $receipt $RepositoryRoot))
            ) -Force
            $receipt
        } catch { }
    } | Sort-Object recordedAtUtc -Descending)
}
