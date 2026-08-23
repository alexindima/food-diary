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
        (Invoke-LlmWikiGitCommand -RepositoryRoot $RepositoryRoot -Arguments @('diff', '--name-only', '--diff-filter=ACMRD', 'HEAD', '--') -FailureMessage 'Unable to enumerate changed paths for verification receipts.').Lines
        (Invoke-LlmWikiGitCommand -RepositoryRoot $RepositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to enumerate untracked paths for verification receipts.').Lines
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
    $gitDirectory = @(& git -C $RepositoryRoot rev-parse --absolute-git-dir)[0]
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the Git directory for verification receipts.' }
    return Join-Path $gitDirectory 'llm-wiki/verification-receipts'
}

function Get-LlmWikiVerificationReceipts([string]$RepositoryRoot) {
    $state = Get-LlmWikiVerificationFingerprint $RepositoryRoot
    $root = Get-LlmWikiVerificationReceiptRoot $RepositoryRoot
    if (-not (Test-Path -LiteralPath $root -PathType Container)) { return @() }
    return @(Get-ChildItem -LiteralPath $root -Filter '*.json' -File -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            $receipt = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
            $receipt | Add-Member -NotePropertyName validForCurrentState -NotePropertyValue (
                [int]$receipt.schemaVersion -eq 1 -and
                [string]$receipt.result -eq 'passed' -and
                [string]$receipt.fingerprint -ceq [string]$state.fingerprint
            ) -Force
            $receipt
        } catch { }
    } | Sort-Object recordedAtUtc -Descending)
}
