[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$facadeText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') -Raw
$fullVerificationText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/tools/Invoke-LlmWikiFullVerification.ps1') -Raw
$toolSmokeText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/tools/Test-LlmWikiTools.ps1') -Raw
$indexCacheText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/tools/LlmWikiIndexCache.ps1') -Raw
$pipelineText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/tools/Invoke-LlmWikiIndexPipeline.ps1') -Raw
$cachedBuilderTexts = @(
    'Build-LlmWikiFrontendIndex.ps1', 'Build-LlmWikiFrontendContractIndex.ps1',
    'Build-LlmWikiBackendContractIndex.ps1', 'Build-LlmWikiQualityIndex.ps1',
    'Build-LlmWikiArchitectureHealthIndex.ps1'
) | ForEach-Object { Get-Content -LiteralPath (Join-Path $repositoryRoot ".llm-wiki/tools/$_") -Raw }

if ($facadeText -notmatch "'verify-strict-affected'") { throw 'Wiki facade does not expose verify-strict-affected.' }
if ($facadeText -notmatch "'ui-finalize'") { throw 'Wiki facade does not expose one-time UI finalization.' }
if (-not $facadeText.Contains("`$Command = 'verify-fast'") -or -not $facadeText.Contains('Compatibility alias: verify -Fast -> verify-fast')) {
    throw 'Wiki facade does not support the verify -Fast compatibility alias.'
}
foreach ($progressContract in @('Starting Wiki verify stage:', 'Wiki verify stage still running:', 'Wiki verify stage timed out:', 'Run separately:')) {
    if (-not $facadeText.Contains($progressContract)) { throw "Observed verify omitted '$progressContract'." }
}
if ($facadeText -match 'Start-Job' -or -not $facadeText.Contains('Invoke-LlmWikiObservedStage.ps1')) {
    throw 'Observed verify must use an inherited-output child process rather than a buffered PowerShell job.'
}
$verifyStart = $facadeText.IndexOf("    'verify' {")
$verifyEnd = $facadeText.IndexOf("    'verify-fast' {", $verifyStart)
$verifyBody = if ($verifyStart -ge 0 -and $verifyEnd -gt $verifyStart) { $facadeText.Substring($verifyStart, $verifyEnd - $verifyStart) } else { '' }
if ($verifyBody -notmatch '\$stages\s*=\s*@\(' -or @('workspace policy', 'page contracts', 'lint regression', 'indexes', 'affected smoke', 'failure knowledge', 'change policy', 'source impact' | Where-Object { $verifyBody -notmatch [regex]::Escape($_) }).Count -gt 0) {
    throw 'Ordinary verify does not route every verification stage through the observed runner.'
}
$strictStart = $facadeText.IndexOf("    'verify-strict-affected' {")
$strictEnd = $facadeText.IndexOf("    { `$_ -in @('repair-verify', 'completion') } {", $strictStart)
if ($strictStart -lt 0 -or $strictEnd -le $strictStart) { throw 'Unable to isolate verify-strict-affected implementation.' }
$body = $facadeText.Substring($strictStart, $strictEnd - $strictStart)
foreach ($required in @('AffectedOnly = $true', 'Invoke-LlmWikiAffectedSmoke.ps1', 'FailOnViolation = $true', 'FailOnUnreviewed = $true')) {
    if (-not $body.Contains($required)) { throw "Strict affected verification omitted '$required'." }
}
if ($body -match 'ReuseUnchangedChecks|DeferPossiblyConcurrentStale') {
    throw 'Strict affected verification unexpectedly enables cache reuse or stale deferral.'
}
if ($body -match 'Invoke-ObservedWikiStage') { throw 'Strict affected verification accidentally inherited the full observed verify stages.' }
$visualFastStart = $facadeText.IndexOf("    'verify-fast' {")
$visualFastEnd = $facadeText.IndexOf("    'verify-strict-affected' {", $visualFastStart)
$visualFastBody = $facadeText.Substring($visualFastStart, $visualFastEnd - $visualFastStart)
if ($visualFastBody -notmatch 'VisualUiCompletion' -or $visualFastBody -notmatch 'index regeneration is deferred until ui-finalize') {
    throw 'Visual UI iteration does not defer index synchronization to ui-finalize.'
}
foreach ($receiptContract in @('verify-progress.json', 'Write-VerifyProgress', "'timed-out'", 'Buffered-shell progress receipt:')) {
    if (-not $facadeText.Contains($receiptContract)) { throw "Observed verify omitted durable progress contract '$receiptContract'." }
}
if ($verifyBody -notmatch "'affected smoke'" -or $verifyBody -notmatch 'Invoke-LlmWikiParallelSmoke\.ps1' -or $verifyBody -match "'adaptive verification' 'Invoke-LlmWikiAdaptiveVerification") {
    throw 'Ordinary product verify still replays the complete Wiki adaptive eval suite instead of affected tool regressions.'
}
if ($verifyBody -notmatch "verifyStageExpectedSeconds\['affected smoke'\]\s*=\s*240" -or
    $verifyBody -notmatch 'Timeout\s*=\s*420') {
    throw 'Affected smoke does not reserve enough time for the complete promoted context-search suite.'
}
if ($facadeText -notmatch "CI -ne 'true'" -or $facadeText -notmatch 'content-addressed stage resume') {
    throw 'Local verify does not enable resumable stages by default while keeping CI uncached.'
}
if ($facadeText -notmatch 'ContractIndexesOnly' -or $facadeText -notmatch 'RequiredOnly = \$ContractIndexesOnly') {
    throw 'Wiki facade does not expose the required contract/navigation index tier.'
}
if ($facadeText -notmatch 'No governed task workspace exists' -or $facadeText -notmatch 'task-start first') {
    throw 'Delivery status does not explain absent governed workspace state.'
}
if ($facadeText -notmatch "'repair-verify'" -or $facadeText -notmatch 'Repair verify \[1/3\]' -or $facadeText -notmatch 'pendingReviewIds') {
    throw 'Wiki facade does not expose the combined affected update, grouped impact, and resumable verify repair flow.'
}
if ($facadeText -notmatch 'Grouped source-impact review recorded' -or $facadeText -notmatch '\$ReviewId') {
    throw 'Wiki facade does not support one-rationale grouped source-impact reviews.'
}
foreach ($builderText in $cachedBuilderTexts) {
    $checkStart = $builderText.IndexOf('if ($Check)')
    $writeAfterCheck = $builderText.IndexOf('Write-LlmWikiIndexCache', $checkStart)
    $successMessage = $builderText.IndexOf('is current:', $checkStart)
    if ($checkStart -lt 0 -or $writeAfterCheck -lt $checkStart -or $writeAfterCheck -gt $successMessage) {
        throw 'A cacheable index check does not refresh its receipt after proving the output current.'
    }
}
if ($indexCacheText -notmatch 'TrimStart\(\[char\]0xFEFF\)' -or
    $indexCacheText -notmatch "'hash-object', '--stdin-paths'" -or
    $indexCacheText -notmatch 'RedirectStandardInput \$stdinPath' -or
    $indexCacheText -notmatch '\[Text\.UTF8Encoding\]::new\(\$false\)') {
    throw 'Index input fingerprints must normalize BOM-prefixed paths and use explicit BOM-free UTF-8 for native hashing, via a temp-file stdin redirect that behaves identically on every PowerShell/.NET runtime.'
}
if ($pipelineText -notmatch 'analytical indexes:' -or $pipelineText -notmatch 'API snapshot review:' -or $pipelineText -notmatch 'migration review:') {
    throw 'Affected index pipeline omitted the compact delivery summary.'
}
if ($pipelineText -notmatch 'affected pipeline cache hit' -or $pipelineText -notmatch 'Get-PipelineCacheState' -or $pipelineText -notmatch 'outputFingerprint') {
    throw 'Affected pipeline does not transfer exact successful index evidence to the pre-commit freshness check.'
}
if ($pipelineText -notmatch "'rev-parse', '--absolute-git-dir'" -or $pipelineText -match 'receiptPath = Join-Path \$repositoryRoot "\.artifacts/llm-wiki/index-cache') {
    throw 'Affected pipeline receipt is stored under cleanup-prone .artifacts instead of the durable Git directory.'
}
if ($pipelineText -notmatch 'Restore-OrphanedIndexTransaction' -or $pipelineText -notmatch "status = 'in-progress'" -or $pipelineText -notmatch "status = 'committed'") {
    throw 'Index updates do not recover an interrupted durable transaction before the next write.'
}
if ($facadeText -notmatch "'start'" -or $facadeText -notmatch 'Start-LlmWikiDevelopment\.ps1') {
    throw 'Wiki facade does not expose one-command baseline, research, checklist, and governed workspace startup.'
}
if ($facadeText -notmatch 'Repair verify \[0/3\]' -or $facadeText -notmatch 'Test-LlmWikiFormattingReady\.ps1') {
    throw 'Repair flow does not stabilize formatting before hashing and index generation.'
}
if ($facadeText -notmatch 'Wiki verify mode: bounded affected/resumable' -or $verifyBody -notmatch 'AffectedOnly = \$true') {
    throw 'Ordinary verify is not an affected/resumable gate by default.'
}
if ($facadeText -notmatch 'VerifyAfterUpdate' -or $facadeText -notmatch 'Update completed; continuing with resumable affected verify') {
    throw 'Update facade does not expose the one-command affected update and resumable verify flow.'
}
if ($facadeText -notmatch 'Stale task baseline' -or $facadeText -notmatch '-Action Close') {
    throw 'Facade does not retire stale task baselines with observable age and session context.'
}
if ($facadeText -notmatch "'review-affected'" -or $facadeText -notmatch 'AllowSharedReviewReason' -or $facadeText -notmatch 'ReviewAreaReason') {
    throw 'Facade does not group affected reviews by area or require explicit permission for a cross-area rationale.'
}
if ($facadeText -match '\$area\s*=\s*Get-ReviewArea') {
    throw 'review-affected reuses the validated facade -Area parameter as a local review-area variable.'
}
if ($facadeText -match "developer\|workflow'\) \{ return 'quality-workflow'") {
    throw 'review-affected classifies every workflow page as quality and makes the documentation area unreachable.'
}
if ($facadeText -notmatch "ValidateSet\('All', 'Backend', 'Frontend'\)" -or $verifyBody -notmatch 'Area = \$Area') {
    throw 'Wiki verify does not expose independently diagnosable Backend and Frontend areas.'
}
if ($facadeText -notmatch 'Invoke-LlmWikiReadOnlyTool\.ps1' -or $facadeText -notmatch 'explicitScopePlanningCommands') {
    throw 'Read-oriented facade commands are not protected from tracked Wiki writes or stale baseline expansion.'
}
$testPlanText = Get-Content -LiteralPath (Join-Path $repositoryRoot '.llm-wiki/tools/Get-LlmWikiTestPlan.ps1') -Raw
foreach ($contract in @('commandGroups', 'compile-direct-consumer', 'fullRegression')) {
    if (-not $testPlanText.Contains($contract)) { throw "Focused test planning omitted '$contract'." }
}

$frontendSmoke = @(& (Join-Path $repositoryRoot '.llm-wiki/tools/Invoke-LlmWikiAffectedSmoke.ps1') `
    -Plan -ChangedPath 'FoodDiary.Web.Client/src/app/example/example.ts' 6>&1 | ForEach-Object { $_.ToString() })
if (($frontendSmoke -join "`n") -notmatch 'no LLM Wiki implementation paths changed' -or ($frontendSmoke -join "`n") -match 'full-tools') {
    throw 'Strict affected verification expanded a product-only change into the monolithic Wiki tools smoke.'
}
$adaptiveDocsPlan = @(& (Join-Path $repositoryRoot '.llm-wiki/tools/Invoke-LlmWikiAffectedSmoke.ps1') `
    -Plan -ChangedPath '.llm-wiki/workflows/adaptive-development.md' 6>&1 | ForEach-Object { $_.ToString() }) -join "`n"
if ($adaptiveDocsPlan -match 'adaptive-routing' -or $adaptiveDocsPlan -notmatch 'facade-contract') {
    throw 'Adaptive workflow documentation still triggers the complete adaptive eval suite.'
}
$stylePolicy = & (Join-Path $repositoryRoot '.llm-wiki/tools/Test-LlmWikiChangePolicy.ps1') `
    -ChangedPath 'FoodDiary.Web.Client/src/app/example/example.scss' `
    -Format Json | ConvertFrom-Json
if (@($stylePolicy.reviewObligations.id) -contains 'frontend-public-contract') {
    throw 'A stylesheet-only change incorrectly requires Angular public-contract review.'
}
if (@($stylePolicy.reviewObligations.id) -notcontains 'frontend-accessibility') {
    throw 'A stylesheet-only change lost accessibility review.'
}
if ($fullVerificationText -notmatch 'still running' -or $fullVerificationText -notmatch 'groupStopwatch') {
    throw 'Full verification does not expose periodic per-group progress and duration.'
}
if ($fullVerificationText -notmatch 'LLM Wiki tool verification profile' -or $fullVerificationText -notmatch '\$FullTools' -or $fullVerificationText -notmatch '\$CoreTools') {
    throw 'Full verification does not expose its adaptive tool profile and explicit override.'
}
if ($facadeText -notmatch "\[string\]\`$VerificationProfile = 'Focused'" -or
    $facadeText -notmatch "fullVerificationArguments\.FullTools = \`$true" -or
    $facadeText -notmatch 'verify-runs/\$runId' -or
    $facadeText -notmatch 'runProgressPath') {
    throw 'verify-full does not default to focused coverage, retain an explicit Full audit, or isolate concurrent verify state.'
}
if ($fullVerificationText -notmatch 'Invoke-LlmWikiParallelSmoke\.ps1' -or $fullVerificationText -notmatch 'AllGroups') {
    throw 'Full verification omitted the complete focused regression suite.'
}
if ($toolSmokeText -notmatch "ValidateSet\('Focused', 'Core', 'Full'\)" -or $toolSmokeText -notmatch 'Skipped governed task-workspace and orchestration smoke coverage') {
    throw 'Tool smoke suite omitted its observable Focused, Core, and Full profiles.'
}
if ($toolSmokeText -notmatch '\[string\]\$Profile = ''Focused''' -or
    $toolSmokeText -notmatch 'Invoke-LlmWikiParallelSmoke\.ps1' -or
    $toolSmokeText -notmatch 'monolithic core phase completed' -or
    $toolSmokeText -notmatch 'LLM_WIKI_READ_ONLY_SNAPSHOT_ROOT' -or
    $toolSmokeText -notmatch 'PrepareCodeGraph') {
    throw 'Direct tool smoke must default to the parallel focused catalog while legacy audit profiles report phase timing.'
}

Write-Host 'LLM Wiki strict-affected smoke passed: scoped publication checks are uncached and non-deferred.'
