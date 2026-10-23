[CmdletBinding()]
param(
    [string]$SutPath = (Join-Path $PSScriptRoot 'Update-LlmWikiTaskEvidence.ps1'),
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$OutputRoot
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$sourceRoot = $RepositoryRoot
$windowsPaths = [IO.Path]::DirectorySeparatorChar -eq '\'
$pathComparison = if ($windowsPaths) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
$ownsOutputRoot = [string]::IsNullOrWhiteSpace($OutputRoot)
if ($ownsOutputRoot) {
    . (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
    $OutputRoot = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $sourceRoot -Name 'task-evidence-mapping-retention'
}
try {
$fixture=Join-Path $OutputRoot ('fixture-' + [Guid]::NewGuid().ToString())
$tools=Join-Path $fixture '.llm-wiki/tools'
$workspace='.artifacts/llm-wiki/tasks/fixture'
$null=New-Item -ItemType Directory -Path $tools,(Join-Path $fixture $workspace),(Join-Path $fixture 'src'),(Join-Path $fixture 'tests') -Force
Copy-Item -LiteralPath $SutPath -Destination (Join-Path $tools 'Update-LlmWikiTaskEvidence.ps1')
foreach($name in @('LlmWikiVerificationReceipts.ps1','LlmWikiGitPaths.ps1')){Copy-Item -LiteralPath (Join-Path $sourceRoot ".llm-wiki/tools/$name") -Destination $tools}
# Explicit isolated boundaries: no graph lookup or receipt import is under test.
[IO.File]::WriteAllText((Join-Path $tools 'Find-LlmWikiProductJourney.ps1'),'param($Query,$ChangedPath,$Format); ''{"journeys":[]}''')
[IO.File]::WriteAllText((Join-Path $tools 'Import-LlmWikiEvidenceReceipts.ps1'),'param($WorkspacePath); [IO.File]::WriteAllText((Join-Path $PSScriptRoot "../../.artifacts/import-boundary-called"),"observed"); ''{"importedCount":0}''')
function Write-Json([string]$Path,[object]$Value){[IO.File]::WriteAllText((Join-Path $fixture $Path),($Value|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))}
foreach($p in @('src/value.ps1','tests/focused.ps1','tests/explicit.ps1','tests/second-explicit.ps1')){[IO.File]::WriteAllText((Join-Path $fixture $p),'# owned fixture test reference')}
$caseSiblingRelative = $null
$caseSiblingUnavailableReason = 'Windows filesystem: case-sensitive sibling check is not applicable.'
if (-not $windowsPaths) {
    $caseSiblingName = [IO.Path]::GetFileName($fixture).ToUpperInvariant()
    $caseSibling = Join-Path $OutputRoot $caseSiblingName
    if (-not (Test-Path -LiteralPath $caseSibling)) {
        $null = New-Item -ItemType Directory -Path (Join-Path $caseSibling 'tests') -Force
        [IO.File]::WriteAllText((Join-Path $caseSibling 'tests/sibling.ps1'), '# existing sibling outside the copied repo')
        $caseSiblingRelative = '../' + $caseSiblingName + '/tests/sibling.ps1'
    } else { $caseSiblingUnavailableReason = 'Filesystem aliases case-only sibling names; no case-sensitive claim is made.' }
}
[IO.File]::WriteAllText((Join-Path $OutputRoot 'outside.ps1'),'# existing but outside copied repo')
$checkKeep=@{id='keep-check';command='./check.ps1';sourceRule='rule'}
$checkDrop=@{id='removed-check';command='./removed.ps1';sourceRule='rule'}
$reviewKeep=@{id='keep-review';description='Keep';sourceRule='rule'}
$reviewDrop=@{id='removed-review';description='Remove';sourceRule='rule'}
$old=@{fingerprint='old';requestedBaseRef='HEAD';inputs=@{gitHead='fixture-head';baseRef='fixture-base';headRef=$null};diff=@{changedPaths=@('src/value.ps1','src/removed.ps1');scopes=@();modules=@();renames=@()};policy=@{requiredChecks=@($checkKeep,$checkDrop);reviewObligations=@($reviewKeep,$reviewDrop);matchedRules=@(@{id='rule';matchedPaths=@('src/value.ps1')});violations=@()};testPlan=@{focusedTestFiles=@('tests/focused.ps1');scenarios=@(@{id='keep-scenario';description='Keep';evidence='Same'},@{id='changed-scenario';description='Old';evidence='Old'})}}
$new=$old|ConvertTo-Json -Depth 30|ConvertFrom-Json
$new.fingerprint='new';$new.diff.changedPaths=@('src/value.ps1');$new.policy.requiredChecks=@($checkKeep);$new.policy.reviewObligations=@($reviewKeep)
$new.testPlan.scenarios[1].description='Changed'
function Criterion([string]$Id,[object]$TestPaths,[object]$ChangedPaths=@(),[object]$Checks=@(),[object]$Reviews=@(),[object]$Scenarios=@()){
 @{id=$Id;text="Preserve $Id";status='satisfied';mapping=@{testPaths=$TestPaths;changedPaths=$ChangedPaths;checkIds=$Checks;reviewIds=$Reviews;scenarioIds=$Scenarios};resolution=@{reason='Prior reference';evidenceNote='Existing declared evidence relationship'}}
}
$criteria=@(
 (Criterion 'scalar-in-focus' 'tests/focused.ps1'),
 (Criterion 'scalar-explicit' 'tests/explicit.ps1'),
 (Criterion 'multiple-explicit' @('tests/focused.ps1','tests/explicit.ps1','tests/second-explicit.ps1')),
 (Criterion 'null' $null $null $null $null $null),
 (Criterion 'scalar-identities' @() 'src/value.ps1' 'keep-check' 'keep-review' 'keep-scenario'),
 (Criterion 'outside' '../outside.ps1'),
 (Criterion 'rooted' (Join-Path $fixture 'tests/explicit.ps1')),
 (Criterion 'missing' 'tests/missing.ps1'),
 (Criterion 'invalidated' 'tests/missing.ps1' 'src/removed.ps1' 'removed-check' 'removed-review' 'changed-scenario')
)
$matrix=@{objective='Keep explicit evidence references';criteria=$criteria;availableEvidence=@{scenarios=@();changedPaths=@();checks=@();reviews=@();testPaths=@()}}
if ($caseSiblingRelative) { $matrix.criteria += (Criterion 'case-sibling' $caseSiblingRelative) }
$evidence=@{structuralViolations=@();checks=@(@{id='keep-check';status='pending';command='./check.ps1'},@{id='removed-check';status='pending';command='./removed.ps1'});reviews=@(@{id='keep-review';status='pending';description='Keep'},@{id='removed-review';status='pending';description='Remove'});git=@{head='fixture-head';base='fixture-base';comparedHead=$null};change=@{changedPaths=@('src/value.ps1');scopes=@();modules=@()}}
Write-Json "$workspace/change-packet.json" $old
Write-Json "$workspace/.refresh-fixture-packet.json" $new
Write-Json "$workspace/acceptance-matrix.json" $matrix
Write-Json "$workspace/evidence.json" $evidence
$result=& (Join-Path $tools 'Update-LlmWikiTaskEvidence.ps1') -WorkspacePath $workspace -PacketPath "$workspace/.refresh-fixture-packet.json" -Apply -Format Json|ConvertFrom-Json
$actual=Get-Content -LiteralPath (Join-Path $fixture "$workspace/acceptance-matrix.json") -Raw|ConvertFrom-Json
$cases=[Collections.Generic.List[object]]::new()
function Check([string]$Id,[bool]$Passed,[object]$Observed){$cases.Add([pscustomobject]@{id=$Id;passed=$Passed;observed=$Observed})}
function M([string]$Id){($actual.criteria|Where-Object id -eq $Id).mapping}
Check 'scalar-in-inferred-focus-control' (@((M 'scalar-in-focus').testPaths).Count -eq 1 -and (M 'scalar-in-focus').testPaths[0] -eq 'tests/focused.ps1') (M 'scalar-in-focus')
Check 'scalar-explicit-existing-outside-focus' (@((M 'scalar-explicit').testPaths).Count -eq 1 -and (M 'scalar-explicit').testPaths[0] -eq 'tests/explicit.ps1') (M 'scalar-explicit')
Check 'array-multiple-explicit-existing-outside-focus' (@((M 'multiple-explicit').testPaths).Count -eq 3) (M 'multiple-explicit')
Check 'outside-and-rooted-references-removed' (@((M 'outside').testPaths).Count -eq 0 -and @((M 'rooted').testPaths).Count -eq 0) @((M 'outside'),(M 'rooted'))
Check 'missing-reference-removed' (@((M 'missing').testPaths).Count -eq 0) (M 'missing')
if ($caseSiblingRelative) {
    Check 'case-sensitive-sibling-outside-reference-removed' (@((M 'case-sibling').testPaths).Count -eq 0) (M 'case-sibling')
} else {
    $cases.Add([pscustomobject]@{ id = 'case-sensitive-sibling-outside-reference-removed'; passed = $null; notApplicable = $true; reason = $caseSiblingUnavailableReason })
}
$nullMapping=M 'null'
Check 'null-canonical-empty-all5-fields' (@($nullMapping.testPaths).Count -eq 0 -and @($nullMapping.changedPaths).Count -eq 0 -and @($nullMapping.checkIds).Count -eq 0 -and @($nullMapping.reviewIds).Count -eq 0 -and @($nullMapping.scenarioIds).Count -eq 0) $nullMapping
$ids=M 'scalar-identities'
Check 'scalar-changed-check-review-scenario-retained' (@($ids.changedPaths).Count -eq 1 -and @($ids.checkIds).Count -eq 1 -and @($ids.reviewIds).Count -eq 1 -and @($ids.scenarioIds).Count -eq 1) $ids
$invalid=M 'invalidated'
Check 'invalidated-identities-and-missing-test-removed' (@($invalid.testPaths).Count -eq 0 -and @($invalid.changedPaths).Count -eq 0 -and @($invalid.checkIds).Count -eq 0 -and @($invalid.reviewIds).Count -eq 0 -and @($invalid.scenarioIds).Count -eq 0 -and ($actual.criteria|Where-Object id -eq 'invalidated').status -eq 'pending') $invalid
Check 'native-apply-and-explicit-import-boundary-called' ($result.applied -and (Test-Path -LiteralPath (Join-Path $fixture '.artifacts/import-boundary-called'))) $result
$beforeNoop=($actual.criteria|Where-Object id -eq 'scalar-identities').mapping|ConvertTo-Json -Depth 10 -Compress
Write-Json "$workspace/change-packet.json" $new
& (Join-Path $tools 'Update-LlmWikiTaskEvidence.ps1') -WorkspacePath $workspace -PacketPath "$workspace/.refresh-fixture-packet.json" -Apply -Format Json|Out-Null
$afterNoop=Get-Content -LiteralPath (Join-Path $fixture "$workspace/acceptance-matrix.json") -Raw|ConvertFrom-Json
Check 'noop-refresh-identities-preserved' ((($afterNoop.criteria|Where-Object id -eq 'scalar-identities').mapping|ConvertTo-Json -Depth 10 -Compress) -ceq $beforeNoop) $null
$report=@{sutPath=$SutPath;sutSha256=(Get-FileHash -LiteralPath $SutPath -Algorithm SHA256).Hash.ToLowerInvariant();fixture=$fixture;boundary='Actual copied Update tool; real JSON IO; graph journey and receipts are explicit non-proof stubs. No checks executed or phase/evidence proof invented.';cases=@($cases);failedCount=@($cases|Where-Object passed -eq $false).Count}
$report|ConvertTo-Json -Depth 15|Set-Content -LiteralPath (Join-Path $OutputRoot 'latest.json') -Encoding utf8
foreach($case in $cases){
    if ($null -eq $case.passed) { Write-Host "$($case.id): N/A ($($case.reason))" }
    else { Write-Host "$($case.id): $($case.passed)" }
}
if($report.failedCount){throw "$($report.failedCount) causal mapping retention assertions failed"}
Write-Host 'Mapping refresh retention regression passed'

} finally {
    if ($ownsOutputRoot) {
        $sandbox = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $sourceRoot)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        if (-not [IO.Path]::GetFullPath($OutputRoot).StartsWith($sandbox, $pathComparison)) { throw 'Mapping fixture cleanup escaped its owned sandbox.' }
        Remove-Item -LiteralPath $OutputRoot -Recurse -Force
    }
}
