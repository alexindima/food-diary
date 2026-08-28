[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

function Assert-CriticalTool([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

# Directly inventory the remaining lifecycle/helper tools that quality indexing
# cannot prove through dynamic facade dispatch. Each entry asserts its stable
# contract marker, while behavior-heavy tools receive executable checks below.
foreach ($contract in @(
    @{ file = 'Add-LlmWikiSourceReview.ps1'; marker = 'source-impact-reviews.json' }
    @{ file = 'Clear-LlmWikiReadOnlySnapshotCache.ps1'; marker = 'fooddiary-llm-wiki-read-only' }
    @{ file = 'Complete-LlmWikiUnseenContextCorpus.ps1'; marker = 'frozen-independent-query-corpus' }
    @{ file = 'Complete-LlmWikiAnswerEvaluationCorpus.ps1'; marker = 'frozen-independent-answer-corpus' }
    @{ file = 'Ensure-LlmWikiSqliteProjection.ps1'; marker = 'BackendOnlyRefresh' }
    @{ file = 'Get-LlmWikiConcurrentDrift.ps1'; marker = 'concurrentOrPreExistingPaths' }
    @{ file = 'Get-LlmWikiPhaseStatus.ps1'; marker = "ValidateSet('status', 'next', 'complete')" }
    @{ file = 'Invoke-LlmWikiMcpCommand.ps1'; marker = "'test-plan'" }
    @{ file = 'LlmWikiChangeSetSnapshot.ps1'; marker = 'Get-LlmWikiChangeSetSnapshot' }
    @{ file = 'LlmWikiGitRenames.ps1'; marker = 'ConvertFrom-LlmWikiGitNameStatus' }
    @{ file = 'LlmWikiImplementationBrief.ps1'; marker = 'Normalize-LlmWikiImplementationBrief' }
    @{ file = 'Measure-LlmWikiCodeGraph.ps1'; marker = 'incremental-build' }
    @{ file = 'Measure-LlmWikiContextConcurrency.ps1'; marker = 'throughputPerSecond' }
    @{ file = 'Measure-LlmWikiContextLatency.ps1'; marker = 'warmQueryP95Ms' }
    @{ file = 'Measure-LlmWikiAnswerQuality.ps1'; marker = 'claimCitationCoverage' }
    @{ file = 'Update-LlmWikiTaskEvidence.ps1'; marker = 'PacketPath must belong to WorkspacePath' }
    @{ file = 'Write-LlmWikiContextQueryObservation.ps1'; marker = 'context-query-observations.jsonl' }
)) {
    $contractSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot $contract.file) -Raw
    Assert-CriticalTool ($contractSource.Contains($contract.marker)) "Critical tool contract marker is missing: $($contract.file)"
}

$facadeSource = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') -Raw
$eagerGraphCommands = [regex]::Match(
    $facadeSource,
    '(?s)\$compiledIndexReadOnlyCommands\s*=\s*@\((?<commands>.*?)\)')
Assert-CriticalTool $eagerGraphCommands.Success 'Unable to inspect the eager compiled-index facade command contract.'
Assert-CriticalTool (
    $eagerGraphCommands.Groups['commands'].Value -notmatch "(?m)^\s*'context'\s*,?") `
    'The context facade must let Find-LlmWikiContext choose backend-only or full projection refresh from ChangeType.'
Assert-CriticalTool (
    $facadeSource.Contains("-PrepareCodeGraph:(`$Command -in `$compiledIndexReadOnlyCommands -and `$CompiledIndexSource -eq 'Sqlite')")) `
    'Explicit JSON facades must not prepare the SQLite code graph.'
Assert-CriticalTool (
    $facadeSource -match "Parameters\.ContainsKey\('CompiledIndexSource'\)") `
    'Facade dispatch no longer propagates the selected compiled-index source to compatible tools.'
Assert-CriticalTool (
    $facadeSource -match '\$automaticJsonFallbackCommands\s*=\s*@\([^)]*''start''[^)]*''brief''[^)]*''research''') `
    'Cold-checkout start must select the JSON baseline alongside brief and research.'

$helpOutput = @(& (Join-Path $PSScriptRoot 'Show-LlmWikiHelp.ps1') -Tier core 6>&1 | ForEach-Object { [string]$_ })
Assert-CriticalTool ($helpOutput -contains 'Command stability tiers: core, governed, experimental.') 'Registry-backed compact help omitted stability tiers.'
$commandHelp = @(& (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') topology -Help 6>&1 | ForEach-Object { [string]$_ })
Assert-CriticalTool (@($commandHelp | Where-Object { $_ -match 'topology.*Query.*CompiledIndexSource' }).Count -gt 0) 'Command-specific facade help omitted topology parameters.'
$limitValidationRejected = $false
try {
    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') privacy -Limit 100 2>&1 | Out-Null
} catch {
    $limitValidationRejected = $global:LASTEXITCODE -ne 0 -and $_.Exception.Message -match 'between 1 and 50'
} finally {
    $global:LASTEXITCODE = 0
}
Assert-CriticalTool $limitValidationRejected 'Facade validation failure did not publish a non-zero LASTEXITCODE.'

# Get-LlmWikiOwnershipImpact: synthetic input must stay bounded to the supplied
# path and resolve its nearest scoped guide without consulting the worktree diff.
$ownershipDiff = [pscustomobject]@{
    changedPaths = @('.llm-wiki/wiki.ps1')
    modules = @()
}
$ownership = & (Join-Path $PSScriptRoot 'Get-LlmWikiOwnershipImpact.ps1') `
    -DiffInput $ownershipDiff -Format Json | ConvertFrom-Json
Assert-CriticalTool (@($ownership.changedPaths).Count -eq 1) 'Ownership impact did not preserve the injected scope.'
Assert-CriticalTool ($ownership.ownershipGuides[0].guide -eq 'AGENTS.md') 'Wiki tooling ownership did not resolve the repository guide.'

# Get-LlmWikiVerificationStageFingerprint: identical material is stable and
# command arguments participate in the receipt identity.
$fingerprintA = & (Join-Path $PSScriptRoot 'Get-LlmWikiVerificationStageFingerprint.ps1') `
    -Stage 'page contracts' -Arguments @{ mode = 'a' } -Format Json | ConvertFrom-Json
$fingerprintB = & (Join-Path $PSScriptRoot 'Get-LlmWikiVerificationStageFingerprint.ps1') `
    -Stage 'page contracts' -Arguments @{ mode = 'a' } -Format Json | ConvertFrom-Json
$fingerprintC = & (Join-Path $PSScriptRoot 'Get-LlmWikiVerificationStageFingerprint.ps1') `
    -Stage 'page contracts' -Arguments @{ mode = 'b' } -Format Json | ConvertFrom-Json
Assert-CriticalTool ($fingerprintA.fingerprint -eq $fingerprintB.fingerprint) 'Verification-stage fingerprint is not deterministic.'
Assert-CriticalTool ($fingerprintA.fingerprint -ne $fingerprintC.fingerprint) 'Verification-stage fingerprint ignored command arguments.'

# Invoke-LlmWikiDeliveryFinalization: reject paths outside the durable task root
# before any lifecycle component can mutate state.
$invalidWorkspaceRejected = $false
try {
    & (Join-Path $PSScriptRoot 'Invoke-LlmWikiDeliveryFinalization.ps1') `
        -WorkspacePath '../outside' -Format Json | Out-Null
} catch {
    $invalidWorkspaceRejected = $_.Exception.Message -match 'directly inside'
}
Assert-CriticalTool $invalidWorkspaceRejected 'Delivery finalization accepted an out-of-root workspace.'

# Start-LlmWikiVerifyWorker: execute it out-of-process because the worker owns
# exit-code propagation and transcript lifetime.
$fixtureRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'critical-tool-contracts'
try {
    $unseenDraftPath = Join-Path $fixtureRoot 'unseen-draft.json'
    $unseenFrozenPath = Join-Path $fixtureRoot 'unseen-frozen.json'
    $unseenDraft = [pscustomobject]@{
        schemaVersion = 1
        status = 'draft-unseen-not-executable'
        cases = @([pscustomobject]@{
            id = 'independent-001'
            cohort = 'wiki-tooling'
            query = '<independent-author-query-required>'
            changeType = 'Any'
            expectedPaths = @('.llm-wiki/tools/code-graph.mjs')
        })
    }
    [IO.File]::WriteAllText($unseenDraftPath, ($unseenDraft | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
    $placeholderRejected = $false
    try {
        & (Join-Path $PSScriptRoot 'Complete-LlmWikiUnseenContextCorpus.ps1') `
            -DraftPath $unseenDraftPath -OutputPath $unseenFrozenPath | Out-Null
    } catch {
        $placeholderRejected = $_.Exception.Message -match 'independently authored query'
    }
    Assert-CriticalTool $placeholderRejected 'Unseen corpus freeze synthesized a query from the target path.'
    $unseenDraft.cases[0].query = 'Which JavaScript module ranks repository context?'
    [IO.File]::WriteAllText($unseenDraftPath, ($unseenDraft | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
    & (Join-Path $PSScriptRoot 'Complete-LlmWikiUnseenContextCorpus.ps1') `
        -DraftPath $unseenDraftPath -OutputPath $unseenFrozenPath | Out-Null
    $unseenFrozen = Get-Content -LiteralPath $unseenFrozenPath -Raw | ConvertFrom-Json
    Assert-CriticalTool ($unseenFrozen.status -eq 'frozen-independent-query-corpus') 'Unseen corpus freeze omitted its authorship status.'
    Assert-CriticalTool ($unseenFrozen.cases[0].query -eq $unseenDraft.cases[0].query) 'Unseen corpus freeze rewrote the authored query.'

    $answerDraftPath = Join-Path $fixtureRoot 'answer-draft.json'
    $answerCorpusPath = Join-Path $fixtureRoot 'answer-corpus.json'
    $answerSubmissionPath = Join-Path $fixtureRoot 'answer-submission.json'
    $answerReviewPath = Join-Path $fixtureRoot 'answer-review.json'
    $answerDraft = [pscustomobject]@{
        schemaVersion = 1
        status = 'draft-human-query-intake'
        description = 'Executable answer-quality contract fixture.'
        thresholds = [pscustomobject]@{
            minimumAverageCorrectness = 3.5; minimumAverageCompleteness = 3.5
            minimumAverageActionability = 3.25; minimumClaimCitationCoverage = 1
            minimumEvidenceRecall = 1; minimumValidCitationRate = 1; maximumUnsupportedClaimRate = 0
        }
        cases = @([pscustomobject]@{
            id = 'answer-001'; query = 'Which module ranks Wiki repository context?'
            authorship = [pscustomobject]@{
                source = 'independent-human-authored'; authorOrSessionId = 'fixture-author'
                collectedBeforeAnswerGeneration = $true
            }
            requiredEvidencePaths = @('.llm-wiki/tools/code-graph.mjs')
        })
    }
    [IO.File]::WriteAllText($answerDraftPath, ($answerDraft | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    & (Join-Path $PSScriptRoot 'Complete-LlmWikiAnswerEvaluationCorpus.ps1') `
        -DraftPath $answerDraftPath -OutputPath $answerCorpusPath -MinimumCaseCount 1 | Out-Null
    $answerSubmission = [pscustomobject]@{
        schemaVersion = 1; generatorId = 'fixture-generator'
        answers = @([pscustomobject]@{
            id = 'answer-001'; answer = 'The Node code graph module performs the ranking.'
            claims = @([pscustomobject]@{
                text = 'The Node code graph module performs the ranking.'
                citations = @([pscustomobject]@{ path = '.llm-wiki/tools/code-graph.mjs'; line = 1 })
            })
        })
    }
    $answerReview = [pscustomobject]@{
        schemaVersion = 1; reviewerId = 'fixture-reviewer'; independentOfGenerator = $true
        caseReviews = @([pscustomobject]@{
            id = 'answer-001'; correctness = 4; completeness = 4; actionability = 4
            unsupportedClaimCount = 0; notes = 'Fixture answer is grounded in the cited source.'
        })
    }
    [IO.File]::WriteAllText($answerSubmissionPath, ($answerSubmission | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($answerReviewPath, ($answerReview | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    $answerEvaluation = & (Join-Path $PSScriptRoot 'Measure-LlmWikiAnswerQuality.ps1') `
        -CorpusPath $answerCorpusPath -SubmissionPath $answerSubmissionPath -ReviewPath $answerReviewPath `
        -FailOnRegression -Format Json | ConvertFrom-Json
    Assert-CriticalTool ([bool]$answerEvaluation.passed -and $answerEvaluation.caseCount -eq 1 -and
        [double]$answerEvaluation.metrics.claimCitationCoverage -eq 1 -and
        [double]$answerEvaluation.metrics.validCitationRate -eq 1) 'Answer-quality evaluation did not enforce its grounded citation contract.'
    $answerReview.reviewerId = 'fixture-generator'
    [IO.File]::WriteAllText($answerReviewPath, ($answerReview | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    $selfReviewRejected = $false
    try {
        & (Join-Path $PSScriptRoot 'Measure-LlmWikiAnswerQuality.ps1') `
            -CorpusPath $answerCorpusPath -SubmissionPath $answerSubmissionPath -ReviewPath $answerReviewPath `
            -Format Json | Out-Null
    } catch {
        $selfReviewRejected = $_.Exception.Message -match 'reviewer must differ'
    }
    Assert-CriticalTool $selfReviewRejected 'Answer-quality evaluation accepted generator self-review.'

    $drift = & (Join-Path $PSScriptRoot 'Get-LlmWikiConcurrentDrift.ps1') -Format Json | ConvertFrom-Json
    $expectedDrift = @($drift.concurrentOrPreExistingPaths).Count -gt 0 -or [int]$drift.commitsAhead -gt 0
    Assert-CriticalTool ([bool]$drift.driftDetected -eq $expectedDrift) 'Concurrent drift result contradicted its reported evidence.'

    $contextCorpusPath = Join-Path $fixtureRoot 'context-corpus.json'
    $contextCorpus = [pscustomobject]@{
        schemaVersion = 1
        diagnosticLimit = 10
        thresholds = [pscustomobject]@{ minimumTop1Rate = 0; minimumTop10Rate = 0; minimumMeanReciprocalRank = 0 }
        switchCriteria = [pscustomobject]@{ minimumCaseCount = 1; minimumTop1Rate = 0; minimumTop10Rate = 0; minimumMeanReciprocalRank = 0 }
        cases = @([pscustomobject]@{
            id = 'tool-contract-context'
            query = 'JavaScript module ranks repository context'
            changeType = 'Any'
            expectedPaths = @('.llm-wiki/tools/code-graph.mjs')
        })
    }
    [IO.File]::WriteAllText($contextCorpusPath, ($contextCorpus | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
    $latency = & (Join-Path $PSScriptRoot 'Measure-LlmWikiContextLatency.ps1') `
        -CorpusPath $contextCorpusPath -Iterations 1 -Format Json | ConvertFrom-Json
    Assert-CriticalTool ($latency.iterations -eq 1 -and [double]$latency.warmQueryP95Ms -ge 0 -and [bool]$latency.workspaceStable) 'Context latency probe returned an inconsistent measurement.'
    $concurrency = & (Join-Path $PSScriptRoot 'Measure-LlmWikiContextConcurrency.ps1') `
        -CorpusPath $contextCorpusPath -Workers 2 -QueriesPerWorker 1 -Format Json | ConvertFrom-Json
    Assert-CriticalTool ($concurrency.queryCount -eq 2 -and [double]$concurrency.throughputPerSecond -gt 0 -and [bool]$concurrency.workspaceStable) 'Context concurrency probe returned an inconsistent measurement.'

    $observationPath = Join-Path $fixtureRoot 'context-observations.jsonl'
    & (Join-Path $PSScriptRoot 'Write-LlmWikiContextQueryObservation.ps1') `
        -DurationMs 12.345 -QueryTermCount 3 -CandidateCount 7 -TopLayer '' -TopRole '' -Ready $true -OutputPath $observationPath
    $observation = Get-Content -LiteralPath $observationPath | Select-Object -Last 1 | ConvertFrom-Json
    Assert-CriticalTool ([double]$observation.durationMs -eq 12.34 -and $observation.queryTermCount -eq 3 -and
        $observation.candidateCount -eq 7 -and $observation.topLayer -eq 'unknown' -and
        $observation.topRole -eq 'unknown' -and [bool]$observation.ready) 'Context observation writer did not preserve its public record contract.'

    $fakeWikiPath = Join-Path $fixtureRoot 'fake-wiki.ps1'
    $argumentsPath = Join-Path $fixtureRoot 'arguments.json'
    $logPath = Join-Path $fixtureRoot 'worker.log'
    [IO.File]::WriteAllText($fakeWikiPath, "param([string]`$Command, [string]`$Probe)`nif (`$Command -ne 'verify' -or `$Probe -ne 'ok') { throw 'worker arguments were not forwarded' }`nWrite-Host 'worker-probe-ok'`n", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($argumentsPath, '{"Probe":"ok"}', [Text.UTF8Encoding]::new($false))
    $shellPath = (Get-Process -Id $PID).Path
    $windowParameters = @{}
    if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
        $windowParameters.WindowStyle = 'Hidden'
    }
    $process = Start-Process @windowParameters -FilePath $shellPath -ArgumentList @(
        '-NoLogo', '-NoProfile', '-File', (Join-Path $PSScriptRoot 'Start-LlmWikiVerifyWorker.ps1'),
        '-WikiPath', $fakeWikiPath, '-ArgumentsPath', $argumentsPath, '-LogPath', $logPath
    ) -Wait -PassThru
    Assert-CriticalTool ($process.ExitCode -eq 0) 'Verify worker did not propagate a successful invocation.'
    Assert-CriticalTool ((Get-Content -LiteralPath $logPath -Raw) -match 'worker-probe-ok') 'Verify worker transcript omitted child output.'

    [IO.File]::WriteAllText($fakeWikiPath, "param([string]`$Command, [string]`$Probe)`nthrow 'worker-probe-failed'`n", [Text.UTF8Encoding]::new($false))
    $failureLogPath = Join-Path $fixtureRoot 'worker-failure.log'
    $failedProcess = Start-Process @windowParameters -FilePath $shellPath -ArgumentList @(
        '-NoLogo', '-NoProfile', '-File', (Join-Path $PSScriptRoot 'Start-LlmWikiVerifyWorker.ps1'),
        '-WikiPath', $fakeWikiPath, '-ArgumentsPath', $argumentsPath, '-LogPath', $failureLogPath
    ) -Wait -PassThru
    Assert-CriticalTool ($failedProcess.ExitCode -ne 0) 'Verify worker swallowed a child failure.'
    Assert-CriticalTool ((Get-Content -LiteralPath $failureLogPath -Raw) -match 'worker-probe-failed') 'Verify worker failure transcript omitted the child error.'
} finally {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}

# LlmWikiInProcessSqlite is referenced explicitly here; its full build/load and
# SQL parity contract remains exercised by Test-LlmWikiDomainDataSqlParity.ps1.
. (Join-Path $PSScriptRoot 'LlmWikiInProcessSqlite.ps1')
$sqliteFirst = Initialize-LlmWikiInProcessSqlite
$sqliteSecond = Initialize-LlmWikiInProcessSqlite
Assert-CriticalTool ([bool]$sqliteFirst.ready -and [bool]$sqliteSecond.ready) 'In-process SQLite reader did not initialize.'
Assert-CriticalTool ($sqliteFirst.fingerprint -eq $sqliteSecond.fingerprint -and $sqliteFirst.outputPath -eq $sqliteSecond.outputPath) 'In-process SQLite cache did not reuse the loaded runtime.'

Write-Host 'LLM Wiki critical tool contracts passed: ownership, fingerprints, lifecycle boundaries, context measurement, worker propagation, and SQLite cache reuse.'
