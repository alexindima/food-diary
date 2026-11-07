[CmdletBinding()]
param([ValidateSet('All', 'Attention', 'Measurement')][string]$Case = 'All')
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
foreach ($scenario in $(if ($Case -eq 'All') { @('Attention', 'Measurement') } else { @($Case) })) {
    $fixture = New-LlmWikiSmokeFixtureDirectory -RepositoryRoot $repositoryRoot -Name "audit-health-$scenario" -IndependentRepository
    $tools = Join-Path $fixture '.llm-wiki/tools'
    $null = [IO.Directory]::CreateDirectory($tools)
    try {
        if ($scenario -eq 'Attention') {
            $null = [IO.Directory]::CreateDirectory((Join-Path $fixture '.artifacts/llm-wiki/tasks'))
            foreach ($name in @('Get-LlmWikiTaskAudit.ps1', 'LlmWikiGitPaths.ps1', 'LlmWikiTaskAuditHelpers.ps1')) { Copy-Item (Join-Path $PSScriptRoot $name) $tools }
            'param($Action,$Format); ''{"audit":{"staleAfterDays":7,"evidenceMaxAgeDays":7,"maximumDays":365}}''' | Set-Content (Join-Path $tools 'Get-LlmWikiWorkspacePolicy.ps1')
            $stub = @'
param($Action,$TasksPath,$AsOfUtc,$Format,[switch]$IncludeDispatchRegistry)
'{"valid":true,"issues":[],"totalCount":0,"flakyCount":0,"registryHash":"fixture","metrics":{"invalidReceiptCount":0,"invalidQualityAdjustmentCount":0},"dispatchRegistry":{"dispatches":[],"invalidCount":0},"slo":{"violationCount":0},"summary":{"issueCount":0},"agents":[],"circuits":[],"invalidReceiptCount":0,"invalidCount":0,"degradedProfileCount":0,"degradedCohortProfileCount":0,"rollbackRecommended":false,"staleCount":0,"eligibleCount":0,"approvedCount":0,"appliedCount":0,"rolledBackCount":0,"activeCount":0,"successfulCount":0,"rollbackRecommendationCount":0,"pendingCount":0,"leases":[],"nodes":[]}'
'@
            foreach ($name in @('Get-LlmWikiTaskGraph', 'Manage-LlmWikiTaskLease', 'Get-LlmWikiDispatchMetrics', 'Manage-LlmWikiAgentRegistry', 'Manage-LlmWikiWorkspaceCircuit', 'Manage-LlmWikiTaskDecomposition', 'Manage-LlmWikiContextFeedback', 'Manage-LlmWikiContextOutcome', 'Manage-LlmWikiMemory', 'Manage-LlmWikiRepairLearning', 'Manage-LlmWikiLearningPromotion', 'Manage-LlmWikiLearningExperiment', 'Manage-LlmWikiLearningHealth', 'Manage-LlmWikiEvalPromotion', 'Manage-LlmWikiVerificationTelemetry', 'Test-LlmWikiOrchestrationLineage')) {
                $content = if ($name -eq 'Manage-LlmWikiContextOutcome') { $stub.Replace('"degradedProfileCount":0', '"degradedProfileCount":1').Replace('"degradedCohortProfileCount":0', '"degradedCohortProfileCount":1').Replace('"rollbackRecommended":false', '"rollbackRecommended":true') } else { $stub }
                [IO.File]::WriteAllText((Join-Path $tools "$name.ps1"), $content)
            }
            $null = & git -C $fixture add .
            $null = & git -C $fixture -c user.name=WikiFixture -c user.email=wiki-fixture@example.invalid commit --quiet -m baseline
            if ($LASTEXITCODE) { throw 'Audit health fixture commit failed.' }
            $output = & (Get-Process -Id $PID).Path -NoProfile -File (Join-Path $tools 'Get-LlmWikiTaskAudit.ps1') -Format Json -FailOnAttention
            $code = $LASTEXITCODE
            $result = ($output -join "`n") | ConvertFrom-Json
            if ($code -eq 0 -or $result.valid -or $result.attentionCount -ne 2) { throw 'Wiki audit health: degraded outcomes must fail FailOnAttention.' }
        } else {
            Copy-Item (Join-Path $PSScriptRoot 'Measure-LlmWikiStandaloneIndexRoutes.ps1') $tools
            'param($Action,$Format); ''{}''' | Set-Content (Join-Path $tools 'Manage-LlmWikiCodeGraph.ps1')
            $generated = Join-Path $fixture '.llm-wiki/generated'
            $null = [IO.Directory]::CreateDirectory($generated)
            foreach ($name in @('runtime-topology.json', 'domain-data-index.json', 'architecture-health-index.json')) { '{}' | Set-Content (Join-Path $generated $name) }
            foreach ($name in @('Find-LlmWikiRuntimeTopology.ps1', 'Find-LlmWikiDomainData.ps1', 'Find-LlmWikiArchitectureHealth.ps1')) {
                'param($Query,$View,$Format,[switch]$IncludeDiagnostics); ''{"_freshness":{"verified":false},"records":[]}''' | Set-Content (Join-Path $tools $name)
            }
            $failure = ''
            try { $null = & (Join-Path $tools 'Measure-LlmWikiStandaloneIndexRoutes.ps1') -Iterations 2 -Format Json } catch { $failure = $_.Exception.Message }
            if ($failure -notlike '*measurement response*') { throw 'Wiki audit health: stale responses must not produce complete performance measurements.' }
        }
        Write-Host "Wiki audit health: $scenario passed."
    } finally {
        $resolvedFixture = [IO.Path]::GetFullPath($fixture)
        $sandboxPrefix = [IO.Path]::GetFullPath((Get-LlmWikiSmokeSandboxRoot -RepositoryRoot $repositoryRoot -IndependentRepository)).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
        if (-not $resolvedFixture.StartsWith($sandboxPrefix, [StringComparison]::OrdinalIgnoreCase) -or -not [IO.Path]::GetFileName($resolvedFixture).StartsWith("audit-health-$scenario-", [StringComparison]::Ordinal)) { throw 'Refusing to clean an unowned health fixture.' }
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
