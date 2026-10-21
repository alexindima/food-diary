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
    foreach ($name in @('Get-LlmWikiPhaseStatus', 'Manage-LlmWikiAcceptanceMatrix', 'Manage-LlmWikiEvidence', 'Test-LlmWikiEvidenceLineage', 'New-LlmWikiEvidenceLineage', 'Test-LlmWikiChangePolicy', 'Get-LlmWikiContentFingerprint', 'LlmWikiCollections', 'LlmWikiEvidenceCommands', 'LlmWikiVerificationReceipts', 'LlmWikiGitPaths', 'LlmWikiGitRenames', 'LlmWikiJson', 'LlmWikiRequirementCriteria', 'LlmWikiSubstantiveReview')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot "$name.ps1") -Destination $fixtureTools
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../policies/workspace-policies.json') -Destination (Join-Path $fixture '.llm-wiki/policies')
    function Write-FixtureJson([string]$Path, [object]$Value) {
        [IO.File]::WriteAllText((Join-Path $fixture $Path), ($Value | ConvertTo-Json -Depth 30), [Text.UTF8Encoding]::new($false))
    }
    [IO.File]::WriteAllText((Join-Path $fixture '.gitignore'), '.artifacts/')
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), '1')
    [IO.File]::WriteAllText((Join-Path $fixture 'src/behavior.ps1'), 'if ([IO.File]::ReadAllText((Join-Path $PSScriptRoot "value.txt")) -cne "2") { throw "Unexpected value" }; Write-Output "Behavior verified"')
    $hostExecutable = (Get-Process -Id $PID).Path
    $command = '"' + $hostExecutable + '" -NoLogo -NoProfile -File src/behavior.ps1'
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
    $evidence = [pscustomobject]@{ git = @{ base = $base }; change = @{ changedPaths = @('src/value.txt') }; checks = @([pscustomobject]@{ id = 'behavior'; status = 'passed'; command = $command; reason = ''; lineage = $null }); reviews = @() }
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
    $logPath = "$workspace/logs/behavior.log"
    Push-Location $fixture
    try { & $hostExecutable -NoLogo -NoProfile -File src/behavior.ps1 *> (Join-Path $fixture $logPath); $checkExit = $LASTEXITCODE } finally { Pop-Location }
    Assert-Behavior ($checkExit -eq 0) 'The real focused fixture check failed.'
    $executed = & (Join-Path $fixtureTools 'New-LlmWikiEvidenceLineage.ps1') -Kind executed-check -EvidencePath "$workspace/evidence.json" -Id behavior -Command $command -Status passed -ExitCode $checkExit -Format Json | ConvertFrom-Json
    $executed | Add-Member -NotePropertyName artifact -NotePropertyValue @{ path = $logPath; sha256 = (Get-FileHash -LiteralPath (Join-Path $fixture $logPath)).Hash.ToLowerInvariant() }
    $evidence.checks[0].lineage = $executed
    Write-FixtureJson "$workspace/evidence.json" $evidence
    Assert-Behavior (-not (Read-Phase).valid) 'Legacy acceptance without a current criterion resolution completed the phase.'
    & (Join-Path $fixtureTools 'Manage-LlmWikiAcceptanceMatrix.ps1') resolve -Path "$workspace/acceptance-matrix.json" -CriterionId AC-001 -AcceptanceStatus satisfied -EvidenceNote 'src/behavior.ps1 asserts value 2.' | Out-Null
    $matrix = Get-Content -LiteralPath (Join-Path $fixture "$workspace/acceptance-matrix.json") -Raw | ConvertFrom-Json
    Assert-Behavior ((Read-Phase).valid -and (Read-Phase).currentPhase.state -eq 'verified') 'Current executed evidence did not verify the mapped acceptance outcome.'
    $manifest.plan.phases += @{ id = 'generated-artifacts'; order = 2; title = 'Refresh derived artifacts'; files = @('./.llm-wiki/wiki.ps1 update') }
    Write-FixtureJson "$workspace/change-manifest.json" $manifest
    Assert-Behavior ((Read-Phase 'generated-artifacts').valid) 'An artifact phase could not use task-wide acceptance and execution proof.'
    Assert-Behavior (-not (Read-Phase 'unknown').valid) 'An unknown phase ID was accepted.'
    [IO.File]::WriteAllText((Join-Path $fixture $logPath), 'Replaced log')
    Assert-Behavior (-not (Read-Phase).valid) 'A substituted execution log retained phase completion.'
    # Restore the real log and its recorded hash for the review scenarios.
    Push-Location $fixture
    try { & $hostExecutable -NoLogo -NoProfile -File src/behavior.ps1 *> (Join-Path $fixture $logPath) } finally { Pop-Location }
    $evidence.checks[0].lineage.artifact.sha256 = (Get-FileHash -LiteralPath (Join-Path $fixture $logPath)).Hash.ToLowerInvariant()
    Write-FixtureJson "$workspace/evidence.json" $evidence
    $review = @{ inspectedPaths = @('src/value.txt'); requirements = @(@{ criterionId = 'AC-001'; conclusion = 'satisfied'; evidence = 'src/behavior.ps1:1 asserts the persisted value is 2.' }); codeQuality = 'Reviewed the complete one-line value change and its focused assertion; no additional I/O or failure path was introduced.'; findings = @() }
    & (Join-Path $fixtureTools 'Manage-LlmWikiEvidence.ps1') review -Path "$workspace/evidence.json" -Id substantive-code-review -Status completed -Reason 'Independent pass over final fixture sources.' -ReviewContent $review | Out-Null
    $lineage = & (Join-Path $fixtureTools 'Test-LlmWikiEvidenceLineage.ps1') -WorkspacePath $workspace -Format Json | ConvertFrom-Json
    Assert-Behavior ($lineage.valid -and $lineage.substantiveReview.present) 'A complete substantive review did not validate through the existing evidence route.'
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
