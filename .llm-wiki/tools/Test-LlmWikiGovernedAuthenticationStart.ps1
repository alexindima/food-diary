[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'LlmWikiSmokeSandbox.ps1')
$workspacePath = New-LlmWikiSmokeFixtureRepositoryPath -RepositoryRoot $repositoryRoot -Name 'authentication-start'
$workspaceAbsolute = Join-Path $repositoryRoot $workspacePath
$paths = @(
    'FoodDiary.Application/Authentication/Commands/LinkGoogle/LinkGoogleCommandHandler.cs'
    'FoodDiary.Application/Authentication/Commands/LinkTelegram/LinkTelegramCommandHandler.cs'
    'FoodDiary.Application/Authentication/Commands/ResendEmailVerification/ResendEmailVerificationCommandHandler.cs'
)
$criteria = @(
    'Google linking preserves email mismatch, same identity, different identity, already-owned identity, and successful link outcomes without exposing User to Authentication.'
    'Telegram linking preserves replay protection, already-linked identity, already-owned identity, and successful link outcomes without exposing User to Authentication.'
    'Email verification resend preserves confirmed-user no-op, cooldown failure, token persistence, and email delivery fields without exposing User to Authentication.'
    'Architecture tests reject direct User aggregate access from the migrated Authentication handlers.'
)
function Write-Json([string]$Path, [object]$Value) {
    [IO.File]::WriteAllText($Path, (($Value | ConvertTo-Json -Depth 50) + [Environment]::NewLine), [Text.UTF8Encoding]::new($false))
}
try {
    $design = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') design `
        -Objective 'Replace direct User aggregate mutation in critical Authentication handlers with narrow Users-owned capabilities' `
        -PlannedPath $paths `
        -Format Json | ConvertFrom-Json
    if ($null -eq $design) { throw 'Design did not return a checkpoint result.' }

    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-start `
        -Objective 'Replace direct User aggregate mutation in critical Authentication handlers with narrow Users-owned capabilities' `
        -Criterion $criteria `
        -PlannedPath $paths `
        -AllowedPath @(
            '^FoodDiary\.Application/(?:Authentication|Users)/|^FoodDiary\.Application\.Abstractions/Users/|^tests/FoodDiary\.(?:Application|Architecture)Tests/'
            '^\.llm-wiki/'
        ) `
        -WorkspacePath $workspacePath
    if (-not (Test-Path -LiteralPath (Join-Path $workspaceAbsolute 'workspace.json') -PathType Leaf)) { throw 'Governed task-start did not create a workspace.' }
    $startManifest = Get-Content -LiteralPath (Join-Path $workspaceAbsolute 'change-manifest.json') -Raw | ConvertFrom-Json
    if (@($startManifest.scope.plannedPaths).Count -ne $paths.Count -or @($paths | Where-Object { $_ -notin @($startManifest.scope.plannedPaths) }).Count -gt 0) {
        throw "Governed start did not preserve its PlannedPath list: $(@($startManifest.scope.plannedPaths) -join ', ')."
    }
    $nextText = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') next -WorkspacePath $workspacePath 6>&1 | Out-String
    if ($nextText -notmatch 'Wiki next:.*governed-workspace' -or $nextText -match "property 'profile'") {
        throw "Governed wiki next did not render its schema safely: $nextText"
    }

    $baseArtifactNames = @('workspace.json', 'task-contract.json', 'change-manifest.json', 'acceptance-matrix.json', 'evidence.json')
    $pinnedBase = ''
    foreach ($name in $baseArtifactNames) {
        $path = Join-Path $workspaceAbsolute $name
        $artifact = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
        if ([string]$artifact.git.base -notmatch '^[a-f0-9]{40}$') { throw "New workspace retained a symbolic Git base in $name." }
        if ([string]::IsNullOrWhiteSpace($pinnedBase)) { $pinnedBase = [string]$artifact.git.base }
        elseif ([string]$artifact.git.base -cne $pinnedBase) { throw 'New workspace artifacts disagree on the pinned Git base.' }
        $artifact.git.base = 'HEAD'
        if ($name -eq 'acceptance-matrix.json') { $artifact.packetFingerprint = ('0' * 64) }
        Write-Json $path $artifact
    }
    $legacyDoctor = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-doctor `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if (-not $legacyDoctor.migrationRequired) { throw 'Task doctor did not request migration for symbolic legacy bases.' }
    $migrationPlan = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-migrate `
        -WorkspacePath $workspacePath `
        -DryRun `
        -Format Json | ConvertFrom-Json
    if (-not $migrationPlan.migrationRequired -or $migrationPlan.changed) { throw 'Legacy base migration dry run was not read-only.' }
    $migration = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-migrate `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if (-not $migration.changed) { throw 'Legacy symbolic Git base was not migrated.' }
    foreach ($name in $baseArtifactNames) {
        $artifact = Get-Content -LiteralPath (Join-Path $workspaceAbsolute $name) -Raw | ConvertFrom-Json
        if ([string]$artifact.git.base -cne $pinnedBase) { throw "Migration did not restore the initial SHA in $name." }
    }
    $migratedDescriptor = Get-Content -LiteralPath (Join-Path $workspaceAbsolute 'workspace.json') -Raw | ConvertFrom-Json
    $migratedAcceptance = Get-Content -LiteralPath (Join-Path $workspaceAbsolute 'acceptance-matrix.json') -Raw | ConvertFrom-Json
    if ([string]$migratedAcceptance.packetFingerprint -cne [string]$migratedDescriptor.initialPacketFingerprint) {
        throw 'Migration did not restore the acceptance origin fingerprint.'
    }

    $initialAssessment = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-requirements-assess `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if ($initialAssessment.valid -or @($initialAssessment.model.findings | Where-Object id -eq 'criterion-compound').Count -lt 3) {
        throw 'Requirement assessment did not reject the compound Authentication criteria.'
    }

    $expansion = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-requirements-expand `
        -WorkspacePath $workspacePath `
        -Reason 'Split compound outcomes and add atomic elevated-risk requirements.' `
        -Format Json | ConvertFrom-Json
    if (-not $expansion.valid -or $expansion.addedCount -le 0) {
        throw "Requirement expansion did not produce a valid atomic model: $(@($expansion.model.findings | ForEach-Object { "$($_.criterionId):$($_.id)" }) -join ', ')"
    }

    $assessment = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-requirements-assess `
        -WorkspacePath $workspacePath `
        -FailOnInvalid `
        -Format Json | ConvertFrom-Json
    if (-not $assessment.valid) { throw "Governed requirements assessment failed: $(@($assessment.model.findings | ForEach-Object { "$($_.criterionId):$($_.id)" }) -join ', ')" }
    if (@($assessment.model.classification.criteria | Where-Object { -not $_.atomic }).Count -ne 0) { throw 'Requirement expansion retained compound criteria.' }

    $deliveryStatus = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') delivery-status `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if ($deliveryStatus.assessment.objective -cne 'Replace direct User aggregate mutation in critical Authentication handlers with narrow Users-owned capabilities') {
        throw 'Delivery workflow did not read the objective from the current change-packet schema.'
    }

    $acceptancePath = Join-Path $workspaceAbsolute 'acceptance-matrix.json'
    $acceptance = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    $availableScenarioIds = @($acceptance.availableEvidence.scenarios | ForEach-Object { if ($_.PSObject.Properties['id']) { [string]$_.id } } | Where-Object { $_ })
    if ($availableScenarioIds.Count -eq 0) { throw 'Authentication task did not expose any journey as acceptance evidence.' }
    $scenarioId = if ('FD-AUTH' -in $availableScenarioIds) { 'FD-AUTH' } else { [string]$availableScenarioIds[0] }
    $acceptanceRaw = Get-Content -LiteralPath $acceptancePath -Raw
    $acceptance.availableEvidence.scenarios = @()
    $acceptance.availableEvidence.checks = @()
    $acceptance.availableEvidence.reviews = @()
    $acceptance.availableEvidence.changedPaths = @()
    Write-Json $acceptancePath $acceptance
    $emptyCatalogError = ''
    try {
        & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') acceptance-map `
            -AcceptancePath "$workspacePath/acceptance-matrix.json" `
            -CriterionId AC-001 `
            -ScenarioId $scenarioId
    } catch { $emptyCatalogError = $_.Exception.Message }
    if ($emptyCatalogError -notmatch "Unknown scenario id: $([regex]::Escape($scenarioId))" -or $emptyCatalogError -match "property 'id'") {
        throw "Empty acceptance catalog did not produce a stable diagnostic: $emptyCatalogError"
    }
    [IO.File]::WriteAllText($acceptancePath, $acceptanceRaw, [Text.UTF8Encoding]::new($false))

    $costPath = Join-Path $workspaceAbsolute 'verification-cost.json'
    & (Join-Path $repositoryRoot '.llm-wiki/tools/Manage-LlmWikiVerificationCost.ps1') create `
        -WorkspacePath $workspacePath `
        -Format Json | Out-Null
    $cost = Get-Content -LiteralPath $costPath -Raw | ConvertFrom-Json
    if (@($cost.estimates).Count -gt 0) {
        $cost.estimates[0].PSObject.Properties.Remove('verificationSeconds')
        Write-Json $costPath $cost
    }
    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-refresh `
        -WorkspacePath $workspacePath `
        -DryRun | Out-Null
    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-refresh `
        -WorkspacePath $workspacePath | Out-Null

    $acceptance = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    $changedPath = @($acceptance.availableEvidence.changedPaths | Where-Object { $_ } | Select-Object -First 1)
    $mapArguments = @{
        AcceptancePath = "$workspacePath/acceptance-matrix.json"
        CriterionId = 'AC-001'
        ScenarioId = $scenarioId
    }
    if ($changedPath.Count -eq 1) { $mapArguments.ChangedPath = [string]$changedPath[0] }
    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') acceptance-map @mapArguments | Out-Null
    $mapped = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    $mappedCriterion = $mapped.criteria | Where-Object id -eq 'AC-001' | Select-Object -First 1
    if ($scenarioId -notin @($mappedCriterion.mapping.scenarioIds)) {
        throw 'Acceptance mapping was not retained after the governed task refresh lifecycle.'
    }

    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') delivery-replan `
        -WorkspacePath $workspacePath `
        -Reason 'Regression fixture accepts the current Wiki implementation delta.' | Out-Null
    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') evidence-check `
        -EvidencePath "$workspacePath/evidence.json" `
        -Id wiki-verify `
        -Status passed `
        -EvidenceCommand './.llm-wiki/wiki.ps1 verify' `
        -DurationSeconds 1 | Out-Null
    $acceptance = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    foreach ($criterion in @($acceptance.criteria)) {
        $criterionMap = @{
            AcceptancePath = "$workspacePath/acceptance-matrix.json"
            CriterionId = [string]$criterion.id
            ScenarioId = $scenarioId
        }
        if ($changedPath.Count -eq 1) { $criterionMap.ChangedPath = [string]$changedPath[0] }
        & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') acceptance-map @criterionMap | Out-Null
        & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') acceptance-resolve `
            -AcceptancePath "$workspacePath/acceptance-matrix.json" `
            -CriterionId ([string]$criterion.id) `
            -AcceptanceStatus satisfied `
            -EvidenceNote 'Governed lifecycle regression evidence.' | Out-Null
    }

    $deliveryValidation = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') delivery-validate `
        -WorkspacePath $workspacePath `
        -FailOnInvalid `
        -Format Json | ConvertFrom-Json
    if (-not $deliveryValidation.valid) { throw 'Governed lifecycle delivery validation did not pass.' }
    $validatedAcceptance = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    if (@($deliveryValidation.assessment.automaticCheckLinks).Count -eq 0 -or @($validatedAcceptance.criteria | Where-Object { 'wiki-verify' -notin @($_.mapping.checkIds) }).Count -gt 0) {
        throw 'Delivery validation did not automatically link the completed required check to anchored acceptance criteria.'
    }
    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-proof-seal `
        -WorkspacePath $workspacePath `
        -Format Json | Out-Null
    $proofVerificationText = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-proof-verify `
        -WorkspacePath $workspacePath 6>&1 | Out-String
    if ($proofVerificationText -notmatch 'valid=True' -or $proofVerificationText -match "property 'issues'") {
        throw "Proof verification did not preserve its stable result schema: $proofVerificationText"
    }
    $contextSecurity = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-context-security-create `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if (-not $contextSecurity.valid -or $contextSecurity.assessment.summary.sourceCount -eq 0) {
        throw 'Context security did not discover manifest/change-packet paths when ChangedPath was omitted.'
    }

    $evidencePath = Join-Path $workspaceAbsolute 'evidence.json'
    $evidenceRaw = Get-Content -LiteralPath $evidencePath -Raw
    $staleEvidence = $evidenceRaw | ConvertFrom-Json
    $staleEvidence.checks[0].lineage.subject.definition = 'stale definition injected by regression test'
    Write-Json $evidencePath $staleEvidence
    $staleDelivery = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') delivery-status `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    $staleTaskStatus = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-status `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    $staleEvidenceGate = $staleDelivery.assessment.gates | Where-Object id -eq 'evidence' | Select-Object -First 1
    if ($staleEvidenceGate.passed -or $staleTaskStatus.verdict -ne 'blocked' -or $staleTaskStatus.evidenceLineage.valid) {
        throw 'Evidence lineage drift was not evaluated consistently by delivery-status and task-status.'
    }
    [IO.File]::WriteAllText($evidencePath, $evidenceRaw, [Text.UTF8Encoding]::new($false))

    $deliveryCritique = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') delivery-critique `
        -WorkspacePath $workspacePath `
        -FailOnInvalid `
        -Format Json | ConvertFrom-Json
    if (-not $deliveryCritique.valid) { throw 'Governed lifecycle delivery critique did not pass.' }

    $deliveryStatus = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') delivery-status `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if ($deliveryStatus.assessment.PSObject.Properties['refreshRequired'] -and $deliveryStatus.assessment.refreshRequired) {
        throw 'Refreshed governed workspace still reports packet drift.'
    }
    $readyTaskStatus = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-status `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if (@($readyTaskStatus.pendingCriteria).Count -eq 0 -and @($readyTaskStatus.nextActions | Where-Object { $_ -match 'task-requirements-expand' }).Count -gt 0) {
        throw 'Ready atomic acceptance criteria retained a stale task-requirements-expand recommendation.'
    }
    $finalization = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') delivery-finalize `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    if (-not $finalization.valid -or @($finalization.stages).Count -ne 5 -or @($finalization.stages | Where-Object status -ne 'passed').Count -gt 0) {
        throw "Delivery finalization did not complete all stages: $($finalization | ConvertTo-Json -Depth 5 -Compress)"
    }

    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-refresh `
        -WorkspacePath $workspacePath `
        -HeadRef HEAD | Out-Null
    $pinnedHead = (& git -C $repositoryRoot rev-parse --verify 'HEAD^{commit}').Trim()
    foreach ($name in @('workspace.json', 'task-contract.json')) {
        $artifact = Get-Content -LiteralPath (Join-Path $workspaceAbsolute $name) -Raw | ConvertFrom-Json
        if ([string]$artifact.git.head -cne $pinnedHead) { throw "Task refresh did not persist the resolved head SHA in $name." }
    }
    $pinnedStatus = & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') task-status `
        -WorkspacePath $workspacePath `
        -Format Json | ConvertFrom-Json
    $pinnedPacket = Get-Content -LiteralPath (Join-Path $workspaceAbsolute 'change-packet.json') -Raw | ConvertFrom-Json
    if ([string]$pinnedStatus.currentPacketFingerprint -cne [string]$pinnedPacket.fingerprint) {
        throw 'Task status did not reuse the persisted task head.'
    }
} finally {
    Remove-Item -LiteralPath $workspaceAbsolute -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki governed Authentication task-start tests passed.'
