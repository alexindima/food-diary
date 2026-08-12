[CmdletBinding()]
param(
    [ValidateSet('Capture', 'ChangedPaths', 'Status', 'Close')]
    [string]$Action,
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$SessionId,
    [ValidateSet('Object', 'Text', 'Json')]
    [string]$Format = 'Object'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')

function Invoke-Git([string[]]$Arguments) {
    $output = @(& git -C $RepositoryRoot @Arguments)
    if ($LASTEXITCODE -ne 0) { throw "git $($Arguments -join ' ') failed." }
    return $output
}

function ConvertTo-RepositoryPath([string]$Path) {
    return ConvertTo-LlmWikiRepositoryPath $Path
}

function Get-ChangedPaths([string]$BaseRef) {
    $paths = @(
        Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments @('diff', '--name-only', '--diff-filter=ACMRD', $BaseRef, '--') -FailureMessage "Unable to collect baseline paths from '$BaseRef'."
        Invoke-LlmWikiGitPathList -RepositoryRoot $RepositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to collect baseline untracked paths.'
    )
    return @($paths | Where-Object { $_ } | ForEach-Object { ConvertTo-RepositoryPath $_ } | Sort-Object -Unique)
}

function Get-PathFingerprint([string]$RepositoryPath) {
    $absolutePath = Join-Path $RepositoryRoot $RepositoryPath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) { return '<missing>' }
    $stream = [System.IO.File]::OpenRead($absolutePath)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha256.ComputeHash($stream)) -replace '-', '').ToLowerInvariant()
    } finally {
        $sha256.Dispose()
        $stream.Dispose()
    }
}

$session = & (Join-Path $PSScriptRoot 'Resolve-LlmWikiSession.ps1') -SessionId $SessionId -Create:($Action -eq 'Capture') -RepositoryRoot $RepositoryRoot -Format Object
$SessionId = [string]$session.id
$gitDirectory = (Invoke-Git @('rev-parse', '--absolute-git-dir') | Select-Object -First 1)
$stateDirectory = Join-Path $gitDirectory 'llm-wiki'
$sessionKey = if ([string]::IsNullOrWhiteSpace($SessionId)) { 'default' } else {
    $normalizedSessionId = $SessionId -replace '[^a-zA-Z0-9_.-]', '-'
    if ($normalizedSessionId.Length -gt 80) { $normalizedSessionId.Substring(0, 80) } else { $normalizedSessionId }
}
$baselinePath = Join-Path $stateDirectory "task-baseline-$sessionKey.json"

if ($Action -eq 'Close') {
    $wasAvailable = Test-Path -LiteralPath $baselinePath -PathType Leaf
    if ($wasAvailable) { Remove-Item -LiteralPath $baselinePath -Force }
    $result = [pscustomobject]@{ available = $false; closed = $wasAvailable; sessionKey = $sessionKey; baselinePath = $baselinePath; head = $null; initialChangedPaths = @(); changedPaths = @(); excludedChangedPaths = @(); ageHours = 0; commitsAhead = 0 }
} elseif ($Action -eq 'Capture') {
    $head = (Invoke-Git @('rev-parse', 'HEAD') | Select-Object -First 1)
    $initialPaths = @(Get-ChangedPaths 'HEAD')
    $fingerprints = [ordered]@{}
    foreach ($path in $initialPaths) { $fingerprints[$path] = Get-PathFingerprint $path }
    $baseline = [ordered]@{
        schemaVersion = 1
        capturedAtUtc = [DateTime]::UtcNow.ToString('o')
        head = $head
        initialChangedPaths = $initialPaths
        fingerprints = $fingerprints
    }
    $null = New-Item -ItemType Directory -Path $stateDirectory -Force
    $baseline | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $baselinePath -Encoding utf8
    $result = [pscustomobject]@{ available = $true; sessionKey = $sessionKey; baselinePath = $baselinePath; head = $head; initialChangedPaths = $initialPaths; changedPaths = @(); excludedChangedPaths = $initialPaths; capturedAtUtc = $baseline.capturedAtUtc; ageHours = 0; commitsAhead = 0 }
} elseif (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) {
    $result = [pscustomobject]@{ available = $false; sessionKey = $sessionKey; baselinePath = $baselinePath; head = $null; initialChangedPaths = @(); changedPaths = @(); excludedChangedPaths = @(); ageHours = 0; commitsAhead = 0 }
} else {
    $baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
    $currentPaths = @(Get-ChangedPaths ([string]$baseline.head))
    $delta = [System.Collections.Generic.List[string]]::new()
    foreach ($path in $currentPaths) {
        $baselineFingerprint = $baseline.fingerprints.PSObject.Properties[$path]
        if ($null -eq $baselineFingerprint -or [string]$baselineFingerprint.Value -cne (Get-PathFingerprint $path)) {
            $delta.Add($path)
        }
    }
    $capturedAt = [DateTimeOffset]::Parse([string]$baseline.capturedAtUtc)
    $commitsAheadOutput = @(Invoke-Git @('rev-list', '--count', "$([string]$baseline.head)..HEAD"))
    $result = [pscustomobject]@{
        available = $true
        sessionKey = $sessionKey
        baselinePath = $baselinePath
        head = [string]$baseline.head
        initialChangedPaths = @($baseline.initialChangedPaths)
        changedPaths = @($delta)
        excludedChangedPaths = @($currentPaths | Where-Object { $_ -notin $delta })
        capturedAtUtc = [string]$baseline.capturedAtUtc
        ageHours = [Math]::Round(([DateTimeOffset]::UtcNow - $capturedAt).TotalHours, 1)
        commitsAhead = [int]($commitsAheadOutput | Select-Object -First 1)
    }
}

switch ($Format) {
    'Json' { $result | ConvertTo-Json -Depth 5 }
    'Text' {
        if (-not $result.available) { Write-Host 'LLM Wiki task baseline: not captured.' }
        elseif ($Action -eq 'Capture') { Write-Host "LLM Wiki task baseline captured: session=$sessionKey, $(@($result.initialChangedPaths).Count) pre-existing changed path(s)." }
        elseif ($Action -eq 'Close') { Write-Host "LLM Wiki task baseline closed: session=$sessionKey, existed=$($result.closed)." }
        else { Write-Host "LLM Wiki task delta: session=$sessionKey, age=$($result.ageHours)h, commits-ahead=$($result.commitsAhead), $(@($result.changedPaths).Count) changed, $(@($result.excludedChangedPaths).Count) excluded." }
    }
    default { return $result }
}
