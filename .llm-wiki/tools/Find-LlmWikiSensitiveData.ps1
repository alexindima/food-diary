[CmdletBinding()]
param(
    [string]$Query,
    [Alias('PlannedPath', 'ProposedPath', 'ChangedPath')]
    [string[]]$ScopePath,
    [ValidateSet('all', 'credential', 'identity', 'health', 'financial', 'privateContent', 'logging', 'boundaries', 'external')]
    [string]$Category = 'all',
    [ValidateRange(1, 100)]
    [int]$Limit = 30,
    [switch]$NoImplicitScope,
    [ValidateSet('Sqlite', 'Json')]
    [string]$CompiledIndexSource = 'Sqlite',
    [switch]$IncludeDiagnostics,
    [ValidateSet('Text', 'Json')]
    [string]$Format = 'Text'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiGitPaths.ps1')
$wikiRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = (Resolve-Path (Join-Path $wikiRoot '..')).Path
$stopwatch = [Diagnostics.Stopwatch]::StartNew()
$scopePaths = @(
    $ScopePath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_ -split '[;,]' } |
        ForEach-Object { $_.Trim().Replace('\', '/') } |
        Where-Object { $_.Length -gt 0 } |
        Sort-Object -Unique
)
$scopeMode = if ($scopePaths.Count -gt 0) { 'explicit' } else { 'none' }
if (-not $NoImplicitScope -and $scopePaths.Count -eq 0 -and [string]::IsNullOrWhiteSpace($Query) -and $Category -eq 'all') {
    $gitPaths = @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('diff', '--name-only', 'HEAD', '--') -FailureMessage 'Unable to enumerate changed paths for sensitive-data scope.')
    $gitPaths += @(Invoke-LlmWikiGitPathList -RepositoryRoot $repositoryRoot -Arguments @('ls-files', '--others', '--exclude-standard') -FailureMessage 'Unable to enumerate untracked paths for sensitive-data scope.')
    $scopePaths = @(
        $gitPaths |
            ForEach-Object { $_.Replace('\', '/') } |
            Where-Object { $_ -notmatch '^\.llm-wiki/' } |
            Sort-Object -Unique
    )
    if ($scopePaths.Count -gt 0) { $scopeMode = 'git-diff' }
}
$searchInput = (@($Query) + @($scopePaths)) -join ' '
$aliases = @{ photo = 'image'; picture = 'image'; ai = 'openai'; credential = 'token' }
$queryTokens = @(
    [regex]::Matches($searchInput.ToLowerInvariant(), '[\p{L}\p{Nd}]+') |
        ForEach-Object {
            $token = $_.Value
            if ($aliases.ContainsKey($token)) { @($token, $aliases[$token]) } else { $token }
        } |
        Where-Object { $_.Length -ge 3 -and $_ -notin @('fooddiary', 'client', 'shared', 'features', 'components', 'app', 'src') } |
        Sort-Object -Unique
)
$diagnostics = $null
if ($CompiledIndexSource -eq 'Sqlite') {
    $sqlResult = & (Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1') `
        -Action sensitive-data `
        -SensitiveDataView $Category `
        -SensitiveDataFilter:(-not [string]::IsNullOrWhiteSpace($searchInput)) `
        -Query ($queryTokens -join ';') `
        -ChangedPath $scopePaths `
        -Limit $Limit `
        -SkipRefresh `
        -Format Json | ConvertFrom-Json
    if (-not [bool]$sqlResult.ready) {
        throw "SQLite sensitive-data projection is unavailable ($($sqlResult.unavailableReason)). Run ./.llm-wiki/wiki.ps1 graph-build and retry."
    }
    $items = if (-not [string]::IsNullOrWhiteSpace($searchInput)) {
        @($sqlResult.matches |
            Sort-Object @{ Expression = 'score'; Descending = $true }, @{ Expression = { $_.item.path } } |
            Select-Object -ExpandProperty item)
    } else {
        @($sqlResult.matches | Select-Object -ExpandProperty item)
    }
    $summary = $sqlResult.summary
    $diagnostics = [ordered]@{
        source = [string]$sqlResult.source
        sqlDurationMs = [double]$sqlResult.durationMs
        scannedRecords = [int]$sqlResult.scannedRecords
        candidateRecords = [int]$sqlResult.candidateRecords
        returnedRecords = [int]$sqlResult.returnedRecords
        sourceHash = [string]$sqlResult.sourceHash
        sourceBytesVerified = [int64]$sqlResult.sourceBytesVerified
        sourceBytesMaterialized = [int64]$sqlResult.sourceBytesMaterialized
    }
} else {
    $indexRaw = Get-Content -LiteralPath (Join-Path $wikiRoot 'generated/sensitive-data-index.json') -Raw
    $index = $indexRaw | ConvertFrom-Json
    $items = if ($Category -eq 'logging') {
        @($index.potentialLogging)
    } elseif ($Category -eq 'boundaries') {
        @($index.boundaryFiles)
    } elseif ($Category -eq 'external') {
        @($index.externalTransfers)
    } elseif ($Category -eq 'all') {
        @($index.fields) + @($index.externalTransfers)
    } else {
        @($index.fields | Where-Object category -eq $Category)
    }
    $candidateRecords = $items.Count
    if (-not [string]::IsNullOrWhiteSpace($searchInput)) {
        $items = @(
            $items |
                ForEach-Object {
                    $item = $_
                    $searchText = $item | ConvertTo-Json -Compress
                    $matchCount = @($queryTokens | Where-Object {
                        $searchText -match [regex]::Escape($_)
                    }).Count
                    $itemPath = [string]$item.path
                    $scopeMatch = @($scopePaths | Where-Object {
                        $scopePath = $_
                        $scopeDirectory = if ([IO.Path]::HasExtension($scopePath)) { Split-Path -Parent $scopePath } else { $scopePath }
                        $itemPath -eq $scopePath -or $itemPath.StartsWith("$($scopeDirectory.Replace('\', '/').TrimEnd('/'))/")
                    }).Count -gt 0
                    $score = $matchCount + $(if ($scopeMatch) { 20 } else { 0 })
                    $minimumMatches = if ($scopePaths.Count -gt 0 -and -not $scopeMatch) { 2 } else { 1 }
                    if ($scopeMatch -or $matchCount -ge $minimumMatches) {
                        [pscustomobject]@{ item = $item; score = $score; scopeMatch = $scopeMatch }
                    }
                } |
                Sort-Object @{ Expression = 'score'; Descending = $true }, @{ Expression = { $_.item.path } } |
                Select-Object -ExpandProperty item
        )
    }
    $summary = $index.summary
    $sourceBytes = [Text.Encoding]::UTF8.GetByteCount($indexRaw)
    $diagnostics = [ordered]@{
        source = 'json-baseline'
        sqlDurationMs = $null
        scannedRecords = @($index.fields).Count + @($index.boundaryFiles).Count + @($index.potentialLogging).Count + @($index.externalTransfers).Count
        candidateRecords = $candidateRecords
        returnedRecords = $items.Count
        sourceHash = $null
        sourceBytesVerified = $sourceBytes
        sourceBytesMaterialized = $sourceBytes
    }
}
$guidance = @()
if ($scopeMode -eq 'none' -and [string]::IsNullOrWhiteSpace($Query) -and $Category -eq 'all') {
    $items = @()
    $guidance = @(
        "Provide -Query, choose -PrivacyCategory, or scope the review with -PlannedPath @('path/one','path/two')."
        "Unless -NoImplicitScope is set, a non-wiki git diff scopes the default privacy command to that diff."
    )
}
$items = @($items | Select-Object -First $Limit)
$stopwatch.Stop()
$diagnostics['roundTripDurationMs'] = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 2)
if ($Format -eq 'Json') {
    $output = [ordered]@{
        category = $Category
        count = $items.Count
        scope = [pscustomobject]@{ mode = $scopeMode; paths = $scopePaths }
        guidance = $guidance
        summary = $summary
        items = $items
    }
    if ($IncludeDiagnostics) { $output['_diagnostics'] = [pscustomobject]$diagnostics }
    [pscustomobject]$output | ConvertTo-Json -Depth 8
    exit 0
}
Write-Host "Sensitive data '$Category': $($items.Count) candidate(s), scope=$scopeMode."
if ($IncludeDiagnostics) { Write-Host "Source: $($diagnostics.source), returned=$($diagnostics.returnedRecords)/$($diagnostics.candidateRecords), round-trip=$($diagnostics.roundTripDurationMs)ms." }
foreach ($message in $guidance) { Write-Host " - $message" }
foreach ($item in $items) { Write-Host " - $(($item | ConvertTo-Json -Compress))" }
