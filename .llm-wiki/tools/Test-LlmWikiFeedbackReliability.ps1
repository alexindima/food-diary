[CmdletBinding()]
param([ValidateSet('Pure', 'Integration', 'All')][string]$Group = 'All')
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiChangePacket.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
function Assert-Reliability([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

if ($Group -in @('Pure', 'All')) {
    & {
        Set-StrictMode -Version Latest
        $calibrationFixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repository -Name 'empty-cost-calibration'
        try {
            $evidencePath = Join-Path $calibrationFixture 'evidence.json'
            $repairPath = Join-Path $calibrationFixture 'no-repairs.json'
            $costSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Manage-LlmWikiVerificationCost.ps1'))
            $costAst = [Management.Automation.Language.Parser]::ParseInput($costSource, [ref]$null, [ref]$null)
            $calibrationFunction = $costAst.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Get-Calibration' }, $true)
            . ([scriptblock]::Create($calibrationFunction.Extent.Text))
            [IO.File]::WriteAllText($evidencePath, '{"checks":[]}')
            $estimate = [pscustomobject]@{ checkId = 'removed-check'; expectedTotalSeconds = 20 }
            Assert-Reliability ((Get-Calibration @($estimate)).resolvedCount -eq 0) 'A stale estimate for a removed check broke empty-check calibration.'
            [IO.File]::WriteAllText($evidencePath, '{"checks":[{"id":"removed-check","status":"passed","durationSeconds":10}]}')
            $measured = Get-Calibration @($estimate)
            Assert-Reliability ($measured.resolvedCount -eq 1 -and $measured.outcomes[0].actualTotalSeconds -eq 10) 'Current measured verification stopped contributing to cost calibration.'
        } finally {
            Remove-Item -LiteralPath $evidencePath -ErrorAction SilentlyContinue
        }
    }
    $packet = [pscustomobject]@{ fingerprint = 'same-task'; brief = @{ contract = 'original'; compiledIndex = @{ sqlDurationMs = 1; roundTripDurationMs = 2; sourceHashes = @{ frontend = 'first' } } } }
    $original = Get-LlmWikiPacketContentFingerprint $packet
    $packet.brief.compiledIndex.sqlDurationMs = 100
    Assert-Reliability ($original -ceq (Get-LlmWikiPacketContentFingerprint $packet)) 'SQL diagnostic timing invalidated a task packet.'
    $packet.brief.compiledIndex.sourceHashes.frontend = 'second'
    Assert-Reliability ($original -cne (Get-LlmWikiPacketContentFingerprint $packet)) 'Index lineage changed without packet-content invalidation.'
    $genericImpact = & (Join-Path $PSScriptRoot 'Get-LlmWikiImpact.ps1') -ChangedPath @('.llm-wiki/generated/quality-index.json', '.llm-wiki/generated/frontend-index.json', '.llm-wiki/generated/frontend-contract-index.json', '.llm-wiki/generated/backend-contract-index.json') -Format Json | ConvertFrom-Json
    $genericIds = @('workflow-quality-risk', 'workflow-security-review', 'workflow-trace-feature', 'workflow.frontend-contract-review', 'workflow-backend-contract-review')
    Assert-Reliability (@($genericImpact.impacts | Where-Object Id -in $genericIds).Count -eq 0) 'Generated record changes still invalidate generic command guides.'

    $fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repository -Name 'feedback-cache' -IndependentRepository
    $ownedParent = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture))
    try {
        $tools = Join-Path $fixture '.llm-wiki/tools'
        $workspace = '.artifacts/llm-wiki/tasks/cache'
        $null = New-Item -ItemType Directory -Path $tools,(Join-Path $fixture "$workspace/logs"),(Join-Path $fixture 'src'),(Join-Path $fixture '.llm-wiki/policies') -Force
        foreach ($name in @('LlmWikiAssessmentRunner', 'LlmWikiQueryCache', 'LlmWikiChangeSetSnapshot', 'LlmWikiGitPaths', 'Invoke-LlmWikiObservedStage', 'LlmWikiNavigationSemantics', 'LlmWikiJson', 'Get-LlmWikiImpact', 'Add-LlmWikiSourceReview')) {
            Copy-Item -LiteralPath (Join-Path $PSScriptRoot "$name.ps1") -Destination $tools
        }
        [IO.File]::WriteAllText((Join-Path $fixture '.gitignore'), '.artifacts/')
        [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), 'first')
        [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/policies/test.json'), '{}')
        [IO.File]::WriteAllText((Join-Path $fixture "$workspace/logs/check.log"), 'passed')
        [IO.File]::WriteAllText((Join-Path $fixture "$workspace/valid.txt"), 'true')
        $counter = Join-Path $fixture '.artifacts/calls.txt'
        $stub = @'
param([string]$Action, [string]$WorkspacePath, [string]$Format)
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$counter = Join-Path $root '.artifacts/calls.txt'
$count = if (Test-Path -LiteralPath $counter) { [int][IO.File]::ReadAllText($counter) } else { 0 }
[IO.File]::WriteAllText($counter, [string]($count + 1))
Start-Sleep -Milliseconds 1200
if (Test-Path -LiteralPath (Join-Path $root "$WorkspacePath/mutate.txt")) { [IO.File]::AppendAllText((Join-Path $root 'src/value.txt'), 'mutation') }
$timestamp = '{"value":"2026-01-01T00:04:30.0000000Z"}' | ConvertFrom-Json
@{ valid = [IO.File]::ReadAllText((Join-Path $root "$WorkspacePath/valid.txt")) -eq 'true'; call = $count + 1; stringDate = $timestamp.value -is [string] } | ConvertTo-Json
'@
        [IO.File]::WriteAllText((Join-Path $tools 'Manage-LlmWikiRequirementModel.ps1'), $stub)
        $null = New-Item -ItemType Directory -Path (Join-Path $fixture '.llm-wiki/reviews'),(Join-Path $fixture '.llm-wiki/workflows'),(Join-Path $fixture '.llm-wiki/generated') -Force
        [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/reviews/source-impact-reviews.json'), '{"schemaVersion":1,"reviews":[]}')
        $index = '{"schemaVersion":1,"generator":".llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1","symbols":[{"path":"src/a.ts","line":3,"selector":"fd-a"}]}'
        [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/generated/frontend-index.json'), $index)
        [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/workflows/probe.md'), "---`nid: probe`nkind: workflow`nstatus: current`nsources:`n  - src/value.txt`n  - .llm-wiki/generated/frontend-index.json`n---`nStable workflow.")
        & git -C $fixture add .
        & git -C $fixture -c user.name=WikiFixture -c user.email=wiki-fixture@example.invalid commit -qm 'Initialize reliability fixture'
        if ($LASTEXITCODE -ne 0) { throw 'Fixture commit failed.' }
        . (Join-Path $tools 'LlmWikiAssessmentRunner.ps1')
        $arguments = @{ Action = 'assess'; WorkspacePath = $workspace }
        $messages = @(Invoke-LlmWikiAssessment -ToolName 'Manage-LlmWikiRequirementModel.ps1' -Arguments $arguments -HeartbeatSeconds 1 6>&1)
        Assert-Reliability (($messages -join "`n") -match 'running, .*elapsed') 'A slow stage emitted no heartbeat.'
        $first = @($messages | Where-Object { $_ -is [pscustomobject] -and $_.PSObject.Properties['valid'] })[0]
        Assert-Reliability $first.stringDate 'An assessment worker rehydrated persisted date strings as CLR timestamps.'
        $second = Invoke-LlmWikiAssessment -ToolName 'Manage-LlmWikiRequirementModel.ps1' -Arguments $arguments
        Assert-Reliability ($first.valid -and $second.call -eq 1 -and [IO.File]::ReadAllText($counter) -eq '1') 'An unchanged successful assessment was executed twice.'
        [IO.File]::WriteAllText((Join-Path $fixture "$workspace/change-critique.json"), '{}')
        $withDerivedOutput = Invoke-LlmWikiAssessment -ToolName 'Manage-LlmWikiRequirementModel.ps1' -Arguments $arguments
        Assert-Reliability ($withDerivedOutput.call -eq 1) 'An unconsumed critique output invalidated reusable requirement assessment.'
        $cacheFile = @(Get-ChildItem -LiteralPath (Join-Path $fixture '.git/llm-wiki/query-cache/task-assessments') -File -Filter '*.json')[0]
        [IO.File]::WriteAllText($cacheFile.FullName, '{broken')
        $recovered = Invoke-LlmWikiAssessment -ToolName 'Manage-LlmWikiRequirementModel.ps1' -Arguments $arguments
        Assert-Reliability ($recovered.call -eq 2) 'A corrupt cache entry was replayed or blocked recovery.'
        $telemetryPath = if ($env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH) { $null } else { '.git/llm-wiki/verification-telemetry.json' }
        if ($telemetryPath) {
            [IO.File]::WriteAllText((Join-Path $fixture $telemetryPath), '{}')
            $telemetryChanged = Invoke-LlmWikiAssessment -ToolName 'Manage-LlmWikiRequirementModel.ps1' -Arguments $arguments
            Assert-Reliability ($telemetryChanged.call -eq 3) 'Ignored global telemetry changes replayed stale assessment evidence.'
        }
        foreach ($path in @('src/value.txt', "$workspace/logs/check.log", '.llm-wiki/policies/test.json', '.llm-wiki/tools/Manage-LlmWikiRequirementModel.ps1')) {
            [IO.File]::AppendAllText((Join-Path $fixture $path), "`n ")
            $beforeCount = [int][IO.File]::ReadAllText($counter)
            $rerun = Invoke-LlmWikiAssessment -ToolName 'Manage-LlmWikiRequirementModel.ps1' -Arguments $arguments
            Assert-Reliability ($rerun.call -eq $beforeCount + 1) "Changed dependency '$path' replayed stale success."
        }
        [IO.File]::WriteAllText((Join-Path $fixture "$workspace/valid.txt"), 'false')
        $failed = Invoke-LlmWikiAssessment -ToolName 'Manage-LlmWikiRequirementModel.ps1' -Arguments $arguments
        $failedAgain = Invoke-LlmWikiAssessment -ToolName 'Manage-LlmWikiRequirementModel.ps1' -Arguments $arguments
        Assert-Reliability (-not $failed.valid -and $failedAgain.call -gt $failed.call) 'A failed assessment was cached as success.'
        [IO.File]::WriteAllText((Join-Path $fixture "$workspace/valid.txt"), 'true')
        [IO.File]::WriteAllText((Join-Path $fixture "$workspace/mutate.txt"), 'mutate')
        $driftRejected = $false
        try { $null = Invoke-LlmWikiAssessment -ToolName 'Manage-LlmWikiRequirementModel.ps1' -Arguments $arguments } catch { $driftRejected = $_.Exception.Message -match 'inputs changed while' }
        Assert-Reliability $driftRejected 'A successful result from changing inputs escaped the cache freshness guard.'
        & (Join-Path $tools 'Add-LlmWikiSourceReview.ps1') -Id probe -Reason 'Reviewed the changed value.' -ChangedPath 'src/value.txt' | Out-Null
        [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/generated/frontend-index.json'), $index.Replace('"line":3', '"line":30'))
        $impact = & (Join-Path $tools 'Get-LlmWikiImpact.ps1') -ChangedPath @('src/value.txt', '.llm-wiki/generated/frontend-index.json') -Format Json | ConvertFrom-Json
        Assert-Reliability ($impact.unreviewedCount -eq 0) 'Coordinate-only changes invalidated a current semantic source review.'
        [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/generated/frontend-index.json'), $index.Replace('fd-a', 'fd-changed'))
        & (Join-Path $tools 'Add-LlmWikiSourceReview.ps1') -Id probe -Reason 'Reviewed the new selector contract.' -ChangedPath '.llm-wiki/generated/frontend-index.json' | Out-Null
        $impact = & (Join-Path $tools 'Get-LlmWikiImpact.ps1') -ChangedPath @('src/value.txt', '.llm-wiki/generated/frontend-index.json') -Format Json | ConvertFrom-Json
        Assert-Reliability ($impact.unreviewedCount -eq 0 -and @($impact.impacts[0].ReviewReceipt.sources).Count -eq 2) 'A partial review erased current review coverage for another source.'
        $retained = @($impact.impacts[0].ReviewReceipt.sources | Where-Object path -eq 'src/value.txt')[0]
        Assert-Reliability ($retained.reviewReason -eq 'Reviewed the changed value.') 'Retained source coverage lost its original rationale.'
        $ledgerHash = (Get-FileHash -LiteralPath (Join-Path $fixture '.llm-wiki/reviews/source-impact-reviews.json')).Hash
        & (Join-Path $tools 'Add-LlmWikiSourceReview.ps1') -Id probe -Reason 'Reviewed the new selector contract.' -ChangedPath '.llm-wiki/generated/frontend-index.json' | Out-Null
        Assert-Reliability ((Get-FileHash -LiteralPath (Join-Path $fixture '.llm-wiki/reviews/source-impact-reviews.json')).Hash -ceq $ledgerHash) 'An unchanged partial review rewrote its receipt.'
        [IO.File]::AppendAllText((Join-Path $fixture 'src/value.txt'), 'changed')
        $impact = & (Join-Path $tools 'Get-LlmWikiImpact.ps1') -ChangedPath @('src/value.txt', '.llm-wiki/generated/frontend-index.json') -Format Json | ConvertFrom-Json
        Assert-Reliability ($impact.unreviewedCount -eq 1 -and $impact.impacts[0].ReviewReason -eq 'reviewed-source-content-changed' -and @($impact.impacts[0].ReviewRequiredSources).Count -eq 1) 'Semantic source drift lacked precise review recovery.'
        & (Join-Path $tools 'Add-LlmWikiSourceReview.ps1') -Id probe -Reason 'Reviewed the new selector contract.' -ChangedPath '.llm-wiki/generated/frontend-index.json' | Out-Null
        $ledger = Get-Content -LiteralPath (Join-Path $fixture '.llm-wiki/reviews/source-impact-reviews.json') -Raw | ConvertFrom-Json
        Assert-Reliability (@($ledger.reviews[0].sources | Where-Object path -eq 'src/value.txt').Count -eq 0) 'A partial review retained stale source coverage.'
    } finally {
        if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture)) -cne $ownedParent) { throw 'Fixture cleanup escaped its owned parent.' }
        Remove-Item -LiteralPath $fixture -Recurse -Force
    }
}
if ($Group -in @('Integration', 'All')) {
    $workspace = New-LlmWikiSmokeFixtureRepositoryPath -RepositoryRoot $repository -Name 'feedback-refresh'
    $absolute = Join-Path $repository $workspace
    $previousTelemetryPath = $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH
    $telemetryPath = Join-Path $repository ".artifacts/llm-wiki/feedback-telemetry-$([guid]::NewGuid().ToString('N')).json"
    $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH = $telemetryPath
    try {
        & (Join-Path $PSScriptRoot 'Initialize-LlmWikiTaskWorkspace.ps1') -Objective 'Verify source index metadata refresh.' -Criterion 'The review packet reflects the current source metadata.' -WorkspacePath $workspace -ChangedPath 'docs/backend/BACKEND_MIGRATION_SAFETY.md' | Out-Null
        Assert-Reliability (-not (Test-Path -LiteralPath $telemetryPath)) 'The fresh-start fixture already has verification telemetry.'
        $confidence = & (Join-Path $PSScriptRoot 'Manage-LlmWikiConfidenceLedger.ps1') create -WorkspacePath $workspace -AsOfUtc ([DateTime]'2026-01-01T00:04:30Z') -Format Json | ConvertFrom-Json
        Assert-Reliability ($confidence.valid -and (Test-Path -LiteralPath $telemetryPath)) 'A fresh assessment did not initialize its telemetry baseline.'
        $critique = & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeCritique.ps1') create -WorkspacePath $workspace -Format Json | ConvertFrom-Json
        Assert-Reliability (@($critique.critique.findings.id) -notcontains 'operability-confidence-invalid') 'A child assessment rejected a valid saved confidence ledger.'
        Assert-Reliability ($critique.valid -and $critique.critique.verdict -in @('reject', 'request-changes')) 'Unresolved acceptance was approved by the optimized critique.'
        $verified = & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeCritique.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
        Assert-Reliability $verified.valid 'Independent verification rejected an unchanged critique.'
        $critique.critique.verdict = 'approve'
        [IO.File]::WriteAllText((Join-Path $absolute 'change-critique.json'), ($critique.critique | ConvertTo-Json -Depth 50))
        $tampered = & (Join-Path $PSScriptRoot 'Manage-LlmWikiChangeCritique.ps1') verify -WorkspacePath $workspace -Format Json | ConvertFrom-Json
        Assert-Reliability (-not $tampered.valid) 'A saved critique bypassed independent verification after verdict tampering.'
        $packet = Get-Content -LiteralPath (Join-Path $absolute 'change-packet.json') -Raw | ConvertFrom-Json
        $packet.brief | Add-Member -NotePropertyName reliabilityFixture -NotePropertyValue 'new metadata'
        $derived = @('impact-simulation.json', 'proof-of-change.json', 'plan-conformance.json', 'requirement-model.json', 'confidence-ledger.json', 'change-critique.json')
        foreach ($name in $derived) { [IO.File]::WriteAllText((Join-Path $absolute $name), '{}') }
        $preview = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') refresh -WorkspacePath $workspace -PacketInput $packet -DryRun -Format Json | ConvertFrom-Json
        Assert-Reliability ($preview.refreshRequired -and @($preview.derivedInvalidation.artifacts).Count -eq $derived.Count) 'Same task identity hid stale index contents from refresh preview.'
        Assert-Reliability (Test-Path -LiteralPath (Join-Path $absolute 'impact-simulation.json')) 'Refresh preview deleted a derived artifact.'
        $refreshed = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') refresh -WorkspacePath $workspace -PacketInput $packet -Format Json | ConvertFrom-Json
        Assert-Reliability (-not $refreshed.refreshRequired -and $refreshed.derivedInvalidation.reason -eq 'compiled-packet-content-changed') 'Refresh did not explain compiled metadata invalidation.'
        foreach ($name in $derived) { Assert-Reliability (-not (Test-Path -LiteralPath (Join-Path $absolute $name))) "Refresh retained stale $name." }
        [IO.File]::WriteAllText((Join-Path $absolute 'impact-simulation.json'), '{}')
        $stable = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') refresh -WorkspacePath $workspace -PacketInput $packet -Format Json | ConvertFrom-Json
        Assert-Reliability (@($stable.derivedInvalidation.artifacts).Count -eq 0 -and (Test-Path -LiteralPath (Join-Path $absolute 'impact-simulation.json'))) 'An unchanged refresh discarded successful assessment artifacts.'
        $packet.fingerprint = 'c' * 64
        $newIdentity = & (Join-Path $PSScriptRoot 'Manage-LlmWikiTaskWorkspace.ps1') refresh -WorkspacePath $workspace -PacketInput $packet -Format Json | ConvertFrom-Json
        Assert-Reliability ($newIdentity.reviewReportFreshness.state -eq 'current') 'Refresh returned the stale freshness state of the report it replaced.'
    } finally {
        $env:LLM_WIKI_VERIFICATION_TELEMETRY_PATH = $previousTelemetryPath
        if (Test-Path -LiteralPath $telemetryPath) { [IO.File]::Delete($telemetryPath) }
        $owned = [IO.Path]::GetFullPath((Join-Path $repository '.artifacts/llm-wiki/tasks'))
        if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($absolute)) -cne $owned -or [IO.Path]::GetFileName($absolute) -notmatch '^\.smoke-') { throw 'Task fixture cleanup escaped its owned scope.' }
        if (Test-Path -LiteralPath $absolute) { Remove-Item -LiteralPath $absolute -Recurse -Force }
    }
}
Write-Host "Wiki feedback reliability regressions passed: $Group."
