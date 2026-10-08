[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot '.artifacts/llm-wiki'))
$fixtureRoot = Join-Path $artifactRoot ('dispatch-metrics-reuse-' + [Guid]::NewGuid().ToString('N'))
$fixtureTools = Join-Path $fixtureRoot '.llm-wiki/tools'
function Assert-Condition([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
try {
    New-Item -ItemType Directory -Path $fixtureTools -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Get-LlmWikiDispatchMetrics.ps1') -Destination $fixtureTools
    & (Join-Path $PSScriptRoot 'Get-LlmWikiWorkspacePolicy.ps1') get -Format Json | Set-Content (Join-Path $fixtureTools 'policy.json')
    'Get-Content (Join-Path $PSScriptRoot ''policy.json'') -Raw' | Set-Content (Join-Path $fixtureTools 'Get-LlmWikiWorkspacePolicy.ps1')
    @'
param($Action, $AsOfUtc, $Format)
Add-Content (Join-Path $PSScriptRoot 'reads.txt') 'read'
Get-Content (Join-Path $PSScriptRoot 'registry.json') -Raw
'@ | Set-Content (Join-Path $fixtureTools 'Manage-LlmWikiTaskDispatch.ps1')
    $registry = [ordered]@{
        totalCount = 2; invalidCount = 1; orphanedCount = 0; driftedCount = 0
        dispatches = @(
            @{ dispatchId = 'old-running'; state = 'running'; valid = $true; workspace = 'fixture' }
            @{ dispatchId = 'invalid'; state = 'invalid'; valid = $false; workspace = 'fixture' }
        )
    }
    $registryPath = Join-Path $fixtureTools 'registry.json'
    $registry | ConvertTo-Json -Depth 10 | Set-Content $registryPath
    $dispatchRoot = Join-Path $fixtureRoot '.artifacts/llm-wiki/scheduler/dispatches'
    New-Item -ItemType Directory -Path $dispatchRoot -Force | Out-Null
    '{"startedAtUtc":"2020-01-01T00:00:00Z"}' | Set-Content (Join-Path $dispatchRoot 'old-running.json')
    $tool = Join-Path $fixtureTools 'Get-LlmWikiDispatchMetrics.ps1'
    $arguments = @{ AsOfUtc = [DateTime]'2026-09-10T00:00:00Z'; Format = 'Json' }
    $plain = & $tool @arguments | ConvertFrom-Json
    $withRegistry = & $tool @arguments -IncludeDispatchRegistry | ConvertFrom-Json
    Assert-Condition ($null -eq $plain.PSObject.Properties['dispatchRegistry']) 'Default metrics schema changed.'
    Assert-Condition ($withRegistry.dispatchRegistry.dispatches.Count -eq 2) 'Registry lost an old or invalid dispatch outside metrics records.'
    Assert-Condition ($withRegistry.dispatchCount -eq 0 -and $withRegistry.attentionCount -eq 1) 'Metrics filtering or invalid-receipt attention changed.'
    $withRegistry.PSObject.Properties.Remove('dispatchRegistry')
    Assert-Condition (($plain | ConvertTo-Json -Depth 20 -Compress) -ceq ($withRegistry | ConvertTo-Json -Depth 20 -Compress)) 'Including the registry changed metric values.'
    Assert-Condition (@(Get-Content (Join-Path $fixtureTools 'reads.txt')).Count -eq 2) 'Each metrics invocation must validate the registry exactly once.'
    $registry.invalidCount = 2
    $registry | ConvertTo-Json -Depth 10 | Set-Content $registryPath
    $fresh = & $tool @arguments -IncludeDispatchRegistry | ConvertFrom-Json
    Assert-Condition ($fresh.dispatchRegistry.invalidCount -eq 2 -and $fresh.attentionCount -eq 2) 'A later invocation reused stale registry state.'
    # Execute the actual audit composition against owned collaborators. Invalid
    # dispatches outside metrics records must still contribute to attention.
    $auditSource = Join-Path $PSScriptRoot 'Get-LlmWikiTaskAudit.ps1'
    Copy-Item -LiteralPath $auditSource -Destination $fixtureTools
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LlmWikiTaskAuditHelpers.ps1') -Destination $fixtureTools
    $auditAst = [Management.Automation.Language.Parser]::ParseFile($auditSource, [ref]$null, [ref]$null)
    $auditTools = @($auditAst.FindAll({param($node)
        $node -is [Management.Automation.Language.CommandAst] -and $node.InvocationOperator -eq [Management.Automation.Language.TokenKind]::Ampersand
    }, $true) | ForEach-Object {
        $match = [regex]::Match($_.CommandElements[0].Extent.Text, "'([^']+\.ps1)'")
        if ($match.Success) { $match.Groups[1].Value }
    } | Sort-Object -Unique)
    $generic = [pscustomobject]@{
        valid=$true;issues=@();summary=@{issueCount=0};metrics=@{invalidReceiptCount=0;invalidQualityAdjustmentCount=0}
        invalidCount=0;invalidReceiptCount=0;degradedProfileCount=0;degradedCohortProfileCount=0;rollbackRecommended=$false
        staleCount=0;rollbackRecommendationCount=0;eligibleCount=0;approvedCount=0;appliedCount=0;rolledBackCount=0
        activeCount=0;successfulCount=0;pendingCount=0;flakyCount=0;totalCount=0;registryHash='fixture-registry'
    }
    $generic | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $fixtureTools 'generic.json')
    foreach ($name in $auditTools) {
        if ($name -in @('Get-LlmWikiWorkspacePolicy.ps1', 'Get-LlmWikiDispatchMetrics.ps1')) { continue }
        [IO.File]::WriteAllText((Join-Path $fixtureTools $name), "param(`$Action, `$AsOfUtc, `$Format) [IO.File]::ReadAllText((Join-Path `$PSScriptRoot 'generic.json'))")
    }
    [IO.File]::WriteAllText((Join-Path $fixtureTools 'LlmWikiGitPaths.ps1'), "function Invoke-LlmWikiGitCommand { param(`$RepositoryRoot, `$Arguments, `$FailureMessage) [pscustomobject]@{Lines=@('fixture-head')} }")
    $auditMetricsPath = Join-Path $fixtureTools 'Get-LlmWikiDispatchMetrics.ps1'
    $originalMetrics = [IO.File]::ReadAllText($auditMetricsPath)
    $auditMetrics = @'
param($AsOfUtc, $Format, [switch]$IncludeDispatchRegistry)
if (-not $IncludeDispatchRegistry) { throw 'Audit omitted the validated dispatch registry.' }
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'reject-metrics')) { throw 'Fixture registry validation failed.' }
[IO.File]::AppendAllText((Join-Path $PSScriptRoot 'audit-metrics-reads.txt'), "read`n")
@{windowDays=7;slo=@{violationCount=2};dispatchRegistry=@{invalidCount=1;dispatches=@()}} | ConvertTo-Json -Depth 10
'@
    [IO.File]::WriteAllText($auditMetricsPath, $auditMetrics)
    $dispatchReadsBeforeAudit = @([IO.File]::ReadAllLines((Join-Path $fixtureTools 'reads.txt'))).Count
    $audit = & (Join-Path $fixtureTools 'Get-LlmWikiTaskAudit.ps1') -TasksPath '.artifacts/llm-wiki/tasks/empty/children' -AsOfUtc $arguments.AsOfUtc -Format Json | ConvertFrom-Json
    Assert-Condition (-not $audit.valid -and $audit.invalidDispatchCount -eq 1 -and $audit.dispatchSloViolationCount -eq 2 -and $audit.attentionCount -eq 3) 'Audit lost dispatch or SLO attention while reusing metrics.'
    Assert-Condition ($null -eq $audit.dispatchMetrics.PSObject.Properties['dispatchRegistry']) 'Internal dispatch registry leaked into public audit metrics.'
    Assert-Condition (@([IO.File]::ReadAllLines((Join-Path $fixtureTools 'audit-metrics-reads.txt'))).Count -eq 1) 'Audit repeated metrics validation.'
    Assert-Condition (@([IO.File]::ReadAllLines((Join-Path $fixtureTools 'reads.txt'))).Count -eq $dispatchReadsBeforeAudit) 'Audit reread the dispatch registry outside metrics.'
    [IO.File]::WriteAllText((Join-Path $fixtureTools 'reject-metrics'), '')
    $auditRejected = $false
    try { & (Join-Path $fixtureTools 'Get-LlmWikiTaskAudit.ps1') -TasksPath '.artifacts/llm-wiki/tasks/empty/children' -Format Json | Out-Null }
    catch { $auditRejected = $_.Exception.Message -like '*Fixture registry validation failed*' }
    Assert-Condition $auditRejected 'Audit concealed a dispatch registry validation failure.'
    [IO.File]::WriteAllText($auditMetricsPath, $originalMetrics)
    # Terminal dates, not start dates or local offsets, determine the UTC daily buckets.
    $registry.dispatches = @(
        @{ dispatchId = 'before-midnight'; state = 'failed'; valid = $true }
        @{ dispatchId = 'after-midnight'; state = 'completed'; valid = $true }
    )
    $registry.invalidCount = 0
    $registry | ConvertTo-Json -Depth 10 | Set-Content $registryPath
    foreach ($sample in @(
        @{ id = 'before-midnight'; at = '2026-09-10T03:59:00+04:00'; type = 'failed' }
        @{ id = 'after-midnight'; at = '2026-09-09T20:01:00-04:00'; type = 'completed' }
    )) {
        @{
            dispatchId = $sample.id; owner = 'midnight-agent'; workspace = 'fixture'
            agentId = ''; agentCapabilities = @(); requiredCapabilities = @(); lane = 1
            startedAtUtc = '2026-09-09T23:50:00Z'
            events = @(@{ type = $sample.type; atUtc = $sample.at; details = @{ result = 'Fixture result' } })
        } | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $dispatchRoot ($sample.id + '.json'))
    }
    $midnight = & $tool -AsOfUtc ([DateTime]'2026-09-10T00:02:00Z') -Format Json | ConvertFrom-Json
    Assert-Condition ($midnight.dispatchCount -eq 2 -and @($midnight.owners).Count -eq 1) 'Midnight grouping lost dispatches or split their owner.'
    Assert-Condition (@($midnight.daily).Count -eq 2) 'Terminal events across UTC midnight must produce two daily buckets.'
    Assert-Condition ($midnight.daily[0].date -eq '2026-09-09' -and $midnight.daily[0].failedCount -eq 1 -and $midnight.daily[0].completedCount -eq 0 -and $midnight.daily[0].terminalCount -eq 1) 'Previous UTC day has incorrect terminal outcomes.'
    Assert-Condition ($midnight.daily[1].date -eq '2026-09-10' -and $midnight.daily[1].completedCount -eq 1 -and $midnight.daily[1].failedCount -eq 0 -and $midnight.daily[1].terminalCount -eq 1) 'Next UTC day has incorrect terminal outcomes.'
    $scheduler = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Get-LlmWikiTaskSchedule.ps1'))
    Assert-Condition ($scheduler.Contains('-IncludeDispatchRegistry') -and -not $scheduler.Contains("'Manage-LlmWikiTaskDispatch.ps1'")) 'Scheduler must consume the full validated registry without a second scan.'
    # Exercise the actual dispatch list/exit contract with controlled read-only collaborators.
    $driftRoot = Join-Path $fixtureRoot 'dispatch-drift'
    $driftTools = Join-Path $driftRoot '.llm-wiki/tools'
    $driftReceipts = Join-Path $driftRoot '.artifacts/llm-wiki/scheduler/dispatches'
    $workspace = '.artifacts/llm-wiki/tasks/fixture'
    $driftWorkspace = Join-Path $driftRoot $workspace
    $null = New-Item -ItemType Directory -Path $driftTools, $driftReceipts, $driftWorkspace -Force
    $dispatchSource = Join-Path $PSScriptRoot 'Manage-LlmWikiTaskDispatch.ps1'
    Copy-Item -LiteralPath $dispatchSource -Destination $driftTools
    'param($Action, $Format); ''{}''' | Set-Content (Join-Path $driftTools 'Get-LlmWikiWorkspacePolicy.ps1')
    'param($Action, $WorkspacePath, $Format); ''{"valid":true}''' | Set-Content (Join-Path $driftTools 'Manage-LlmWikiContextBundle.ps1')
    'param($Action, $AsOfUtc, $Format); ''{"leases":[{"active":true,"leaseId":"fixture-lease"}]}''' | Set-Content (Join-Path $driftTools 'Manage-LlmWikiTaskLease.ps1')
    $dispatchAst = [Management.Automation.Language.Parser]::ParseFile($dispatchSource, [ref]$null, [ref]$null)
    $receipt = & {
        foreach ($name in @('Get-Hash', 'New-Event')) {
            $definition = $dispatchAst.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true)
            if ($null -eq $definition) { throw "Dispatch fixture could not find actual function $name." }
            . ([scriptblock]::Create($definition.Extent.Text))
        }
        $now = ([DateTime]'2026-10-07T00:00:00Z').ToUniversalTime()
        [pscustomobject][ordered]@{
            schemaVersion = 1; dispatchId = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'; workspace = $workspace
            owner = 'fixture'; leaseId = 'fixture-lease'; schedulePlanId = ''; schedulePlanHash = ''; scheduleClaimId = ''
            contextBundlePath = "$workspace/context-bundle.json"; contextBundleHash = ('b' * 64)
            packetFingerprint = ('c' * 64); events = @(New-Event @() 'started' ([pscustomobject]@{ result = 'fixture' }))
        }
    }
    $receiptPath = Join-Path $driftReceipts "$($receipt.dispatchId).json"
    $receipt | ConvertTo-Json -Depth 20 | Set-Content $receiptPath
    foreach ($state in @('running', 'packet-drift', 'context-drift')) {
        @{ currentPacketFingerprint = $(if ($state -eq 'packet-drift') { 'd' * 64 } else { 'c' * 64 }) } |
            ConvertTo-Json | Set-Content (Join-Path $driftWorkspace 'workspace.json')
        @{ bundleHash = $(if ($state -eq 'context-drift') { 'a' * 64 } else { 'b' * 64 }) } |
            ConvertTo-Json | Set-Content (Join-Path $driftWorkspace 'context-bundle.json')
        $dispatchOutput = & (Get-Process -Id $PID).Path -NoLogo -NoProfile -File (Join-Path $driftTools 'Manage-LlmWikiTaskDispatch.ps1') -Action list -FailOnInvalid -Format Json
        $dispatchExit = $LASTEXITCODE
        $dispatchResult = $dispatchOutput -join "`n" | ConvertFrom-Json
        Assert-Condition ($dispatchResult.totalCount -eq 1 -and $dispatchResult.dispatches[0].state -ceq $state) "Actual dispatch fixture returned $($dispatchResult.dispatches[0].state) for ${state}: $(@($dispatchResult.dispatches[0].issues) -join ' ')"
        $expectedDrift = [int]($state -ne 'running')
        Assert-Condition ($dispatchResult.driftedCount -eq $expectedDrift -and $dispatchExit -eq $expectedDrift) "Dispatch list did not count or reject $state consistently."
    }
    $receipt.events[0].atUtc = ''
    $receipt | ConvertTo-Json -Depth 20 | Set-Content $receiptPath
    $malformedOutput = & (Get-Process -Id $PID).Path -NoLogo -NoProfile -File (Join-Path $driftTools 'Manage-LlmWikiTaskDispatch.ps1') -Action list -FailOnInvalid -Format Json
    $malformedExit = $LASTEXITCODE
    $malformed = $malformedOutput -join "`n" | ConvertFrom-Json
    Assert-Condition ($malformed.invalidCount -eq 1 -and $malformedExit -eq 1 -and @($malformed.dispatches[0].issues | Where-Object { $_ -match 'timestamp is invalid' }).Count -eq 1) 'Malformed event timestamp did not produce an invalid receipt and failing exit.'
    $global:LASTEXITCODE = 0
    Write-Host 'Dispatch metrics reuse passed: unchanged metrics, invalid and out-of-window dispatches retained, one fresh validation per invocation.'
} finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolvedFixture.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture cleanup escaped its artifact root.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
