[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LlmWikiBehaviorSpecification.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSubstantiveReview.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiRequirementCriteria.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
function Assert-Behavior([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
$spec = @{ currentBehavior = 'Changed files can look complete.'; preservedBehavior = @('Existing evidence remains authoritative.'); nonGoals = @('Install another workflow.'); examples = @(
    @{ kind = 'success'; given = 'A satisfied criterion and current executed check'; when = 'Complete the phase'; then = 'The phase is verified.' }
    @{ kind = 'failure'; given = 'Only a changed file'; when = 'Complete the phase'; then = 'Completion fails with missing evidence.' }
) }
$specified = Get-LlmWikiBehaviorSpecification -Objective 'Verify phase behavior' -InputSpec $spec -Criterion @('Completion requires current executed evidence.') -SourcePath @('source.ps1') -Required $true
Assert-Behavior ($specified.status -eq 'specified' -and $specified.examples.Count -eq 2) 'Explicit behavior examples did not produce a usable specification.'
$draft = Get-LlmWikiBehaviorSpecification -Objective 'Verify phase behavior' -Required $true
Assert-Behavior ($draft.status -eq 'needs-input' -and $draft.examples.Count -eq 0) 'Missing behavior was silently invented.'
$small = Get-LlmWikiBehaviorSpecification -Objective 'Fix a typo' -Required $false
Assert-Behavior ($small.status -eq 'not-required') 'Small work gained mandatory specification ceremony.'
Assert-Behavior (-not (Test-LlmWikiSubstantiveReview -Evidence ([pscustomobject]@{}) -Required).valid) 'Required semantic review was satisfied by its absence.'
$spec.openQuestions = @('Should a manual pass count?')
Assert-Behavior ((Get-LlmWikiBehaviorSpecification -Objective 'Verify phases' -InputSpec $spec -Criterion @('Use execution evidence.') -Required $true).status -eq 'needs-input') 'An unresolved product decision did not block design.'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'behavior-workflow' -IndependentRepository
$ownedParent = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture))
try {
    $fixtureTools = Join-Path $fixture '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $fixtureTools,(Join-Path $fixture '.llm-wiki/policies'),(Join-Path $fixture 'src') -Force
    foreach ($name in @('Get-LlmWikiPhaseStatus', 'Manage-LlmWikiAcceptanceMatrix', 'Manage-LlmWikiEvidence', 'Manage-LlmWikiVerificationReceipts', 'Import-LlmWikiEvidenceReceipts', 'Invoke-LlmWikiTaskChecks', 'Resolve-LlmWikiRecordedCheckResult', 'Recover-LlmWikiEvidenceExecution', 'Test-LlmWikiEvidenceLineage', 'New-LlmWikiEvidenceLineage', 'Test-LlmWikiChangePolicy', 'Get-LlmWikiContentFingerprint', 'LlmWikiCollections', 'LlmWikiEvidenceCommands', 'LlmWikiVerificationReceipts', 'LlmWikiGitPaths', 'LlmWikiGitRenames', 'LlmWikiJson', 'LlmWikiRequirementCriteria', 'LlmWikiSubstantiveReview')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot "$name.ps1") -Destination $fixtureTools
    }
    # Fault seams exist only in copied fixture scripts. They deterministically
    # mutate source after command completion and during receipt validation.
    $factoryPath = Join-Path $fixtureTools 'New-LlmWikiEvidenceLineage.ps1'
    $factoryText = [IO.File]::ReadAllText($factoryPath).Replace('$ErrorActionPreference = ''Stop''', @'
$ErrorActionPreference = 'Stop'
if (Test-Path (Join-Path $PSScriptRoot '../../.artifacts/lineage-drift')) { [IO.File]::WriteAllText((Join-Path $PSScriptRoot '../../src/value.txt'), '5') }
'@)
    [IO.File]::WriteAllText($factoryPath, $factoryText)
    $validatorPath = Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1'
    $validatorText = [IO.File]::ReadAllText($validatorPath).Replace('$ErrorActionPreference = ''Stop''', @'
$ErrorActionPreference = 'Stop'
if (Test-Path (Join-Path $PSScriptRoot '../../.artifacts/publication-drift')) { [IO.File]::WriteAllText((Join-Path $PSScriptRoot '../../src/unrelated.txt'), 'changed-during-publication') }
'@)
    [IO.File]::WriteAllText($validatorPath, $validatorText)
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../policies/workspace-policies.json') -Destination (Join-Path $fixture '.llm-wiki/policies')
    function Write-FixtureJson([string]$Path, [object]$Value) {
        [IO.File]::WriteAllText((Join-Path $fixture $Path), ($Value | ConvertTo-Json -Depth 30), [Text.UTF8Encoding]::new($false))
    }
    [IO.File]::WriteAllText((Join-Path $fixture '.gitignore'), '.artifacts/')
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '1')
    [IO.File]::WriteAllText((Join-Path $fixture 'src/unrelated.txt'), 'stable')
    [IO.File]::WriteAllText((Join-Path $fixture 'src/behavior.ps1'), '$countPath = Join-Path $PSScriptRoot "../.artifacts/run-count.txt"; $count = if (Test-Path $countPath) { [int][IO.File]::ReadAllText($countPath) } else { 0 }; [IO.File]::WriteAllText($countPath, [string]($count + 1)); if ([IO.File]::ReadAllText((Join-Path $PSScriptRoot "value.txt")) -cne "2") { throw "Unexpected value" }; if (Test-Path (Join-Path $PSScriptRoot "../.artifacts/drift")) { [IO.File]::WriteAllText((Join-Path $PSScriptRoot "value.txt"), "4") }; Write-Output "Behavior verified"')
    $hostExecutable = (Get-Process -Id $PID).Path
    $command = './.llm-wiki/wiki.ps1 fixture-behavior'
    [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/wiki.ps1'), '& (Join-Path $PSScriptRoot "../src/behavior.ps1")')
    Write-FixtureJson '.llm-wiki/policies/change-policies.json' @{ schemaVersion = 1; rules = @(@{ id = 'fixture'; description = 'Real focused behavior check'; pathPatterns = @('^src/'); excludePatterns = @(); requiredChecks = @(@{ id = 'behavior'; command = $command }); reviewObligations = @() }) }
    & git -C $fixture add .
    & git -C $fixture -c user.name=WikiFixture -c user.email=wiki-fixture@example.invalid commit -qm 'Initialize isolated behavior fixture'
    if ($LASTEXITCODE -ne 0) { throw 'Could not initialize fixture baseline.' }
    $base = ([string](& git -C $fixture rev-parse HEAD)).Trim()
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '2')
    & git -C $fixture add src/value.txt
    $workspace = '.artifacts/llm-wiki/tasks/phase'
    $null = New-Item -ItemType Directory -Path (Join-Path $fixture "$workspace/logs") -Force
    $manifest = @{ git = @{ base = $base }; plan = @{ phases = @(@{ id = 'implementation'; order = 1; title = 'Observable behavior'; files = @('src/value.txt') }); requiredChecks = @(@{ id = 'behavior' }); reviewObligations = @() } }
    Write-FixtureJson "$workspace/change-manifest.json" $manifest
    $matrix = @{ criteria = @(@{ id = 'AC-001'; text = 'The fixture returns the expected value.'; status = 'satisfied'; mapping = @{ changedPaths = @('src/value.txt'); testPaths = @(); checkIds = @('behavior'); reviewIds = @() }; resolution = @{ reason = 'Verified by the focused check.'; evidenceNote = 'The actual check asserts value 2.' } }) }
    Write-FixtureJson "$workspace/acceptance-matrix.json" $matrix
    $evidence = [pscustomobject]@{ git = @{ base = $base }; change = @{ changedPaths = @('src/value.txt') }; checks = @([pscustomobject]@{ id = 'behavior'; status = 'passed'; command = $command; durationSeconds = $null; reason = ''; lineage = $null }); reviews = @() }
    Write-FixtureJson "$workspace/evidence.json" $evidence
    function Read-Phase([string]$Id = 'implementation', [string]$Action = 'complete') {
        & (Join-Path $fixtureTools 'Get-LlmWikiPhaseStatus.ps1') -WorkspacePath $workspace -PhaseId $Id -Action $Action -Format Json | ConvertFrom-Json
    }
    $unproven = Read-Phase
    Assert-Behavior (-not $unproven.valid -and $unproven.currentPhase.changedFiles -contains 'src/value.txt') 'Staged files alone incorrectly completed the phase.'
    $manual = & (Join-Path $fixtureTools 'New-LlmWikiEvidenceLineage.ps1') -Kind manual-check -EvidencePath "$workspace/evidence.json" -Id behavior -Command $command -Status passed -Reason 'Manual label' -Format Json | ConvertFrom-Json
    $evidence.checks[0].lineage = $manual
    Write-FixtureJson "$workspace/evidence.json" $evidence
    Assert-Behavior (-not (Read-Phase).valid) 'Manual passed evidence was mistaken for executed verification.'
    Push-Location $fixture
    try { & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') run -Path "$workspace/evidence.json" -Id behavior -NoExitOnFailure | Out-Null } finally { Pop-Location }
    $evidence = Get-Content -LiteralPath (Join-Path $fixture "$workspace/evidence.json") -Raw | ConvertFrom-Json
    Assert-Behavior ($evidence.checks[0].lineage.PSObject.Properties['artifact'] -and $null -ne $evidence.checks[0].lineage.artifact) 'Direct evidence-run did not capture the real execution log.'
    $logPath = [string]$evidence.checks[0].lineage.artifact.path
    Assert-Behavior (([IO.File]::ReadAllText((Join-Path $fixture $logPath))) -match 'Behavior verified') 'Direct evidence-run log omitted actual command output.'
    Assert-Behavior (-not (Read-Phase).valid) 'Legacy acceptance without a current criterion resolution completed the phase.'
    & (Join-Path $fixtureTools 'Manage-LlmWikiAcceptanceMatrix.ps1') resolve -Path "$workspace/acceptance-matrix.json" -CriterionId AC-001 -AcceptanceStatus satisfied -EvidenceNote 'src/behavior.ps1 asserts value 2.' | Out-Null
    $matrix = Get-Content -LiteralPath (Join-Path $fixture "$workspace/acceptance-matrix.json") -Raw | ConvertFrom-Json
    Assert-Behavior ((Read-Phase).valid -and (Read-Phase).currentPhase.state -eq 'verified') 'Current executed evidence did not verify the mapped acceptance outcome.'
    # Inferred phase candidates may overlap another task's changed sources.
    # Reuse genuine executed evidence while checking only phase ownership here.
    [IO.File]::WriteAllText((Join-Path $fixture 'src/unrelated.txt'), 'changed-by-another-task')
    $manifest.plan.phases[0].files = @('src/value.txt', 'src/unrelated.txt')
    Write-FixtureJson "$workspace/change-manifest.json" $manifest
    Write-FixtureJson "$workspace/change-packet.json" @{ diff = @{ changedPaths = @('src/value.txt') } }
    $foreignPhase = Read-Phase
    Assert-Behavior ($foreignPhase.valid -and $foreignPhase.currentPhase.changedFiles -contains 'src/value.txt' -and $foreignPhase.currentPhase.changedFiles -notcontains 'src/unrelated.txt') 'Another task changed an inferred phase candidate and incorrectly required acceptance mapping in this task.'
    Write-FixtureJson "$workspace/change-packet.json" @{ diff = @{ changedPaths = @('src/value.txt', 'src/unrelated.txt') } }
    $unmappedOwnedPhase = Read-Phase
    Assert-Behavior (-not $unmappedOwnedPhase.valid -and ($unmappedOwnedPhase.currentPhase.issues -join ' ') -match "Map changed phase source 'src/unrelated.txt'") 'An owned observed phase change bypassed acceptance mapping.'
    [IO.File]::Delete((Join-Path $fixture "$workspace/change-packet.json"))
    Assert-Behavior ((Read-Phase).valid) 'A legacy packet-less workspace lost its explicit evidence change scope.'
    [IO.File]::WriteAllText((Join-Path $fixture 'src/unrelated.txt'), 'stable')
    $manifest.plan.phases[0].files = @('src/value.txt')
    Write-FixtureJson "$workspace/change-manifest.json" $manifest
    $targetWorkspace = '.artifacts/llm-wiki/tasks/imported-phase'
    $null = New-Item -ItemType Directory -Path (Join-Path $fixture $targetWorkspace) -Force
    Write-FixtureJson "$targetWorkspace/change-manifest.json" $manifest
    Write-FixtureJson "$targetWorkspace/acceptance-matrix.json" $matrix
    $targetEvidence = $evidence | ConvertTo-Json -Depth 20 | ConvertFrom-Json
    $targetEvidence.checks[0].status = 'pending'; $targetEvidence.checks[0].lineage = $null
    Write-FixtureJson "$targetWorkspace/evidence.json" $targetEvidence
    $runsBeforeImport = [IO.File]::ReadAllText((Join-Path $fixture '.artifacts/run-count.txt'))
    $import = & (Join-Path $fixtureTools 'Import-LlmWikiEvidenceReceipts.ps1') -WorkspacePath $targetWorkspace -Format Json | ConvertFrom-Json
    Assert-Behavior ($import.importedCount -eq 1) 'Actual execution receipt was not imported into another unsealed workspace.'
    $targetPhase = & (Join-Path $fixtureTools 'Get-LlmWikiPhaseStatus.ps1') -WorkspacePath $targetWorkspace -PhaseId implementation -Action complete -Format Json | ConvertFrom-Json
    Assert-Behavior ($targetPhase.valid) 'Imported actual execution proof did not complete the target phase.'
    Assert-Behavior ([IO.File]::ReadAllText((Join-Path $fixture '.artifacts/run-count.txt')) -ceq $runsBeforeImport) 'Import reran the command instead of preserving actual execution proof.'
    $actualTarget = Get-Content -LiteralPath (Join-Path $fixture "$targetWorkspace/evidence.json") -Raw | ConvertFrom-Json
    Assert-Behavior ($actualTarget.checks[0].lineage.artifact.path -cne $logPath -and $actualTarget.checks[0].lineage.artifact.sha256 -ceq $evidence.checks[0].lineage.artifact.sha256) 'Import failed to copy the exact actual log into its target boundary.'
    & (Join-Path $fixtureTools 'Manage-LlmWikiVerificationReceipts.ps1') -Action Record -RepositoryRoot $fixture -Command $command -Format Json | Out-Null
    Write-FixtureJson "$targetWorkspace/evidence.json" $targetEvidence
    & (Join-Path $fixtureTools 'Import-LlmWikiEvidenceReceipts.ps1') -WorkspacePath $targetWorkspace -Format Json | Out-Null
    $legacyTarget = Get-Content -LiteralPath (Join-Path $fixture "$targetWorkspace/evidence.json") -Raw | ConvertFrom-Json
    Assert-Behavior ($legacyTarget.checks[0].lineage.kind -eq 'manual-check') 'A schema1 receipt was promoted into executed proof.'
    $legacyPhase = & (Join-Path $fixtureTools 'Get-LlmWikiPhaseStatus.ps1') -WorkspacePath $targetWorkspace -PhaseId implementation -Action complete -Format Json | ConvertFrom-Json
    Assert-Behavior (-not $legacyPhase.valid) 'Legacy manual import completed a behavioral phase.'
    $manualRejected = $false
    try { & (Join-Path $fixtureTools 'Manage-LlmWikiVerificationReceipts.ps1') -Action Record -RepositoryRoot $fixture -Command $command -SourceEvidencePath "$targetWorkspace/evidence.json" -CheckId behavior -Format Json | Out-Null }
    catch { $manualRejected = $_.Exception.Message -match 'current passing executed check' }
    Assert-Behavior $manualRejected 'A manual check published a schema2 execution receipt.'
    & (Join-Path $fixtureTools 'Manage-LlmWikiVerificationReceipts.ps1') -Action Record -RepositoryRoot $fixture -Command $command -SourceEvidencePath "$workspace/evidence.json" -CheckId behavior -Format Json | Out-Null
    $outsideRejected = $false
    try { & (Join-Path $fixtureTools 'New-LlmWikiEvidenceLineage.ps1') -Kind executed-check -EvidencePath "$workspace/evidence.json" -Id behavior -Command $command -Status passed -ExitCode 0 -ArtifactPath 'src/value.txt' -Format Json | Out-Null }
    catch { $outsideRejected = $_.Exception.Message -match 'evidence boundary' }
    Assert-Behavior $outsideRejected 'Execution binding accepted a log outside the owned evidence boundary.'
    $scopedProof = & (Join-Path $fixtureTools 'New-LlmWikiEvidenceLineage.ps1') -Kind executed-check -EvidencePath "$targetWorkspace/evidence.json" -Id behavior -Command $command -Status passed -ExitCode 0 -DurationSeconds $evidence.checks[0].durationSeconds -ArtifactPath $actualTarget.checks[0].lineage.artifact.path -Format Json | ConvertFrom-Json
    $legacyTarget.checks[0].lineage = $scopedProof
    Write-FixtureJson "$targetWorkspace/evidence.json" $legacyTarget
    $scopedRejected = $false
    try { & (Join-Path $fixtureTools 'Manage-LlmWikiVerificationReceipts.ps1') -Action Record -RepositoryRoot $fixture -Command $command -SourceEvidencePath "$targetWorkspace/evidence.json" -CheckId behavior -Format Json | Out-Null }
    catch { $scopedRejected = $_.Exception.Message -match 'whole-repository execution seal' }
    Assert-Behavior $scopedRejected 'A scoped external binding was promoted to a whole-repository execution receipt.'
    Write-FixtureJson "$targetWorkspace/evidence.json" $targetEvidence
    [IO.File]::Move((Join-Path $fixture $logPath), (Join-Path $fixture ($logPath + '.saved')))
    $missingLogImport = & (Join-Path $fixtureTools 'Import-LlmWikiEvidenceReceipts.ps1') -WorkspacePath $targetWorkspace -DryRun -Format Json | ConvertFrom-Json
    Assert-Behavior ($missingLogImport.importedCount -eq 0) 'A missing actual log remained reusable.'
    [IO.File]::Move((Join-Path $fixture ($logPath + '.saved')), (Join-Path $fixture $logPath))
    $sourceBeforeTampering = [IO.File]::ReadAllText((Join-Path $fixture "$workspace/evidence.json"))
    $substitutedCommand = $sourceBeforeTampering | ConvertFrom-Json
    $substitutedCommand.checks[0].command = './.llm-wiki/wiki.ps1 different-check'
    Write-FixtureJson "$workspace/evidence.json" $substitutedCommand
    $commandImport = & (Join-Path $fixtureTools 'Import-LlmWikiEvidenceReceipts.ps1') -WorkspacePath $targetWorkspace -DryRun -Format Json | ConvertFrom-Json
    Assert-Behavior ($commandImport.importedCount -eq 0) 'A substituted source command retained reusable execution proof.'
    [IO.File]::WriteAllText((Join-Path $fixture "$workspace/evidence.json"), $sourceBeforeTampering)
    $manifest.plan.phases += @{ id = 'generated-artifacts'; order = 2; title = 'Refresh derived artifacts'; files = @('./.llm-wiki/wiki.ps1 update') }
    Write-FixtureJson "$workspace/change-manifest.json" $manifest
    Assert-Behavior ((Read-Phase 'generated-artifacts').valid) 'An artifact phase could not use task-wide acceptance and execution proof.'
    Assert-Behavior (-not (Read-Phase 'unknown').valid) 'An unknown phase ID was accepted.'
    [IO.File]::WriteAllText((Join-Path $fixture $logPath), 'Replaced log')
    Assert-Behavior (-not (Read-Phase).valid) 'A substituted execution log retained phase completion.'
    $tamperedImport = & (Join-Path $fixtureTools 'Import-LlmWikiEvidenceReceipts.ps1') -WorkspacePath $targetWorkspace -DryRun -Format Json | ConvertFrom-Json
    Assert-Behavior ($tamperedImport.importedCount -eq 0) 'A tampered actual log remained reusable through its receipt.'
    # Execute again through the public evidence path; never repair proof by hand.
    Push-Location $fixture
    try { & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') run -Path "$workspace/evidence.json" -Id behavior -NoExitOnFailure | Out-Null } finally { Pop-Location }
    $evidence = Get-Content -LiteralPath (Join-Path $fixture "$workspace/evidence.json") -Raw | ConvertFrom-Json
    # The task runner shares the real execution owner; unrelated telemetry and
    # workspace refresh are explicit fixture boundaries, not full readiness proof.
    [IO.File]::WriteAllText((Join-Path $fixtureTools 'Manage-LlmWikiVerificationTelemetry.ps1'), 'param($WorkspacePath, $CheckId, $Status, $DurationSeconds, $Command, $AsOfUtc, [Parameter(Position=0)]$Action); @{ valid = $true }')
    [IO.File]::WriteAllText((Join-Path $fixtureTools 'Manage-LlmWikiTaskWorkspace.ps1'), 'param([Parameter(Position=0)]$Action, $WorkspacePath); @{ valid = $true }')
    Push-Location $fixture
    try { $taskRun = & (Join-Path $fixtureTools 'Invoke-LlmWikiTaskChecks.ps1') -WorkspacePath $workspace -CheckId behavior -IncludePassed -Format Json | ConvertFrom-Json } finally { Pop-Location }
    $evidence = Get-Content -LiteralPath (Join-Path $fixture "$workspace/evidence.json") -Raw | ConvertFrom-Json
    Assert-Behavior ($taskRun.executedCount -eq 1 -and $taskRun.runs[0].logPath -ceq $evidence.checks[0].lineage.artifact.path) 'The task runner replaced the execution owner log.'
    $currentReceipts = @(& (Join-Path $fixtureTools 'Manage-LlmWikiVerificationReceipts.ps1') -Action List -RepositoryRoot $fixture -Format Json | ConvertFrom-Json)
    Assert-Behavior (@($currentReceipts | Where-Object { $_.schemaVersion -eq 2 -and $_.validForCurrentState }).Count -eq 1) 'The task runner invalidated its actual execution receipt.'
    Assert-Behavior ($evidence.checks[0].lineage.artifact.path -cne $logPath) 'A fresh execution overwrote the previous execution artifact.'
    $currentLog = [string]$evidence.checks[0].lineage.artifact.path
    [IO.File]::WriteAllText((Join-Path $fixture '.artifacts/drift'), '')
    $driftRejected = $false
    Push-Location $fixture
    try { try { & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') run -Path "$workspace/evidence.json" -Id behavior -NoExitOnFailure | Out-Null } catch { $driftRejected = $_.Exception.Message -match 'Source changed while check' } } finally { Pop-Location }
    Assert-Behavior $driftRejected 'Source edits during a passing command published current execution proof.'
    $historical = @(Get-ChildItem -LiteralPath (Join-Path $fixture "$workspace/logs") -Filter '*.execution.json' | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json } | Where-Object { $_.fingerprintBefore -cne $_.fingerprintAfter })
    Assert-Behavior ($historical.Count -eq 1 -and $historical[0].exitCode -eq 0 -and (Test-Path -LiteralPath $historical[0].logPath)) 'Rejected source drift lost the actual native outcome or historical log.'
    [IO.File]::Delete((Join-Path $fixture '.artifacts/drift'))
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '3')
    Push-Location $fixture
    try { & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') run -Path "$workspace/evidence.json" -Id behavior -NoExitOnFailure | Out-Null } finally { Pop-Location }
    $failedEvidence = Get-Content -LiteralPath (Join-Path $fixture "$workspace/evidence.json") -Raw | ConvertFrom-Json
    Assert-Behavior ($failedEvidence.checks[0].status -eq 'failed' -and $failedEvidence.checks[0].lineage.execution.exitCode -ne 0 -and $null -ne $failedEvidence.checks[0].lineage.artifact) 'A failed real command lost its outcome or was recorded as passing.'
    Assert-Behavior ([IO.File]::ReadAllText((Join-Path $fixture $failedEvidence.checks[0].lineage.artifact.path)) -match 'Unexpected value') 'Failure stderr was missing from the execution log.'
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '2')
    foreach ($boundary in @('lineage', 'publication')) {
        $beforeBoundaryEvidence = [IO.File]::ReadAllText((Join-Path $fixture "$workspace/evidence.json"))
        [IO.File]::WriteAllText((Join-Path $fixture ".artifacts/$boundary-drift"), '')
        $boundaryRejected = $false
        Push-Location $fixture
        try { try { & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') run -Path "$workspace/evidence.json" -Id behavior -NoExitOnFailure | Out-Null } catch { $boundaryRejected = $_.Exception.Message -match 'Source changed|Repository inputs changed|execution state|execution seal' } } finally { Pop-Location }
        Assert-Behavior $boundaryRejected "A source edit at the $boundary binding boundary published current execution proof."
        Assert-Behavior ([IO.File]::ReadAllText((Join-Path $fixture "$workspace/evidence.json")) -ceq $beforeBoundaryEvidence) 'A rejected execution changed the previous evidence bundle.'
        [IO.File]::Delete((Join-Path $fixture ".artifacts/$boundary-drift"))
        [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '2')
        [IO.File]::WriteAllText((Join-Path $fixture 'src/unrelated.txt'), 'stable')
    }
    Push-Location $fixture
    try { & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') run -Path "$workspace/evidence.json" -Id behavior -NoExitOnFailure | Out-Null } finally { Pop-Location }
    $evidence = Get-Content -LiteralPath (Join-Path $fixture "$workspace/evidence.json") -Raw | ConvertFrom-Json
    $review = @{ inspectedPaths = @('src/value.txt'); requirements = @(@{ criterionId = 'AC-001'; conclusion = 'satisfied'; evidence = 'src/behavior.ps1:1 asserts the persisted value is 2.' }); codeQuality = 'Reviewed the complete one-line value change and its focused assertion; no additional I/O or failure path was introduced.'; findings = @() }
    & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') review -Path "$workspace/evidence.json" -Id substantive-code-review -Status completed -Reason 'Independent pass over final fixture sources.' -ReviewContent $review | Out-Null
    $lineage = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior ($lineage.valid -and $lineage.substantiveReview.present) 'A complete substantive review did not validate through the existing evidence route.'
    $originalMatrixText = [IO.File]::ReadAllText((Join-Path $fixture "$workspace/acceptance-matrix.json"))
    $equivalentMatrix = $originalMatrixText | ConvertFrom-Json
    $equivalentMatrix.criteria[0].mapping.checkIds = 'behavior'
    $equivalentMatrix.criteria[0].mapping.testPaths = $null
    $equivalentMatrix.criteria[0].mapping.reviewIds = $null
    Write-FixtureJson "$workspace/acceptance-matrix.json" $equivalentMatrix
    $equivalentReview = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior $equivalentReview.substantiveReview.valid 'Equivalent scalar, duplicate and null mapping representations invalidated a completed review.'
    $equivalentMatrix.criteria[0].mapping.checkIds = @('behavior', 'behavior')
    Write-FixtureJson "$workspace/acceptance-matrix.json" $equivalentMatrix
    $duplicateMappingReview = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior $duplicateMappingReview.substantiveReview.valid 'A duplicate reference to the same check invalidated a completed review.'
    $originalReviewEvidence = [IO.File]::ReadAllText((Join-Path $fixture "$workspace/evidence.json"))
    $legacyReviewEvidence = $originalReviewEvidence | ConvertFrom-Json
    $legacyReviewEvidence.substantiveReview.payload.binding.requirements[0].mapping.checkIds = 'behavior'
    $legacyReviewEvidence.substantiveReview.payload.binding.requirements[0].mapping.testPaths = $null
    $legacyReviewEvidence.substantiveReview.payload.binding.requirements[0].mapping.PSObject.Properties.Remove('scenarioIds')
    $legacyReviewEvidence.substantiveReview.fingerprint = Get-LlmWikiJsonFingerprint $legacyReviewEvidence.substantiveReview.payload
    Write-FixtureJson "$workspace/evidence.json" $legacyReviewEvidence
    $legacyReview = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior $legacyReview.substantiveReview.valid 'An intact legacy attestation with equivalent mapping formatting required another review.'
    [IO.File]::WriteAllText((Join-Path $fixture "$workspace/evidence.json"), $originalReviewEvidence)
    $equivalentMatrix.criteria[0].mapping.checkIds = @('different-check')
    Write-FixtureJson "$workspace/acceptance-matrix.json" $equivalentMatrix
    $changedMappingReview = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior (-not $changedMappingReview.substantiveReview.valid) 'Changing the actual required check preserved review approval.'
    [IO.File]::WriteAllText((Join-Path $fixture "$workspace/acceptance-matrix.json"), $originalMatrixText)
    $recoveryEvidence = Get-Content -LiteralPath (Join-Path $fixture "$workspace/evidence.json") -Raw | ConvertFrom-Json
    $executionRecord = [string]$recoveryEvidence.checks[0].lineage.execution.verification.recordPath
    $executionLogPath = Join-Path $fixture ([string]$recoveryEvidence.checks[0].lineage.artifact.path)
    $originalLog = [IO.File]::ReadAllText($executionLogPath)
    $recoveryEvidence.checks[0].status = 'pending'
    $recoveryEvidence.checks[0].lineage = $null
    Write-FixtureJson "$workspace/evidence.json" $recoveryEvidence
    $pendingEvidence = [IO.File]::ReadAllText((Join-Path $fixture "$workspace/evidence.json"))
    $executionCount = @(Get-ChildItem -LiteralPath (Join-Path $fixture "$workspace/logs") -Filter '*.execution.json').Count
    $liveExecutionLease = [IO.File]::Open((Join-Path $fixture "$workspace/evidence.json.execution.lock"), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $rejected = $false
        try { & (Join-Path $fixtureTools 'Recover-LlmWikiEvidenceExecution.ps1') -EvidencePath "$workspace/evidence.json" -Id behavior -ExecutionRecordPath $executionRecord -Format Json | Out-Null } catch { $rejected = $_.Exception.Message -match 'Cannot acquire evidence execution lease' }
        Assert-Behavior $rejected 'Recovery raced a still-live evidence publisher.'
    } finally { $liveExecutionLease.Dispose() }
    [IO.File]::AppendAllText($executionLogPath, 'tampered')
    $rejected = $false
    try { & (Join-Path $fixtureTools 'Recover-LlmWikiEvidenceExecution.ps1') -EvidencePath "$workspace/evidence.json" -Id behavior -ExecutionRecordPath $executionRecord -Format Json | Out-Null } catch { $rejected = $_.Exception.Message -match 'log hash changed' }
    Assert-Behavior $rejected 'Recovery accepted a tampered execution log.'
    Assert-Behavior ([IO.File]::ReadAllText((Join-Path $fixture "$workspace/evidence.json")) -ceq $pendingEvidence) 'Rejected recovery modified evidence.'
    [IO.File]::WriteAllText($executionLogPath, $originalLog)
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '3')
    $rejected = $false
    try { & (Join-Path $fixtureTools 'Recover-LlmWikiEvidenceExecution.ps1') -EvidencePath "$workspace/evidence.json" -Id behavior -ExecutionRecordPath $executionRecord -Format Json | Out-Null } catch { $rejected = $_.Exception.Message -match 'current repository inputs' }
    Assert-Behavior $rejected 'Recovery promoted an execution against changed source.'
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '2')
    $recovered = & (Join-Path $fixtureTools 'Recover-LlmWikiEvidenceExecution.ps1') -EvidencePath "$workspace/evidence.json" -Id behavior -ExecutionRecordPath $executionRecord -Format Json | ConvertFrom-Json
    Assert-Behavior ($recovered.status -eq 'passed' -and $recovered.commandExitCode -eq 0 -and -not $recovered.commandRerun) 'Original successful execution was not recovered.'
    Assert-Behavior (@(Get-ChildItem -LiteralPath (Join-Path $fixture "$workspace/logs") -Filter '*.execution.json').Count -eq $executionCount) 'Recovery reran the command.'
    $recoveredLineage = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior $recoveredLineage.valid 'Recovered execution could not satisfy the real lineage validator.'
    $recordedText = [IO.File]::ReadAllText((Join-Path $fixture "$workspace/evidence.json"))
    $tampered = $recordedText | ConvertFrom-Json
    $tampered.substantiveReview.payload.content.codeQuality = 'Substituted review conclusion.'
    Write-FixtureJson "$workspace/evidence.json" $tampered
    $invalidReview = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior (-not $invalidReview.valid -and ($invalidReview.issues -join ' ') -match 'integrity') 'A substituted reviewer conclusion retained approval.'
    [IO.File]::WriteAllText((Join-Path $fixture "$workspace/evidence.json"), $recordedText)
    $review.findings = @(@{ id = 'R-1'; severity = 'major'; kind = 'defect'; location = @{ path = 'src/value.txt'; startLine = 1 }; trigger = 'A rejected input reaches this value.'; consequence = 'The expected result changes.'; testGap = 'Rejected inputs are not checked.'; remediation = 'Handle the rejected input.'; status = 'open' })
    & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') review -Path "$workspace/evidence.json" -Id substantive-code-review -Status completed -Reason 'Record an unresolved review finding.' -ReviewContent $review | Out-Null
    $blocked = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior (-not $blocked.valid -and ($blocked.issues -join ' ') -match 'Blocking code finding') 'An unresolved major finding did not block evidence validation.'
    $review.findings = @()
    & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') review -Path "$workspace/evidence.json" -Id substantive-code-review -Status completed -Reason 'Repeat the clean review.' -ReviewContent $review | Out-Null
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '3')
    Assert-Behavior (-not (Read-Phase).valid) 'Changed source retained stale phase verification.'
    $staleImport = & (Join-Path $fixtureTools 'Import-LlmWikiEvidenceReceipts.ps1') -WorkspacePath $targetWorkspace -DryRun -Format Json | ConvertFrom-Json
    Assert-Behavior ($staleImport.importedCount -eq 0) 'Changed repository inputs retained a reusable execution receipt.'
    $stale = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior (-not $stale.substantiveReview.valid -and ($stale.substantiveReview.issues -join ' ') -match 'stale') 'Source edits retained a current substantive review.'
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '2')
    $matrix.criteria[0].text = 'The fixture contains exactly value 2.'
    Write-FixtureJson "$workspace/acceptance-matrix.json" $matrix
    Assert-Behavior (-not (Read-Phase).valid) 'Changing criterion text retained phase completion.'
    $changedRequirements = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior (-not $changedRequirements.substantiveReview.valid) 'Changing requirements retained the prior review.'
    Write-Host 'Behavior workflow regression passed: specification, staged changes, execution lineage, source/log drift and substantive review closure.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if ([IO.Path]::GetDirectoryName($resolvedFixture) -cne $ownedParent) { throw 'Fixture cleanup escaped its owned parent.' }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}
