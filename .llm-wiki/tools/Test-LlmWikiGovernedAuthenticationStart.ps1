[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$workspaceName = ".authentication-start-$([guid]::NewGuid().ToString('N'))"
$workspacePath = ".artifacts/llm-wiki/tasks/$workspaceName"
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
    if ('FD-AUTH' -notin @($acceptance.availableEvidence.scenarios | ForEach-Object { if ($_.PSObject.Properties['id']) { [string]$_.id } })) {
        throw 'Authentication journey was not exposed as acceptance evidence.'
    }
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
            -ScenarioId FD-AUTH
    } catch { $emptyCatalogError = $_.Exception.Message }
    if ($emptyCatalogError -notmatch 'Unknown scenario id: FD-AUTH' -or $emptyCatalogError -match "property 'id'") {
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
        ScenarioId = 'FD-AUTH'
    }
    if ($changedPath.Count -eq 1) { $mapArguments.ChangedPath = [string]$changedPath[0] }
    & (Join-Path $repositoryRoot '.llm-wiki/wiki.ps1') acceptance-map @mapArguments | Out-Null
    $mapped = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    $mappedCriterion = $mapped.criteria | Where-Object id -eq 'AC-001' | Select-Object -First 1
    if ('FD-AUTH' -notin @($mappedCriterion.mapping.scenarioIds)) {
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
            ScenarioId = 'FD-AUTH'
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
} finally {
    Remove-Item -LiteralPath $workspaceAbsolute -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'LLM Wiki governed Authentication task-start tests passed.'
