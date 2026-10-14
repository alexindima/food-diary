[CmdletBinding()]
param([ValidateSet('Pure', 'Integration', 'Routing', 'All')][string]$Group = 'All')
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiEvidenceCommands.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiNavigationSemantics.ps1')
. (Join-Path $PSScriptRoot 'LlmWikiReportFreshness.ps1')
function Assert-Usability([bool]$Valid, [string]$Message) { if (-not $Valid) { throw $Message } }

if ($Group -in @('Pure', 'All')) {
    $required = 'dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj'
    Assert-Usability (Test-LlmWikiEvidenceExecutionCommand $required "$required --artifacts-path .artifacts/test --logger trx --no-restore" $repositoryRoot) 'Output-only flags changed the required check identity.'
    Assert-Usability (-not (Test-LlmWikiEvidenceExecutionCommand $required "$required --filter SomeSubset" $repositoryRoot)) 'A narrowed test filter was accepted as full required verification.'
    Assert-Usability (-not (Test-LlmWikiEvidenceExecutionCommand $required 'dotnet test Other.Tests.csproj' $repositoryRoot)) 'A different project satisfied the required check.'
    Assert-Usability (-not (Test-LlmWikiEvidenceExecutionCommand $required "$required; exit 0" $repositoryRoot)) 'A shell chain was accepted as the required command.'
    $before = '{"schemaVersion":1,"generator":".llm-wiki/tools/Build-LlmWikiFrontendIndex.ps1","symbols":[{"path":"src/a.ts","line":3,"selector":"fd-a"},{"path":"src/b.ts","line":8,"selector":"fd-b"}]}'
    $path = '.llm-wiki/generated/frontend-index.json'
    Assert-Usability (Test-LlmWikiNavigationCoordinatesOnly $path $before ($before.Replace('"line":3', '"line":30'))) 'A source coordinate move required semantic review.'
    Assert-Usability (-not (Test-LlmWikiNavigationCoordinatesOnly $path $before ($before.Replace('fd-a', 'fd-changed')))) 'A component contract change was dismissed as coordinates.'
    Assert-Usability (-not (Test-LlmWikiNavigationCoordinatesOnly $path $before ($before.Replace('src/a.ts', 'src/c.ts')))) 'A source relocation was dismissed as coordinates.'
    Assert-Usability (-not (Test-LlmWikiNavigationCoordinatesOnly '.llm-wiki/generated/frontend-contract-index.json' $before $before)) 'An unclassified index bypassed semantic review.'
    Assert-Usability (-not (Test-LlmWikiNavigationCoordinatesOnly $path $before '{}')) 'Missing schema metadata bypassed review.'
    $directory = Join-Path $repositoryRoot ('.artifacts/llm-wiki/report-fixture-' + [guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($directory)
    $input = Join-Path $directory 'evidence.json'
    [IO.File]::WriteAllText($input, '{}')
    $metadata = @{ state = 'assessment-snapshot'; assessedAtUtc = [DateTime]::UtcNow.ToString('o'); packetFingerprint = ('a' * 64); artifactHashes = @(@{ path = $input; sha256 = (Get-FileHash -LiteralPath $input).Hash.ToLowerInvariant() }) }
    $report = Join-Path $directory 'review-report.md'
    [IO.File]::WriteAllText($report, '<!-- LLM_WIKI_REPORT_FRESHNESS: ' + ($metadata | ConvertTo-Json -Depth 8 -Compress) + " -->`r`n**READINESS: BLOCKED**")
    $relativeReport = $report.Substring($repositoryRoot.Length + 1).Replace('\', '/')
    Assert-Usability ((Get-LlmWikiReportFreshness $repositoryRoot $relativeReport ('a' * 64)).state -eq 'current') 'A current CRLF report was not recognized.'
    Assert-Usability ((Get-LlmWikiReportFreshness $repositoryRoot $relativeReport ('b' * 64)).state -eq 'stale') 'A changed packet left the report marked current.'
    [IO.File]::WriteAllText($input, '{"checks":[]}')
    Assert-Usability ((Get-LlmWikiReportFreshness $repositoryRoot $relativeReport ('a' * 64)).state -eq 'stale') 'Changed evidence left a saved verdict marked current.'
}
if ($Group -in @('Integration', 'All')) {
    $fixture = '.artifacts/llm-wiki/usability-' + [guid]::NewGuid().ToString('N') + '.json'
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') init -Path $fixture -ChangedPath '.llm-wiki/tools/Get-LlmWikiCoveragePlan.ps1' | Out-Null
    & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') check -Path $fixture -Id supplemental-probe -Status passed -Command 'node --version' -Reason 'Supplemental fixture observation' | Out-Null
    $evidence = Get-Content -LiteralPath (Join-Path $repositoryRoot $fixture) -Raw | ConvertFrom-Json
    Assert-Usability (@($evidence.supplementalChecks).Count -eq 1) 'Supplemental evidence was not retained.'
    Assert-Usability (@($evidence.checks | Where-Object status -ne pending).Count -eq 0) 'Supplemental evidence satisfied a required gate.'
    $lineage = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') -EvidencePath $fixture -Format Json | ConvertFrom-Json
    Assert-Usability $lineage.valid 'Supplemental evidence failed lineage validation.'
    $canonical = @($evidence.checks | Select-Object -First 1)[0]
    $hashBefore = (Get-FileHash -LiteralPath (Join-Path $repositoryRoot $fixture)).Hash
    $rejected = $false
    try { & (Join-Path $PSScriptRoot 'Manage-LlmWikiEvidence.ps1') check -Path $fixture -Id $canonical.id -Status passed -Command 'echo unrelated' | Out-Null } catch { $rejected = $true }
    Assert-Usability $rejected 'An unrelated command overwrote a required definition.'
    Assert-Usability ((Get-FileHash -LiteralPath (Join-Path $repositoryRoot $fixture)).Hash -ceq $hashBefore) 'Rejected evidence changed the persisted bundle.'
    $evidence.supplementalChecks[0].lineage.execution.command = 'tampered'
    [IO.File]::WriteAllText((Join-Path $repositoryRoot $fixture), ($evidence | ConvertTo-Json -Depth 30))
    $tampered = & (Join-Path $PSScriptRoot 'Test-LlmWikiEvidenceLineage.ps1') -EvidencePath $fixture -Format Json | ConvertFrom-Json
    Assert-Usability (-not $tampered.valid) 'Tampered supplemental execution metadata was accepted.'
}
if ($Group -in @('Routing', 'All')) {
    $paths = @('FoodDiary.Web.Client/src/app/features/dashboard/lib/dashboard.facade.spec.ts', 'FoodDiary.Web.Client/src/app/features/recipes/recipes.routes.ts')
    foreach ($intent in @('Capitalize the first letter of ingredient display names without changing API data.', 'Сделать первую букву ингредиентов заглавной в отображении.')) {
        $route = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') -Objective $intent -ProposedPath $paths -Format Json | ConvertFrom-Json
        Assert-Usability ($route.profile -eq 'visual-ui-change' -and $route.workflowLevel -eq 'small') 'Bounded ingredient display formatting retained feature ceremony.'
    }
    $boundary = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') -Objective 'Capitalize display names and persist data using a new API contract.' -ProposedPath $paths -Format Json | ConvertFrom-Json
    Assert-Usability ($boundary.profile -ne 'visual-ui-change') 'API/data mutation was downgraded to a visual-only route.'
    $calculation = & (Join-Path $PSScriptRoot 'Get-LlmWikiAdaptiveWorkflow.ps1') -Objective 'Capitalize ingredient display names and recalculate calories.' -ProposedPath $paths -Format Json | ConvertFrom-Json
    Assert-Usability ($calculation.profile -ne 'visual-ui-change') 'Nutrition calculations were downgraded to a visual-only route.'
}
Write-Host "Wiki usability regressions passed: $Group."
