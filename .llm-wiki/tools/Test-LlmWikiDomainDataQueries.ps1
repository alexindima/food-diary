[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$queryTool = Join-Path $PSScriptRoot 'Find-LlmWikiDomainData.ps1'
$buildTool = Join-Path $PSScriptRoot 'Build-LlmWikiInProcessSqliteReader.ps1'
$repositoryRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repositoryRoot)) { throw 'Unable to resolve the repository root for domain-data parity.' }
$build = & $buildTool -Format Json | ConvertFrom-Json
if (-not [bool]$build.ready) { throw 'In-process SQLite reader is not ready for domain-data parity.' }
$reusedBuild = & $buildTool -Format Json | ConvertFrom-Json
if (-not [bool]$reusedBuild.reused -or [string]$reusedBuild.fingerprint -cne [string]$build.fingerprint) {
    throw 'In-process SQLite reader did not reuse an identical tooling build.'
}

# The relationships view returns mapping records, each of which may own several relationships.
$domainIndex = Get-Content (Join-Path $repositoryRoot '.llm-wiki/generated/domain-data-index.json') -Raw | ConvertFrom-Json
$relationshipMappingCount = @($domainIndex.persistenceMappings | Where-Object { @($_.relationships).Count -gt 0 }).Count
if ($relationshipMappingCount -eq 0) { throw 'Domain-data relationship parity requires non-empty source mappings.' }
$boundedRelationshipMappingCount = [Math]::Min(30, $relationshipMappingCount)

$cases = @(
    [pscustomobject]@{ View = 'all'; Query = 'weight'; Minimum = 3 }
    [pscustomobject]@{ View = 'types'; Query = 'User'; Minimum = 3 }
    [pscustomobject]@{ View = 'invariants'; Query = 'weight'; Minimum = 1 }
    [pscustomobject]@{ View = 'mappings'; Query = 'User'; Minimum = 3 }
    [pscustomobject]@{ View = 'indexes'; Query = ''; Minimum = 30 }
    [pscustomobject]@{ View = 'indexes'; Query = 'User'; Minimum = 1 }
    [pscustomobject]@{ View = 'relationships'; Query = ''; Minimum = 1; Expected = $boundedRelationshipMappingCount }
    [pscustomobject]@{ View = 'relationships'; Query = 'User'; Minimum = 1 }
    [pscustomobject]@{ View = 'invariants'; Query = 'WeightKg'; Minimum = 2 }
    [pscustomobject]@{ View = 'all'; Query = 'zzzxqv'; Minimum = 0; Expected = 0 }
    [pscustomobject]@{ View = 'all'; Query = '%_zzzxqv'; Minimum = 0; Expected = 0 }
)
$sqlDurations = [Collections.Generic.List[double]]::new()
$null = & $queryTool -View invariants -Query weight -Limit 30 -Format Json

foreach ($case in $cases) {
    $arguments = @{ View = $case.View; Query = $case.Query; Limit = 30; Format = 'Json' }
    $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
    $sqlite = & $queryTool @arguments | ConvertFrom-Json
    $sqlStopwatch.Stop()

    $returnedCount = 0
    foreach ($property in $sqlite.PSObject.Properties) { $returnedCount += @($property.Value).Count }
    if ($returnedCount -lt [int]$case.Minimum) {
        throw "$($case.View)/$($case.Query): domain-data parity was vacuous; expected at least $($case.Minimum) record(s), got $returnedCount."
    }
    if ($case.PSObject.Properties['Expected'] -and $returnedCount -ne [int]$case.Expected) {
        throw "$($case.View)/$($case.Query): expected exactly $($case.Expected) record(s), got $returnedCount."
    }
    $sqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
}

$probeArguments = @{ View = 'all'; Query = 'weight'; Limit = 30; IncludeDiagnostics = $true; Format = 'Json' }
$probe = & $queryTool @probeArguments | ConvertFrom-Json

$sourceText = [IO.File]::ReadAllText((Join-Path $repositoryRoot '.llm-wiki/generated/domain-data-index.json')).Replace("`r`n", "`n")
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($sourceText))).ToLowerInvariant()
if ([string]$probe._diagnostics.source -ne 'sqlite-domain-data-in-process' -or
    [string]$probe._diagnostics.reader -ne 'microsoft-data-sqlite' -or
    [string]$probe._diagnostics.sourceHash -cne $sourceHash -or
    [int64]$probe._diagnostics.sourceBytesMaterialized -ge [int64]$probe._diagnostics.sourceBytesVerified -or
    [double]$probe._diagnostics.completeCommandDurationMs -lt [double]$probe._diagnostics.sqlDurationMs) {
    throw 'Domain-data in-process SQLite diagnostics are stale or incomplete.'
}

$missingRoot = Join-Path ([IO.Path]::GetTempPath()) "llm-wiki-domain-missing-$([guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $missingRoot
try {
    $failedClosed = $false
    try {
        $null = [LlmWiki.SqliteReader.DomainDataReader]::Query($missingRoot, 'all', '', 30, $false)
    } catch [Management.Automation.MethodInvocationException] {
        $failedClosed = $_.Exception.InnerException -is [InvalidOperationException]
    }
    if (-not $failedClosed) { throw 'Domain-data in-process reader did not fail closed when its source and database were missing.' }
} finally {
    Remove-Item -LiteralPath $missingRoot -Recurse -Force
}

$staleRoot = Join-Path ([IO.Path]::GetTempPath()) "llm-wiki-domain-stale-$([guid]::NewGuid().ToString('N'))"
$staleSourceDirectory = Join-Path $staleRoot '.llm-wiki/generated'
$staleDatabaseDirectory = Join-Path $staleRoot '.artifacts/llm-wiki/code-graph'
$null = New-Item -ItemType Directory -Path $staleSourceDirectory -Force
$null = New-Item -ItemType Directory -Path $staleDatabaseDirectory -Force
try {
    Set-Content -LiteralPath (Join-Path $staleSourceDirectory 'domain-data-index.json') -Value '{"domainTypes":[],"invariants":[],"persistenceMappings":[]}' -Encoding utf8
    $staleDatabasePath = Join-Path $staleDatabaseDirectory 'code-graph.sqlite'
    $connection = [Microsoft.Data.Sqlite.SqliteConnection]::new("Data Source=$staleDatabasePath")
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = "CREATE TABLE metadata(key TEXT PRIMARY KEY, value TEXT NOT NULL); INSERT INTO metadata(key, value) VALUES ('query_source:domain', 'stale-hash');"
        $null = $command.ExecuteNonQuery()
        $command.Dispose()
    } finally {
        $connection.Dispose()
    }
    $failedStale = $false
    try {
        $null = [LlmWiki.SqliteReader.DomainDataReader]::Query($staleRoot, 'all', '', 30, $false)
    } catch [Management.Automation.MethodInvocationException] {
        $failedStale = $_.Exception.InnerException -is [InvalidOperationException] -and
            $_.Exception.InnerException.Message -match 'projection is stale'
    }
    if (-not $failedStale) { throw 'Domain-data in-process reader did not fail closed for a stale projection.' }
} finally {
    [Microsoft.Data.Sqlite.SqliteConnection]::ClearAllPools()
    Remove-Item -LiteralPath $staleRoot -Recurse -Force
}

$warmSqlAverage = [Math]::Round(($sqlDurations | Measure-Object -Average).Average, 2)
Write-Host "Domain-data SQLite behavior passed: all views, literal queries, source lineage and missing/stale rejection; average=${warmSqlAverage}ms."
