[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'LlmWikiPlanReview.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiRoleAssignment.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiEnvironmentDiagnostics.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
function Assert-Improvement([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'workflow-improvements' -IndependentRepository
$ownedParent = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($fixture))
try {
    $null = New-Item -ItemType Directory -Path (Join-Path $fixture '.llm-wiki/tools'),(Join-Path $fixture '.llm-wiki/evals'),(Join-Path $fixture 'docs/ai'),(Join-Path $fixture '.artifacts') -Force
    [IO.File]::WriteAllText((Join-Path $fixture 'source.txt'), 'owner and consumer')
    $plan = [pscustomobject]@{ objective = 'Preserve a working consumer'; phases = @([pscustomobject]@{ id = 'implementation'; files = @('source.txt') },[pscustomobject]@{ id = 'focused-verification'; files = @('consumer.test') }) }
    $behavior = [pscustomobject]@{ acceptanceCriteria = @('The consumer reads the new shape.', 'Malformed responses fail explicitly.') }
    function Read-Plan([object]$Review = $null, [bool]$Required = $true) { Get-LlmWikiPlanReview -Plan $plan -BehaviorSpecification $behavior -SourcePath 'source.txt' -RepositoryRoot $fixture -ReviewContent $Review -Required $Required }
    $initial = Read-Plan
    Assert-Improvement (-not $initial.valid -and $initial.status -eq 'needs-review') 'Unreviewed plan was ready.'
    Assert-Improvement ((Read-Plan -Required $false).status -eq 'not-required') 'Bounded work gained mandatory review.'
    Assert-Improvement (-not (Read-Plan ([pscustomobject]@{})).valid) 'Empty review was accepted.'
    $review = [pscustomobject]@{
        planFingerprint = $initial.planFingerprint
        outcomes = @($behavior.acceptanceCriteria | ForEach-Object { [pscustomobject]@{ criterion = $_; phaseIds = @('implementation', 'focused-verification'); verification = 'Run consumer behavior test with valid and malformed responses.'; reasoning = 'The current consumer and error path are covered by the named phases.' } })
        perspectives = @($initial.perspectives | ForEach-Object { [pscustomobject]@{ id = $_.id; status = 'covered'; reasoning = 'source.txt: owner and consumer migrate together; consumer.test verifies both response paths in the declared order.' } })
        findings = @()
    }
    Assert-Improvement ((Read-Plan $review).valid) 'Complete review was rejected.'
    $review.perspectives[1].status = 'gap'
    Assert-Improvement (-not (Read-Plan $review).valid) 'Missing consumer wiring was accepted.'
    $review.perspectives[1].status = 'covered'
    $saved = $review.outcomes
    $review.outcomes = @($saved[0])
    Assert-Improvement (-not (Read-Plan $review).valid) 'Missing outcome was accepted.'
    $review.outcomes = $saved
    $review.outcomes[0].phaseIds = @('invented-phase')
    Assert-Improvement (-not (Read-Plan $review).valid) 'Unknown phase was accepted.'
    $review.outcomes[0].phaseIds = @('implementation', 'focused-verification')
    [IO.File]::WriteAllText((Join-Path $fixture 'source.txt'), 'changed owner')
    Assert-Improvement (-not (Read-Plan $review).valid) 'Source drift preserved a plan review.'

    $bundle = [pscustomobject]@{ workspace = '.artifacts/task'; query = [pscustomobject]@{ objective = 'Verify consumer behavior' }; bundleHash = 'verified-bundle'; packetFingerprint = 'packet'; budgets = [pscustomobject]@{ usedCharacters = 200 }; items = @([pscustomobject]@{ path = 'test.txt'; kind = 'test'; required = $false; score = 60; excerpt = '[REDACTED]' },[pscustomobject]@{ path = 'AGENTS.md'; kind = 'guide'; required = $true; score = 120; excerpt = 'instructions' },[pscustomobject]@{ path = 'wiki.md'; kind = 'wiki'; required = $false; score = 80; excerpt = 'flow' }); memories = @(); omitted = @('other.txt') }
    $researcher = Get-LlmWikiRoleAssignment -Bundle $bundle -Role researcher
    $reviewer = Get-LlmWikiRoleAssignment -Bundle $bundle -Role reviewer
    Assert-Improvement ($researcher.items[0].path -eq 'AGENTS.md' -and $reviewer.items[0].path -eq 'AGENTS.md') 'Role dropped or deprioritized mandatory instructions.'
    Assert-Improvement ($researcher.items[1].path -eq 'wiki.md' -and $reviewer.items[1].path -eq 'test.txt') 'Roles did not rank their relevant context.'
    Assert-Improvement ($reviewer.items[1].excerpt -eq '[REDACTED]' -and $reviewer.items.Count -eq $bundle.items.Count -and $reviewer.bundleHash -eq $bundle.bundleHash) 'Role changed redaction, budget selection or bundle identity.'
    Assert-Improvement ($researcher.assignmentHash -ne $reviewer.assignmentHash) 'Role is absent from the assignment binding.'
    # Exercise the real bundle command. Only retrieval and unrelated registries
    # are stubbed; creation, hashing, source validation and role gating are real.
    $fixtureTools = Join-Path $fixture '.llm-wiki/tools'
    foreach ($name in @('Manage-LlmWikiContextBundle', 'LlmWikiRoleAssignment')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot "$name.ps1") -Destination $fixtureTools }
    $null = New-Item -ItemType Directory -Path (Join-Path $fixture '.llm-wiki/policies'),(Join-Path $fixture '.artifacts/llm-wiki/tasks/role-test') -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../policies/workspace-policies.json') -Destination (Join-Path $fixture '.llm-wiki/policies')
    [IO.File]::WriteAllText((Join-Path $fixture 'AGENTS.md'), 'Fixture instructions')
    [IO.File]::WriteAllText((Join-Path $fixture '.artifacts/llm-wiki/tasks/role-test/workspace.json'), '{"objective":"Inspect source.txt","currentPacketFingerprint":"fixture-packet"}')
    [IO.File]::WriteAllText((Join-Path $fixture '.artifacts/llm-wiki/tasks/role-test/change-packet.json'), '{"diff":{"modules":[],"scopes":[],"changedPaths":["source.txt"]},"brief":{"instructions":[],"contextPages":[],"focusedTests":[]}}')
    $stubResult = '{"valid":true,"registryFingerprint":"fixture","memories":[],"candidates":[],"experiments":[],"metrics":{"profiles":[],"feedbackFingerprint":"fixture","validReceiptCount":0},"assessment":{"assessmentHash":"fixture","scannerFingerprint":"fixture","sources":[],"summary":{"findingCount":0,"trustedInstructionCount":1,"untrustedSourceCount":1}}}'
    foreach ($name in @('Find-LlmWikiContext', 'Manage-LlmWikiContextFeedback', 'Manage-LlmWikiMemory', 'Manage-LlmWikiLearningPromotion', 'Manage-LlmWikiLearningExperiment', 'Manage-LlmWikiContextSecurity')) {
        $resultJson = if ($name -eq 'Find-LlmWikiContext') { '{"agentGuides":[],"wikiPages":[],"symbols":[],"frontendSymbols":[],"tests":[]}' } else { $stubResult }
        [IO.File]::WriteAllText((Join-Path $fixtureTools "$name.ps1"), "'$resultJson'")
    }
    $created = & (Join-Path $fixtureTools 'Manage-LlmWikiContextBundle.ps1') create -WorkspacePath '.artifacts/llm-wiki/tasks/role-test' -Role implementer -Format Json | ConvertFrom-Json
    Assert-Improvement ($created.valid -and $created.assignment.bundleHash -eq $created.bundle.bundleHash) 'Real bundle creation failed to prepare a validated role assignment.'
    [IO.File]::WriteAllText((Join-Path $fixture 'source.txt'), 'source drift after bundle creation')
    $stale = & (Join-Path $fixtureTools 'Manage-LlmWikiContextBundle.ps1') show -WorkspacePath '.artifacts/llm-wiki/tasks/role-test' -Role reviewer -Format Json | ConvertFrom-Json
    Assert-Improvement (-not $stale.valid -and $null -eq $stale.assignment) 'A stale real bundle produced a role assignment.'
    $environment = Get-LlmWikiEnvironmentDiagnostics -RepositoryRoot $fixture
    Assert-Improvement (@($environment.storage).Count -eq 3 -and @($environment.runtimes).Count -eq 3 -and $environment.durationMs -ge 0) 'Environment observations are incomplete.'
    Assert-Improvement ($environment.cache.sizeStatus -eq 'not-measured' -and -not (Test-Path (Join-Path $fixture '.artifacts/llm-wiki/read-only-snapshots'))) 'Read-only health invented cache size or wrote cache state.'
    $originalPath = $env:PATH
    try {
        $env:PATH = ''
        $blocked = Get-LlmWikiEnvironmentDiagnostics -RepositoryRoot $fixture
        Assert-Improvement (-not $blocked.valid -and @($blocked.issues | Where-Object kind -eq 'runtime').Count -eq 3) 'Unavailable runtime prerequisites were presented as healthy.'
    } finally { $env:PATH = $originalPath }

    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Invoke-LlmWikiBehavioralEvaluation.ps1') -Destination (Join-Path $fixture '.llm-wiki/tools')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../evals/behavioral-instructions.json') -Destination (Join-Path $fixture '.llm-wiki/evals')
    foreach ($name in @('AI_WORKFLOW', 'CODE_REVIEW')) { [IO.File]::WriteAllText((Join-Path $fixture "docs/ai/$name.md"), 'Synthetic instruction fixture; never a model measurement.') }
    function Read-Eval([string]$Request) {
        $arguments = @{ Format = 'Json' }
        if ($Request) { $arguments.EvaluationPath = $Request }
        & (Join-Path $fixture '.llm-wiki/tools/Invoke-LlmWikiBehavioralEvaluation.ps1') @arguments | ConvertFrom-Json
    }
    $empty = Read-Eval
    Assert-Improvement ($empty.status -eq 'not-run' -and $empty.runs.Count -eq 0) 'No model execution was reported as a behavioral pass.'
    $runs = @(foreach ($case in $empty.cases) {
        foreach ($condition in @('baseline', 'guided')) {
            $transcriptPath = ".artifacts/$($case.id)-$condition.txt"
            [IO.File]::WriteAllText((Join-Path $fixture $transcriptPath), 'SYNTHETIC TEST FIXTURE. I inspected source, found the consumer gap, and retained execution evidence.')
            [pscustomobject]@{ caseId = $case.id; trial = 1; condition = $condition; model = 'fixture-only'; settings = [pscustomobject]@{ temperature = 0 }; transcriptPath = $transcriptPath; transcriptSha256 = (Get-FileHash -LiteralPath (Join-Path $fixture $transcriptPath) -Algorithm SHA256).Hash.ToLowerInvariant(); instructions = @(if ($condition -eq 'guided') { $empty.instructions }); review = [pscustomobject]@{ reviewer = 'synthetic-fixture-review'; assessments = @($case.rubric | ForEach-Object { [pscustomobject]@{ id = $_.id; verdict = 'passed'; reason = 'Validator fixture only, not a semantic model judgment.'; evidenceQuote = 'I inspected source' } }) } }
        }
    })
    $request = [pscustomobject]@{ corpusSha256 = $empty.corpusSha256; runs = $runs }
    function Write-Request { [IO.File]::WriteAllText((Join-Path $fixture '.artifacts/request.json'), ($request | ConvertTo-Json -Depth 20)) }
    Write-Request
    $observed = Read-Eval '.artifacts/request.json'
    Assert-Improvement ($observed.valid -and $observed.status -eq 'observed' -and $observed.comparablePairCount -eq 5) 'Complete paired evidence structure was rejected.'
    $transcriptAbsolute = Join-Path $fixture $runs[0].transcriptPath
    $originalTranscript = [IO.File]::ReadAllText($transcriptAbsolute)
    [IO.File]::WriteAllText($transcriptAbsolute, $originalTranscript + ' edited after review')
    Assert-Improvement (-not (Read-Eval '.artifacts/request.json').valid) 'Edited transcripts preserved behavioral evidence.'
    [IO.File]::WriteAllText($transcriptAbsolute, $originalTranscript)
    $runs[0].review.assessments[0].evidenceQuote = 'invented evidence'
    Write-Request
    Assert-Improvement (-not (Read-Eval '.artifacts/request.json').valid) 'A review with invented transcript anchors was accepted.'
    $runs[0].review.assessments[0].evidenceQuote = 'I inspected source'
    $runs[0].model = 'other-model'
    Write-Request
    Assert-Improvement ((Read-Eval '.artifacts/request.json').status -eq 'insufficient-data') 'Different models were compared as an instruction effect.'
    $runs[0].model = 'fixture-only'
    $runs[0].review.assessments = @($runs[0].review.assessments[0])
    Write-Request
    Assert-Improvement (-not (Read-Eval '.artifacts/request.json').valid) 'Incomplete rubric judgments were accepted.'
    [IO.File]::WriteAllText((Join-Path $fixture 'docs/ai/AI_WORKFLOW.md'), 'Changed instructions')
    Assert-Improvement (-not (Read-Eval '.artifacts/request.json').valid) 'Instruction drift preserved guided evaluation.'
    Write-Host 'Workflow improvement contracts passed: plan gaps/drift, bounded role views, read-only diagnostics and behavioral evidence validation. Synthetic fixtures are not model evaluation results.'
} finally {
    $resolved = [IO.Path]::GetFullPath($fixture)
    if ([IO.Path]::GetDirectoryName($resolved) -cne $ownedParent) { throw 'Fixture cleanup escaped its owned parent.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
