[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$sandbox = Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot
$fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name 'task-handoff'
$tools = Join-Path $fixture '.llm-wiki/tools'
$workspace = '.artifacts/llm-wiki/tasks/fixture'
$workspaceDirectory = Join-Path $fixture $workspace
$callLog = Join-Path $fixture 'calls.jsonl'
$assessmentTools = @('Manage-LlmWikiConfidenceLedger.ps1', 'Manage-LlmWikiChangeCritique.ps1', 'Manage-LlmWikiImpactSimulation.ps1')
$receipts = @('confidence-ledger.json', 'change-critique.json', 'impact-simulation.json')
function Write-Json([string]$Path, [object]$Value) {
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
}
function Assert-Handoff([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Get-Calls {
    if (Test-Path -LiteralPath $callLog) {
        foreach ($line in [IO.File]::ReadAllLines($callLog)) { $line | ConvertFrom-Json }
    }
}
function Invoke-Handoff([switch]$Compact, [string]$Format = 'Json', [object]$PacketInput, [object]$StatusInput, [string]$OutputPath) {
    if (Test-Path -LiteralPath $callLog) { Remove-Item -LiteralPath $callLog }
    $arguments = @{WorkspacePath=$workspace; Compact=$Compact; Format=$Format}
    foreach ($key in @('PacketInput','StatusInput','OutputPath')) {
        if ($PSBoundParameters.ContainsKey($key)) { $arguments[$key] = $PSBoundParameters[$key] }
    }
    $content = & (Join-Path $tools 'Get-LlmWikiTaskHandoff.ps1') @arguments
    if ($Format -eq 'Json' -and -not $OutputPath) { return ($content -join [Environment]::NewLine) | ConvertFrom-Json }
    return $content
}
function Assert-Rejected([scriptblock]$Action, [string]$Pattern) {
    $rejected = $false
    try { & $Action | Out-Null } catch {
        if ($_.Exception.Message -notlike $Pattern) { throw }
        $rejected = $true
    }
    Assert-Handoff $rejected "Handoff did not preserve failure: $Pattern"
}
try {
    $null = New-Item -ItemType Directory -Path $tools, $workspaceDirectory -Force
    $source = Join-Path $PSScriptRoot 'Get-LlmWikiTaskHandoff.ps1'
    Copy-Item -LiteralPath $source -Destination $tools
    $packet = @{fingerprint='current';diff=@{changedPaths=@('source.cs');scopes=@('Documentation');modules=@()};brief=@{instructions=@();contextPages=@()}}
    $status = @{verdict='blocked';score=37;risk=@{level='moderate'};blockingDimensions=@('checks');unassessedDimensions=@('critique');initialPacketFingerprint='initial';currentPacketFingerprint='current';fingerprintChanged=$true;outOfScopePaths=@();nextActions=@('Run the required check.')}
    $journal = @{entryCount=3;openCount=1;openBlockerCount=0;entries=@(
        @{id='open';status='open';type='note';text='Keep the open note.'}
        @{id='resolved';status='resolved';type='note';text='Omit the resolved note.'}
        @{id='decision';status='closed';type='decision';text='Keep the decision.'}
    )}
    $shared = @{
        valid=$true;registryFingerprint='registry';registryHash='registry';eligibleCount=0;appliedCount=0;rolledBackCount=0;duplicateCandidateCount=0
        nodes=@();edges=@();leases=@();dispatches=@();owners=@();capabilityProfiles=@();agents=@();circuits=@();experiments=@();health=@();candidates=@();memories=@()
        metrics=@{checkId='other';profiles=@();feedbackFingerprint='feedback';qualityAdjustmentFingerprint='quality';validReceiptCount=0;validQualityAdjustmentCount=0;qualityAdjustmentProfiles=@();ownerQualityProfiles=@();capabilityQualityProfiles=@()}
        summary=@{issueCount=0};slo=@{verdict='healthy';violations=@()};windowDays=7;successRatePercent=100;reconciliationRatePercent=100;heartbeatCoveragePercent=100
        bundle=@{items=@(@{path='source.cs';line=2;kind='implementation';reasons=@('Fixture source.');excerpt=@{startLine=2}})}
    }
    Write-Json (Join-Path $fixture 'packet.json') $packet
    Write-Json (Join-Path $fixture 'status.json') $status
    Write-Json (Join-Path $fixture 'journal.json') $journal
    Write-Json (Join-Path $fixture 'shared.json') $shared
    Write-Json (Join-Path $workspaceDirectory 'workspace.json') @{objective='Preserve compact handoff behavior.'}
    Write-Json (Join-Path $workspaceDirectory 'task-contract.json') @{git=@{base='fixture-base';headAtStart='fixture-head'}}
    Write-Json (Join-Path $workspaceDirectory 'acceptance-matrix.json') @{criteria=@(
        @{id='pending';text='Keep pending acceptance.';status='pending';mapping=@{scenarioIds=@();checkIds=@();reviewIds=@();testPaths=@()}}
        @{id='done';text='Omit satisfied acceptance.';status='satisfied';mapping=@{scenarioIds=@();checkIds=@();reviewIds=@();testPaths=@()}}
    )}
    Write-Json (Join-Path $workspaceDirectory 'evidence.json') @{
        checks=@(@{id='required';status='pending';command='run check';logPath=$null;reason='Required.'},@{id='done';status='passed';command='done';logPath=$null;reason='Done.'})
        reviews=@(@{id='required';status='pending';description='Review the result.';reason='Required.'},@{id='done';status='completed';description='Done.';reason='Done.'})
    }
    [IO.File]::WriteAllText((Join-Path $fixture 'source.cs'), "namespace Fixture {" + [Environment]::NewLine + 'class Sample {}' + [Environment]::NewLine + '}')
    # Control collaborators, execute the unchanged public composition script.
    $stub = @'
param([Parameter(Position=0)][string]$Action, [string]$WorkspacePath, [string]$Format, [string]$BaseRef, [string]$Objective, [object]$PacketInput, [switch]$IncludeSealed, [string]$Id)
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$name = Split-Path -Leaf $PSCommandPath
$fingerprint = if ($null -ne $PacketInput) { $PacketInput.fingerprint } else { '' }
$call = @{tool=$name;action=$Action;packetFingerprint=$fingerprint;includeSealed=[bool]$IncludeSealed}
[IO.File]::AppendAllText((Join-Path $root 'calls.jsonl'), ($call | ConvertTo-Json -Compress) + [Environment]::NewLine)
$receipt = switch ($name) {
    'Manage-LlmWikiConfidenceLedger.ps1' { 'confidence-ledger.json' }
    'Manage-LlmWikiChangeCritique.ps1' { 'change-critique.json' }
    'Manage-LlmWikiImpactSimulation.ps1' { 'impact-simulation.json' }
    'Manage-LlmWikiContextBundle.ps1' { 'context-bundle.json' }
    'Manage-LlmWikiVerificationPlan.ps1' { 'verification-plan.json' }
}
if ($receipt) {
    $path = Join-Path (Join-Path $root $WorkspacePath) $receipt
    if ((Test-Path -LiteralPath $path) -and [IO.File]::ReadAllText($path) -eq 'broken') { throw "Malformed saved $receipt" }
}
if ($Action -eq 'assess' -and $name -in @('Manage-LlmWikiConfidenceLedger.ps1','Manage-LlmWikiChangeCritique.ps1','Manage-LlmWikiImpactSimulation.ps1') -and (Test-Path (Join-Path $root 'block-assess'))) { throw 'Unexpected optional assessment.' }
if ($name -eq 'Manage-LlmWikiTaskWorkspace.ps1' -and (Test-Path (Join-Path $root 'block-status'))) { throw 'Required readiness failed.' }
$dataName = switch ($name) {
    'Get-LlmWikiChangePacket.ps1' { 'packet.json' }
    'Manage-LlmWikiTaskWorkspace.ps1' { 'status.json' }
    'Manage-LlmWikiTaskJournal.ps1' { 'journal.json' }
    default { 'shared.json' }
}
Get-Content (Join-Path $root $dataName) -Raw
'@
    $ast = [Management.Automation.Language.Parser]::ParseFile($source,[ref]$null,[ref]$null)
    $names = @($ast.FindAll({param($node)
        $node -is [Management.Automation.Language.CommandAst] -and $node.InvocationOperator -eq [Management.Automation.Language.TokenKind]::Ampersand
    },$true) | ForEach-Object {
        $match = [regex]::Match($_.CommandElements[0].Extent.Text, "'([^']+\.ps1)'")
        if ($match.Success) { $match.Groups[1].Value }
    } | Sort-Object -Unique)
    foreach ($name in $names) { [IO.File]::WriteAllText((Join-Path $tools $name),$stub,[Text.UTF8Encoding]::new($false)) }
    $full = Invoke-Handoff
    Assert-Handoff (@(Get-Calls | Where-Object { $_.tool -in $assessmentTools -and $_.action -eq 'assess' }).Count -eq 3) 'Full handoff stopped assessing missing optional artifacts.'
    $compact = Invoke-Handoff -Compact
    Assert-Handoff (@(Get-Calls | Where-Object { $_.tool -in $assessmentTools }).Count -eq 0) 'Compact handoff still synthesizes missing full-only assessments.'
    Assert-Handoff (@(Get-Calls | Where-Object { $_.tool -eq 'Manage-LlmWikiTaskWorkspace.ps1' -and $_.packetFingerprint -eq 'current' }).Count -eq 1) 'Compact handoff skipped required readiness or lost its fresh packet.'
    foreach ($key in @('objective','state','readiness','continuity','nextActions','resumeCommands')) {
        Assert-Handoff (($compact.$key | ConvertTo-Json -Depth 10 -Compress) -ceq ($full.$key | ConvertTo-Json -Depth 10 -Compress)) "Compact output changed $key."
    }
    Assert-Handoff ($compact.readiness.verdict -eq 'blocked' -and $compact.readiness.score -eq 37 -and $compact.continuity.fingerprintChanged) 'Compact handoff hid readiness or continuity drift.'
    Assert-Handoff ($compact.acceptanceCriteria.Count -eq 1 -and $compact.acceptanceCriteria[0].id -eq 'pending' -and $compact.checks.Count -eq 1 -and $compact.reviews.Count -eq 1) 'Compact handoff lost unresolved requirements.'
    Assert-Handoff (($compact.journal.entries.id -join ',') -eq 'open,decision') 'Compact handoff changed journal selection.'
    foreach ($receipt in $receipts) {
        $path = Join-Path $workspaceDirectory $receipt
        [IO.File]::WriteAllText($path,'{}')
        $null = Invoke-Handoff -Compact
        Assert-Handoff (@(Get-Calls | Where-Object { $_.tool -in $assessmentTools -and $_.action -eq 'verify' }).Count -eq 1) "Compact handoff skipped saved $receipt."
        [IO.File]::WriteAllText($path,'broken')
        Assert-Rejected { Invoke-Handoff -Compact } "*Malformed saved $receipt*"
        Remove-Item -LiteralPath $path
    }
    foreach ($receipt in $receipts + @('verification-plan.json','context-bundle.json')) { [IO.File]::WriteAllText((Join-Path $workspaceDirectory $receipt),'{}') }
    $compact = Invoke-Handoff -Compact
    Assert-Handoff (@(Get-Calls | Where-Object { $_.tool -in $assessmentTools -and $_.action -eq 'verify' }).Count -eq 3) 'Compact handoff omitted multiple saved validations.'
    Assert-Handoff ($compact.scope.sourceAnchors[0].line -eq 2 -and @($compact.resumeCommands | Where-Object {$_ -match 'task-verification-run|task-context-verify'}).Count -eq 2) 'Compact handoff lost saved context anchors or verification resume commands.'
    foreach ($receipt in @('verification-plan.json','context-bundle.json')) {
        [IO.File]::WriteAllText((Join-Path $workspaceDirectory $receipt),'broken')
        Assert-Rejected { Invoke-Handoff -Compact } "*Malformed saved $receipt*"
        [IO.File]::WriteAllText((Join-Path $workspaceDirectory $receipt),'{}')
    }
    $null = Invoke-Handoff -Compact -PacketInput $packet -StatusInput $status
    Assert-Handoff (@(Get-Calls | Where-Object {$_.tool -in @('Get-LlmWikiChangePacket.ps1','Manage-LlmWikiTaskWorkspace.ps1')}).Count -eq 0) 'Handoff stopped reusing explicitly supplied packet/status.'
    [IO.File]::WriteAllText((Join-Path $fixture 'block-status'),'')
    Assert-Rejected { Invoke-Handoff -Compact } '*Required readiness failed*'
    Remove-Item -LiteralPath (Join-Path $fixture 'block-status')
    [IO.File]::WriteAllText((Join-Path $workspaceDirectory 'completion.json'),'{}')
    Assert-Handoff ((Invoke-Handoff -Compact).state -eq 'sealed') 'Compact handoff lost sealed state.'
    Remove-Item -LiteralPath (Join-Path $workspaceDirectory 'completion.json')
    Write-Json (Join-Path $workspaceDirectory 'workspace.json') @{objective='Preserve compact handoff behavior.';decomposition=@{state='applied'}}
    Assert-Handoff ((Invoke-Handoff -Compact).state -eq 'decomposed') 'Compact handoff lost decomposed state.'
    $markdown = Invoke-Handoff -Compact -Format Markdown
    Assert-Handoff (($markdown -join [Environment]::NewLine) -match '# AI Task Handoff \(Compact\)' -and ($markdown -join [Environment]::NewLine) -match 'source.cs:2') 'Compact Markdown lost its heading or source anchor.'
    $outputPath = 'output/handoff.json'
    $null = Invoke-Handoff -Compact -OutputPath $outputPath
    Assert-Handoff ((Get-Content (Join-Path $fixture $outputPath) -Raw | ConvertFrom-Json).view -eq 'compact') 'Compact OutputPath did not publish its JSON view.'
    foreach ($receipt in $receipts) { Remove-Item -LiteralPath (Join-Path $workspaceDirectory $receipt) }
    [IO.File]::WriteAllText((Join-Path $fixture 'block-assess'),'')
    $null = Invoke-Handoff -Compact
    Assert-Rejected { Invoke-Handoff } '*Unexpected optional assessment*'
    Remove-Item -LiteralPath (Join-Path $fixture 'block-assess')
    [IO.File]::WriteAllText((Join-Path $workspaceDirectory 'evidence.json'),'broken')
    Assert-Rejected { Invoke-Handoff -Compact } '*JSON*'
    Write-Host 'Task handoff passed: compact assessment suppression, saved validation, readiness/continuity, supplied inputs, states and output formats.'
} finally {
    $target = [IO.Path]::GetFullPath($fixture)
    if ([IO.Path]::GetDirectoryName($target) -cne [IO.Path]::GetFullPath($sandbox) -or [IO.Path]::GetFileName($target) -notmatch '^task-handoff-[a-f0-9]{32}$') { throw 'Unsafe handoff fixture cleanup target.' }
    Remove-Item -LiteralPath $target -Recurse -Force
}
