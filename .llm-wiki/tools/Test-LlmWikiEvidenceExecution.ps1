[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$repository = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repository -Name 'evidence-execution' -IndependentRepository
$ownedParent = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture))
try {
    $tools = Join-Path $fixture '.llm-wiki/tools'
    $null = New-Item -ItemType Directory -Path $tools,(Join-Path $fixture '.llm-wiki/policies'),(Join-Path $fixture 'src'),(Join-Path $fixture '.artifacts/foreign') -Force
    foreach ($name in @('Invoke-LlmWikiTaskChecks','Manage-LlmWikiEvidence','Manage-LlmWikiVerificationReceipts','Test-LlmWikiEvidenceLineage','LlmWikiCollections','LlmWikiEvidenceCommands','LlmWikiVerificationReceipts','LlmWikiSubstantiveReview','LlmWikiJson','LlmWikiGitPaths','Get-LlmWikiContentFingerprint','New-LlmWikiEvidenceLineage')) {
        Copy-Item (Join-Path $PSScriptRoot "$name.ps1") $tools
    }
    [IO.File]::WriteAllText((Join-Path $fixture '.gitignore'), '.artifacts/')
    [IO.File]::WriteAllText((Join-Path $fixture 'src/value.txt'), 'source')
    [IO.File]::WriteAllText((Join-Path $fixture 'src/probe.test.mjs'), "import {test} from 'node:test'; test('actual node check', () => {});")
    [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/wiki.ps1'), '[IO.File]::WriteAllText((Join-Path $PSScriptRoot "../.artifacts/check-ran"),"executed"); Write-Output passed')
    [IO.File]::WriteAllText((Join-Path $fixture '.llm-wiki/policies/change-policies.json'), '{}')
    [IO.File]::WriteAllText((Join-Path $tools 'Test-LlmWikiChangePolicy.ps1'), @'
param($BaseRef,$ChangedPath,$Format)
@{requiredChecks=@(@{id='probe';command='./.llm-wiki/wiki.ps1 probe';sourceRule='fixture'});matchedRules=@(@{id='fixture';matchedPaths=@('src/value.txt')});reviewObligations=@();violations=@()} | ConvertTo-Json -Depth 6
'@)
    & git -C $fixture add .
    & git -C $fixture -c user.name=WikiFixture -c user.email=wiki-fixture@example.invalid commit -qm initial
    if ($LASTEXITCODE -ne 0) { throw 'Evidence fixture commit failed.' }
    $evidence = @{git=@{base='HEAD';head='fixture'};change=@{changedPaths=@('src/value.txt');scopes=@();modules=@()};checks=@(@{id='probe';command='./.llm-wiki/wiki.ps1 probe';status='pending';durationSeconds=$null;reason=''});reviews=@();supplementalChecks=@();artifacts=@();structuralViolations=@()}
    $invalidPath = '.artifacts/foreign/evidence.json'
    [IO.File]::WriteAllText((Join-Path $fixture $invalidPath), ($evidence | ConvertTo-Json -Depth 8))
    $manager = Join-Path $tools 'Manage-LlmWikiEvidence.ps1'
    $rejected = $false
    try { & $manager run -Path $invalidPath -Id probe -NoExitOnFailure | Out-Null } catch { $rejected = $true }
    if (-not $rejected -or (Test-Path -LiteralPath (Join-Path $fixture '.artifacts/check-ran'))) { throw 'Invalid evidence destination was rejected only after executing the check.' }
    $validPath = '.artifacts/llm-wiki/tasks/fixture/nested/evidence.json'
    $null = New-Item -ItemType Directory -Path (Split-Path (Join-Path $fixture $validPath)) -Force
    [IO.File]::WriteAllText((Join-Path $fixture $validPath), ($evidence | ConvertTo-Json -Depth 8))
    & $manager run -Path $validPath -Id node-check -Command 'node --test src/probe.test.mjs' -NoExitOnFailure | Out-Null
    $actual = Get-Content -LiteralPath (Join-Path $fixture $validPath) -Raw | ConvertFrom-Json
    $nodeCheck = @($actual.supplementalChecks | Where-Object id -eq 'node-check')
    if ($nodeCheck.Count -ne 1 -or $nodeCheck[0].status -ne 'passed' -or $nodeCheck[0].lineage.execution.runtime -ne 'node') { throw 'Node supplemental execution did not retain actual passing runtime evidence.' }
    if (-not (Test-Path -LiteralPath (Join-Path $fixture $nodeCheck[0].lineage.artifact.path))) { throw 'Nested evidence did not preserve its execution log.' }
    & $manager run -Path $validPath -Id probe -NoExitOnFailure | Out-Null
    . (Join-Path $tools 'LlmWikiVerificationReceipts.ps1')
    $receipt = @(Get-LlmWikiVerificationReceipts $fixture | Where-Object normalizedCommand -eq './.llm-wiki/wiki.ps1 probe')
    if ($receipt.Count -ne 1 -or -not $receipt[0].validForCurrentState) { throw 'Nested required execution did not publish a current receipt.' }
    $actual = Get-Content -LiteralPath (Join-Path $fixture $validPath) -Raw | ConvertFrom-Json
    $requiredLog = Join-Path $fixture $actual.checks[0].lineage.artifact.path
    $logBytes = [IO.File]::ReadAllBytes($requiredLog)
    [IO.File]::AppendAllText($requiredLog, 'tampered')
    if (Test-LlmWikiVerificationExecutionProof $receipt[0] $fixture) { throw 'Nested receipt accepted a modified execution log.' }
    [IO.File]::WriteAllBytes($requiredLog, $logBytes)
    foreach ($invalidProofPath in @('.artifacts/llm-wiki/../foreign/evidence.json','.artifacts/llm-wiki/..\foreign/evidence.json','.artifacts/foreign/evidence.json')) {
        $receipt[0].executionProof.sourceEvidencePath = $invalidProofPath
        if (Test-LlmWikiVerificationExecutionProof $receipt[0] $fixture) { throw 'Nested receipt accepted a path outside the evidence boundary.' }
    }
    [IO.File]::Delete((Join-Path $fixture '.artifacts/check-ran'))
    $actual.checks = @()
    [IO.File]::WriteAllText((Join-Path $fixture $validPath), ($actual | ConvertTo-Json -Depth 20))
    $requiredRejected = $false
    try { & $manager run -Path $validPath -Id probe -Command './.llm-wiki/wiki.ps1 probe' -NoExitOnFailure | Out-Null } catch { $requiredRejected = $true }
    if (-not $requiredRejected -or (Test-Path -LiteralPath (Join-Path $fixture '.artifacts/check-ran'))) { throw 'A policy-required check missing from the bundle was executed as supplemental.' }
    $blocked = $false
    try { & $manager run -Path $validPath -Id injected -Command 'node --test src/probe.test.mjs; echo injected' -NoExitOnFailure | Out-Null } catch { $blocked = $_.Exception.Message -match 'allowlist' }
    if (-not $blocked) { throw 'Supplemental execution accepted shell syntax outside the allowlist.' }
    . (Join-Path $tools 'LlmWikiEvidenceCommands.ps1')
    foreach ($command in @('docker compose config --quiet','docker compose -f docker-compose.yml config -q','docker build --check .','pwsh -NoProfile -File .llm-wiki/tools/Test-Probe.ps1')) {
        if (-not (Test-LlmWikiSupplementalExecutionCommand $command)) { throw "A supported validation command was rejected: $command" }
    }
    foreach ($command in @('docker compose down','docker compose config-q','node --test tests/a.mjs | more','node --test tests/a.mjs && echo injected')) {
        if (Test-LlmWikiSupplementalExecutionCommand $command) { throw "A non-validation command was accepted: $command" }
    }
    $filteredCommand = 'dotnet test Tooling/tests/FoodDiary.Development.Mcp.Tests/FoodDiary.Development.Mcp.Tests.csproj --artifacts-path .artifacts/llm-wiki/reader-tests --filter FullyQualifiedName~SqliteWikiContextSearchTests'
    $actual.checks = @(@{id='probe';command=$filteredCommand;status='pending'})
    [IO.File]::WriteAllText((Join-Path $fixture $validPath), ($actual | ConvertTo-Json -Depth 20))
    [IO.File]::WriteAllText((Join-Path $tools 'Test-LlmWikiChangePolicy.ps1'), "param(`$BaseRef,`$ChangedPath,`$Format); @{requiredChecks=@(@{id='probe';command='$filteredCommand'})} | ConvertTo-Json -Depth 6")
    $taskRunnerAccepted = $false
    try { $null = & (Join-Path $tools 'Invoke-LlmWikiTaskChecks.ps1') -WorkspacePath '.artifacts/llm-wiki/tasks/fixture/nested' -DryRun -Format Json; $taskRunnerAccepted = $true } catch { Write-Host "Task runner diagnosis: $($_.Exception.Message)" }
    if (-not $taskRunnerAccepted) { throw 'The governed runner rejected its canonical filtered reader check.' }
    Write-Host 'Evidence execution passed: preflight, nested destinations, actual Node execution and command rejection.'
} finally {
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture)) -cne $ownedParent) { throw 'Evidence fixture escaped its owned parent.' }
    if (Test-Path -LiteralPath $fixture) { Remove-Item -LiteralPath $fixture -Recurse -Force }
}
