[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$manager = Join-Path $PSScriptRoot 'Manage-LlmWikiCodeGraph.ps1'
$briefTool = Join-Path $PSScriptRoot 'Get-LlmWikiTaskBrief.ps1'
$null = & $manager -Action build -Format Json

$cases = @(
    [pscustomobject]@{ Intent = 'Update the password reset command handler'; ProposedPath = @(); Compact = $true }
    [pscustomobject]@{ Intent = 'Improve the fasting controller endpoint'; ProposedPath = @(); Compact = $true }
    [pscustomobject]@{ Intent = 'Improve the responsive dashboard component visual layout'; ProposedPath = @(); Compact = $true }
    [pscustomobject]@{ Intent = 'Adjust the admin sidebar button responsive styling'; ProposedPath = @(); Compact = $true }
    [pscustomobject]@{ Intent = 'Change the meals dashboard component and backend API endpoint'; ProposedPath = @(); Compact = $true }
    [pscustomobject]@{ Intent = 'Review the hydration service and progress component'; ProposedPath = @(); Compact = $true }
    [pscustomobject]@{ Intent = 'Review repository architecture documentation'; ProposedPath = @(); Compact = $false }
    [pscustomobject]@{ Intent = 'Audit repository correctness reliability concurrency architecture privacy CI operations'; ProposedPath = @(); Compact = $true }
    [pscustomobject]@{
        Intent = 'Update user application behavior'
        ProposedPath = @('FoodDiary.Application.Users/Commands/UpdateUser/UpdateUserCommandHandler.cs')
        Compact = $false
    }
    [pscustomobject]@{ Intent = 'Review a hosted service'; ProposedPath = @('MailInbox/FoodDiary.MailInbox.Infrastructure/Services/MailInboxRetentionHostedService.cs'); Compact = $false }
    [pscustomobject]@{ Intent = 'Review sensitive account data'; ProposedPath = @('FoodDiary.Application.Abstractions/Authentication/Common/AccountCreatedMessage.cs'); Compact = $false }
    [pscustomobject]@{ Intent = 'Review an admin component contract'; ProposedPath = @('FoodDiary.Web.Client/projects/fooddiary-admin/src/app/features/admin-achievements/pages/admin-achievements.ts'); Compact = $false }
    [pscustomobject]@{ Intent = 'Review a domain aggregate'; ProposedPath = @('Shared/FoodDiary.Domain.Primitives/AggregateRoot.cs'); Compact = $false }
    [pscustomobject]@{ Intent = 'Review a backend command contract'; ProposedPath = @('FoodDiary.Application.Users/Commands/AcceptAiConsent/AcceptAiConsentCommand.cs'); Compact = $false }
)
$sqlDurations = [Collections.Generic.List[double]]::new()
$jsonDurations = [Collections.Generic.List[double]]::new()
$sqliteCandidateRecords = [Collections.Generic.List[int]]::new()
$jsonSourceBytes = [Collections.Generic.List[int64]]::new()
$sqliteImpactCandidates = [Collections.Generic.List[int]]::new()
$sqliteImpactBytes = [Collections.Generic.List[int64]]::new()
$jsonImpactBytes = [Collections.Generic.List[int64]]::new()
$reusedIntentCases = 0
$caseIndex = 0

function ConvertTo-FunctionalJson($Brief) {
    $copy = $Brief | ConvertTo-Json -Depth 14 | ConvertFrom-Json
    if ($copy.analysis.PSObject.Properties['compiledIndex']) {
        $copy.analysis.PSObject.Properties.Remove('compiledIndex')
    }
    if ($copy.analysis.PSObject.Properties['impactIndex']) {
        $copy.analysis.PSObject.Properties.Remove('impactIndex')
    }
    return $copy | ConvertTo-Json -Depth 14 -Compress
}

foreach ($case in $cases) {
    $arguments = @{
        Intent = $case.Intent
        ProposedPath = [string[]]@($case.ProposedPath)
        Format = 'Json'
        Limit = 6
        Compact = [bool]$case.Compact
        SkipTestPlan = $true
        SkipQueryCache = $true
    }
    if (($caseIndex % 2) -eq 0) {
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $briefTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $briefTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
    } else {
        $sqlStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $sqlite = & $briefTool @arguments | ConvertFrom-Json
        $sqlStopwatch.Stop()
        $jsonStopwatch = [Diagnostics.Stopwatch]::StartNew()
        $json = & $briefTool @arguments -CompiledIndexSource Json | ConvertFrom-Json
        $jsonStopwatch.Stop()
    }

    if ((ConvertTo-FunctionalJson $sqlite) -cne (ConvertTo-FunctionalJson $json)) {
        throw "$($case.Intent): SQLite/JSON task-brief parity failed."
    }
    if ($case.Intent -match '^Audit repository' -and
        ([string]$sqlite.analysis.mode -ne 'broad-assessment' -or @($sqlite.change.directModules).Count -ne 0)) {
        throw "$($case.Intent): broad assessment was incorrectly reduced to a feature module."
    }
    if ([string]$sqlite.analysis.impactIndex.source -ne 'sqlite-task-brief-impact' -or
        [string]$sqlite.analysis.impactIndex.selectionMode -ne 'exact-changed-paths' -or
        [int]$sqlite.analysis.impactIndex.returnedRecords -ne [int]$sqlite.analysis.impactIndex.candidateRecords -or
        @($sqlite.analysis.impactIndex.sourceHashes.PSObject.Properties).Count -ne 7) {
        throw "$($case.Intent): SQLite task-brief impact diagnostics are incomplete."
    }
    if ([string]$json.analysis.impactIndex.source -ne 'json-baseline' -or
        [int64]$json.analysis.impactIndex.sourceBytesVerified -le 0 -or
        [int64]$json.analysis.impactIndex.sourceBytesMaterialized -le 0) {
        throw "$($case.Intent): explicit JSON task-brief impact baseline did not report materialized bytes."
    }
    if ([int64]$sqlite.analysis.impactIndex.sourceBytesVerified -ne [int64]$json.analysis.impactIndex.sourceBytesVerified -or
        [int64]$sqlite.analysis.impactIndex.sourceBytesMaterialized -ge [int64]$json.analysis.impactIndex.sourceBytesMaterialized) {
        throw "$($case.Intent): SQLite impact projection did not preserve freshness coverage while reducing materialized JSON bytes."
    }
    $sqliteImpactCandidates.Add([int]$sqlite.analysis.impactIndex.candidateRecords)
    $sqliteImpactBytes.Add([int64]$sqlite.analysis.impactIndex.sourceBytesMaterialized)
    $jsonImpactBytes.Add([int64]$json.analysis.impactIndex.sourceBytesMaterialized)
    if ($sqlite.analysis.mode -eq 'intent-inferred') {
        if ([string]$sqlite.analysis.compiledIndex.source -ne 'sqlite-compiled-index' -or
            -not [bool]$sqlite.analysis.compiledIndex.reusedForDiff -or
            $null -ne $sqlite.analysis.compiledIndex.sourceBytesRead) {
            throw "$($case.Intent): SQLite task-brief intent route did not reuse the compiled context without direct JSON bytes."
        }
        if ([string]$json.analysis.compiledIndex.source -ne 'json-baseline' -or
            [int64]$json.analysis.compiledIndex.sourceBytesRead -le 0) {
            throw "$($case.Intent): explicit JSON task-brief baseline did not report its source bytes."
        }
        $sqliteCandidateRecords.Add([int]$sqlite.analysis.compiledIndex.candidateRecords)
        $jsonSourceBytes.Add([int64]$json.analysis.compiledIndex.sourceBytesRead)
        $reusedIntentCases++
    }
    $sqlDurations.Add($sqlStopwatch.Elapsed.TotalMilliseconds)
    $jsonDurations.Add($jsonStopwatch.Elapsed.TotalMilliseconds)
    $caseIndex++
}

$sqlAverage = [Math]::Round(($sqlDurations | Measure-Object -Average).Average, 2)
$jsonAverage = [Math]::Round(($jsonDurations | Measure-Object -Average).Average, 2)
if ($sqlAverage -ge $jsonAverage) {
    throw "SQLite task-brief context reuse did not improve average end-to-end latency: SQL=${sqlAverage}ms, JSON=${jsonAverage}ms."
}
$averageSqlCandidates = [Math]::Round(($sqliteCandidateRecords | Measure-Object -Average).Average, 2)
$averageJsonBytes = [Math]::Round(($jsonSourceBytes | Measure-Object -Average).Average, 2)
$averageImpactCandidates = [Math]::Round(($sqliteImpactCandidates | Measure-Object -Average).Average, 2)
$averageSqlImpactBytes = [Math]::Round(($sqliteImpactBytes | Measure-Object -Average).Average, 2)
$averageJsonImpactBytes = [Math]::Round(($jsonImpactBytes | Measure-Object -Average).Average, 2)
$averageAvoidedImpactBytes = [Math]::Round($averageJsonImpactBytes - $averageSqlImpactBytes, 2)
Write-Host "LLM Wiki task-brief SQL parity passed: $($cases.Count)/$($cases.Count) cases; reused=$reusedIntentCases; SQL=${sqlAverage}ms, JSON=${jsonAverage}ms average end-to-end; intent SQL candidates=${averageSqlCandidates}, avoided intent JSON bytes=${averageJsonBytes}; impact SQL candidates=${averageImpactCandidates}, materialized SQL=${averageSqlImpactBytes} vs JSON=${averageJsonImpactBytes} bytes, avoided=${averageAvoidedImpactBytes}."
